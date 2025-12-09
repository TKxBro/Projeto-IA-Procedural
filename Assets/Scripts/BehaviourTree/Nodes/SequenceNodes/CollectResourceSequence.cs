using UnityEngine;

public class CollectResourceSequence : SequenceNode
{
    public override string name { get; protected set; } = "Collect Resource Sequence";

    public override void Setup()
    {
        AddChild(new WalkToResourceLeafNew());
        AddChild(new CollectResourceLeafNew());
    }
}
