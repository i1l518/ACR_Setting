// 파일명: ACR_PhysicalController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/*
// Pickup 작업을 정의하는 클래스 (IPhysicalAction 인터페이스를 구현)
public class PickupAction : IPhysicalAction
{
    private ACR_PhysicalController physicalController;

    public PickupAction(ACR_PhysicalController controller)
    {
        this.physicalController = controller;
    }

    public IEnumerator Execute(Dictionary<string, object> stopData)
    {
        // 기존 PickupSequence 코드를 그대로 여기에 붙여넣습니다.
        // 단, 마지막 줄은 수정합니다.
        Debug.Log($"--- [{physicalController.acrId}] 물품 회수(Pickup) 시퀀스 시작 ---");
        // ... (1단계부터 9단계까지의 모든 로직) ...
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"--- [{physicalController.acrId}] 모든 물리 작업 완료! ---");
        // 이 코루틴이 끝나면 자동으로 제어권이 ACRController에게 돌아갑니다.
    }
}*/

public class ACR_PhysicalController : MonoBehaviour
{
    [Header("ACR 고유 ID")]
    public string acrId; // Inspector에서 각 ACR마다 고유 ID를 반드시 설정해줘야 합니다. (예: acr_01, acr_02)

    [Header("연결 컴포넌트")]
    public GripperController gripperController;
    public GrabController grabController;

    // ★★★ 1. 새로운 변수 추가 ★★★
    [Header("보관소 설정")]
    [Tooltip("ACR 내부의 보관소 위치들. 아래쪽 슬롯부터 순서대로 할당하세요.")]
    public List<Transform> storageSlots; // Inspector에서 슬롯 Transform들을 연결

    [Tooltip("각 슬롯이 비어있는지 확인할 때 사용할 감지 상자의 크기")]
    public Vector3 checkBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

    [Tooltip("박스(Box) 오브젝트들이 속한 레이어")]
    public LayerMask boxLayer;

    // ★★★ 2. 신규 함수 작성: FindEmptyStorageSlot ★★★
    /// <summary>
    /// 비어있는 보관소 슬롯을 찾습니다.
    /// </summary>
    /// <param name="preferredSlotIndex">우선적으로 확인하고 싶은 슬롯의 인덱스</param>
    /// <returns>비어있는 슬롯의 Transform. 모두 찼으면 null을 반환합니다.</returns>
    private Transform FindEmptyStorageSlot(float preferredWorldY)
    {
        // 1. 가장 가까운 슬롯 찾기
        Transform closestSlot = null;
        float minDistance = float.MaxValue;

        // 모든 슬롯을 순회하며 preferredWorldY와 Y좌표 차이가 가장 작은 슬롯을 찾습니다.
        foreach (Transform slot in storageSlots)
        {
            float distance = Mathf.Abs(slot.position.y - preferredWorldY);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestSlot = slot;
            }
        }

        // 2. 가장 가까운 슬롯(우선순위 슬롯)이 비어있는지 확인
        if (closestSlot != null)
        {
            Collider[] colliders = new Collider[1];
            int count = Physics.OverlapBoxNonAlloc(closestSlot.position, checkBoxSize / 2, colliders, closestSlot.rotation, boxLayer);

            if (count == 0) // 비어있다면
            {
                Debug.Log($"[{acrId}] 박스를 꺼낸 높이와 가장 가까운 슬롯(Y={closestSlot.position.y})이 비어있어 선택합니다.");
                return closestSlot;
            }
        }

        // 3. 우선순위 슬롯이 차 있다면, 0번부터 순서대로 빈 슬롯을 다시 찾습니다.
        Debug.Log($"[{acrId}] 우선순위 슬롯이 차 있어서, 가장 아래부터 빈 슬롯을 다시 검색합니다.");
        for (int i = 0; i < storageSlots.Count; i++)
        {
            Transform currentSlot = storageSlots[i];
            Collider[] colliders = new Collider[1];
            int count = Physics.OverlapBoxNonAlloc(currentSlot.position, checkBoxSize / 2, colliders, currentSlot.rotation, boxLayer);

            if (count == 0) // 처음으로 발견된 빈 슬롯
            {
                Debug.Log($"[{acrId}] 비어있는 다음 슬롯 #{i}을(를) 찾아 선택합니다.");
                return currentSlot;
            }
        }

        // 4. 모든 슬롯이 꽉 찬 경우
        Debug.LogError($"[{acrId}] 모든 보관소가 꽉 찼습니다! 적재할 공간이 없습니다.");
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
        Transform targetSlot = FindEmptyStorageSlot(pickupWorldHeight);

        // 만약 빈 슬롯을 찾지 못했다면 시퀀스를 중단하고 에러 처리
        if (targetSlot == null)
        {
            Debug.LogError($"[{acrId}] 적재 실패: 빈 슬롯이 없어 Pickup 시퀀스를 중단합니다.");
            yield break;
        }

        float targetLiftHeight = targetSlot.position.y - transform.position.y;
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLiftHeight));
        Debug.Log($"[{acrId}] 7단계 (슬롯 높이로 리프트 이동) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 8단계: 슬롯을 향해 Gripper 전진
        Vector3 localPosInSlider = gripperController.transform.parent.InverseTransformPoint(targetSlot.position);
        float slideDistanceToStorage = localPosInSlider.z;
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToStorage));
        Debug.Log($"[{acrId}] 8단계 (슬롯으로 슬라이더 전진) 완료!");

        // 9단계: 적재 (박스 놓기)
        grabController.Release();
        Debug.Log($"[{this.acrId}] 9단계 (적재) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 10단계: Gripper 후진 (원위치로)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToStorage));
        Debug.Log($"[{this.acrId}] 10단계 (내부 적재 공간에서 후진) 완료!");

        Debug.Log($"--- [{this.acrId}] 모든 물리 작업 완료! 중앙 관제소에 보고합니다. ---");
        //// '나의 작업이 끝났다'고 ID를 명시하여 관제소에 보고
        //ACREvents.RaiseActionCompleted(this.acrId);
    }

    // 나중에 dropoff 기능이 필요하면 여기에 추가하면 됩니다.
    public IEnumerator DropoffSequence(Dictionary<string, object> stopData)
    {
        Debug.Log($"--- [{this.acrId}] 물품 하역(Dropoff) 시퀀스 시작 ---");
        // ... Dropoff 로직 ...
        yield return new WaitForSeconds(1.0f);
        Debug.Log($"--- [{this.acrId}] Dropoff 작업 완료! ---");
    }



}