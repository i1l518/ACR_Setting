using UnityEngine;

[RequireComponent(typeof(BoxInfo))]
[RequireComponent(typeof(Renderer))]
public class BoxColor : MonoBehaviour
{
    public Material[] materials;
    private Renderer boxRenderer;
    private BoxInfo boxInfo;

    void Awake()
    {
        boxRenderer = GetComponent<Renderer>();
        boxInfo = GetComponent<BoxInfo>();
    }

    void Start()
    {
        if (materials != null && materials.Length > 0)
        {
            Material selectedMaterial = materials[Random.Range(0, materials.Length)];
            boxRenderer.material = selectedMaterial;

            // �ڡڡ� �� �ڵ尡 �ٽ��Դϴ�! �ڡڡ�
            // ���õ� ������ �̸��� BoxInfo�� �������ݴϴ�.
            boxInfo.boxColorName = selectedMaterial.name.ToLower();

            // ����� ����Ǿ����� �ܼ� â���� Ȯ���غ��ô�.
            Debug.Log($"�ڽ� ����: {this.name}, ����: {boxInfo.boxColorName}");
        }
    }
}