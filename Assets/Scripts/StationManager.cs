// StationManager.cs
using System.Collections.Generic;
using UnityEngine;
using static ACR_PhysicalController;

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

    public ACRAssigner ACRAssigner
    {
        get => default;
        set
        {
        }
    }

    // Inspector에서 모든 스테이션을 등록합니다.
    public List<Station> stations = new List<Station>();

    [Header("Slot Check Settings")]
    [Tooltip("슬롯에 놓인 박스를 감지할 때 사용할 레이어입니다.")]
    public LayerMask boxCheckLayer;

    [Tooltip("슬롯의 점유 여부를 확인할 감지 영역의 크기입니다.")]
    public Vector3 boxCheckSize = new Vector3(0.75f, 0.55f, 0.85f);

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

    /// <summary>
    /// 특정 스테이션에서 비어있는 첫 번째 슬롯의 Transform을 찾아 반환합니다.
    /// 물리 체크(OverlapBox)를 사용하여 슬롯에 다른 콜라이더(박스)가 있는지 확인합니다.
    /// </summary>
    /// <param name="stationId">찾을 스테이션의 ID</param>
    /// <returns>비어있는 첫 번째 슬롯의 Transform. 빈 슬롯이 없으면 null.</returns>
    public Transform FindEmptySlotTransform(string stationId) // <<< 파라미터가 stationId만 남습니다.
    {
        if (stationDictionary.TryGetValue(stationId, out Station station))
        {
            foreach (var slotTransform in station.slots)
            {
                // 이제 StationManager가 직접 가진 변수들을 사용합니다.
                Collider[] colliders = Physics.OverlapBox(slotTransform.position, boxCheckSize / 2, slotTransform.rotation, boxCheckLayer);

                if (colliders.Length == 0)
                {
                    Debug.Log($"[StationManager] 스테이션 '{stationId}'에서 비어있는 슬롯 '{slotTransform.name}'을(를) 찾았습니다.");
                    return slotTransform;
                }
            }

            Debug.LogError($"[StationManager] 스테이션 '{stationId}'에 비어있는 슬롯이 없습니다!");
            return null;
        }

        Debug.LogError($"[StationManager] ID가 '{stationId}'인 스테이션을 찾을 수 없습니다!");
        return null;
    }

    /// <summary>
    /// Scene 뷰에서 선택했을 때, 각 스테이션 슬롯의 물리 체크 영역을 시각적으로 표시합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // stations 리스트가 비어있으면 아무것도 하지 않습니다.
        if (stations == null || stations.Count == 0)
        {
            return;
        }

        // 모든 스테이션을 순회합니다.
        foreach (var station in stations)
        {
            // 스테이션에 슬롯이 없으면 건너뜁니다.
            if (station.slots == null || station.slots.Count == 0)
            {
                continue;
            }

            // 해당 스테이션의 모든 슬롯을 순회합니다.
            foreach (var slotTransform in station.slots)
            {
                // 슬롯 Transform이 할당되지 않은 경우를 대비합니다.
                if (slotTransform == null)
                {
                    continue;
                }

                // 기즈모의 색상을 설정합니다. (반투명한 노란색)
                Gizmos.color = new Color(1, 0.92f, 0.016f, 0.5f);

                // 기즈모의 행렬을 슬롯의 위치와 회전에 맞게 설정합니다.
                // 이렇게 하면 boxCheckSize가 로컬 좌표계 기준이 아닌, 슬롯의 회전을 따라갑니다.
                Gizmos.matrix = Matrix4x4.TRS(slotTransform.position, slotTransform.rotation, Vector3.one);

                // 설정된 행렬을 기준으로 와이어 큐브를 그립니다.
                // OverlapBox의 중심점은 position이므로, 기즈모는 (0,0,0)에 그립니다.
                Gizmos.DrawCube(Vector3.zero, boxCheckSize);
            }
        }
    }
}
