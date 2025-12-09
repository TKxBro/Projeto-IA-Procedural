using UnityEngine;
using System.Collections.Generic;

public class Recurso : MonoBehaviour
{
    public enum Recursos
    {
        Madeira,
        Pedra,
    }

    public Tile myTile;
    [SerializeField]
    LayerMask GroundLayerMask;

    public Recursos meuRecurso = Recursos.Madeira;

    void Start()
    {
        FindTileAtStart();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FindTileAtStart()
    {
        if (myTile != null)
        {
            FinalizePosition(myTile);
            return;
        }

        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 50f, GroundLayerMask))
        {
            FinalizePosition(hit.transform.GetComponent<Tile>());
            return;
        }

        Debug.Log("Unable to find a start position");
    }

    void FinalizePosition(Tile tile)
    {
        switch (meuRecurso)
        {
            case Recursos.Madeira:
                tile.gameObject.tag = "Arvore";
                break;
        }
        transform.position = tile.transform.position;
        myTile = tile;
        tile.Occupied = true;
    }
}
