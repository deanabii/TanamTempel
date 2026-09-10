using UnityEngine;

/// <summary>
/// Komponen untuk objek Gayung (Water Scoop / Dipper).
/// Memiliki 2 kondisi: Kosong dan Terisi air.
/// Saat terisi, bisa digunakan menyiram tanaman hingga 3 kali.
/// Diisi ulang dengan berinteraksi ke Tong Air.
/// </summary>
[RequireComponent(typeof(Grabbable))]
public class Gayung : MonoBehaviour
{
    [Header("Pengaturan Kapasitas Air")]
    [Tooltip("Jumlah maksimal menyiram per gayung (default 3 kali).")]
    public int maxWater = 3;

    [Tooltip("Jumlah air saat ini (0 = Kosong, > 0 = Terisi).")]
    public int currentWater = 0;

    [Header("Visual Kondisi Gayung")]
    [Tooltip("Objek visual air atau model gayung terisi (aktif jika ada air).")]
    public GameObject filledVisual;

    [Tooltip("Objek visual gayung kosong (aktif jika air habis).")]
    public GameObject emptyVisual;

    [Tooltip("Opsional: Array visual level air (misal: index 0 = 1 air, index 1 = 2 air, index 2 = 3 air).")]
    public GameObject[] waterLevelVisuals;

    public bool HasWater => currentWater > 0;
    public bool IsFull => currentWater >= maxWater;

    private Grabbable _grabbable;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();

        try
        {
            if (!CompareTag("Gayung")) tag = "Gayung";
        }
        catch { }

        UpdateVisuals();
    }

    /// <summary>
    /// Mengisi air ke gayung hingga penuh (3/3).
    /// </summary>
    public void Refill()
    {
        currentWater = maxWater;
        UpdateVisuals();
        Debug.Log($"[Gayung] Air terisi penuh: {currentWater}/{maxWater}");
    }

    /// <summary>
    /// Menggunakan 1 takaran air untuk menyiram tanaman.
    /// Mengembalikan true jika berhasil menyiram.
    /// </summary>
    public bool UseWater()
    {
        if (currentWater <= 0)
        {
            Debug.Log("[Gayung] Air kosong! Isi air di Tong Air terlebih dahulu.");
            return false;
        }

        currentWater--;
        UpdateVisuals();
        Debug.Log($"[Gayung] Air disiramkan. Sisa air: {currentWater}/{maxWater}");
        return true;
    }

    /// <summary>
    /// Memperbarui tampilan visual gayung (terisi/kosong & level air).
    /// </summary>
    public void UpdateVisuals()
    {
        if (filledVisual != null)
        {
            filledVisual.SetActive(HasWater);
        }

        if (emptyVisual != null)
        {
            emptyVisual.SetActive(!HasWater);
        }

        if (waterLevelVisuals != null && waterLevelVisuals.Length > 0)
        {
            for (int i = 0; i < waterLevelVisuals.Length; i++)
            {
                if (waterLevelVisuals[i] != null)
                {
                    waterLevelVisuals[i].SetActive(i < currentWater);
                }
            }
        }
    }
}
