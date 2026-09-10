using UnityEngine;

public class Tanaman : MonoBehaviour
{
    public TanamanData dataTanaman;
    
    // Status Tanaman
    public float levelAir { get; private set; }
    public int tahapTumbuh { get; private set; } = 0;
    public bool isDiberiPupuk { get; private set; } = false;
    public bool isMati { get; private set; } = false;
    public bool isSiapPanen { get; private set; } = false;

    private float timerTumbuh = 0f;
    private GameObject visualSaatIni;

    void Start()
    {
        levelAir = dataTanaman.nilaiAirIdealAwal;
        UpdateVisual();
    }

    void Update()
        {
        if (isMati || isSiapPanen) return;

        UpdateMekanismeAir();
        UpdateMekanismePertumbuhan();
    }

    private void UpdateMekanismeAir()
    {
        // Air berkurang seiring waktu
        levelAir -= dataTanaman.penguranganAirPerDetik * Time.deltaTime;
        levelAir = Mathf.Clamp(levelAir, 0f, 120f); // Dibatasi sedikit lewat dari 100 untuk buffer

        // Cek kondisi kematian (Terlalu kering ATAU Terlalu basah)
        if (levelAir <= dataTanaman.batasKekeringan || levelAir >= dataTanaman.batasOverwater)
        {
            Mati();
        }
    }

    private void UpdateMekanismePertumbuhan()
    {
        // Kecepatan tumbuh bertambah 2x lipat jika diberi pupuk
        float kecepatanTumbuh = isDiberiPupuk ? 2f : 1f;
        timerTumbuh += Time.deltaTime * kecepatanTumbuh;

        if (timerTumbuh >= dataTanaman.waktuPerTahap)
        {
            TumbuhKeTahapBerikutnya();
        }
    }

    private void TumbuhKeTahapBerikutnya()
    {
        timerTumbuh = 0f;
        tahapTumbuh++;

        if (tahapTumbuh >= dataTanaman.prefabTahapanVisual.Length - 1)
        {
            isSiapPanen = true;
        }

        UpdateVisual();
    }

    public void Siram()
    {
        if (isMati) return;
        levelAir += dataTanaman.penambahanAirPerSiram;
        Debug.Log($"Tanaman disiram. Level air sekarang: {levelAir}%");
    }

    public void BeriPupuk()
    {
        if (isMati || isDiberiPupuk) return;
        isDiberiPupuk = true;
        Debug.Log("Tanaman berhasil diberi pupuk! Pertumbuhan dipercepat.");
    }

    private void UpdateVisual()
    {
        if (visualSaatIni != null) Destroy(visualSaatIni);

        visualSaatIni = Instantiate(dataTanaman.prefabTahapanVisual[tahapTumbuh], transform.position, Quaternion.identity, transform);
    }

    private void Mati()
    {
        isMati = true;
        Debug.Log("Tanaman MATI! Karena kekeringan atau terlalu banyak disiram.");
        // Anda bisa menambahkan visual tanaman layu di sini
    }
}

