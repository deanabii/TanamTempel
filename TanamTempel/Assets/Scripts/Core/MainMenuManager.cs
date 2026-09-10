using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Masukkan GameObject Panel Credit ke sini")]
    public GameObject creditPanel;

    private void Start()
    {
        // Memastikan panel credit tidak aktif saat scene pertama kali dimuat
        if (creditPanel != null)
        {
            creditPanel.SetActive(false);
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