using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GTA benzeri, sahneyi otomatik tarayan mini/buyuk harita.
/// Road/Sokak/Cadde isimli nesnelerin gercek boyutlarindan yol grafigi kurar.
/// Gorev rotasi bu grafik uzerinden hesaplanir; M buyuk haritayi acar.
/// </summary>
[DefaultExecutionOrder(-8500)]
public sealed class TaylanGtaHarita : MonoBehaviour
{
    private sealed class YolParcasi
    {
        public Vector3 bas;
        public Vector3 son;
        public readonly List<Vector3> noktalar = new List<Vector3>();
    }

    private struct Kenar
    {
        public int hedef;
        public float mesafe;

        public Kenar(int yeniHedef, float yeniMesafe)
        {
            hedef = yeniHedef;
            mesafe = yeniMesafe;
        }
    }

    [Header("Harita")]
    [SerializeField] private KeyCode buyukHaritaTusu = KeyCode.M;
    [SerializeField] private float miniHaritaMenzili = 58f;
    [SerializeField] private float yolBaglantiMesafesi = 13f;
    [SerializeField] private int haritaCozunurlugu = 512;
    [SerializeField] private string varsayilanGorevNoktasi = "GTAMagazaKapisiKararliSistemi";

    private static TaylanGtaHarita ornek;
    private static Transform bekleyenHedef;
    private static string bekleyenHedefAdi = "GOREV";

    private readonly List<YolParcasi> yolParcalari = new List<YolParcasi>();
    private readonly List<Vector3> dugumler = new List<Vector3>();
    private readonly List<List<Kenar>> baglantilar = new List<List<Kenar>>();
    private readonly List<Vector3> rota = new List<Vector3>();

    private Transform oyuncu;
    private Transform gorevHedefi;
    private string gorevAdi = "GOREV";
    private Camera haritaKamerasi;
    private RenderTexture haritaDokusu;
    private Texture2D beyazDoku;
    private GUIStyle cerceveStili;
    private GUIStyle bilgiStili;
    private Bounds yolSinirlari;
    private bool yolSinirlariHazir;
    private bool oynanisAktif;
    private bool buyukHaritaAcik;
    private float sonrakiRenderZamani;
    private float sonrakiRotaZamani;
    private Vector3 sonRotaOyuncuPozisyonu;
    private Vector3 sonRotaHedefPozisyonu;
    private float oncekiZamanOlcegi = 1f;
    private CursorLockMode oncekiKilit;
    private bool oncekiImlec;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StatigiSifirla()
    {
        ornek = null;
        bekleyenHedef = null;
        bekleyenHedefAdi = "GOREV";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OtomatikKur()
    {
        if (FindAnyObjectByType<TaylanGtaHarita>() != null)
            return;

        GameObject sistem = new GameObject("Taylan - GTA Harita");
        DontDestroyOnLoad(sistem);
        sistem.AddComponent<TaylanGtaHarita>();
    }

    /// <summary>Gorev sistemi yeni hedef verdiginde bunu cagirabilir.</summary>
    public static void HedefAyarla(Transform hedef, string hedefAdi = "GOREV")
    {
        bekleyenHedef = hedef;
        bekleyenHedefAdi = string.IsNullOrWhiteSpace(hedefAdi) ? "GOREV" : hedefAdi;

        if (ornek != null)
            ornek.HedefiUygula(hedef, bekleyenHedefAdi);
    }

    public static void HedefiTemizle()
    {
        bekleyenHedef = null;
        if (ornek != null)
        {
            ornek.gorevHedefi = null;
            ornek.rota.Clear();
        }
    }

    /// <summary>Intro tamamlandiginda mini haritayi gorunur yapar.</summary>
    public static void OynanisiBaslat()
    {
        if (ornek != null)
            ornek.oynanisAktif = true;
    }

    private void Awake()
    {
        if (ornek != null && ornek != this)
        {
            Destroy(gameObject);
            return;
        }

        ornek = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SahneYuklendi;
        DokuVeKamerayiHazirla();
        StartCoroutine(SahneyiHazirla());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SahneYuklendi;
        if (buyukHaritaAcik)
            BuyukHaritayiKapat();
        if (haritaDokusu != null)
        {
            haritaDokusu.Release();
            Destroy(haritaDokusu);
        }
        if (beyazDoku != null)
            Destroy(beyazDoku);
    }

    private void SahneYuklendi(Scene sahne, LoadSceneMode mod)
    {
        StartCoroutine(SahneyiHazirla());
    }

    private IEnumerator SahneyiHazirla()
    {
        yield return null;
        yield return null;

        OyuncuyuBul();
        YolAginiKur();

        if (bekleyenHedef != null)
            HedefiUygula(bekleyenHedef, bekleyenHedefAdi);
        else
            VarsayilanHedefiBul();

        oynanisAktif = oyuncu != null && yolParcalari.Count > 0 && !SahnedeIntroVar();
        RotaHesapla();
    }

    private void Update()
    {
        if (oyuncu == null)
            OyuncuyuBul();

        if (oynanisAktif && Input.GetKeyDown(buyukHaritaTusu))
        {
            if (buyukHaritaAcik)
                BuyukHaritayiKapat();
            else
                BuyukHaritayiAc();
        }

        if (!oynanisAktif || oyuncu == null)
            return;

        if (gorevHedefi != null &&
            (Time.unscaledTime >= sonrakiRotaZamani ||
             Vector3.SqrMagnitude(oyuncu.position - sonRotaOyuncuPozisyonu) > 16f ||
             Vector3.SqrMagnitude(gorevHedefi.position - sonRotaHedefPozisyonu) > 4f))
        {
            RotaHesapla();
        }

        if (Time.unscaledTime >= sonrakiRenderZamani)
        {
            HaritayiRenderEt();
            sonrakiRenderZamani = Time.unscaledTime + (buyukHaritaAcik ? 0.05f : 0.09f);
        }
    }

    private void DokuVeKamerayiHazirla()
    {
        haritaCozunurlugu = Mathf.Clamp(haritaCozunurlugu, 256, 1024);
        haritaDokusu = new RenderTexture(haritaCozunurlugu, haritaCozunurlugu, 16, RenderTextureFormat.ARGB32);
        haritaDokusu.name = "Taylan GTA Harita Dokusu";
        haritaDokusu.filterMode = FilterMode.Bilinear;
        haritaDokusu.Create();

        GameObject kameraObjesi = new GameObject("Harita Kamerasi");
        kameraObjesi.transform.SetParent(transform, false);
        haritaKamerasi = kameraObjesi.AddComponent<Camera>();
        haritaKamerasi.enabled = false;
        haritaKamerasi.orthographic = true;
        haritaKamerasi.clearFlags = CameraClearFlags.SolidColor;
        haritaKamerasi.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
        haritaKamerasi.allowHDR = false;
        haritaKamerasi.allowMSAA = false;
        haritaKamerasi.useOcclusionCulling = false;
        haritaKamerasi.nearClipPlane = 0.3f;
        haritaKamerasi.farClipPlane = 450f;
        haritaKamerasi.depth = -100f;
        haritaKamerasi.targetTexture = haritaDokusu;
        haritaKamerasi.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        beyazDoku = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        beyazDoku.name = "Taylan Harita Cizgi Dokusu";
        beyazDoku.SetPixel(0, 0, Color.white);
        beyazDoku.Apply();
    }

    private void OyuncuyuBul()
    {
        Camera anaKamera = Camera.main;
        if (anaKamera != null && anaKamera.transform.root != null && anaKamera.transform.root != haritaKamerasi.transform)
        {
            Transform kok = anaKamera.transform.root;
            if (kok.GetComponentInChildren<Animator>(true) != null || kok.GetComponentInChildren<CharacterController>(true) != null)
                oyuncu = kok;
        }

        if (oyuncu != null)
            return;

        CharacterController[] controllerlar = FindObjectsByType<CharacterController>();
        float enBuyuk = -1f;
        for (int i = 0; i < controllerlar.Length; i++)
        {
            CharacterController aday = controllerlar[i];
            if (aday != null && aday.height > enBuyuk)
            {
                enBuyuk = aday.height;
                oyuncu = aday.transform;
            }
        }
    }

    private static bool SahnedeIntroVar()
    {
        MonoBehaviour[] davranislar = FindObjectsByType<MonoBehaviour>();
        for (int i = 0; i < davranislar.Length; i++)
        {
            if (davranislar[i] != null && davranislar[i].GetType().Name == "OyunAcilisSahnesi")
                return true;
        }
        return false;
    }

    private void VarsayilanHedefiBul()
    {
        Transform[] tumu = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        Transform yedek = null;
        for (int i = 0; i < tumu.Length; i++)
        {
            Transform aday = tumu[i];
            if (aday == null)
                continue;

            if (aday.name == varsayilanGorevNoktasi)
            {
                HedefiUygula(aday, "IS GORUSMESI");
                return;
            }

            string ad = aday.name.ToLowerInvariant();
            if (yedek == null && (ad.Contains("magaza") || ad.Contains("mağaza")))
                yedek = aday;
        }

        if (yedek != null)
            HedefiUygula(yedek, "IS GORUSMESI");
    }

    private void HedefiUygula(Transform hedef, string ad)
    {
        gorevHedefi = hedef;
        gorevAdi = string.IsNullOrWhiteSpace(ad) ? "GOREV" : ad;
        RotaHesapla();
    }

    private void YolAginiKur()
    {
        yolParcalari.Clear();
        dugumler.Clear();
        baglantilar.Clear();
        yolSinirlariHazir = false;

        Transform[] tumu = FindObjectsByType<Transform>();
        for (int i = 0; i < tumu.Length; i++)
        {
            Transform yol = tumu[i];
            if (yol == null || !YolAdiMi(yol.name))
                continue;

            Renderer[] ciziciler = yol.GetComponentsInChildren<Renderer>(true);
            if (ciziciler.Length == 0)
                continue;

            Bounds sinir = ciziciler[0].bounds;
            for (int r = 1; r < ciziciler.Length; r++)
                sinir.Encapsulate(ciziciler[r].bounds);

            Vector3 sag = Vector3.ProjectOnPlane(yol.right, Vector3.up).normalized;
            Vector3 ileri = Vector3.ProjectOnPlane(yol.forward, Vector3.up).normalized;
            if (sag.sqrMagnitude < 0.5f) sag = Vector3.right;
            if (ileri.sqrMagnitude < 0.5f) ileri = Vector3.forward;

            float sagBoy = EksenBoyu(sinir, sag);
            float ileriBoy = EksenBoyu(sinir, ileri);
            Vector3 eksen = sagBoy >= ileriBoy ? sag : ileri;
            float yariBoy = Mathf.Max(sagBoy, ileriBoy);
            if (yariBoy < 2f)
                continue;

            YolParcasi parca = new YolParcasi();
            Vector3 merkez = sinir.center;
            merkez.y = yol.position.y + 0.5f;
            parca.bas = merkez - eksen * yariBoy;
            parca.son = merkez + eksen * yariBoy;
            parca.noktalar.Add(parca.bas);
            parca.noktalar.Add(merkez);
            parca.noktalar.Add(parca.son);
            yolParcalari.Add(parca);

            if (!yolSinirlariHazir)
            {
                yolSinirlari = sinir;
                yolSinirlariHazir = true;
            }
            else
            {
                yolSinirlari.Encapsulate(sinir);
            }
        }

        // Kesisen yol parcalarinin kesisimlerini ortak dugum yap.
        for (int i = 0; i < yolParcalari.Count; i++)
        {
            for (int j = i + 1; j < yolParcalari.Count; j++)
            {
                if (KesisimNoktasi(yolParcalari[i].bas, yolParcalari[i].son,
                                   yolParcalari[j].bas, yolParcalari[j].son,
                                   out Vector3 kesisim))
                {
                    yolParcalari[i].noktalar.Add(kesisim);
                    yolParcalari[j].noktalar.Add(kesisim);
                }
            }
        }

        for (int i = 0; i < yolParcalari.Count; i++)
        {
            YolParcasi parca = yolParcalari[i];
            Vector3 yon = (parca.son - parca.bas).normalized;
            parca.noktalar.Sort((a, b) =>
                Vector3.Dot(a - parca.bas, yon).CompareTo(Vector3.Dot(b - parca.bas, yon)));

            int onceki = -1;
            for (int n = 0; n < parca.noktalar.Count; n++)
            {
                int simdiki = DugumEkle(parca.noktalar[n]);
                if (onceki >= 0 && simdiki != onceki)
                    IkiYonluBagla(onceki, simdiki);
                onceki = simdiki;
            }
        }

        // Yol uclari tam cakismasa bile yakin kaldirim/kavsaklari birbirine bagla.
        float baglantiKaresi = yolBaglantiMesafesi * yolBaglantiMesafesi;
        for (int i = 0; i < dugumler.Count; i++)
        {
            for (int j = i + 1; j < dugumler.Count; j++)
            {
                if (Vector3.SqrMagnitude(dugumler[i] - dugumler[j]) <= baglantiKaresi)
                    IkiYonluBagla(i, j);
            }
        }
    }

    private static bool YolAdiMi(string ad)
    {
        if (string.IsNullOrWhiteSpace(ad))
            return false;
        string kucuk = ad.ToLowerInvariant();
        return kucuk.Contains("road_") || kucuk.Contains("sokak") || kucuk.Contains("cadde");
    }

    private static float EksenBoyu(Bounds sinir, Vector3 eksen)
    {
        Vector3 e = sinir.extents;
        return Mathf.Abs(eksen.x) * e.x + Mathf.Abs(eksen.y) * e.y + Mathf.Abs(eksen.z) * e.z;
    }

    private int DugumEkle(Vector3 nokta)
    {
        for (int i = 0; i < dugumler.Count; i++)
        {
            Vector2 fark = new Vector2(dugumler[i].x - nokta.x, dugumler[i].z - nokta.z);
            if (fark.sqrMagnitude < 2.25f)
                return i;
        }

        int yeni = dugumler.Count;
        dugumler.Add(nokta);
        baglantilar.Add(new List<Kenar>());
        return yeni;
    }

    private void IkiYonluBagla(int a, int b)
    {
        if (a == b)
            return;
        float mesafe = Vector3.Distance(dugumler[a], dugumler[b]);
        KenarEkle(a, b, mesafe);
        KenarEkle(b, a, mesafe);
    }

    private void KenarEkle(int a, int b, float mesafe)
    {
        List<Kenar> liste = baglantilar[a];
        for (int i = 0; i < liste.Count; i++)
        {
            if (liste[i].hedef == b)
                return;
        }
        liste.Add(new Kenar(b, mesafe));
    }

    private static bool KesisimNoktasi(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector3 sonuc)
    {
        Vector2 p = new Vector2(a.x, a.z);
        Vector2 r = new Vector2(b.x - a.x, b.z - a.z);
        Vector2 q = new Vector2(c.x, c.z);
        Vector2 s = new Vector2(d.x - c.x, d.z - c.z);
        float payda = r.x * s.y - r.y * s.x;
        sonuc = Vector3.zero;
        if (Mathf.Abs(payda) < 0.0001f)
            return false;

        Vector2 qp = q - p;
        float t = (qp.x * s.y - qp.y * s.x) / payda;
        float u = (qp.x * r.y - qp.y * r.x) / payda;
        if (t < 0f || t > 1f || u < 0f || u > 1f)
            return false;

        Vector2 k = p + r * t;
        sonuc = new Vector3(k.x, Mathf.Lerp(a.y, b.y, t), k.y);
        return true;
    }

    private void RotaHesapla()
    {
        rota.Clear();
        sonrakiRotaZamani = Time.unscaledTime + 1.1f;
        if (oyuncu == null || gorevHedefi == null)
            return;

        sonRotaOyuncuPozisyonu = oyuncu.position;
        sonRotaHedefPozisyonu = gorevHedefi.position;
        rota.Add(oyuncu.position);

        if (dugumler.Count == 0)
        {
            rota.Add(gorevHedefi.position);
            return;
        }

        int baslangic = EnYakinDugum(oyuncu.position);
        int hedef = EnYakinDugum(gorevHedefi.position);
        List<int> yol = EnKisaYol(baslangic, hedef);
        if (yol.Count == 0)
        {
            rota.Add(gorevHedefi.position);
            return;
        }

        for (int i = 0; i < yol.Count; i++)
            rota.Add(dugumler[yol[i]]);
        rota.Add(gorevHedefi.position);
    }

    private int EnYakinDugum(Vector3 nokta)
    {
        int enYakin = 0;
        float enKisa = float.MaxValue;
        for (int i = 0; i < dugumler.Count; i++)
        {
            Vector2 fark = new Vector2(dugumler[i].x - nokta.x, dugumler[i].z - nokta.z);
            float kare = fark.sqrMagnitude;
            if (kare < enKisa)
            {
                enKisa = kare;
                enYakin = i;
            }
        }
        return enYakin;
    }

    private List<int> EnKisaYol(int baslangic, int hedef)
    {
        int sayi = dugumler.Count;
        float[] uzaklik = new float[sayi];
        int[] onceki = new int[sayi];
        bool[] kullanildi = new bool[sayi];
        for (int i = 0; i < sayi; i++)
        {
            uzaklik[i] = float.MaxValue;
            onceki[i] = -1;
        }
        uzaklik[baslangic] = 0f;

        for (int adim = 0; adim < sayi; adim++)
        {
            int secilen = -1;
            float enKisa = float.MaxValue;
            for (int i = 0; i < sayi; i++)
            {
                if (!kullanildi[i] && uzaklik[i] < enKisa)
                {
                    enKisa = uzaklik[i];
                    secilen = i;
                }
            }

            if (secilen < 0)
                break;
            if (secilen == hedef)
                break;

            kullanildi[secilen] = true;
            List<Kenar> komsular = baglantilar[secilen];
            for (int k = 0; k < komsular.Count; k++)
            {
                Kenar kenar = komsular[k];
                float aday = uzaklik[secilen] + kenar.mesafe;
                if (aday < uzaklik[kenar.hedef])
                {
                    uzaklik[kenar.hedef] = aday;
                    onceki[kenar.hedef] = secilen;
                }
            }
        }

        List<int> sonuc = new List<int>();
        if (hedef != baslangic && onceki[hedef] < 0)
            return sonuc;

        int simdiki = hedef;
        while (simdiki >= 0)
        {
            sonuc.Add(simdiki);
            if (simdiki == baslangic)
                break;
            simdiki = onceki[simdiki];
        }
        sonuc.Reverse();
        return sonuc;
    }

    private void HaritayiRenderEt()
    {
        if (haritaKamerasi == null || haritaDokusu == null || oyuncu == null)
            return;

        Vector3 merkez;
        float boyut;
        if (buyukHaritaAcik && yolSinirlariHazir)
        {
            merkez = yolSinirlari.center;
            boyut = Mathf.Max(yolSinirlari.extents.x, yolSinirlari.extents.z) + 24f;
        }
        else
        {
            merkez = oyuncu.position;
            boyut = miniHaritaMenzili;
        }

        haritaKamerasi.orthographicSize = Mathf.Max(20f, boyut);
        haritaKamerasi.transform.position = new Vector3(merkez.x, merkez.y + 180f, merkez.z);
        haritaKamerasi.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        haritaKamerasi.Render();
    }

    private void BuyukHaritayiAc()
    {
        buyukHaritaAcik = true;
        oncekiZamanOlcegi = Time.timeScale;
        oncekiKilit = Cursor.lockState;
        oncekiImlec = Cursor.visible;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        HaritayiRenderEt();
    }

    private void BuyukHaritayiKapat()
    {
        buyukHaritaAcik = false;
        Time.timeScale = oncekiZamanOlcegi;
        Cursor.lockState = oncekiKilit;
        Cursor.visible = oncekiImlec;
        HaritayiRenderEt();
    }

    private void OnGUI()
    {
        if (!oynanisAktif || oyuncu == null || haritaDokusu == null || Event.current.type != EventType.Repaint)
            return;

        StilleriHazirla();
        Rect alan;
        Vector3 merkez;
        float boyut;

        if (buyukHaritaAcik)
        {
            float kare = Mathf.Min(Screen.width * 0.82f, Screen.height * 0.82f);
            alan = new Rect((Screen.width - kare) * 0.5f, (Screen.height - kare) * 0.5f, kare, kare);
            merkez = yolSinirlariHazir ? yolSinirlari.center : oyuncu.position;
            boyut = yolSinirlariHazir ? Mathf.Max(yolSinirlari.extents.x, yolSinirlari.extents.z) + 24f : miniHaritaMenzili;
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), beyazDoku);
            GUI.color = Color.white;
        }
        else
        {
            float kare = Mathf.Clamp(Screen.height * 0.245f, 180f, 270f);
            alan = new Rect(20f, Screen.height - kare - 24f, kare, kare);
            merkez = oyuncu.position;
            boyut = miniHaritaMenzili;
        }

        GUI.color = new Color(0.01f, 0.015f, 0.02f, 0.96f);
        GUI.Box(new Rect(alan.x - 5f, alan.y - 5f, alan.width + 10f, alan.height + 10f), GUIContent.none, cerceveStili);
        GUI.color = Color.white;
        GUI.DrawTexture(alan, haritaDokusu, ScaleMode.StretchToFill, false);

        YollariCiz(alan, merkez, boyut);
        RotayiCiz(alan, merkez, boyut);
        OyuncuyuCiz(alan, merkez, boyut);
        GoreviCiz(alan, merkez, boyut);

        if (buyukHaritaAcik)
        {
            GUI.Label(new Rect(alan.x, alan.y - 34f, alan.width, 30f), "HARITA  |  M: KAPAT  |  HEDEF: " + gorevAdi, bilgiStili);
        }
        else
        {
            GUI.Label(new Rect(alan.x, alan.y - 27f, alan.width, 24f), "M  HARITA", bilgiStili);
        }
    }

    private void StilleriHazirla()
    {
        if (cerceveStili != null)
            return;
        cerceveStili = new GUIStyle(GUI.skin.box);
        cerceveStili.normal.background = beyazDoku;
        bilgiStili = new GUIStyle(GUI.skin.label);
        bilgiStili.fontSize = 16;
        bilgiStili.fontStyle = FontStyle.Bold;
        bilgiStili.alignment = TextAnchor.MiddleCenter;
        bilgiStili.normal.textColor = Color.white;
    }

    private void YollariCiz(Rect alan, Vector3 merkez, float boyut)
    {
        Color renk = new Color(0.78f, 0.82f, 0.86f, buyukHaritaAcik ? 0.7f : 0.48f);
        float kalinlik = buyukHaritaAcik ? 4f : 2.5f;
        for (int i = 0; i < yolParcalari.Count; i++)
        {
            Vector2 a = HaritaNoktasi(yolParcalari[i].bas, alan, merkez, boyut);
            Vector2 b = HaritaNoktasi(yolParcalari[i].son, alan, merkez, boyut);
            if (NoktaHaritaYakininda(a, alan) || NoktaHaritaYakininda(b, alan))
                CizgiCiz(HaritaAlaninaSinirla(a, alan), HaritaAlaninaSinirla(b, alan), kalinlik, renk);
        }
    }

    private void RotayiCiz(Rect alan, Vector3 merkez, float boyut)
    {
        for (int i = 1; i < rota.Count; i++)
        {
            Vector2 a = HaritaNoktasi(rota[i - 1], alan, merkez, boyut);
            Vector2 b = HaritaNoktasi(rota[i], alan, merkez, boyut);
            CizgiCiz(
                HaritaAlaninaSinirla(a, alan),
                HaritaAlaninaSinirla(b, alan),
                buyukHaritaAcik ? 7f : 5f,
                new Color(0.12f, 0.62f, 1f, 0.98f));
        }
    }

    private void OyuncuyuCiz(Rect alan, Vector3 merkez, float boyut)
    {
        Vector2 p = HaritaNoktasi(oyuncu.position, alan, merkez, boyut);
        float boy = buyukHaritaAcik ? 16f : 13f;
        Color eski = GUI.color;
        Matrix4x4 matris = GUI.matrix;
        GUI.color = Color.white;
        GUIUtility.RotateAroundPivot(oyuncu.eulerAngles.y, p);
        GUI.DrawTexture(new Rect(p.x - boy * 0.35f, p.y - boy * 0.5f, boy * 0.7f, boy), beyazDoku);
        GUI.matrix = matris;
        GUI.color = eski;
    }

    private void GoreviCiz(Rect alan, Vector3 merkez, float boyut)
    {
        if (gorevHedefi == null)
            return;
        Vector2 p = HaritaAlaninaSinirla(
            HaritaNoktasi(gorevHedefi.position, alan, merkez, boyut),
            alan);
        float nabiz = 12f + Mathf.Sin(Time.unscaledTime * 5f) * 2f;
        Color eski = GUI.color;
        GUI.color = new Color(1f, 0.78f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(p.x - nabiz * 0.5f, p.y - nabiz * 0.5f, nabiz, nabiz), beyazDoku);
        GUI.color = eski;
    }

    private static Vector2 HaritaNoktasi(Vector3 dunya, Rect alan, Vector3 merkez, float boyut)
    {
        float nx = 0.5f + (dunya.x - merkez.x) / (boyut * 2f);
        float ny = 0.5f - (dunya.z - merkez.z) / (boyut * 2f);
        return new Vector2(alan.x + nx * alan.width, alan.y + ny * alan.height);
    }

    private static bool NoktaHaritaYakininda(Vector2 p, Rect alan)
    {
        return p.x >= alan.x - 30f && p.x <= alan.xMax + 30f && p.y >= alan.y - 30f && p.y <= alan.yMax + 30f;
    }

    private static Vector2 HaritaAlaninaSinirla(Vector2 p, Rect alan)
    {
        return new Vector2(
            Mathf.Clamp(p.x, alan.x + 4f, alan.xMax - 4f),
            Mathf.Clamp(p.y, alan.y + 4f, alan.yMax - 4f));
    }

    private void CizgiCiz(Vector2 bas, Vector2 son, float kalinlik, Color renk)
    {
        Vector2 fark = son - bas;
        float uzunluk = fark.magnitude;
        if (uzunluk < 0.5f)
            return;

        Matrix4x4 oncekiMatris = GUI.matrix;
        Color oncekiRenk = GUI.color;
        GUI.color = renk;
        float aci = Mathf.Atan2(fark.y, fark.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(aci, bas);
        GUI.DrawTexture(new Rect(bas.x, bas.y - kalinlik * 0.5f, uzunluk, kalinlik), beyazDoku);
        GUI.matrix = oncekiMatris;
        GUI.color = oncekiRenk;
    }
}
