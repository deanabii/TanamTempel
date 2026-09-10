using UnityEngine;

public class PemainKarakter : MonoBehaviour
{
    [Header("Status Tangan Pemain (Pilih Salah Satu di Inspector untuk Uji Coba)")]
    public bool sedangBawaPot = false;
    public bool sedangBawaBibit = false;
    public bool sedangBawaPupuk = false;
    public bool sedangBawaPenyiram = false;

    [Header("Referensi Prefab")]
    [Tooltip("Masukkan Prefab Inti Tanaman yang memiliki script 'Tanaman' dan 'TanamanUI'")]
    public GameObject prefabTanamanInti; 

    /// <summary>
    /// Fungsi untuk meletakkan pot dari tangan pemain ke slot Dinding Gardenia.
    /// </summary>
    public void InteraksiDenganDinding(WallGardenia wall, GameObject objekPotTangan)
    {
        // Validasi: Pemain harus benar-benar sedang membawa pot
        if (sedangBawaPot && objekPotTangan != null)
        {
            Transform slotKosong = wall.DapatkanSlotKosong();
            
            if (slotKosong != null)
            {
                // Pindahkan pot dari tangan ke slot dinding
                objekPotTangan.transform.SetParent(slotKosong);
                objekPotTangan.transform.localPosition = Vector3.zero;
                objekPotTangan.transform.localRotation = Quaternion.identity;
                
                // Ubah status pot menjadi tertata di dinding
                Pot potScript = objekPotTangan.GetComponent<Pot>();
                if (potScript != null)
                {
                    potScript.TataDiWall();
                }
                
                // Reset status tangan pemain
                sedangBawaPot = false;
                Debug.Log("✅ Pot berhasil ditata di Wall Gardenia!");
            }
            else
            {
                Debug.LogWarning("⚠️ Wall Gardenia sudah penuh! Tidak ada slot kosong.");
            }
        }
    }

    /// <summary>
    /// Fungsi inti untuk mengelola seluruh interaksi pada pot yang sudah ada di dinding.
    /// Berfungsi untuk menanam, memupuk, menyiram, memanen, dan membuang tanaman mati.
    /// </summary>
    public void InteraksiDenganPotDiDinding(Pot potTarget)
    {
        // Keamanan: Pot harus sudah ditata di dinding sebelum bisa diinteraksi lebih lanjut
        if (potTarget == null || !potTarget.tertataDiWall) 
        {
            Debug.LogWarning("⚠️ Pot harus ditata di Wall Gardenia terlebih dahulu!");
            return;
        }

        // --- TAHAP 1: MENANAM BIBIT ---
        if (sedangBawaBibit && !potTarget.memilikiBibit)
        {
            potTarget.IsiBibit();
            
            // Memunculkan (spawn) bibit tanaman di titik tumbuh pot
            if (prefabTanamanInti != null && potTarget.tempatTumbuh != null)
            {
                Instantiate(prefabTanamanInti, potTarget.tempatTumbuh.position, Quaternion.identity, potTarget.tempatTumbuh);
                sedangBawaBibit = false;
                Debug.Log("🌱 Bibit berhasil dimasukkan ke dalam pot!");
            }
            else
            {
                Debug.LogError("❌ Prefab Tanaman Inti atau Tempat Tumbuh Pot belum di-assign di Inspector!");
            }
            return;
        }

        // Ambil komponen script Tanaman yang ada di dalam pot tersebut
        Tanaman tanaman = potTarget.GetComponentInChildren<Tanaman>();
        
        // Jika pot memiliki bibit tetapi objek tanaman belum/gagal terdeteksi, hentikan proses
        if (tanaman == null) 
        {
            if (!potTarget.memilikiBibit)
            {
                Debug.Log("ℹ️ Pot ini masih kosong. Silakan bawa bibit untuk menanam.");
            }
            return;
        }

        // --- TAHAP 2: MEMBERI PUPUK ---
        if (sedangBawaPupuk)
        {
            tanaman.BeriPupuk();
            // sedangBawaPupuk = false; // Hapus tanda komentar jika pupuk bersifat habis pakai
            return;
        }

        // --- TAHAP 3: MENYIRAM TANAMAN ---
        if (sedangBawaPenyiram)
        {
            tanaman.Siram();
            return;
        }
        
        // --- TAHAP 4: PENGELOLAAN TANAMAN MATI ---
        if (tanaman.isMati)
        {
            Debug.Log("🪹 Tanaman mati akibat kelalaian (terlalu kering/basah). Membuang ke tempat sampah...");
            
            Destroy(tanaman.gameObject); // Hancurkan visual dan logikanya
            potTarget.KosongkanPot();    // Reset status pot agar bisa ditanami kembali
            return;
        }

        // --- TAHAP 5: PANEN SUKSES ---
        if (tanaman.isSiapPanen)
        {
            // Ambil nilai harga jual dari database tanaman dan tambahkan ke sistem ekonomi
            int pendapatan = tanaman.dataTanaman.hargaJual;
            
            if (ManajerEkonomi.Instance != null)
            {
                ManajerEkonomi.Instance.TambahUang(pendapatan);
                Debug.Log($"💰 Sukses memanen {tanaman.dataTanaman.namaTanaman}! Mendapat Rp{pendapatan}");
            }
            else
            {
                Debug.LogError("❌ ManajerEkonomi.Instance tidak ditemukan di Scene! Uang gagal ditambahkan.");
            }
            
            Destroy(tanaman.gameObject); // Hancurkan objek tanaman setelah dipanen
            potTarget.KosongkanPot();    // Reset status pot agar bisa ditanami kembali
            return;
        }

        // Jika pemain berinteraksi dengan tangan kosong saat tanaman sedang tumbuh biasa
        Debug.Log($"⏳ Tanaman {tanaman.dataTanaman.namaTanaman} sedang tumbuh (Tahap: {tanaman.tahapTumbuh}). Level Air: {Mathf.Round(tanaman.levelAir)}%");
    }
}
