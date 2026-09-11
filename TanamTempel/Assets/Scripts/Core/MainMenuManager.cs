using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Masukkan GameObject Panel Credit ke sini")]
    public GameObject creditPanel;

    [Tooltip("Masukkan GameObject Panel Pengaturan / Settings ke sini (opsional)")]
    public GameObject settingsPanel;

    private void Start()
    {
        // Memastikan panel credit & settings tidak aktif saat scene pertama kali dimuat
        if (creditPanel != null)
        {
            creditPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Dipanggil saat tombol MAIN ditekan.
    /// Pastikan nama scene yang dituju sudah masuk di Build Settings.
    /// </summary>
    public void LoadMainScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Dipanggil saat tombol CREDIT ditekan.
    /// </summary>
    public void ShowCreditPanel()
    {
        if (creditPanel != null)
        {
            creditPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Credit Panel belum dimasukkan ke dalam Inspector!");
        }
    }

    /// <summary>
    /// Fungsi tambahan untuk tombol 'Tutup/Back' di dalam panel credit.
    /// </summary>
    public void HideCreditPanel()
    {
        if (creditPanel != null)
        {
            creditPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Dipanggil saat tombol PENGATURAN / SETTINGS ditekan.
    /// </summary>
    public void ShowSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else if (SettingsMenuUI.Instance != null)
        {
            SettingsMenuUI.Instance.OpenSettings();
        }
    }

    /// <summary>
    /// Menutup panel pengaturan di Main Menu.
    /// </summary>
    public void HideSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        else if (SettingsMenuUI.Instance != null)
        {
            SettingsMenuUI.Instance.CloseSettings();
        }
    }

    /// <summary>
    /// Dipanggil saat tombol EXIT ditekan.
    /// </summary>
    public void QuitGame()
    {
        // Pesan ini hanya akan muncul di console Unity Editor untuk simulasi
        Debug.Log("Keluar dari Game!"); 
        
        // Perintah ini akan menutup aplikasi saat sudah di-build
        Application.Quit();
    }

}