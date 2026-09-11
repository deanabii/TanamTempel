using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pengendali UI Toko Pembelian.
/// Mengatur pergantian Tab antara kategori Tanam dan Dekorasi,
/// memotong koin pemain, me-spawn item yang dibeli di dunia 3D,
/// serta mengontrol status Kursor Mouse dan jeda pergerakan pemain saat toko terbuka.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static bool IsShopOpen { get; private set; } = false;

    [Header("Panel Utama Toko")]
    [Tooltip("Objek Root Panel Toko (Diaktifkan saat toko dibuka).")]
    public GameObject shopPanel;

    [Header("Tab Kategori")]
    [Tooltip("Panel konten untuk kategori 'Tanam' (Pot & Benih).")]
    public GameObject tanamTabPanel;

    [Tooltip("Panel konten untuk kategori 'Dekorasi'.")]
    public GameObject dekorasiTabPanel;

    [Header("Container Kartu Item")]
    [Tooltip("Transform parent (misal Grid Layout Group) tempat kartu item kategori Tanam dimunculkan.")]
    public Transform tanamContainer;

    [Tooltip("Transform parent tempat kartu item kategori Dekorasi dimunculkan.")]
    public Transform dekorasiContainer;

    [Header("Prefab Kartu UI Item")]
    [Tooltip("Prefab UI Kartu Item (berisi Text Nama, Text Harga, Image Icon, dan Button Beli).")]
    public GameObject shopItemCardPrefab;

    [Header("Daftar Item Toko")]
    [Tooltip("Daftar aset ScriptableObject ShopItemData yang akan dijual di Toko.")]
    public List<ShopItemData> availableItems = new List<ShopItemData>();

    [Header("Shortcut Global (Opsional)")]
    [Tooltip("Apakah toko bisa dibuka/ditutup dari mana saja dengan tombol pintas keyboard.")]
    public bool enableGlobalHotkey = false;

#if ENABLE_INPUT_SYSTEM
    [Tooltip("Tombol shortcut global untuk membuka/menutup toko (misal: B).")]
    public UnityEngine.InputSystem.Key globalShopKey = UnityEngine.InputSystem.Key.B;
#endif
    [Tooltip("Tombol shortcut global legacy (default: B).")]
    public KeyCode legacyGlobalShopKey = KeyCode.B;

    private ShopArea _currentShopArea;

    private void Awake()
    {
        IsShopOpen = false;
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (IsShopOpen)
        {
            // Pastikan kursor mouse terlepas dan terlihat untuk interaksi UI toko
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Tahan dan nolkan input pergerakan keyboard & rotasi kamera mouse
            DisablePlayerInputs();

            // Tutup toko jika menekan tombol Escape atau tombol toko saat toko sedang terbuka
            if (IsCloseInputPressed())
            {
                CloseShop();
            }
            return;
        }

        // Jika fitur shortcut global diaktifkan pada ShopUI
        if (enableGlobalHotkey && IsGlobalShopKeyPressed())
        {
            OpenShop();
        }
    }

    private bool IsCloseInputPressed()
    {
        if (KeyBindingManager.Instance != null)
        {
            if (KeyBindingManager.Instance.IsMenuPressed() || KeyBindingManager.Instance.IsShopPressed())
            {
                return true;
            }
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) return true;

            if (_currentShopArea != null)
            {
                var shopKeyControl = UnityEngine.InputSystem.Keyboard.current[_currentShopArea.shopKey];
                if (shopKeyControl != null && shopKeyControl.wasPressedThisFrame) return true;
            }
            else if (enableGlobalHotkey)
            {
                var globalKeyControl = UnityEngine.InputSystem.Keyboard.current[globalShopKey];
                if (globalKeyControl != null && globalKeyControl.wasPressedThisFrame) return true;
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape)) return true;
        if (_currentShopArea != null && Input.GetKeyDown(_currentShopArea.legacyShopKey)) return true;
        if (enableGlobalHotkey && Input.GetKeyDown(legacyGlobalShopKey)) return true;
#endif
        return false;
    }

    private bool IsGlobalShopKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var globalKeyControl = UnityEngine.InputSystem.Keyboard.current[globalShopKey];
            if (globalKeyControl != null && globalKeyControl.wasPressedThisFrame) return true;
        }
#else
        if (Input.GetKeyDown(legacyGlobalShopKey)) return true;
#endif
        return false;
    }

    /// <summary>
    /// Membuka tampilan UI Toko.
    /// </summary>
    public void OpenShop(ShopArea shopArea = null)
    {
        _currentShopArea = shopArea;
        IsShopOpen = true;

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        // Lepas kursor mouse agar pemain dapat mengeklik tombol UI toko
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisablePlayerInputs();

        // Buka Tab Tanam secara default
        SelectTanamTab();

        // Refresh kartu item
        PopulateItems();
    }

    /// <summary>
    /// Menutup tampilan UI Toko dan mengunci kursor kembali ke kamera.
    /// </summary>
    public void CloseShop()
    {
        IsShopOpen = false;

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        // Kunci kursor mouse kembali jika tidak ada UI lain yang terbuka
        if (!GameManager.IsUIOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            EnablePlayerInputs();
        }
    }

    private void DisablePlayerInputs()
    {
        // StarterAssetsInputs (Unity Starter Assets)
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

    private void EnablePlayerInputs()
    {
        // Pulihkan StarterAssetsInputs
        StarterAssets.StarterAssetsInputs[] starterInputs = FindObjectsOfType<StarterAssets.StarterAssetsInputs>();
        foreach (var inp in starterInputs)
        {
            if (inp != null)
            {
                inp.cursorInputForLook = true;
                inp.cursorLocked = true;
            }
        }
    }

    /// <summary>
    /// Memilih Tab kategori 'Tanam'.
    /// </summary>
    public void SelectTanamTab()
    {
        tanamTabPanel.SetActive(true);
        dekorasiTabPanel.SetActive(false);
    }

    /// <summary>
    /// Memilih Tab kategori 'Dekorasi'.
    /// </summary>
    public void SelectDekorasiTab()
    {
        tanamTabPanel.SetActive(false);
        dekorasiTabPanel.SetActive(true);
    }

    /// <summary>
    /// Membuat ulang daftar kartu item pada kedua tab.
    /// </summary>
    public void PopulateItems()
    {
        if (availableItems == null) return;

        // Bersihkan container lama jika ada
        if (tanamContainer != null)
        {
            foreach (Transform child in tanamContainer) Destroy(child.gameObject);
        }

        if (dekorasiContainer != null)
        {
            foreach (Transform child in dekorasiContainer) Destroy(child.gameObject);
        }

        // Buat kartu item untuk setiap data yang sesuai
        foreach (var item in availableItems)
        {
            if (item == null) continue;

            Transform targetContainer = (item.category == ShopCategory.Tanam) ? tanamContainer : dekorasiContainer;
            if (targetContainer == null) continue;

            CreateItemCard(item, targetContainer);
        }
    }

    private void CreateItemCard(ShopItemData item, Transform container)
    {
        if (shopItemCardPrefab != null && container != null)
        {
            // PENTING: Gunakan instantiateInWorldSpace = false agar transform/scale UI RectTransform 
            // di-instantiate sesuai dengan container tanpa mengubah world position/scale
            GameObject cardObj = Instantiate(shopItemCardPrefab, container, false);
            
            // Pastikan localScale bernilai (1, 1, 1) dan posisi Z bernilai 0 agar GraphicRaycaster dapat mendeteksi klik
            cardObj.transform.localScale = Vector3.one;
            Vector3 localPos = cardObj.transform.localPosition;
            localPos.z = 0f;
            cardObj.transform.localPosition = localPos;
            cardObj.transform.localRotation = Quaternion.identity;
            
            // Coba menggunakan komponen ShopItemCard yang menempel pada prefab (baik di root atau anak objek)
            ShopItemCard cardScript = cardObj.GetComponent<ShopItemCard>();
            if (cardScript == null)
            {
                cardScript = cardObj.GetComponentInChildren<ShopItemCard>(true);
            }

            if (cardScript != null)
            {
                cardScript.SetupCard(item, BuyItem);
            }
            else
            {
                // Fallback pencarian komponen otomatis (UI Legacy / TextMeshPro)
                TMPro.TextMeshProUGUI nameTMP = cardObj.transform.Find("NameText")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (nameTMP == null) nameTMP = cardObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (nameTMP != null) nameTMP.text = item.itemName;
                else
                {
                    Text nameTxt = cardObj.transform.Find("NameText")?.GetComponent<Text>();
                    if (nameTxt == null) nameTxt = cardObj.GetComponentInChildren<Text>();
                    if (nameTxt != null) nameTxt.text = item.itemName;
                }

                TMPro.TextMeshProUGUI priceTMP = cardObj.transform.Find("PriceText")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (priceTMP != null) priceTMP.text = $"{item.price} Koin";
                else
                {
                    Text priceTxt = cardObj.transform.Find("PriceText")?.GetComponent<Text>();
                    if (priceTxt != null) priceTxt.text = $"{item.price} Koin";
                }

                Image iconImg = cardObj.transform.Find("IconImage")?.GetComponent<Image>();
                if (iconImg == null) iconImg = cardObj.GetComponentInChildren<Image>();
                if (iconImg != null && item.itemIcon != null) iconImg.sprite = item.itemIcon;

                Button buyBtn = cardObj.transform.Find("BuyButton")?.GetComponent<Button>();
                if (buyBtn == null) buyBtn = cardObj.GetComponentInChildren<Button>();
                if (buyBtn != null)
                {
                    buyBtn.onClick.RemoveAllListeners();
                    if (item.category == ShopCategory.Dekorasi && item.isPurchased)
                    {
                        buyBtn.interactable = false;
                        TMPro.TextMeshProUGUI buyBtnTMP = buyBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                        if (buyBtnTMP != null) buyBtnTMP.text = "Dibeli";
                        else
                        {
                            Text buyBtnTxt = buyBtn.GetComponentInChildren<Text>();
                            if (buyBtnTxt != null) buyBtnTxt.text = "Dibeli";
                        }
                    }
                    else
                    {
                        buyBtn.interactable = true;
                        buyBtn.onClick.AddListener(() => BuyItem(item));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Logika proses pembelian item.
    /// </summary>
    public void BuyItem(ShopItemData item)
    {
        if (item == null) return;

        // Cek apakah koin mencukupi via CoinManager
        if (CoinManager.Instance.UseCoins(item.price))
        {
            Debug.Log($"[ShopUI] Berhasil membeli {item.itemName} (Kategori: {item.category}) seharga {item.price} koin!");

            // Tentukan posisi spawn item di dunia 3D
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (_currentShopArea != null && _currentShopArea.spawnPoint != null)
            {
                spawnPos = _currentShopArea.spawnPoint.position;
                spawnRot = _currentShopArea.spawnPoint.rotation;
            }
            else
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    spawnPos = cam.transform.position + cam.transform.forward * 2.0f;
                }
            }

            // 1. Eksekusi Aksi Tag & Nama Objek di Scene jika dikonfigurasi
            if (item.useTagAction)
            {
                ExecuteTagActions(item);
            }

            // 2. Eksekusi Aksi Objek Spesifik via Komponen ShopItemSceneAction di Scene
            ExecuteSceneActions(item);

            // 3. Spawn prefab objek 3D di dunia game (jika itemPrefab diisi)
            if (item.itemPrefab != null)
            {
                GameObject spawnedObj = Instantiate(item.itemPrefab, spawnPos, spawnRot);
                spawnedObj.name = item.itemName;

                // Jika objek adalah Dekorasi, tandai isPlaced = false terlebih dahulu
                // sampai pemain mengambil (Grab) dan meletakkannya (Drop) di posisi tujuan.
                DecorationItem dec = spawnedObj.GetComponent<DecorationItem>();
                if (dec != null)
                {
                    dec.shopData = item;
                    dec.SetPlaced(false);
                }
            }

            // 4. Jika kategori Dekorasi, tandai sudah dibeli dan cek kondisi menang
            if (item.category == ShopCategory.Dekorasi)
            {
                item.isPurchased = true;
                
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckWinCondition();
                }
            }

            // Cek juga lose condition untuk memastikan koin tidak habis tanpa sisa
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckLoseCondition();
            }
        }
        else
        {
            Debug.LogWarning($"[ShopUI] Koin tidak mencukupi untuk membeli {item.itemName}!");
        }
    }

    /// <summary>
    /// Menjalankan aksi mengaktifkan atau menonaktifkan GameObject di scene berdasarkan daftar Tag atau Nama Objek pada item.
    /// </summary>
    private void ExecuteTagActions(ShopItemData item)
    {
        if (item == null || !item.useTagAction) return;

        // A1. Nonaktifkan objek di scene berdasarkan tagsToDeactivate (misal: 'Sampah')
        if (item.tagsToDeactivate != null && item.tagsToDeactivate.Count > 0)
        {
            foreach (string tag in item.tagsToDeactivate)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;

                List<GameObject> objects = FindObjectsWithTagIncludingInactive(tag);
                foreach (var obj in objects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                        Debug.Log($"[ShopUI] Nonaktifkan GameObject '{obj.name}' dengan Tag '{tag}' (Item: {item.itemName})");
                    }
                }
            }
        }

        // A2. Hidupkan objek di scene berdasarkan tagsToActivate (misal: 'Gate', 'SampahMagicEffect')
        if (item.tagsToActivate != null && item.tagsToActivate.Count > 0)
        {
            foreach (string tag in item.tagsToActivate)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;

                List<GameObject> objects = FindObjectsWithTagIncludingInactive(tag);
                foreach (var obj in objects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                        Debug.Log($"[ShopUI] Aktifkan GameObject '{obj.name}' dengan Tag '{tag}' (Item: {item.itemName})");

                        DecorationItem dec = obj.GetComponent<DecorationItem>();
                        if (dec != null)
                        {
                            dec.shopData = item;
                            dec.SetPlaced(true);
                        }
                    }
                }
            }
        }

        // B1. Nonaktifkan objek di scene berdasarkan Nama Objek (objectNamesToDeactivate)
        if (item.objectNamesToDeactivate != null && item.objectNamesToDeactivate.Count > 0)
        {
            foreach (string objName in item.objectNamesToDeactivate)
            {
                if (string.IsNullOrWhiteSpace(objName)) continue;

                List<GameObject> objects = FindObjectsByNameIncludingInactive(objName);
                foreach (var obj in objects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                        Debug.Log($"[ShopUI] Nonaktifkan GameObject '{obj.name}' berdasarkan Nama (Item: {item.itemName})");
                    }
                }
            }
        }

        // B2. Hidupkan objek di scene berdasarkan Nama Objek (objectNamesToActivate)
        if (item.objectNamesToActivate != null && item.objectNamesToActivate.Count > 0)
        {
            foreach (string objName in item.objectNamesToActivate)
            {
                if (string.IsNullOrWhiteSpace(objName)) continue;

                List<GameObject> objects = FindObjectsByNameIncludingInactive(objName);
                foreach (var obj in objects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                        Debug.Log($"[ShopUI] Aktifkan GameObject '{obj.name}' berdasarkan Nama (Item: {item.itemName})");

                        DecorationItem dec = obj.GetComponent<DecorationItem>();
                        if (dec != null)
                        {
                            dec.shopData = item;
                            dec.SetPlaced(true);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Menjalankan komponen ShopItemSceneAction yang ditempelkan langsung pada GameObject spesifik di Scene.
    /// </summary>
    private void ExecuteSceneActions(ShopItemData item)
    {
        if (item == null) return;

        ShopItemSceneAction[] sceneActions = Resources.FindObjectsOfTypeAll<ShopItemSceneAction>();
        foreach (var action in sceneActions)
        {
            if (action != null && action.gameObject.scene.isLoaded)
            {
                if (action.targetShopItem == item)
                {
                    action.ExecuteAction();
                }
            }
        }
    }

    /// <summary>
    /// Mencari seluruh GameObject di scene yang memiliki Tag tertentu (termasuk objek yang sedang inactive).
    /// </summary>
    private List<GameObject> FindObjectsWithTagIncludingInactive(string tag)
    {
        List<GameObject> result = new List<GameObject>();
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t != null && t.gameObject.scene.isLoaded)
            {
                if (t.CompareTag(tag))
                {
                    result.Add(t.gameObject);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Mencari seluruh GameObject di scene yang memiliki Nama persis (termasuk objek yang sedang inactive).
    /// </summary>
    private List<GameObject> FindObjectsByNameIncludingInactive(string objName)
    {
        List<GameObject> result = new List<GameObject>();
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t != null && t.gameObject.scene.isLoaded)
            {
                if (t.gameObject.name.Equals(objName, System.StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(t.gameObject);
                }
            }
        }
        return result;
    }
}
