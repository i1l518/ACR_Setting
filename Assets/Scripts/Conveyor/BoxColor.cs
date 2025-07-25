using UnityEngine;

public class BoxColor : MonoBehaviour
{
    public Material[] materials;  // 유니티에서 Inspector에 머티리얼 배열 할당
    private Renderer boxRenderer;

    void Start()
    {
        boxRenderer = GetComponent<Renderer>();
        if (materials.Length > 0 && boxRenderer != null)
        {
            int index = Random.Range(0, materials.Length);
            boxRenderer.material = materials[index];
        }
    }
}
