using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ACR_PhysicalController : MonoBehaviour
{
    [Header("ACR 고유 ID")]
    public string acRId = "acr_01";
    [Header("연결 컴포넌트")]
    public GripperController gripperController;

    void OnEnable() { ACREvents.OnArrivedForAction += HandleArrivedForAction; }
    void OnDisable() { ACREvents.OnArrivedForAction -= HandleArrivedForAction; }

    private void HandleArrivedForAction(string id, string action, Dictionary<string, object> stopData)
    {
        if (id != this.acRId) return;
        Debug.Log($"[{acRId}] 물리 제어기: '{action}' 작업을 위한 도착 신호 수신!");
        if (action == "pickup") StartCoroutine(PickupSequence(stopData));
        else ACREvents.RaiseOnActionCompleted(this.acRId);
    }

    private IEnumerator PickupSequence(Dictionary<string, object> stopData)
    {
        Debug.Log("--- 물품 회수(Pickup) 시퀀스 시작 ---");

        // 1단계: 리프트 상승
        float targetLocalHeight = 0f;
        try
        {
            var locationMap = stopData["source"] as Dictionary<string, object>;
            var posMap = locationMap["position"] as Dictionary<string, object>;
            float targetWorldHeight = Convert.ToSingle(posMap["y"]);
            targetLocalHeight = targetWorldHeight - transform.position.y;
        }
        catch (Exception e) { Debug.LogError($"목표 높이 파싱 실패: {e.Message}"); }
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLocalHeight));
        Debug.Log($"1단계 (리프트 상승) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 2단계: 턴테이블 회전 (랙 방향)
        yield return StartCoroutine(gripperController.RotateTurntableSequence(90f));
        Debug.Log($"2단계 (턴테이블 90도 회전) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 3단계: Gripper 전진 (랙으로)
        float slideDistanceToRack = 1.0f; // 랙까지의 전진 거리 (예시)
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToRack));
        Debug.Log($"3단계 (슬라이더 전진) 완료!");

        // 4단계: 파지 대기
        Debug.Log("4단계 (파지 대기) 시작...");
        yield return new WaitForSeconds(1.0f);
        Debug.Log("4단계 (파지 대기) 완료!");

        // 5단계: Gripper 후진 (원위치로)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToRack));
        Debug.Log("5단계 (슬라이더 후진) 완료!");
        yield return new WaitForSeconds(0.5f);

        // 6단계: 턴테이블 원위치 (정면 방향)
        yield return StartCoroutine(gripperController.RotateTurntableSequence(0f));
        Debug.Log("6단계 (턴테이블 원위치) 완료!");
        yield return new WaitForSeconds(0.5f);

        // ▼▼▼▼▼ 새로운 7, 8, 9단계 추가 ▼▼▼▼▼
        // 7단계: Gripper 전진 (내부 적재 공간으로)
        float slideDistanceToStorage = 0.8f; // 내부 적재 공간까지의 전진 거리 (예시)
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToStorage));
        Debug.Log("7단계 (내부 적재 공간으로 전진) 완료!");

        // 8단계: 적재 대기
        Debug.Log("8단계 (적재 대기) 시작...");
        yield return new WaitForSeconds(0.5f);
        Debug.Log("8단계 (적재 대기) 완료!");

        // 9단계: Gripper 후진 (원위치로)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToStorage));
        Debug.Log("9단계 (내부 적재 공간에서 후진) 완료!");

        Debug.Log("--- 모든 물리 작업 완료! ACRController에게 보고합니다. ---");
        ACREvents.RaiseOnActionCompleted(this.acRId);
    }
}