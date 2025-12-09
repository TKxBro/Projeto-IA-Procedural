using UnityEngine;

public class GetRawResourceSequence : SequenceNode
{
    public override string name { get; protected set; } =  "GetRawResourceSequence";

    public override void Setup()
    {
        AddChild(new WalkToDepositLeafNew());
        AddChild(new CollectResourceLeaf());
    }
}
