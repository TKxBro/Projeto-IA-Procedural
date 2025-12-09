using Unity.VisualScripting;
using UnityEngine;

public class ConstrutorRootNode : SelectorNode
{
    public override string name { get; protected set; } =  "ConstrutorRootNode";

    public override void Setup()
    {
        AddChild(new HideSequence());
        AddChild(new FindFoodSelector());
        AddChild(new GetRefineResourceSelector());
        AddChild(new BuildHouseSequence());
    }
}
