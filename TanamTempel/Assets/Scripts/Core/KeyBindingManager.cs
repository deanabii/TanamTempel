using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Aksi-aksi yang dapat diatur tombolnya (Key Binding).
/// </summary>
public enum KeyAction
{
    InteractAndShop,    // Interaksi (Pegang / Tanam / Siram / Isi Air) & Masuk Toko (Shop)
    SettingsMenu,       // Membuka / Menutup Menu Pengaturan
    Interact,           // Kompatibilitas mundur (mengarah ke InteractAndShop)
    Shop                // Kompatibilitas mundur (mengarah ke InteractAndShop)
}

/// <summary>
/// Pengendali terpusat sistem Key Binding (Pengaturan Tombol).
/// Mendukung New Input System & Legacy Input, penyimpanan otomatis ke PlayerPrefs,
/// serta fungsi rebind interaktif dari modal panel pengaturan.
/// </summary>
public class KeyBindingManager : MonoBehaviour
{
    private static KeyBindingManager _instance;
    public static KeyBindingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<KeyBindingManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("KeyBindingManager");
                    _instance = go.AddComponent<KeyBindingManager>();
                }
            }
            return _instance;
        }
    }

#if ENABLE_INPUT_SYSTEM
    [Header("Key Bindings (New Input System)")]
    [Tooltip("Tombol terpadu untuk Interaksi (Grab, Tanam, Siram, Isi Air) dan Masuk Toko (Shop).")]
    public Key interactAndShopKey = Key.E;

    // Property helper untuk kompatibilitas mundur jika ada script yang mengakses langsung
    public Key interactKey { get => interactAndShopKey; set => interactAndShopKey = value; }
    public Key shopKey { get => interactAndShopKey; set => interactAndShopKey = value; }

    [Tooltip("Tombol untuk membuka / menutup Menu Pengaturan.")]
    public Key settingsMenuKey = Key.Escape;
#endif

    [Header("Key Bindings (Legacy Fallback)")]
    [Tooltip("Tombol terpadu legacy untuk Interaksi dan Masuk Toko.")]
    public KeyCode legacyInteractAndShopKey = KeyCode.E;
    public KeyCode legacyInteractKey { get => legacyInteractAndShopKey; set => legacyInteractAndShopKey = value; }
    public KeyCode legacyShopKey { get => legacyInteractAndShopKey; set => legacyInteractAndShopKey = value; }
    public KeyCode legacySettingsMenuKey = KeyCode.Escape;

    /// <summary>
    /// Event yang dipicu setiap kali salah satu tombol keybinding diubah.
    /// </summary>
    public static event Action OnKeyBindingsChanged;

    /// <summary>
    /// Apakah sedang dalam proses mendengarkan tombol keyboard baru untuk rebind.
    /// </summary>
    public bool IsRebinding { get; private set; } = false;
    private KeyAction _rebindTargetAction;
    private Action<bool, string> _onRebindResult;

    private const string PREF_INTERACT_SHOP = "KeyBinding_InteractAndShop";
    private const string PREF_INTERACT = "KeyBinding_Interact";
    private const string PREF_SHOP = "KeyBinding_Shop";
    private const string PREF_SETTINGS = "KeyBinding_Settings";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadBindings();
    }

    private void Update()
    {
        if (IsRebinding)
        {
            CheckRebindInput();
        }
    }

    /// <summary>
    /// Memuat key bindings dari PlayerPrefs.
    /// </summary>
    public void LoadBindings()
    {
        string interactShopStr = PlayerPrefs.GetString(PREF_INTERACT_SHOP, "");
        if (string.IsNullOrEmpty(interactShopStr))
        {
            interactShopStr = PlayerPrefs.GetString(PREF_INTERACT, "E");
        }
        string settingsStr = PlayerPrefs.GetString(PREF_SETTINGS, "Escape");

#if ENABLE_INPUT_SYSTEM
        if (Enum.TryParse(interactShopStr, true, out Key ik)) interactAndShopKey = ik;
        if (Enum.TryParse(settingsStr, true, out Key mk)) settingsMenuKey = mk;
#endif

        if (Enum.TryParse(interactShopStr, true, out KeyCode lik)) legacyInteractAndShopKey = lik;
        if (Enum.TryParse(settingsStr, true, out KeyCode lmk)) legacySettingsMenuKey = lmk;
    }

    /// <summary>
    /// Menyimpan key bindings ke PlayerPrefs.
    /// </summary>
    public void SaveBindings()
    {
#if ENABLE_INPUT_SYSTEM
        PlayerPrefs.SetString(PREF_INTERACT_SHOP, interactAndShopKey.ToString());
        PlayerPrefs.SetString(PREF_INTERACT, interactAndShopKey.ToString());
        PlayerPrefs.SetString(PREF_SHOP, interactAndShopKey.ToString());
        PlayerPrefs.SetString(PREF_SETTINGS, settingsMenuKey.ToString());
#else
        PlayerPrefs.SetString(PREF_INTERACT_SHOP, legacyInteractAndShopKey.ToString());
        PlayerPrefs.SetString(PREF_INTERACT, legacyInteractAndShopKey.ToString());
        PlayerPrefs.SetString(PREF_SHOP, legacyInteractAndShopKey.ToString());
        PlayerPrefs.SetString(PREF_SETTINGS, legacySettingsMenuKey.ToString());
#endif
        PlayerPrefs.Save();
        OnKeyBindingsChanged?.Invoke();
    }

    /// <summary>
    /// Mengembalikan semua tombol ke default (Interact & Shop = E, Menu = Escape).
    /// </summary>
    public void ResetToDefault()
    {
#if ENABLE_INPUT_SYSTEM
        interactAndShopKey = Key.E;
        settingsMenuKey = Key.Escape;
#endif
        legacyInteractAndShopKey = KeyCode.E;
        legacySettingsMenuKey = KeyCode.Escape;

        SaveBindings();
        Debug.Log("[KeyBindingManager] Key bindings berhasil di-reset ke default (Interact & Shop: E, Menu: Esc).");
    }

    /// <summary>
    /// Memeriksa apakah tombol terpadu Interaksi & Toko (E) baru saja ditekan pada frame ini.
    /// </summary>
    public bool IsInteractAndShopPressed()
    {
        if (IsRebinding) return false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            var k = Keyboard.current[interactAndShopKey];
            if (k != null && k.wasPressedThisFrame) return true;
        }
#else
        if (Input.GetKeyDown(legacyInteractAndShopKey)) return true;
#endif
        return false;
    }

    /// <summary>
    /// Memeriksa apakah tombol Interaksi (Grab/Tanam/Siram) baru saja ditekan pada frame ini.
    /// Terhubung dengan tombol terpadu Interaksi & Toko.
    /// </summary>
    public bool IsInteractPressed() => IsInteractAndShopPressed();

    /// <summary>
    /// Memeriksa apakah tombol Masuk Toko (Shop) baru saja ditekan pada frame ini.
    /// Terhubung dengan tombol terpadu Interaksi & Toko.
    /// </summary>
    public bool IsShopPressed() => IsInteractAndShopPressed();

    /// <summary>
    /// Memeriksa apakah tombol Menu Pengaturan baru saja ditekan pada frame ini.
    /// </summary>
    public bool IsMenuPressed()
    {
        if (IsRebinding) return false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            var k = Keyboard.current[settingsMenuKey];
            if (k != null && k.wasPressedThisFrame) return true;
        }
#else
        if (Input.GetKeyDown(legacySettingsMenuKey)) return true;
#endif
        return false;
    }

    /// <summary>
    /// Mengambil nama tampilan teks untuk tombol aksi tertentu (misal: 'E', 'Esc', 'Space').
    /// </summary>
    public string GetKeyName(KeyAction action)
    {
#if ENABLE_INPUT_SYSTEM
        Key key = action switch
        {
            KeyAction.InteractAndShop or KeyAction.Interact or KeyAction.Shop => interactAndShopKey,
            KeyAction.SettingsMenu => settingsMenuKey,
            _ => Key.None
        };
        return FormatKeyName(key.ToString());
#else
        KeyCode code = action switch
        {
            KeyAction.InteractAndShop or KeyAction.Interact or KeyAction.Shop => legacyInteractAndShopKey,
            KeyAction.SettingsMenu => legacySettingsMenuKey,
            _ => KeyCode.None
        };
        return FormatKeyName(code.ToString());
#endif
    }

    private string FormatKeyName(string rawName)
    {
        if (rawName.Equals("Escape", StringComparison.OrdinalIgnoreCase)) return "Esc";
        if (rawName.Equals("Space", StringComparison.OrdinalIgnoreCase)) return "Space";
        if (rawName.StartsWith("Digit", StringComparison.OrdinalIgnoreCase)) return rawName.Substring(5);
        if (rawName.StartsWith("Alpha", StringComparison.OrdinalIgnoreCase)) return rawName.Substring(5);
        return rawName;
    }

    /// <summary>
    /// Memulai proses rebind: mendengarkan tombol keyboard berikutnya yang ditekan pemain.
    /// Callback mengembalikan (bool success, string keyName). Jika tombol Escape ditekan, proses dibatalkan (success = false).
    /// </summary>
    public void StartRebind(KeyAction action, Action<bool, string> onComplete)
    {
        _rebindTargetAction = action;
        _onRebindResult = onComplete;
        IsRebinding = true;
        Debug.Log($"[KeyBindingManager] Menunggu input tombol baru untuk {action}...");
    }

    /// <summary>
    /// Membatalkan proses rebind yang sedang berlangsung.
    /// </summary>
    public void CancelRebind()
    {
        if (!IsRebinding) return;
        IsRebinding = false;
        var cb = _onRebindResult;
        _onRebindResult = null;
        cb?.Invoke(false, GetKeyName(_rebindTargetAction));
        Debug.Log($"[KeyBindingManager] Rebind untuk {_rebindTargetAction} dibatalkan.");
    }

    private void CheckRebindInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            foreach (var control in Keyboard.current.allControls)
            {
                if (control is KeyControl keyControl && keyControl.wasPressedThisFrame)
                {
                    Key pressedKey = keyControl.keyCode;

                    // Jika menekan tombol Escape: batalkan rebind tanpa mengubah tombol
                    if (pressedKey == Key.Escape)
                    {
                        CancelRebind();
                        return;
                    }

                    ApplyRebind(pressedKey.ToString());
                    return;
                }
            }
        }
#else
        if (Input.anyKeyDown)
        {
            // Jika menekan Escape: batalkan rebind
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelRebind();
                return;
            }

            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            {
                // Abaikan tombol klik mouse
                if (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6) continue;

                if (Input.GetKeyDown(k))
                {
                    ApplyRebind(k.ToString());
                    return;
                }
            }
        }
#endif
    }

    private void ApplyRebind(string keyName)
    {
#if ENABLE_INPUT_SYSTEM
        if (Enum.TryParse(keyName, true, out Key parsedKey))
        {
            switch (_rebindTargetAction)
            {
                case KeyAction.InteractAndShop:
                case KeyAction.Interact:
                case KeyAction.Shop:
                    interactAndShopKey = parsedKey;
                    break;
                case KeyAction.SettingsMenu:
                    settingsMenuKey = parsedKey;
                    break;
            }
        }
#endif
        if (Enum.TryParse(keyName, true, out KeyCode parsedCode))
        {
            switch (_rebindTargetAction)
            {
                case KeyAction.InteractAndShop:
                case KeyAction.Interact:
                case KeyAction.Shop:
                    legacyInteractAndShopKey = parsedCode;
                    break;
                case KeyAction.SettingsMenu:
                    legacySettingsMenuKey = parsedCode;
                    break;
            }
        }

        SaveBindings();
        IsRebinding = false;

        string displayName = GetKeyName(_rebindTargetAction);
        Debug.Log($"[KeyBindingManager] Berhasil mengubah {_rebindTargetAction} menjadi [{displayName}]");

        var cb = _onRebindResult;
        _onRebindResult = null;
        cb?.Invoke(true, displayName);
    }
}
