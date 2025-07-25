using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    public GameObject boxPrefab;
    public Vector3 startPosition = new Vector3(42f, 0.15f, 23f);
    public float spacing = 0.35f;
    public Vector3 boxScale = new Vector3(0.3f, 0.3f, 0.3f);

    void Start()
    {
        SpawnBoxes();
    }

    void SpawnBoxes()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    Vector3 pos = startPosition + new Vector3(x * spacing, y * spacing, z * spacing);
                    GameObject box = Instantiate(boxPrefab, pos, Quaternion.identity);
                    box.transform.localScale = boxScale;
                    box.transform.parent = this.transform;  // BoxSpawner ¹Ø¿¡ Á¤·Ä
                    ApplyRandomColor(box);
                }
            }
        }
    }

    void ApplyRandomColor(GameObject box)
    {
        Color[] colors = new Color[]
        {
            Color.green,
            Color.blue,
            Color.red,
            Color.yellow
        };

        int randomIndex = Random.Range(0, colors.Length);
        Color randomColor = colors[randomIndex];

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = randomColor;
        }
    }
}