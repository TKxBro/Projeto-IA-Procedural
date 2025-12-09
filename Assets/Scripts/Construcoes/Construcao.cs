using UnityEngine;

public class Construcao : MonoBehaviour
{
    [SerializeField] private string nomeConstrucao;
    void Start()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        Tile tile = collision.gameObject.GetComponent<Tile>();
        if (tile != null)
        {
            tile.ReceiveConstruction(nomeConstrucao);
        }
    }
}
