using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Pengendali terpusat sistem Audio & SFX (AudioGame).
/// Mengelola pemutaran efek suara untuk seluruh interaksi gameplay (Grab, Drop, Tanam, Siram, Isi Air, Panen, Pasang Pot)
/// serta penekanan/klik tombol UI (Button Click).
/// 
/// Menyediakan input slot AudioSource di Inspector dan mengizinkan kustomisasi klip suara per aksi,
/// dengan fallback cerdas ke buttonClickSound jika klip spesifik belum diisi.
/// </summary>
public class AudioGame : MonoBehaviour
{
    private static AudioGame _instance;

    /// <summary>
    /// Akses Singleton global ke AudioGame.
    /// </summary>
    public static AudioGame Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioGame>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioGame");
                    _instance = go.AddComponent<AudioGame>();
                }
            }
            return _instance;
        }
    }

    [Header("Input Audio Source")]
    [Tooltip("Komponen AudioSource utama tempat efek suara diputar. Input AudioSource Anda di sini.")]
    public AudioSource audioSource;

    [Header("Pengaturan Volume & Variasi")]
    [Range(0f, 1f)]
    [Tooltip("Volume global untuk seluruh efek suara.")]
    public float sfxVolume = 1f;

    [Tooltip("Apakah menambahkan sedikit variasi pitch (nada) acak agar efek suara terdengar alami dan tidak monoton.")]
    public bool randomizePitch = true;

    [Range(0.8f, 1.2f)]
    public float minPitch = 0.95f;

    [Range(0.8f, 1.2f)]
    public float maxPitch = 1.05f;

    [Header("Klip Suara UI & Tombol")]
    [Tooltip("Suara saat tombol UI diklik (Button Click).")]
    public AudioClip buttonClickSound;

    [Header("Klip Suara Interaksi Gameplay")]
    [Tooltip("Suara interaksi umum (fallback jika klip spesifik kosong).")]
    public AudioClip interactSound;

    [Tooltip("Suara saat mengambil/memegang objek (Grab Pot/Biji/Gayung).")]
    public AudioClip grabSound;

    [Tooltip("Suara saat melepaskan atau menjatuhkan objek (Drop).")]
    public AudioClip dropSound;

    [Tooltip("Suara saat menanam benih ke dalam pot (Plant).")]
    public AudioClip plantSound;

    [Tooltip("Suara saat menyiram tanaman dengan gayung (Water).")]
    public AudioClip waterSound;

    [Tooltip("Suara saat mengisi air gayung dari tong air (Refill Water).")]
    public AudioClip refillWaterSound;

    [Tooltip("Suara saat memanen tanaman (Harvest).")]
    public AudioClip harvestSound;

    [Tooltip("Suara saat menempelkan pot ke dinding grid (Place Pot).")]
    public AudioClip placePotSound;

    [Tooltip("Suara saat membuka Toko (Shop Open).")]
    public AudioClip shopOpenSound;

    [Tooltip("Suara saat berhasil membeli item di Toko (Shop Buy).")]
    public AudioClip shopBuySound;

    [Tooltip("Suara gagal / koin tidak cukup / error.")]
    public AudioClip errorSound;

    [Header("Otomatis Hook Tombol UI")]
    [Tooltip("Jika aktif, seluruh tombol Button UI di scene otomatis dipasangi suara klik.")]
    public bool autoHookAllButtons = true;

    private HashSet<Button> _hookedButtons = new HashSet<Button>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Jika audioSource belum dimasukkan di Inspector, cari atau buat otomatis
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Konfigurasi dasar AudioSource untuk SFX 2D
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D (terdengar jelas merata)
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (autoHookAllButtons)
        {
            HookAllButtonsInScene();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (autoHookAllButtons)
        {
            _hookedButtons.Clear();
            HookAllButtonsInScene();
        }
    }

    /// <summary>
    /// Mencari seluruh UI Button di scene yang aktif dan menambahkan listener suara klik otomatis.
    /// </summary>
    public void HookAllButtonsInScene()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (var btn in buttons)
        {
            HookButton(btn);
        }
    }

    /// <summary>
    /// Memasang listener suara klik pada UI Button tertentu.
    /// </summary>
    public void HookButton(Button btn)
    {
        if (btn == null || _hookedButtons.Contains(btn)) return;

        btn.onClick.AddListener(PlayButtonClick);
        _hookedButtons.Add(btn);
    }

    // =========================================================================
    // FUNGSI PEMUTAR SUARA SFX
    // =========================================================================

    /// <summary>
    /// Memutar suara saat tombol UI diklik.
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySound(GetValidClip(buttonClickSound));
    }

    /// <summary>
    /// Memutar suara interaksi umum.
    /// </summary>
    public void PlayInteract()
    {
        PlaySound(GetValidClip(interactSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara saat mengambil objek.
    /// </summary>
    public void PlayGrab()
    {
        PlaySound(GetValidClip(grabSound, interactSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara saat melepaskan / drop objek.
    /// </summary>
    public void PlayDrop()
    {
        PlaySound(GetValidClip(dropSound, interactSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara saat menanam benih ke dalam pot.
    /// </summary>
    public void PlayPlant()
    {
        PlaySound(GetValidClip(plantSound, interactSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara saat menyiram tanaman dengan gayung.
    /// </summary>
    public void PlayWater()
    {
        PlaySound(GetValidClip(waterSound, interactSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara saat mengisi air gayung di tong air.
    /// </summary>
    public void PlayRefillWater()
    {
        PlaySound(GetValidClip(refillWaterSound, waterSound, interactSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara saat memanen tanaman.
    /// </summary>
    public void PlayHarvest()
    {
        PlaySound(GetValidClip(harvestSound, shopBuySound, interactSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara saat menempelkan pot ke grid dinding.
    /// </summary>
    public void PlayPlacePot()
    {
        PlaySound(GetValidClip(placePotSound, grabSound, interactSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara saat membuka toko.
    /// </summary>
    public void PlayShopOpen()
    {
        PlaySound(GetValidClip(shopOpenSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara saat berhasil membeli item di toko.
    /// </summary>
    public void PlayShopBuy()
    {
        PlaySound(GetValidClip(shopBuySound, buttonClickSound));
    }

    /// <summary>
    /// Memutar suara gagal / error / koin tidak cukup.
    /// </summary>
    public void PlayError()
    {
        PlaySound(GetValidClip(errorSound, buttonClickSound));
    }

    /// <summary>
    /// Memutar klip audio tertentu melalui AudioSource dengan volume dan variasi pitch.
    /// </summary>
    public void PlaySound(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null || audioSource == null) return;

        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        audioSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
    }

    /// <summary>
    /// Mencari klip pertama yang tidak null dari daftar kandidat fallback.
    /// Memastikan suara tetap berbunyi walaupun user hanya mengisi satu jenis suara di Inspector.
    /// </summary>
    private AudioClip GetValidClip(params AudioClip[] candidates)
    {
        if (candidates == null) return null;
        foreach (var clip in candidates)
        {
            if (clip != null) return clip;
        }
        return null;
    }
}
