using UnityEngine;

/// <summary>
/// Komponen untuk area Toko / Meja Penjualan.
/// Mendeteksi keberadaan pemain di area toko, menampilkan indikator buka toko,
/// serta menyediakan titik spawnPoint untuk menempatkan item yang dibeli.
/// Tombol pembuka toko dapat diubah bebas melalui Inspector (default: B, atau key lain).
/// </summary>
public class ShopArea : MonoBehaviour
{
    [Header("Referensi Toko & Indicator")]
    [Tooltip("Anak objek ikon/teks indikator (misal: 'Tekan [B] untuk Membuka Toko').")]
    public GameObject shopIndicator;

    [Tooltip("Titik Spawn Point tempat objek yang dibeli akan dimunculkan di dunia game.")]
    public Transform spawnPoint;

    [Tooltip("Referensi ke komponen ShopUI di Canvas UI (jika kosong, otomatis mencari di scene).")]
    public ShopUI shopUI;

    [Header("Pengaturan Tombol Buka Toko (Terintegrasi dengan Interaksi)")]
#if ENABLE_INPUT_SYSTEM
    [Tooltip("Tombol keyboard untuk membuka toko (default: E, terintegrasi dengan tombol interaksi).")]
    public UnityEngine.InputSystem.Key shopKey = UnityEngine.InputSystem.Key.E;
#endif
    [Tooltip("Tombol keyboard legacy/fallback jika New Input System tidak aktif (default: E).")]
    public KeyCode legacyShopKey = KeyCode.E;

    [Tooltip("Apakah klik kiri mouse juga bisa digunakan untuk membuka toko saat berada di area toko.")]
    public bool allowMouseClick = false;

    [Tooltip("Apakah toko juga bisa dibuka dari mana saja dengan menekan tombol ini (tanpa harus berdiri di area toko).")]
    public bool allowGlobalAccess = false;

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

        UpdateIndicatorText();
        SetIndicator(false);
    }

    private void Start()
    {
        UpdateIndicatorText();
        SetIndicator(false);
    }

    private void OnEnable()
    {
        KeyBindingManager.OnKeyBindingsChanged += UpdateIndicatorText;
        UpdateIndicatorText();
    }

    private void OnDisable()
    {
        KeyBindingManager.OnKeyBindingsChanged -= UpdateIndicatorText;
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

        // Jika toko sudah terbuka, jangan panggil OpenShop lagi
        if (ShopUI.IsShopOpen) return;

        // Jika player di dalam trigger area (atau allowGlobalAccess aktif) dan menekan tombol toko
        bool canOpen = (useTriggerCollider && _isPlayerInArea) || allowGlobalAccess;
        if (canOpen && IsInputPressed())
        {
            // Jika pemain sedang membidik objek interaktif lain (misal: mengambil item dari meja toko/tanaman), dahulukan grab/interact
            if (PlayerGrab.Instance != null && PlayerGrab.Instance.HasInteractionTarget)
            {
                return;
            }

            OpenShop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (useTriggerCollider && (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.GetComponentInParent<PlayerGrab>() != null))
        {
            _isPlayerInArea = true;
            UpdateIndicatorText();
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
            AudioGame.Instance?.PlayShopOpen();
            shopUI.OpenShop(this);
        }
        else
        {
            Debug.LogWarning("[ShopArea] ShopUI belum di-assign di scene!");
        }
    }

    private bool IsInputPressed()
    {
        // 1. Cek sistem KeyBindingManager jika ada
        if (KeyBindingManager.Instance != null)
        {
            if (KeyBindingManager.Instance.IsShopPressed()) return true;
            if (allowMouseClick)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) return true;
#else
                if (Input.GetMouseButtonDown(0)) return true;
#endif
            }
            return false;
        }

        // 2. Fallback jika KeyBindingManager belum ada
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var keyControl = UnityEngine.InputSystem.Keyboard.current[shopKey];
            if (keyControl != null && keyControl.wasPressedThisFrame) return true;
        }

        if (allowMouseClick && UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#else
        if (Input.GetKeyDown(legacyShopKey)) return true;
        if (allowMouseClick && Input.GetMouseButtonDown(0)) return true;
#endif
        return false;
    }

    /// <summary>
    /// Memperbarui teks indikator jika anak objek memiliki komponen TextMeshPro atau Text.
    /// Mengganti karakter di dalam tanda kurung siku [...] dengan nama tombol saat ini.
    /// </summary>
    public void UpdateIndicatorText()
    {
        if (shopIndicator == null) return;

        string keyName = KeyBindingManager.Instance != null
            ? KeyBindingManager.Instance.GetKeyName(KeyAction.InteractAndShop)
            :
#if ENABLE_INPUT_SYSTEM
            shopKey.ToString();
#else
            legacyShopKey.ToString();
#endif

        TMPro.TMP_Text tmp = shopIndicator.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (tmp != null)
        {
            if (tmp.text.Contains("[") && tmp.text.Contains("]"))
            {
                int start = tmp.text.IndexOf('[');
                int end = tmp.text.IndexOf(']');
                if (end > start)
                {
                    tmp.text = tmp.text.Substring(0, start + 1) + keyName + tmp.text.Substring(end);
                }
            }
        }
        else
        {
            UnityEngine.UI.Text uiText = shopIndicator.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (uiText != null && uiText.text.Contains("[") && uiText.text.Contains("]"))
            {
                int start = uiText.text.IndexOf('[');
                int end = uiText.text.IndexOf(']');
                if (end > start)
                {
                    uiText.text = uiText.text.Substring(0, start + 1) + keyName + uiText.text.Substring(end);
                }
            }
        }
    }
}
