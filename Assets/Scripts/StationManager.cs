// StationManager.cs
using UnityEngine;
using System.Collections.Generic;

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

    // Inspector���� ��� �����̼��� ����մϴ�.
    public List<Station> stations = new List<Station>();

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
}