using UnityEngine;

[CreateAssetMenu(fileName = "DataTanamanBaru", menuName = "Pertanian/Data Tanaman")]
public class TanamanData : ScriptableObject
{
    public string namaTanaman;
    public float waktuPerTahap = 5f;
    public GameObject[] prefabTahapanVisual; 
    
    [Header("Ekonomi")]
    public int hargaJual = 150; // <--- TAMBAHKAN INI

    [Header("Sistem Pengairan")]
    public float batasKekeringan = 0f;      
    public float batasOverwater = 100f;     
    public float penguranganAirPerDetik = 5f;
    public float penambahanAirPerSiram = 30f;
    public float nilaiAirIdealAwal = 50f;  
}
