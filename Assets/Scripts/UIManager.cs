// OutboundRequestUI.cs
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class OutboundRequestUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button buttonA;
    public Button buttonB;
    public Button buttonC;
    public Button buttonD;
    public Button requestButton;
    public TMP_Text selectedItemsText;
    public TMP_Text statusMessageText;

    [Header("System References")]
    // <<<--- 변경점 1: CentralControlSystem 대신 TaskManager를 참조합니다. ---
    [Tooltip("씬에 있는 TaskManager 오브젝트를 연결해주세요.")]
    public TaskManager taskManager;

    // 내부적으로 요청 아이템 목록을 관리
    private List<string> requestedItems = new List<string>();
    private const int MAX_ITEMS = 5;

    void Start()
    {
        // <<<--- 변경점 2: 시작 시 taskManager가 연결되었는지 확인합니다. ---
        if (taskManager == null)
        {
            Debug.LogError("[OutboundRequestUI] TaskManager가 연결되지 않았습니다! Inspector에서 설정해주세요.");
            // 모든 버튼을 비활성화하여 오류를 방지합니다.
            SetButtonsInteractable(false);
            statusMessageText.text = "<color=red>시스템 오류: TaskManager 없음</color>";
            return;
        }

        buttonA.onClick.AddListener(() => AddItem("A"));
        buttonB.onClick.AddListener(() => AddItem("B"));
        buttonC.onClick.AddListener(() => AddItem("C"));
        buttonD.onClick.AddListener(() => AddItem("D"));
        requestButton.onClick.AddListener(SendRequest);

        UpdateSelectedItemsUI();
        statusMessageText.text = "";
    }

    private void AddItem(string itemType)
    {
        if (requestedItems.Count >= MAX_ITEMS)
        {
            statusMessageText.text = $"<color=red>최대 {MAX_ITEMS}개까지 선택 가능합니다.</color>";
            return;
        }
        requestedItems.Add(itemType);
        UpdateSelectedItemsUI();
        statusMessageText.text = "";
    }

    private void UpdateSelectedItemsUI()
    {
        if (requestedItems.Count > 0)
        {
            selectedItemsText.text = string.Join(", ", requestedItems);
        }
        else
        {
            selectedItemsText.text = "아이템을 선택하세요...";
        }
    }

    private void SendRequest()
    {
        if (requestedItems.Count == 0)
        {
            statusMessageText.text = "<color=orange>아이템을 하나 이상 선택해주세요.</color>";
            return;
        }

        // <<<--- 변경점 3: taskManager의 함수를 호출하도록 수정합니다. ---
        if (taskManager != null)
        {
            // controlSystem.OnOutboundRequest 대신 taskManager.CreateMultiOutboundTask를 호출
            taskManager.CreateMultiOutboundTask(new List<string>(requestedItems));

            statusMessageText.text = "<color=green>출고 요청이 완료되었습니다.</color>";
            requestedItems.Clear();
            UpdateSelectedItemsUI();
        }
    }

    // 버튼 활성화/비활성화를 위한 헬퍼 함수
    private void SetButtonsInteractable(bool isInteractable)
    {
        buttonA.interactable = isInteractable;
        buttonB.interactable = isInteractable;
        buttonC.interactable = isInteractable;
        buttonD.interactable = isInteractable;
        requestButton.interactable = isInteractable;
    }
}