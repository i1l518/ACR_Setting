/*// 파일명: IPhysicalAction.cs
using System.Collections;
using System.Collections.Generic;

// 모든 물리 작업(Pickup, Dropoff 등)이 따라야 할 규칙
public interface IPhysicalAction
{
    // 이 작업을 수행하는 코루틴
    IEnumerator Execute(Dictionary<string, object> stopData);
}*/