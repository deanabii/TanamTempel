using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Komponen UI standalone untuk Kartu Item di Toko Pembelian.
/// Ditempelkan pada prefab kartu item UI (ShopItemCardPrefab).
/// Komponen TextMeshPro (TMP), Image Sprite, dan Button diserialisasi di Inspector.
/// </summary>
public class ShopItemCard : MonoBehaviour
{
    [Header("Referensi TextMeshPro (TMP)")]
    [Tooltip("Komponen TextMeshProUGUI untuk menampilkan nama item.")]
    public TextMeshProUGUI itemNameTMP;

    [Tooltip("Komponen TextMeshProUGUI untuk menampilkan harga item.")]
    public TextMeshProUGUI itemPriceTMP;

    [Tooltip("Komponen TextMeshProUGUI untuk deskripsi item (opsional).")]
    public TextMeshProUGUI itemDescriptionTMP;

    [Header("Referensi Gambar / Sprite")]
    [Tooltip("Komponen Image untuk menampilkan ikon/sprite item.")]
    public Image itemIconImage;

    [Header("Referensi Kontrol UI")]
    [Tooltip("Komponen Button untuk tombol Beli.")]
    public Button buyButton;

    [Tooltip("Komponen TextMeshProUGUI untuk label teks pada tombol beli (misal 'Beli' atau 'Dibeli').")]
    public TextMeshProUGUI buyButtonTextTMP;

    [Header("Pengaturan Teks Tambahan")]
    [Tooltip("Format teks harga item di UI (default: '{0} Koin').")]
    public string priceFormat = "{0} Koin";

    [Tooltip("Teks label tombol jika item dekorasi sudah dibeli.")]
    public string purchasedText = "Dibeli";

    [Tooltip("Teks label tombol default.")]
    public string defaultBuyText = "Beli";

    private ShopItemData _currentItemData;
    private Action<ShopItemData> _onBuyAction;

    /// <summary>
    /// Mengkonfigurasi data item dan listener tombol beli pada kartu UI ini.
    /// </summary>
    /// <param name="data">Data item dari ScriptableObject (ShopItemData).</param>
    /// <param name="onBuyAction">Action callback yang dipanggil saat tombol Beli diklik.</param>
    public void SetupCard(ShopItemData data, Action<ShopItemData> onBuyAction)
    {
        _currentItemData = data;
        _onBuyAction = onBuyAction;

        RefreshCardUI();
    }

    /// <summary>
    /// Memperbarui tampilan UI kartu (nama, harga, sprite, dan status tombol beli) 
    /// secara independen tanpa perlu me-reinstantiate objek UI.
    /// </summary>
    public void RefreshCardUI()
    {
        if (_currentItemData == null) return;

        // Auto-assign referensi komponen jika belum diisi di Inspector
        if (buyButton == null)
        {
            buyButton = GetComponentInChildren<Button>(true);
        }

        if (itemNameTMP == null)
        {
            itemNameTMP = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (itemNameTMP == null) itemNameTMP = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        }

        if (itemPriceTMP == null)
        {
            itemPriceTMP = transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
            if (itemPriceTMP == null) itemPriceTMP = transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
        }

        if (itemIconImage == null)
        {
            itemIconImage = transform.Find("IconImage")?.GetComponent<Image>();
            if (itemIconImage == null) itemIconImage = transform.Find("Icon")?.GetComponent<Image>();
        }

        if (buyButtonTextTMP == null && buyButton != null)
        {
            buyButtonTextTMP = buyButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Set Nama Item (TMP)
        if (itemNameTMP != null)
        {
            itemNameTMP.text = _currentItemData.itemName;
        }

        // Set Harga Item (TMP)
        if (itemPriceTMP != null)
        {
            itemPriceTMP.text = string.Format(priceFormat, _currentItemData.price);
        }

        // Set Deskripsi Item (TMP opsional)
        if (itemDescriptionTMP != null)
        {
            itemDescriptionTMP.text = _currentItemData.itemDescription;
        }

        // Set Icon Sprite Item
        if (itemIconImage != null)
        {
            if (_currentItemData.itemIcon != null)
            {
                itemIconImage.sprite = _currentItemData.itemIcon;
                itemIconImage.gameObject.SetActive(true);
            }
            else
            {
                itemIconImage.gameObject.SetActive(false);
            }
        }

        // Configure Tombol Beli & Callback
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();

            if (_currentItemData.category == ShopCategory.Dekorasi && _currentItemData.isPurchased)
            {
                buyButton.interactable = false;
                if (buyButtonTextTMP != null)
                {
                    buyButtonTextTMP.text = purchasedText;
                }
            }
            else
            {
                buyButton.interactable = true;
                if (buyButtonTextTMP != null)
                {
                    buyButtonTextTMP.text = defaultBuyText;
                }

                buyButton.onClick.AddListener(OnBuyButtonClicked);
            }
        }
    }

    private void OnBuyButtonClicked()
    {
        if (_currentItemData != null && _onBuyAction != null)
        {
            _onBuyAction.Invoke(_currentItemData);
            // Perbarui status UI kartu secara langsung setelah dibeli
            RefreshCardUI();
        }
    }
}
