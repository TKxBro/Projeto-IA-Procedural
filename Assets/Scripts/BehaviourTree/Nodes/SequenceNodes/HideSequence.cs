using UnityEngine;

public class HideSequence : SequenceNode
{
    public override string name { get; protected set; } = "Hide Sequence";

    public override void Setup()
    {
        AddChild(new WalkToCaveLeafNew());
        AddChild(new WaitForDangerLeafNwe());
    }
}
