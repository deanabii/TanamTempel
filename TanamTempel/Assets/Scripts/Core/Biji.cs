using UnityEngine;

/// <summary>
/// Komponen sederhana (MVP) untuk objek Biji / Benih tanaman.
/// Membawa referensi PlantData (ScriptableObject) yang akan menentukan
/// jenis tanaman dan model visual yang tumbuh saat ditanam ke Pot.
/// </summary>
[RequireComponent(typeof(Grabbable))]
public class Biji : MonoBehaviour
{
    [Header("Data Tanaman (ScriptableObject)")]
    [Tooltip("Aset PlantData untuk jenis biji ini (berisi model 4 stage dan waktu tumbuhnya)")]
    public PlantData plantData;

    private Grabbable _grabbable;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();

        try
        {
            if (!CompareTag("Biji")) tag = "Biji";
        }
        catch { }
    }

    /// <summary>
    /// Menghilangkan biji setelah ditanam ke dalam pot.
    /// </summary>
    public void Consume()
    {
        Destroy(gameObject);
    }
}
