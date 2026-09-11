using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pengendali UI Menu Pengaturan (Settings Menu) & Key Binding.
/// Menyediakan display untuk melihat key saat ini (Label Text) untuk Interact dan Shop,
/// tombol untuk membuka modal pop-up rebinding, serta otomatis menutup modal dan mengganti binding
/// setelah 1 tombol keyboard ditekan (kecuali tombol Escape untuk membatalkan).
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    private static SettingsMenuUI _instance;
    public static SettingsMenuUI Instance => _instance;

    public static bool IsSettingsOpen
    {
        get
        {
            if (_instance != null && _instance.settingsPanel != null)
            {
                return _instance.settingsPanel.activeInHierarchy;
            }
            return false;
        }
    }

    [Header("Panel Utama Pengaturan")]
    [Tooltip("Panel UI utama Menu Pengaturan (jika kosong, otomatis mencari atau membuat di Canvas).")]
    public GameObject settingsPanel;

    [Header("Panel Modal Rebinding Pop-up")]
    [Tooltip("Panel pop-up yang muncul saat sedang menunggu tombol baru ditekan.")]
    public GameObject rebindModalPanel;

    [Tooltip("Teks instruksi pada modal rebinding (misal: 'Tekan 1 tombol di keyboard...').")]
    public TMPro.TMP_Text rebindModalPromptText;

    [Tooltip("Tombol batal di dalam modal pop-up (opsional).")]
    public Button rebindCancelButton;

    [Header("Display & Tombol Binding: Interaksi & Toko (Satu Tombol)")]
    [Tooltip("Label teks untuk melihat key Interaksi & Toko sekarang (misal: 'E').")]
    public TMPro.TMP_Text interactAndShopCurrentKeyText;

    [Tooltip("Tombol untuk membuka panel/modal rebinding Interaksi & Toko.")]
    public Button interactAndShopRebindButton;

    [Header("Display & Tombol Binding: Menu Pengaturan")]
    [Tooltip("Label teks untuk melihat key Menu Pengaturan sekarang (misal: 'Esc').")]
    public TMPro.TMP_Text menuCurrentKeyText;

    [Tooltip("Tombol untuk membuka panel/modal rebinding Menu Pengaturan.")]
    public Button menuRebindButton;

    [Header("Legacy / Fallback Fields (Untuk Kompatibilitas)")]
    [HideInInspector] public TMPro.TMP_Text interactCurrentKeyText;
    [HideInInspector] public Button interactRebindButton;
    [HideInInspector] public TMPro.TMP_Text shopCurrentKeyText;
    [HideInInspector] public Button shopRebindButton;

    [Header("Tombol Kontrol Menu")]
    [Tooltip("Tombol untuk mengembalikan tombol ke default (Interact & Shop: E, Menu: Escape).")]
    public Button resetButton;

    [Tooltip("Tombol untuk menutup menu pengaturan dan kembali bermain.")]
    public Button closeButton;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // Auto-assign atau buat panel jika belum ada di Inspector
        if (settingsPanel == null)
        {
            SetupDefaultUI();
        }

        SetupListeners();
        CloseSettings();
    }

    private void Start()
    {
        UpdateUI();
    }

    private void OnEnable()
    {
        KeyBindingManager.OnKeyBindingsChanged += UpdateUI;
    }

    private void OnDisable()
    {
        KeyBindingManager.OnKeyBindingsChanged -= UpdateUI;
        if (KeyBindingManager.Instance != null && KeyBindingManager.Instance.IsRebinding)
        {
            KeyBindingManager.Instance.CancelRebind();
        }
    }

    private void Update()
    {
        // Deteksi penekanan tombol Menu Pengaturan saat tidak sedang rebind
        if (KeyBindingManager.Instance != null && !KeyBindingManager.Instance.IsRebinding && KeyBindingManager.Instance.IsMenuPressed())
        {
            // Jika Toko sedang terbuka, biarkan ShopUI yang menangani penutupan toko terlebih dahulu
            if (ShopUI.IsShopOpen) return;

            // Jika modal rebind sedang aktif, tombol Escape akan ditangani oleh KeyBindingManager untuk menutup modal
            if (rebindModalPanel != null && rebindModalPanel.activeSelf) return;

            if (IsSettingsOpen)
            {
                CloseSettings();
            }
            else
            {
                if (!GameManager.IsUIOpen)
                {
                    OpenSettings();
                }
            }
        }
    }

    private void SetupListeners()
    {
        // Hubungkan jika di Inspector menggunakan field lama
        if (interactAndShopRebindButton == null && interactRebindButton != null)
        {
            interactAndShopRebindButton = interactRebindButton;
        }
        if (interactAndShopCurrentKeyText == null && interactCurrentKeyText != null)
        {
            interactAndShopCurrentKeyText = interactCurrentKeyText;
        }

        if (settingsPanel != null)
        {
            FindComponentsInPanel(settingsPanel);
        }

        if (interactAndShopRebindButton != null)
        {
            interactAndShopRebindButton.onClick.RemoveAllListeners();
            interactAndShopRebindButton.onClick.AddListener(() => OpenRebindModal(KeyAction.InteractAndShop));
        }

        if (shopRebindButton != null && shopRebindButton != interactAndShopRebindButton)
        {
            shopRebindButton.onClick.RemoveAllListeners();
            shopRebindButton.onClick.AddListener(() => OpenRebindModal(KeyAction.InteractAndShop));
        }

        if (menuRebindButton != null)
        {
            menuRebindButton.onClick.RemoveAllListeners();
            menuRebindButton.onClick.AddListener(() => OpenRebindModal(KeyAction.SettingsMenu));
        }

        if (rebindCancelButton != null)
        {
            rebindCancelButton.onClick.RemoveAllListeners();
            rebindCancelButton.onClick.AddListener(CancelRebind);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(OnResetClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseSettings);
            closeButton.onClick.AddListener(CloseSettings);
        }
    }

    /// <summary>
    /// Membuka modal pop-up rebinding untuk aksi tertentu.
    /// Modal akan menunggu 1 penekanan tombol keyboard (kecuali Escape untuk batal),
    /// lalu otomatis menutup panelnya dan memperbarui bindingnya.
    /// </summary>
    public void OpenRebindModal(KeyAction action)
    {
        string actionLabel = action switch
        {
            KeyAction.InteractAndShop or KeyAction.Interact or KeyAction.Shop => "Interaksi & Buka Toko (Shop)",
            KeyAction.SettingsMenu => "Menu Pengaturan",
            _ => action.ToString()
        };

        if (rebindModalPromptText != null)
        {
            rebindModalPromptText.text = $"Mengubah Tombol:\n<b><color=#FFD700>{actionLabel}</color></b>\n\nTekan <b>1 tombol</b> apa saja di keyboard...\n\n<size=16><color=#AAAAAA>(Tekan [Escape] untuk membatalkan)</color></size>";
        }

        if (rebindModalPanel != null)
        {
            rebindModalPanel.SetActive(true);
        }

        AudioGame.Instance?.PlayButtonClick();

        KeyBindingManager.Instance.StartRebind(action, (success, newKeyName) =>
        {
            // Panel modal otomatis menghilang setelah 1 tombol ditekan (atau dibatalkan dengan Escape)
            if (rebindModalPanel != null)
            {
                rebindModalPanel.SetActive(false);
            }

            AudioGame.Instance?.PlayButtonClick();

            // Perbarui tampilan label teks key sekarang
            UpdateUI();
        });
    }

    /// <summary>
    /// Membatalkan proses rebind dari tombol modal dan menutup modal.
    /// </summary>
    public void CancelRebind()
    {
        AudioGame.Instance?.PlayButtonClick();

        if (KeyBindingManager.Instance != null && KeyBindingManager.Instance.IsRebinding)
        {
            KeyBindingManager.Instance.CancelRebind();
        }

        if (rebindModalPanel != null)
        {
            rebindModalPanel.SetActive(false);
        }

        UpdateUI();
    }

    /// <summary>
    /// Membuka Menu Pengaturan dan menghentikan pergerakan pemain.
    /// </summary>
    public void OpenSettings()
    {
        AudioGame.Instance?.PlayButtonClick();

        if (rebindModalPanel != null)
        {
            rebindModalPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OpenPanel(settingsPanel);
            }
            else
            {
                settingsPanel.SetActive(true);
            }
        }

        UpdateUI();
    }

    /// <summary>
    /// Menutup Menu Pengaturan dan memulihkan kontrol permainan.
    /// </summary>
    public void CloseSettings()
    {
        AudioGame.Instance?.PlayButtonClick();

        CancelRebind();

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ClosePanel(settingsPanel);
            }
            else
            {
                settingsPanel.SetActive(false);
            }
        }

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        UpdateUI();
    }

    /// <summary>
    /// Dipanggil ketika panel pengaturan ditutup dari luar (misal: tombol yang langsung memanggil GameManager.ClosePanel).
    /// </summary>
    public void OnPanelClosedExternally()
    {
        CancelRebind();

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        UpdateUI();
    }

    private void OnResetClicked()
    {
        AudioGame.Instance?.PlayButtonClick();

        if (KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.ResetToDefault();
        }
        UpdateUI();
    }

    /// <summary>
    /// Memperbarui label teks untuk melihat key sekarang pada setiap binding.
    /// </summary>
    public void UpdateUI()
    {
        if (KeyBindingManager.Instance == null) return;

        string interactShopKey = KeyBindingManager.Instance.GetKeyName(KeyAction.InteractAndShop);

        if (interactAndShopCurrentKeyText != null)
        {
            interactAndShopCurrentKeyText.text = interactShopKey;
        }

        if (interactCurrentKeyText != null && interactCurrentKeyText != interactAndShopCurrentKeyText)
        {
            interactCurrentKeyText.text = interactShopKey;
        }

        if (shopCurrentKeyText != null)
        {
            shopCurrentKeyText.text = interactShopKey;
        }

        if (menuCurrentKeyText != null)
        {
            menuCurrentKeyText.enableAutoSizing = true;
            menuCurrentKeyText.fontSizeMin = 11;
            menuCurrentKeyText.fontSizeMax = 18;
            menuCurrentKeyText.text = KeyBindingManager.Instance.GetKeyName(KeyAction.SettingsMenu);
        }
    }

    /// <summary>
    /// Membuat panel UI pengaturan & modal rebind otomatis jika di Inspector belum ada panel yang di-assign.
    /// </summary>
    private void SetupDefaultUI()
    {
        // Cari panel yang sudah ada di scene terlebih dahulu
        GameObject existing = GameObject.Find("SettingsPanel");
        if (existing == null) existing = GameObject.Find("Setting Panel");
        if (existing == null) existing = GameObject.Find("Settings");
        if (existing != null)
        {
            settingsPanel = existing;
            FindComponentsInPanel(existing);
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("SettingsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        }

        // 1. Buat Root Panel Pengaturan
        GameObject panelObj = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(650, 420);

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);

        // Judul Panel
        CreateText(panelObj.transform, "PENGATURAN KONTROL & KEY BINDING", new Vector2(0, 145), new Vector2(560, 45), 24, FontStyle.Bold, Color.white);

        // Baris 1: Interaksi & Buka Toko (Satu Tombol untuk Ambil, Tanam, Siram, Isi Air, Toko)
        CreateRow(panelObj.transform, "InteractAndShop", "Interaksi & Buka Toko (Shop)", 55, out interactAndShopCurrentKeyText, out interactAndShopRebindButton);

        // Baris 2: Menu Pengaturan
        CreateRow(panelObj.transform, "Menu", "Buka / Tutup Menu Pengaturan", -20, out menuCurrentKeyText, out menuRebindButton);

        // Baris 3: Tombol Reset & Tutup
        resetButton = CreateActionButton(panelObj.transform, "ResetBtn", "Reset Default", new Vector2(-130, -135), new Vector2(210, 46), new Color(0.55f, 0.2f, 0.2f, 1f));
        closeButton = CreateActionButton(panelObj.transform, "CloseBtn", "Tutup / Resume", new Vector2(130, -135), new Vector2(210, 46), new Color(0.2f, 0.55f, 0.3f, 1f));

        settingsPanel = panelObj;

        // 2. Buat Panel Modal Rebinding Pop-up (Menimpa di atas panel pengaturan)
        GameObject modalObj = new GameObject("RebindModalPanel", typeof(RectTransform), typeof(Image));
        modalObj.transform.SetParent(panelObj.transform, false);

        RectTransform modalRect = modalObj.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero;
        modalRect.anchorMax = Vector2.one;
        modalRect.sizeDelta = Vector2.zero; // Full overlay

        Image modalImg = modalObj.GetComponent<Image>();
        modalImg.color = new Color(0.05f, 0.05f, 0.08f, 0.88f);

        // Kotak dialog modal di tengah
        GameObject dialogBox = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
        dialogBox.transform.SetParent(modalObj.transform, false);

        RectTransform dialogRect = dialogBox.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.sizeDelta = new Vector2(480, 270);

        Image dialogImg = dialogBox.GetComponent<Image>();
        dialogImg.color = new Color(0.15f, 0.18f, 0.24f, 1f);

        // Teks instruksi di dalam dialog box
        rebindModalPromptText = CreateText(dialogBox.transform, "Tekan 1 tombol apa saja di keyboard...", new Vector2(0, 30), new Vector2(440, 160), 20, FontStyle.Normal, Color.white);

        // Tombol Batal di dalam dialog box
        rebindCancelButton = CreateActionButton(dialogBox.transform, "CancelBtn", "Batal (Esc)", new Vector2(0, -90), new Vector2(160, 42), new Color(0.4f, 0.4f, 0.45f, 1f));

        rebindModalPanel = modalObj;
        rebindModalPanel.SetActive(false);
    }

    private void CreateRow(Transform parent, string rowId, string label, float posY, out TMPro.TMP_Text keyText, out Button rebindBtn)
    {
        // 1. Label Nama Aksi
        CreateText(parent, label, new Vector2(-120, posY), new Vector2(320, 40), 18, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f), TMPro.TextAlignmentOptions.MidlineLeft);

        // 2. Display Box Label Text untuk melihat key sekarang
        GameObject displayBox = new GameObject(rowId + "KeyDisplay", typeof(RectTransform), typeof(Image));
        displayBox.transform.SetParent(parent, false);
        RectTransform rtBox = displayBox.GetComponent<RectTransform>();
        rtBox.anchoredPosition = new Vector2(85, posY);
        rtBox.sizeDelta = new Vector2(105, 42);
        displayBox.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 1f);

        keyText = CreateText(displayBox.transform, "E", Vector2.zero, new Vector2(105, 42), 18, FontStyle.Bold, Color.yellow);
        keyText.enableAutoSizing = true;
        keyText.fontSizeMin = 11;
        keyText.fontSizeMax = 18;

        // 3. Tombol untuk membuka modal rebinding
        rebindBtn = CreateActionButton(parent, rowId + "RebindBtn", "Ubah", new Vector2(210, posY), new Vector2(110, 42), new Color(0.25f, 0.4f, 0.65f, 1f));
    }

    private void FindComponentsInPanel(GameObject panel)
    {
        if (rebindModalPanel == null) rebindModalPanel = panel.transform.Find("RebindModalPanel")?.gameObject;
        if (rebindModalPanel != null && rebindModalPromptText == null) rebindModalPromptText = rebindModalPanel.GetComponentInChildren<TMPro.TMP_Text>();
        if (rebindModalPanel != null && rebindCancelButton == null) rebindCancelButton = rebindModalPanel.GetComponentInChildren<Button>();

        if (interactAndShopRebindButton == null)
        {
            interactAndShopRebindButton = panel.transform.Find("InteractAndShopRebindBtn")?.GetComponent<Button>()
                ?? panel.transform.Find("InteractRebindBtn")?.GetComponent<Button>();
        }

        if (menuRebindButton == null) menuRebindButton = panel.transform.Find("MenuRebindBtn")?.GetComponent<Button>();
        if (resetButton == null) resetButton = panel.transform.Find("ResetBtn")?.GetComponent<Button>();
        
        if (closeButton == null)
        {
            closeButton = panel.transform.Find("CloseBtn")?.GetComponent<Button>()
                ?? panel.transform.Find("Btn Back")?.GetComponent<Button>()
                ?? panel.transform.Find("BtnBack")?.GetComponent<Button>();

            if (closeButton == null)
            {
                Button[] buttons = panel.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    string bName = btn.gameObject.name.ToLower();
                    if (bName.Contains("back") || bName.Contains("close") || bName.Contains("kembali") || bName.Contains("tutup"))
                    {
                        closeButton = btn;
                        break;
                    }
                }
            }
        }
    }

    private TMPro.TextMeshProUGUI CreateText(Transform parent, string content, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color, TMPro.TextAlignmentOptions align = TMPro.TextAlignmentOptions.Center)
    {
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TMPro.TextMeshProUGUI tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        return tmp;
    }

    private Button CreateActionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = bgColor * 1.25f;
        cb.pressedColor = bgColor * 0.8f;
        btn.colors = cb;

        CreateText(btnObj.transform, label, Vector2.zero, size, 17, FontStyle.Bold, Color.white);
        return btn;
    }
}
