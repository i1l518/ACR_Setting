using UnityEngine;

public class BoxColor : MonoBehaviour
{
    public Material[] materials;  // ����Ƽ���� Inspector�� ��Ƽ���� �迭 �Ҵ�
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
