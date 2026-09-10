using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Komponen pengendali Grab sederhana (MVP).
/// Mendeteksi objek dengan lebar raycast yang bisa diatur, memunculkan indikator di atas objek,
/// memindahkan ke tangan, serta menempatkan Pot ke dinding ber-tag 'Grid' dengan Ghost preview.
/// </summary>
public class PlayerGrab : MonoBehaviour
{
    [Header("Referensi")]
    public Transform playerCamera;
    [Tooltip("Objek kosong posisi tangan")]
    public Transform handPoint;

    [Header("Pengaturan Deteksi")]
    public float grabDistance = 3.0f;
    [Tooltip("Lebar / radius area raycast (0 = garis tipis, > 0 = lebih tebal/mudah dibidik)")]
    public float raycastRadius = 0.3f;
    public LayerMask grabLayer = ~0;

    private Grabbable _currentTarget;
    private Grabbable _heldItem;

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    private void Update()
    {
        // 1. Jika sedang memegang sesuatu
        if (_heldItem != null)
        {
            // Cek apakah memegang Pot
            Pot heldPot = _heldItem.GetComponent<Pot>();
            if (heldPot != null)
            {
                CheckGridPlacement(heldPot);
                return;
            }

            // Jika barang biasa dan tombol ditekan -> Drop
            if (IsInputTriggered())
            {
                _heldItem.Drop();
                _heldItem = null;
            }
            return;
        }

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

                Vector3 placePos = wallGrid != null
                    ? wallGrid.GetPlacementPosition(hit.point, hit.normal)
                    : hit.point;

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
        if (_currentTarget != null)
        {
            _currentTarget.SetIndicator(false);
            _currentTarget = null;
        }
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
