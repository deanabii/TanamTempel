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

    [Header("Pengaturan Deteksi Box Collider")]
    [Tooltip("Komponen BoxCollider yang dipasang di kamera untuk area interaksi. Jika kosong, akan dicari otomatis pada playerCamera.")]
    public BoxCollider interactionBoxCollider;
    public LayerMask grabLayer = ~0;

    [Header("Gizmos / Visualisasi Box Collider")]
    public bool showGizmos = true;
    public bool alwaysShowGizmos = false;
    public Color gizmoBoxColor = Color.yellow;
    public Color gizmoHitColor = Color.green;

    public static PlayerGrab Instance { get; private set; }

    /// <summary>
    /// Apakah pemain sedang mengarahkan pandangan / siap berinteraksi dengan objek dunia
    /// (seperti mengambil barang, menanam biji, menyiram, mengisi gayung, atau panen).
    /// </summary>
    public bool HasInteractionTarget =>
        _currentTarget != null ||
        _currentHoveredPotForPlanting != null ||
        _currentHoveredTongAir != null ||
        _currentHoveredPotForWatering != null ||
        _currentHoveredPotForHarvesting != null;

    private Grabbable _currentTarget;
    private Grabbable _heldItem;
    private Pot _currentHoveredPotForPlanting;
    private TongAir _currentHoveredTongAir;
    private Pot _currentHoveredPotForWatering;
    private Pot _currentHoveredPotForHarvesting;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (interactionBoxCollider == null && playerCamera != null)
        {
            interactionBoxCollider = playerCamera.GetComponent<BoxCollider>();
        }
    }

    private void OnDisable()
    {
        ClearGrabTarget();
        ClearPlantingTarget();
        ClearWateringTarget();
        ClearHarvestingTarget();
    }

    private void Update()
    {
        // Hentikan aksi grab & raycast jika UI Toko, UI Menang, UI Kalah, atau panel lain sedang terbuka
        if (GameManager.IsUIOpen)
        {
            ClearGrabTarget();
            ClearPlantingTarget();
            ClearWateringTarget();
            ClearHarvestingTarget();
            return;
        }

        // 0. Cek apakah pemain sedang melihat Pot yang Siap Panen (ready to harvest)
        if (CheckHarvestInteraction())
        {
            ClearGrabTarget();
            ClearPlantingTarget();
            ClearWateringTarget();
            return;
        }

        // Jika sedang membidik Pot Siap Panen (tanpa menekan tombol), utamakan indikator Panen daripada Grab/Tanam/Siram
        if (_currentHoveredPotForHarvesting != null)
        {
            ClearGrabTarget();
            ClearPlantingTarget();
            ClearWateringTarget();
            return;
        }

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
                AudioGame.Instance?.PlayDrop();
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
                AudioGame.Instance?.PlayGrab();

                Pot grabbedPot = _heldItem.GetComponent<Pot>();
                if (grabbedPot != null)
                {
                    grabbedPot.OnGrabbed();
                }
            }
            else
            {
                Debug.LogWarning("Hand Point belum diisi di Inspector!");
            }
        }
    }

    /// <summary>
    /// Mencari active BoxCollider (baik dari inspector atau otomatis dari playerCamera).
    /// </summary>
    public BoxCollider GetActiveBoxCollider()
    {
        if (interactionBoxCollider != null) return interactionBoxCollider;
        if (playerCamera != null)
        {
            interactionBoxCollider = playerCamera.GetComponent<BoxCollider>();
        }
        return interactionBoxCollider;
    }

    /// <summary>
    /// Melakukan deteksi objek di area interaksi menggunakan BoxCollider kamera.
    /// </summary>
    private bool PerformDetection(out RaycastHit hit, out Collider hitCollider)
    {
        hit = default;
        hitCollider = null;

        BoxCollider box = GetActiveBoxCollider();
        if (box == null) return false;

        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
        Quaternion orientation = box.transform.rotation;

        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, orientation, grabLayer);
        if (overlaps != null && overlaps.Length > 0)
        {
            float closestMetric = float.MaxValue;
            Collider bestCol = null;
            Transform cam = playerCamera != null ? playerCamera : transform;
            Vector3 camPos = cam.position;
            Vector3 camForward = cam.forward;

            // 1. Pertama, cari collider yang MEMILIKI komponen interaktif
            foreach (var col in overlaps)
            {
                if (col == box) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                if (playerCamera != null && (col.transform == playerCamera || col.transform.IsChildOf(playerCamera))) continue;

                bool isInteractable = col.GetComponentInParent<Grabbable>() != null ||
                                     col.GetComponentInParent<Pot>() != null ||
                                     col.GetComponentInParent<TongAir>() != null ||
                                     col.GetComponentInParent<WallGrid>() != null ||
                                     col.CompareTag("Grid");

                if (!isInteractable) continue;

                // Prioritaskan objek yang paling searah dengan pandangan tengah kamera & terdekat
                Vector3 toCol = (col.bounds.center - center).normalized;
                float angleDev = 1.0f - Mathf.Clamp01(Vector3.Dot(camForward, toCol));
                float dist = Vector3.Distance(camPos, col.bounds.center);
                float metric = angleDev * 5f + dist;

                if (metric < closestMetric)
                {
                    closestMetric = metric;
                    bestCol = col;
                }
            }

            // 2. Jika tidak ditemukan objek interaktif spesifik, cari collider umum terdekat
            if (bestCol == null)
            {
                closestMetric = float.MaxValue;
                foreach (var col in overlaps)
                {
                    if (col == box) continue;
                    if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                    if (playerCamera != null && (col.transform == playerCamera || col.transform.IsChildOf(playerCamera))) continue;

                    float dist = Vector3.Distance(camPos, col.bounds.center);
                    if (dist < closestMetric)
                    {
                        closestMetric = dist;
                        bestCol = col;
                    }
                }
            }

            if (bestCol != null)
            {
                hitCollider = bestCol;

                // Hit hitpoint & normal terdekat untuk penempatan grid / indikator
                hit.point = bestCol.ClosestPoint(center);
                Ray ray = new Ray(center, camForward);
                if (!bestCol.Raycast(ray, out hit, 10f))
                {
                    hit.point = bestCol.ClosestPoint(center);
                    hit.normal = -camForward;
                }
                return true;
            }
        }
        return false;
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

        TongAir targetTong = null;
        Pot targetPot = null;
        Pot lookedAtPotNeedsWater = null;

        RaycastHit hit;
        Collider hitCollider;

        if (PerformDetection(out hit, out hitCollider))
        {
            // 1. Cek apakah mengarah ke Tong Air
            targetTong = hitCollider.GetComponentInParent<TongAir>();

            // 2. Jika bukan Tong Air, cek apakah mengarah ke Pot yang mebutuhkan air
            if (targetTong == null)
            {
                Pot foundPot = hitCollider.GetComponentInParent<Pot>();
                if (foundPot != null && foundPot.isPlanted && foundPot.plantGrowth != null && foundPot.plantGrowth.needsWater)
                {
                    lookedAtPotNeedsWater = foundPot;
                    targetPot = foundPot;
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
                    AudioGame.Instance?.PlayRefillWater();
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
                        AudioGame.Instance?.PlayWater();
                    }
                }
                else
                {
                    AudioGame.Instance?.PlayError();
                }
                return;
            }

            // C. Jika membidik pot yang butuh air tetapi gayung kosong -> Beri pesan, jangan drop
            if (lookedAtPotNeedsWater != null)
            {
                Debug.Log("[PlayerGrab] Gayung kosong! Isi air di Tong Air terlebih dahulu.");
                AudioGame.Instance?.PlayError();
                return;
            }

            // D. Tidak membidik Tong Air maupun Pot yang butuh air -> Drop gayung ke lantai
            ClearWateringTarget();
            _heldItem.Drop();
            _heldItem = null;
            AudioGame.Instance?.PlayDrop();
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
    /// Menangani deteksi pot saat tanaman di dalamnya Siap Panen (Ready to Harvest).
    /// Mengarahkan pandangan (hover) ke pot akan memunculkan indikator Panen.
    /// Menekan tombol interaksi (E / Klik Kiri) akan memanen tanaman, menambah koin, dan mengosongkan pot.
    /// </summary>
    private bool CheckHarvestInteraction()
    {
        if (playerCamera == null) return false;

        RaycastHit hit;
        Collider hitCollider;
        Pot targetPot = null;

        if (PerformDetection(out hit, out hitCollider))
        {
            Pot foundPot = hitCollider.GetComponentInParent<Pot>();
            if (foundPot != null && foundPot.IsReadyToHarvest)
            {
                targetPot = foundPot;
            }
        }

        // Perbarui tampilan indikator Panen pada pot yang di-hover
        if (_currentHoveredPotForHarvesting != targetPot)
        {
            if (_currentHoveredPotForHarvesting != null)
            {
                _currentHoveredPotForHarvesting.SetHarvestIndicator(false);
            }

            _currentHoveredPotForHarvesting = targetPot;

            if (_currentHoveredPotForHarvesting != null)
            {
                _currentHoveredPotForHarvesting.SetHarvestIndicator(true);
            }
        }

        // Eksekusi panen jika tombol ditekan saat membidik pot Siap Panen
        if (_currentHoveredPotForHarvesting != null && IsInputTriggered())
        {
            Pot potToHarvest = _currentHoveredPotForHarvesting;
            _currentHoveredPotForHarvesting = null;

            potToHarvest.Harvest();
            AudioGame.Instance?.PlayHarvest();
            return true;
        }

        return false;
    }

    private void ClearHarvestingTarget()
    {
        if (_currentHoveredPotForHarvesting != null)
        {
            _currentHoveredPotForHarvesting.SetHarvestIndicator(false);
            _currentHoveredPotForHarvesting = null;
        }
    }

    /// <summary>
    /// Menangani deteksi pot saat memegang Biji dan melakukan penanaman.
    /// Menggunakan Raycast presisi agar indikator Tanam seketika mati saat pandangan beralih dari pot.
    /// </summary>
    private void CheckSeedPlanting(Biji biji)
    {
        if (playerCamera == null) return;

        RaycastHit hit;
        Collider hitCollider;
        Pot targetPot = null;

        if (PerformDetection(out hit, out hitCollider))
        {
            Pot foundPot = hitCollider.GetComponentInParent<Pot>();
            if (foundPot != null && foundPot.isPlacedOnWall && !foundPot.isPlanted)
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
                AudioGame.Instance?.PlayPlant();

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

        RaycastHit hit;
        Collider hitCollider;
        bool lookingAtGrid = false;

        if (PerformDetection(out hit, out hitCollider))
        {
            // Cek apakah objek ber-tag 'Grid' atau punya komponen WallGrid
            if (hitCollider.CompareTag("Grid") || hitCollider.GetComponentInParent<WallGrid>() != null)
            {
                lookingAtGrid = true;
                WallGrid wallGrid = hitCollider.GetComponentInParent<WallGrid>();
                float colliderOffset = pot.GetColliderOffset();

                Vector3 placePos = wallGrid != null
                    ? wallGrid.GetPlacementPosition(hit.point, hit.normal, colliderOffset)
                    : hit.point + hit.normal * colliderOffset;

                Quaternion placeRot = Quaternion.LookRotation(hit.normal);
                if (wallGrid != null && wallGrid.gridRotationOffset != Vector3.zero)
                {
                    placeRot *= Quaternion.Euler(wallGrid.gridRotationOffset);
                }

                // Tampilkan ghost object di grid
                pot.UpdateGhost(placePos, placeRot);

                // Jika tekan tombol E di Grid -> Tempatkan Pot ke dinding
                if (IsInputTriggered())
                {
                    pot.Place(hitCollider.transform, placePos, placeRot);
                    _heldItem = null;
                    AudioGame.Instance?.PlayPlacePot();
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
                AudioGame.Instance?.PlayDrop();
            }
        }
    }

    private void CheckLookAt()
    {
        if (playerCamera == null) return;

        RaycastHit hit;
        Collider hitCollider;
        bool hasHit = PerformDetection(out hit, out hitCollider);

        if (hasHit)
        {
            Grabbable grabbable = hitCollider.GetComponentInParent<Grabbable>();
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
        // 1. Cek sistem KeyBindingManager jika ada
        if (KeyBindingManager.Instance != null)
        {
            if (KeyBindingManager.Instance.IsInteractPressed()) return true;
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#else
            if (Input.GetMouseButtonDown(0)) return true;
#endif
            return false;
        }

        // 2. Fallback jika KeyBindingManager belum ada
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

    private void OnDrawGizmos()
    {
        if (alwaysShowGizmos)
        {
            DrawInteractionGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!alwaysShowGizmos)
        {
            DrawInteractionGizmos();
        }
    }

    private void DrawInteractionGizmos()
    {
        if (!showGizmos) return;

        BoxCollider box = GetActiveBoxCollider();
        if (box == null) return;

        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
        Quaternion orientation = box.transform.rotation;

        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, orientation, grabLayer);
        bool hasTarget = false;
        if (overlaps != null)
        {
            foreach (var col in overlaps)
            {
                if (col == box) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                if (playerCamera != null && (col.transform == playerCamera || col.transform.IsChildOf(playerCamera))) continue;

                if (col.GetComponentInParent<Grabbable>() != null ||
                    col.GetComponentInParent<Pot>() != null ||
                    col.GetComponentInParent<TongAir>() != null ||
                    col.GetComponentInParent<WallGrid>() != null ||
                    col.CompareTag("Grid"))
                {
                    hasTarget = true;
                    break;
                }
            }
        }

        Color mainColor = hasTarget ? gizmoHitColor : gizmoBoxColor;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(box.transform.position, box.transform.rotation, box.transform.lossyScale);

        Gizmos.color = mainColor;
        Gizmos.DrawWireCube(box.center, box.size);

        Color semiTransparent = mainColor;
        semiTransparent.a = 0.15f;
        Gizmos.color = semiTransparent;
        Gizmos.DrawCube(box.center, box.size);

        Gizmos.matrix = oldMatrix;
    }
}
