using System.Diagnostics;
using UnityEngine;

public class WalkToRefineDepositLeafNew : LeafNode
{
    public override string name { get; protected set; } =  "WalkToRefineDepositLeafNew";

    public override void Setup()
    {
        
    }

    public override Status Process()
    {
        if(MyManager.estouEmPerigo) return Status.Falha;

        if (MyManager.tipoDeTrabalhador == BehaviourTreeManager.TipoDeTrabalhador.Refinador)
        {
            if (!MyManager.TemMadeiraRefinada() && !MyManager.TemPedraRefinada())
                return Status.Falha;

        }

        if (MyManager.tipoDeTrabalhador == BehaviourTreeManager.TipoDeTrabalhador.Construtor)
        {
            if (MyManager.TemMadeiraRefinada() || MyManager.TemMadeiraRefinada()) return Status.Falha;
            
        }

        return MyManager.MoveToTransform(MyManager.posicaoDepositoMateriaisRefinados, 6f);
    }
}
