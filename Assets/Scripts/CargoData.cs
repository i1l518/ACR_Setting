using Firebase.Firestore;
using System.Collections.Generic;

[FirestoreData]
public class CargoItem
{
    [FirestoreProperty] public string itemType { get; set; }
    [FirestoreProperty] public string fromRack { get; set; } // ��� �� ��� �Դ��� ���
}

[FirestoreData]
public class CargoSlot
{
    [FirestoreProperty] public int slotId { get; set; }
    [FirestoreProperty] public string status { get; set; } // "empty" or "occupied"
    [FirestoreProperty] public CargoItem item { get; set; }
}

[FirestoreData]
public class CargoData
{
    [FirestoreProperty] public List<CargoSlot> slots { get; set; }
    [FirestoreProperty] public int itemCount { get; set; }
}