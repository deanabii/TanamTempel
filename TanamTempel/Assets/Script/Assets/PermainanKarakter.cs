using UnityEngine;

public class PemainKarakter : MonoBehaviour
{
    [Header("Status Tangan Pemain")]
    public bool sedangBawaPot = false;
    public bool sedangBawaBibit = false;
    public bool sedangBawaPupuk = false;
    public bool sedangBawaPenyiram = false;

    public GameObject prefabTanamanInti; // Prefab yang berisi Script Tanaman & TanamanUI

    // Fungsi simulasi interaksi (Bisa dipicu lewat Raycast klik tik/tombol UI)
    public void InteraksiDenganDinding(WallGardenia wall, GameObject objekPotTangan)
    {
        if (sedangBawaPot)
        {
            Transform slotKosong = wall.DapatkanSlotKosong();
            if (slotKosong != null)
            {
                objekPotTangan.transform.SetParent(slotKosong);
                objekPotTangan.transform.localPosition = Vector3.zero;
                
                Pot potScript = objekPotTangan.GetComponent<Pot>();
                potScript.TataDiWall();
                
                sedangBawaPot = false;
                Debug.Log("Pot berhasil ditata di Wall Gardenia!");
            }
        }
    }

    public void InteraksiDenganPotDiDinding(Pot potTarget)
    {
        if (!potTarget.tertataDiWall) return;

        // Tahap 1: Taruh Bibit
        if (sedangBawaBibit && !potTarget.memilikiBibit)
        {
            potTarget.IsiBibit();
            // Spawn sistem pertumbuhan tanaman
            Instantiate(prefabTanamanInti, potTarget.tempatTumbuh.position, Quaternion.identity, potTarget.tempatTumbuh);
            sedangBawaBibit = false;
            Debug.Log("Bibit dimasukkan ke dalam pot!");
            return;
        }

        // Ambil komponen tanaman untuk aksi lanjutan
        Tanaman tanaman = potTarget.GetComponentInChildren<Tanaman>();
        if (tanaman == null) return;

        // Tahap 2: Beri Pupuk
        if (sedangBawaPupuk)
        {
            tanaman.BeriPupuk();
            return;
        }

        // Tahap 3: Siram Air
        if (sedangBawaPenyiram)
        {
            tanaman.Siram();
            return;
        }
        
        // Tahap Akhir: Panen
        if (tanaman.isSiapPanen)
        {
            Debug.Log("Memanen hasil tanaman!");
            Destroy(tanaman.gameObject);
            potTarget.KosongkanPot();
        }
    }
}
