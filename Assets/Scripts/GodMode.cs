using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GodMode : MonoBehaviour
{
    public enum TiposDeRecurso
    {
        Madeira,
        Pedra,
        Comida,
    }

    public TiposDeRecurso recursoSelecionado = TiposDeRecurso.Madeira;
    private Tile currentTile;
    public Camera mainCam;
    public LayerMask interactMask;

    [Header("Limite de Recursos")]
    public int capacidadeMaximaRecursos = 50;     
    public float recursosPorMinuto = 10f;         
    public int recursosAtuais = 50;              

    private float recursosAcumulados;            

    [Header("UI")]
    public TextMeshProUGUI recursoText; 
    public TextMeshProUGUI recursoText2;


    private void Update()
    {
      RegenerarRecursos();
      AtualizarUI();
      Clear();
      MouseUpdate();
    }

    private void MouseUpdate()
    {
        if (!Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 200f, interactMask))
            return;

        currentTile = hit.transform.GetComponent<Tile>();
        InspectTile();
    }

   private void InspectTile()
{
    if (currentTile.Occupied) return;

    if (Input.GetMouseButtonDown(0))
    {
        if (recursosAtuais > 0)
        {
            DepositarRecurso(recursoSelecionado);
            recursosAtuais--;
        }
        else
        {
            Debug.Log("Sem recursos disponíveis para depositar!");
        }
    }
}

    private void DepositarRecurso(TiposDeRecurso tipo)
    {
        switch (tipo)
        {
            case TiposDeRecurso.Madeira:
                currentTile.ReceiveResource("Arvore");
                break;
            case TiposDeRecurso.Pedra:
                currentTile.ReceiveResource("Pedra");
                break;
            case TiposDeRecurso.Comida:
                currentTile.ReceiveResource("Comida");
                break;
        }
    }

    private void RegenerarRecursos()
{
    if (recursosAtuais >= capacidadeMaximaRecursos) return;

    float recursosPorSegundo = recursosPorMinuto / 60f;

    recursosAcumulados += recursosPorSegundo * Time.deltaTime;

    if (recursosAcumulados >= 1f)
    {
        int adicionar = Mathf.FloorToInt(recursosAcumulados);
        recursosAcumulados -= adicionar;

        recursosAtuais = Mathf.Min(recursosAtuais + adicionar, capacidadeMaximaRecursos);
    }
}

private void AtualizarUI()
{
    if (recursoText == null) return;

    recursoText.text = $"Recursos: {recursosAtuais}/{capacidadeMaximaRecursos}";
    recursoText2.text = $"Produzindo {recursosPorMinuto} recursos por minuto";
}

    private void Clear()
    {
        if (currentTile == null || !currentTile.Occupied) return;

        currentTile = null;
    }

    public void SelecionarMadeira()
    {
        recursoSelecionado = TiposDeRecurso.Madeira;
    }
    public void SelecionarPedra()
    {
        recursoSelecionado = TiposDeRecurso.Pedra;
    }
    public void SelecionarComida()
    {
        recursoSelecionado = TiposDeRecurso.Comida;
    }

}
