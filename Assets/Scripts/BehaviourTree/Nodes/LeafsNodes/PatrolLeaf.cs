using UnityEngine;

public class PatrolLeaf : LeafNode
{
    public override string name { get; protected set; } = "Patrol Leaf";


    public override void Setup()
    {
        
    }

    public override Status Process()  
    {
        if (MyManager.TemTileDeRecursoProximo() && MyManager.InventarioVazio() || MyManager.estouEmPerigo) return Status.Falha;
        else if(MyManager.InventarioCheio()) return Status.Falha;
        
        return MyManager.PatrulharTiles();
    }
}
