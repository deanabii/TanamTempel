using UnityEngine;

/// <summary>
/// Komponen pembantu untuk memberikan Powerup (Coin Multiplier & Growth Speed) pada Pot.
/// Tempelkan pada GameObject Pot atau Prefab Variant Pot untuk mengkonfigurasi status Powerup.
/// </summary>
public class PotPowerUp : MonoBehaviour
{
    [Header("Pengaturan Powerup")]
    [Tooltip("Pengganda Koin hasil panen di pot ini (misal: 2.0 = 2x Koin, 3.0 = 3x Koin).")]
    public float coinMultiplier = 2.0f;

    [Tooltip("Pengganda Kecepatan Tumbuh di pot ini (misal: 2.0 = 2x Lebih Cepat Tumbuh).")]
    public float growthSpeedMultiplier = 2.0f;

    private void Awake()
    {
        ApplyToPot();
    }

    private void Start()
    {
        ApplyToPot();
    }

    /// <summary>
    /// Menerapkan nilai powerup ke komponen Pot yang menempel pada objek ini atau anak objeknya.
    /// </summary>
    public void ApplyToPot()
    {
        Pot pot = GetComponent<Pot>();
        if (pot == null) pot = GetComponentInChildren<Pot>();

        if (pot != null)
        {
            pot.ApplyPowerUp(coinMultiplier, growthSpeedMultiplier);
        }
    }
}
