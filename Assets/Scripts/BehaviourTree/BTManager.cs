using UnityEngine;
using System.Collections.Generic;

public class BTManager : MonoBehaviour
{
      // ==========================
    //      ENUMS / TIPOS
    // ==========================

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

    // ==========================
    //      CONFIG BT
    // ==========================

    [Header("Behaviour Tree")]
    public TipoDeTrabalhador tipoDeTrabalhador;

    private Node rootNode;

    // ==========================
    //      PATRULHA
    // ==========================

    [Header("Patrulha")]
    public List<Transform> patrolPoints = new List<Transform>();
    [SerializeField] private int currentPatrolIndex = 0;
    [SerializeField] private float patrolArriveRadius = 2f;

    // ==========================
    //      MOVIMENTO TILE / A*
    // ==========================

    [Header("Movimenta��o por Tiles / A*")]
    public Character tileCharacter;          // componente Character (tiles) neste NPC
    public Pathfinder pathfinder;           // refer�ncia ao Pathfinder da cena
    public LayerMask groundTileMask;        // layer dos tiles

    private Path currentPath;
    private Tile targetTile;

    // ==========================
    //      DETEC��O DE RECURSOS
    // ==========================

    [Header("Detec��o de Tiles de Recurso")]
    public float detectionRange = 5f;
    public TipoDeRecuso tipoTileRecursoProximo = TipoDeRecuso.Nenhum;
    public Tile tileRecursoProximo = null;

    // ==========================
    //      COMPONENTES VISUAIS
    // ==========================

    private Animator _animator;
    private AudioSource _audioSource;

    // ==========================
    //      UNITY LIFECYCLE
    // ==========================

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

    // ==========================
    //      BT CORE
    // ==========================

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
                // segue rodando
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

    // ==========================
    //      MOVIMENTO / PATRULHA
    // ==========================

    /// <summary>
    /// Patrulha simples: anda at� o ponto atual e, ao chegar, escolhe outro aleat�rio.
    /// Deve ser chamado pela leaf de patrulha.
    /// </summary>
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

    /// <summary>
    /// Move usando A* e tiles at� o Transform alvo.
    /// Respeita raio de chegada em dist�ncia de mundo.
    /// </summary>
    public Status MoveToTransform(Transform target, float arriveRadiusWorld = 1f)
    {
        if (target == null || tileCharacter == null || pathfinder == null)
            return Status.Falha;

        // 1) Chegou no alvo em dist�ncia do mundo?
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= arriveRadiusWorld)
            return Status.Sucesso;

        // 2) Resolver tile destino
        Tile dest = GetTileFromWorld(target.position);
        if (dest == null || dest.Occupied)
            return Status.Falha;

        // 3) Se o destino mudou ou ainda n�o temos path, recalcula
        if (targetTile != dest || currentPath == null)
        {
            targetTile = dest;
            currentPath = pathfinder.FindPath(tileCharacter.characterTile, targetTile);
            if (currentPath == null)
                return Status.Falha;

            // dispara (ou substitui) o movimento atual
            tileCharacter.StartMove(currentPath);
        }
        else if (!tileCharacter.Moving)
        {
            // j� temos path pra esse destino, mas n�o estamos andando (por seguran�a)
            tileCharacter.StartMove(currentPath);
        }

        return Status.EmAndamento;
    }

    /// <summary>
    /// Raycast para encontrar o Tile abaixo de uma posi��o de mundo.
    /// </summary>
    public Tile GetTileFromWorld(Vector3 worldPos, float rayHeight = 6f)
    {
        Vector3 start = worldPos + Vector3.up * rayHeight;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, rayHeight * 2f, groundTileMask))
            return hit.transform.GetComponent<Tile>();

        return null;
    }

    // ==========================
    //      DETEC��O DE RECURSOS
    // ==========================

    /// <summary>
    /// Detecta tiles de recurso (tag Arvore/Pedra/Comida) pr�ximos.
    /// Armazena o tipo em tipoTileRecursoProximo e o tile em tileRecursoProximo.
    /// </summary>
    public bool TemTileDeRecursoProximo()
    {
        tipoTileRecursoProximo = TipoDeRecuso.Nenhum;
        tileRecursoProximo = null;

        // Procura qualquer tile na �rea de detec��o
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

    /// <summary>
    /// Converte a tag do tile no enum de recurso.
    /// </summary>
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

    /// <summary>
    /// Retorna o vizinho livre mais pr�ximo em volta de um tile (hex + ladder).
    /// �til quando o tile do recurso est� ocupado.
    /// </summary>
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

        // Ladder como alternativa
        if (center.connectedTile != null && !center.connectedTile.Occupied)
        {
            float d = Vector3.Distance(transform.position, center.connectedTile.transform.position);
            if (d < bestDist) best = center.connectedTile;
        }

        return best;
    }

    // ==========================
    //      VISUAL / ANIMA��O
    // ==========================

    private void AtualizarAnimator()
    {
        if (_animator == null || tileCharacter == null)
            return;

        // simples: andando = anima��o ligada; parado = congelada
        _animator.speed = tileCharacter.Moving ? 1f : 0f;
    }
}
