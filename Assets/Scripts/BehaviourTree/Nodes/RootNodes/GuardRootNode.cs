using UnityEngine;

public class GuardRootNode : SelectorNode
{
    public override string name { get; protected set; } = "Guard Root Node";
    public override void Setup()
    {
        AddChild(new PatrolLeaf());
    }
}
