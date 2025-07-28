// 파일명: ACR_PhysicalController.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ACR_PhysicalController : MonoBehaviour
{
    [System.Serializable] // 인스펙터에서 보려면 추가
    public class StorageSlot
    {
        public int SlotId { get; private set; }
        public Transform SlotTransform { get; private set; }
        public GameObject StoredBoxObject { get; private set; }
        public string StoredBoxId { get; private set; } // 박스의 고유 ID (가장 중요!)

        public bool IsEmpty => StoredBoxObject == null;

        public StorageSlot(int id, Transform transform)
        {
            this.SlotId = id;
            this.SlotTransform = transform;
        }

        public void StoreBox(GameObject boxObject, string boxId)
        {
            StoredBoxObject = boxObject;
            StoredBoxId = boxId;
            // 물리적 부모-자식 관계 설정
            boxObject.transform.SetParent(SlotTransform, true);
            boxObject.transform.localPosition = Vector3.zero;
            boxObject.transform.localRotation = Quaternion.identity;
        }

        public GameObject ReleaseBox()
        {
            GameObject boxToRelease = StoredBoxObject;
            StoredBoxObject = null;
            StoredBoxId = null;
            return boxToRelease;
        }
    }

    [Header("ACR 고유 ID")]
    public string acrId; // Inspector에서 각 ACR마다 고유 ID를 반드시 설정해줘야 합니다. (예: acr_01, acr_02)

    [Header("연결 컴포넌트")]
    public GripperController gripperController;
    public GrabController grabController;

    // ★★★ 1. 새로운 변수 추가 ★★★
    [Header("보관소 설정")]
    [Tooltip("ACR 내부의 보관소 위치들. 아래쪽 슬롯부터 순서대로 할당하세요.")]
    public List<Transform> storageSlotTransforms; // Inspector에서 슬롯 Transform들을 연결

    private StorageSlot[] internalStorage;

    [Tooltip("각 슬롯이 비어있는지 확인할 때 사용할 감지 상자의 크기")]
    public Vector3 checkBoxSize = new Vector3(0.75f, 0.55f, 0.85f);

    [Tooltip("박스(Box) 오브젝트들이 속한 레이어")]
    public LayerMask boxLayer;

    void Awake()
    {
        InitializeStorage();
    }

    /// <summary>
    /// 인스펙터에서 할당한 Transform들을 기반으로 내부 보관소 시스템을 초기화합니다.
    /// </summary>
    private void InitializeStorage()
    {
        internalStorage = new StorageSlot[storageSlotTransforms.Count];
        for (int i = 0; i < storageSlotTransforms.Count; i++)
        {
            internalStorage[i] = new StorageSlot(i, storageSlotTransforms[i]);
        }
        Debug.Log($"[{acrId}] {internalStorage.Length}개의 내부 보관소 초기화 완료.");
    }

    /// <summary>
    /// 비어있는 논리적 보관소 슬롯을 찾습니다. (더 이상 Physics를 사용하지 않음)
    /// </summary>
    private StorageSlot FindEmptyStorageSlot(float preferredWorldY)
    {
        // Linq를 사용해 간단하게 표현
        var emptySlots = internalStorage.Where(slot => slot.IsEmpty).ToList();

        if (emptySlots.Count == 0)
        {
            Debug.LogError($"[{acrId}] 모든 보관소가 꽉 찼습니다!");
            return null;
        }

        // 비어있는 슬롯 중 Y좌표가 가장 가까운 슬롯을 찾음
        StorageSlot closestEmptySlot = emptySlots.OrderBy(slot => Mathf.Abs(slot.SlotTransform.position.y - preferredWorldY)).First();

        Debug.Log($"[{acrId}] 비어있는 슬롯 중 가장 가까운 슬롯 #{closestEmptySlot.SlotId}을(를) 선택합니다.");
        return closestEmptySlot;
    }

    /// <summary>
    /// 특정 ID를 가진 박스가 저장된 슬롯을 찾습니다.
    /// </summary>
    private StorageSlot FindSlotContainingBox(string boxId)
    {
        foreach (var slot in internalStorage)
        {
            if (!slot.IsEmpty && slot.StoredBoxId == boxId)
            {
                return slot;
            }
        }
        Debug.LogError($"[{acrId}] ID '{boxId}'를 가진 박스를 찾을 수 없습니다!");
        return null;
    }

    public IEnumerator PickupSequence(Dictionary<string, object> stopData)
    {
        Debug.Log($"--- [{this.acrId}] 물품 회수(Pickup) 시퀀스 시작 ---");

        // 1단계: 리프트 상승
        float targetLocalHeight = 0f;
        float pickupWorldHeight = 0f; // ★ 박스를 집은 월드 높이를 저장할 변수
        try
        {
            var locationMap = stopData["source"] as Dictionary<string, object>;
            var posMap = locationMap["position"] as Dictionary<string, object>;
            pickupWorldHeight = Convert.ToSingle(posMap["y"]);
            targetLocalHeight = pickupWorldHeight - transform.position.y;
        }
        catch (Exception e) { Debug.LogError($"목표 높이 파싱 실패: {e.Message}"); }
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLocalHeight));
        Debug.Log($"[{this.acrId}] 1단계 (리프트 상승) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 2단계: 턴테이블 회전 (랙 방향)
        yield return StartCoroutine(gripperController.RotateTurntableSequence(90f));
        Debug.Log($"[{this.acrId}] 2단계 (턴테이블 회전) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 3단계: Gripper 전진 (랙으로)
        float slideDistanceToRack = 1.0f;
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToRack));
        Debug.Log($"[{this.acrId}] 3단계 (슬라이더 전진) 완료!");

        // 4단계: 파지
        grabController.Grab();
        Debug.Log($"[{this.acrId}] 4단계 (파지) 완료!");
        yield return new WaitForSeconds(1.0f);

        // 5단계: Gripper 후진 (박스를 가지고)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToRack));
        Debug.Log($"[{this.acrId}] 5단계 (슬라이더 후진) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 6단계: 턴테이블 원위치 (정면 방향)
        yield return StartCoroutine(gripperController.RotateTurntableSequence(0f));
        Debug.Log($"[{this.acrId}] 6단계 (턴테이블 원위치) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 7단계: 비어있는 보관소 슬롯 찾기 및 위치로 이동
        // ★★★ 박스를 집어 올린 월드 높이(pickupWorldHeight)를 인자로 전달 ★★★
        StorageSlot targetSlot = FindEmptyStorageSlot(pickupWorldHeight);

        // 만약 빈 슬롯을 찾지 못했다면 시퀀스를 중단하고 에러 처리
        if (targetSlot == null)
        {
            Debug.LogError($"[{acrId}] 적재 실패: 빈 슬롯이 없어 Pickup 시퀀스를 중단합니다.");
            yield break;
        }

        float targetLiftHeight = targetSlot.SlotTransform.position.y - transform.position.y;
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLiftHeight));
        Debug.Log($"[{this.acrId}] 7단계 (슬롯 높이로 리프트 이동) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 8단계: 슬롯을 향해 Gripper 전진
        Vector3 localPosInSlider = gripperController.gripperSlider.InverseTransformPoint(targetSlot.SlotTransform.position);
        float slideDistanceToStorage = -localPosInSlider.z;
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToStorage));
        Debug.Log($"[{acrId}] 8단계 (슬롯으로 슬라이더 전진) 완료!");

        // 9단계: 적재 (박스 놓기)
        GameObject releasedBox = grabController.Release();

        // 박스가 성공적으로 놓아졌는지 확인합니다.
        if (releasedBox != null)
        {
            // (1) 박스의 고유 ID 가져오기
            //    - 아래 코드는 박스 오브젝트에 BoxData.cs 스크립트가 있고,
            //      그 안에 public string Id { get; } 프로퍼티가 있다고 가정한 예시입니다.
            //    - 실제 프로젝트의 박스 ID 정책에 맞게 수정해야 합니다.
            //    - 만약 BoxData 스크립트가 없다면, 임시로 releasedBox.name 등을 사용할 수 있습니다.

            string boxId = "unknown_id"; // 기본값
            BoxData boxData = releasedBox.GetComponent<BoxData>();
            if (boxData != null)
            {
                boxId = boxData.Id;
            }
            else
            {
                Debug.LogWarning($"[{acrId}] 박스에 BoxData 컴포넌트가 없어 ID를 찾을 수 없습니다. 오브젝트 이름을 ID로 사용합니다.");
                boxId = releasedBox.name; // 임시방편
            }

            // (2) 논리적 보관소(StorageSlot)에 박스 정보 저장
            targetSlot.StoreBox(releasedBox, boxId);

            Debug.Log($"[{acrId}] 9단계 (적재) 완료! 슬롯 #{targetSlot.SlotId}에 박스 '{boxId}'를 저장했습니다.");
        }
        else
        {
            // 이 경고가 뜬다면, Gripper가 박스를 놓으려고 했으나 실제로 잡고 있는 것이 없었다는 의미입니다.
            Debug.LogWarning($"[{acrId}] 9단계 (적재) 실패: Release()가 호출되었지만 반환된 박스가 없습니다. 파지 단계에 문제가 있었을 수 있습니다.");
        }

        yield return new WaitForSeconds(0.5f); // 다음 단계를 위한 대기

        // 10단계: Gripper 후진 (원위치로)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToStorage));
        Debug.Log($"[{this.acrId}] 10단계 (내부 적재 공간에서 후진) 완료!");

        Debug.Log($"--- [{this.acrId}] 모든 물리 작업 완료! 중앙 관제소에 보고합니다. ---");
        //// '나의 작업이 끝났다'고 ID를 명시하여 관제소에 보고
        //ACREvents.RaiseActionCompleted(this.acrId);
    }

    public IEnumerator DropoffSequence(Dictionary<string, object> stopData)
    {
        Debug.Log($"--- [{this.acrId}] 전체 물품 하역(Dropoff) 시퀀스 시작 ---");

        // 1. 목적지(outbound_station) 정보 파싱
        Transform outboundStation = null;
        try
        {
            // 이 부분은 실제 `stopData` 구조에 맞게 수정해야 합니다.
            // 여기서는 예시로 "outbound_station_01" 같은 이름의 오브젝트를 찾는다고 가정합니다.
            string stationName = stopData["destination_name"] as string;
            outboundStation = GameObject.Find(stationName)?.transform;

            if (outboundStation == null)
            {
                Debug.LogError($"목적지 스테이션 '{stationName}'을 찾을 수 없습니다! Dropoff 시퀀스를 중단합니다.");
                yield break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"목적지 정보 파싱 실패: {e.Message}");
            yield break;
        }

        // 2. 보관소에 있는 모든 박스를 순회하며 하역 작업 반복
        //    Linq를 사용해 박스가 있는 슬롯만 필터링합니다.
        var filledSlots = internalStorage.Where(slot => !slot.IsEmpty).ToList();

        if (filledSlots.Count == 0)
        {
            Debug.LogWarning($"[{acrId}] 보관소에 하역할 박스가 없습니다. 시퀀스를 종료합니다.");
            yield break;
        }

        Debug.Log($"[{acrId}] 총 {filledSlots.Count}개의 박스를 하역합니다.");

        foreach (StorageSlot sourceSlot in filledSlots)
        {
            Debug.Log($"--- 슬롯 #{sourceSlot.SlotId}의 박스 '{sourceSlot.StoredBoxId}' 하역 시작 ---");

            // === 서브 시퀀스: 박스 꺼내기 (Pickup의 역순) ===

            // A. 박스가 있는 슬롯 높이로 리프트 이동
            float liftHeightToGrab = sourceSlot.SlotTransform.position.y - transform.position.y;
            yield return StartCoroutine(gripperController.MoveLiftSequence(liftHeightToGrab));
            yield return new WaitForSeconds(0.5f);

            // B. 슬롯을 향해 Gripper 전진
            Vector3 localPosToSlot = gripperController.gripperSlider.InverseTransformPoint(sourceSlot.SlotTransform.position);
            float slideDistToGrab = localPosToSlot.z;
            yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistToGrab));

            // C. 박스 파지
            grabController.Grab(); // 이 시점에는 슬롯에 있는 박스만 감지되어야 함
            yield return new WaitForSeconds(1.0f);

            // D. 논리적 상태 업데이트: 보관소에서 박스 제거
            GameObject boxToMove = sourceSlot.ReleaseBox();
            if (boxToMove == null)
            {
                Debug.LogError($"심각한 오류: 슬롯 #{sourceSlot.SlotId}이 비어있지 않다고 판단했으나, ReleaseBox()가 null을 반환했습니다. 시퀀스를 중단합니다.");
                yield break;
            }
            Debug.Log($"[{acrId}] 슬롯 #{sourceSlot.SlotId}에서 박스를 성공적으로 꺼냈습니다.");


            // E. Gripper 후진
            yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistToGrab));
            yield return new WaitForSeconds(0.5f);

            // === 서브 시퀀스: 박스 내려놓기 (Pickup과 유사) ===

            // F. 목적지 스테이션 높이로 리프트 이동
            float liftHeightToDrop = outboundStation.position.y - transform.position.y;
            yield return StartCoroutine(gripperController.MoveLiftSequence(liftHeightToDrop));
            yield return new WaitForSeconds(0.5f);

            // G. 목적지 스테이션 방향으로 턴테이블 회전 (예: -90도 또는 270도)
            //    이 각도는 스테이션의 위치에 따라 달라져야 합니다. 
            //    여기서는 임시로 -90도로 가정합니다.
            yield return StartCoroutine(gripperController.RotateTurntableSequence(-90f));
            yield return new WaitForSeconds(0.5f);

            // H. 목적지를 향해 Gripper 전진
            Vector3 localPosToStation = gripperController.gripperSlider.InverseTransformPoint(outboundStation.position);
            float slideDistToDrop = localPosToStation.z;
            yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistToDrop));

            // I. 박스 놓기
            grabController.Release();
            Debug.Log($"[{acrId}] 목적지 스테이션에 박스 '{boxToMove.name}'을(를) 내려놓았습니다.");
            yield return new WaitForSeconds(0.5f);

            // J. Gripper 후진
            yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistToDrop));
            yield return new WaitForSeconds(0.5f);

            // K. 턴테이블 원위치 (다음 작업을 위해)
            yield return StartCoroutine(gripperController.RotateTurntableSequence(0f));
        }

        Debug.Log($"--- [{this.acrId}] 모든 물품 하역(Dropoff) 완료! ---");
        //ACREvents.RaiseActionCompleted(this.acrId); // 관제소에 작업 완료 보고
    }

    //감지영역 시각화
    private void OnDrawGizmos()
    {
        // Application.isPlaying을 통해 현재 게임이 실행 중인지 확인합니다.
        // 실행 중이고, internalStorage가 성공적으로 초기화되었다면...
        if (Application.isPlaying && internalStorage != null)
        {
            // 런타임 데이터를 사용하여 각 슬롯의 상태를 시각화합니다.
            foreach (StorageSlot slot in internalStorage)
            {
                // 슬롯의 Transform이 유효한지 확인합니다.
                if (slot.SlotTransform != null)
                {
                    // ★★★ 슬롯이 비어있으면(IsEmpty) 녹색, 차있으면 빨간색으로 그립니다. ★★★
                    Gizmos.color = slot.IsEmpty ? Color.green : Color.red;
                    Gizmos.DrawWireCube(slot.SlotTransform.position, checkBoxSize);
                }
            }
        }
        // 게임이 실행 중이 아닐 때 (Edit 모드일 때)
        else
        {
            // 인스펙터에 할당된 Transform 정보를 사용해 노란색으로 그립니다.
            if (storageSlotTransforms == null) return;

            Gizmos.color = Color.yellow;
            foreach (Transform slotTransform in storageSlotTransforms)
            {
                if (slotTransform != null)
                {
                    Gizmos.DrawWireCube(slotTransform.position, checkBoxSize);
                }
            }
        }
    }
}