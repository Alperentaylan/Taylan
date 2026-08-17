using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Oyunun ilk telefon sahnesini yonetir.
/// Tek akisi yonetir: goz acilisi -> sabit FPS kafa bakisi -> E ile telefon ->
/// kontrollu sag el IK -> yatagin soluna kalkis -> normal Idle.
/// Animator ve KarakterHareketi hicbir zaman kapatilmaz.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(45000)]
public sealed class OyunAcilisSahnesi : MonoBehaviour
{
    [Header("KARAKTER")]
    [SerializeField] private Animator animator;
    [SerializeField] private Camera oyuncuKamerasi;
    [SerializeField] private Transform oyuncuKoku;
    [Tooltip("Intro baslarken ucuncu sahsi kapatip bu kamerayi zorla aktif eder.")]
    [HideInInspector][SerializeField] private bool birinciSahislaBasla = true;
    [Tooltip("Animator state'in tam yolu.")]
    [SerializeField] private string yatakIdleStateAdi = "Base Layer.YatakIdle";
    [Tooltip("Ayni yatma klibini kullanan, Speed=-1 yapilmis state.")]
    [SerializeField] private string yataktanKalkStateAdi = "Base Layer.YataktanKalk";
    [SerializeField] private float kalkisAnimasyonuSuresi = 6f;
    [Tooltip("Kod negatif state hizini otomatik algilar. Yalniz otomatik algilama calismazsa zorlamak icin ac.")]
    [HideInInspector][SerializeField] private bool kalkisStateTersOynuyor = false;

    [Header("YATAKTA BASLANGIC POZU")]
    [Tooltip("Yatagin minder yuzeyine bir Empty koyup buraya ver. Bu nokta karakter KOKU degil, kalcanin yatacagi yuzeydir.")]
    [SerializeField] private Transform yatakPozNoktasi;
    [SerializeField] private Transform yatakObjesi;
    [HideInInspector][SerializeField] private bool introBoyuncaYatakPozunaKilitle = true;
    [HideInInspector][SerializeField] private Vector3 yatakPozisyonDuzeltmesi = new Vector3(0f, 0.04f, 0f);
    [Tooltip("Kalca merkezinin yatak yuzeyinden yuksekligi. Govde yataga gomulurse artir.")]
    [Range(0f, 0.4f)]
    [SerializeField] private float kalcaYatakUstuYuksekligi = 0.12f;
    [HideInInspector][SerializeField] private Vector3 yatakRotasyonDuzeltmesi = Vector3.zero;

    [Header("KALKIS SONU")]
    [Tooltip("Istege bagli: Karakter kalkinca duracagi zemine bir Empty koy. Bos ise yatagin yanindaki zemin otomatik bulunur.")]
    [SerializeField] private Transform kalkisSonuNoktasi;
    [HideInInspector][Min(0.55f)][SerializeField] private float kalkisYatakYanMesafesi = 1.05f;
    [HideInInspector][SerializeField] private string normalIdleStateAdi = "Base Layer.idle";

    [Header("YATAKTA GERCEK FPS KAMERA")]
    [Tooltip("Yeni kamera olusturmaz; Oyuncu Kamerasi alanindaki mevcut FPS kamerayi gozlere yerlestirir.")]
    [HideInInspector][SerializeField] private bool kalkisaKadarKamerayiGozlereKilitle = true;
    [Tooltip("Elle ayarlamak icin Head kemiginin altina bir Empty koyup buraya ver. Verilirse otomatik goz hesabi yerine bu nokta kullanilir.")]
    [SerializeField] private Transform yatakKameraNoktasi;
    [Tooltip("Yalniz otomatik goz konumunda kullanilir. Manuel Yatak Kamera Noktasi verildiyse noktanin kendi rotasyonu kullanilir.")]
    [HideInInspector][SerializeField] private bool yatakKamerasiBaslangictaTelefonaBaksin = true;
    [HideInInspector][SerializeField] private Vector3 yatakKameraLocalKonumDuzeltmesi = Vector3.zero;
    [HideInInspector][SerializeField] private Vector3 yatakKameraAciDuzeltmesi = Vector3.zero;
    [Tooltip("Kamerayi goz/yuz geometrisinin biraz disina cikarir.")]
    [Range(0.01f, 0.2f)]
    [HideInInspector][SerializeField] private float yatakKamerasiOneCikma = 0.11f;
    [Tooltip("Kameraya cok yakin kafa/yuz geometrisini keser.")]
    [Range(0.05f, 0.3f)]
    [HideInInspector][SerializeField] private float yatakKamerasiNearClip = 0.16f;
    [SerializeField] private float yatakKamerasiMouseHassasiyeti = 1.4f;
    [Range(45f, 179f)]
    [SerializeField] private float yatakKamerasiYatayLimit = 72f;
    [Range(15f, 80f)]
    [SerializeField] private float yatakKamerasiDikeyLimit = 52f;

    [Header("TELEFON")]
    [SerializeField] private Transform telefon;
    [SerializeField] private Collider telefonCollider;
    [SerializeField] private AudioClip telefonCalmaSesi;
    [HideInInspector][SerializeField] private AudioSource telefonAudioSource;
    [HideInInspector][Min(0.5f)][SerializeField] private float telefonEtkilesimMesafesi = 4f;
    [SerializeField] private KeyCode etkilesimTusu = KeyCode.E;

    [Header("TELEFONUN ELDEKI POZU")]
    [Tooltip("Telefon sag ele parent edildikten sonraki local konumu.")]
    [SerializeField] private Vector3 telefonElLocalKonumu = new Vector3(0.025f, 0.055f, 0.015f);
    [SerializeField] private Vector3 telefonElLocalAcisi = new Vector3(5f, 90f, 90f);
    [Tooltip("Istege bagli: Head altina bir Empty koyup telefonun kulaktaki pozunu elle ayarlayabilirsin. Bos ise kafa kemiginden hesaplanir.")]
    [SerializeField] private Transform telefonKulakNoktasi;
    [Tooltip("Konusma bitince telefonun gidecegi cep noktasi. Bos birakilabilir.")]
    [HideInInspector][SerializeField] private Transform telefonCepNoktasi;
    [HideInInspector][SerializeField] private bool cebeKoyuncaTelefonuGizle = true;

    [Header("PROSEDUREL HAREKET")]
    [HideInInspector][SerializeField] private float telefonaUzanmaSuresi = 0.8f;
    [HideInInspector][SerializeField] private float kulagaGoturmeSuresi = 0.75f;
    [HideInInspector][SerializeField] private float cebeKoymaSuresi = 0.55f;
    [SerializeField] private Vector3 kulakPozisyonOffseti = new Vector3(0.115f, -0.015f, 0.025f);
    [HideInInspector][SerializeField] private Vector3 cepPozisyonOffseti = new Vector3(0.16f, -0.12f, 0.07f);
    [SerializeField] private Vector3 elIKRotasyonOffseti = new Vector3(5f, 80f, 90f);
    [Range(0.45f, 0.82f)]
    [SerializeField] private float guvenliElIKAgirligi = 0.72f;

    [Header("DIYALOG")]
    [TextArea(2, 5)]
    [SerializeField]
    private string insanKaynaklariMetni =
        "Alperen Bey, merhabalar. Başvurduğunuz ilanla ilgili sizinle iletişime geçiyorum. Bugün müsaitseniz iki saat sonra sizinle görüşmek isteriz.";
    [TextArea(2, 4)]
    [SerializeField]
    private string alperenMetni =
        "Tabii ki. İki saat sonra orada olacağım.";
    [SerializeField] private AudioClip insanKaynaklariSesi;
    [SerializeField] private AudioClip alperenSesi;
    [HideInInspector][SerializeField] private AudioSource konusmaAudioSource;
    [HideInInspector][SerializeField] private float insanKaynaklariYaziSuresi = 7.5f;
    [HideInInspector][SerializeField] private float alperenYaziSuresi = 3.2f;

    [Header("ACILIS")]
    [Tooltip("Sahne yuklendikten sonra, gozler hala kapaliyken telefonun calmadan once bekleyecegi sure.")]
    [HideInInspector][SerializeField] private float telefonCalmayaBaslamaGecikmesi = 0.3f;
    [HideInInspector][SerializeField] private float siyahEkranBekleme = 0.65f;
    [HideInInspector][SerializeField] private float goruntuAcilmaSuresi = 1.6f;
    [HideInInspector][SerializeField] private float gozKirpmaKapanmaSuresi = 0.32f;
    [HideInInspector][SerializeField] private float gozKirpmaAcilmaSuresi = 0.58f;
    [HideInInspector][SerializeField] private float kirpmadaKapaliBekleme = 0.14f;
    [HideInInspector][SerializeField] private float kirpmalarArasiBekleme = 0.48f;
    [Range(1, 4)]
    [HideInInspector][SerializeField] private int gozKirpmaSayisi = 2;
    [HideInInspector][SerializeField] private string ilkGorevMetni = "İş görüşmesine hazırlan";
    [HideInInspector][SerializeField] private float gorevYazisiSuresi = 4f;
    [HideInInspector][SerializeField] private UnityEvent acilisTamamlandi;

    private enum SahneAsamasi
    {
        Basliyor,
        TelefonCaliyor,
        TelefonaUzaniyor,
        TelefondaKonusuyor,
        CebeKoyuyor,
        KalkisBekliyor,
        Kalkiyor,
        Bitti
    }

    private SahneAsamasi asama = SahneAsamasi.Basliyor;
    private Transform kafa;
    private Transform sagEl;
    private Transform sagUstKol;
    private Transform sagAltKol;
    private Transform kalca;
    private Transform solGoz;
    private Transform sagGoz;
    private AcilisAnimatorIKKoprusu ikKoprusu;

    private Canvas arayuzCanvas;
    private CanvasGroup siyahPerde;
    private Image gozKapagiMaskesi;
    private Texture2D gozKapagiMaskTexture;
    private Color32[] gozKapagiPikselleri;
    private float[] gozKapagiYatayEgrisi;
    private CanvasGroup etkilesimGrubu;
    private Text etkilesimText;
    private CanvasGroup altyaziGrubu;
    private Text konusanText;
    private Text altyaziText;
    private CanvasGroup gorevGrubu;
    private Text gorevText;
    private Font font;
    private Texture2D duzTexture;

    private bool telefonaBakiliyor;
    private bool telefonZiliCalmali;
    private bool aktifKalkisTersOynuyor;
    private Vector3 ikHedefPozisyon;
    private Quaternion ikHedefRotasyon = Quaternion.identity;
    private float elIKAgirligi;
    private Vector3 kilitliYatakPozisyonu;
    private Quaternion kilitliYatakRotasyonu;
    private bool yatakPozuHazir;
    private bool kameraSistemiFPSOlarakBildirildi;
    private Camera[] sahneKameralari;
    private MonoBehaviour mevcutFPSKameraSistemi;
    private MethodInfo fpsKameraRiginiSabitleMetodu;
    private bool fpsKameraYenilemeHatasiYazildi;
    private bool yatakKameraTemeliHazir;
    private Quaternion yatakKameraTemelRotasyonu = Quaternion.identity;
    private Vector3 yatakKameraSabitDunyaPozisyonu;
    private float yatakKameraYaw;
    private float yatakKameraPitch;
    private const float KalkisBeklemeKameraYuksekligiMetre = 0.30f;
    private const float YatakKenarindaOturmaBeklemesi = 0.32f;
    private const float OturmadanAyagaGecisSuresi = 0.9f;
    private const float OtururkenYatakKenarPayi = 0.28f;
    private Quaternion kafaKameraRotasyonFarki = Quaternion.identity;
    private bool kafaKameraRotasyonFarkiHazir;
    private Vector3 kalkisKameraKafaDunyaOffseti;
    private Quaternion kalkisKameraSabitDunyaRotasyonu = Quaternion.identity;
    private bool kalkisKameraTakibiHazir;
    private Vector3 ilkOyuncuPozisyonu;
    private Quaternion ilkOyuncuRotasyonu = Quaternion.identity;

    private void Awake()
    {
        // KarakterBeyni.controller dosyasindaki GERCEK state adlari ve hizlari:
        // Gonderilen FBX normal yonde OTURMA -> YATMA, Speed=-1 ile ise
        // YATMA -> OTURMA oynuyor. Klipte oturmadan ayaga kalkma bolumu yoktur;
        // o son kisim asagida kontrollu bir Idle gecisiyle tamamlanir.
        // Eski Inspector degerleri serialize edilmis olsa bile bunlari duzelt.
        yataktanKalkStateAdi = "Base Layer.YataktanKalk";
        kalkisStateTersOynuyor = true;
        normalIdleStateAdi = "Base Layer.idle";
        // Gonderilen FBX'in gercek suresi 5.7167 saniye. Eski sahnede serialize
        // edilmis 2.8 degeri klibi oturma karesinde yariyordu.
        kalkisAnimasyonuSuresi = Mathf.Max(kalkisAnimasyonuSuresi, 6f);

        // Yatma animasyonunda normal ayakta-FPS hesabi kamerayi karakterin disina
        // tasiyor. Intro bitene kadar mevcut FPS Camera componentini yatak kamera
        // noktasinda tut; yeni veya ikinci bir kamera olusturma.
        kalkisaKadarKamerayiGozlereKilitle = true;

        // Inspector'da eski surumden acik kalmis olsa bile bu klipte Root Motion
        // kullanma. Karakterin yataktan havaya/tavana firlamasinin ana nedeni buydu.
        yatakKamerasiYatayLimit = Mathf.Clamp(yatakKamerasiYatayLimit, 45f, 90f);
        yatakKamerasiDikeyLimit = Mathf.Clamp(yatakKamerasiDikeyLimit, 25f, 65f);

        ReferanslariBul();
        PrefabReferanslariniSahneOrneklerineCevir();
        if (oyuncuKoku != null)
        {
            ilkOyuncuPozisyonu = oyuncuKoku.position;
            ilkOyuncuRotasyonu = oyuncuKoku.rotation;
        }
        YatakPozunuHazirla();
        BirinciSahisBaslangiciniKur();
        ArayuzuKur();
        IKKoprusunuKur();

        if (animator == null || telefon == null)
        {
            Debug.LogError("OyunAcilisSahnesi: Animator ve Telefon alanlari atanmalidir.", this);
            enabled = false;
            return;
        }

        // FPS kamerada karakter render disinda kalsa bile yatma/kalkma
        // animasyonlarinin durmasina izin verme.
        animator.enabled = true;
        animator.speed = 1f;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (telefonCollider == null)
            telefonCollider = telefon.GetComponentInChildren<Collider>(true);

        AudioKaynaklariniHazirla();
        YatakIdleOynat();
        StartCoroutine(AcilisiBaslat());
    }

    private void Update()
    {
        // Gozler henuz kapaliyken de zil calmalidir. Klip ilk karede yuklenmediyse
        // kaynak hazir olur olmaz otomatik yeniden baslat.
        if (telefonZiliCalmali &&
            (int)asama <= (int)SahneAsamasi.TelefonCaliyor &&
            telefonAudioSource != null &&
            telefonCalmaSesi != null &&
            !telefonAudioSource.isPlaying)
        {
            TelefonuCaldir();
        }

        // KarakterHareketi acik kalir; fakat kalkis klibi oynarken eski
        // Jump/merdiven parametreleri Any State gecisiyle klibi kesemez.
        if (asama == SahneAsamasi.Kalkiyor)
            KalkisAnimatorParametreleriniTemizle();

        if (kalkisaKadarKamerayiGozlereKilitle &&
            asama == SahneAsamasi.TelefonCaliyor)
        {
            YatakKamerasiFareBakisiniGuncelle();
        }

        if (asama == SahneAsamasi.KalkisBekliyor)
        {
            etkilesimGrubu.alpha = Mathf.MoveTowards(
                etkilesimGrubu.alpha,
                1f,
                Time.unscaledDeltaTime * 6f);

            if (Input.GetKeyDown(etkilesimTusu))
                StartCoroutine(KalkisSahnesiniOynat());

            return;
        }

        if (asama != SahneAsamasi.TelefonCaliyor)
            return;

        // Bu acilis sahnesinde tek etkilesim telefondur. Collider, bakis acisi
        // veya model pivotu bozuk olsa bile E istemi kesinlikle kaybolmasin.
        telefonaBakiliyor = telefon != null;
        etkilesimGrubu.alpha = Mathf.MoveTowards(
            etkilesimGrubu.alpha,
            telefonaBakiliyor ? 1f : 0f,
            Time.unscaledDeltaTime * 6f);

        if (telefonaBakiliyor &&
            Input.GetKeyDown(etkilesimTusu))
        {
            StartCoroutine(TelefonSahnesiniOynat());
        }
    }

    private void LateUpdate()
    {
        // Diger kamera scriptleri Update/LateUpdate'ta tekrar TPS acsa bile
        // intro boyunca en son bu script calisir ve FPS kamerayi korur.
        if (birinciSahislaBasla && asama != SahneAsamasi.Bitti)
            FPSKamerasiniZorla();

        // Yatma klibinin root/hip yuksekligi karakteri havaya tasiyamaz.
        // Kalkis baslayana kadar oyuncu koku yatak anchor'inda sabit kalir.
        if (introBoyuncaYatakPozunaKilitle &&
            yatakPozuHazir &&
            (int)asama < (int)SahneAsamasi.Kalkiyor)
        {
            KarakteriYatakPozunaAl();

            // BirinciUcuncuSahisKesin kendi LateUpdate'ini daha once calistirdi.
            // Karakteri simdi yataga tasidigimiz icin ayni FPS rig hesabini
            // yeni kemik konumlariyla bir kez daha yaptir.
            MevcutFPSKameraPozunuYenile();
        }

        // Ayni mevcut FPS Camera componentini son katmanda gercek goz
        // merkezine yerlestir. Boylece yatma klibinde karakter disaridan
        // gorunmez ve normal FPS scriptinin ayakta-yaw varsayimi araya girmez.
        if (kalkisaKadarKamerayiGozlereKilitle &&
            (int)asama < (int)SahneAsamasi.Kalkiyor)
        {
            YatakFPSKamerasiniUygula();
        }
        else if (asama == SahneAsamasi.Kalkiyor)
        {
            // Kalkma klibinde kamera kafaya bagli kalir; disariya/TPS'e firlamaz.
            KalkisFPSKamerasiniUygula();
        }

    }

    private IEnumerator AcilisiBaslat()
    {
        siyahPerde.alpha = 1f;
        GozKapagiKapaliliginiAyarla(1f);
        etkilesimGrubu.alpha = 0f;
        altyaziGrubu.alpha = 0f;
        gorevGrubu.alpha = 0f;

        // Sahne once tamamen yuklensin. Telefon, gozler hala kapaliyken
        // calmaya baslar; ardindan goz acilma animasyonu gelir.
        yield return null;
        if (telefonCalmayaBaslamaGecikmesi > 0f)
        {
            yield return new WaitForSecondsRealtime(
                telefonCalmayaBaslamaGecikmesi);
        }

        TelefonuCaldir();
        yield return new WaitForSecondsRealtime(siyahEkranBekleme);

        // Once ekran gercekten kapali/gozler kapali kalsin. Tam ekran
        // siyah perde kalkarken iki goz kapagi goruntuyu kapatmaya devam eder.
        float sure = 0f;
        const float perdeKalkmaSuresi = 0.18f;
        while (sure < perdeKalkmaSuresi)
        {
            sure += Time.unscaledDeltaTime;
            siyahPerde.alpha = 1f - Mathf.Clamp01(sure / perdeKalkmaSuresi);
            yield return null;
        }

        siyahPerde.alpha = 0f;

        // Tek, genis goz acikligi: kapali -> yavas ve sinematik acilis.
        yield return GozKapagiAnimasyonu(
            1f,
            0f,
            Mathf.Max(2.1f, goruntuAcilmaSuresi));
        yield return new WaitForSecondsRealtime(0.52f);

        // Tam istenen ritim:
        // acildi -> kapandi/acildi -> kapandi/acildi.
        int kirpmaAdedi = Mathf.Max(2, Mathf.Clamp(gozKirpmaSayisi, 1, 4));
        float kapanmaSuresi = Mathf.Max(0.28f, gozKirpmaKapanmaSuresi);
        float acilmaSuresi = Mathf.Max(0.52f, gozKirpmaAcilmaSuresi);
        for (int i = 0; i < kirpmaAdedi; i++)
        {
            yield return GozKapagiAnimasyonu(0f, 1f, kapanmaSuresi);
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.1f, kirpmadaKapaliBekleme));
            yield return GozKapagiAnimasyonu(1f, 0f, acilmaSuresi);

            if (i < kirpmaAdedi - 1)
            {
                yield return new WaitForSecondsRealtime(
                    Mathf.Max(0.35f, kirpmalarArasiBekleme));
            }
        }

        GozKapagiKapaliliginiAyarla(0f);

        asama = SahneAsamasi.TelefonCaliyor;
        etkilesimGrubu.alpha = 1f;
    }

    private IEnumerator TelefonSahnesiniOynat()
    {
        asama = SahneAsamasi.TelefonaUzaniyor;
        etkilesimGrubu.alpha = 0f;
        elIKAgirligi = 0f;
        telefonZiliCalmali = false;

        if (telefonAudioSource != null)
            telefonAudioSource.Stop();

        Vector3 masaTelefonPozisyonu = telefon.position;
        Quaternion masaTelefonRotasyonu = telefon.rotation;
        Quaternion masaElRotasyonu = masaTelefonRotasyonu *
            Quaternion.Inverse(Quaternion.Euler(telefonElLocalAcisi));
        Vector3 masaElPozisyonu = masaTelefonPozisyonu -
            masaElRotasyonu * telefonElLocalKonumu;
        Vector3 elBaslangicPozisyonu = sagEl != null
            ? sagEl.position
            : masaTelefonPozisyonu;
        Quaternion elBaslangicRotasyonu = sagEl != null
            ? sagEl.rotation
            : masaTelefonRotasyonu;

        // Sag el telefona kontrollu ve sinirli agirlikla uzanir. LookAt/kafa IK
        // kullanilmaz; bu nedenle omurga ve kafa eski surumdeki gibi carpılmaz.
        float sure = 0f;
        while (sure < telefonaUzanmaSuresi)
        {
            sure += Time.deltaTime;
            float t = Yumusa(Mathf.Clamp01(sure / telefonaUzanmaSuresi));
            ikHedefPozisyon = Vector3.Lerp(
                elBaslangicPozisyonu,
                masaElPozisyonu,
                t);
            ikHedefRotasyon = Quaternion.Slerp(
                elBaslangicRotasyonu,
                masaElRotasyonu,
                t);
            elIKAgirligi = Mathf.Lerp(
                0f,
                guvenliElIKAgirligi * 0.72f,
                t);
            yield return null;
        }

        TelefonuEleBagla();

        // Telefon artik elde. El, kafa ve govdeyi zorlamayacak sinirli IK ile
        // kulaga gider; telefon ele parent oldugu icin eli birebir takip eder.
        Vector3 uzanmaSonu = ikHedefPozisyon;
        Quaternion uzanmaRotasyonu = ikHedefRotasyon;
        sure = 0f;
        while (sure < kulagaGoturmeSuresi)
        {
            sure += Time.deltaTime;
            float t = Yumusa(Mathf.Clamp01(sure / kulagaGoturmeSuresi));
            ikHedefPozisyon = Vector3.Lerp(
                uzanmaSonu,
                KulakElHedefPozisyonu(),
                t);
            ikHedefRotasyon = Quaternion.Slerp(
                uzanmaRotasyonu,
                KulakElHedefRotasyonu(),
                t);
            elIKAgirligi = Mathf.Lerp(
                guvenliElIKAgirligi * 0.72f,
                guvenliElIKAgirligi,
                t);
            yield return null;
        }

        asama = SahneAsamasi.TelefondaKonusuyor;
        yield return DiyalogGoster("İNSAN KAYNAKLARI", insanKaynaklariMetni, insanKaynaklariSesi, insanKaynaklariYaziSuresi);
        yield return new WaitForSecondsRealtime(0.3f);
        yield return DiyalogGoster("ALPEREN", alperenMetni, alperenSesi, alperenYaziSuresi);
        yield return new WaitForSecondsRealtime(0.35f);
        altyaziGrubu.alpha = 0f;

        asama = SahneAsamasi.CebeKoyuyor;
        Vector3 kulakElPozisyonu = ikHedefPozisyon;
        Quaternion kulakElRotasyonu = ikHedefRotasyon;
        sure = 0f;
        while (sure < cebeKoymaSuresi)
        {
            sure += Time.deltaTime;
            float t = Yumusa(Mathf.Clamp01(sure / cebeKoymaSuresi));
            ikHedefPozisyon = Vector3.Lerp(
                kulakElPozisyonu,
                CepDunyaPozisyonu(),
                t);
            ikHedefRotasyon = Quaternion.Slerp(
                kulakElRotasyonu,
                CepElRotasyonu(),
                t);
            elIKAgirligi = Mathf.Lerp(guvenliElIKAgirligi, 0f, t);
            yield return null;
        }

        TelefonuCebeKoy();
        elIKAgirligi = 0f;

        // Konusma bittikten sonra otomatik kalkma. Oyuncu hazir oldugunda
        // ikinci kez E'ye basar; ancak o zaman mevcut YataktanKalk state'i oynar.
        yield return new WaitForSecondsRealtime(0.2f);
        asama = SahneAsamasi.KalkisBekliyor;
        etkilesimText.text = "[E]  KALKMAK İÇİN BASIN";
        etkilesimGrubu.alpha = 1f;
    }

    private IEnumerator KalkisSahnesiniOynat()
    {
        // Update ayni karede ikinci bir coroutine baslatamasin.
        asama = SahneAsamasi.Kalkiyor;
        etkilesimGrubu.alpha = 0f;
        altyaziGrubu.alpha = 0f;
        elIKAgirligi = 0f;

        bool oncekiRootMotion = animator.applyRootMotion;
        animator.enabled = true;
        animator.speed = 1f;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // Bu ters cevrilmis klibin root egrileri karakteri capraz/havaya
        // tasiyordu. Animator kemikleri oynatir; oyuncu koku asagida kontrollu
        // olarak yatagin sol kenarina ve ardindan zemine tasinir.
        animator.applyRootMotion = false;

        bool stateBasladi = YataktanKalkisiOynat();
        // E'ye basildigi anda oynatilan state'e kamerayi bagla. Araya bir kare
        // girip baska kamera scriptinin konumu bozmasina izin verme.
        KalkisKameraTakibiniHazirla();
        yield return null;
        Vector3 kalkisBaslangicPozisyonu = oyuncuKoku != null
            ? oyuncuKoku.position
            : Vector3.zero;
        Quaternion kalkisBaslangicRotasyonu = oyuncuKoku != null
            ? oyuncuKoku.rotation
            : Quaternion.identity;
        KalkisSonuPozunuHesapla(
            out Vector3 kalkisHedefPozisyonu,
            out Quaternion kalkisHedefRotasyonu);

        Vector3 yataginSolunaYon = KalkisSolYonunuBul(kalkisHedefRotasyonu);
        Vector3 oturmaHedefPozisyonu = kalkisHedefPozisyonu -
            yataginSolunaYon * OtururkenYatakKenarPayi;
        // Oturma boyunca karakter henuz zemine dusmez; yalniz yatagin sol
        // kenarina gelir. Y ekseni ancak ayaga kalkma gecisinde zemine iner.
        oturmaHedefPozisyonu.y = kalkisBaslangicPozisyonu.y;
        Quaternion oturmaHedefRotasyonu = Quaternion.LookRotation(
            yataginSolunaYon,
            Vector3.up);

        int kalkisHash = Animator.StringToHash(yataktanKalkStateAdi);
        float gecen = 0f;
        float azamiSure = Mathf.Max(6f, kalkisAnimasyonuSuresi);

        // Inspector suresi yerine Animator'un o anda oynattigi gercek klip
        // uzunlugunu da hesaba kat. Boylece FBX bir daha yarida kesilmez.
        if (stateBasladi)
        {
            AnimatorStateInfo baslangicBilgisi =
                animator.GetCurrentAnimatorStateInfo(0);
            if (baslangicBilgisi.fullPathHash == kalkisHash &&
                baslangicBilgisi.length > 0.05f)
            {
                float hiz = Mathf.Max(
                    0.01f,
                    Mathf.Abs(
                        baslangicBilgisi.speed *
                        baslangicBilgisi.speedMultiplier));
                azamiSure = Mathf.Max(
                    azamiSure,
                    baslangicBilgisi.length / hiz + 0.25f);
            }
        }

        bool stateGoruldu = false;
        float sonKlipIlerlemesi = 0f;

        while (gecen < azamiSure)
        {
            gecen += Time.deltaTime;

            if (stateBasladi)
            {
                AnimatorStateInfo bilgi = animator.GetCurrentAnimatorStateInfo(0);
                bool kalkisStateinde = bilgi.fullPathHash == kalkisHash;

                if (kalkisStateinde)
                {
                    stateGoruldu = true;
                    float ilerleme = bilgi.normalizedTime;
                    sonKlipIlerlemesi = aktifKalkisTersOynuyor
                        ? 1f - Mathf.Clamp01(ilerleme)
                        : Mathf.Clamp01(ilerleme);
                    bool tamamlandi = aktifKalkisTersOynuyor
                        ? ilerleme <= 0.02f
                        : ilerleme >= 0.98f;

                    if (gecen > 0.15f && tamamlandi && !animator.IsInTransition(0))
                        break;
                }
                else if (stateGoruldu && !animator.IsInTransition(0))
                {
                    break;
                }
            }
            else
            {
                // State sahnede yanlis adlandirilmis olsa bile karakteri
                // yatagin sol kenarina goturen guvenli hareket calisir.
                sonKlipIlerlemesi = Mathf.Clamp01(gecen / azamiSure);
            }

            // FBX yatistan oturmaya gecerken oyuncu kokunu tek bir kontrollu
            // hat uzerinde yatagin SOL kenarina getir. Serbest root motion,
            // diagonal ucus veya her kare farkli hedef yoktur.
            float kenaraGecis = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.06f, 0.88f, sonKlipIlerlemesi));
            if (oyuncuKoku != null)
            {
                oyuncuKoku.SetPositionAndRotation(
                    Vector3.Lerp(
                        kalkisBaslangicPozisyonu,
                        oturmaHedefPozisyonu,
                        kenaraGecis),
                    Quaternion.Slerp(
                        kalkisBaslangicRotasyonu,
                        oturmaHedefRotasyonu,
                        kenaraGecis));
            }

            yield return null;
        }

        if (oyuncuKoku != null)
        {
            oyuncuKoku.SetPositionAndRotation(
                oturmaHedefPozisyonu,
                oturmaHedefRotasyonu);
        }

        // Ters klibin bittigi kare gercek oturma karesidir. Kisa bir an burada
        // kal; kullanici once kenara oturdugunu net gorur.
        animator.speed = 0f;
        yield return new WaitForSecondsRealtime(YatakKenarindaOturmaBeklemesi);
        animator.speed = 1f;

        // Gonderilen FBX oturma karesinde biter. Eksik olan son kalkma bolumunu
        // oturma pozundan normal ayakta Idle'a yumusak gecisle tamamla. Kok,
        // yalniz bu gecis boyunca kenardan zemine iner; sonra kesin sabitlenir.
        int idleHash = NormalIdleHashiniBul();
        if (idleHash != 0)
        {
            animator.CrossFadeInFixedTime(
                idleHash,
                OturmadanAyagaGecisSuresi,
                0,
                0f);
        }

        float ayagaGecen = 0f;
        while (ayagaGecen < OturmadanAyagaGecisSuresi)
        {
            ayagaGecen += Time.deltaTime;
            float ayagaIlerleme = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(ayagaGecen / OturmadanAyagaGecisSuresi));

            if (oyuncuKoku != null)
            {
                oyuncuKoku.SetPositionAndRotation(
                    Vector3.Lerp(
                        oturmaHedefPozisyonu,
                        kalkisHedefPozisyonu,
                        ayagaIlerleme),
                    Quaternion.Slerp(
                        oturmaHedefRotasyonu,
                        kalkisHedefRotasyonu,
                        ayagaIlerleme));
            }

            yield return null;
        }

        if (oyuncuKoku != null)
        {
            oyuncuKoku.SetPositionAndRotation(
                kalkisHedefPozisyonu,
                kalkisHedefRotasyonu);
            Physics.SyncTransforms();
        }

        NormalIdlePozunaDon();
        animator.applyRootMotion = oncekiRootMotion;
        kalkisKameraTakibiHazir = false;
        kalkisaKadarKamerayiGozlereKilitle = false;

        // Ayakta Idle pozu uygulandiktan sonra mevcut FPS sistemini yeniden
        // kur. Bu, yataktaki asagi bakan kamera rotasyonunu sifirlar ve kamerayi
        // ayaktaki gercek kafa/goz yuksekligine tek karede tasir.
        kameraSistemiFPSOlarakBildirildi = false;
        BirinciSahisBaslangiciniKur();
        MevcutFPSKameraPozunuYenile();

        // Intro kamera takibi burada biter; bundan sonra normal FPS sistemi
        // kamerayi yonetir ve Animator normal idle state'inde kalir.
        asama = SahneAsamasi.Bitti;
        yield return GorevGoster();
        acilisTamamlandi?.Invoke();
    }

    private IEnumerator DiyalogGoster(
        string konusan,
        string metin,
        AudioClip ses,
        float varsayilanSure)
    {
        konusanText.text = konusan;
        altyaziText.text = metin;
        altyaziGrubu.alpha = 1f;

        float sure = varsayilanSure;
        if (ses != null && konusmaAudioSource != null)
        {
            konusmaAudioSource.Stop();
            konusmaAudioSource.clip = ses;
            konusmaAudioSource.Play();
            sure = Mathf.Max(sure, ses.length);
        }

        float gecen = 0f;
        while (gecen < sure)
        {
            gecen += Time.unscaledDeltaTime;

            // Konusma boyunca el kulak hedefini korur. Yalniz milimetrik bir
            // canlilik vardir; telefon ele parent oldugu icin ayri tasinmaz.
            if (asama == SahneAsamasi.TelefondaKonusuyor)
            {
                ikHedefPozisyon = KulakElHedefPozisyonu() +
                    (kafa != null ? kafa.up : Vector3.up) *
                    Mathf.Sin(Time.time * 2.1f) * 0.0015f;
                ikHedefRotasyon = KulakElHedefRotasyonu();
                elIKAgirligi = guvenliElIKAgirligi;
            }

            yield return null;
        }
    }

    private IEnumerator GorevGoster()
    {
        gorevText.text = "YENİ HEDEF\n<size=28>" + ilkGorevMetni + "</size>";
        gorevGrubu.alpha = 0f;

        float sure = 0f;
        while (sure < 0.35f)
        {
            sure += Time.unscaledDeltaTime;
            gorevGrubu.alpha = Mathf.Clamp01(sure / 0.35f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(gorevYazisiSuresi);

        sure = 0f;
        while (sure < 0.5f)
        {
            sure += Time.unscaledDeltaTime;
            gorevGrubu.alpha = 1f - Mathf.Clamp01(sure / 0.5f);
            yield return null;
        }
    }

    public void IKUygula(int layerIndex)
    {
        if (animator == null || !animator.isHuman)
            return;

        // Kafa/omurga LookAt IK ile cekilmez. Yalniz sag el, sinirli agirlik
        // ve dirsek yonlendirmesiyle telefon hedefine gider.
        animator.SetLookAtWeight(0f);
        float agirlik = Mathf.Clamp01(elIKAgirligi);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, agirlik);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, agirlik * 0.72f);
        animator.SetIKPosition(AvatarIKGoal.RightHand, ikHedefPozisyon);
        animator.SetIKRotation(AvatarIKGoal.RightHand, ikHedefRotasyon);

        float dirsekAgirligi = agirlik * 0.82f;
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, dirsekAgirligi);
        animator.SetIKHintPosition(
            AvatarIKHint.RightElbow,
            SagDirsekGuvenliHedefi());
    }

    private bool TelefonaBakiliyorMu()
    {
        if (oyuncuKamerasi == null || telefon == null)
            return false;

        Ray ray = oyuncuKamerasi.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, telefonEtkilesimMesafesi, ~0, QueryTriggerInteraction.Collide))
        {
            if ((telefonCollider != null && hit.collider == telefonCollider) ||
                hit.collider.transform.IsChildOf(telefon))
            {
                return true;
            }
        }

        // Carsaf, yatak veya masa collider'i merkez isinini cok kolay kesebilir.
        // Telefon ekranda merkeze yeterince yakinsa etkilesimi yine kabul et.
        Vector3 telefonaDogru = telefon.position - oyuncuKamerasi.transform.position;
        float mesafe = telefonaDogru.magnitude;
        if (mesafe < 0.001f || mesafe > telefonEtkilesimMesafesi)
            return false;

        float merkezeYakinlik = Vector3.Dot(
            oyuncuKamerasi.transform.forward,
            telefonaDogru / mesafe);

        return merkezeYakinlik >= 0.93f;
    }

    private bool TelefonYatakEtkilesimMesafesindeMi()
    {
        if (oyuncuKamerasi == null || telefon == null)
            return false;

        // Telefon yatagin yanindaysa collider veya bakis acisi yuzunden
        // etkilesim kaybolmasin. Uzak mesafeden acilmasina yine izin verme.
        float garantiMesafe = Mathf.Min(2.35f, telefonEtkilesimMesafesi);
        return Vector3.Distance(
            oyuncuKamerasi.transform.position,
            telefon.position) <= garantiMesafe;
    }

    private void TelefonuCaldir()
    {
        if (telefonAudioSource == null)
        {
            Debug.LogError("OyunAcilisSahnesi: Telefon AudioSource olusturulamadi.", this);
            return;
        }

        if (telefonCalmaSesi == null)
        {
            Debug.LogError("OyunAcilisSahnesi: Telefon Calma Sesi alani bos.", this);
            return;
        }

        telefonZiliCalmali = true;

        if (telefonCalmaSesi.loadState == AudioDataLoadState.Unloaded)
            telefonCalmaSesi.LoadAudioData();

        if (telefonCalmaSesi.loadState == AudioDataLoadState.Loading)
            return;

        if (telefonCalmaSesi.loadState == AudioDataLoadState.Failed)
        {
            telefonZiliCalmali = false;
            Debug.LogError(
                "OyunAcilisSahnesi: Telefon zil klibinin ses verisi yuklenemedi.",
                this);
            return;
        }

        telefonAudioSource.Stop();
        telefonAudioSource.enabled = true;
        telefonAudioSource.mute = false;
        telefonAudioSource.clip = telefonCalmaSesi;
        telefonAudioSource.loop = true;
        // Acilis zilini tamamen 2D cal: kamera/telefon pivotu veya mesafe sesi kesemez.
        telefonAudioSource.spatialBlend = 0f;
        telefonAudioSource.volume = 1f;
        telefonAudioSource.time = 0f;
        telefonAudioSource.Play();
    }

    private void TelefonuEleBagla()
    {
        if (telefon == null || sagEl == null)
            return;

        if (!SahneTransformuMu(telefon))
        {
            Transform sahneTelefonu = AyniAdliSahneTransformunuBul(telefon.name);
            if (sahneTelefonu != null)
                telefon = sahneTelefonu;
        }

        if (!SahneTransformuMu(telefon) || !SahneTransformuMu(sagEl))
        {
            Debug.LogError(
                "OyunAcilisSahnesi: Telefon veya sag el bir Prefab asset Transform'u. " +
                "Project'teki prefab yerine Hierarchy'deki sahne nesnesini kullan.",
                this);
            return;
        }

        telefon.SetParent(sagEl, false);
        telefon.localPosition = telefonElLocalKonumu;
        telefon.localRotation = Quaternion.Euler(telefonElLocalAcisi);
        if (telefonCollider != null)
            telefonCollider.enabled = false;
    }

    private void TelefonuCebeKoy()
    {
        if (telefon == null)
            return;

        if (telefonCepNoktasi != null)
        {
            telefon.SetParent(telefonCepNoktasi, false);
            telefon.localPosition = Vector3.zero;
            telefon.localRotation = Quaternion.identity;
        }

        if (cebeKoyuncaTelefonuGizle)
            telefon.gameObject.SetActive(false);
    }

    private Vector3 KulakDunyaPozisyonu()
    {
        if (telefonKulakNoktasi != null)
            return telefonKulakNoktasi.position;

        if (kafa == null)
            return telefon != null ? telefon.position : transform.position;

        Transform yonReferansi = oyuncuKamerasi != null
            ? oyuncuKamerasi.transform
            : kafa;
        return kafa.position +
               yonReferansi.right * kulakPozisyonOffseti.x +
               yonReferansi.up * kulakPozisyonOffseti.y +
               yonReferansi.forward * kulakPozisyonOffseti.z;
    }

    private Quaternion KulakElRotasyonu()
    {
        Quaternion temel = oyuncuKamerasi != null
            ? oyuncuKamerasi.transform.rotation
            : (kafa != null ? kafa.rotation : transform.rotation);
        return temel * Quaternion.Euler(elIKRotasyonOffseti);
    }

    private Quaternion KulakElHedefRotasyonu()
    {
        if (telefonKulakNoktasi == null)
            return KulakElRotasyonu();

        Quaternion telefonLocalRotasyonu =
            Quaternion.Euler(telefonElLocalAcisi);
        return telefonKulakNoktasi.rotation *
               Quaternion.Inverse(telefonLocalRotasyonu);
    }

    private Vector3 KulakElHedefPozisyonu()
    {
        Quaternion elRotasyonu = KulakElHedefRotasyonu();
        return KulakDunyaPozisyonu() -
               elRotasyonu * telefonElLocalKonumu;
    }

    private Vector3 SagDirsekGuvenliHedefi()
    {
        Transform kolReferansi = sagAltKol != null
            ? sagAltKol
            : sagUstKol;
        Vector3 baslangic = kolReferansi != null
            ? kolReferansi.position
            : (sagEl != null ? sagEl.position : transform.position);
        Transform yonReferansi = oyuncuKamerasi != null
            ? oyuncuKamerasi.transform
            : (kafa != null ? kafa : animator.transform);

        // Dirsegi karakterin sag disina ve cok az geriye tut. Bu ipucu,
        // Humanoid IK'nin kolu kafa/ense icinden gecirip bukmesini engeller.
        return baslangic +
               yonReferansi.right * 0.28f -
               yonReferansi.forward * 0.08f;
    }

    private Vector3 CepDunyaPozisyonu()
    {
        Transform referans = kalca != null ? kalca : animator.transform;
        if (telefonCepNoktasi != null)
            return telefonCepNoktasi.position;

        return referans.position +
               referans.right * cepPozisyonOffseti.x +
               referans.up * cepPozisyonOffseti.y +
               referans.forward * cepPozisyonOffseti.z;
    }

    private Quaternion CepElRotasyonu()
    {
        Transform referans = kalca != null ? kalca : animator.transform;
        return referans.rotation * Quaternion.Euler(10f, 80f, 85f);
    }

    private void YatakIdleOynat()
    {
        int hash = Animator.StringToHash(yatakIdleStateAdi);
        if (animator.HasState(0, hash))
            animator.Play(hash, 0, 0f);
        else
            Debug.LogWarning("Yatak idle state bulunamadi: " + yatakIdleStateAdi, this);
    }

    private bool YataktanKalkisiOynat()
    {
        // KarakterBeyni.controller incelendi: bu state'in Speed degeri -1.
        // Bu nedenle 0'dan baslatmak ilk karede takilir; SON KAREDEN baslat.
        const string kesinStateAdi = "Base Layer.YataktanKalk";
        int hash = Animator.StringToHash(kesinStateAdi);
        if (!animator.HasState(0, hash))
            hash = Animator.StringToHash(yataktanKalkStateAdi);

        if (animator.HasState(0, hash))
        {
            KalkisAnimatorParametreleriniTemizle();
            animator.enabled = true;
            animator.speed = 1f;
            aktifKalkisTersOynuyor = true;
            animator.Play(hash, 0, 0.999f);
            animator.Update(0f);
            return true;
        }

        Debug.LogWarning("Kalkis state bulunamadi: " + yataktanKalkStateAdi, this);
        return false;
    }

    private void KalkisAnimatorParametreleriniTemizle()
    {
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parametre in animator.parameters)
        {
            switch (parametre.name)
            {
                case "Jump":
                case "RunJump":
                    if (parametre.type == AnimatorControllerParameterType.Trigger)
                        animator.ResetTrigger(parametre.nameHash);
                    break;

                case "IsOnStairs":
                case "IsStairMoving":
                case "IsStairDescending":
                    if (parametre.type == AnimatorControllerParameterType.Bool)
                        animator.SetBool(parametre.nameHash, false);
                    break;
            }
        }
    }

    private void ReferanslariBul()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null)
            animator = FindFirstObjectByType<Animator>();

        if (oyuncuKoku == null && animator != null)
        {
            CharacterController controller = animator.GetComponentInParent<CharacterController>();
            oyuncuKoku = controller != null ? controller.transform : animator.transform;
        }

        sahneKameralari = FindObjectsByType<Camera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (oyuncuKamerasi == null)
        {
            foreach (Camera kamera in sahneKameralari)
            {
                string ad = kamera.name.ToLowerInvariant();
                if (ad.Contains("birincisahis") ||
                    ad.Contains("birinci sahis") ||
                    ad.Contains("fps"))
                {
                    oyuncuKamerasi = kamera;
                    break;
                }
            }
        }

        if (oyuncuKamerasi == null)
            oyuncuKamerasi = Camera.main;

        if (animator != null && animator.isHuman)
        {
            kafa = animator.GetBoneTransform(HumanBodyBones.Head);
            sagEl = animator.GetBoneTransform(HumanBodyBones.RightHand);
            sagUstKol = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            sagAltKol = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            kalca = animator.GetBoneTransform(HumanBodyBones.Hips);
            solGoz = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            sagGoz = animator.GetBoneTransform(HumanBodyBones.RightEye);
        }

        // Avatar Humanoid olarak taninmasa bile bu projedeki Mixamo iskeletinde
        // gercek kafa kemigini ada gore bul. Boylece kamera asla mevcut eski
        // konumunda (govde/bacak ustunde) kalmaz.
        if (kafa == null && animator != null)
        {
            Transform enIyiAday = null;
            int enIyiPuan = 0;

            foreach (Transform kemik in animator.GetComponentsInChildren<Transform>(true))
            {
                string ad = kemik.name.ToLowerInvariant();
                int puan = 0;

                if (ad.EndsWith(":head", StringComparison.Ordinal))
                    puan = 3;
                else if (ad == "head" || ad == "kafa")
                    puan = 2;
                else if (ad.EndsWith("head", StringComparison.Ordinal))
                    puan = 1;

                if (puan > enIyiPuan)
                {
                    enIyiPuan = puan;
                    enIyiAday = kemik;
                }
            }

            kafa = enIyiAday;
        }
    }

    private static bool SahneTransformuMu(Transform hedef)
    {
        return hedef != null &&
               hedef.gameObject.scene.IsValid() &&
               hedef.gameObject.scene.isLoaded;
    }

    private Transform AyniAdliSahneTransformunuBul(string nesneAdi)
    {
        if (string.IsNullOrWhiteSpace(nesneAdi))
            return null;

        Transform enYakin = null;
        float enKisaMesafe = float.PositiveInfinity;
        Vector3 referansKonum = oyuncuKoku != null
            ? oyuncuKoku.position
            : transform.position;
        Transform[] sahneTransformlari = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform aday in sahneTransformlari)
        {
            if (!SahneTransformuMu(aday) ||
                !string.Equals(aday.name, nesneAdi, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            float mesafe = (aday.position - referansKonum).sqrMagnitude;
            if (mesafe < enKisaMesafe)
            {
                enKisaMesafe = mesafe;
                enYakin = aday;
            }
        }

        return enYakin;
    }

    private void PrefabReferanslariniSahneOrneklerineCevir()
    {
        // Project penceresinden suruklenen prefab Transform'u runtime'da
        // tasinamaz/parent edilemez. Ayni adli Hierarchy ornegini otomatik bul.
        if (telefon != null && !SahneTransformuMu(telefon))
        {
            string prefabAdi = telefon.name;
            Transform sahneTelefonu = AyniAdliSahneTransformunuBul(prefabAdi);
            if (sahneTelefonu != null)
            {
                telefon = sahneTelefonu;
                telefonCollider = telefon.GetComponentInChildren<Collider>(true);
                telefonAudioSource = telefon.GetComponent<AudioSource>();
                Debug.Log(
                    "OyunAcilisSahnesi: Prefab Telefon referansi, Hierarchy'deki sahne ornegiyle degistirildi.",
                    this);
            }
            else
            {
                Debug.LogError(
                    "OyunAcilisSahnesi: Telefon alaninda prefab asset var fakat Hierarchy'de '" +
                    prefabAdi + "' adli sahne ornegi bulunamadi.",
                    this);
            }
        }

        if (telefonCollider != null &&
            !telefonCollider.gameObject.scene.IsValid())
        {
            telefonCollider = telefon != null
                ? telefon.GetComponentInChildren<Collider>(true)
                : null;
        }

        if (yatakPozNoktasi != null && !SahneTransformuMu(yatakPozNoktasi))
            yatakPozNoktasi = AyniAdliSahneTransformunuBul(yatakPozNoktasi.name);

        if (yatakObjesi != null && !SahneTransformuMu(yatakObjesi))
            yatakObjesi = AyniAdliSahneTransformunuBul(yatakObjesi.name);

        if (yatakKameraNoktasi != null && !SahneTransformuMu(yatakKameraNoktasi))
            yatakKameraNoktasi = AyniAdliSahneTransformunuBul(yatakKameraNoktasi.name);

        if (telefonKulakNoktasi != null && !SahneTransformuMu(telefonKulakNoktasi))
            telefonKulakNoktasi = AyniAdliSahneTransformunuBul(telefonKulakNoktasi.name);

        if (kalkisSonuNoktasi != null && !SahneTransformuMu(kalkisSonuNoktasi))
            kalkisSonuNoktasi = AyniAdliSahneTransformunuBul(kalkisSonuNoktasi.name);
    }

    private void YatakKamerasiFareBakisiniGuncelle()
    {
        yatakKameraYaw = Mathf.Clamp(
            yatakKameraYaw +
            Input.GetAxisRaw("Mouse X") * yatakKamerasiMouseHassasiyeti,
            -yatakKamerasiYatayLimit,
            yatakKamerasiYatayLimit);

        yatakKameraPitch = Mathf.Clamp(
            yatakKameraPitch -
            Input.GetAxisRaw("Mouse Y") * yatakKamerasiMouseHassasiyeti,
            -yatakKamerasiDikeyLimit,
            yatakKamerasiDikeyLimit);
    }

    private Vector3 GercekGozMerkeziniAl()
    {
        // Bu sahnede sag/sol goz nesnelerinin mesh pivotlari karakter kokunde
        // kalabiliyor. Kamera icin tek guvenilir referans Humanoid Head kemigidir.
        // Kullanici istegi: kamerayi dogrudan KAFANIN oldugu yere al.
        if (kafa != null)
            return kafa.position;

        if (solGoz != null && sagGoz != null)
            return (GozDunyaMerkeziniAl(solGoz) + GozDunyaMerkeziniAl(sagGoz)) * 0.5f;

        if (solGoz != null)
            return GozDunyaMerkeziniAl(solGoz);

        if (sagGoz != null)
            return GozDunyaMerkeziniAl(sagGoz);

        return oyuncuKamerasi != null
            ? oyuncuKamerasi.transform.position
            : transform.position;
    }

    private static Vector3 GozDunyaMerkeziniAl(Transform goz)
    {
        if (goz == null)
            return Vector3.zero;

        // Bu modelde "sag goz / sol goz" ayri mesh nesneleri olabilir ve
        // Transform pivotlari karakter kokunde kalabilir. Kucuk goz renderer'inin
        // bounds merkezi, gercek gorunen goz konumudur.
        Renderer renderer = goz.GetComponentInChildren<Renderer>(true);
        if (renderer != null && renderer.bounds.extents.magnitude < 0.35f)
            return renderer.bounds.center;

        return goz.position;
    }

    private void YatakKameraTemeliniHazirla()
    {
        if (yatakKameraTemeliHazir)
            return;

        if (yatakKameraNoktasi != null)
        {
            yatakKameraTemelRotasyonu =
                YatakKameraNoktasiTemelRotasyonunuAl();
            YatakKameraSabitPozisyonunuHesapla();
            yatakKameraYaw = 0f;
            yatakKameraPitch = 0f;
            KafaKameraRotasyonFarkiniHazirla();
            yatakKameraTemeliHazir = true;
            return;
        }

        Vector3 gozMerkezi = GercekGozMerkeziniAl();
        Vector3 hedef = yatakKamerasiBaslangictaTelefonaBaksin && telefon != null
            ? telefon.position + Vector3.up * 0.025f
            : gozMerkezi + transform.forward;
        Vector3 yon = hedef - gozMerkezi;

        if (yon.sqrMagnitude < 0.0001f)
            yon = transform.forward;

        // Telefonu temel bakis noktasi yapmak, FBX kafa/eye eksenlerinin
        // modele gore ters veya 90 derece olmasindan etkilenmez.
        yatakKameraTemelRotasyonu = Quaternion.LookRotation(
            yon.normalized,
            Vector3.up);
        YatakKameraSabitPozisyonunuHesapla();
        yatakKameraYaw = 0f;
        yatakKameraPitch = 0f;
        KafaKameraRotasyonFarkiniHazirla();
        yatakKameraTemeliHazir = true;
    }

    private void KafaKameraRotasyonFarkiniHazirla()
    {
        if (kafa == null)
            return;

        // Modelin Head kemigi ekseni kamerayla ayni olmak zorunda degil.
        // Ilk pozdaki farki saklayip fare donusunu bu fark uzerinden uygula.
        kafaKameraRotasyonFarki =
            Quaternion.Inverse(yatakKameraTemelRotasyonu) * kafa.rotation;
        kafaKameraRotasyonFarkiHazir = true;
    }

    private Quaternion YatakKameraNoktasiTemelRotasyonunuAl()
    {
        if (yatakKameraNoktasi == null)
            return yatakKameraTemelRotasyonu;

        // Manuel nokta verildiginde konum gibi rotasyona da dokunma.
        // Mavi Z oku kameranin bakacagi yonu birebir belirler.
        return yatakKameraNoktasi.rotation *
               Quaternion.Euler(yatakKameraAciDuzeltmesi);
    }

    private void YatakKameraSabitPozisyonunuHesapla()
    {
        // Kamera noktasinin sahnedeki KONUMUNU kullanma. Kullanici Empty'yi
        // govdeye koydugunda kamera gogus/bacak hizasinda kaliyordu. Konum her
        // zaman avatarin gercek iki goz merkezinden (yoksa Head kemiginden) gelir.
        // Yatak Kamera Noktasi yalnizca baslangic bakis YONUNU belirler.
        Vector3 temelPozisyon = GercekGozMerkeziniAl();

        Vector3 sabitIleri =
            yatakKameraTemelRotasyonu * Vector3.forward;
        yatakKameraSabitDunyaPozisyonu =
            temelPozisyon +
            sabitIleri * Mathf.Max(0.13f, yatakKamerasiOneCikma);

        // Kafa/yuz geometrisi kameranin onune gecmesin.
        Vector3 kafaReferansi = GercekGozMerkeziniAl();
        float kafaKameraOnunde = Vector3.Dot(
            kafaReferansi - yatakKameraSabitDunyaPozisyonu,
            sabitIleri);
        if (kafaKameraOnunde > -0.12f)
        {
            yatakKameraSabitDunyaPozisyonu +=
                sabitIleri * (kafaKameraOnunde + 0.12f);
        }
    }

    private void YatakFPSKamerasiniUygula()
    {
        if (oyuncuKamerasi == null)
            return;

        YatakKameraTemeliniHazirla();

        Quaternion fareRotasyonu = Quaternion.Euler(
            yatakKameraPitch,
            yatakKameraYaw,
            0f);
        // Baslangic yonunu bir kez al; telefon ele/kulaga giderken kamera
        // telefonu takip edip kendi kendine donmesin.
        Quaternion temelRotasyon = yatakKameraTemelRotasyonu;
        Quaternion sonRotasyon = temelRotasyon * fareRotasyonu;

        // Ekran goruntusundeki istenen konum: ilk goz acilisindan telefon
        // gorusmesinin sonuna kadar ayni. Asamaya gore asagi/yukari oynamaz.
        // Head'den hesaplanan sabit temel konumu daima 30 cm yukarida tut.
        Vector3 kameraPozisyonu =
            yatakKameraSabitDunyaPozisyonu +
            Vector3.up * KalkisBeklemeKameraYuksekligiMetre;

        oyuncuKamerasi.transform.SetPositionAndRotation(
            kameraPozisyonu,
            sonRotasyon);

        // Kamera pivot etrafinda dolanmaz; konumu sabittir. Fare yalniz kamera
        // yonunu ve karakterin Head kemigini ayni miktarda cevirir.
        if (kafa != null && kafaKameraRotasyonFarkiHazir)
            kafa.rotation = sonRotasyon * kafaKameraRotasyonFarki;

        oyuncuKamerasi.nearClipPlane = Mathf.Clamp(
            Mathf.Max(0.12f, yatakKamerasiNearClip),
            0.12f,
            0.3f);
    }

    private void KalkisKameraTakibiniHazirla()
    {
        if (oyuncuKamerasi == null || kafa == null)
            return;

        // Baslangictaki kamera-Head farkini DUNYA uzayinda sakla. Bunu Head'in
        // local uzayinda saklamak, kemik donerken kamerayi ense etrafina dolastirip
        // karakterin arkasina geciriyordu.
        kalkisKameraKafaDunyaOffseti =
            oyuncuKamerasi.transform.position - kafa.position;
        kalkisKameraSabitDunyaRotasyonu =
            oyuncuKamerasi.transform.rotation;
        kalkisKameraTakibiHazir = true;
    }

    private void KalkisFPSKamerasiniUygula()
    {
        if (!kalkisKameraTakibiHazir ||
            oyuncuKamerasi == null ||
            kafa == null)
        {
            return;
        }

        oyuncuKamerasi.transform.SetPositionAndRotation(
            kafa.position + kalkisKameraKafaDunyaOffseti,
            kalkisKameraSabitDunyaRotasyonu);
    }

    private void YatakPozunuHazirla()
    {
        if (oyuncuKoku == null)
            return;

        if (yatakPozNoktasi != null)
        {
            kilitliYatakPozisyonu =
                yatakPozNoktasi.TransformPoint(yatakPozisyonDuzeltmesi);
            kilitliYatakRotasyonu =
                yatakPozNoktasi.rotation * Quaternion.Euler(yatakRotasyonDuzeltmesi);
            yatakPozuHazir = true;
            return;
        }

        if (yatakObjesi == null)
            yatakObjesi = EnYakinYatakObjesiniBul();

        // Oyuncu zaten yatagin uzerine yerlestirildiyse en guvenilir sonuc,
        // karaktere ait olmayan ilk yuzeye yukaridan isin atmaktir.
        // Isini tavandan baslatma: yukaridan uzun isin atilinca odanin tavani
        // yatak sanilabiliyordu. Oyuncu kokunun hemen ustunden asagi bak.
        Vector3 isinBaslangici = oyuncuKoku.position + Vector3.up * 0.75f;
        RaycastHit[] vuruslar = Physics.RaycastAll(
            isinBaslangici,
            Vector3.down,
            5f,
            ~0,
            QueryTriggerInteraction.Ignore);
        Array.Sort(vuruslar, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit vurus in vuruslar)
        {
            Transform vurulan = vurus.collider.transform;
            if (vurulan == oyuncuKoku || vurulan.IsChildOf(oyuncuKoku))
                continue;

            // Tavanin alt yuzu veya dik bir duvar yatak yuzeyi olamaz.
            if (vurus.normal.y < 0.45f)
                continue;

            // Yatak ismiyle bir obje bulunduysa baska yuzeyleri (tavan/zemin)
            // kesinlikle kabul etme.
            if (yatakObjesi != null &&
                vurulan != yatakObjesi &&
                !vurulan.IsChildOf(yatakObjesi))
            {
                continue;
            }

            kilitliYatakPozisyonu = vurus.point + yatakPozisyonDuzeltmesi;
            kilitliYatakRotasyonu =
                oyuncuKoku.rotation * Quaternion.Euler(yatakRotasyonDuzeltmesi);
            yatakPozuHazir = true;
            return;
        }

        // Yatak modelinde collider yoksa renderer sinirlarindan yatak ustunu bul.
        if (yatakObjesi != null && YatakUstYuksekliginiBul(yatakObjesi, out float yatakUstu))
        {
            kilitliYatakPozisyonu = new Vector3(
                oyuncuKoku.position.x + yatakPozisyonDuzeltmesi.x,
                yatakUstu + yatakPozisyonDuzeltmesi.y,
                oyuncuKoku.position.z + yatakPozisyonDuzeltmesi.z);
            kilitliYatakRotasyonu =
                oyuncuKoku.rotation * Quaternion.Euler(yatakRotasyonDuzeltmesi);
            yatakPozuHazir = true;
            return;
        }

        // Sahne kurulumu eksik olsa bile karakteri baska bir yere zipl atma.
        kilitliYatakPozisyonu = oyuncuKoku.position;
        kilitliYatakRotasyonu = oyuncuKoku.rotation;
        yatakPozuHazir = true;
        Debug.LogWarning(
            "OyunAcilisSahnesi: Yatak yuzeyi otomatik bulunamadi. " +
            "Yatagin ustune bir Empty koyup Yatak Poz Noktasi alanina ver.",
            this);
    }

    private Transform EnYakinYatakObjesiniBul()
    {
        Transform enYakin = null;
        float enKisaMesafe = float.PositiveInfinity;
        Transform[] tumTransformlar = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform aday in tumTransformlar)
        {
            if (aday == oyuncuKoku || aday.IsChildOf(oyuncuKoku))
                continue;

            string ad = aday.name.ToLowerInvariant();
            bool yatakAdi = ad.Contains("bed") ||
                            ad.Contains("yatak") ||
                            ad.Contains("mattress") ||
                            ad.Contains("dosek");
            if (!yatakAdi)
                continue;

            float mesafe = (aday.position - oyuncuKoku.position).sqrMagnitude;
            if (mesafe < enKisaMesafe)
            {
                enKisaMesafe = mesafe;
                enYakin = aday;
            }
        }

        return enYakin;
    }

    private bool YatakUstYuksekliginiBul(Transform yatak, out float yukseklik)
    {
        yukseklik = float.NegativeInfinity;
        bool bulundu = false;
        Vector3 oyuncuKonumu = oyuncuKoku.position;
        Renderer[] renderlar = yatak.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer render in renderlar)
        {
            Bounds sinir = render.bounds;
            bool oyuncununAltinda =
                oyuncuKonumu.x >= sinir.min.x - 0.35f &&
                oyuncuKonumu.x <= sinir.max.x + 0.35f &&
                oyuncuKonumu.z >= sinir.min.z - 0.35f &&
                oyuncuKonumu.z <= sinir.max.z + 0.35f;

            if (!oyuncununAltinda)
                continue;

            if (sinir.max.y > yukseklik)
            {
                yukseklik = sinir.max.y;
                bulundu = true;
            }
        }

        return bulundu;
    }

    private void KarakteriYatakPozunaAl()
    {
        if (!yatakPozuHazir || oyuncuKoku == null)
            return;

        // Onceki surumde oyuncu KOKU yatak yuzeyine konuyordu. Yatma
        // animasyonundaki Hips yuksekligi bunun ustune eklenince karakter
        // havaya/tavana cikiyordu. Burada animasyon o frame uygulandiktan
        // sonra asil kalca kemigini minder yuzeyine hizaliyoruz.
        oyuncuKoku.rotation = kilitliYatakRotasyonu;

        Transform hizalamaKemigi = kalca != null ? kalca : oyuncuKoku;
        Vector3 yatakYukari = yatakPozNoktasi != null
            ? yatakPozNoktasi.up
            : Vector3.up;
        Vector3 hedefKalcaPozisyonu =
            kilitliYatakPozisyonu + yatakYukari * kalcaYatakUstuYuksekligi;
        Vector3 duzeltme = hedefKalcaPozisyonu - hizalamaKemigi.position;

        oyuncuKoku.position += duzeltme;
    }

    private void KalkisSonuPozunuHesapla(
        out Vector3 hedefPozisyon,
        out Quaternion hedefRotasyon)
    {
        hedefPozisyon = ilkOyuncuPozisyonu;
        hedefRotasyon = ilkOyuncuRotasyonu;

        if (kalkisSonuNoktasi != null)
        {
            hedefPozisyon = kalkisSonuNoktasi.position;
            hedefRotasyon = kalkisSonuNoktasi.rotation;
        }
        else
        {
            Transform yatakReferansi = yatakPozNoktasi != null
                ? yatakPozNoktasi
                : yatakObjesi;

            if (yatakReferansi == null)
            {
                hedefPozisyon = ilkOyuncuPozisyonu;
                hedefRotasyon = ilkOyuncuRotasyonu;
            }
            else
            {
                // Kullanici istegi: kalkis daima yatagin SOL tarafina dogru.
                Vector3 yan = -yatakReferansi.right;
                hedefPozisyon = yatakReferansi.position +
                    yan * Mathf.Max(0.55f, kalkisYatakYanMesafesi);

                // Yatagin yanindan asagi bakip gercek zemini bul. Karakterin ve
                // yatagin kendi collider'lari kalkis noktasi olarak kabul edilmez.
                RaycastHit[] vuruslar = Physics.RaycastAll(
                    hedefPozisyon + Vector3.up * 2.5f,
                    Vector3.down,
                    6f,
                    ~0,
                    QueryTriggerInteraction.Ignore);
                Array.Sort(vuruslar, (a, b) => a.distance.CompareTo(b.distance));

                bool zeminBulundu = false;
                foreach (RaycastHit vurus in vuruslar)
                {
                    Transform vurulan = vurus.collider.transform;
                    if (vurulan == oyuncuKoku || vurulan.IsChildOf(oyuncuKoku))
                        continue;
                    if (yatakObjesi != null &&
                        (vurulan == yatakObjesi || vurulan.IsChildOf(yatakObjesi)))
                    {
                        continue;
                    }
                    if (vurus.normal.y < 0.55f)
                        continue;
                    // Komodin/masa gibi yataktan yuksek yuzeylere cikma.
                    if (vurus.point.y > yatakReferansi.position.y - 0.18f)
                        continue;

                    hedefPozisyon = vurus.point;
                    zeminBulundu = true;
                    break;
                }

                if (!zeminBulundu)
                    hedefPozisyon.y = ilkOyuncuPozisyonu.y;

                // Karakter yatagin uzunlamasina degil, kalktigi SOL tarafa
                // bakar. Boylece oturma ve ayaga kalkma yonleri ayni kalir.
                Vector3 ileri = Vector3.ProjectOnPlane(yan, Vector3.up);
                hedefRotasyon = ileri.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(ileri.normalized, Vector3.up)
                    : ilkOyuncuRotasyonu;
            }
        }

    }

    private Vector3 KalkisSolYonunuBul(Quaternion yedekRotasyon)
    {
        Transform yatakReferansi = yatakPozNoktasi != null
            ? yatakPozNoktasi
            : yatakObjesi;

        Vector3 solYon = yatakReferansi != null
            ? -yatakReferansi.right
            : yedekRotasyon * Vector3.forward;
        solYon = Vector3.ProjectOnPlane(solYon, Vector3.up);

        if (solYon.sqrMagnitude < 0.001f)
            solYon = Vector3.forward;

        return solYon.normalized;
    }

    private int NormalIdleHashiniBul()
    {
        if (animator == null)
            return 0;

        int idleHash = Animator.StringToHash("Base Layer.idle");
        if (!animator.HasState(0, idleHash))
            idleHash = Animator.StringToHash(normalIdleStateAdi);

        return animator.HasState(0, idleHash) ? idleHash : 0;
    }

    private void NormalIdlePozunaDon()
    {
        if (animator == null)
            return;

        // Controller'daki state adi kucuk harfle "idle". Eski Inspector'da
        // "Idle" kayitli olsa bile dogru state'i kesin olarak kullan.
        const string kesinIdleStateAdi = "Base Layer.idle";
        int idleHash = NormalIdleHashiniBul();

        if (idleHash == 0)
        {
            Debug.LogWarning(
                "OyunAcilisSahnesi: Normal idle state bulunamadi: " +
                kesinIdleStateAdi,
                this);
            return;
        }

        KalkisAnimatorParametreleriniTemizle();
        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(idleHash, 0, 0f);
        animator.Update(0f);
    }

    private void BirinciSahisBaslangiciniKur()
    {
        if (!birinciSahislaBasla || kameraSistemiFPSOlarakBildirildi)
            return;

        MonoBehaviour[] tumScriptler = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        BindingFlags bayraklar =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (MonoBehaviour script in tumScriptler)
        {
            if (script == null)
                continue;

            Type tur = script.GetType();
            string turAdi = tur.Name.ToLowerInvariant();
            if (!turAdi.Contains("birinciucuncusahis") &&
                !turAdi.Contains("cameragecis") &&
                !turAdi.Contains("kameragecis"))
            {
                continue;
            }

            try
            {
                // Mevcut FPS sisteminde Inspector'dan verilmis goz kemikleri
                // varsa aynilarini kullan. Bazi avatarlar LeftEye/RightEye'yi
                // Humanoid haritasinda sunmaz.
                if (solGoz == null)
                {
                    FieldInfo solGozAlani = tur.GetField("solGozKemigi", bayraklar);
                    solGoz = solGozAlani?.GetValue(script) as Transform;
                }

                if (sagGoz == null)
                {
                    FieldInfo sagGozAlani = tur.GetField("sagGozKemigi", bayraklar);
                    sagGoz = sagGozAlani?.GetValue(script) as Transform;
                }

                MethodInfo kameraSabitle = tur.GetMethod(
                    "KameraRiginiSabitle",
                    bayraklar);
                if (kameraSabitle != null && kameraSabitle.GetParameters().Length == 0)
                {
                    mevcutFPSKameraSistemi = script;
                    fpsKameraRiginiSabitleMetodu = kameraSabitle;
                }

                FieldInfo baslangicAlani = tur.GetField(
                    "oyunBaslangicindaBirinciSahis",
                    bayraklar);
                baslangicAlani?.SetValue(script, true);

                FieldInfo aktifAlan = tur.GetField("birinciSahisAktif", bayraklar);
                aktifAlan?.SetValue(script, true);

                MethodInfo modDegistir = tur.GetMethod("ModDegistir", bayraklar);
                if (modDegistir != null)
                {
                    ParameterInfo[] parametreler = modDegistir.GetParameters();
                    if (parametreler.Length == 2 &&
                        parametreler[0].ParameterType == typeof(bool) &&
                        parametreler[1].ParameterType == typeof(bool))
                    {
                        modDegistir.Invoke(script, new object[] { true, true });
                    }
                    else if (parametreler.Length == 1 &&
                             parametreler[0].ParameterType == typeof(bool))
                    {
                        modDegistir.Invoke(script, new object[] { true });
                    }
                }
            }
            catch (Exception hata)
            {
                Debug.LogWarning(
                    "OyunAcilisSahnesi: Kamera gecisi FPS'e bildirilemedi: " +
                    hata.Message,
                    script);
            }
        }

        kameraSistemiFPSOlarakBildirildi = true;
        FPSKamerasiniZorla();
    }

    private void MevcutFPSKameraPozunuYenile()
    {
        if (mevcutFPSKameraSistemi == null ||
            fpsKameraRiginiSabitleMetodu == null)
        {
            return;
        }

        try
        {
            fpsKameraRiginiSabitleMetodu.Invoke(
                mevcutFPSKameraSistemi,
                null);
        }
        catch (Exception hata)
        {
            if (!fpsKameraYenilemeHatasiYazildi)
            {
                Debug.LogWarning(
                    "OyunAcilisSahnesi: Mevcut FPS kamera pozu yenilenemedi: " +
                    hata.Message,
                    mevcutFPSKameraSistemi);
                fpsKameraYenilemeHatasiYazildi = true;
            }

            fpsKameraRiginiSabitleMetodu = null;
        }
    }

    private void FPSKamerasiniZorla()
    {
        if (oyuncuKamerasi == null)
            return;

        // FPS kamera pasif bir rig altindaysa once butun parent zincirini ac.
        Transform zincir = oyuncuKamerasi.transform;
        while (zincir != null)
        {
            if (!zincir.gameObject.activeSelf)
                zincir.gameObject.SetActive(true);
            if (zincir == oyuncuKoku)
                break;
            zincir = zincir.parent;
        }

        oyuncuKamerasi.enabled = true;
        oyuncuKamerasi.tag = "MainCamera";

        AudioListener fpsDinleyici = oyuncuKamerasi.GetComponent<AudioListener>();
        if (fpsDinleyici != null)
            fpsDinleyici.enabled = true;

        if (sahneKameralari == null || sahneKameralari.Length == 0)
        {
            sahneKameralari = FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        foreach (Camera kamera in sahneKameralari)
        {
            if (kamera == null || kamera == oyuncuKamerasi)
                continue;

            string ad = kamera.name.ToLowerInvariant();
            AudioListener dinleyici = kamera.GetComponent<AudioListener>();
            bool oyuncuRakipKamerasi =
                kamera.CompareTag("MainCamera") ||
                dinleyici != null ||
                ad.Contains("maincamera") ||
                ad.Contains("ucuncu") ||
                ad.Contains("third");

            if (!oyuncuRakipKamerasi)
                continue;

            kamera.enabled = false;
            kamera.tag = "Untagged";
            if (dinleyici != null)
                dinleyici.enabled = false;
        }
    }

    private void IKKoprusunuKur()
    {
        if (animator == null)
            return;

        ikKoprusu = animator.GetComponent<AcilisAnimatorIKKoprusu>();
        if (ikKoprusu == null)
            ikKoprusu = animator.gameObject.AddComponent<AcilisAnimatorIKKoprusu>();
        ikKoprusu.SahneyiAyarla(this);
    }

    private void AudioKaynaklariniHazirla()
    {
        // Zil kaynagi telefona bagli olursa telefon parent/aktiflik degistirince
        // ses kesilebiliyor. Kaynak daima bu yonetici nesnesinde ve 2D calisir.
        if (telefonAudioSource == null || telefonAudioSource.gameObject != gameObject)
            telefonAudioSource = gameObject.AddComponent<AudioSource>();

        telefonAudioSource.enabled = true;
        telefonAudioSource.playOnAwake = false;
        telefonAudioSource.ignoreListenerPause = true;
        telefonAudioSource.priority = 0;
        telefonAudioSource.dopplerLevel = 0f;
        telefonAudioSource.spatialBlend = 0f;
        telefonAudioSource.volume = 1f;
        telefonAudioSource.mute = false;
        telefonAudioSource.bypassEffects = true;
        telefonAudioSource.bypassListenerEffects = true;
        telefonAudioSource.bypassReverbZones = true;

        // FPS kamerada AudioListener yoksa zil dahil hicbir ses duyulmaz.
        if (oyuncuKamerasi != null)
        {
            AudioListener dinleyici = oyuncuKamerasi.GetComponent<AudioListener>();
            if (dinleyici == null)
                dinleyici = oyuncuKamerasi.gameObject.AddComponent<AudioListener>();
            dinleyici.enabled = true;

            AudioListener[] tumDinleyiciler = FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (AudioListener digerDinleyici in tumDinleyiciler)
            {
                if (digerDinleyici != null && digerDinleyici != dinleyici)
                    digerDinleyici.enabled = false;
            }
        }

        AudioListener.pause = false;
        AudioListener.volume = 1f;

        if (konusmaAudioSource == null)
            konusmaAudioSource = gameObject.AddComponent<AudioSource>();
        konusmaAudioSource.spatialBlend = 0f;
        konusmaAudioSource.ignoreListenerPause = true;
    }

    private void ArayuzuKur()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObj = new GameObject("AcilisSahnesi_UI");
        canvasObj.transform.SetParent(transform, false);
        arayuzCanvas = canvasObj.AddComponent<Canvas>();
        arayuzCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        arayuzCanvas.sortingOrder = 5000;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        Image perde = Resim("Siyah_Perde", canvasObj.transform, Color.black);
        TamEkran(perde.rectTransform);
        siyahPerde = perde.gameObject.AddComponent<CanvasGroup>();
        siyahPerde.alpha = 1f;
        perde.raycastTarget = false;

        GameObject etkilesim = UIObjesi("Etkilesim", canvasObj.transform);
        RectTransform etkilesimRect = etkilesim.GetComponent<RectTransform>();
        etkilesimRect.anchorMin = new Vector2(0.5f, 0.5f);
        etkilesimRect.anchorMax = new Vector2(0.5f, 0.5f);
        etkilesimRect.pivot = new Vector2(0.5f, 0.5f);
        etkilesimRect.anchoredPosition = new Vector2(0f, -130f);
        etkilesimRect.sizeDelta = new Vector2(360f, 54f);
        Image etkilesimZemin = etkilesim.AddComponent<Image>();
        etkilesimZemin.sprite = DuzSprite();
        etkilesimZemin.color = new Color(0f, 0f, 0f, 0.68f);
        etkilesimGrubu = etkilesim.AddComponent<CanvasGroup>();
        etkilesimText = Yazi("Yazi", etkilesim.transform, "[E]  TELEFONU AÇ", 20, FontStyle.Bold, Color.white);
        TamEkran(etkilesimText.rectTransform);
        etkilesimText.alignment = TextAnchor.MiddleCenter;

        GameObject altyazi = UIObjesi("Altyazi", canvasObj.transform);
        RectTransform altyaziRect = altyazi.GetComponent<RectTransform>();
        altyaziRect.anchorMin = new Vector2(0.5f, 0f);
        altyaziRect.anchorMax = new Vector2(0.5f, 0f);
        altyaziRect.pivot = new Vector2(0.5f, 0f);
        altyaziRect.anchoredPosition = new Vector2(0f, 72f);
        altyaziRect.sizeDelta = new Vector2(1300f, 165f);
        Image altyaziZemin = altyazi.AddComponent<Image>();
        altyaziZemin.sprite = DuzSprite();
        altyaziZemin.color = new Color(0f, 0f, 0f, 0.76f);
        altyaziGrubu = altyazi.AddComponent<CanvasGroup>();

        konusanText = Yazi("Konusan", altyazi.transform, "", 17, FontStyle.Bold, new Color(0.92f, 0.18f, 0.12f));
        RectTransform konusanRect = konusanText.rectTransform;
        konusanRect.anchorMin = new Vector2(0f, 1f);
        konusanRect.anchorMax = new Vector2(1f, 1f);
        konusanRect.pivot = new Vector2(0f, 1f);
        konusanRect.anchoredPosition = new Vector2(42f, -25f);
        konusanRect.sizeDelta = new Vector2(-84f, 30f);

        altyaziText = Yazi("Metin", altyazi.transform, "", 26, FontStyle.Normal, Color.white);
        RectTransform metinRect = altyaziText.rectTransform;
        metinRect.anchorMin = new Vector2(0f, 0f);
        metinRect.anchorMax = new Vector2(1f, 1f);
        metinRect.offsetMin = new Vector2(42f, 24f);
        metinRect.offsetMax = new Vector2(-42f, -57f);
        altyaziText.alignment = TextAnchor.UpperLeft;

        GameObject gorev = UIObjesi("Yeni_Gorev", canvasObj.transform);
        RectTransform gorevRect = gorev.GetComponent<RectTransform>();
        gorevRect.anchorMin = new Vector2(0.5f, 1f);
        gorevRect.anchorMax = new Vector2(0.5f, 1f);
        gorevRect.pivot = new Vector2(0.5f, 1f);
        gorevRect.anchoredPosition = new Vector2(0f, -85f);
        gorevRect.sizeDelta = new Vector2(720f, 115f);
        Image gorevZemin = gorev.AddComponent<Image>();
        gorevZemin.sprite = DuzSprite();
        gorevZemin.color = new Color(0f, 0f, 0f, 0.72f);
        gorevGrubu = gorev.AddComponent<CanvasGroup>();
        gorevText = Yazi("Yazi", gorev.transform, "", 19, FontStyle.Bold, Color.white);
        TamEkran(gorevText.rectTransform);
        gorevText.alignment = TextAnchor.MiddleCenter;

        gozKapagiMaskesi = Resim(
            "Organik_Goz_Kapagi_Maskesi",
            canvasObj.transform,
            Color.white);
        TamEkran(gozKapagiMaskesi.rectTransform);
        gozKapagiMaskesi.sprite = GozKapagiMaskesiSpriteOlustur();
        gozKapagiMaskesi.raycastTarget = false;

        perde.transform.SetAsLastSibling();
        gozKapagiMaskesi.transform.SetAsLastSibling();
        GozKapagiKapaliliginiAyarla(1f);
    }

    private IEnumerator GozKapagiAnimasyonu(
        float baslangic,
        float bitis,
        float sure)
    {
        if (sure <= 0f)
        {
            GozKapagiKapaliliginiAyarla(bitis);
            yield break;
        }

        float gecen = 0f;
        while (gecen < sure)
        {
            gecen += Time.unscaledDeltaTime;
            float t = Yumusa(Mathf.Clamp01(gecen / sure));
            GozKapagiKapaliliginiAyarla(Mathf.Lerp(baslangic, bitis, t));
            yield return null;
        }

        GozKapagiKapaliliginiAyarla(bitis);
    }

    private void GozKapagiKapaliliginiAyarla(float kapalilik)
    {
        if (gozKapagiMaskesi == null ||
            gozKapagiMaskTexture == null ||
            gozKapagiPikselleri == null)
        {
            return;
        }

        float kapalilik01 = Mathf.Clamp01(kapalilik);
        if (kapalilik01 <= 0.001f)
        {
            gozKapagiMaskesi.enabled = false;
            return;
        }

        gozKapagiMaskesi.enabled = true;
        int genislik = gozKapagiMaskTexture.width;
        int yukseklik = gozKapagiMaskTexture.height;

        if (kapalilik01 >= 0.995f)
        {
            Color32 tamSiyah = new Color32(0, 0, 0, 255);
            for (int i = 0; i < gozKapagiPikselleri.Length; i++)
                gozKapagiPikselleri[i] = tamSiyah;
        }
        else
        {
            float aciklik = 1f - kapalilik01;
            const float yumusakKenar = 0.035f;

            for (int y = 0; y < yukseklik; y++)
            {
                float dikey = ((y + 0.5f) / yukseklik) * 2f - 1f;

                for (int x = 0; x < genislik; x++)
                {
                    // Ortada oval, iki yanda sivrilen organik goz acikligi.
                    float egri = gozKapagiYatayEgrisi[x];
                    float ustSinir = aciklik * (0.91f * egri + 0.018f);
                    float altSinir = -aciklik * (0.77f * egri + 0.018f);

                    float ustAlfa = Mathf.SmoothStep(
                        ustSinir - yumusakKenar,
                        ustSinir + yumusakKenar,
                        dikey);
                    float altAlfa = 1f - Mathf.SmoothStep(
                        altSinir - yumusakKenar,
                        altSinir + yumusakKenar,
                        dikey);
                    float alfa = Mathf.Max(ustAlfa, altAlfa);

                    gozKapagiPikselleri[y * genislik + x] =
                        new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alfa * 255f));
                }
            }
        }

        gozKapagiMaskTexture.SetPixels32(gozKapagiPikselleri);
        gozKapagiMaskTexture.Apply(false, false);
    }

    private Sprite GozKapagiMaskesiSpriteOlustur()
    {
        const int genislik = 256;
        const int yukseklik = 144;

        gozKapagiMaskTexture = new Texture2D(
            genislik,
            yukseklik,
            TextureFormat.RGBA32,
            false);
        gozKapagiMaskTexture.name = "Organik_Goz_Kapagi_Maskesi_Runtime";
        gozKapagiMaskTexture.wrapMode = TextureWrapMode.Clamp;
        gozKapagiMaskTexture.filterMode = FilterMode.Bilinear;
        gozKapagiPikselleri = new Color32[genislik * yukseklik];
        gozKapagiYatayEgrisi = new float[genislik];

        for (int x = 0; x < genislik; x++)
        {
            float yatay = ((x + 0.5f) / genislik) * 2f - 1f;
            float oval = Mathf.Max(0f, 1f - yatay * yatay);
            gozKapagiYatayEgrisi[x] = Mathf.Pow(oval, 0.55f);
        }

        return Sprite.Create(
            gozKapagiMaskTexture,
            new Rect(0f, 0f, genislik, yukseklik),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private static float Yumusa(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private GameObject UIObjesi(string ad, Transform parent)
    {
        GameObject obje = new GameObject(ad, typeof(RectTransform));
        obje.transform.SetParent(parent, false);
        return obje;
    }

    private Text Yazi(string ad, Transform parent, string metin, int boyut, FontStyle stil, Color renk)
    {
        GameObject obje = UIObjesi(ad, parent);
        Text text = obje.AddComponent<Text>();
        text.font = font;
        text.text = metin;
        text.fontSize = boyut;
        text.fontStyle = stil;
        text.color = renk;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private Image Resim(string ad, Transform parent, Color renk)
    {
        GameObject obje = UIObjesi(ad, parent);
        Image image = obje.AddComponent<Image>();
        image.sprite = DuzSprite();
        image.color = renk;
        return image;
    }

    private Sprite DuzSprite()
    {
        if (duzTexture == null)
        {
            duzTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            duzTexture.SetPixel(0, 0, Color.white);
            duzTexture.Apply();
        }
        return Sprite.Create(duzTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    private static void TamEkran(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (duzTexture != null)
            Destroy(duzTexture);
        if (gozKapagiMaskTexture != null)
            Destroy(gozKapagiMaskTexture);
    }
}

/// <summary>
/// OnAnimatorIK mesaji yalniz Animator'un bulundugu GameObject'e gelir.
/// Bu kopru ana sahne yoneticisine mesaji aktarir.
/// </summary>
internal sealed class AcilisAnimatorIKKoprusu : MonoBehaviour
{
    private OyunAcilisSahnesi sahne;

    public void SahneyiAyarla(OyunAcilisSahnesi hedef)
    {
        sahne = hedef;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (sahne != null)
            sahne.IKUygula(layerIndex);
    }
}
