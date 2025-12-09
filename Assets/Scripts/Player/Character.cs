using System.Collections;
using UnityEngine;

public class Character : MonoBehaviour
{
    #region member fields
    public bool Moving { get; private set; } = false;

    public CharacterMoveData movedata;
    public Tile characterTile;
    [SerializeField]
    LayerMask GroundLayerMask;
    private Coroutine moveRoutine;
    #endregion

    private void Awake()
    {
        FindTileAtStart();
    }

    /// <summary>
    /// If no starting tile has been manually assigned, we find one beneath us
    /// </summary>
    void FindTileAtStart()
    {
        if (characterTile != null)
        {
            FinalizePosition(characterTile);
            return;
        }

        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 50f, GroundLayerMask))
        {
            FinalizePosition(hit.transform.GetComponent<Tile>());
            return;
        }

        Debug.Log("Unable to find a start position");
    }

    IEnumerator MoveAlongPath(Path path)
    {
        const float MIN_DISTANCE = 0.05f;
        const float TERRAIN_PENALTY = 0.5f;

        int currentStep = 0;
        int pathLength = path.tiles.Length - 1;
        Tile currentTile = path.tiles[0];
        float animationTime = 0f;

        while (currentStep <= pathLength)
        {
            yield return null;

            Vector3 nextTilePosition = path.tiles[currentStep].transform.position;

            float movementTime = animationTime / (movedata.MoveSpeed + path.tiles[currentStep].terrainCost * TERRAIN_PENALTY);
            MoveAndRotate(currentTile.transform.position, nextTilePosition, movementTime);
            animationTime += Time.deltaTime;

            if (Vector3.Distance(transform.position, nextTilePosition) > MIN_DISTANCE)
                continue;

            currentTile = path.tiles[currentStep];
            currentStep++;
            animationTime = 0f;
        }

        FinalizePosition(path.tiles[pathLength]);
    }

    public void StartMove(Path _path)
    {
        // se j� estiver se movendo, interrompe o movimento anterior
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        Moving = true;
        characterTile.Occupied = false;
        moveRoutine = StartCoroutine(MoveAlongPath(_path));
    }

    void FinalizePosition(Tile tile)
    {
        transform.position = tile.transform.position;
        characterTile = tile;
        Moving = false;

        // terminou este movimento
        moveRoutine = null;

        tile.Occupied = true;
        tile.occupyingCharacter = this;
    }

    void MoveAndRotate(Vector3 origin, Vector3 destination, float duration)
    {
        transform.position = Vector3.Lerp(origin, destination, duration);
        transform.rotation = Quaternion.LookRotation(origin.DirectionTo(destination).Flat(), Vector3.up);
    }

    public void StopMove()
{
    if (moveRoutine != null)
    {
        StopCoroutine(moveRoutine);
        moveRoutine = null;
    }

    Moving = false;

    // Garante que o personagem fique encaixado em um tile
    if (characterTile == null)
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 50f, GroundLayerMask))
        {
            FinalizePosition(hit.transform.GetComponent<Tile>());
        }
    }
    else
    {
        FinalizePosition(characterTile);
    }
}


}