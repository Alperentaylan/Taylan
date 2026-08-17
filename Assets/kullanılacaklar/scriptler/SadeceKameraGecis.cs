using UnityEngine;

/// <summary>
/// Yalnızca iki kamera arasında geçiş yapar.
/// Hareket, Animator, animasyonlar, etkileşimler veya karakter dönüşüne dokunmaz.
/// </summary>
[DisallowMultipleComponent]
public sealed class SadeceKameraGecis : MonoBehaviour
{
    [Header("Kameralar")]
    [SerializeField] private Camera ucuncuSahisKamerasi;
    [SerializeField] private Camera birinciSahisKamerasi;

    [Header("Ayar")]
    [SerializeField] private KeyCode gecisTusu = KeyCode.V;
    [SerializeField] private bool birinciSahislaBasla = false;

    private bool birinciSahisAktif;

    private AudioListener ucuncuSahisListener;
    private AudioListener birinciSahisListener;

    private void Awake()
    {
        if (ucuncuSahisKamerasi == null ||
            birinciSahisKamerasi == null)
        {
            Debug.LogError(
                "SadeceKameraGecis: İki kamera alanını da doldur.",
                this);

            enabled = false;
            return;
        }

        ucuncuSahisListener =
            ucuncuSahisKamerasi.GetComponent<AudioListener>();

        birinciSahisListener =
            birinciSahisKamerasi.GetComponent<AudioListener>();

        KameraDegistir(birinciSahislaBasla);
    }

    private void Update()
    {
        if (Input.GetKeyDown(gecisTusu))
        {
            KameraDegistir(!birinciSahisAktif);
        }
    }

    private void KameraDegistir(bool birinciSahisaGec)
    {
        birinciSahisAktif = birinciSahisaGec;

        // Yalnızca Camera componentlerini açıp kapatır.
        // Kamera GameObjectlerini kapatmaz; diğer scriptlere dokunmaz.
        ucuncuSahisKamerasi.enabled = !birinciSahisAktif;
        birinciSahisKamerasi.enabled = birinciSahisAktif;

        if (ucuncuSahisListener != null)
            ucuncuSahisListener.enabled = !birinciSahisAktif;

        if (birinciSahisListener != null)
            birinciSahisListener.enabled = birinciSahisAktif;

        ucuncuSahisKamerasi.tag =
            birinciSahisAktif ? "Untagged" : "MainCamera";

        birinciSahisKamerasi.tag =
            birinciSahisAktif ? "MainCamera" : "Untagged";
    }
}
