using UnityEngine;

public class WaitForDangerLeafNwe : LeafNode
{
    public override string name { get; protected set; } = "WaitForDangerLeafNwe";
    public override void Setup()
    {
        
    }
    public override Status Process()
    {
        if(MyManager.estouEmPerigo == false) return Status.Sucesso;

        MyManager.PararNoTileAtual();
        Debug.Log("parei");
        return Status.EmAndamento;
    }
}
