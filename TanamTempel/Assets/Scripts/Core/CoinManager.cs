using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Komponen penyimpan dan pengelola total koin pemain.
/// Menerapkan pola Singleton agar mudah diakses dari script panen manapun (CoinManager.Instance).
/// </summary>
public class CoinManager : MonoBehaviour
{
    private static CoinManager _instance;

    public static CoinManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CoinManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CoinManager");
                    _instance = go.AddComponent<CoinManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Pengaturan Koin Awal")]
    [SerializeField]
    [Tooltip("Jumlah koin awal pemain.")]
    private int currentCoins = 0;

    [Header("UI Binding (Opsional)")]
    [Tooltip("Komponen UI Text (Legacy) untuk menampilkan jumlah koin di layar.")]
    public Text coinText;

    [Tooltip("Komponen TextMeshProUGUI untuk menampilkan jumlah koin di layar (jika menggunakan TMP).")]
    public TMPro.TextMeshProUGUI coinTMP;

    /// <summary>
    /// Event yang dipicu setiap kali jumlah koin berubah.
    /// </summary>
    public event Action<int> OnCoinsChanged;

    /// <summary>
    /// Property untuk mengambil jumlah koin saat ini.
    /// </summary>
    public int CurrentCoins => currentCoins;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        UpdateUI();
    }

    private void Start()
    {
        UpdateUI();
    }

    /// <summary>
    /// Menambahkan koin ke saldo pemain.
    /// </summary>
    /// <param name="amount">Jumlah koin yang ditambahkan.</param>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        currentCoins += amount;
        Debug.Log($"[CoinManager] Mendapatkan +{amount} koin! Total koin saat ini: {currentCoins}");
        
        OnCoinsChanged?.Invoke(currentCoins);
        UpdateUI();
    }

    /// <summary>
    /// Mengurangi koin pemain (misal untuk membeli item/biji di toko).
    /// Mengembalikan true jika koin mencukupi.
    /// </summary>
    public bool UseCoins(int amount)
    {
        if (amount <= 0) return true;

        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            Debug.Log($"[CoinManager] Menggunakan {amount} koin. Sisa koin: {currentCoins}");

            OnCoinsChanged?.Invoke(currentCoins);
            UpdateUI();
            return true;
        }

        Debug.LogWarning($"[CoinManager] Koin tidak cukup! Butuh: {amount}, Dimiliki: {currentCoins}");
        return false;
    }

    /// <summary>
    /// Memperbarui tampilan UI koin jika komponen UI terhubung.
    /// </summary>
    public void UpdateUI()
    {
        if (coinText != null)
        {
            coinText.text = currentCoins.ToString();
        }

        if (coinTMP != null)
        {
            coinTMP.text = currentCoins.ToString();
        }
    }
}
