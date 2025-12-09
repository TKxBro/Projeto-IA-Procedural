using UnityEngine;

public class RefineResourceSequence : SequenceNode
{
    public override string name { get; protected set; } = "GetRawResourceSelector";

    public override void Setup()
    {
        AddChild(new WalkToRefineLeafNew());
        AddChild(new RefineResourceLeaf());
        AddChild(new WalkToRefineDepositLeafNew());
        AddChild(new DepositRefineResourceSequence());
    }
}
