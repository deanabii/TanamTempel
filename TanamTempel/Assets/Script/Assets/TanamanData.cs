using UnityEngine;

[CreateAssetMenu(fileName = "DataTanamanBaru", menuName = "Pertanian/Data Tanaman")]
public class TanamanData : ScriptableObject
{
    public string namaTanaman;
    public float waktuPerTahap = 5f;
    public GameObject[] prefabTahapanVisual; // Benih, Tumbuh, Siap Panen
    
    [Header("Sistem Pengairan")]
    public float batasKekeringan = 0f;      // 0% air = Mati
    public float batasOverwater = 100f;     // 100% air = Mati akibat kelebihan air
    public float penguranganAirPerDetik = 5f;
    public float penambahanAirPerSiram = 30f;
    public float nilaiAirIdeal Awal = 50f;  // Mulai dari tengah-tengah
}
