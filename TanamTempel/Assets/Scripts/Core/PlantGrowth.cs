using UnityEngine;

/// <summary>
/// Enum legacy tahapan pertumbuhan untuk backward-compatibility.
/// </summary>
public enum GrowthStage
{
    None = -1,
    Biji = 0,
    Tunas = 1,
    Dewasa = 2,
    SiapPanen = 3
}

/// <summary>
/// Komponen pengendali pertumbuhan tanaman berbasis ScriptableObject (PlantData).
/// Mengatur pergantian prefab model dan durasi waktu sesuai tahapan (stages) berbasis index.
/// Index terakhir otomatis dianggap sebagai stage Siap Panen.
/// </summary>
public class PlantGrowth : MonoBehaviour
{
    [Header("Titik Posisi Tumbuh")]
    [Tooltip("Titik spawn di pot tempat model tanaman dimunculkan (misal di tengah tanah pot). Jika dikosongkan, menggunakan transform objek ini.")]
    public Transform plantSpawnPoint;

    [Header("Data Tanaman Aktif (ScriptableObject)")]
    [Tooltip("Data tanaman yang sedang tumbuh di pot ini (otomatis didapatkan dari Biji yang ditanam).")]
    public PlantData currentPlantData;

    [Header("Status Pertumbuhan Saat Ini (Index)")]
    [Tooltip("Index stage saat ini (-1 = belum ditanami, 0 = stage pertama, dst. Jika mencapai index terakhir, tanaman Siap Panen).")]
    public int currentStage = -1;

    [Tooltip("Timer waktu pertumbuhan untuk stage saat ini.")]
    public float currentStageTimer = 0f;

    [Header("Sistem Menyiram (Watering)")]
    [Tooltip("Apakah tanaman saat ini sedang membutuhkan air sebelum melanjutkan pertumbuhan.")]
    public bool needsWater = false;

    [Tooltip("Batas waktu tanaman bertahan tanpa disiram (dalam detik). Jika habis, tanaman akan mati.")]
    public float maxWaterWaitTime = 15f;

    [Tooltip("Timer sisa waktu sebelum tanaman mati karena kekeringan.")]
    public float currentWaterWaitTimer = 0f;

    [Header("Indikator Status Tanaman (PlantGrowth)")]
    [Tooltip("Anak objek ikon/teks indikator Status Butuh Air (selalu menyala dari jauh saat tanaman membutuhkan air).")]
    public GameObject needWateringIndicator;

    [Tooltip("Anak objek ikon/teks indikator Status Siap Panen (selalu menyala dari jauh saat tanaman Siap Panen).")]
    public GameObject readyHarvestIndicator;

    [Header("Bar Timer Kematian")]
    [Tooltip("Objek root visual bar timer kekeringan (aktif selama tanaman butuh disiram).")]
    public GameObject waterBarRoot;

    [Tooltip("UI Slider untuk bar timer siram (opsional jika menggunakan World Space Canvas).")]
    public UnityEngine.UI.Slider waterSlider;

    [Tooltip("Transform bar pengisi untuk scaling visual bar (opsional, misal Sprite atau Cube horizontal).")]
    public Transform waterBarFill;

    // Field legacy agar referensi waterIndicator sebelumnya tetap terjaga
    [HideInInspector] public GameObject waterIndicator;

    private GameObject _currentStageInstance;
    private bool _isGrowing = false;
    private Pot _pot;
    private Vector3 _initialBarScale = Vector3.one;
    private Camera _mainCam;

    public PlantStage[] ActiveStages => currentPlantData != null ? currentPlantData.GetStages() : null;
    public int TotalStages => ActiveStages != null ? ActiveStages.Length : 0;
    public bool IsReadyToHarvest => TotalStages > 0 && currentStage >= TotalStages - 1;
    public bool IsPlanted => currentStage >= 0 && currentPlantData != null;

    private void Awake()
    {
        _pot = GetComponentInParent<Pot>();
        if (_pot == null) _pot = GetComponent<Pot>();

        _mainCam = Camera.main;

        if (plantSpawnPoint == null)
        {
            plantSpawnPoint = transform;
        }

        if (waterBarFill != null)
        {
            _initialBarScale = waterBarFill.localScale;
        }

        // Auto-assign legacy waterIndicator jika ada
        if (waterIndicator != null)
        {
            string lower = waterIndicator.name.ToLower();
            if (lower.Contains("bar") || lower.Contains("slider") || lower.Contains("fill"))
            {
                if (waterBarRoot == null) waterBarRoot = waterIndicator;
            }
            else
            {
                if (needWateringIndicator == null) needWateringIndicator = waterIndicator;
            }
        }

        // Auto-assign needWateringIndicator jika belum diisi
        if (needWateringIndicator == null)
        {
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                string lower = sr.gameObject.name.ToLower();
                if (sr.gameObject != gameObject && (lower.Contains("need") || lower.Contains("butuh")) && (lower.Contains("water") || lower.Contains("air") || lower.Contains("siram")))
                {
                    needWateringIndicator = sr.gameObject;
                    break;
                }
            }
        }

        // Auto-assign readyHarvestIndicator jika belum diisi
        if (readyHarvestIndicator == null)
        {
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                string lower = sr.gameObject.name.ToLower();
                if (sr.gameObject != gameObject && (lower.Contains("ready") || lower.Contains("siap")))
                {
                    readyHarvestIndicator = sr.gameObject;
                    break;
                }
            }
        }

        SetWaterBarActive(false);
        if (needWateringIndicator != null) needWateringIndicator.SetActive(false);
        if (readyHarvestIndicator != null) readyHarvestIndicator.SetActive(false);

        // Jika ada data awal di Inspector dan ingin mulai langsung (opsional untuk testing)
        if (currentPlantData != null && currentStage >= 0)
        {
            SetStage(currentStage);
            _isGrowing = true;
        }
        else
        {
            SetStage(-1);
        }
    }

    private void LateUpdate()
    {
        if (_mainCam == null) _mainCam = Camera.main;
        if (_mainCam == null) return;

        // Indikator Status Butuh Air (needWateringIndicator): selalu menyala dari jauh jika tanaman butuh air
        if (needWateringIndicator != null)
        {
            needWateringIndicator.SetActive(needsWater);
            if (needWateringIndicator.activeSelf)
            {
                needWateringIndicator.transform.forward = _mainCam.transform.forward;
            }
        }

        // Indikator Status Siap Panen (readyHarvestIndicator): selalu menyala dari jauh jika tanaman Siap Panen
        if (readyHarvestIndicator != null)
        {
            readyHarvestIndicator.SetActive(IsReadyToHarvest);
            if (readyHarvestIndicator.activeSelf)
            {
                readyHarvestIndicator.transform.forward = _mainCam.transform.forward;
            }
        }

        // Billboard effect: buat bar timer kekeringan selalu menghadap ke kamera
        if (waterBarRoot != null && waterBarRoot.activeSelf)
        {
            waterBarRoot.transform.forward = _mainCam.transform.forward;
        }
    }

    private void Update()
    {
        if (!_isGrowing || currentPlantData == null || currentStage < 0 || IsReadyToHarvest)
            return;

        PlantStage[] stages = ActiveStages;
        if (stages == null || currentStage >= stages.Length)
            return;

        // Jika stage saat ini butuh air, kurangi timer toleransi kekeringan (timer pertumbuhan stage ditahan)
        if (needsWater)
        {
            currentWaterWaitTimer -= Time.deltaTime;
            UpdateWaterBarVisual();

            if (currentWaterWaitTimer <= 0f)
            {
                Die();
            }
            return;
        }

        // Hitung kecepatan tumbuh berdasarkan multiplier powerup dari Pot (default: 1.0 = normal, 2.0 = 2x lebih cepat)
        float speedMultiplier = 1.0f;
        if (_pot != null)
        {
            speedMultiplier = Mathf.Max(0.1f, _pot.growthSpeedMultiplier);
        }

        // Jika sudah disiram (atau stage tidak butuh air), timer durasi stage berjalan sesuai kecepatan
        currentStageTimer += Time.deltaTime * speedMultiplier;

        float stageDuration = stages[currentStage].duration;
        if (currentStageTimer >= stageDuration)
        {
            SetStage(currentStage + 1);
        }
    }

    /// <summary>
    /// Memulai pertumbuhan tanaman berdasarkan data dari ScriptableObject (PlantData).
    /// Dimulai dari index 0.
    /// </summary>
    public void StartGrowth(PlantData data)
    {
        if (data != null)
        {
            currentPlantData = data;
        }

        _isGrowing = true;
        SetStage(0);
    }

    /// <summary>
    /// Mengubah stage ke index tertentu, memunculkan prefab model, dan mengecek kebutuhan air serta status Siap Panen.
    /// </summary>
    public void SetStage(int newStageIndex)
    {
        currentStage = newStageIndex;
        currentStageTimer = 0f;

        // Hancurkan model visual stage sebelumnya
        if (_currentStageInstance != null)
        {
            Destroy(_currentStageInstance);
            _currentStageInstance = null;
        }

        PlantStage[] stages = ActiveStages;
        if (newStageIndex < 0 || stages == null || newStageIndex >= stages.Length)
        {
            currentStage = -1;
            SetWaterBarActive(false);
            SetPromptIndicator(false);
            needsWater = false;
            return;
        }

        PlantStage stageData = stages[newStageIndex];

        // Munculkan prefab model di titik spawn pot
        if (stageData != null && stageData.stagePrefab != null)
        {
            Transform parent = plantSpawnPoint != null ? plantSpawnPoint : transform;
            _currentStageInstance = Instantiate(stageData.stagePrefab, parent);
            _currentStageInstance.transform.localPosition = Vector3.zero;
            _currentStageInstance.transform.localRotation = Quaternion.identity;
        }

        // Cek apakah stage ini adalah index terakhir (Siap Panen)
        if (IsReadyToHarvest)
        {
            needsWater = false;
            SetWaterBarActive(false);
            SetPromptIndicator(false);
            Debug.Log($"[PlantGrowth] Tanaman mencapai stage terakhir (Index: {currentStage} - {stageData?.stageName}): SIAP PANEN!");
            return;
        }

        // Jika bukan stage terakhir, periksa apakah stage ini membutuhkan air sebelum proses tumbuh
        if (stageData != null && stageData.needsWater)
        {
            TriggerNeedsWater();
        }
        else
        {
            needsWater = false;
            SetWaterBarActive(false);
            SetPromptIndicator(false);
        }
    }

    // Overload untuk kemudahan jika menggunakan enum
    public void SetStage(GrowthStage stage)
    {
        SetStage((int)stage);
    }

    /// <summary>
    /// Memicu status kebutuhan air, mengaktifkan timer kematian, dan memunculkan bar timer.
    /// Indikator 'Siram' hanya akan menyala saat pemain membidik pot dengan gayung berisi air.
    /// </summary>
    /// <summary>
    /// Memicu status kebutuhan air, mengaktifkan timer kematian, dan memunculkan bar timer.
    /// </summary>
    public void TriggerNeedsWater()
    {
        needsWater = true;
        currentWaterWaitTimer = maxWaterWaitTime;
        UpdateWaterBarVisual();
        SetWaterBarActive(true);
        if (needWateringIndicator != null) needWateringIndicator.SetActive(true);
    }

    /// <summary>
    /// Menyiram tanaman: mematikan indikator status butuh air & bar timer, lalu melanjutkan perhitungan waktu tumbuh.
    /// </summary>
    public void WaterPlant()
    {
        if (!needsWater) return;

        needsWater = false;
        SetWaterBarActive(false);
        if (needWateringIndicator != null) needWateringIndicator.SetActive(false);

        if (_pot != null)
        {
            _pot.SetWaterIndicator(false);
        }

        Debug.Log($"[PlantGrowth] Tanaman pada stage index {currentStage} berhasil disiram! Pertumbuhan dilanjutkan.");
    }

    /// <summary>
    /// Tanaman mati dan hilang karena kehabisan waktu sebelum disiram.
    /// Pot dikembalikan ke status kosong sehingga bisa ditanami kembali.
    /// </summary>
    public void Die()
    {
        Debug.Log("[PlantGrowth] Tanaman mati karena kehabisan air!");
        needsWater = false;
        _isGrowing = false;
        SetWaterBarActive(false);
        if (needWateringIndicator != null) needWateringIndicator.SetActive(false);
        if (readyHarvestIndicator != null) readyHarvestIndicator.SetActive(false);
        SetStage(-1);
        currentPlantData = null;

        if (_pot == null)
        {
            _pot = GetComponentInParent<Pot>();
            if (_pot == null) _pot = GetComponent<Pot>();
        }

        if (_pot != null)
        {
            _pot.SetWaterIndicator(false);
            _pot.SetHarvestIndicator(false);
            _pot.isPlanted = false;
        }
    }

    /// <summary>
    /// Memanen tanaman setelah mencapai stage Siap Panen.
    /// Menghancurkan model visual tanaman, mengembalikan sisa state ke kondisi kosong,
    /// dan mengembalikan jumlah koin reward dari ScriptableObject.
    /// </summary>
    public int Harvest()
    {
        int coinsEarned = currentPlantData != null ? currentPlantData.coinReward : 10;
        string plantName = currentPlantData != null ? currentPlantData.plantName : "Tanaman";

        Debug.Log($"[PlantGrowth] Memanen {plantName}! Mendapatkan {coinsEarned} koin.");

        needsWater = false;
        _isGrowing = false;
        SetWaterBarActive(false);
        if (needWateringIndicator != null) needWateringIndicator.SetActive(false);
        if (readyHarvestIndicator != null) readyHarvestIndicator.SetActive(false);

        // Hancurkan model 3D visual tanaman saat ini
        if (_currentStageInstance != null)
        {
            Destroy(_currentStageInstance);
            _currentStageInstance = null;
        }

        currentStage = -1;
        currentStageTimer = 0f;
        currentPlantData = null;

        if (_pot == null)
        {
            _pot = GetComponentInParent<Pot>();
            if (_pot == null) _pot = GetComponent<Pot>();
        }

        if (_pot != null)
        {
            _pot.SetWaterIndicator(false);
            _pot.SetHarvestIndicator(false);
            _pot.isPlanted = false;
        }

        return coinsEarned;
    }

    /// <summary>
    /// Memperbarui visual bar timer siram (Slider UI atau Transform fill scale).
    /// </summary>
    private void UpdateWaterBarVisual()
    {
        float ratio = maxWaterWaitTime > 0f ? Mathf.Clamp01(currentWaterWaitTimer / maxWaterWaitTime) : 0f;

        if (waterSlider != null)
        {
            waterSlider.value = ratio;
        }

        if (waterBarFill != null)
        {
            waterBarFill.localScale = new Vector3(_initialBarScale.x * ratio, _initialBarScale.y, _initialBarScale.z);
        }
    }

    /// <summary>
    /// Menampilkan atau menyembunyikan ikon/tulisan indikator status butuh air.
    /// </summary>
    public void SetPromptIndicator(bool show)
    {
        if (needWateringIndicator != null)
        {
            needWateringIndicator.SetActive(show && needsWater);
        }
    }

    /// <summary>
    /// Menampilkan atau menyembunyikan bar timer siram.
    /// </summary>
    public void SetWaterBarActive(bool show)
    {
        if (waterBarRoot != null)
        {
            waterBarRoot.SetActive(show);
        }

        if (waterSlider != null)
        {
            waterSlider.gameObject.SetActive(show);
        }

        if (waterBarFill != null)
        {
            waterBarFill.gameObject.SetActive(show);
        }
    }

    // Metode backward compatibility
    public void SetWaterIndicator(bool show)
    {
        SetPromptIndicator(show);
    }

    private void OnDestroy()
    {
        if (_currentStageInstance != null)
        {
            Destroy(_currentStageInstance);
        }
    }
}
