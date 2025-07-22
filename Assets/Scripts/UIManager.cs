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
    // <<<--- ������ 1: CentralControlSystem ��� TaskManager�� �����մϴ�. ---
    [Tooltip("���� �ִ� TaskManager ������Ʈ�� �������ּ���.")]
    public TaskManager taskManager;

    // ���������� ��û ������ ����� ����
    private List<string> requestedItems = new List<string>();
    private const int MAX_ITEMS = 5;

    void Start()
    {
        // <<<--- ������ 2: ���� �� taskManager�� ����Ǿ����� Ȯ���մϴ�. ---
        if (taskManager == null)
        {
            Debug.LogError("[OutboundRequestUI] TaskManager�� ������� �ʾҽ��ϴ�! Inspector���� �������ּ���.");
            // ��� ��ư�� ��Ȱ��ȭ�Ͽ� ������ �����մϴ�.
            SetButtonsInteractable(false);
            statusMessageText.text = "<color=red>�ý��� ����: TaskManager ����</color>";
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
            statusMessageText.text = $"<color=red>�ִ� {MAX_ITEMS}������ ���� �����մϴ�.</color>";
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
            selectedItemsText.text = "�������� �����ϼ���...";
        }
    }

    private void SendRequest()
    {
        if (requestedItems.Count == 0)
        {
            statusMessageText.text = "<color=orange>�������� �ϳ� �̻� �������ּ���.</color>";
            return;
        }

        // <<<--- ������ 3: taskManager�� �Լ��� ȣ���ϵ��� �����մϴ�. ---
        if (taskManager != null)
        {
            // controlSystem.OnOutboundRequest ��� taskManager.CreateMultiOutboundTask�� ȣ��
            taskManager.CreateMultiOutboundTask(new List<string>(requestedItems));

            statusMessageText.text = "<color=green>��� ��û�� �Ϸ�Ǿ����ϴ�.</color>";
            requestedItems.Clear();
            UpdateSelectedItemsUI();
        }
    }

    // ��ư Ȱ��ȭ/��Ȱ��ȭ�� ���� ���� �Լ�
    private void SetButtonsInteractable(bool isInteractable)
    {
        buttonA.interactable = isInteractable;
        buttonB.interactable = isInteractable;
        buttonC.interactable = isInteractable;
        buttonD.interactable = isInteractable;
        requestButton.interactable = isInteractable;
    }
}