using UnityEngine;

/// <summary>
/// ScriptableObject untuk menyimpan data dan model 4 stage setiap jenis tanaman.
/// Buat aset baru melalui klik kanan di Project: Create -> TanamTempel -> Plant Data.
/// </summary>
[CreateAssetMenu(fileName = "NewPlantData", menuName = "TanamTempel/Plant Data", order = 1)]
public class PlantData : ScriptableObject
{
    [Header("Informasi Tanaman")]
    [Tooltip("Nama jenis tanaman (misal: Tomat, Cabai, Bunga Matahari)")]
    public string plantName = "Tanaman Baru";

    [Header("Prefab Model 4 Stage")]
    [Tooltip("Prefab model 3D untuk Stage 1: Biji")]
    public GameObject bijiPrefab;

    [Tooltip("Prefab model 3D untuk Stage 2: Tunas")]
    public GameObject tunasPrefab;

    [Tooltip("Prefab model 3D untuk Stage 3: Dewasa")]
    public GameObject dewasaPrefab;

    [Tooltip("Prefab model 3D untuk Stage 4: Siap Panen")]
    public GameObject siapPanenPrefab;

    [Header("Durasi Waktu Pertumbuhan (Detik)")]
    [Tooltip("Waktu dari Stage Biji menuju Tunas (detik)")]
    public float timeBijiToTunas = 5f;

    [Tooltip("Waktu dari Stage Tunas menuju Dewasa (detik)")]
    public float timeTunasToDewasa = 5f;

    [Tooltip("Waktu dari Stage Dewasa menuju Siap Panen (detik)")]
    public float timeDewasaToPanen = 5f;
}
