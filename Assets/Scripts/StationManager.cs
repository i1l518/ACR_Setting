// StationManager.cs
using UnityEngine;
using System.Collections.Generic;

// 스테이션 정보를 담는 클래스를 확장하여, 여러 개의 슬롯 Transform 리스트를 포함하도록 합니다.
[System.Serializable]
public class Station
{
    [Tooltip("Firebase의 스테이션 ID와 정확히 일치해야 합니다.")]
    public string id;

    [Tooltip("스테이션의 기준점 역할을 하는 Transform 입니다.")]
    public Transform transform;

    [Tooltip("이 스테이션에 속한 개별 슬롯들의 Transform 목록입니다. 순서가 중요합니다.")]
    public List<Transform> slots = new List<Transform>(); // <<--- 슬롯 리스트 추가
}

public class StationManager : MonoBehaviour
{
    public static StationManager Instance { get; private set; }

    // Inspector에서 모든 스테이션을 등록합니다.
    public List<Station> stations = new List<Station>();

    // 빠른 조회를 위해 Station 객체 전체를 저장하는 딕셔너리로 변경합니다.
    private Dictionary<string, Station> stationDictionary = new Dictionary<string, Station>();

    void Awake()
    {
        // 싱글턴 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 리스트에 있는 스테이션 정보들을 딕셔너리로 변환하여 빠른 검색을 준비합니다.
        foreach (var station in stations)
        {
            if (!stationDictionary.ContainsKey(station.id))
            {
                // 이제 Transform 뿐만 아니라 Station 객체 전체를 저장합니다.
                stationDictionary.Add(station.id, station);
            }
            else
            {
                Debug.LogWarning($"[StationManager] 중복된 스테이션 ID가 존재합니다: {station.id}");
            }
        }
    }

    /// <summary>
    /// 스테이션 ID로 해당 스테이션의 기준점(메인) Transform을 찾아 반환합니다.
    /// </summary>
    /// <param name="id">찾을 스테이션의 ID</param>
    /// <returns>찾은 Transform. 없으면 null.</returns>
    public Transform GetStationTransform(string id)
    {
        if (stationDictionary.TryGetValue(id, out Station station))
        {
            return station.transform;
        }

        Debug.LogError($"[StationManager] ID가 '{id}'인 스테이션을 찾을 수 없습니다!");
        return null;
    }

    /// <summary>
    /// 스테이션 ID와 슬롯 인덱스(번호)로 해당 슬롯의 Transform을 찾아 반환합니다.
    /// </summary>
    /// <param name="stationId">찾을 스테이션의 ID</param>
    /// <param name="slotIndex">찾을 슬롯의 번호 (0부터 시작)</param>
    /// <returns>찾은 슬롯의 Transform. 없으면 null.</returns>
    public Transform GetSlotTransform(string stationId, int slotIndex)
    {
        // 먼저 해당 ID의 스테이션 정보를 딕셔너리에서 찾습니다.
        if (stationDictionary.TryGetValue(stationId, out Station station))
        {
            // 스테이션을 찾았다면, 해당 스테이션의 슬롯 리스트가 유효한지,
            // 그리고 요청한 인덱스가 리스트 범위 내에 있는지 확인합니다.
            if (station.slots != null && slotIndex >= 0 && slotIndex < station.slots.Count)
            {
                // 모든 조건이 맞으면 해당 인덱스의 슬롯 Transform을 반환합니다.
                return station.slots[slotIndex];
            }
            else
            {
                Debug.LogError($"[StationManager] 스테이션 '{stationId}'에서 인덱스 '{slotIndex}'에 해당하는 슬롯을 찾을 수 없습니다. (슬롯 개수: {station.slots.Count})");
                return null;
            }
        }

        Debug.LogError($"[StationManager] ID가 '{stationId}'인 스테이션을 찾을 수 없습니다!");
        return null;
    }
}