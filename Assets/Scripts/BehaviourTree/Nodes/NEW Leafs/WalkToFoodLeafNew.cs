using UnityEngine;

public class WalkToFoodLeafNew : LeafNode
{
    public override string name { get; protected set; } = "WalkToFoodLeafNew";

    public override void Setup()
    {
        
    }

    public override Status Process()
    {
         if (!MyManager.EstouComFome() || MyManager.fomeAtual <= 0f) return Status.Falha;
        
        Debug.Log("AI QUE FOME");
        
        return MyManager.MoveToTransform(MyManager.posicaoDepositoComida, 4f);
    }
}
