using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Komponen pengendali utama alur dan status permainan (GameManager).
/// Menilai kondisi Menang (Win Condition: seluruh dekorasi dibeli & ditata -> UI Menang + Objek 3D Piala)
/// dan kondisi Kalah (Lose Condition: 0 Koin + 0 Biji/Benih + Pot kosong tanpa tanaman).
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Panel UI Kemenangan & Kekalahan")]
    [Tooltip("Panel Canvas UI saat pemain Menang (Selamat!).")]
    public GameObject winUI;

    [Tooltip("Panel Canvas UI saat pemain Kalah (Game Over).")]
    public GameObject loseUI;

    [Header("Objek Hadiah Kemenangan")]
    [Tooltip("Objek 3D Piala yang akan diaktifkan/dimunculkan saat pemain berhasil Menang.")]
    public GameObject trophy3DObject;

    [Header("Daftar Dekorasi Syarat Menang")]
    [Tooltip("Daftar Aset ShopItemData dekorasi yang wajib dibeli pemain untuk menang.")]
    public List<ShopItemData> requiredDecorations = new List<ShopItemData>();

    [Tooltip("Daftar komponen DecorationItem yang ada di scene (opsional, jika ingin melacak status penataan secara otomatis).")]
    public List<DecorationItem> sceneDecorations = new List<DecorationItem>();

    [Header("Pengaturan Evaluasi Status Game")]
    [Tooltip("Interval waktu otomatis (detik) untuk memeriksa apakah pemain mengalami kekalahan (softlock).")]
    public float loseCheckInterval = 3.0f;

    public bool IsGameOver { get; private set; } = false;
    public bool IsGameWon { get; private set; } = false;

    // Daftar panel custom UI yang sedang aktif/terbuka
    private HashSet<GameObject> _activeCustomPanels = new HashSet<GameObject>();

    /// <summary>
    /// Property Static Global untuk mengecek apakah ADA UI/Panel yang sedang terbuka.
    /// Mengembalikan true jika Toko terbuka, Game Over (Lose UI), Menang (Win UI), atau ada Panel Custom yang aktif.
    /// </summary>
    public static bool IsUIOpen
    {
        get
        {
            if (ShopUI.IsShopOpen) return true;
            if (_instance != null)
            {
                if (_instance.IsGameOver || _instance.IsGameWon) return true;
                if (_instance._activeCustomPanels.Count > 0) return true;
            }
            return false;
        }
    }

    // Melacak status UI pada frame sebelumnya
    private bool _wasUIOpen = false;

    private float _loseCheckTimer = 0f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (winUI != null) winUI.SetActive(false);
        if (loseUI != null) loseUI.SetActive(false);

        // Piala disembunyikan terlebih dahulu di awal
        if (trophy3DObject != null)
        {
            trophy3DObject.SetActive(false);
        }
    }

    private void Update()
    {
        bool currentUIOpen = IsUIOpen;

        // Deteksi perubahan status UI (Terbuka <-> Tertutup)
        if (currentUIOpen != _wasUIOpen)
        {
            _wasUIOpen = currentUIOpen;
            if (currentUIOpen)
            {
                OnUIOpened();
            }
            else
            {
                OnUIClosed();
            }
        }

        if (currentUIOpen)
        {
            // Pastikan kursor bebas & terlihat saat ada UI terbuka
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Nolkan input pergerakan & kamera pemain
            ZeroPlayerInputs();
        }

        if (IsGameOver || IsGameWon) return;

        // Pengecekan otomatis berkala untuk kondisi kalah
        _loseCheckTimer += Time.deltaTime;
        if (_loseCheckTimer >= loseCheckInterval)
        {
            _loseCheckTimer = 0f;
            CheckLoseCondition();
        }
    }

    private void OnUIOpened()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPlayerControllersEnabled(false);
    }

    private void OnUIClosed()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPlayerControllersEnabled(true);
    }

    /// <summary>
    /// Mengaktifkan atau menonaktifkan skrip penggerak pemain (FirstPersonController, ThirdPersonController, FPP_PlayerController) di scene.
    /// </summary>
    public void SetPlayerControllersEnabled(bool enable)
    {
        // 1. StarterAssets FirstPersonController
        StarterAssets.FirstPersonController[] fpcList = FindObjectsOfType<StarterAssets.FirstPersonController>();
        foreach (var fpc in fpcList)
        {
            if (fpc != null) fpc.enabled = enable;
        }

        // 2. StarterAssets ThirdPersonController
        StarterAssets.ThirdPersonController[] tpcList = FindObjectsOfType<StarterAssets.ThirdPersonController>();
        foreach (var tpc in tpcList)
        {
            if (tpc != null) tpc.enabled = enable;
        }

        // 3. FPP_PlayerController
        PlayerControllers.FPP_PlayerController[] customFppList = FindObjectsOfType<PlayerControllers.FPP_PlayerController>();
        foreach (var fpp in customFppList)
        {
            if (fpp != null) fpp.enabled = enable;
        }

        // 4. Update StarterAssetsInputs
        StarterAssets.StarterAssetsInputs[] starterInputs = FindObjectsOfType<StarterAssets.StarterAssetsInputs>();
        foreach (var inp in starterInputs)
        {
            if (inp != null)
            {
                inp.move = Vector2.zero;
                inp.look = Vector2.zero;
                inp.jump = false;
                inp.sprint = false;
                inp.cursorInputForLook = enable;
                inp.cursorLocked = enable;
            }
        }
    }

    /// <summary>
    /// Menolkan input StarterAssetsInputs saat UI terbuka.
    /// </summary>
    public void ZeroPlayerInputs()
    {
        StarterAssets.StarterAssetsInputs[] starterInputs = FindObjectsOfType<StarterAssets.StarterAssetsInputs>();
        foreach (var inp in starterInputs)
        {
            if (inp != null)
            {
                inp.move = Vector2.zero;
                inp.look = Vector2.zero;
                inp.jump = false;
                inp.sprint = false;
                inp.cursorInputForLook = false;
                inp.cursorLocked = false;
            }
        }
    }

    /// <summary>
    /// Membuka / Mengaktifkan panel UI custom dan mendaftarkannya ke GameManager.
    /// </summary>
    public void OpenPanel(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(true);
        if (!_activeCustomPanels.Contains(panel))
        {
            _activeCustomPanels.Add(panel);
        }
    }

    /// <summary>
    /// Menutup / Mendorong nonaktif panel UI custom.
    /// </summary>
    public void ClosePanel(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(false);
        if (_activeCustomPanels.Contains(panel))
        {
            _activeCustomPanels.Remove(panel);
        }
    }

    /// <summary>
    /// Memeriksa apakah syarat kemenangan telah terpenuhi:
    /// Semua dekorasi pada daftar requiredDecorations telah dibeli dan (jika ada) ditata di dunia game.
    /// </summary>
    public void CheckWinCondition()
    {
        if (IsGameOver || IsGameWon) return;

        // 1. Cek apakah ada dekorasi yang terdaftar di requiredDecorations
        bool allDecorationsPurchased = true;
        if (requiredDecorations != null && requiredDecorations.Count > 0)
        {
            foreach (var dec in requiredDecorations)
            {
                if (dec != null && !dec.isPurchased)
                {
                    allDecorationsPurchased = false;
                    break;
                }
            }
        }
        else
        {
            // Jika list kosong, cari semua ShopItemData Dekorasi dari objek yang ada
            DecorationItem[] foundDecs = FindObjectsOfType<DecorationItem>();
            if (foundDecs.Length == 0) return; // Belum ada dekorasi

            foreach (var dec in foundDecs)
            {
                if (dec.shopData != null && !dec.shopData.isPurchased)
                {
                    allDecorationsPurchased = false;
                    break;
                }
                if (!dec.isPlaced)
                {
                    allDecorationsPurchased = false;
                    break;
                }
            }
        }

        // 2. Cek apakah semua sceneDecorations telah ditata (isPlaced == true)
        bool allPlaced = true;
        if (sceneDecorations != null && sceneDecorations.Count > 0)
        {
            foreach (var item in sceneDecorations)
            {
                if (item != null && !item.isPlaced)
                {
                    allPlaced = false;
                    break;
                }
            }
        }

        // Jika semua dekorasi telah dibeli dan ditata -> TRIGGER MENANG!
        if (allDecorationsPurchased && allPlaced)
        {
            TriggerWin();
        }
    }

    /// <summary>
    /// Memeriksa apakah pemain kehabisan koin, benih, dan tanaman (Softlock -> Game Over).
    /// </summary>
    public void CheckLoseCondition()
    {
        if (IsGameOver || IsGameWon) return;

        // Condition 1: Uang / Koin = 0
        int coins = CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0;
        if (coins > 0) return; // Pemain masih punya koin, belum kalah

        // Condition 2: Tidak ada Biji / Benih tersisa di scene
        Biji[] seedsInScene = FindObjectsOfType<Biji>();
        if (seedsInScene != null && seedsInScene.Length > 0) return; // Masih ada biji di dunia game, belum kalah

        // Condition 3: Tidak ada Pot yang sedang terisi tanaman (tumbuh atau siap panen)
        Pot[] potsInScene = FindObjectsOfType<Pot>();
        if (potsInScene != null && potsInScene.Length > 0)
        {
            foreach (var pot in potsInScene)
            {
                if (pot != null && pot.isPlanted)
                {
                    return; // Masih ada tanaman di pot yang sedang tumbuh/bisa dipanen, belum kalah
                }
            }
        }

        // Jika Koin = 0, Biji = 0, dan Pot Kosong -> TRIGGER KALAH!
        TriggerLose();
    }

    /// <summary>
    /// Memicu alur Kemenangan.
    /// </summary>
    public void TriggerWin()
    {
        if (IsGameWon) return;
        IsGameWon = true;

        Debug.Log("[GameManager] SELAMAT! Anda Menang! Semua Dekorasi Telah Dibeli dan Ditata.");

        // Aktifkan Hadiah Objek 3D Piala
        if (trophy3DObject != null)
        {
            trophy3DObject.SetActive(true);
        }

        // Munculkan UI Menang
        if (winUI != null)
        {
            winUI.SetActive(true);
        }

        // Lepas kursor mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Memicu alur Kekalahan.
    /// </summary>
    public void TriggerLose()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Debug.Log("[GameManager] GAME OVER! Koin = 0, Tidak ada Benih, dan Pot Kosong.");

        // Munculkan UI Kalah
        if (loseUI != null)
        {
            loseUI.SetActive(true);
        }

        // Lepas kursor mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
