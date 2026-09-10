using UnityEngine;

public class Pot : MonoBehaviour
{
    // Properti status pot
    public bool tertataDiWall { get; private set; } = false;
    public bool memilikiBibit { get; private set; } = false;
    
    [Header("Pengaturan Tempat Spawn")]
    public Transform tempatTumbuh; // Titik posisi spawn visual tanaman

    // Fungsi untuk mengubah status saat pot dipasang di dinding
    public void TataDiWall()
    {
        tertataDiWall = true;
    }

    // Fungsi untuk mengubah status saat pot diberi bibit
    public void IsiBibit()
    {
        memilikiBibit = true;
    }

    // Fungsi untuk mengosongkan status pot setelah tanaman dipanen
    public void KosongkanPot()
    {
        memilikiBibit = false;
    }
}
