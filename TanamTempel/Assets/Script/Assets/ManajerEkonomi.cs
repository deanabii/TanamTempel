using UnityEngine;

public class ManajerEkonomi : MonoBehaviour
{
    // Singleton agar bisa dipanggil lewat: ManajerEkonomi.Instance
    public static ManajerEkonomi Instance;

    public int totalUang { get; private set; } = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TambahUang(int jumlah)
    {
        totalUang += jumlah;
        Debug.Log($"Uang bertambah! Total Uang sekarang: Rp{totalUang}");
        // Di sini Anda bisa memicu UI Uang untuk memperbarui teksnya
    }
}
