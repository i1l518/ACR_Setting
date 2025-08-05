// StationManager.cs
using System.Collections.Generic;
using UnityEngine;
using static ACR_PhysicalController;

// �����̼� ������ ��� Ŭ������ Ȯ���Ͽ�, ���� ���� ���� Transform ����Ʈ�� �����ϵ��� �մϴ�.
[System.Serializable]
public class Station
{
    [Tooltip("Firebase�� �����̼� ID�� ��Ȯ�� ��ġ�ؾ� �մϴ�.")]
    public string id;

    [Tooltip("�����̼��� ������ ������ �ϴ� Transform �Դϴ�.")]
    public Transform transform;

    [Tooltip("�� �����̼ǿ� ���� ���� ���Ե��� Transform ����Դϴ�. ������ �߿��մϴ�.")]
    public List<Transform> slots = new List<Transform>(); // <<--- ���� ����Ʈ �߰�
}

public class StationManager : MonoBehaviour
{
    public static StationManager Instance { get; private set; }

    public ACRAssigner ACRAssigner
    {
        get => default;
        set
        {
        }
    }

    // Inspector���� ��� �����̼��� ����մϴ�.
    public List<Station> stations = new List<Station>();

    [Header("Slot Check Settings")]
    [Tooltip("���Կ� ���� �ڽ��� ������ �� ����� ���̾��Դϴ�.")]
    public LayerMask boxCheckLayer;

    [Tooltip("������ ���� ���θ� Ȯ���� ���� ������ ũ���Դϴ�.")]
    public Vector3 boxCheckSize = new Vector3(0.75f, 0.55f, 0.85f);

    // ���� ��ȸ�� ���� Station ��ü ��ü�� �����ϴ� ��ųʸ��� �����մϴ�.
    private Dictionary<string, Station> stationDictionary = new Dictionary<string, Station>();

    void Awake()
    {
        // �̱��� ����
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ����Ʈ�� �ִ� �����̼� �������� ��ųʸ��� ��ȯ�Ͽ� ���� �˻��� �غ��մϴ�.
        foreach (var station in stations)
        {
            if (!stationDictionary.ContainsKey(station.id))
            {
                // ���� Transform �Ӹ� �ƴ϶� Station ��ü ��ü�� �����մϴ�.
                stationDictionary.Add(station.id, station);
            }
            else
            {
                Debug.LogWarning($"[StationManager] �ߺ��� �����̼� ID�� �����մϴ�: {station.id}");
            }
        }
    }

    /// <summary>
    /// �����̼� ID�� �ش� �����̼��� ������(����) Transform�� ã�� ��ȯ�մϴ�.
    /// </summary>
    /// <param name="id">ã�� �����̼��� ID</param>
    /// <returns>ã�� Transform. ������ null.</returns>
    public Transform GetStationTransform(string id)
    {
        if (stationDictionary.TryGetValue(id, out Station station))
        {
            return station.transform;
        }

        Debug.LogError($"[StationManager] ID�� '{id}'�� �����̼��� ã�� �� �����ϴ�!");
        return null;
    }

    /// <summary>
    /// �����̼� ID�� ���� �ε���(��ȣ)�� �ش� ������ Transform�� ã�� ��ȯ�մϴ�.
    /// </summary>
    /// <param name="stationId">ã�� �����̼��� ID</param>
    /// <param name="slotIndex">ã�� ������ ��ȣ (0���� ����)</param>
    /// <returns>ã�� ������ Transform. ������ null.</returns>
    public Transform GetSlotTransform(string stationId, int slotIndex)
    {
        // ���� �ش� ID�� �����̼� ������ ��ųʸ����� ã���ϴ�.
        if (stationDictionary.TryGetValue(stationId, out Station station))
        {
            // �����̼��� ã�Ҵٸ�, �ش� �����̼��� ���� ����Ʈ�� ��ȿ����,
            // �׸��� ��û�� �ε����� ����Ʈ ���� ���� �ִ��� Ȯ���մϴ�.
            if (station.slots != null && slotIndex >= 0 && slotIndex < station.slots.Count)
            {
                // ��� ������ ������ �ش� �ε����� ���� Transform�� ��ȯ�մϴ�.
                return station.slots[slotIndex];
            }
            else
            {
                Debug.LogError($"[StationManager] �����̼� '{stationId}'���� �ε��� '{slotIndex}'�� �ش��ϴ� ������ ã�� �� �����ϴ�. (���� ����: {station.slots.Count})");
                return null;
            }
        }

        Debug.LogError($"[StationManager] ID�� '{stationId}'�� �����̼��� ã�� �� �����ϴ�!");
        return null;
    }

    /// <summary>
    /// Ư�� �����̼ǿ��� ����ִ� ù ��° ������ Transform�� ã�� ��ȯ�մϴ�.
    /// ���� üũ(OverlapBox)�� ����Ͽ� ���Կ� �ٸ� �ݶ��̴�(�ڽ�)�� �ִ��� Ȯ���մϴ�.
    /// </summary>
    /// <param name="stationId">ã�� �����̼��� ID</param>
    /// <returns>����ִ� ù ��° ������ Transform. �� ������ ������ null.</returns>
    public Transform FindEmptySlotTransform(string stationId) // <<< �Ķ���Ͱ� stationId�� �����ϴ�.
    {
        if (stationDictionary.TryGetValue(stationId, out Station station))
        {
            foreach (var slotTransform in station.slots)
            {
                // ���� StationManager�� ���� ���� �������� ����մϴ�.
                Collider[] colliders = Physics.OverlapBox(slotTransform.position, boxCheckSize / 2, slotTransform.rotation, boxCheckLayer);

                if (colliders.Length == 0)
                {
                    Debug.Log($"[StationManager] �����̼� '{stationId}'���� ����ִ� ���� '{slotTransform.name}'��(��) ã�ҽ��ϴ�.");
                    return slotTransform;
                }
            }

            Debug.LogError($"[StationManager] �����̼� '{stationId}'�� ����ִ� ������ �����ϴ�!");
            return null;
        }

        Debug.LogError($"[StationManager] ID�� '{stationId}'�� �����̼��� ã�� �� �����ϴ�!");
        return null;
    }

    /// <summary>
    /// Scene �信�� �������� ��, �� �����̼� ������ ���� üũ ������ �ð������� ǥ���մϴ�.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // stations ����Ʈ�� ��������� �ƹ��͵� ���� �ʽ��ϴ�.
        if (stations == null || stations.Count == 0)
        {
            return;
        }

        // ��� �����̼��� ��ȸ�մϴ�.
        foreach (var station in stations)
        {
            // �����̼ǿ� ������ ������ �ǳʶݴϴ�.
            if (station.slots == null || station.slots.Count == 0)
            {
                continue;
            }

            // �ش� �����̼��� ��� ������ ��ȸ�մϴ�.
            foreach (var slotTransform in station.slots)
            {
                // ���� Transform�� �Ҵ���� ���� ��츦 ����մϴ�.
                if (slotTransform == null)
                {
                    continue;
                }

                // ������� ������ �����մϴ�. (�������� �����)
                Gizmos.color = new Color(1, 0.92f, 0.016f, 0.5f);

                // ������� ����� ������ ��ġ�� ȸ���� �°� �����մϴ�.
                // �̷��� �ϸ� boxCheckSize�� ���� ��ǥ�� ������ �ƴ�, ������ ȸ���� ���󰩴ϴ�.
                Gizmos.matrix = Matrix4x4.TRS(slotTransform.position, slotTransform.rotation, Vector3.one);

                // ������ ����� �������� ���̾� ť�긦 �׸��ϴ�.
                // OverlapBox�� �߽����� position�̹Ƿ�, ������ (0,0,0)�� �׸��ϴ�.
                Gizmos.DrawCube(Vector3.zero, boxCheckSize);
            }
        }
    }
}
