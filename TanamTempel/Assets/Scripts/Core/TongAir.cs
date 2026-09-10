using UnityEngine;

/// <summary>
/// Komponen untuk objek Tong Air (Water Barrel / Source).
/// Berfungsi sebagai sumber pengisian air untuk objek Gayung.
/// Menampilkan indikator isi air saat didekati pemain yang memegang Gayung.
/// </summary>
public class TongAir : MonoBehaviour
{
    [Header("Indikator Isi Air")]
    [Tooltip("Anak objek yang memiliki ikon/teks indikator untuk mengisi air ke Gayung.")]
    public GameObject fillIndicator;

    private Camera _mainCam;

    private void Awake()
    {
        _mainCam = Camera.main;

        try
        {
            if (!CompareTag("TongAir")) tag = "TongAir";
        }
        catch { }

        SetIndicator(false);
    }

    private void Start()
    {
        SetIndicator(false);
    }

    private void OnDisable()
    {
        SetIndicator(false);
    }

    private void LateUpdate()
    {
        // Billboard effect: buat indikator isi air selalu menghadap ke kamera
        if (fillIndicator != null && fillIndicator.activeSelf)
        {
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam != null)
            {
                fillIndicator.transform.forward = _mainCam.transform.forward;
            }
        }
    }

    /// <summary>
    /// Menampilkan atau menyembunyikan indikator isi air.
    /// </summary>
    public void SetIndicator(bool show)
    {
        if (fillIndicator != null)
        {
            fillIndicator.SetActive(show);
        }
    }
}
