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

    [Tooltip("Rotasi tambahan jika model pot perlu diputar saat menempel di dinding (Euler).")]
    public Vector3 wallRotationOffset = Vector3.zero;

    [Tooltip("Jarak dorong keluar dari dinding (meter).")]
    public float wallOffset = 0.5f;

    [Header("Tanam & Siram / Indicators")]
    [Tooltip("Anak objek yang memiliki SpriteRenderer khusus untuk tulisan/ikon indikator Tanam (berbeda dari indikator Grab)")]
    public GameObject plantIndicator;

    [Tooltip("Anak objek yang memiliki SpriteRenderer/UI khusus untuk tulisan/ikon indikator Siram (muncul saat melihat pot dengan Gayung berair)")]
    public GameObject waterIndicator;

    [Tooltip("Komponen PlantGrowth yang mengatur tahapan pertumbuhan tanaman. Jika dikosongkan, otomatis mencari di objek/anak objek.")]
    public PlantGrowth plantGrowth;

    [Tooltip("Status apakah pot sudah ditanami")]
    public bool isPlanted = false;

    [HideInInspector] public GameObject ghostInstance;
    private Grabbable _grabbable;
    private Vector3 _originalLocalScale;
    private Camera _mainCam;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _mainCam = Camera.main;

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
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                string lower = sr.gameObject.name.ToLower();
                if (sr.gameObject != gameObject && (lower.Contains("siram") || lower.Contains("water")) && !lower.Contains("bar") && !lower.Contains("fill") && !lower.Contains("slider"))
                {
                    waterIndicator = sr.gameObject;
                    break;
                }
            }
        }

        // Pastikan indikator tanam & siram mati di awal
        SetPlantIndicator(false);
        SetWaterIndicator(false);
    }

    private void Start()
    {
        // Pastikan kembali indikator tanam & siram mati saat permainan dimulai
        SetPlantIndicator(false);
        SetWaterIndicator(false);
    }

    private void OnDisable()
    {
        SetPlantIndicator(false);
        SetWaterIndicator(false);
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

        // Billboard effect: buat indikator selalu menghadap ke kamera
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
        }
    }

    /// <summary>
    /// Menanam benih ke dalam pot dan memicu siklus pertumbuhan pada PlantGrowth sesuai data benih.
    /// </summary>
    public void Plant(PlantData data = null)
    {
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
    /// Menampilkan atau menyembunyikan indikator 'Tanam' di atas pot.
    /// </summary>
    public void SetPlantIndicator(bool show)
    {
        if (plantIndicator != null)
        {
            // Hanya menyala jika diminta (show == true) DAN belum ditanami (!isPlanted)
            plantIndicator.SetActive(show && !isPlanted);
        }
    }

    /// <summary>
    /// Menampilkan atau menyembunyikan indikator 'Siram' di atas pot.
    /// Hanya menyala jika diminta (show == true) DAN pot sudah ditanami DAN sedang membutuhkan air.
    /// </summary>
    public void SetWaterIndicator(bool show)
    {
        bool canWater = isPlanted && plantGrowth != null && plantGrowth.needsWater;

        if (waterIndicator != null)
        {
            waterIndicator.SetActive(show && canWater);
        }

        if (plantGrowth != null)
        {
            plantGrowth.SetPromptIndicator(show && canWater);
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
