// ACREvents.cs (이 코드로 파일을 업데이트하거나 새로 만드세요)
using System;
using System.Collections.Generic;

public static class ACREvents
{
    /// <summary>
    /// ACR이 작업을 위해 특정 위치에 도착했을 때 발생하는 이벤트입니다.
    /// PhysicalController가 이 신호를 듣고 물리적인 작업을 시작합니다.
    /// 파라미터: (어떤 ACR이, 어떤 액션을 위해, 어떤 경유지 정보로 도착했는가)
    /// </summary>
    public static event Action<string, string, Dictionary<string, object>> OnArrivedForAction;
    public static void RaiseOnArrivedForAction(string acrId, string action, Dictionary<string, object> stopData)
    {
        OnArrivedForAction?.Invoke(acrId, action, stopData);
    }

    /// <summary>
    /// PhysicalController가 물리적인 작업을 완료했을 때 발생하는 이벤트입니다.
    /// ACRController가 이 신호를 듣고 다음 경유지로 이동을 재개합니다.
    /// 파라미터: (작업을 완료한 ACR의 ID)
    /// </summary>
    public static event Action<string> OnActionCompleted;
    public static void RaiseOnActionCompleted(string acrId)
    {
        OnActionCompleted?.Invoke(acrId);
    }
}