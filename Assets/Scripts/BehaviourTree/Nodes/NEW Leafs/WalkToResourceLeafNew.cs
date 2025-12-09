using UnityEngine;

public class WalkToResourceLeafNew : LeafNode
{
    public override string name { get; protected set; } = "NewWalkToResourceLeaf";

    public override void Setup()
    {
        
    }

    public override Status Process()
    {
        if(MyManager.InventarioCheio() ||!MyManager.TemTileDeRecursoProximo() || MyManager.estouEmPerigo)
            return Status.Falha;

        Debug.Log("Achei Recurso");

        Tile recursoTile = MyManager.tileRecursoProximo;
        if (recursoTile == null)
            return Status.Falha;

        // Se o tile do recurso estiver ocupado, v� para um vizinho livre
        if (recursoTile.Occupied)
        {
            Tile vizinho = MyManager.GetClosestFreeNeighbor(recursoTile);
            if (vizinho == null)
                return Status.Falha;

            return MyManager.MoveToTransform(vizinho.transform, 1f);
        }

        // Se n�o estiver ocupado, vai direto nele
        return MyManager.MoveToTransform(recursoTile.transform, 1f);
    }
}
