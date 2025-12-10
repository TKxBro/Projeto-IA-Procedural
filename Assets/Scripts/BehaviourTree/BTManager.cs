using UnityEngine;
using System.Collections.Generic;

public class BTManager : MonoBehaviour
{

    public enum TipoDeTrabalhador
    {
        Coletor,
        Refinador,
        Construtor,
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

    [Header("Behaviour Tree")]
    public TipoDeTrabalhador tipoDeTrabalhador;

    private Node rootNode;

    [Header("Patrulha")]
    public List<Transform> patrolPoints = new List<Transform>();
    [SerializeField] private int currentPatrolIndex = 0;
    [SerializeField] private float patrolArriveRadius = 2f;

    [Header("Movimenta��o por Tiles / A*")]
    public Character tileCharacter;          
    public Pathfinder pathfinder;           
    public LayerMask groundTileMask;        

    private Path currentPath;
    private Tile targetTile;

    // ==========================
    //      DETEC��O DE RECURSOS
    // ==========================

    [Header("Detec��o de Tiles de Recurso")]
    public float detectionRange = 5f;
    public TipoDeRecuso tipoTileRecursoProximo = TipoDeRecuso.Nenhum;
    public Tile tileRecursoProximo = null;


    private Animator _animator;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponent<Animator>();

        if (tileCharacter == null)
            tileCharacter = GetComponent<Character>();

        if (pathfinder == null)
            pathfinder = FindObjectOfType<Pathfinder>();

        if (tileCharacter == null)
            Debug.LogError($"[{name}] NewBehaviourManager: Character (tiles) n�o encontrado neste NPC.");

        if (pathfinder == null)
            Debug.LogError($"[{name}] NewBehaviourManager: Pathfinder n�o encontrado na cena.");
    }

    private void Start()
    {
        InitializeTree();
    }

    private void Update()
    {
        Tick();
        AtualizarAnimator();
    }


    private void Tick()
    {
        if (rootNode == null) return;

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
        }

        if (rootNode == null)
        {
            Debug.LogError($"[{name}] NewBehaviourManager: rootNode n�o foi definido para o tipo {tipoDeTrabalhador}");
            return;
        }

        rootNode.Setup();
        //rootNode.SetManager(this);
    }

 
    public Status PatrulharTiles()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
            return Status.Falha;

        Transform p = patrolPoints[currentPatrolIndex];
        var st = MoveToTransform(p, patrolArriveRadius);

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

        Tile dest = GetTileFromWorld(target.position);
        if (dest == null || dest.Occupied)
            return Status.Falha;

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

    public Tile GetTileFromWorld(Vector3 worldPos, float rayHeight = 6f)
    {
        Vector3 start = worldPos + Vector3.up * rayHeight;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, rayHeight * 2f, groundTileMask))
            return hit.transform.GetComponent<Tile>();

        return null;
    }

    public bool TemTileDeRecursoProximo()
    {
        tipoTileRecursoProximo = TipoDeRecuso.Nenhum;
        tileRecursoProximo = null;

        Collider[] tilesPerto = Physics.OverlapSphere(transform.position, detectionRange, groundTileMask);

        if (tilesPerto == null || tilesPerto.Length == 0)
            return false;

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

        return tileRecursoProximo != null;
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

        if (center.connectedTile != null && !center.connectedTile.Occupied)
        {
            float d = Vector3.Distance(transform.position, center.connectedTile.transform.position);
            if (d < bestDist) best = center.connectedTile;
        }

        return best;
    }

    

    private void AtualizarAnimator()
    {
        if (_animator == null || tileCharacter == null)
            return;

        _animator.speed = tileCharacter.Moving ? 1f : 0f;
    }
}
