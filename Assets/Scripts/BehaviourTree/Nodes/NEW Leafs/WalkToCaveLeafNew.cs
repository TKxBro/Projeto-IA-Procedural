using UnityEngine;

public class WalkToCaveLeafNew : LeafNode
{
    public override string name { get; protected set; } = "WalkToCaveLeafNew";

    public override void Setup()
    {
        
    }

    public override Status Process()
    {
        if(MyManager.estouEmPerigo == false) return Status.Falha;
        Debug.Log("AI TO COM MEDO");
        return MyManager.MoveToTransform(MyManager.posicaoRefugio, 0f);
    }
}
