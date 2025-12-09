using UnityEngine;

public class WalkToDepositLeafNew : LeafNode
{
    public override string name { get; protected set; } = "WalkToDepositLeafNew";

    public override void Setup()
    {
        
    }

    public override Status Process()
    {
        var trabalhador = MyManager.tipoDeTrabalhador;
        Transform destino = null;

        // ---- COLETOR: está carregando algum recurso e deve ir para um depósito ----
        if (trabalhador == BehaviourTreeManager.TipoDeTrabalhador.Coletor)
        {
            // Se não estiver carregando nada, não faz sentido ir para depósito
            if (MyManager.InventarioVazio())
                return Status.Falha;

            switch (MyManager.recursoQueEstouCarregando)
            {
                case BehaviourTreeManager.TipoDeRecuso.Comida:
                    destino = MyManager.posicaoDepositoComida;
                    break;

                case BehaviourTreeManager.TipoDeRecuso.Madeira:
                case BehaviourTreeManager.TipoDeRecuso.Pedra:
                    destino = MyManager.posicaoDepositoMateriaisBrutos;
                    break;

                default:
                    // Tipo de recurso desconhecido / Nenhum
                    return Status.Falha;
            }
        }
        // ---- REFINADOR: ir buscar materiais brutos no depósito ----
        else if (trabalhador == BehaviourTreeManager.TipoDeTrabalhador.Refinador)
        {
            // Se inventário já está cheio, não precisa ir até o depósito
            if (MyManager.InventarioCheio() || MyManager.estouEmPerigo)
                return Status.Falha;

            destino = MyManager.posicaoDepositoMateriaisBrutos;
        }
        else
        {
            // Outros tipos de trabalhador não usam essa folha
            return Status.Falha;
        }

        if (destino == null)
            return Status.Falha;

        Debug.Log("Indo para o depósito (A*)");
        // Usa o sistema de tiles/A* do BehaviourTreeManager
        return MyManager.MoveToTransform(destino, 5f);
    }
}
