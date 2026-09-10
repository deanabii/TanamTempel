using UnityEngine;

public class WallGardenia : MonoBehaviour
{
    public Transform[] slotPotDinding; // Tentukan titik koordinat di Inspector Unity

    public Transform DapatkanSlotKosong()
    {
        // Logika mencari slot yang belum ditempati pot
        foreach (Transform slot in slotPotDinding)
        {
            if (slot.childCount == 0) return slot;
        }
        return null;
    }
}
