using Firebase.Firestore;
using System.Collections.Generic;

[FirestoreData]
public class CargoItem
{
    [FirestoreProperty] public string itemType { get; set; }
    [FirestoreProperty] public string fromRack { get; set; } // 출고 시 어디서 왔는지 기록
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