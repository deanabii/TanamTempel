using UnityEngine;

/// <summary>
/// Komponen sederhana (MVP) untuk objek yang bisa diambil.
/// Menjaga ukuran (scale) asli objek agar tidak berubah saat dipindahkan antar parent.
/// </summary>
public class Grabbable : MonoBehaviour
{
    [Tooltip("Anak objek yang memiliki SpriteRenderer untuk tulisan/ikon indikator Grab")]
    public GameObject indicator;

    [Header("Pengaturan Pegang (Grab)")]
    [Tooltip("Rotasi objek saat dipegang di tangan pemain (Euler Angles: X, Y, Z).")]
    public Vector3 grabRotation = Vector3.zero;

    [Tooltip("Offset posisi objek saat dipegang relatif terhadap posisi tangan.")]
    public Vector3 grabPositionOffset = Vector3.zero;

    [HideInInspector] public bool isGrabbed = false;

    private Rigidbody _rb;
    private Camera _mainCam;
    private Vector3 _originalLocalScale;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _mainCam = Camera.main;

        // Simpan ukuran lokal asli objek sejak awal
        _originalLocalScale = transform.localScale;

        // Cari otomatis jika belum di-drag ke Inspector (abaikan anak objek indikator tanam)
        if (indicator == null)
        {
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                string lower = sr.gameObject.name.ToLower();
                if (sr.gameObject != gameObject && !lower.Contains("plant") && !lower.Contains("tanam") && !lower.Contains("water") && !lower.Contains("siram") && !lower.Contains("fill") && !lower.Contains("isi"))
                {
                    indicator = sr.gameObject;
                    break;
                }
            }
        }

        SetIndicator(false);
    }

    private void LateUpdate()
    {
        // Buat indikator selalu menghadap kamera jika sedang aktif
        if (indicator != null && indicator.activeSelf && _mainCam != null)
        {
            indicator.transform.forward = _mainCam.transform.forward;
        }
    }

    public void SetIndicator(bool show)
    {
        if (indicator != null)
        {
            indicator.SetActive(show);
        }
    }

    public void Grab(Transform hand)
    {
        isGrabbed = true;
        SetIndicator(false);

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
        }

        // Lepas dulu dari parent sebelumnya ke root dan pulihkan ukuran asli
        transform.SetParent(null);
        transform.localScale = _originalLocalScale;

        // Pindahkan ke tangan dan jaga ukuran asli
        transform.SetParent(hand);
        transform.localPosition = grabPositionOffset;
        transform.localRotation = Quaternion.Euler(grabRotation);
        transform.localScale = _originalLocalScale;
    }

    public void Drop()
    {
        isGrabbed = false;
        transform.SetParent(null);
        transform.localScale = _originalLocalScale;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.detectCollisions = true;
        }
    }

    public Vector3 GetOriginalScale()
    {
        return _originalLocalScale;
    }
}
