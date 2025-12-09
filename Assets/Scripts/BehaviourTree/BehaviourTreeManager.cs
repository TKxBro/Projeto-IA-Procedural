using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;
using Random = UnityEngine.Random;

public class BehaviourTreeManager : MonoBehaviour
{

    public enum TipoDeTrabalhador
    {
        Coletor,
        Refinador,
        Construtor,
        Guarda,
        Teste,
    }

    public enum TipoDeRecuso
    {
        Nenhum,
        Madeira,
        Pedra,
        Comida,
        MadeiraRefinada,
        PedraRefinada,
    }
    
    [Header("Tipo de trabalhador")]
    public TipoDeTrabalhador tipoDeTrabalhador;

    [Header("Configs Fome")]
    public float fomeMaxima = 100f;
    public float fomeAtual;
    public Transform posicaoDepositoComida;
    public DepositoComida scriptDepositoComida;
    
    [Header("Configs Patrulha")]
    public List<Transform> patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Configs detecção e distâncias")]
    public float detectionRange = 5f;
    public float distanceToCollectResource = 2f;
    public LayerMask layerArvore;
    public LayerMask layerPedra;
    public LayerMask layerComida;
    public LayerMask groundLayerMask;
    [HideInInspector] public Transform posicaoArvoreProxima = null;

    private float baseDetectionRange;
    private bool resourceLocked = false;

    //COISAS NOVAS
    [Header("Detecção de Tiles de Recurso")]
    public TipoDeRecuso tipoTileRecursoProximo = TipoDeRecuso.Nenhum;
    public Tile tileRecursoProximo = null;
    //COISAS NOVAS

    [Header("Componentes")]
    public NavMeshAgent navMeshAgent;
    
    [Header("Configs Depositos")]
    public Transform posicaoDepositoMateriaisBrutos;
    public DepositoMateriais scriptDepositoMateriaisBrutos;
    public Transform posicaoRefinaria;
    public Transform posicaoDepositoMateriaisRefinados;
    public DepositoRefinados scriptDepositoRefinados;
    public BehaviourTreeManager.TipoDeRecuso materialAguardado = BehaviourTreeManager.TipoDeRecuso.Nenhum;
    public string debugAguardando = "";
    public Transform posicaoRefugio;
    public bool estouEmPerigo = false;  
    private bool estavaEmPerigo = false;
    
    [Header("Configs Cabanas")]
    public CabanasManager scripsCabanas;
    
    [Header("Configs Inventário")]
    public int capacidadeAtual = 0;
    public TipoDeRecuso tipoRecursoAtual = TipoDeRecuso.Nenhum; //Armazena o tipo de recurso do recurso(arvore,pedra,comida) em que estou a caminho
    public TipoDeRecuso recursoQueEstouCarregando; //Armazena o tipo de recurso que eu já coletei

    [Header("Componentes (Tiles/A*)")]
    public Character tileCharacter;   // referência ao seu script Character (tiles)
    public Pathfinder pathfinder;     // referência ao Pathfinder do A*
    public LayerMask groundTileMask;  // máscara dos tiles (mesma ideia do GroundLayerMask)

    private Path currentPath;
    private Tile targetTile;
    private Node rootNode;
    private Animator _animator;
    private AudioSource _audioSource;


    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        //navMeshAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
         baseDetectionRange = detectionRange;

        // ===== Tiles/A* =====
        if (tileCharacter == null)
            tileCharacter = GetComponent<Character>(); // precisa existir no prefab

        if (pathfinder == null)
            pathfinder = FindObjectOfType<Pathfinder>(); // ou GameObject.Find("Pathfinder").GetComponent<Pathfinder>();

        if (tileCharacter == null)
            Debug.LogError($"[{name}] Não achei o componente Character (tiles). Adicione ele no NPC.");

        if (pathfinder == null)
            Debug.LogError("Não achei Pathfinder na cena.");
    }

    void Start()
    {
        fomeAtual = fomeMaxima;
        InitializeTree();
    }

    void Update()
    {
        Tick();
        
        ControlarAnimator();
        
        fomeAtual -=  Time.deltaTime;
        if (fomeAtual <= 0)
        {
            Morrer();
        }
    }

   private void Tick()
{
    if (rootNode == null) return;

    if (estouEmPerigo && !estavaEmPerigo)
    {
        PararNoTileAtual();

        rootNode.Reset();
    }

    estavaEmPerigo = estouEmPerigo;

    var status = rootNode.Process();

    switch (status)
    {
        case Status.Sucesso:
        case Status.Falha:
            rootNode.Reset();   
            break;

        case Status.EmAndamento:
        case Status.Desconhecido:
            break;
    }
}

    private void InitializeTree()
    {
        switch (tipoDeTrabalhador)
        {
            case TipoDeTrabalhador.Coletor:
                rootNode = new ColetorRootNode();
                break;
            case TipoDeTrabalhador.Refinador:
                rootNode = new RefinadorRootNode();
                break;
            case TipoDeTrabalhador.Construtor:
                rootNode = new ConstrutorRootNode();
                break;
            case TipoDeTrabalhador.Teste:
                rootNode = new TesteRootNode();
                break;
            case TipoDeTrabalhador.Guarda:
                rootNode = new GuardRootNode();
                break;
        }
        
        rootNode.Setup();
        rootNode.SetManager(this);
    }

    public void Patrulhar()
    {
        navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
        if (navMeshAgent.remainingDistance <= 2f)
        {
            currentPatrolIndex = Random.Range(0, patrolPoints.Count);
        }
    }

    public Status PatrulharTiles(float arriveRadius = 2f)
    {
        Transform p = patrolPoints[currentPatrolIndex];
        var st = MoveToTransform(p, arriveRadius);

        if (st == Status.Sucesso)
            currentPatrolIndex = Random.Range(0, patrolPoints.Count);

        return st;
    }

    public Status MoveToTransform(Transform target, float arriveRadiusWorld = 1f)
{
    if (target == null || tileCharacter == null || pathfinder == null)
        return Status.Falha;

    float dist = Vector3.Distance(transform.position, target.position);
    if (dist <= arriveRadiusWorld)
        return Status.Sucesso;

    // 1) Sincroniza o tile atual com a posição real
    Tile currentTile = GetTileFromWorld(transform.position);
    if (currentTile == null)
        return Status.Falha;

    if (tileCharacter.characterTile != currentTile)
    {
        if (tileCharacter.characterTile != null)
        {
            tileCharacter.characterTile.Occupied = false;
            tileCharacter.characterTile.occupyingCharacter = null;
        }

        tileCharacter.characterTile = currentTile;
        currentTile.Occupied = true;
        currentTile.occupyingCharacter = tileCharacter;
    }

    // 2) Destino
    Tile dest = GetTileFromWorld(target.position);
    if (dest == null || dest.Occupied)
        return Status.Falha;

    // 3) Calcula path a partir do tile sincronizado
    if (targetTile != dest || currentPath == null)
    {
        targetTile = dest;
        currentPath = pathfinder.FindPath(tileCharacter.characterTile, targetTile);
        if (currentPath == null)
            return Status.Falha;

        tileCharacter.StartMove(currentPath);
    }
    else if (!tileCharacter.Moving)
    {
        tileCharacter.StartMove(currentPath);
    }

    return Status.EmAndamento;
}

    public bool ColetarRecurso(TipoDeRecuso tipo)
    {
        if (tipo == TipoDeRecuso.Nenhum) return false;

        if (tipoRecursoAtual == TipoDeRecuso.Nenhum)
        {
            tipoRecursoAtual = tipo;
            recursoQueEstouCarregando = tipo;
        }

        if (InventarioCheio())
        {
            return false;
        }
        
        capacidadeAtual++;
        return true;
    }

    public bool RemoverRecurso()
    {
        if (tipoRecursoAtual == TipoDeRecuso.Nenhum || InventarioVazio()) return false;
        
        capacidadeAtual--;
        tipoRecursoAtual = TipoDeRecuso.Nenhum;
        recursoQueEstouCarregando = TipoDeRecuso.Nenhum;
        
        return true;
    }

    public bool SeAlimentar()
    {
        if(!EstouComFome()) return false;
        
        fomeAtual = fomeMaxima;
        return true;
    }
    
    public TipoDeRecuso PegarTipoDeRecursoAtual()
    {
        if (posicaoArvoreProxima == null)
            return TipoDeRecuso.Nenhum;

        int layer = posicaoArvoreProxima.gameObject.layer;

        if (layer == LayerMask.NameToLayer("Arvore"))
            return TipoDeRecuso.Madeira;

        if (layer == LayerMask.NameToLayer("Pedra"))
            return TipoDeRecuso.Pedra;
        
        if(layer == LayerMask.NameToLayer("Comida"))
            return TipoDeRecuso.Comida;

        return TipoDeRecuso.Nenhum;
    }

    public void Morrer()
    {
        scripsCabanas.trabalhadoresAtivos--;
        Destroy(gameObject);
    }

    public bool TemRecursoProximo() 
    {
        var layerRecurso = layerArvore | layerPedra | layerComida;
        var colliderArvores = Physics.OverlapSphere(transform.position, detectionRange, layerRecurso);
        if (colliderArvores.Length == 0)
        {
            posicaoArvoreProxima = null;
            return false;
        }
        
        float menorDistancia = float.MaxValue;
        Transform arvoreMaisProxima = null;
        
        foreach (var colliderArvore in colliderArvores)
        {
            float distancia = Vector3.Distance(transform.position, colliderArvore.transform.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                arvoreMaisProxima = colliderArvore.transform;
            }
        }
        
        posicaoArvoreProxima = arvoreMaisProxima;
        return posicaoArvoreProxima != null;
    }

    public bool TemTileDeRecursoProximo()
{
    tipoTileRecursoProximo = TipoDeRecuso.Nenhum;
    tileRecursoProximo = null;

    // raio dinâmico: normal ou 1.5x se já “travou” num recurso
    float radius = baseDetectionRange * (resourceLocked ? 1.5f : 1f);

    Collider[] tilesPerto = Physics.OverlapSphere(transform.position, radius, groundTileMask);

    if (tilesPerto == null || tilesPerto.Length == 0)
    {
        resourceLocked = false;
        return false;
    }

    float menorDist = float.MaxValue;

    foreach (var col in tilesPerto)
    {
        Tile t = col.GetComponent<Tile>();
        if (t == null) continue;

        if (!t.CompareTag("Arvore") && !t.CompareTag("Pedra") && !t.CompareTag("Comida"))
            continue;

        float d = Vector3.Distance(transform.position, t.transform.position);
        if (d < menorDist)
        {
            menorDist = d;
            tileRecursoProximo = t;
            tipoTileRecursoProximo = TagToTipoRecurso(t.tag);
        }
    }

    // se achou recurso, "trava" e aumenta alcance nas próximas chamadas
    resourceLocked = tileRecursoProximo != null;
    return resourceLocked;
}
    private TipoDeRecuso TagToTipoRecurso(string tag)
    {
        switch (tag)
        {
            case "Arvore": return TipoDeRecuso.Madeira;
            case "Pedra": return TipoDeRecuso.Pedra;
            case "Comida": return TipoDeRecuso.Comida;
            default: return TipoDeRecuso.Nenhum;
        }
    }
    public void RemoverRecursoDoMapa()
    {
        if (posicaoArvoreProxima == null && tileRecursoProximo == null) return;
        tileRecursoProximo.RemoveResource();
        //Destroy(posicaoArvoreProxima.gameObject);
    }
   private void ControlarAnimator()
{
    if (tileCharacter != null && tileCharacter.Moving)
    {
        _animator.speed = 1f;
    }
    else
    {
        _animator.speed = 0f;
    }
}

    public Tile GetTileFromWorld(Vector3 worldPos, float rayHeight = 6f)
    {
        Vector3 start = worldPos + Vector3.up * rayHeight;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, rayHeight * 2f, groundTileMask))
            return hit.transform.GetComponent<Tile>();

        return null;
    }
    public Tile GetClosestFreeNeighbor(Tile center)
    {
        if (center == null) return null;

        const float HEXAGONAL_OFFSET = 1.75f;
        float rayLength = 6f;
        float rayHeightOffset = 2f;

        Vector3 direction = Vector3.forward *
            (center.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * HEXAGONAL_OFFSET);

        Tile best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < 6; i++)
        {
            direction = Quaternion.Euler(0f, 60f, 0f) * direction;

            Vector3 abovePos = center.transform.position + direction;
            abovePos.y = center.transform.position.y + rayHeightOffset;

            if (Physics.Raycast(abovePos, Vector3.down, out RaycastHit hit, rayLength, groundTileMask))
            {
                Tile t = hit.transform.GetComponent<Tile>();
                if (t != null && !t.Occupied)
                {
                    float d = Vector3.Distance(transform.position, t.transform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = t;
                    }
                }
            }
        }

        // considera ladder também
        if (center.connectedTile != null && !center.connectedTile.Occupied)
        {
            float d = Vector3.Distance(transform.position, center.connectedTile.transform.position);
            if (d < bestDist) best = center.connectedTile;
        }

        return best;
    }

    public void PararNoTileAtual() 
    {
    // Para o NavMesh, caso ainda esteja sendo usado
    if (navMeshAgent != null)
    {
        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
        navMeshAgent.velocity = Vector3.zero;
    }

    // Para o sistema de tiles/A*
    if (tileCharacter != null)
    {
        tileCharacter.StopMove();
    }

    // Limpa qualquer path/objetivo pendente
    currentPath = null;
    targetTile  = null;
    }

    public bool EstouComFome() => fomeAtual <= fomeMaxima * 0.3;
    public bool InventarioCheio() => capacidadeAtual >= 1;
    public bool InventarioVazio() => capacidadeAtual == 0;
    public bool TemMadeira() => recursoQueEstouCarregando == TipoDeRecuso.Madeira; 
    public bool TemPedra() => recursoQueEstouCarregando == TipoDeRecuso.Pedra;
    public bool TemComida() =>  recursoQueEstouCarregando == TipoDeRecuso.Comida;
    public bool TemPedraRefinada() => recursoQueEstouCarregando == TipoDeRecuso.PedraRefinada;
    public bool TemMadeiraRefinada() => recursoQueEstouCarregando == TipoDeRecuso.MadeiraRefinada;
}
