using UnityEngine;

/// <summary>
/// Komponen untuk objek ber-tag 'Pot'.
/// Mengatur penempatan pot ke dinding Grid dan penanaman Biji.
/// Proses 4 stage pertumbuhan ditangani oleh komponen terpisah 'PlantGrowth.cs'.
/// </summary>
[RequireComponent(typeof(Grabbable))]
public class Pot : MonoBehaviour
{
    [Header("Ghost Object (Preview)")]
    [Tooltip("Prefab khusus untuk ghost preview (opsional). Jika dikosongkan, script akan menduplikasi objek ini dan menggunakan Ghost Material di bawah.")]
    public GameObject ghostPrefab;

    [Tooltip("Material khusus untuk tampilan ghost preview (misal: material transparan/hologram).")]
    public Material ghostMaterial;

    [Tooltip("Rotasi tambahan jika model pot perlu diputar saat menempel di dinding (Euler). Default: (0, 180, 0) agar pot dan ghost preview tidak terbalik 180 Y.")]
    public Vector3 wallRotationOffset = new Vector3(0f, 180f, 0f);

    [Tooltip("Jarak dorong keluar dari dinding (meter).")]
    public float wallOffset = 0.5f;

    [Header("Indikator Prompt Interaksi (Pot)")]
    [Tooltip("Anak objek yang memiliki SpriteRenderer khusus untuk tulisan/ikon indikator Tanam (muncul saat memegang Biji dan melihat Pot kosong)")]
    public GameObject plantIndicator;

    [Tooltip("Anak objek yang memiliki SpriteRenderer/UI khusus untuk tulisan/ikon indikator Siram (muncul saat melihat pot dengan Gayung berair)")]
    public GameObject waterIndicator;

    [Tooltip("Anak objek yang memiliki SpriteRenderer/UI indikator Prompt Panen (muncul saat mengarahkan pandangan ke pot Siap Panen)")]
    public GameObject harvestIndicator;

    [Tooltip("Komponen PlantGrowth yang mengatur tahapan pertumbuhan tanaman. Jika dikosongkan, otomatis mencari di objek/anak objek.")]
    public PlantGrowth plantGrowth;

    [Tooltip("Status apakah pot sudah ditanami")]
    public bool isPlanted = false;

    [Tooltip("Status apakah pot sedang terpasang di dinding grid.")]
    public bool isPlacedOnWall = false;

    [Header("PowerUp Pot")]
    [Tooltip("Multiplier pengganda koin hasil panen di pot ini (misal: 1.0 = normal, 2.0 = 2x koin, 3.0 = 3x koin).")]
    public float coinMultiplier = 1.0f;

    [Tooltip("Multiplier kecepatan pertumbuhan tanaman di pot ini (misal: 1.0 = normal, 2.0 = 2x lebih cepat tumbuh).")]
    public float growthSpeedMultiplier = 1.0f;

    [Header("Visual Indikator Powerup (Opsional)")]
    [Tooltip("Efek visual / VFX (misal Partikel/Aura Glow) yang diaktifkan saat pot memiliki Powerup.")]
    public GameObject powerUpVFX;

    [Tooltip("Komponen TextMeshProUGUI untuk menampilkan label teks Powerup di pot (misal: '2x KOIN' atau '2x SPEED').")]
    public TMPro.TextMeshProUGUI powerUpTextTMP;

    /// <summary>
    /// Property untuk mengecek apakah tanaman di pot ini sudah Siap Panen.
    /// </summary>
    public bool IsReadyToHarvest
    {
        get
        {
            if (plantGrowth == null) plantGrowth = GetComponentInChildren<PlantGrowth>();
            return isPlanted && plantGrowth != null && plantGrowth.IsReadyToHarvest;
        }
    }

    [HideInInspector] public GameObject ghostInstance;
    private Grabbable _grabbable;
    private Vector3 _originalLocalScale;
    private Camera _mainCam;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _mainCam = Camera.main;

        // Cek jika sejak awal pot sudah terpasang sebagai anak objek dari Grid
        if (transform.parent != null && (transform.parent.CompareTag("Grid") || transform.parent.GetComponentInParent<WallGrid>() != null))
        {
            isPlacedOnWall = true;
        }

        // Simpan ukuran lokal asli objek sejak awal
        _originalLocalScale = transform.localScale;

        if (!gameObject.CompareTag("Pot"))
        {
            try { gameObject.tag = "Pot"; } catch { }
        }

        // Cari otomatis komponen PlantGrowth jika belum di-assign
        if (plantGrowth == null)
        {
            plantGrowth = GetComponentInChildren<PlantGrowth>();
        }

        // Cari otomatis indikator siram jika belum di-assign di Inspector
        if (waterIndicator == null)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.gameObject == gameObject) continue;
                string lower = t.name.ToLower();
                if ((lower.Contains("siram") || lower.Contains("water")) && !lower.Contains("bar") && !lower.Contains("fill") && !lower.Contains("slider") && !lower.Contains("need"))
                {
                    waterIndicator = t.gameObject;
                    break;
                }
            }
        }

        // Cari otomatis indikator prompt panen jika belum di-assign di Inspector
        if (harvestIndicator == null)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.gameObject == gameObject) continue;
                string lower = t.name.ToLower();
                if (lower.Contains("panen") || lower.Contains("harvest"))
                {
                    harvestIndicator = t.gameObject;
                    break;
                }
            }
        }

        // Pastikan indikator prompt tanam, siram, & panen mati di awal
        SetPlantIndicator(false);
        SetWaterIndicator(false);
        SetHarvestIndicator(false);
    }

    private void Start()
    {
        // Pastikan kembali semua indikator prompt mati saat permainan dimulai
        SetPlantIndicator(false);
        SetWaterIndicator(false);
        SetHarvestIndicator(false);

        // Perbarui tampilan visual powerup di pot
        UpdatePowerUpVisuals();
    }

    private void OnDisable()
    {
        SetPlantIndicator(false);
        SetWaterIndicator(false);
        SetHarvestIndicator(false);
    }

    private void LateUpdate()
    {
        if (_mainCam == null) _mainCam = Camera.main;

        // Pastikan indikator tanam selalu mati jika sudah ditanami
        if (isPlanted && plantIndicator != null && plantIndicator.activeSelf)
        {
            plantIndicator.SetActive(false);
        }

        // Pastikan indikator siram selalu mati jika pot tidak butuh air
        if (waterIndicator != null && waterIndicator.activeSelf)
        {
            if (!isPlanted || plantGrowth == null || !plantGrowth.needsWater)
            {
                waterIndicator.SetActive(false);
            }
        }

        // Indikator Prompt Panen (harvestIndicator): mati jika tanaman belum Siap Panen
        if (harvestIndicator != null && harvestIndicator.activeSelf)
        {
            if (!IsReadyToHarvest)
            {
                harvestIndicator.SetActive(false);
            }
        }

        // Billboard effect: buat indikator prompt selalu menghadap ke kamera
        if (_mainCam != null)
        {
            if (plantIndicator != null && plantIndicator.activeSelf)
            {
                plantIndicator.transform.forward = _mainCam.transform.forward;
            }

            if (waterIndicator != null && waterIndicator.activeSelf)
            {
                waterIndicator.transform.forward = _mainCam.transform.forward;
            }

            if (harvestIndicator != null && harvestIndicator.activeSelf)
            {
                harvestIndicator.transform.forward = _mainCam.transform.forward;
            }
        }
    }

    /// <summary>
    /// Menanam benih ke dalam pot dan memicu siklus pertumbuhan pada PlantGrowth sesuai data benih.
    /// Hanya dapat dilakukan jika pot sudah dipasang di dinding grid.
    /// </summary>
    public void Plant(PlantData data = null)
    {
        if (!isPlacedOnWall)
        {
            Debug.LogWarning("[Pot] Pot harus dipasang di dinding grid terlebih dahulu sebelum dapat ditanami!");
            return;
        }

        isPlanted = true;
        SetPlantIndicator(false);
        SetWaterIndicator(false);

        // Mulai siklus pertumbuhan tanaman dengan data benih
        if (plantGrowth != null)
        {
            plantGrowth.StartGrowth(data);
        }
    }

    /// <summary>
    /// Dipanggil saat pot diambil oleh pemain dari dinding atau lantai.
    /// </summary>
    public void OnGrabbed()
    {
        isPlacedOnWall = false;
        SetPlantIndicator(false);
    }

    /// <summary>
    /// Menampilkan atau menyembunyikan indikator 'Tanam' di atas pot.
    /// </summary>
    public void SetPlantIndicator(bool show)
    {
        if (plantIndicator != null)
        {
            // Hanya menyala jika diminta (show == true) DAN terpasang di dinding (isPlacedOnWall) DAN belum ditanami (!isPlanted)
            plantIndicator.SetActive(show && isPlacedOnWall && !isPlanted);
        }
    }

    /// <summary>
    /// Menampilkan atau menyembunyikan indikator prompt 'Siram' di atas pot.
    /// Hanya menyala jika diminta (show == true) DAN pot sudah ditanami DAN sedang membutuhkan air.
    /// </summary>
    public void SetWaterIndicator(bool show)
    {
        if (plantGrowth == null) plantGrowth = GetComponentInChildren<PlantGrowth>();
        bool canWater = isPlanted && plantGrowth != null && plantGrowth.needsWater;

        if (waterIndicator != null)
        {
            waterIndicator.SetActive(show && canWater);
        }

        // Fallback ke PlantGrowth jika waterIndicator di Pot belum terhubung
        if (plantGrowth != null)
        {
            if (plantGrowth.needWateringIndicator != null && plantGrowth.needWateringIndicator != waterIndicator)
            {
                plantGrowth.needWateringIndicator.SetActive(show && canWater);
            }
            if (plantGrowth.waterIndicator != null && plantGrowth.waterIndicator != waterIndicator)
            {
                plantGrowth.waterIndicator.SetActive(show && canWater);
            }
        }
    }

    /// <summary>
    /// Menampilkan atau menyembunyikan indikator prompt 'Panen' di atas pot.
    /// Hanya menyala jika diminta (show == true) DAN pot sudah ditanami DAN tanaman Siap Panen.
    /// </summary>
    public void SetHarvestIndicator(bool show)
    {
        if (plantGrowth == null) plantGrowth = GetComponentInChildren<PlantGrowth>();
        bool canHarvest = IsReadyToHarvest;

        if (harvestIndicator != null)
        {
            harvestIndicator.SetActive(show && canHarvest);
        }

        // Fallback ke PlantGrowth jika harvestIndicator di Pot belum terhubung
        if (plantGrowth != null && plantGrowth.readyHarvestIndicator != null && plantGrowth.readyHarvestIndicator != harvestIndicator)
        {
            plantGrowth.readyHarvestIndicator.SetActive(show && canHarvest);
        }
    }

    /// <summary>
    /// Memanen tanaman di dalam pot:
    /// 1. Mengambil koin reward dari ScriptableObject tanaman dan mengalikannya dengan coinMultiplier.
    /// 2. Mereset pertumbuhan tanaman dan menghancurkan model visualnya.
    /// 3. Mengubah kondisi pot menjadi tidak ditanam (isPlanted = false).
    /// </summary>
    public void Harvest()
    {
        if (!IsReadyToHarvest) return;

        int baseCoins = 0;
        if (plantGrowth != null)
        {
            baseCoins = plantGrowth.Harvest();
        }

        // Terapkan multiplier koin dari Powerup Pot (default: 1.0 = normal, 2.0 = 2x koin)
        float mult = Mathf.Max(1.0f, coinMultiplier);
        int finalCoins = Mathf.RoundToInt(baseCoins * mult);

        // Tambahkan koin ke CoinManager
        CoinManager.Instance.AddCoins(finalCoins);

        isPlanted = false;
        SetHarvestIndicator(false);
        SetWaterIndicator(false);
        SetPlantIndicator(false);

        Debug.Log($"[Pot] Memanen tanaman sukses! Base Koin: {baseCoins}, Multiplier Pot: {mult}x -> Mendapatkan {finalCoins} koin.");
    }

    /// <summary>
    /// Menerapkan status Powerup baru pada pot ini (misal dari item toko atau upgrade pot).
    /// </summary>
    public void ApplyPowerUp(float newCoinMultiplier = 1.0f, float newGrowthSpeedMultiplier = 1.0f)
    {
        coinMultiplier = Mathf.Max(1.0f, newCoinMultiplier);
        growthSpeedMultiplier = Mathf.Max(1.0f, newGrowthSpeedMultiplier);

        UpdatePowerUpVisuals();
    }

    /// <summary>
    /// Memperbarui visual indikator / VFX powerup pada pot.
    /// </summary>
    public void UpdatePowerUpVisuals()
    {
        bool hasPowerUp = coinMultiplier > 1.0f || growthSpeedMultiplier > 1.0f;

        if (powerUpVFX != null)
        {
            powerUpVFX.SetActive(hasPowerUp);
        }

        if (powerUpTextTMP != null)
        {
            if (hasPowerUp)
            {
                System.Collections.Generic.List<string> labels = new System.Collections.Generic.List<string>();
                if (coinMultiplier > 1.0f) labels.Add($"{coinMultiplier:0.#}x KOIN");
                if (growthSpeedMultiplier > 1.0f) labels.Add($"{growthSpeedMultiplier:0.#}x SPEED");

                powerUpTextTMP.text = string.Join(" | ", labels);
                powerUpTextTMP.gameObject.SetActive(true);
            }
            else
            {
                powerUpTextTMP.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Mengambil jarak dari pusat objek ke tepi luar Collider.
    /// </summary>
    public float GetColliderOffset()
    {
        return wallOffset;
    }

    /// <summary>
    /// Mengatur rotasi offset penempatan pot pada grid (dalam sudut Euler X, Y, Z).
    /// </summary>
    public void SetWallRotationOffset(Vector3 newOffset)
    {
        wallRotationOffset = newOffset;
    }

    public void SetWallRotationOffset(float x, float y, float z)
    {
        wallRotationOffset = new Vector3(x, y, z);
    }

    /// <summary>
    /// Menampilkan dan memperbarui posisi/rotasi ghost object pada grid.
    /// </summary>
    public void UpdateGhost(Vector3 position, Quaternion rotation)
    {
        if (ghostInstance == null)
        {
            CreateGhostInstance();
        }

        if (ghostInstance != null)
        {
            ghostInstance.SetActive(true);
            ghostInstance.transform.position = position;
            ghostInstance.transform.rotation = rotation * Quaternion.Euler(wallRotationOffset);
            ghostInstance.transform.localScale = _originalLocalScale;
        }
    }

    /// <summary>
    /// Menyembunyikan ghost object.
    /// </summary>
    public void HideGhost()
    {
        if (ghostInstance != null && ghostInstance.activeSelf)
        {
            ghostInstance.SetActive(false);
        }
    }

    /// <summary>
    /// Menempatkan pot ke dinding:
    /// 1. Pindahkan terlebih dahulu ke transform ghost (posisi, rotasi, dan scale sama persis).
    /// 2. Kemudian ubah parent-nya ke grid.
    /// </summary>
    public void Place(Transform gridParent, Vector3 fallbackPosition, Quaternion fallbackRotation)
    {
        // 1. Pindah dulu ke ghost agar posisi, rotasi, dan scale sama persis dengan preview
        transform.SetParent(null);

        if (ghostInstance != null && ghostInstance.activeSelf)
        {
            transform.position = ghostInstance.transform.position;
            transform.rotation = ghostInstance.transform.rotation;
            transform.localScale = ghostInstance.transform.localScale;

            HideGhost();
        }
        else
        {
            transform.position = fallbackPosition;
            transform.rotation = fallbackRotation * Quaternion.Euler(wallRotationOffset);
            transform.localScale = _originalLocalScale;
        }

        // 2. Kemudian ubah parent-nya ke grid
        if (gridParent != null)
        {
            transform.SetParent(gridParent, true);
        }

        isPlacedOnWall = true;

        if (_grabbable != null)
        {
            _grabbable.isGrabbed = false;
        }

        // Kunci fisika saat menempel di dinding
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = true;
        }

        // Aktifkan kembali collider pot
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }
    }

    private void CreateGhostInstance()
    {
        if (ghostPrefab != null)
        {
            ghostInstance = Instantiate(ghostPrefab);
        }
        else
        {
            // Duplikasi objek saat ini
            ghostInstance = Instantiate(gameObject);
            ghostInstance.name = name + "_Ghost";

            // Pastikan ghost tidak punya parent dan ukurannya sama persis dengan pot asli
            ghostInstance.transform.SetParent(null);
            ghostInstance.transform.localScale = _originalLocalScale;

            // Matikan dan buang PlantGrowth pada ghost agar tidak ikut tumbuh
            PlantGrowth ghostGrowth = ghostInstance.GetComponentInChildren<PlantGrowth>();
            if (ghostGrowth != null)
            {
                ghostGrowth.SetWaterIndicator(false);
                ghostGrowth.SetStage(-1);
                Destroy(ghostGrowth);
            }

            // Hapus script logika dan komponen fisika dari ghost
            Destroy(ghostInstance.GetComponent<Pot>());
            Destroy(ghostInstance.GetComponent<Grabbable>());
            Rigidbody rb = ghostInstance.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            // Matikan collider agar tidak memantulkan raycast
            foreach (var col in ghostInstance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Matikan indikator sprite pada ghost
            foreach (var sr in ghostInstance.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.enabled = false;
            }

            // Terapkan ghostMaterial jika ada
            if (ghostMaterial != null)
            {
                foreach (var rend in ghostInstance.GetComponentsInChildren<Renderer>())
                {
                    if (rend is SpriteRenderer) continue;

                    Material[] newMats = new Material[rend.sharedMaterials.Length];
                    for (int i = 0; i < newMats.Length; i++)
                    {
                        newMats[i] = ghostMaterial;
                    }
                    rend.materials = newMats;
                }
            }
            else
            {
                // Fallback jika belum memasukkan ghostMaterial
                foreach (var rend in ghostInstance.GetComponentsInChildren<Renderer>())
                {
                    if (rend is SpriteRenderer) continue;

                    foreach (var mat in rend.materials)
                    {
                        Color c = mat.color;
                        c.a = 0.5f;
                        mat.color = c;
                    }
                }
            }
        }

        ghostInstance.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
        }
    }
}
