/*
// ���ϸ�: ACREvents.cs
using System;
using System.Collections.Generic;

/// <summary>
/// ��� ACR ���� ����� �����ϴ� �߾� ������(���� �̺�Ʈ ����ó)�Դϴ�.
/// </summary>
public static class ACREvents
{
    /// <summary>
    /// ACR�� �۾��� ���� Ư�� ��ġ�� �������� �� �߻��ϴ� �̺�Ʈ�Դϴ�.
    /// PhysicalController�� �� ��ȣ�� ��� �������� �۾��� �����մϴ�.
    /// �Ķ����: (� ACR��, � �׼��� ����, � ������ ������ �����ߴ°�)
    /// </summary>
    public static event Action<string, string, Dictionary<string, object>> OnArrivedForAction;
    public static void RaiseArrivedForAction(string acrId, string action, Dictionary<string, object> stopData)
    {
        OnArrivedForAction?.Invoke(acrId, action, stopData);
    }

    /// <summary>
    /// PhysicalController�� �������� �۾��� �Ϸ����� �� �߻��ϴ� �̺�Ʈ�Դϴ�.
    /// ACRController�� �� ��ȣ�� ��� ���� �������� �̵��� �簳�մϴ�.
    /// �Ķ����: (�۾��� �Ϸ��� ACR�� ID)
    /// </summary>
    public static event Action<string> OnActionCompleted;
    public static void RaiseActionCompleted(string acrId)
    {
        OnActionCompleted?.Invoke(acrId);
    }
}
*/