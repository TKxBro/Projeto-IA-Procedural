using UnityEngine;

public class DepositResourcesSequence : SequenceNode
{
    public override string name { get; protected set; } =  "DepositResourcesSequence";

    public override void Setup()
    {
        AddChild(new WalkToDepositLeafNew());
        AddChild(new DepositResourceLeaf());
    }
}
