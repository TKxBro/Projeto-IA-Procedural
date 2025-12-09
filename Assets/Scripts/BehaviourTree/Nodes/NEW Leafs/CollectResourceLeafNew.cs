using UnityEngine;

public class CollectResourceLeafNew : LeafNode
{
    public override string name { get; protected set; } = "CollectResourceLeafNew";

    private float _timeToCollect = 3f;
    public override void Setup()
    {
        
    }

    public override Status Process()
    {
        if(MyManager.tipoDeTrabalhador == BehaviourTreeManager.TipoDeTrabalhador.Coletor)
        {
             _timeToCollect -= Time.deltaTime;

            if (_timeToCollect <= 0)
            {
                var tipo = MyManager.tipoTileRecursoProximo;

                Debug.Log($"Terminei de coletar {tipo}");

                bool coletei = MyManager.ColetarRecurso(tipo);

                if (coletei)
                {
                    Debug.Log($"Coletei {tipo}");
                    _timeToCollect = 3f;
                    //MyManager.recursoQueEstouCarregando = tipo;
                    MyManager.RemoverRecursoDoMapa();
                    return Status.Sucesso;
                }
                else
                {
                    Debug.LogError("Deu errado a coleta");
                    _timeToCollect = 3f;
                    return Status.Falha;
                }
            }
        }

        if(MyManager.tipoDeTrabalhador == BehaviourTreeManager.TipoDeTrabalhador.Construtor)
        {

        }

        return Status.EmAndamento;
    }

}

