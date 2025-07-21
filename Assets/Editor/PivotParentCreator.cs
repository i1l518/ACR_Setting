using UnityEngine;
using UnityEditor;

public class PivotParentCreator
{
    [MenuItem("Tools/Create Pivot Parent At Center %&p")] // ����Ű: Ctrl+Alt+P
    private static void CreatePivotParent()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("���� ����", "�ϳ� �̻��� ���� ������Ʈ�� �������ּ���.", "Ȯ��");
            return;
        }

        foreach (var selectedObject in Selection.gameObjects)
        {
            // �̹� �θ� �ִٸ� �ǳʶٰų�, �ʿ信 ���� ������ �߰��� �� �ֽ��ϴ�.
            if (selectedObject.transform.parent != null && selectedObject.transform.parent.name.EndsWith("_Pivot"))
            {
                Debug.Log(selectedObject.name + " ��(��) �̹� �ǹ� �θ� ������ �־� �ǳʶݴϴ�.");
                continue;
            }

            // 1. �ڽ� ������Ʈ�� ��ü ���(Bounds)�� ���
            var renderers = selectedObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds totalBounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }
            Vector3 center = totalBounds.center;

            // 2. ���ο� �θ� ������Ʈ�� ����� �߽ɿ� ����
            GameObject pivotParent = new GameObject(selectedObject.name + "_Pivot");
            pivotParent.transform.position = center;

            // Undo ����� ���� ���
            Undo.RegisterCreatedObjectUndo(pivotParent, "Create Pivot Parent");
            Undo.SetTransformParent(selectedObject.transform, pivotParent.transform, "Parent to Pivot");

            // 3. ���� ������Ʈ�� �� �θ��� �ڽ����� ����
            selectedObject.transform.SetParent(pivotParent.transform, true); // ���� ������ ����
        }
    }
}