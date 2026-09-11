using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Mengontrol aktivasi daftar GameObject secara berurutan (sekuensial), misalnya untuk
/// pop-up tutorial, slide panduan bertahap, cutscene intro, atau panel instruksi saat scene dibuka.
/// 
/// Fitur:
/// 1. Otomatis mengaktifkan objek pertama dari list saat scene dibuka.
/// 2. Menekan tombol sembarang di keyboard atau klik mouse / layar akan memajukan ke objek berikutnya.
/// 3. Objek sebelumnya otomatis dimatikan (SetActive(false)), dan objek baru dinyalakan (SetActive(true)) bergantian.
/// 4. Saat mencapai objek terakhir, penekanan tombol / klik akan langsung menutup objek terakhir tersebut.
/// 5. Mendukung lebih dari 2 GameObject dengan kapasitas list yang fleksibel di Inspector.
/// 6. Kompatibel dengan New Input System maupun Legacy Input.
/// 7. Terintegrasi dengan GameManager (opsional) untuk membebaskan kursor dan menahan pergerakan player selama urutan aktif.
/// </summary>
public class GameObjectSequenceController : MonoBehaviour
{
    [Header("Daftar Objek Berurutan (List)")]
    [Tooltip("Daftar GameObject yang akan dibuka dan ditutup secara berurutan satu per satu.")]
    public List<GameObject> targetObjects = new List<GameObject>();

    [Header("Pengaturan Otomatis")]
    [Tooltip("Apakah urutan objek langsung otomatis dimulai begitu scene dibuka.")]
    public bool autoStartOnSceneOpen = true;

    [Tooltip("Jeda waktu awal (detik) sebelum menerima input saat scene baru dibuka (mencegah klik ganda yang tidak disengaja dari scene sebelumnya).")]
    public float initialInputDelay = 0.25f;

    [Tooltip("Jeda minimal antar input (detik) agar tidak terlewat jika pemain menekan tombol secara beruntun.")]
    public float inputCooldown = 0.2f;

    [Header("Pengaturan Input")]
    [Tooltip("Apakah penekanan tombol apa saja di keyboard dapat memajukan urutan objek.")]
    public bool allowAnyKey = true;

    [Tooltip("Apakah klik mouse (kiri/kanan) atau sentuhan layar dapat memajukan urutan objek.")]
    public bool allowMouseOrTouchClick = true;

    [Tooltip("Jika aktif, tombol Escape akan diabaikan (tidak memajukan urutan) agar tombol Escape tetap bisa digunakan untuk Pause/Settings.")]
    public bool ignoreEscapeKey = true;

    [Header("Integrasi Game & Kontrol (Opsional)")]
    [Tooltip("Jika aktif dan terdapat GameManager, kursor mouse akan dibebaskan dan kontrol pemain ditahan selama urutan aktif.")]
    public bool integrateWithGameManager = true;

    [Tooltip("Apakah menghentikan waktu game (Time.timeScale = 0) saat urutan objek sedang aktif.")]
    public bool pauseTimeWhileActive = false;

    [Tooltip("Apakah menghancurkan GameObject controller ini setelah seluruh urutan selesai ditampilkan.")]
    public bool destroyOnFinish = false;

    [Header("Events (Opsional)")]
    [Tooltip("Event yang dipicu saat urutan dimulai.")]
    public UnityEvent onSequenceStarted;

    [Tooltip("Event yang dipicu setiap kali langkah berganti, mengirimkan indeks objek yang aktif saat ini.")]
    public UnityEvent<int> onStepChanged;

    [Tooltip("Event yang dipicu saat seluruh urutan selesai dan objek terakhir ditutup.")]
    public UnityEvent onSequenceFinished;

    private int _currentIndex = -1;
    private bool _isSequenceActive = false;
    private float _lastInputTime = 0f;
    private float _delayTimer = 0f;

    /// <summary>
    /// Apakah urutan objek sedang aktif berjalan saat ini.
    /// </summary>
    public bool IsActive => _isSequenceActive;

    /// <summary>
    /// Indeks objek yang sedang aktif saat ini (-1 jika tidak ada yang aktif).
    /// </summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>
    /// Total jumlah objek yang ada di dalam list.
    /// </summary>
    public int TotalCount => targetObjects != null ? targetObjects.Count : 0;

    private void Start()
    {
        if (autoStartOnSceneOpen)
        {
            StartSequence();
        }
    }

    private void Update()
    {
        if (!_isSequenceActive) return;

        // Jeda pengaman di awal scene
        if (_delayTimer > 0f)
        {
            _delayTimer -= Time.unscaledDeltaTime;
            return;
        }

        // Cek cooldown input
        if (Time.unscaledTime - _lastInputTime < inputCooldown) return;

        // Cek apakah ada input tombol keyboard atau klik mouse/layar
        if (IsInputTriggered())
        {
            _lastInputTime = Time.unscaledTime;
            Next();
        }
    }

    /// <summary>
    /// Memulai urutan dari objek pertama (indeks 0).
    /// </summary>
    public void StartSequence()
    {
        if (targetObjects == null || targetObjects.Count == 0)
        {
            Debug.LogWarning("[GameObjectSequenceController] Daftar targetObjects kosong! Tambahkan objek di Inspector.");
            return;
        }

        // Matikan seluruh objek terlebih dahulu untuk memastikan kondisi bersih
        for (int i = 0; i < targetObjects.Count; i++)
        {
            if (targetObjects[i] != null)
            {
                targetObjects[i].SetActive(false);
            }
        }

        _currentIndex = 0;

        // Lewati jika elemen pertama kebetulan bernilai null
        while (_currentIndex < targetObjects.Count && targetObjects[_currentIndex] == null)
        {
            _currentIndex++;
        }

        if (_currentIndex >= targetObjects.Count)
        {
            Debug.LogWarning("[GameObjectSequenceController] Seluruh item dalam list bernilai null!");
            return;
        }

        _isSequenceActive = true;
        _delayTimer = initialInputDelay;
        _lastInputTime = Time.unscaledTime;

        if (pauseTimeWhileActive)
        {
            Time.timeScale = 0f;
        }

        // Aktifkan objek pertama
        ActivateCurrentObject();

        onSequenceStarted?.Invoke();
        onStepChanged?.Invoke(_currentIndex);
    }

    /// <summary>
    /// Berpindah ke objek berikutnya dalam list dan mematikan objek saat ini.
    /// Jika sudah di objek terakhir, seluruh urutan akan ditutup.
    /// Fungsi ini juga dapat dipanggil lewat UI Button (OnClick).
    /// </summary>
    public void Next()
    {
        if (!_isSequenceActive) return;

        // 1. Matikan objek yang sedang aktif sekarang
        DeactivateCurrentObject();

        AudioGame.Instance?.PlayButtonClick();

        // 2. Geser ke indeks berikutnya
        _currentIndex++;

        // Lewati slot kosong/null jika ada di Inspector
        while (_currentIndex < targetObjects.Count && targetObjects[_currentIndex] == null)
        {
            _currentIndex++;
        }

        // 3. Jika masih ada objek di list, nyalakan
        if (_currentIndex < targetObjects.Count)
        {
            ActivateCurrentObject();
            onStepChanged?.Invoke(_currentIndex);
        }
        else
        {
            // 4. Jika sudah melewati objek terakhir, langsung tutup dan selesaikan
            FinishSequence();
        }
    }

    /// <summary>
    /// Kembali ke objek sebelumnya dalam list (opsional jika dibutuhkan tombol Back).
    /// </summary>
    public void Previous()
    {
        if (!_isSequenceActive || _currentIndex <= 0) return;

        DeactivateCurrentObject();
        AudioGame.Instance?.PlayButtonClick();

        _currentIndex--;
        while (_currentIndex >= 0 && targetObjects[_currentIndex] == null)
        {
            _currentIndex--;
        }

        if (_currentIndex >= 0)
        {
            ActivateCurrentObject();
            onStepChanged?.Invoke(_currentIndex);
        }
    }

    /// <summary>
    /// Menutup seluruh objek seketika dan mengakhiri urutan.
    /// </summary>
    public void CloseAll()
    {
        AudioGame.Instance?.PlayButtonClick();
        FinishSequence();
    }

    /// <summary>
    /// Melompat langsung ke indeks objek tertentu.
    /// </summary>
    public void JumpTo(int index)
    {
        if (targetObjects == null || index < 0 || index >= targetObjects.Count) return;

        DeactivateCurrentObject();
        _currentIndex = index;
        _isSequenceActive = true;
        ActivateCurrentObject();
        onStepChanged?.Invoke(_currentIndex);
    }

    private void ActivateCurrentObject()
    {
        if (_currentIndex >= 0 && _currentIndex < targetObjects.Count)
        {
            GameObject currentObj = targetObjects[_currentIndex];
            if (currentObj != null)
            {
                if (integrateWithGameManager && GameManager.Instance != null)
                {
                    GameManager.Instance.OpenPanel(currentObj);
                }
                else
                {
                    currentObj.SetActive(true);
                }
            }
        }
    }

    private void DeactivateCurrentObject()
    {
        if (_currentIndex >= 0 && _currentIndex < targetObjects.Count)
        {
            GameObject currentObj = targetObjects[_currentIndex];
            if (currentObj != null)
            {
                if (integrateWithGameManager && GameManager.Instance != null)
                {
                    GameManager.Instance.ClosePanel(currentObj);
                }
                else
                {
                    currentObj.SetActive(false);
                }
            }
        }
    }

    private void FinishSequence()
    {
        _isSequenceActive = false;

        // Pastikan seluruh objek di list dimatikan
        if (targetObjects != null)
        {
            foreach (var obj in targetObjects)
            {
                if (obj != null)
                {
                    if (integrateWithGameManager && GameManager.Instance != null)
                    {
                        GameManager.Instance.ClosePanel(obj);
                    }
                    else
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }

        if (pauseTimeWhileActive)
        {
            Time.timeScale = 1f;
        }

        onSequenceFinished?.Invoke();
        Debug.Log("[GameObjectSequenceController] Urutan objek telah selesai ditampilkan dan ditutup.");

        if (destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }

    private bool IsInputTriggered()
    {
#if ENABLE_INPUT_SYSTEM
        // 1. New Input System: Keyboard
        if (allowAnyKey && Keyboard.current != null)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                if (ignoreEscapeKey && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    return false;
                }
                return true;
            }
        }

        // 2. New Input System: Mouse & Touch
        if (allowMouseOrTouchClick)
        {
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
                {
                    return true;
                }
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))
            {
                return true;
            }
        }
#else
        // 3. Legacy Input Fallback
        if (allowAnyKey && Input.anyKeyDown)
        {
            if (ignoreEscapeKey && Input.GetKeyDown(KeyCode.Escape))
            {
                return false;
            }
            return true;
        }

        if (allowMouseOrTouchClick)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                return true;
            }
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                return true;
            }
        }
#endif
        return false;
    }

    private void OnDisable()
    {
        if (pauseTimeWhileActive)
        {
            Time.timeScale = 1f;
        }
    }
}
