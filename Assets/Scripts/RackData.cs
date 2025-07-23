// RackData.cs
using Firebase.Firestore;
using System.Collections.Generic;
using UnityEngine;

[FirestoreData]
public class RackData
{
    [FirestoreProperty]
    public double angle { get; set; }

    [FirestoreProperty]
    public Dictionary<string, double> position { get; set; }

    [FirestoreProperty]
    public int status { get; set; }


    [FirestoreProperty]
    public string itemType { get; set; }

    [FirestoreDocumentId]
    public string DocumentId { get; set; }

    public Vector3 GetPositionVector3()
    {
        if (position != null && position.ContainsKey("x") && position.ContainsKey("y") && position.ContainsKey("z"))
        {
            return new Vector3(
                (float)position["x"],
                (float)position["y"],
                (float)position["z"]
            );
        }
        return Vector3.zero;
    }
}