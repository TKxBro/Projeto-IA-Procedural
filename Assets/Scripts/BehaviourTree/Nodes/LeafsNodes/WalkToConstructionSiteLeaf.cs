using UnityEngine;

public class WalkToConstructionSiteLeaf : LeafNode
{
    public override string name { get; protected set; } =  "WalkToConstructionSiteLeaf";

    public override void Setup()
    {
        
    }

    public override Status Process()
    {
        if (MyManager.InventarioVazio()) return Status.Falha;

        return MyManager.MoveToTransform(MyManager.scripsCabanas.posicaoCasaEmConstrucao, 4f);
    }
}
