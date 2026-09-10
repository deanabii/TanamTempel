using UnityEngine;

/// <summary>
/// Komponen sederhana (MVP) untuk objek yang bisa diambil.
/// </summary>
public class Grabbable : MonoBehaviour
{
    [Tooltip("Anak objek yang memiliki SpriteRenderer untuk tulisan/ikon indikator")]
    public GameObject indicator;

    [HideInInspector] public bool isGrabbed = false;

    private Rigidbody _rb;
    private Camera _mainCam;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _mainCam = Camera.main;

        // Cari otomatis jika belum di-drag ke Inspector
        if (indicator == null)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.gameObject != gameObject)
            {
                indicator = sr.gameObject;
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

        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        isGrabbed = false;
        transform.SetParent(null);

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.detectCollisions = true;
        }
    }
}
