# ACR-Firebase ������Ʈ

�� ������Ʈ�� ���� �̵� �κ�(AMR) �ùķ��̼� ȯ�濡 Firebase Firestore �����ͺ��̽��� �����ϴ� ����� �ٷ�ϴ�.

##  ����

- [������Ʈ ����](#-������Ʈ-����)
- [Firebase ����](#-firebase-����)

---

## ? ������Ʈ ����

�⺻ ȯ���� �����ϰ� ��ֹ�(Obstacle)�� ��ġ�ϴ� �����Դϴ�.

1.  **������Ʈ ���� �ٿ�ε� �� ����**
    -   ������ `.zip` ������ �ٿ�ε��մϴ�.
    -   ������ ������ ��, ���� Unity ������Ʈ ������ **�����ϴ�.**

2.  **���� ������Ʈ(Share Object) ��ġ**
    -   Unity �������� `Project` â���� `Assets/Share` ������ �̵��մϴ�.
    -   `Our Project`, `ACR_Pivot` ������ `Hierarchy` â���� �巡�� �� ����Ͽ� ���� ��ġ�մϴ�.

    ![Share ������ ������Ʈ](./Image/share-object.png)

3.  **������(Prefab) ���� ����**
    -   `Hierarchy` â���� `Our Project`�� `ACR_Pivot` ������Ʈ�� ���� �����մϴ�.
    -   ���콺 ��Ŭ�� �� `Prefab > Unpack Completely`�� �����Ͽ� �����հ��� ������ ������ �����մϴ�. �̴� ������Ʈ�� �����Ӱ� �����ϱ� �����Դϴ�.

    ![������ Unpack Completely](./Image/unpack.png)

4.  **��ֹ� Static ����**
    -   ���� ��ġ�� ��ֹ�(����, �� ��) ������ �ϴ� ��� ���� ������Ʈ�� �����մϴ�.
    -   `Inspector` â ���� ����� **Static** ��Ӵٿ� �޴��� Ŭ���ϰ�, **Everything**�� �����մϴ�. �̴� Unity�� �׺���̼� �� ����Ʈ���� �ý����� �ش� ������Ʈ���� ������ ȯ�� ��ҷ� �ν��ϵ��� �����ϴ� �߿��� �����Դϴ�.

    ![Static ����](./Image/static.png)

5. **��ü�� ������ �ٴ� ����**
    -   Unity���� ��ü�� ������ �ٴ��� Ŭ���Ѵ�
    -   Inspector â�� Navmesh�� �߰��ϰ� agent type�� �����ϰ� bake�� ������.

    ![Bake �ϴ� ��](./Image/Bake.png) 

---

## ? Firebase ����

Unity ������Ʈ�� Firebase Firestore �����ͺ��̽��� �����ϴ� �����Դϴ�.

1.  **Firebase Unity SDK �ٿ�ε�**
    -   [Firebase ���� ����](https://firebase.google.com/docs/unity/setup)�� �����մϴ�.
    -   **`.NET Framework 4.x`** ������ SDK (`.zip` ����)�� �ٿ�ε��մϴ�.

    ![Unity SDK ���� �ٿ�](./Image/Unity-SDK.png)

2.  **�ʼ� ��Ű�� ����Ʈ**
    -   �ٿ�ε��� SDK�� ������ �����ϸ� `dotnet4` ������ �ֽ��ϴ�.
    -   �Ʒ� **�� ���� `.unitypackage` ����**�� Unity ������Ʈ�� `Assets` â���� ������� �巡�� �� ����Ͽ� ����Ʈ�մϴ�.

    > ** ?? ������ ��:** ���Ӽ� ������ �����ϱ� ���� �ݵ�� �Ʒ� ������ �����ּ���.
    >
    > 1.  `FirebaseAuth.unitypackage` (���� �� �ٽ� ���̺귯��)
    > 2.  `FirebaseFirestore.unitypackage` (Firestore �����ͺ��̽�)

3.  **����Ʈ Ȯ��**
    -   �� ��Ű���� �巡���ϸ� "Import Unity Package" â�� ��Ÿ���ϴ�.
    -   ��� �׸��� üũ�� �⺻ ���¿��� `Import` ��ư�� Ŭ���Ͽ� ����Ʈ�� �Ϸ��մϴ�.

���� ������Ʈ�� Firebase ������ ���� ��� �غ� �Ϸ�Ǿ����ϴ�.