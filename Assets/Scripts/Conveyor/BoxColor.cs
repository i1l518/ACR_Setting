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

            // ★★★ 이 코드가 핵심입니다! ★★★
            // 선택된 재질의 이름을 BoxInfo에 저장해줍니다.
            boxInfo.boxColorName = selectedMaterial.name.ToLower();

            // 제대로 저장되었는지 콘솔 창에서 확인해봅시다.
            Debug.Log($"박스 생성: {this.name}, 색상: {boxInfo.boxColorName}");
        }
    }
}