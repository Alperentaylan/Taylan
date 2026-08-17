using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/*
 * TVStand hazır kliplerindeki binding yolları model hiyerarşisiyle
 * eşleşmediği için kapakları doğrudan kendi menteşe gruplarından açar.
 * Ekrandaki beyaz crosshair hangi kapağa değerse yalnız o kapak yeşil
 * vurgulanır ve E tuşu yalnız seçili kapağı açıp kapatır.
 */
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public class TVStandKapakSistemi : MonoBehaviour
{
    [Header("Kapak Meshleri")]
    [Tooltip("Hierarchy: grp_doorsL altındaki tvStandDoor_L")]
    public Transform kapak1Nesnesi;

    [Tooltip("Hierarchy: grp_doorsR altındaki tvStandDoor_R")]
    public Transform kapak2Nesnesi;

    [Header("Menteşe Grupları - Boş Bırakılabilir")]
    [Tooltip("Boşsa grp_doorsL otomatik bulunur.")]
    public Transform solKapakMentesesi;

    [Tooltip("Boşsa grp_doorsR otomatik bulunur.")]
    public Transform sagKapakMentesesi;

    [Header("Açılma Yönleri")]
    [Range(-160f, 160f)]
    public float solKapakAcisi = -105f;

    [Range(-160f, 160f)]
    public float sagKapakAcisi = 105f;

    [Range(0.15f, 2f)]
    public float animasyonSuresi = 0.55f;

    [Range(0.25f, 3f)]
    public float animasyonHizi = 1f;

    public bool solKapakBaslangictaAcik;
    public bool sagKapakBaslangictaAcik;

    [Header("Crosshair ile Bakış")]
    [Tooltip("Boşsa aktif oyun kamerası otomatik bulunur.")]
    public Camera bakisKamerasi;

    public KeyCode etkilesimTusu = KeyCode.E;

    [Tooltip(
        "Yakınlık alanı değildir; crosshair ışınının erişebileceği azami mesafedir.")]
    [Range(5f, 200f)]
    public float bakisMesafesi = 100f;

    public LayerMask bakisKatmanlari = ~0;

    [Tooltip("Crosshair ekran merkezinden kaç piksel yukarıda olsun.")]
    [Range(-250f, 300f)]
    public float crosshairDikeyKaydirma = 70f;

    [Range(6f, 30f)]
    public float crosshairBoyutu = 14f;

    [Range(1f, 5f)]
    public float crosshairKalinligi = 2f;

    [Header("Bakılan Kapağın Yeşil Vurgusu")]
    public Color vurguRengi =
        new Color(0.10f, 1f, 0.25f, 1f);

    [Range(0.004f, 0.04f)]
    public float vurguKalinligi = 0.012f;

    [Range(0f, 0.05f)]
    public float vurguBoslugu = 0.012f;

    [Range(1f, 8f)]
    public float vurguParlakligi = 4f;

    [Range(0f, 10f)]
    public float vurguNabizHizi = 4f;

    [Header("Çakışma Önleme")]
    [Tooltip(
        "Eski TVStand Animator/Animation bileşenlerini kapatır. " +
        "Hazır klipler bu modelde eşleşmediği için açık bırak.")]
    public bool mevcutAnimatorleriDevreDisiBirak = true;

    [Header("CANLI TEST - Play Modunda Bak")]
    [SerializeField]
    private string canliDurum = "Oyun henüz başlamadı";

    [SerializeField]
    private string bakilanNesne = "-";

    [SerializeField]
    private bool solKapakAcik;

    [SerializeField]
    private bool sagKapakAcik;

    [SerializeField]
    private bool solKapakAnimasyonda;

    [SerializeField]
    private bool sagKapakAnimasyonda;

    private enum KapakSecimi
    {
        Yok = -1,
        Sol = 0,
        Sag = 1
    }

    private KapakSecimi seciliKapak = KapakSecimi.Yok;

    private readonly Transform[] kapakMeshleri = new Transform[2];
    private readonly Transform[] kapakPivotleri = new Transform[2];
    private readonly Collider[] kapakColliderlari = new Collider[2];
    private readonly Quaternion[] kapaliRotasyonlar = new Quaternion[2];
    private readonly Quaternion[] acikRotasyonlar = new Quaternion[2];
    private readonly Quaternion[] animasyonBaslangiclari =
        new Quaternion[2];
    private readonly Quaternion[] animasyonHedefleri =
        new Quaternion[2];
    private readonly bool[] kapakAcikDurumlari = new bool[2];
    private readonly bool[] kapakAnimasyonlari = new bool[2];
    private readonly float[] kapakAnimasyonZamanlari = new float[2];

    private Renderer[] vurguParcalari;
    private Material vurguMateryali;
    private GUIStyle mesajStili;
    private bool sistemHazir;

    void Awake()
    {
        EskiOynaticilariKapat();
    }

    void Start()
    {
        KamerayiBul();
        KapakNesneleriniBul();

        kapakMeshleri[0] = KapakMeshiniCoz(kapak1Nesnesi);
        kapakMeshleri[1] = KapakMeshiniCoz(kapak2Nesnesi);

        if (kapakMeshleri[0] == null || kapakMeshleri[1] == null)
        {
            canliDurum =
                "HATA: tvStandDoor_L ve tvStandDoor_R alanlarını doldur";

            Debug.LogError(
                name +
                ": Sol veya sağ TVStand kapak meshi bulunamadı.",
                this
            );
            return;
        }

        kapak1Nesnesi = kapakMeshleri[0];
        kapak2Nesnesi = kapakMeshleri[1];

        if (solKapakMentesesi == null)
        {
            solKapakMentesesi =
                KapakMentesesiniBul(kapakMeshleri[0], true);
        }

        if (sagKapakMentesesi == null)
        {
            sagKapakMentesesi =
                KapakMentesesiniBul(kapakMeshleri[1], false);
        }

        kapakPivotleri[0] = solKapakMentesesi;
        kapakPivotleri[1] = sagKapakMentesesi;

        if (kapakPivotleri[0] == null || kapakPivotleri[1] == null)
        {
            canliDurum =
                "HATA: grp_doorsL veya grp_doorsR bulunamadı";

            Debug.LogError(
                name +
                ": Kapak menteşe grupları bulunamadı.",
                this
            );
            return;
        }

        kapaliRotasyonlar[0] = kapakPivotleri[0].localRotation;
        kapaliRotasyonlar[1] = kapakPivotleri[1].localRotation;

        acikRotasyonlar[0] = kapaliRotasyonlar[0] *
            Quaternion.Euler(0f, solKapakAcisi, 0f);
        acikRotasyonlar[1] = kapaliRotasyonlar[1] *
            Quaternion.Euler(0f, sagKapakAcisi, 0f);

        kapakAcikDurumlari[0] = solKapakBaslangictaAcik;
        kapakAcikDurumlari[1] = sagKapakBaslangictaAcik;

        kapakPivotleri[0].localRotation = kapakAcikDurumlari[0]
            ? acikRotasyonlar[0]
            : kapaliRotasyonlar[0];
        kapakPivotleri[1].localRotation = kapakAcikDurumlari[1]
            ? acikRotasyonlar[1]
            : kapaliRotasyonlar[1];

        kapakColliderlari[0] =
            RaycastCollideriHazirla(kapakMeshleri[0]);
        kapakColliderlari[1] =
            RaycastCollideriHazirla(kapakMeshleri[1]);

        VurguCerceveleriniOlustur();
        VurguyuGoster(KapakSecimi.Yok);

        sistemHazir =
            kapakColliderlari[0] != null &&
            kapakColliderlari[1] != null;

        canliDurum = sistemHazir
            ? "HAZIR: Crosshair ile sol veya sağ kapağa bak"
            : "HATA: Kapak Raycast collider'ları oluşturulamadı";

        DurumAlanlariniGuncelle();
    }

    void Update()
    {
        if (bakisKamerasi == null)
        {
            KamerayiBul();
        }
    }

    /*
     * Kamera takip kodları genelde LateUpdate'te çalıştığı için bu script
     * yüksek execution order ile kameranın son pozundan ray gönderir.
     */
    void LateUpdate()
    {
        KapakAnimasyonunuGuncelle(0);
        KapakAnimasyonunuGuncelle(1);
        DurumAlanlariniGuncelle();

        if (!sistemHazir || bakisKamerasi == null)
        {
            seciliKapak = KapakSecimi.Yok;
            VurguyuGoster(seciliKapak);
            return;
        }

        seciliKapak = CrosshairAltindakiKapagiBul();
        VurguyuGoster(seciliKapak);

        if (seciliKapak != KapakSecimi.Yok)
        {
            VurguParlakliginiGuncelle();
            bakilanNesne = seciliKapak == KapakSecimi.Sol
                ? "Sol kapak"
                : "Sağ kapak";

            int indeks = (int)seciliKapak;
            canliDurum = kapakAnimasyonlari[indeks]
                ? "BEKLE: Seçili kapak hareket ediyor"
                : kapakAcikDurumlari[indeks]
                    ? "HAZIR: E ile seçili kapağı kapat"
                    : "HAZIR: E ile seçili kapağı aç";

            if (!kapakAnimasyonlari[indeks] &&
                EtkilesimTusunaBasildi())
            {
                KapakAnimasyonunuBaslat(indeks);
                VurguyuGoster(KapakSecimi.Yok);
            }
        }
        else
        {
            bakilanNesne = "-";
            canliDurum =
                "BAKIŞ YOK: Crosshair'i bir kapağın üzerine getir";
        }
    }

    private bool EtkilesimTusunaBasildi()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard klavye = Keyboard.current;

        if (klavye != null)
        {
            Key yeniTus;

            if (System.Enum.TryParse(
                    etkilesimTusu.ToString(),
                    true,
                    out yeniTus) &&
                klavye[yeniTus].wasPressedThisFrame)
            {
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(etkilesimTusu);
#else
        return false;
#endif
    }

    private void KapakAnimasyonunuBaslat(int indeks)
    {
        if (indeks < 0 || indeks > 1 || kapakPivotleri[indeks] == null)
        {
            return;
        }

        bool hedefAcik = !kapakAcikDurumlari[indeks];

        animasyonBaslangiclari[indeks] =
            kapakPivotleri[indeks].localRotation;
        animasyonHedefleri[indeks] = hedefAcik
            ? acikRotasyonlar[indeks]
            : kapaliRotasyonlar[indeks];

        kapakAcikDurumlari[indeks] = hedefAcik;
        kapakAnimasyonZamanlari[indeks] = 0f;
        kapakAnimasyonlari[indeks] = true;
    }

    private void KapakAnimasyonunuGuncelle(int indeks)
    {
        if (!kapakAnimasyonlari[indeks] ||
            kapakPivotleri[indeks] == null)
        {
            return;
        }

        float gercekSure = animasyonSuresi /
            Mathf.Max(0.01f, animasyonHizi);

        kapakAnimasyonZamanlari[indeks] +=
            Time.unscaledDeltaTime;

        float oran = Mathf.Clamp01(
            kapakAnimasyonZamanlari[indeks] /
            Mathf.Max(0.01f, gercekSure)
        );

        oran = oran * oran * (3f - 2f * oran);

        kapakPivotleri[indeks].localRotation =
            Quaternion.Slerp(
                animasyonBaslangiclari[indeks],
                animasyonHedefleri[indeks],
                oran
            );

        if (kapakAnimasyonZamanlari[indeks] >= gercekSure)
        {
            kapakPivotleri[indeks].localRotation =
                animasyonHedefleri[indeks];
            kapakAnimasyonlari[indeks] = false;
        }
    }

    private KapakSecimi CrosshairAltindakiKapagiBul()
    {
        Vector3 ekranNoktasi = CrosshairEkranNoktasiniAl();
        Ray bakisIsini = bakisKamerasi.ScreenPointToRay(ekranNoktasi);

        RaycastHit vurus;

        if (!Physics.Raycast(
                bakisIsini,
                out vurus,
                bakisMesafesi,
                bakisKatmanlari,
                QueryTriggerInteraction.Collide))
        {
            return KapakSecimi.Yok;
        }

        if (vurus.collider == kapakColliderlari[0] ||
            DonusumKapagaAitMi(vurus.transform, 0))
        {
            return KapakSecimi.Sol;
        }

        if (vurus.collider == kapakColliderlari[1] ||
            DonusumKapagaAitMi(vurus.transform, 1))
        {
            return KapakSecimi.Sag;
        }

        return KapakSecimi.Yok;
    }

    private bool DonusumKapagaAitMi(Transform hedef, int indeks)
    {
        if (hedef == null || kapakMeshleri[indeks] == null)
        {
            return false;
        }

        return hedef == kapakMeshleri[indeks] ||
            hedef.IsChildOf(kapakMeshleri[indeks]);
    }

    private Vector3 CrosshairEkranNoktasiniAl()
    {
        return new Vector3(
            Screen.width * 0.5f,
            Screen.height * 0.5f + crosshairDikeyKaydirma,
            0f
        );
    }

    private void KamerayiBul()
    {
        if (bakisKamerasi != null &&
            bakisKamerasi.enabled &&
            bakisKamerasi.gameObject.activeInHierarchy)
        {
            return;
        }

        bakisKamerasi = Camera.main;

        if (bakisKamerasi != null)
        {
            return;
        }

        Camera[] kameralar =
            FindObjectsByType<Camera>(FindObjectsSortMode.None);

        float enYuksekDerinlik = float.NegativeInfinity;

        for (int i = 0; i < kameralar.Length; i++)
        {
            Camera aday = kameralar[i];

            if (aday == null ||
                !aday.enabled ||
                !aday.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (aday.depth >= enYuksekDerinlik)
            {
                enYuksekDerinlik = aday.depth;
                bakisKamerasi = aday;
            }
        }
    }

    private void KapakNesneleriniBul()
    {
        if (kapak1Nesnesi == null)
        {
            kapak1Nesnesi = AdaGoreKapakBul(true);
        }

        if (kapak2Nesnesi == null)
        {
            kapak2Nesnesi = AdaGoreKapakBul(false);
        }
    }

    private Transform AdaGoreKapakBul(bool sol)
    {
        MeshFilter[] meshler =
            GetComponentsInChildren<MeshFilter>(true);

        for (int i = 0; i < meshler.Length; i++)
        {
            if (meshler[i] == null)
            {
                continue;
            }

            string ad = meshler[i].name.ToLowerInvariant();

            bool eslesme = sol
                ? ad.Contains("door_l") ||
                  ad.Contains("doorsl") ||
                  ad.Contains("door01")
                : ad.Contains("door_r") ||
                  ad.Contains("doorsr") ||
                  ad.Contains("door02");

            if (eslesme)
            {
                return meshler[i].transform;
            }
        }

        return null;
    }

    private Transform KapakMeshiniCoz(Transform verilen)
    {
        if (verilen == null)
        {
            return null;
        }

        MeshFilter meshFiltresi = verilen.GetComponent<MeshFilter>();

        if (meshFiltresi == null)
        {
            meshFiltresi = verilen.GetComponentInChildren<MeshFilter>(true);
        }

        return meshFiltresi != null
            ? meshFiltresi.transform
            : null;
    }

    private Transform KapakMentesesiniBul(
        Transform kapakMesh,
        bool sol)
    {
        if (kapakMesh == null)
        {
            return null;
        }

        Transform aday = kapakMesh.parent;

        while (aday != null && aday != transform.parent)
        {
            string ad = aday.name.ToLowerInvariant();

            bool eslesme = sol
                ? ad.Contains("doorsl") ||
                  ad.Contains("door_l") ||
                  ad.Contains("door01")
                : ad.Contains("doorsr") ||
                  ad.Contains("door_r") ||
                  ad.Contains("door02");

            if (eslesme)
            {
                return aday;
            }

            if (aday == transform)
            {
                break;
            }

            aday = aday.parent;
        }

        return kapakMesh.parent != null
            ? kapakMesh.parent
            : kapakMesh;
    }

    private Collider RaycastCollideriHazirla(Transform kapakMesh)
    {
        if (kapakMesh == null)
        {
            return null;
        }

        Collider mevcutCollider = kapakMesh.GetComponent<Collider>();

        if (mevcutCollider != null)
        {
            return mevcutCollider;
        }

        MeshFilter meshFiltresi = kapakMesh.GetComponent<MeshFilter>();

        if (meshFiltresi == null || meshFiltresi.sharedMesh == null)
        {
            return null;
        }

        Bounds sinir = meshFiltresi.sharedMesh.bounds;
        BoxCollider kutu = kapakMesh.gameObject.AddComponent<BoxCollider>();
        kutu.center = sinir.center;
        kutu.size = new Vector3(
            Mathf.Max(0.02f, sinir.size.x),
            Mathf.Max(0.02f, sinir.size.y),
            Mathf.Max(0.04f, sinir.size.z)
        );
        kutu.isTrigger = true;
        return kutu;
    }

    private void EskiOynaticilariKapat()
    {
        if (!mevcutAnimatorleriDevreDisiBirak)
        {
            return;
        }

        Animator[] animatorler =
            GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animatorler.Length; i++)
        {
            if (animatorler[i] != null)
            {
                animatorler[i].enabled = false;
            }
        }

        Animation[] animationlar =
            GetComponentsInChildren<Animation>(true);

        for (int i = 0; i < animationlar.Length; i++)
        {
            if (animationlar[i] != null)
            {
                animationlar[i].enabled = false;
            }
        }
    }

    private void VurguCerceveleriniOlustur()
    {
        vurguMateryali = VurguMateryaliOlustur();
        vurguParcalari = new Renderer[16];

        KapakCercevesiniOlustur(kapakMeshleri[0], 0, "Sol Kapak");
        KapakCercevesiniOlustur(kapakMeshleri[1], 8, "Sağ Kapak");
    }

    private void KapakCercevesiniOlustur(
        Transform kapakMesh,
        int baslangicIndeksi,
        string kapakAdi)
    {
        MeshFilter meshFiltresi = kapakMesh != null
            ? kapakMesh.GetComponent<MeshFilter>()
            : null;

        if (meshFiltresi == null || meshFiltresi.sharedMesh == null)
        {
            return;
        }

        Bounds sinir = meshFiltresi.sharedMesh.bounds;
        float kalinlik = Mathf.Max(0.004f, vurguKalinligi);
        float genislik = Mathf.Max(0.03f, sinir.size.x) +
            vurguBoslugu * 2f;
        float yukseklik = Mathf.Max(0.03f, sinir.size.y) +
            vurguBoslugu * 2f;
        float derinlik = Mathf.Max(0.006f, sinir.size.z * 0.08f);

        CerceveTarafiniOlustur(
            kapakMesh,
            sinir,
            baslangicIndeksi,
            kapakAdi + " Ön",
            1f,
            genislik,
            yukseklik,
            kalinlik,
            derinlik
        );

        CerceveTarafiniOlustur(
            kapakMesh,
            sinir,
            baslangicIndeksi + 4,
            kapakAdi + " Arka",
            -1f,
            genislik,
            yukseklik,
            kalinlik,
            derinlik
        );
    }

    private void CerceveTarafiniOlustur(
        Transform cerceveKoku,
        Bounds sinir,
        int indeks,
        string ad,
        float taraf,
        float genislik,
        float yukseklik,
        float kalinlik,
        float derinlik)
    {
        float yuzZ = sinir.center.z +
            taraf * (sinir.extents.z + derinlik);

        vurguParcalari[indeks] = CerceveParcasiOlustur(
            cerceveKoku,
            ad + " Üst",
            new Vector3(
                sinir.center.x,
                sinir.center.y + yukseklik * 0.5f,
                yuzZ
            ),
            new Vector3(genislik, kalinlik, derinlik)
        );

        vurguParcalari[indeks + 1] = CerceveParcasiOlustur(
            cerceveKoku,
            ad + " Alt",
            new Vector3(
                sinir.center.x,
                sinir.center.y - yukseklik * 0.5f,
                yuzZ
            ),
            new Vector3(genislik, kalinlik, derinlik)
        );

        vurguParcalari[indeks + 2] = CerceveParcasiOlustur(
            cerceveKoku,
            ad + " Sol",
            new Vector3(
                sinir.center.x - genislik * 0.5f,
                sinir.center.y,
                yuzZ
            ),
            new Vector3(kalinlik, yukseklik, derinlik)
        );

        vurguParcalari[indeks + 3] = CerceveParcasiOlustur(
            cerceveKoku,
            ad + " Sağ",
            new Vector3(
                sinir.center.x + genislik * 0.5f,
                sinir.center.y,
                yuzZ
            ),
            new Vector3(kalinlik, yukseklik, derinlik)
        );
    }

    private Renderer CerceveParcasiOlustur(
        Transform cerceveKoku,
        string parcaAdi,
        Vector3 yerelKonum,
        Vector3 yerelBoyut)
    {
        GameObject parca =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        parca.name = parcaAdi;

        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        parca.layer = ignoreRaycast >= 0
            ? ignoreRaycast
            : cerceveKoku.gameObject.layer;

        parca.transform.SetParent(cerceveKoku, false);
        parca.transform.localPosition = yerelKonum;
        parca.transform.localRotation = Quaternion.identity;
        parca.transform.localScale = yerelBoyut;

        Collider gereksizCollider = parca.GetComponent<Collider>();

        if (gereksizCollider != null)
        {
            Destroy(gereksizCollider);
        }

        Renderer renderer = parca.GetComponent<Renderer>();
        renderer.sharedMaterial = vurguMateryali;
        renderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private Material VurguMateryaliOlustur()
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
        materyal.name = "TVStand Crosshair Yeşil Vurgu";
        vurguMateryali = materyal;
        VurguRenginiUygula(vurguParlakligi);
        return materyal;
    }

    private void VurguParlakliginiGuncelle()
    {
        float nabiz = 0.86f +
            Mathf.Sin(Time.unscaledTime * vurguNabizHizi) * 0.14f;

        VurguRenginiUygula(vurguParlakligi * nabiz);
    }

    private void VurguRenginiUygula(float carpan)
    {
        if (vurguMateryali == null)
        {
            return;
        }

        Color parlakRenk =
            vurguRengi * Mathf.Max(1f, carpan);
        parlakRenk.a = vurguRengi.a;

        if (vurguMateryali.HasProperty("_BaseColor"))
        {
            vurguMateryali.SetColor("_BaseColor", parlakRenk);
        }

        if (vurguMateryali.HasProperty("_Color"))
        {
            vurguMateryali.SetColor("_Color", parlakRenk);
        }

        if (vurguMateryali.HasProperty("_EmissionColor"))
        {
            vurguMateryali.EnableKeyword("_EMISSION");
            vurguMateryali.SetColor("_EmissionColor", parlakRenk);
        }
    }

    private void VurguyuGoster(KapakSecimi secim)
    {
        if (vurguParcalari == null)
        {
            return;
        }

        for (int i = 0; i < vurguParcalari.Length; i++)
        {
            if (vurguParcalari[i] == null)
            {
                continue;
            }

            bool solParca = i < 8;
            vurguParcalari[i].enabled =
                secim == KapakSecimi.Sol && solParca ||
                secim == KapakSecimi.Sag && !solParca;
        }
    }

    private void DurumAlanlariniGuncelle()
    {
        solKapakAcik = kapakAcikDurumlari[0];
        sagKapakAcik = kapakAcikDurumlari[1];
        solKapakAnimasyonda = kapakAnimasyonlari[0];
        sagKapakAnimasyonda = kapakAnimasyonlari[1];
    }

    void OnGUI()
    {
        CrosshairCiz();

        if (seciliKapak == KapakSecimi.Yok || !sistemHazir)
        {
            return;
        }

        int indeks = (int)seciliKapak;

        if (mesajStili == null)
        {
            mesajStili = new GUIStyle(GUI.skin.label);
            mesajStili.richText = true;
            mesajStili.fontSize = 22;
            mesajStili.normal.textColor = Color.white;
            mesajStili.alignment = TextAnchor.MiddleLeft;
        }

        GUI.Box(
            new Rect(20f, 20f, 570f, 54f),
            GUIContent.none
        );

        string taraf = seciliKapak == KapakSecimi.Sol
            ? "sol kapağı"
            : "sağ kapağı";

        string eylem = kapakAcikDurumlari[indeks]
            ? "kapatmak"
            : "açmak";

        string tusAdi = etkilesimTusu.ToString();
        string mesaj = taraf + " " + eylem + " için " +
            "<color=#FFC01A><b>" + tusAdi +
            "</b></color> tuşuna basın";

        GUI.Label(
            new Rect(36f, 20f, 540f, 54f),
            mesaj,
            mesajStili
        );
    }

    private void CrosshairCiz()
    {
        Vector3 ekranNoktasi = CrosshairEkranNoktasiniAl();
        float merkezX = ekranNoktasi.x;
        float merkezY = Screen.height - ekranNoktasi.y;

        Color oncekiRenk = GUI.color;
        GUI.color = Color.white;

        GUI.DrawTexture(
            new Rect(
                merkezX - crosshairBoyutu * 0.5f,
                merkezY - crosshairKalinligi * 0.5f,
                crosshairBoyutu,
                crosshairKalinligi
            ),
            Texture2D.whiteTexture
        );

        GUI.DrawTexture(
            new Rect(
                merkezX - crosshairKalinligi * 0.5f,
                merkezY - crosshairBoyutu * 0.5f,
                crosshairKalinligi,
                crosshairBoyutu
            ),
            Texture2D.whiteTexture
        );

        GUI.color = oncekiRenk;
    }

    void OnDisable()
    {
        seciliKapak = KapakSecimi.Yok;
        VurguyuGoster(seciliKapak);
    }

    void OnDestroy()
    {
        if (vurguMateryali != null)
        {
            Destroy(vurguMateryali);
        }
    }

    void OnValidate()
    {
        animasyonSuresi = Mathf.Clamp(animasyonSuresi, 0.15f, 2f);
        animasyonHizi = Mathf.Clamp(animasyonHizi, 0.25f, 3f);
        bakisMesafesi = Mathf.Clamp(bakisMesafesi, 5f, 200f);
        crosshairBoyutu = Mathf.Clamp(crosshairBoyutu, 6f, 30f);
        crosshairKalinligi = Mathf.Clamp(crosshairKalinligi, 1f, 5f);
    }
}