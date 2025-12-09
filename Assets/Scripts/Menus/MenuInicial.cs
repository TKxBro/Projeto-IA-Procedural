using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicial : MonoBehaviour
{
    public GameObject FundoMenu;
    public GameObject FundoOpcoes;
    
    public void IniciarJogo()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void FecharJogo()
    {
        Application.Quit();
        Debug.Log("Voc� fechou o jogo");
    }

    public void Opcoes()
    {
        FundoMenu.SetActive(false);
        FundoOpcoes.SetActive(true);
    }

    public void Voltar()
    {
        FundoMenu.SetActive(true);
        FundoOpcoes.SetActive(false);
    }
}
