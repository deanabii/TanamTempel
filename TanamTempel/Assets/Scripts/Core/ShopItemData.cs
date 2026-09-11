using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enum kategori item di Toko:
/// Tanam = Biji, Benih, Pot, Peralatan Bercocok Tanam
/// Dekorasi = Hiasan Lingkungan (syarat kemenangan game)
/// </summary>
public enum ShopCategory
{
    Tanam,
    Dekorasi
}

/// <summary>
/// ScriptableObject untuk membuat data item yang dijual di Toko.
/// </summary>
[CreateAssetMenu(fileName = "NewShopItem", menuName = "TanamTempel/Shop Item", order = 2)]
public class ShopItemData : ScriptableObject
{
    [Header("Informasi Item")]
    [Tooltip("Nama item (misal: Biji Tomat, Pot Dinding, Tanaman Hias)")]
    public string itemName = "Item Baru";

    [TextArea(2, 4)]
    [Tooltip("Deskripsi singkat item (opsional)")]
    public string itemDescription = "Deskripsi item";

    [Tooltip("Ikon gambar item untuk ditampilkan di UI Toko")]
    public Sprite itemIcon;

    [Header("Pengaturan Toko")]
    [Tooltip("Harga item dalam koin")]
    public int price = 10;

    [Tooltip("Kategori item: Tanam (dapat dibeli berulang) atau Dekorasi (diperlukan untuk menang)")]
    public ShopCategory category = ShopCategory.Tanam;

    [Header("Prefab Visual / 3D (Opsional)")]
    [Tooltip("Prefab 3D yang akan di-spawn di titik Spawn Point saat item ini dibeli. Kosongkan jika item ini hanya berupa aksi Tag.")]
    public GameObject itemPrefab;

    [Header("Aksi Tag / Nama Objek Saat Dibeli (Opsional)")]
    [Tooltip("Aktifkan jika pembelian item ini menjalankan aksi aktif/nonaktifkan GameObject di scene berdasarkan Tag atau Nama Objek.")]
    public bool useTagAction = false;

    [Tooltip("Daftar Tag GameObject di scene yang akan DIHIDUPKAN (SetActive: true) saat item ini dibeli (misal: 'Gate', 'SampahMagicEffect').")]
    public List<string> tagsToActivate = new List<string>();

    [Tooltip("Daftar Tag GameObject di scene yang akan DINONAKTIFKAN (SetActive: false) saat item ini dibeli (misal: 'Sampah').")]
    public List<string> tagsToDeactivate = new List<string>();

    [Tooltip("Daftar Nama GameObject di Hierarchy scene yang akan DIHIDUPKAN saat dibeli.")]
    public List<string> objectNamesToActivate = new List<string>();

    [Tooltip("Daftar Nama GameObject di Hierarchy scene yang akan DINONAKTIFKAN saat dibeli.")]
    public List<string> objectNamesToDeactivate = new List<string>();

    [Header("Status Pembelian (Dekorasi)")]
    [Tooltip("Status apakah item dekorasi ini telah dibeli (otomatis diperbarui saat dibeli)")]
    public bool isPurchased = false;

    private void OnEnable()
    {
        // Reset status di awal agar testing tidak menyimpan state sebelumnya secara tidak sengaja
        isPurchased = false;
    }
}
