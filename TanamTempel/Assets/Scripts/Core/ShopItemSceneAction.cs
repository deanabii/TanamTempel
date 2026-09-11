using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Komponen penangan aksi objek di Scene untuk item Toko tertentu.
/// Dipasang pada GameObject di Hierarchy Scene (misal: GameObject Manager atau Objek Dekorasi itu sendiri).
/// Memungkinkan memilih langsung GameObject spesifik di Scene yang akan DIHIDUPKAN / DINONAKTIFKAN saat item Toko dibeli.
/// </summary>
public class ShopItemSceneAction : MonoBehaviour
{
    [Header("Item Toko Terkait")]
    [Tooltip("Aset ShopItemData yang memicu aksi ini saat dibeli.")]
    public ShopItemData targetShopItem;

    [Header("Daftar Objek Spesifik di Hierarchy Scene")]
    [Tooltip("Daftar GameObject spesifik di Scene yang akan DIHIDUPKAN (SetActive: true) saat item ini dibeli.")]
    public List<GameObject> objectsToActivate = new List<GameObject>();

    [Tooltip("Daftar GameObject spesifik di Scene yang akan DINONAKTIFKAN (SetActive: false) saat item ini dibeli.")]
    public List<GameObject> objectsToDeactivate = new List<GameObject>();

    /// <summary>
    /// Menjalankan aksi aktifasi / deaktifasi objek di scene saat item toko dibeli.
    /// </summary>
    public void ExecuteAction()
    {
        // 1. Nonaktifkan objek spesifik di scene
        if (objectsToDeactivate != null)
        {
            foreach (var obj in objectsToDeactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"[ShopItemSceneAction] Nonaktifkan GameObject spesifik '{obj.name}' karena pembelian {targetShopItem?.itemName}");
                }
            }
        }

        // 2. Hidupkan objek spesifik di scene
        if (objectsToActivate != null)
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"[ShopItemSceneAction] Aktifkan GameObject spesifik '{obj.name}' karena pembelian {targetShopItem?.itemName}");

                    // Jika objek yang diaktifkan adalah DecorationItem, tandai isPlaced = true
                    DecorationItem dec = obj.GetComponent<DecorationItem>();
                    if (dec != null)
                    {
                        if (targetShopItem != null) dec.shopData = targetShopItem;
                        dec.SetPlaced(true);
                    }
                }
            }
        }
    }
}
