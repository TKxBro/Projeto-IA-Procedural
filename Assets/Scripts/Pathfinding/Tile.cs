using UnityEngine;
using TMPro;
using System.Collections.Generic;
using JetBrains.Annotations;

public class Tile : MonoBehaviour
{
    #region member fields
    public Tile parent;
    public Tile connectedTile;
    public Character occupyingCharacter;
    public GameObject occupyingResource;

    public float costFromOrigin = 0;
    public float costToDestination = 0;
    public int terrainCost = 0;
    public float TotalCost { get { return costFromOrigin + costToDestination + terrainCost; } }
    public bool Occupied { get; set; } = false;

    [Header("Configs Resources")]
    public GameObject prefabArvore;
    public GameObject prefabPedra;
    public GameObject prefabComida;

    [SerializeField] TMP_Text costText;
    #endregion

   Dictionary<int, Color> costMap = new Dictionary<int, Color>()
{
    {0, new Color(0f, 0.3921569f, 0f, 1f) }, // green
    {1, new Color(0.3333333f, 0.4196079f, 0.1843137f, 1f) }, // weird green
    {2, new Color(0.5450981f, 0.2705882f, 0.07450981f, 1f) }, // brown
    {3, new Color(0f, 0f, 1f, 1f) } // blue 
};

    private void Start()
    {
        ApplyTerrainVisual();
    }

    public void Init(int initialTerrainCost)
    {
        terrainCost = Mathf.Clamp(initialTerrainCost, 0, costMap.Count - 1);
        ApplyTerrainVisual();
    }

    public void Highlight()
    {
        SetColor(Color.white);
    }

    public void ClearHighlight()
    {
        ApplyTerrainVisual();
    }

    public void ModifyCost()
    {
        terrainCost++;
        if (terrainCost > costMap.Count - 1)
            terrainCost = 0;

        ApplyTerrainVisual();
    }

    [ContextMenu("Instanciar arvore nesse tile")]
    public void ReceiveResource([CanBeNull] string resourceTag = null)
    {
        if (Occupied) return;
        GameObject resourceObject;

        switch (resourceTag)
        {
            case "Arvore":
                resourceObject = Instantiate(prefabArvore, transform.position, prefabArvore.transform.rotation);
                break;
            case "Pedra":
                resourceObject = Instantiate(prefabPedra, transform.position, prefabPedra.transform.rotation);
                break;
            case "Comida":
                resourceObject = Instantiate(prefabComida, transform.position, prefabComida.transform.rotation);
                break;
            default:
                Debug.LogError("Tag de recurso inv�lida ou nula!");
                return;
        }

        resourceObject.transform.SetParent(this.transform);    
        if(resourceTag != null)
        {
            gameObject.tag = resourceTag;
        }
        occupyingResource = resourceObject;
        Occupied = true;
    }

    [ContextMenu("Remover arvore desse tile")]
    public void RemoveResource()
    {
        if (!Occupied || occupyingResource == null) return;

        Destroy(occupyingResource.gameObject);
        gameObject.tag = "Untagged";
        occupyingResource = null;
        Occupied = false;
    }

    public void ReceiveConstruction(string constructionTag)
    {
        if(Occupied) return;
        gameObject.tag = constructionTag;
        Occupied = true;
    }

    void ApplyTerrainVisual()
    {
        if (costMap.TryGetValue(terrainCost, out Color col))
            SetColor(col);
        else
            Debug.LogWarning("Invalid terrainCost or mapping: " + terrainCost);
    }

    private void SetColor(Color color)
    {
        GetComponent<MeshRenderer>().material.color = color;
    }

    public void DebugCostText()
    {
        costText.text = TotalCost.ToString("F1");
    }

    public void ClearText()
    {
        costText.text = "";
    }
}
