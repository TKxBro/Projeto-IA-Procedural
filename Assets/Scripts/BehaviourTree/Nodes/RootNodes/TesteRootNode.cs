using UnityEngine;

public class TesteRootNode : SelectorNode
{
    public override string name { get; protected set; } = "TesteRootNode";

    public override void Setup()
    {
        AddChild(new FindResourceSelector());
    }
}
