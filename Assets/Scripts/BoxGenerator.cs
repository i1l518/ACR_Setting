using UnityEngine;

public class BoxGenerator : MonoBehaviour
{
    [Header("������ �ڽ� ����")]
    [Tooltip("������ �ڽ��� ���� �������� �����ϼ���.")]
    public GameObject boxPrefab; // �ڽ� ������

    [Header("��ġ ����")]
    [Tooltip("�ڽ��� ������ ���� ��ġ�Դϴ�. �� ������Ʈ�� ��ġ�� �������� �մϴ�.")]
    public Transform startPoint;

    [Header("�׸��� ����")]
    public int countX = 5; // ����(X��)�� �� ���� ���� ���ΰ�
    public int countY = 3; // ����(Y��)�� �� ���� ���� ���ΰ�
    public int countZ = 2; // ����(Z��)�� �� ���� ���� ���ΰ�

    [Header("���� ����")]
    public float spacingX = 1.2f; // ���� �ڽ� ������ �Ÿ�
    public float spacingY = 1.0f; // ���� �ڽ� ������ �Ÿ�
    public float spacingZ = 1.5f; // ���� �ڽ� ������ �Ÿ�

    /// <summary>
    /// Inspector â���� ��Ŭ���Ͽ� �ڽ��� �����ϴ� �Լ��Դϴ�.
    /// </summary>
    [ContextMenu("Execute --- Generate Boxes")]
    private void GenerateBoxes()
    {
        if (boxPrefab == null)
        {
            Debug.LogError("Box Prefab�� ������� �ʾҽ��ϴ�!");
            return;
        }
        if (startPoint == null)
        {
            startPoint = this.transform; // �������� ������ �ڱ� �ڽ��� �������� ��
        }

        // ��ø for���� ����Ͽ� 3���� �׸��带 ��ȸ�մϴ�.
        for (int y = 0; y < countY; y++) // Y�� (��)
        {
            for (int x = 0; x < countX; x++) // X�� (����)
            {
                for (int z = 0; z < countZ; z++) // Z�� (����)
                {
                    // �� �ڽ��� ���� ��ġ�� ����մϴ�.
                    Vector3 position = startPoint.position + new Vector3(x * spacingX, y * spacingY, z * spacingZ);

                    // �������� ����Ͽ� �ڽ��� ������ ����(Instantiate)�մϴ�.
                    // ������ �ڽ��� �� BoxGenerator ������Ʈ�� �ڽ����� ���ϴ�.
                    GameObject newBox = Instantiate(boxPrefab, position, Quaternion.identity, this.transform);

                    // �ڽ� �̸��� �˾ƺ��� ���� �����մϴ�.
                    newBox.name = $"Generated_Box_{x}_{y}_{z}";
                }
            }
        }

        Debug.Log($"{countX * countY * countZ}���� �ڽ� ������ �Ϸ�Ǿ����ϴ�!");
    }

    /// <summary>
    /// �����ߴ� �ڽ��� ��� �����ϴ� ��ƿ��Ƽ �Լ��Դϴ�.
    /// </summary>
    [ContextMenu("Execute --- Clear All Generated Boxes")]
    private void ClearGeneratedBoxes()
    {
        // �ڽ� ������Ʈ�� ������ ���� �� �����Ƿ�, �������� ��ȸ�ϴ� ���� �����մϴ�.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            // ��� ���� (�����Ϳ����� ��� ����)
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        Debug.Log("������ ��� �ڽ��� �����߽��ϴ�.");
    }
}