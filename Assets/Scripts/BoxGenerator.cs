using UnityEngine;

public class BoxGenerator : MonoBehaviour
{
    [Header("생성할 박스 정보")]
    [Tooltip("복제할 박스의 원본 프리팹을 연결하세요.")]
    public GameObject boxPrefab; // 박스 프리팹

    [Header("배치 설정")]
    [Tooltip("박스를 생성할 기준 위치입니다. 이 오브젝트의 위치를 기준으로 합니다.")]
    public Transform startPoint;

    [Header("그리드 개수")]
    public int countX = 5; // 가로(X축)로 몇 개를 놓을 것인가
    public int countY = 3; // 세로(Y축)로 몇 개를 쌓을 것인가
    public int countZ = 2; // 깊이(Z축)로 몇 개를 놓을 것인가

    [Header("간격 설정")]
    public float spacingX = 1.2f; // 가로 박스 사이의 거리
    public float spacingY = 1.0f; // 세로 박스 사이의 거리
    public float spacingZ = 1.5f; // 깊이 박스 사이의 거리

    /// <summary>
    /// Inspector 창에서 우클릭하여 박스를 생성하는 함수입니다.
    /// </summary>
    [ContextMenu("Execute --- Generate Boxes")]
    private void GenerateBoxes()
    {
        if (boxPrefab == null)
        {
            Debug.LogError("Box Prefab이 연결되지 않았습니다!");
            return;
        }
        if (startPoint == null)
        {
            startPoint = this.transform; // 시작점이 없으면 자기 자신을 기준으로 함
        }

        // 중첩 for문을 사용하여 3차원 그리드를 순회합니다.
        for (int y = 0; y < countY; y++) // Y축 (층)
        {
            for (int x = 0; x < countX; x++) // X축 (가로)
            {
                for (int z = 0; z < countZ; z++) // Z축 (깊이)
                {
                    // 각 박스가 놓일 위치를 계산합니다.
                    Vector3 position = startPoint.position + new Vector3(x * spacingX, y * spacingY, z * spacingZ);

                    // 프리팹을 사용하여 박스를 실제로 생성(Instantiate)합니다.
                    // 생성된 박스는 이 BoxGenerator 오브젝트의 자식으로 들어갑니다.
                    GameObject newBox = Instantiate(boxPrefab, position, Quaternion.identity, this.transform);

                    // 박스 이름을 알아보기 쉽게 변경합니다.
                    newBox.name = $"Generated_Box_{x}_{y}_{z}";
                }
            }
        }

        Debug.Log($"{countX * countY * countZ}개의 박스 생성이 완료되었습니다!");
    }

    /// <summary>
    /// 생성했던 박스를 모두 삭제하는 유틸리티 함수입니다.
    /// </summary>
    [ContextMenu("Execute --- Clear All Generated Boxes")]
    private void ClearGeneratedBoxes()
    {
        // 자식 오브젝트의 개수가 변할 수 있으므로, 역순으로 순회하는 것이 안전합니다.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            // 즉시 삭제 (에디터에서만 사용 권장)
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        Debug.Log("생성된 모든 박스를 삭제했습니다.");
    }
}