using UnityEngine;
using UnityEngine.UI;

public class TanamanUI : MonoBehaviour
{
    private Tanaman komponenTanaman;
    
    [Header("Komponen UI")]
    public Slider sliderAir;
    public Image imageIndikatorWarna; // Untuk memberikan sinyal warna
    public GameObject teksNotifikasiBahaya; // Muncul saat kritis

    void Awake()
    {
        komponenTanaman = GetComponent<Tanaman>();
    }

    void Update()
    {
        if (komponenTanaman == null) return;

        // Update Nilai Slider Air
        sliderAir.value = komponenTanaman.levelAir / komponenTanaman.dataTanaman.batasOverwater;

        // Logika Indikator Warna & Notifikasi
        if (komponenTanaman.isMati)
        {
            imageIndikatorWarna.color = Color.black; // Hitam = Mati
            teksNotifikasiBahaya.SetActive(false);
        }
        else if (komponenTanaman.levelAir > 80f)
                {
            imageIndikatorWarna.color = Color.blue; // Biru = Kebasahan!
            teksNotifikasiBahaya.SetActive(true);
        }
        else if (komponenTanaman.levelAir < 25f)
        {
            imageIndikatorWarna.color = Color.red; // Merah = Kering/Butuh Siram!
            teksNotifikasiBahaya.SetActive(true);
        }
        else
        {
            imageIndikatorWarna.color = Color.green; // Hijau = Kondisi Ideal
            teksNotifikasiBahaya.SetActive(false);
        }
    }
}

