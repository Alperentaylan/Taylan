using UnityEngine;

[DisallowMultipleComponent]
public class KesinIsikDugmesi : MonoBehaviour
{
    [Header("Kontrol Edilecek Işıklar")]
    public Light[] kontrolEdilenIsiklar;

    [Header("Etkileşim")]
    public Transform oyuncu;
    public KeyCode etkilesimTusu = KeyCode.E;

    [Tooltip("Sadece yatay uzaklık ölçülür.")]
    public float gercekEtkilesimMesafesi = 1.5f;

    [Header("Sadece Düğme Çerçevesi")]
    public Color cerceveRengi =
        new Color(1f, 0.55f, 0.05f, 1f);
    public float cerceveKalinligi = 0.012f;
    public float cerceveBoslugu = 0.018f;
    [Range(1f, 8f)]
    public float cerceveParlakligi = 4f;
    [Range(0f, 10f)]
    public float cerceveNabizHizi = 4f;

    [Header("CANLI TEST - Play Modunda Bak")]
    [SerializeField]
    private string canliDurum = "Oyun henüz başlamadı";

    [SerializeField]
    private float yatayMesafe;

    private CharacterController oyuncuController;
    private Collider dugmeCollideri;
    private Renderer[] cerceveParcalari;
    private Material cerceveMateryali;
    private bool oyuncuYakinda;
    private bool isiklarAcik;
    private GUIStyle mesajStili;

    void Awake()
    {
        dugmeCollideri = GetComponent<Collider>();
    }

    void Start()
    {
        OyuncuyuBul();
        IsikDurumunuBul();
        CerceveyiOlustur();
        CerceveyiGoster(false);
    }

    void Update()
    {
        if (oyuncu == null)
        {
            OyuncuyuBul();
        }

        if (oyuncu == null)
        {
            oyuncuYakinda = false;
            canliDurum =
                "OYUNCU BULUNAMADI: CharacterController kontrol et";
            CerceveyiGoster(false);
            return;
        }

        Vector3 dugmeDunyaNoktasi = dugmeCollideri != null
            ? dugmeCollideri.bounds.center
            : transform.position;

        Vector2 oyuncuXZ = new Vector2(
            oyuncu.position.x,
            oyuncu.position.z
        );

        Vector2 dugmeXZ = new Vector2(
            dugmeDunyaNoktasi.x,
            dugmeDunyaNoktasi.z
        );

        yatayMesafe = Vector2.Distance(oyuncuXZ, dugmeXZ);

        oyuncuYakinda =
            yatayMesafe <= gercekEtkilesimMesafesi;

        CerceveyiGoster(oyuncuYakinda);

        if (oyuncuYakinda)
        {
            CerceveParlakliginiGuncelle();
        }

        canliDurum = oyuncuYakinda
            ? "HAZIR: E tuşuna bas"
            : "UZAK: Düğmeye yaklaş";

        if (oyuncuYakinda &&
            Input.GetKeyDown(etkilesimTusu))
        {
            IsiklariDegistir();
        }
    }

    private void OyuncuyuBul()
    {
        CharacterController[] adaylar =
            FindObjectsByType<CharacterController>(
                FindObjectsSortMode.None
            );

        CharacterController enIyiAday = null;
        float enIyiPuan = float.PositiveInfinity;
        Camera anaKamera = Camera.main;

        for (int i = 0; i < adaylar.Length; i++)
        {
            CharacterController aday = adaylar[i];

            if (aday == null ||
                !aday.enabled ||
                !aday.gameObject.activeInHierarchy)
            {
                continue;
            }

            MonoBehaviour hareketKodu =
                aday.GetComponent("KarakterHareketi")
                as MonoBehaviour;

            float puan =
                hareketKodu != null && hareketKodu.enabled
                    ? 0f
                    : 10000f;

            if (anaKamera != null)
            {
                puan += (
                    aday.transform.position -
                    anaKamera.transform.position
                ).sqrMagnitude;
            }

            if (puan < enIyiPuan)
            {
                enIyiPuan = puan;
                enIyiAday = aday;
            }
        }

        if (enIyiAday != null)
        {
            oyuncuController = enIyiAday;
            oyuncu = oyuncuController.transform;
            return;
        }

        if (oyuncu != null)
        {
            oyuncuController =
                oyuncu.GetComponent<CharacterController>();
        }
    }

    private void IsikDurumunuBul()
    {
        isiklarAcik = false;

        if (kontrolEdilenIsiklar == null)
        {
            return;
        }

        foreach (Light isik in kontrolEdilenIsiklar)
        {
            if (isik != null)
            {
                isiklarAcik = isik.enabled;
                return;
            }
        }
    }

    private void IsiklariDegistir()
    {
        isiklarAcik = !isiklarAcik;

        if (kontrolEdilenIsiklar != null)
        {
            foreach (Light isik in kontrolEdilenIsiklar)
            {
                if (isik != null)
                {
                    isik.enabled = isiklarAcik;
                }
            }
        }

        canliDurum = isiklarAcik
            ? "ÇALIŞTI: Işıklar açıldı"
            : "ÇALIŞTI: Işıklar kapandı";
    }

    private void CerceveyiOlustur()
    {
        BoxCollider kutu = GetComponent<BoxCollider>();

        Vector3 merkez = kutu != null
            ? kutu.center
            : Vector3.zero;

        Vector3 boyut = kutu != null
            ? kutu.size
            : new Vector3(0.1f, 0.15f, 0.02f);

        float kalinlik = Mathf.Max(
            0.004f,
            cerceveKalinligi
        );

        float genislik = boyut.x + cerceveBoslugu * 2f;
        float yukseklik = boyut.y + cerceveBoslugu * 2f;
        float derinlik = Mathf.Max(0.006f, boyut.z * 0.5f);

        cerceveMateryali = CerceveMateryaliOlustur();
        cerceveParcalari = new Renderer[8];

        /*
         * Bazı düğmelerin yerel Z yönü ters. Çerçeveyi iki yüze de
         * kurarak duvarın arkasında kalma ihtimalini tamamen kaldır.
         */
        CerceveTarafiniOlustur(
            0,
            "Ön",
            1f,
            merkez,
            boyut,
            genislik,
            yukseklik,
            kalinlik,
            derinlik
        );

        CerceveTarafiniOlustur(
            4,
            "Arka",
            -1f,
            merkez,
            boyut,
            genislik,
            yukseklik,
            kalinlik,
            derinlik
        );
    }

    private void CerceveTarafiniOlustur(
        int baslangicIndeksi,
        string tarafAdi,
        float taraf,
        Vector3 merkez,
        Vector3 boyut,
        float genislik,
        float yukseklik,
        float kalinlik,
        float derinlik)
    {
        float yuzZ =
            merkez.z +
            taraf * (boyut.z * 0.5f + derinlik * 0.8f);

        cerceveParcalari[baslangicIndeksi] =
            CerceveParcasiOlustur(
                "Çerçeve " + tarafAdi + " Üst",
                new Vector3(
                    merkez.x,
                    merkez.y + yukseklik * 0.5f,
                    yuzZ
                ),
                new Vector3(
                    genislik + kalinlik,
                    kalinlik,
                    derinlik
                )
            );

        cerceveParcalari[baslangicIndeksi + 1] =
            CerceveParcasiOlustur(
                "Çerçeve " + tarafAdi + " Alt",
                new Vector3(
                    merkez.x,
                    merkez.y - yukseklik * 0.5f,
                    yuzZ
                ),
                new Vector3(
                    genislik + kalinlik,
                    kalinlik,
                    derinlik
                )
            );

        cerceveParcalari[baslangicIndeksi + 2] =
            CerceveParcasiOlustur(
                "Çerçeve " + tarafAdi + " Sol",
                new Vector3(
                    merkez.x - genislik * 0.5f,
                    merkez.y,
                    yuzZ
                ),
                new Vector3(
                    kalinlik,
                    yukseklik + kalinlik,
                    derinlik
                )
            );

        cerceveParcalari[baslangicIndeksi + 3] =
            CerceveParcasiOlustur(
                "Çerçeve " + tarafAdi + " Sağ",
                new Vector3(
                    merkez.x + genislik * 0.5f,
                    merkez.y,
                    yuzZ
                ),
                new Vector3(
                    kalinlik,
                    yukseklik + kalinlik,
                    derinlik
                )
            );
    }

    private Renderer CerceveParcasiOlustur(
        string parcaAdi,
        Vector3 yerelKonum,
        Vector3 yerelBoyut)
    {
        GameObject parca =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        parca.name = parcaAdi;
        parca.transform.SetParent(transform, false);
        parca.transform.localPosition = yerelKonum;
        parca.transform.localRotation = Quaternion.identity;
        parca.transform.localScale = yerelBoyut;

        Collider gereksizCollider =
            parca.GetComponent<Collider>();

        if (gereksizCollider != null)
        {
            Destroy(gereksizCollider);
        }

        Renderer parcaRendereri =
            parca.GetComponent<Renderer>();

        parcaRendereri.sharedMaterial = cerceveMateryali;
        parcaRendereri.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        parcaRendereri.receiveShadows = false;

        return parcaRendereri;
    }

    private Material CerceveMateryaliOlustur()
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Unlit"
        );

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material materyal = new Material(shader);
        materyal.name = "Düğme Vurgu Çerçevesi";

        cerceveMateryali = materyal;
        CerceveRenginiUygula(cerceveParlakligi);

        return materyal;
    }

    private void CerceveParlakliginiGuncelle()
    {
        float nabiz = 0.86f +
            Mathf.Sin(Time.unscaledTime * cerceveNabizHizi) *
            0.14f;

        CerceveRenginiUygula(
            cerceveParlakligi * nabiz
        );
    }

    private void CerceveRenginiUygula(float carpan)
    {
        if (cerceveMateryali == null)
        {
            return;
        }

        Color parlakRenk =
            cerceveRengi * Mathf.Max(1f, carpan);
        parlakRenk.a = cerceveRengi.a;

        if (cerceveMateryali.HasProperty("_BaseColor"))
        {
            cerceveMateryali.SetColor(
                "_BaseColor",
                parlakRenk
            );
        }

        if (cerceveMateryali.HasProperty("_Color"))
        {
            cerceveMateryali.SetColor("_Color", parlakRenk);
        }

        if (cerceveMateryali.HasProperty("_EmissionColor"))
        {
            cerceveMateryali.EnableKeyword("_EMISSION");
            cerceveMateryali.SetColor(
                "_EmissionColor",
                parlakRenk
            );
        }
    }

    private void CerceveyiGoster(bool goster)
    {
        if (cerceveParcalari == null)
        {
            return;
        }

        foreach (Renderer parca in cerceveParcalari)
        {
            if (parca != null)
            {
                parca.enabled = goster;
            }
        }
    }

    void OnGUI()
    {
        if (!oyuncuYakinda)
        {
            return;
        }

        if (mesajStili == null)
        {
            mesajStili = new GUIStyle(GUI.skin.label);
            mesajStili.richText = true;
            mesajStili.fontSize = 22;
            mesajStili.normal.textColor = Color.white;
            mesajStili.alignment = TextAnchor.MiddleLeft;
        }

        Rect arkaPlan = new Rect(20f, 20f, 500f, 54f);
        GUI.Box(arkaPlan, GUIContent.none);

        string mesaj = isiklarAcik
            ? "Işıkları söndürmek için <color=#FFC01A><b>E</b></color> tuşuna basın"
            : "Işıkları yakmak için <color=#FFC01A><b>E</b></color> tuşuna basın";

        GUI.Label(
            new Rect(36f, 20f, 470f, 54f),
            mesaj,
            mesajStili
        );
    }

    void OnDisable()
    {
        oyuncuYakinda = false;
        CerceveyiGoster(false);
    }

    void OnDestroy()
    {
        if (cerceveMateryali != null)
        {
            Destroy(cerceveMateryali);
        }
    }
}