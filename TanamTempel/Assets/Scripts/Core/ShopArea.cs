using UnityEngine;

/// <summary>
/// Komponen untuk area Toko / Meja Penjualan.
/// Mendeteksi keberadaan pemain di area toko, menampilkan indikator "Buka Toko / Tekan E",
/// serta menyediakan titik spawnPoint untuk menempatkan item yang dibeli.
/// </summary>
public class ShopArea : MonoBehaviour
{
    [Header("Referensi Toko & Indicator")]
    [Tooltip("Anak objek ikon/teks indikator (misal: 'Tekan E untuk Membuka Toko').")]
    public GameObject shopIndicator;

    [Tooltip("Titik Spawn Point tempat objek yang dibeli akan dimunculkan di dunia game.")]
    public Transform spawnPoint;

    [Tooltip("Referensi ke komponen ShopUI di Canvas UI (jika kosong, otomatis mencari di scene).")]
    public ShopUI shopUI;

    [Header("Pengaturan Deteksi Player")]
    [Tooltip("Jarak maksimal interaksi pemain jika menggunakan sistem Look/Raycast.")]
    public float interactDistance = 3.5f;

    [Tooltip("Apakah menggunakan Trigger Collider untuk memunculkan indikator saat pemain masuk area.")]
    public bool useTriggerCollider = true;

    private Camera _mainCam;
    private bool _isPlayerInArea = false;

    private void Awake()
    {
        _mainCam = Camera.main;

        if (shopUI == null)
        {
            shopUI = FindObjectOfType<ShopUI>();
        }

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        // Auto-assign shopIndicator jika belum diisi di Inspector
        if (shopIndicator == null)
        {
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                string lower = sr.gameObject.name.ToLower();
                if (sr.gameObject != gameObject && (lower.Contains("toko") || lower.Contains("shop") || lower.Contains("buy") || lower.Contains("beli")))
                {
                    shopIndicator = sr.gameObject;
                    break;
                }
            }
        }

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
        if (_mainCam == null) _mainCam = Camera.main;

        // Billboard effect: buat indikator toko selalu menghadap ke kamera
        if (shopIndicator != null && shopIndicator.activeSelf && _mainCam != null)
        {
            shopIndicator.transform.forward = _mainCam.transform.forward;
        }

        // Jika toko sudah terbuka, jangan panggil OpenShop lagi saat mouse diklik
        if (ShopUI.IsShopOpen) return;

        // Jika player di dalam trigger area dan menekan tombol interaksi E / Tap
        if (useTriggerCollider && _isPlayerInArea && IsInputPressed())
        {
            OpenShop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (useTriggerCollider && (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.GetComponentInParent<PlayerGrab>() != null))
        {
            _isPlayerInArea = true;
            SetIndicator(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (useTriggerCollider && (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.GetComponentInParent<PlayerGrab>() != null))
        {
            _isPlayerInArea = false;
            SetIndicator(false);
        }
    }

    /// <summary>
    /// Menampilkan atau menyembunyikan indikator toko.
    /// </summary>
    public void SetIndicator(bool show)
    {
        if (shopIndicator != null)
        {
            shopIndicator.SetActive(show);
        }
    }

    /// <summary>
    /// Membuka UI Toko dan mengirimkan referensi ShopArea ini.
    /// </summary>
    public void OpenShop()
    {
        if (shopUI != null)
        {
            shopUI.OpenShop(this);
        }
        else
        {
            Debug.LogWarning("[ShopArea] ShopUI belum di-assign di scene!");
        }
    }

    private bool IsInputPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame) return true;
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) return true;
#else
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) return true;
#endif
        return false;
    }
}
