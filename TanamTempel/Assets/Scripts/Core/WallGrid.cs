using UnityEngine;

/// <summary>
/// Komponen untuk GameObject dinding ber-tag 'Grid'.
/// Mengatur posisi penempatan pot pada permukaan grid agar snap rapi dan berada tepat di luar dinding.
/// </summary>
public class WallGrid : MonoBehaviour
{
    [Tooltip("Ukuran sel/kotak grid. Jika 0, posisi pot mengikuti pandangan secara halus. Jika > 0, otomatis snap rapi ke grid.")]
    public float cellSize = 0.5f;

    /// <summary>
    /// Menghitung posisi penempatan pada grid, didorong ke luar permukaan sesuai ketebalan Collider pot.
    /// </summary>
    public Vector3 GetPlacementPosition(Vector3 hitPoint, Vector3 hitNormal, float colliderOffset = 0f)
    {
        Vector3 basePoint = hitPoint;

        if (cellSize > 0.001f)
        {
            // Ubah posisi hit ke koordinat lokal grid
            Vector3 local = transform.InverseTransformPoint(hitPoint);

            // Snap X dan Y ke kelipatan cellSize (pertahankan posisi permukaan dinding)
            local.x = Mathf.Round(local.x / cellSize) * cellSize;
            local.y = Mathf.Round(local.y / cellSize) * cellSize;

            basePoint = transform.TransformPoint(local);
        }

        // Dorong keluar permukaan dinding sebesar ukuran Collider pot
        return basePoint + hitNormal * colliderOffset;
    }
}
