using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data satu tahapan (stage) pertumbuhan tanaman.
/// </summary>
[Serializable]
public class PlantStage
{
    [Tooltip("Nama/keterangan stage (opsional, misal: Biji, Tunas, Dewasa, Siap Panen)")]
    public string stageName = "Stage";

    [Tooltip("Prefab model 3D visual untuk stage ini")]
    public GameObject stagePrefab;

    [Tooltip("Durasi waktu stage ini sebelum lanjut ke stage berikutnya (detik). Diabaikan jika stage terakhir (Siap Panen).")]
    public float duration = 5f;

    [Tooltip("Apakah stage ini membutuhkan disiram air terlebih dahulu sebelum durasi pertumbuhannya berjalan?")]
    public bool needsWater = true;
}

/// <summary>
/// ScriptableObject untuk menyimpan data dan model tahapan pertumbuhan tanaman secara dinamis.
/// Jumlah stage bebas diatur di Inspector. Index terakhir otomatis menjadi stage Siap Panen.
/// </summary>
[CreateAssetMenu(fileName = "NewPlantData", menuName = "TanamTempel/Plant Data", order = 1)]
public class PlantData : ScriptableObject
{
    [Header("Informasi Tanaman")]
    [Tooltip("Nama jenis tanaman (misal: Tomat, Cabai, Bunga Matahari)")]
    public string plantName = "Tanaman Baru";

    [Header("Daftar Tahapan Pertumbuhan (Stages)")]
    [Tooltip("Daftar tahapan pertumbuhan tanaman. Jumlah stage bebas diatur di Inspector. Index terakhir otomatis menjadi stage Siap Panen.")]
    public PlantStage[] stages;

    // Field legacy agar data aset lama tidak hilang dan otomatis dimigrasikan ke stages
    [HideInInspector] public GameObject bijiPrefab;
    [HideInInspector] public GameObject tunasPrefab;
    [HideInInspector] public GameObject dewasaPrefab;
    [HideInInspector] public GameObject siapPanenPrefab;
    [HideInInspector] public float timeBijiToTunas = 5f;
    [HideInInspector] public float timeTunasToDewasa = 5f;
    [HideInInspector] public float timeDewasaToPanen = 5f;

    /// <summary>
    /// Mengambil array stages. Jika array stages di Inspector belum diisi tetapi ada legacy prefab,
    /// otomatis mengembalikan data legacy.
    /// </summary>
    public PlantStage[] GetStages()
    {
        if (stages != null && stages.Length > 0)
        {
            return stages;
        }

        List<PlantStage> legacy = new List<PlantStage>();
        if (bijiPrefab != null) legacy.Add(new PlantStage { stageName = "Biji", stagePrefab = bijiPrefab, duration = timeBijiToTunas, needsWater = true });
        if (tunasPrefab != null) legacy.Add(new PlantStage { stageName = "Tunas", stagePrefab = tunasPrefab, duration = timeTunasToDewasa, needsWater = true });
        if (dewasaPrefab != null) legacy.Add(new PlantStage { stageName = "Dewasa", stagePrefab = dewasaPrefab, duration = timeDewasaToPanen, needsWater = true });
        if (siapPanenPrefab != null) legacy.Add(new PlantStage { stageName = "Siap Panen", stagePrefab = siapPanenPrefab, duration = 0f, needsWater = false });

        return legacy.ToArray();
    }

    private void OnValidate()
    {
        // Otomatis populate ke stages jika stages kosong tapi ada aset lama
        if ((stages == null || stages.Length == 0) && (bijiPrefab != null || tunasPrefab != null || dewasaPrefab != null || siapPanenPrefab != null))
        {
            stages = GetStages();
        }
    }
}
