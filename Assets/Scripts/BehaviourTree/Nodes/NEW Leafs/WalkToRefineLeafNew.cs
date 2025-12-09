using UnityEngine;

public class WalkToRefineLeafNew : LeafNode
{
    public override string name { get; protected set; } = "WalkToRefineLeafNew";

    public override void Setup()
    {
     
    }

    public override Status Process()
    {
         if (MyManager.TemMadeiraRefinada() || MyManager.TemPedraRefinada()) return Status.Sucesso; 
        if (!MyManager.TemPedra() && !MyManager.TemMadeira()) return  Status.Falha;
        
        return MyManager.MoveToTransform(MyManager.posicaoRefinaria, 4f);
    }
}
