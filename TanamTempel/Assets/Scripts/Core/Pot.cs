using UnityEngine;

/// <summary>
/// Komponen untuk objek ber-tag 'Pot'.
/// Mengatur tampilan ghost object di dinding Grid dan penempatan pot ke dinding.
/// Menempatkan pot dengan cara menyamakannya ke transform ghost terlebih dahulu,
/// baru kemudian mengubah parent ke grid agar ukuran/scale tidak berubah.
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

    [HideInInspector] public GameObject ghostInstance;
    private Grabbable _grabbable;
    private Vector3 _originalLocalScale;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();

        // Simpan ukuran lokal asli objek sejak awal
        _originalLocalScale = transform.localScale;

        if (!gameObject.CompareTag("Pot"))
        {
            try { gameObject.tag = "Pot"; } catch { }
        }
    }

    /// <summary>
    /// Mengambil jarak dari pusat objek ke tepi luar Collider secara otomatis
    /// berdasarkan ukuran asli objek.
    /// </summary>
    public float GetColliderOffset()
    {
        Collider col = GetComponentInChildren<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                float halfX = box.size.x * Mathf.Abs(_originalLocalScale.x) * 0.5f;
                float halfZ = box.size.z * Mathf.Abs(_originalLocalScale.z) * 0.5f;
                return Mathf.Max(halfX, halfZ);
            }

            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }

        return 0.5f;
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
