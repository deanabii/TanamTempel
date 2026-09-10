using UnityEngine;

/// <summary>
/// Komponen untuk objek ber-tag 'Pot'.
/// Mengatur tampilan ghost object di dinding Grid dan penempatan (placement) pot ke dinding.
/// </summary>
[RequireComponent(typeof(Grabbable))]
public class Pot : MonoBehaviour
{
    [Header("Ghost Object (Preview)")]
    [Tooltip("Prefab/GameObject bayangan preview (opsional). Jika dikosongkan, script akan otomatis membuat bayangan semi-transparan dari model Pot ini.")]
    public GameObject ghostPrefab;

    [Tooltip("Rotasi tambahan jika model pot perlu diputar saat menempel di dinding (Euler).")]
    public Vector3 wallRotationOffset = Vector3.zero;

    [HideInInspector] public GameObject ghostInstance;
    private Grabbable _grabbable;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();

        // Pastikan tag terpasang
        if (!gameObject.CompareTag("Pot"))
        {
            try { gameObject.tag = "Pot"; } catch { }
        }
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
    /// Menempatkan pot ke dinding / grid.
    /// </summary>
    public void Place(Transform gridParent, Vector3 position, Quaternion rotation)
    {
        HideGhost();

        if (_grabbable != null)
        {
            _grabbable.isGrabbed = false;
        }

        transform.SetParent(gridParent);
        transform.position = position;
        transform.rotation = rotation * Quaternion.Euler(wallRotationOffset);

        // Pastikan fisika aman saat menempel di dinding
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
            // Buat salinan otomatis dari objek ini
            ghostInstance = Instantiate(gameObject);
            ghostInstance.name = name + "_Ghost";

            // Buang script logika dan fisika dari ghost
            Destroy(ghostInstance.GetComponent<Pot>());
            Destroy(ghostInstance.GetComponent<Grabbable>());
            Rigidbody rb = ghostInstance.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            // Matikan collider pada ghost agar tidak mengganggu raycast
            foreach (var col in ghostInstance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Matikan sprite indikator jika ada di dalam ghost
            foreach (var sr in ghostInstance.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.enabled = false;
            }

            // Buat material semi-transparan
            foreach (var rend in ghostInstance.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in rend.materials)
                {
                    Color c = mat.color;
                    c.a = 0.5f;
                    mat.color = c;
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
