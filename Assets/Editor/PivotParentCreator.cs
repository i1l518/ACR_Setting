using UnityEngine;
using UnityEditor;

public class PivotParentCreator
{
    [MenuItem("Tools/Create Pivot Parent At Center %&p")] // 단축키: Ctrl+Alt+P
    private static void CreatePivotParent()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("선택 오류", "하나 이상의 게임 오브젝트를 선택해주세요.", "확인");
            return;
        }

        foreach (var selectedObject in Selection.gameObjects)
        {
            // 이미 부모가 있다면 건너뛰거나, 필요에 따라 로직을 추가할 수 있습니다.
            if (selectedObject.transform.parent != null && selectedObject.transform.parent.name.EndsWith("_Pivot"))
            {
                Debug.Log(selectedObject.name + " 은(는) 이미 피벗 부모를 가지고 있어 건너뜁니다.");
                continue;
            }

            // 1. 자식 오브젝트의 전체 경계(Bounds)를 계산
            var renderers = selectedObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds totalBounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }
            Vector3 center = totalBounds.center;

            // 2. 새로운 부모 오브젝트를 경계의 중심에 생성
            GameObject pivotParent = new GameObject(selectedObject.name + "_Pivot");
            pivotParent.transform.position = center;

            // Undo 기능을 위해 등록
            Undo.RegisterCreatedObjectUndo(pivotParent, "Create Pivot Parent");
            Undo.SetTransformParent(selectedObject.transform, pivotParent.transform, "Parent to Pivot");

            // 3. 기존 오브젝트를 새 부모의 자식으로 설정
            selectedObject.transform.SetParent(pivotParent.transform, true); // 월드 포지션 유지
        }
    }
}