// 파일명: BoxData.cs
using UnityEngine;


/// <summary>
/// 개별 박스 오브젝트에 부착하여 고유 데이터(ID 등)를 관리하는 컴포넌트입니다.
/// </summary>
public class BoxData : MonoBehaviour
{
    [Tooltip("이 박스의 고유 식별자(ID)입니다. 외부 시스템(서버 등)과 연동할 때 사용됩니다.")]
    public string Id;
}