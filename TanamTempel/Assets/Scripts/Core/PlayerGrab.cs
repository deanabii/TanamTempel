using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Komponen pengendali Grab sederhana (MVP).
/// Mendeteksi objek dengan lebar raycast yang bisa diatur, memunculkan indikator di atas objek,
/// memindahkan ke tangan, menempatkan Pot ke dinding ber-tag 'Grid' dengan Ghost preview,
/// menanam Biji ke dalam Pot dengan indikator 'Tanam',
/// serta menyiram Pot dan mengisi Gayung di Tong Air.
/// </summary>
public class PlayerGrab : MonoBehaviour
{
    [Header("Referensi")]
    public Transform playerCamera;
    [Tooltip("Objek kosong posisi tangan")]
    public Transform handPoint;

    [Header("Pengaturan Deteksi")]
    public float grabDistance = 3.0f;
    [Tooltip("Lebar / radius area raycast untuk mengambil barang (0 = garis tipis, > 0 = lebih tebal/mudah dibidik)")]
    public float raycastRadius = 0.3f;
    public LayerMask grabLayer = ~0;

    private Grabbable _currentTarget;
    private Grabbable _heldItem;
    private Pot _currentHoveredPotForPlanting;
    private TongAir _currentHoveredTongAir;
    private Pot _currentHoveredPotForWatering;

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    private void OnDisable()
    {
        ClearGrabTarget();
        ClearPlantingTarget();
        ClearWateringTarget();
    }

    private void Update()
    {
        // 1. Jika sedang memegang sesuatu di tangan
        if (_heldItem != null)
        {
            // Pastikan target grab biasa dinonaktifkan saat tangan memegang barang
            ClearGrabTarget();

            // A. Cek apakah memegang Pot (untuk ditempel ke dinding Grid)
            Pot heldPot = _heldItem.GetComponent<Pot>();
            if (heldPot != null)
            {
                ClearPlantingTarget();
                ClearWateringTarget();
                CheckGridPlacement(heldPot);
                return;
            }

            // B. Cek apakah memegang Biji (untuk ditanam ke dalam Pot)
            Biji heldBiji = _heldItem.GetComponent<Biji>();
            if (heldBiji != null || _heldItem.CompareTag("Biji"))
            {
                ClearWateringTarget();
                CheckSeedPlanting(heldBiji);
                return;
            }

            // C. Cek apakah memegang Gayung (untuk menyiram tanaman atau mengisi air di Tong Air)
            Gayung heldGayung = _heldItem.GetComponent<Gayung>();
            if (heldGayung != null || _heldItem.CompareTag("Gayung"))
            {
                ClearPlantingTarget();
                CheckGayungInteractions(heldGayung);
                return;
            }

            // D. Jika barang biasa dan tombol ditekan -> Drop
            ClearPlantingTarget();
            ClearWateringTarget();
            if (IsInputTriggered())
            {
                _heldItem.Drop();
                _heldItem = null;
            }
            return;
        }

        // Jika tangan kosong, pastikan indikator tanam dan siram mati
        ClearPlantingTarget();
        ClearWateringTarget();

        // 2. Jika tangan kosong, cari objek di depan
        CheckLookAt();

        // 3. Ambil objek jika ada target dan tombol ditekan
        if (IsInputTriggered() && _currentTarget != null)
        {
            if (handPoint != null)
            {
                _heldItem = _currentTarget;
                _heldItem.Grab(handPoint);
                _currentTarget = null;
            }
            else
            {
                Debug.LogWarning("Hand Point belum diisi di Inspector!");
            }
        }
    }

    /// <summary>
    /// Menangani interaksi pemain saat memegang Gayung:
    /// - Melihat ke Tong Air: memunculkan indikator 'Isi Air', tekan E/tap untuk mengisi penuh (3/3).
    /// - Melihat ke Pot yang butuh air: tekan E/tap untuk menyiram (mengurangi 1 air gayung).
    /// - Melihat ke arah lain: tekan E/tap untuk melepaskan (drop) gayung ke lantai.
    /// </summary>
    private void CheckGayungInteractions(Gayung gayung)
    {
        if (playerCamera == null) return;
        if (gayung == null && _heldItem != null) gayung = _heldItem.GetComponent<Gayung>();

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        TongAir targetTong = null;
        Pot targetPot = null;
        Pot lookedAtPotNeedsWater = null;

        // Gunakan Raycast lurus presisi agar indikator segera hilang saat crosshair beralih
        if (Physics.Raycast(ray, out hit, grabDistance, grabLayer))
        {
            // 1. Cek apakah mengarah ke Tong Air
            targetTong = hit.collider.GetComponentInParent<TongAir>();

            // 2. Jika bukan Tong Air, cek apakah mengarah ke Pot yang butuh disiram
            if (targetTong == null)
            {
                Pot foundPot = hit.collider.GetComponentInParent<Pot>();
                if (foundPot != null && foundPot.isPlanted && foundPot.plantGrowth != null && foundPot.plantGrowth.needsWater)
                {
                    lookedAtPotNeedsWater = foundPot;

                    // Indikator siram hanya muncul jika gayung sedang terisi air
                    if (gayung != null && gayung.HasWater)
                    {
                        targetPot = foundPot;
                    }
                }
            }
        }

        // Perbarui tampilan indikator Tong Air
        if (_currentHoveredTongAir != targetTong)
        {
            if (_currentHoveredTongAir != null)
            {
                _currentHoveredTongAir.SetIndicator(false);
            }

            _currentHoveredTongAir = targetTong;

            if (_currentHoveredTongAir != null)
            {
                _currentHoveredTongAir.SetIndicator(true);
            }
        }

        // Perbarui tampilan indikator Siram pada Pot
        if (_currentHoveredPotForWatering != targetPot)
        {
            if (_currentHoveredPotForWatering != null)
            {
                _currentHoveredPotForWatering.SetWaterIndicator(false);
            }

            _currentHoveredPotForWatering = targetPot;

            if (_currentHoveredPotForWatering != null)
            {
                _currentHoveredPotForWatering.SetWaterIndicator(true);
            }
        }

        // Eksekusi aksi jika tombol ditekan (E / tap)
        if (IsInputTriggered())
        {
            // A. Interaksi dengan Tong Air -> Isi gayung hingga penuh
            if (_currentHoveredTongAir != null)
            {
                if (gayung != null)
                {
                    gayung.Refill();
                }
                return;
            }

            // B. Interaksi dengan Pot yang butuh air -> Siram tanaman
            if (_currentHoveredPotForWatering != null)
            {
                if (gayung != null && gayung.HasWater)
                {
                    Pot potToWater = _currentHoveredPotForWatering;
                    _currentHoveredPotForWatering = null;
                    potToWater.SetWaterIndicator(false);

                    if (gayung.UseWater())
                    {
                        potToWater.plantGrowth.WaterPlant();
                    }
                }
                return;
            }

            // C. Jika membidik pot yang butuh air tetapi gayung kosong -> Beri pesan, jangan drop
            if (lookedAtPotNeedsWater != null)
            {
                Debug.Log("[PlayerGrab] Gayung kosong! Isi air di Tong Air terlebih dahulu.");
                return;
            }

            // D. Tidak membidik Tong Air maupun Pot yang butuh air -> Drop gayung ke lantai
            ClearWateringTarget();
            _heldItem.Drop();
            _heldItem = null;
        }
    }

    private void ClearWateringTarget()
    {
        if (_currentHoveredTongAir != null)
        {
            _currentHoveredTongAir.SetIndicator(false);
            _currentHoveredTongAir = null;
        }

        if (_currentHoveredPotForWatering != null)
        {
            _currentHoveredPotForWatering.SetWaterIndicator(false);
            _currentHoveredPotForWatering = null;
        }
    }

    /// <summary>
    /// Menangani deteksi pot saat memegang Biji dan melakukan penanaman.
    /// Menggunakan Raycast presisi agar indikator Tanam seketika mati saat pandangan beralih dari pot.
    /// </summary>
    private void CheckSeedPlanting(Biji biji)
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        Pot targetPot = null;

        // Gunakan Raycast lurus presisi agar membidik pot tepat di tengah pandangan
        if (Physics.Raycast(ray, out hit, grabDistance, grabLayer))
        {
            Pot foundPot = hit.collider.GetComponentInParent<Pot>();
            if (foundPot != null && !foundPot.isPlanted)
            {
                targetPot = foundPot;
            }
        }

        // Jika target pot berubah dari frame sebelumnya
        if (_currentHoveredPotForPlanting != targetPot)
        {
            // Matikan indikator pot sebelumnya saat pandangan beralih
            if (_currentHoveredPotForPlanting != null)
            {
                _currentHoveredPotForPlanting.SetPlantIndicator(false);
            }

            _currentHoveredPotForPlanting = targetPot;

            // Nyalakan indikator pot baru jika sedang melihat pot
            if (_currentHoveredPotForPlanting != null)
            {
                _currentHoveredPotForPlanting.SetPlantIndicator(true);
            }
        }

        // Jika sedang membidik pot dan tombol ditekan -> Tanam biji!
        if (_currentHoveredPotForPlanting != null)
        {
            if (IsInputTriggered())
            {
                Pot potToPlant = _currentHoveredPotForPlanting;
                _currentHoveredPotForPlanting = null;

                // Ambil data tanaman dari biji yang sedang dipegang
                PlantData plantData = biji != null ? biji.plantData : null;
                potToPlant.Plant(plantData);

                // Hilangkan biji yang dipegang
                if (biji != null)
                {
                    biji.Consume();
                }
                else
                {
                    Destroy(_heldItem.gameObject);
                }

                _heldItem = null;
            }
        }
        else
        {
            // Jika tidak melihat pot dan tombol ditekan -> Drop biji ke lantai
            if (IsInputTriggered())
            {
                _heldItem.Drop();
                _heldItem = null;
            }
        }
    }

    private void ClearPlantingTarget()
    {
        if (_currentHoveredPotForPlanting != null)
        {
            _currentHoveredPotForPlanting.SetPlantIndicator(false);
            _currentHoveredPotForPlanting = null;
        }
    }

    private void ClearGrabTarget()
    {
        if (_currentTarget != null)
        {
            _currentTarget.SetIndicator(false);
            _currentTarget = null;
        }
    }

    /// <summary>
    /// Menangani deteksi dinding Grid dan penempatan Pot menggunakan Ghost object.
    /// </summary>
    private void CheckGridPlacement(Pot pot)
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        bool lookingAtGrid = false;

        if (Physics.Raycast(ray, out hit, grabDistance))
        {
            // Cek apakah objek ber-tag 'Grid' atau punya komponen WallGrid
            if (hit.collider.CompareTag("Grid") || hit.collider.GetComponentInParent<WallGrid>() != null)
            {
                lookingAtGrid = true;
                WallGrid wallGrid = hit.collider.GetComponentInParent<WallGrid>();
                float colliderOffset = pot.GetColliderOffset();

                Vector3 placePos = wallGrid != null
                    ? wallGrid.GetPlacementPosition(hit.point, hit.normal, colliderOffset)
                    : hit.point + hit.normal * colliderOffset;

                Quaternion placeRot = Quaternion.LookRotation(hit.normal);

                // Tampilkan ghost object di grid
                pot.UpdateGhost(placePos, placeRot);

                // Jika tekan tombol E di Grid -> Tempatkan Pot ke dinding
                if (IsInputTriggered())
                {
                    pot.Place(hit.transform, placePos, placeRot);
                    _heldItem = null;
                    return;
                }
            }
        }

        // Jika tidak mengarah ke Grid
        if (!lookingAtGrid)
        {
            pot.HideGhost();

            // Jika tekan E di luar grid -> Drop pot ke lantai
            if (IsInputTriggered())
            {
                _heldItem.Drop();
                _heldItem = null;
            }
        }
    }

    private void CheckLookAt()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        bool hasHit = raycastRadius > 0.001f
            ? Physics.SphereCast(ray, raycastRadius, out hit, grabDistance, grabLayer)
            : Physics.Raycast(ray, out hit, grabDistance, grabLayer);

        if (hasHit)
        {
            Grabbable grabbable = hit.collider.GetComponentInParent<Grabbable>();
            if (grabbable != null && !grabbable.isGrabbed)
            {
                if (_currentTarget != grabbable)
                {
                    if (_currentTarget != null) _currentTarget.SetIndicator(false);
                    _currentTarget = grabbable;
                    _currentTarget.SetIndicator(true);
                }
                return;
            }
        }

        // Jika tidak melihat objek grabbable
        ClearGrabTarget();
    }

    private bool IsInputTriggered()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#else
        if (Input.GetKeyDown(KeyCode.E)) return true;
        if (Input.GetMouseButtonDown(0)) return true;
#endif
        return false;
    }

    // Visualisasi lebar area di Scene View Editor
    private void OnDrawGizmosSelected()
    {
        Transform cam = playerCamera != null ? playerCamera : transform;
        Gizmos.color = _currentTarget != null ? Color.green : Color.yellow;

        if (raycastRadius > 0.001f)
        {
            Gizmos.DrawWireSphere(cam.position + cam.forward * grabDistance, raycastRadius);
        }
        else
        {
            Gizmos.DrawLine(cam.position, cam.position + cam.forward * grabDistance);
        }
    }
}
