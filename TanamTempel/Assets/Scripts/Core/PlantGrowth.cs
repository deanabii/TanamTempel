using UnityEngine;

/// <summary>
/// 4 Tahapan Pertumbuhan Tanaman.
/// </summary>
public enum GrowthStage
{
    None,       // Belum ditanami
    Biji,       // Stage 1: Biji tertanam
    Tunas,      // Stage 2: Tunas muncul
    Dewasa,     // Stage 3: Tanaman dewasa
    SiapPanen   // Stage 4: Siap dipanen
}

/// <summary>
/// Komponen pengendali pertumbuhan tanaman berbasis ScriptableObject (PlantData).
/// Mengatur pergantian model prefab 4 stage dan durasi waktu sesuai data tanaman yang ditanam.
/// </summary>
public class PlantGrowth : MonoBehaviour
{
    [Header("Titik Posisi Tumbuh")]
    [Tooltip("Titik spawn di pot tempat model tanaman dimunculkan (misal di tengah tanah pot). Jika dikosongkan, menggunakan transform objek ini.")]
    public Transform plantSpawnPoint;

    [Header("Data Tanaman Aktif (ScriptableObject)")]
    [Tooltip("Data tanaman yang sedang tumbuh di pot ini (otomatis didapatkan dari Biji yang ditanam).")]
    public PlantData currentPlantData;

    [Header("Status Pertumbuhan Saat Ini")]
    public GrowthStage currentStage = GrowthStage.None;
    public float currentStageTimer = 0f;

    private GameObject _currentStageInstance;
    private bool _isGrowing = false;

    private void Awake()
    {
        if (plantSpawnPoint == null)
        {
            plantSpawnPoint = transform;
        }

        // Jika ada data awal di Inspector dan ingin mulai langsung (opsional untuk testing)
        if (currentPlantData != null && currentStage != GrowthStage.None)
        {
            SetStage(currentStage);
            _isGrowing = true;
        }
        else
        {
            SetStage(GrowthStage.None);
        }
    }

    private void Update()
    {
        if (!_isGrowing || currentPlantData == null || currentStage == GrowthStage.SiapPanen || currentStage == GrowthStage.None)
            return;

        currentStageTimer += Time.deltaTime;

        switch (currentStage)
        {
            case GrowthStage.Biji:
                if (currentStageTimer >= currentPlantData.timeBijiToTunas)
                {
                    SetStage(GrowthStage.Tunas);
                }
                break;

            case GrowthStage.Tunas:
                if (currentStageTimer >= currentPlantData.timeTunasToDewasa)
                {
                    SetStage(GrowthStage.Dewasa);
                }
                break;

            case GrowthStage.Dewasa:
                if (currentStageTimer >= currentPlantData.timeDewasaToPanen)
                {
                    SetStage(GrowthStage.SiapPanen);
                }
                break;
        }
    }

    /// <summary>
    /// Memulai pertumbuhan tanaman berdasarkan data dari ScriptableObject (PlantData).
    /// </summary>
    public void StartGrowth(PlantData data)
    {
        if (data != null)
        {
            currentPlantData = data;
        }

        _isGrowing = true;
        SetStage(GrowthStage.Biji);
    }

    /// <summary>
    /// Mengubah stage dan memunculkan prefab model yang sesuai dari PlantData.
    /// </summary>
    public void SetStage(GrowthStage newStage)
    {
        currentStage = newStage;
        currentStageTimer = 0f;

        // Hancurkan model visual stage sebelumnya
        if (_currentStageInstance != null)
        {
            Destroy(_currentStageInstance);
            _currentStageInstance = null;
        }

        if (newStage == GrowthStage.None || currentPlantData == null)
            return;

        // Ambil prefab sesuai stage dari PlantData
        GameObject prefabToSpawn = null;
        switch (newStage)
        {
            case GrowthStage.Biji:
                prefabToSpawn = currentPlantData.bijiPrefab;
                break;
            case GrowthStage.Tunas:
                prefabToSpawn = currentPlantData.tunasPrefab;
                break;
            case GrowthStage.Dewasa:
                prefabToSpawn = currentPlantData.dewasaPrefab;
                break;
            case GrowthStage.SiapPanen:
                prefabToSpawn = currentPlantData.siapPanenPrefab;
                break;
        }

        // Munculkan prefab model di titik spawn pot
        if (prefabToSpawn != null)
        {
            Transform parent = plantSpawnPoint != null ? plantSpawnPoint : transform;
            _currentStageInstance = Instantiate(prefabToSpawn, parent);
            _currentStageInstance.transform.localPosition = Vector3.zero;
            _currentStageInstance.transform.localRotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        if (_currentStageInstance != null)
        {
            Destroy(_currentStageInstance);
        }
    }
}
