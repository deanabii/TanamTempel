using UnityEngine;

/// <summary>
/// Komponen yang ditempelkan pada objek Dekorasi di dunia game.
/// Bertanggung jawab melacak status penataan dekorasi dan memicu pemeriksaan Win Condition pada GameManager.
/// </summary>
public class DecorationItem : MonoBehaviour
{
    [Header("Data Item Toko")]
    [Tooltip("Aset ShopItemData yang sesuai dengan objek dekorasi ini.")]
    public ShopItemData shopData;

    [Header("Status Penataan")]
    [Tooltip("Status apakah dekorasi ini telah ditata / dipasang di lingkungan game.")]
    public bool isPlaced = true;

    private void Start()
    {
        // Beritahu GameManager bahwa dekorasi telah ada di arena
        NotifyGameManager();
    }

    private void OnEnable()
    {
        NotifyGameManager();
    }

    /// <summary>
    /// Dipanggil saat objek dekorasi diambil ke tangan pemain.
    /// </summary>
    public void OnGrabbed()
    {
        SetPlaced(false);
    }

    /// <summary>
    /// Dipanggil saat objek dekorasi diletakkan / dilepaskan oleh pemain di dunia game.
    /// </summary>
    public void OnDropped()
    {
        SetPlaced(true);
    }

    /// <summary>
    /// Mengubah status penataan dan memperbarui pemeriksaan kondisi menang.
    /// </summary>
    public void SetPlaced(bool placed)
    {
        isPlaced = placed;
        NotifyGameManager();
    }

    private void NotifyGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckWinCondition();
        }
    }
}
