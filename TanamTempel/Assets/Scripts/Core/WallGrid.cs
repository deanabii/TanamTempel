using UnityEngine;

/// <summary>
/// Komponen untuk GameObject dinding ber-tag 'Grid'.
/// Mengatur posisi penempatan pot pada permukaan grid.
/// </summary>
public class WallGrid : MonoBehaviour
{
    [Tooltip("Ukuran sel/kotak grid. Jika 0, posisi pot mengikuti pandangan secara halus. Jika > 0, otomatis snap rapi ke grid.")]
    public float cellSize = 0.5f;

    [Tooltip("Jarak offset keluar dari permukaan dinding agar pot tidak tembus ke dalam dinding.")]
    public float surfaceOffset = 0.05f;

    /// <summary>
    /// Menghitung posisi pot pada permukaan grid (mendukung snap ke kotak grid).
    /// </summary>
    public Vector3 GetPlacementPosition(Vector3 hitPoint, Vector3 hitNormal)
    {
        Vector3 basePoint = hitPoint;

        if (cellSize > 0.001f)
        {
            // Ubah posisi hit ke koordinat lokal grid
            Vector3 local = transform.InverseTransformPoint(hitPoint);

            // Snap X dan Y ke kelipatan cellSize
            local.x = Mathf.Round(local.x / cellSize) * cellSize;
            local.y = Mathf.Round(local.y / cellSize) * cellSize;
            local.z = 0; // Tepat di bidang dinding

            basePoint = transform.TransformPoint(local);
        }

        // Tambahkan offset tipis searah normal dinding
        return basePoint + hitNormal * surfaceOffset;
    }
}
