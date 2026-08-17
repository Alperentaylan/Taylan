using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class KarakterHareketi : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;

    [Header("Hareket Ayarları")]
    public float yurumeHizi = 3f;
    public float kosmaHizi = 6f;
    public float ziplamaGucu = 5f;
    public float yercekimi = -9.81f;

    [Header("Zıplama Hareketi")]
    public float yuruyerekZiplamaIleriHizi = 2.5f;
    public float kosarakZiplamaIleriHizi = 5f;
    public float havadaSagaSolaHizi = 2f;

    [Header("Eğilme Ayarları")]
    public bool egiliyorMu;
    public float egilerekYurumeHizi = 1.5f;
    public float egilerekHizliYurumeHizi = 2.5f;
    public float egilerekAnimasyonHizCarpani = 1.6f;
    public float kalkmaAnimasyonSuresi = 1f;

    [Header("Apartman Merdiveni")]
    public LayerMask merdivenKatmani;
    public float merdivenYurumeHizi = 2.2f;
    public float merdivenKontrolMesafesi = 0.8f;

    [Header("Duvara Tırmanma")]
    public float duvarKontrolMesafesi = 1.25f;
    public float duvarKontrolYuksekligi = 1f;
    public float duvarKontrolYaricapi = 0.18f;
    public float minimumDuvarYuksekligi = 1.1f;
    public float maksimumDuvarYuksekligi = 2.8f;
    public float duvarUstuAramaPayi = 0.4f;
    public float duvarUstundeIlerleme = 0.55f;
    public float duvarTirmanmaSuresi = 1.2f;
    public bool duvarTirmanmaDebug = true;

    [Header("Düşüş Kontrolü")]
    public float dususBaslangicY;
    public int yuvarlanmisBaslangicY;
    public float gercekDususMesafesi;
    public string secilenDususRotasi;

    [Header("Sigara Sistemi")]
    public KeyCode sigaraSondurmeTusu = KeyCode.E;

    [Header("Sigara Modelleri")]
    [FormerlySerializedAs("sigaraObjesi")]
    public GameObject sigaraModeli;
    public GameObject cakmakModeli;
    public GameObject sigaraPaketiModeli;

    [Header("Sigara Model Bağlama Noktaları")]
    [Tooltip("Boş bırakılırsa sağ işaret parmağı otomatik kullanılır.")]
    public Transform sigaraBaglamaNoktasi;
    [Tooltip("Boş bırakılırsa sol orta parmağın avuca yakın kemiği kullanılır.")]
    public Transform cakmakBaglamaNoktasi;
    [Tooltip("Boş bırakılırsa sol orta parmağın avuca yakın kemiği kullanılır.")]
    public Transform sigaraPaketiBaglamaNoktasi;

    [Header("Sigara Modellerinin Boyutları")]
    public bool modelleriOtomatikBoyutlandir = true;
    [Min(0.01f)]
    public float sigaraHedefUzunlugu = 0.085f;
    [Min(0.01f)]
    public float cakmakHedefUzunlugu = 0.09f;
    [Min(0.01f)]
    public float paketHedefUzunlugu = 0.10f;

    [Header("Sigara Modelleri İnce Ayarı")]
    public Vector3 sigaraYerelKonumDuzeltmesi = Vector3.zero;
    public Vector3 sigaraYerelDonusDuzeltmesi =
        new Vector3(0f, 90f, 0f);
    public Vector3 cakmakYerelKonumDuzeltmesi = Vector3.zero;
    public Vector3 cakmakYerelDonusDuzeltmesi = Vector3.zero;
    public Vector3 paketYerelKonumDuzeltmesi = Vector3.zero;
    public Vector3 paketYerelDonusDuzeltmesi = Vector3.zero;

    [Header("Sigara Yakma Model Zamanları")]
    [Range(0f, 1f)]
    public float paketCikarmaAni = 0.05f;
    [Range(0f, 1f)]
    public float sigaraPakettenAlmaAni = 0.28f;
    [Range(0f, 1f)]
    public float paketiCebeKoymaAni = 0.48f;
    [Range(0f, 1f)]
    public float cakmakCikarmaAni = 0.52f;
    [Range(0f, 1f)]
    public float cakmakGizlemeAni = 0.90f;

    public Transform agizDumanNoktasi;
    [Range(0.1f, 0.9f)]
    public float dumanCikisAnimasyonOrani = 0.55f;
    public float sondurmeYazisiGecikmesi = 5f;
    public float sigaraAnimasyonGecisSuresi = 0.15f;
    public Vector2 nefeslerArasiBekleme =
        new Vector2(0.25f, 0.65f);
    [Range(3, 30)]
    public int birNefesteDumanParcacigi = 12;
    [Range(1f, 4f)]
    public float dumanYogunlukCarpani = 1.75f;
    public float dumanAkisSuresi = 1.8f;

    private const float MINIMUM_GERCEK_DUSUS = 0.5f;

    private Vector3 dikeyHiz;
    private Vector3 ziplamaYatayHizi;

    private bool havadaMi;
    private bool ziplamaIleHavayaCiktiMi;
    private bool havaAnimasyonuBasladiMi;

    private bool kalkiyorMu;
    private bool duvaraTirmaniyorMu;
    private bool merdivenAlanindaMi;
    private Vector3 merdivenYukariYonu = Vector3.zero;
    private bool kisaDususInisAnimasyonuOynuyorMu;
    private bool uzunDususKalkmaAnimasyonuOynuyorMu;

    private bool sigaraAkisiAktifMi;
    private bool sigaraSondurmeIsteniyorMu;
    private bool sondurmeYazisiniGoster;
    private float sondurmeYazisiAlfasi;
    private float sigaraBaslangicZamani;
    private ParticleSystem agizDumanParticleSystem;
    private Material dumanMateryali;
    private Texture2D dumanDokusu;
    private Coroutine aktifDumanAkisi;
    private int sigaraSonrasiAnimatorTemizlemeKaresi;

    // Tırmanırken gövdenin duvarın içine girmesini engeller.
    private bool duvarOnundeKilitlemeAktif;
    private Vector3 aktifDuvarNoktasi;
    private Vector3 aktifDuvarDisNormali;
    private float duvardanMinimumGövdeMesafesi;

    private const string TIRMANILABILIR_DUVAR_LAYER_ADI =
        "TirmanilabilirDuvar";

    private int tirmanilabilirDuvarMaskesi;

    private float sonYerdekiYukseklik;

    private Transform sonGecerliHareketKamerasi;

    private enum DususRotasi
    {
        Yok,
        Kisa,
        Uzun
    }

    private DususRotasi kilitliDususRotasi =
        DususRotasi.Yok;

    // Animator durumları
    private int kisaDususHavaState;
    private int uzunDususHavaState;
    private int kisaDususYereInisState;
    private int uzunDususYereInisState;

    private int duvarTirmanmaState;
    private int merdivenCikmaState;
    private int merdivenInmeState;
    private int egilerekYurumeState;
    private int idleState;
    private int kalkmaStateHash;
    private int sigaraYakmaState;
    private int sigaraIcmeState;
    private int sigaraSondurmeState;
    private int getUpState;

    void Start()
    {
        controller =
            GetComponent<CharacterController>();

        animator =
            GetComponent<Animator>();

        sonYerdekiYukseklik =
            transform.position.y;

        // Düşüş durumları
        kisaDususHavaState =
            Animator.StringToHash(
                "Base Layer.kısa düşüş yol"
            );

        uzunDususHavaState =
            Animator.StringToHash(
                "Base Layer.falling"
            );

        kisaDususYereInisState =
            Animator.StringToHash(
                "Base Layer.düşüş kısa"
            );

        uzunDususYereInisState =
            Animator.StringToHash(
                "Base Layer.uzun düşüş"
            );

        getUpState =
            Animator.StringToHash(
                "Base Layer.get up"
            );

        // Duvar ve merdiven durumları
        duvarTirmanmaState =
            Animator.StringToHash(
                "Base Layer.duvar tırmanma"
            );

        merdivenCikmaState =
            Animator.StringToHash(
                "Base Layer.merdiven çıkma"
            );

        merdivenInmeState =
            Animator.StringToHash(
                "Base Layer.merdiven inme"
            );

        egilerekYurumeState =
            Animator.StringToHash(
                "Base Layer.eğilerek yürüme"
            );

        idleState =
            Animator.StringToHash(
                "Base Layer.Idle"
            );

        kalkmaStateHash =
            Animator.StringToHash("Kalkma");

        sigaraYakmaState =
            Animator.StringToHash(
                "Base Layer.sigara yakma"
            );

        sigaraIcmeState =
            Animator.StringToHash(
                "Base Layer.sigara içme"
            );

        sigaraSondurmeState =
            Animator.StringToHash(
                "Base Layer.sigara söndürme"
            );

        int tirmanilabilirDuvarLayerNumarasi =
            LayerMask.NameToLayer(
                TIRMANILABILIR_DUVAR_LAYER_ADI
            );

        if (tirmanilabilirDuvarLayerNumarasi == -1)
        {
            tirmanilabilirDuvarMaskesi = 0;

            Debug.LogError(
                "'TirmanilabilirDuvar' isimli Layer bulunamadı! " +
                "Unity'de bu Layer'ı oluştur ve tırmanılacak duvarlara ver."
            );
        }
        else
        {
            tirmanilabilirDuvarMaskesi =
                1 << tirmanilabilirDuvarLayerNumarasi;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;

            /*
             * Animator state adlarında Türkçe büyük/küçük harf
             * farkı varsa mevcut olan yazımı otomatik seçer.
             */
            SigaraStateIsimleriniBul();

            animator.SetBool("IsFalling", false);
            animator.SetBool("IsCrouching", false);
            animator.SetBool("IsCrouchWalking", false);
            animator.SetFloat("CrouchSpeedMultiplier", 1f);
            animator.SetBool("IsOnStairs", false);
            animator.SetBool("IsStairMoving", false);
            animator.SetBool("IsStairDescending", false);

            AnimasyonIsimleriniKontrolEt();
        }

        /*
         * Inspector'a Project panelinden bir FBX/prefab verilmişse
         * onu çalışma anında sahneye üretip ele bağlar. Böylece modelin
         * Hierarchy'de önceden bulunması zorunlu değildir.
         */
        SigaraModelleriniHazirla();
        TumSigaraModelleriniGizle();

        AgizDumanSisteminiHazirla();
    }

    private Transform AktifHareketKamerasiniBul()
    {
        Camera mainCamera =
            Camera.main;

        if (mainCamera != null &&
            mainCamera.enabled &&
            mainCamera.gameObject.activeInHierarchy)
        {
            sonGecerliHareketKamerasi =
                mainCamera.transform;

            return sonGecerliHareketKamerasi;
        }

        Camera[] aktifKameralar =
            Camera.allCameras;

        Camera ilkAktifKamera = null;

        for (int i = 0;
             i < aktifKameralar.Length;
             i++)
        {
            Camera kamera =
                aktifKameralar[i];

            if (kamera == null ||
                !kamera.enabled ||
                !kamera.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (ilkAktifKamera == null)
            {
                ilkAktifKamera = kamera;
            }

            string kameraAdi =
                kamera.name.ToLowerInvariant();

            if (kameraAdi.Contains("birincisahis") ||
                kameraAdi.Contains("birinci sahis") ||
                kameraAdi.Contains("fps"))
            {
                sonGecerliHareketKamerasi =
                    kamera.transform;

                return sonGecerliHareketKamerasi;
            }
        }

        if (ilkAktifKamera != null)
        {
            sonGecerliHareketKamerasi =
                ilkAktifKamera.transform;

            return sonGecerliHareketKamerasi;
        }

        if (sonGecerliHareketKamerasi != null)
        {
            return sonGecerliHareketKamerasi;
        }

        return transform;
    }

    void Update()
    {
        // Duvar tırmanışını coroutine yönetiyor
        if (duvaraTirmaniyorMu)
            return;

        // Ayağın altında Merdiven Layer’ı var mı?
        MerdivenZeminKontrolu();

        SigaraGirdisiniKontrolEt();
        SigaraSonrasiAnimatoruTemizle();

        if (sigaraAkisiAktifMi)
        {
            /*
             * Sigara animasyonları sırasında bütün hareketleri
             * kilitler ve CharacterController'ı zeminde tutar.
             */
            if (controller.enabled)
            {
                controller.Move(
                    Vector3.down *
                    2f *
                    Time.deltaTime
                );
            }

            return;
        }

        /*
         * Kalkma state'ine CTRL, duvar tırmanma bağlantısı veya başka
         * bir Animator geçişiyle girilmiş olabilir. State gerçekten
         * oynuyorsa kod değişkeninden bağımsız olarak hareketi kilitle.
         */
        bool animatorKalkmaAnimasyonuAktifMi =
            AnimatorKalkmaAnimasyonuOynuyorMu();

        // CTRL İLE EĞİLME VE KALKMA
        if (Input.GetKeyDown(KeyCode.LeftControl) &&
            controller.isGrounded &&
            !havadaMi &&
            !kalkiyorMu &&
            !kisaDususInisAnimasyonuOynuyorMu &&
            !uzunDususKalkmaAnimasyonuOynuyorMu &&
            !animatorKalkmaAnimasyonuAktifMi &&
            !merdivenAlanindaMi)
        {
            if (!egiliyorMu)
            {
                egiliyorMu = true;

                if (animator != null)
                {
                    animator.SetBool(
                        "IsCrouching",
                        true
                    );

                    animator.SetBool(
                        "IsCrouchWalking",
                        false
                    );

                    animator.SetFloat(
                        "CrouchSpeedMultiplier",
                        1f
                    );
                }
            }
            else
            {
                egiliyorMu = false;
                kalkiyorMu = true;

                if (animator != null)
                {
                    animator.SetBool(
                        "IsCrouching",
                        false
                    );

                    animator.SetBool(
                        "IsCrouchWalking",
                        false
                    );
                }

                StartCoroutine(
                    KalkmaAnimasyonununBitmesiniBekle()
                );
            }

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
                animator.SetBool("isRunning", false);
            }
        }

        /*
         * Eğilmeden kalkma veya uzun düşüşten ayağa kalkma
         * tamamen bitmeden hiçbir hareket girdisi işlenmez.
         */
        if (kalkiyorMu ||
            uzunDususKalkmaAnimasyonuOynuyorMu ||
            animatorKalkmaAnimasyonuAktifMi)
        {
            ziplamaYatayHizi = Vector3.zero;
            dikeyHiz.y = -2f;

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
                animator.SetBool("isRunning", false);
                animator.SetBool("IsStairMoving", false);
                animator.SetBool("IsCrouchWalking", false);
            }

            if (controller.enabled)
            {
                controller.Move(
                    Vector3.down *
                    2f *
                    Time.deltaTime
                );
            }

            return;
        }

        bool kosuyorMu =
            Input.GetKey(KeyCode.LeftShift);

        float yatay =
            Input.GetAxisRaw("Horizontal");

        float dikey =
            Input.GetAxisRaw("Vertical");

        bool hareketKilitliMi =
            kalkiyorMu ||
            kisaDususInisAnimasyonuOynuyorMu ||
            uzunDususKalkmaAnimasyonuOynuyorMu ||
            animatorKalkmaAnimasyonuAktifMi;

        if (hareketKilitliMi)
        {
            yatay = 0f;
            dikey = 0f;
            kosuyorMu = false;
        }

        bool egilirkenShiftBasiliMi =
            egiliyorMu &&
            kosuyorMu &&
            !hareketKilitliMi;

        // Eğilmiş halde koşma yok; yalnızca yavaş yürüyebilir.
        if (egiliyorMu)
        {
            kosuyorMu = false;
        }

        // Merdivende koşma kapalı
        if (merdivenAlanindaMi)
        {
            kosuyorMu = false;
        }

        Transform cam =
            AktifHareketKamerasiniBul();

        Vector3 yon =
            cam.forward * dikey +
            cam.right * yatay;

        yon.y = 0f;

        bool ziplayarakHavadaMi =
            !controller.isGrounded &&
            ziplamaIleHavayaCiktiMi;

        float anlikHiz;

        if (egiliyorMu)
        {
            anlikHiz =
                egilirkenShiftBasiliMi
                ? egilerekHizliYurumeHizi
                : egilerekYurumeHizi;
        }
        else if (merdivenAlanindaMi)
        {
            anlikHiz =
                merdivenYurumeHizi;
        }
        else
        {
            anlikHiz =
                kosuyorMu
                ? kosmaHizi
                : yurumeHizi;
        }

        Vector3 yatayHareket =
            Vector3.zero;

        if (yon.magnitude >= 0.1f)
        {
            if (!ziplayarakHavadaMi)
            {
                Quaternion hedefAci =
                    Quaternion.LookRotation(yon);

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        hedefAci,
                        0.15f
                    );
            }

            yatayHareket =
                yon.normalized * anlikHiz;
        }

        // KARAKTER YERDEYKEN
        if (controller.isGrounded)
        {
            // Yere yeni temas etti
            if (havadaMi)
            {
                havadaMi = false;

                gercekDususMesafesi =
                    Mathf.Max(
                        0f,
                        dususBaslangicY -
                        transform.position.y
                    );

                if (animator != null)
                {
                    animator.SetBool(
                        "IsFalling",
                        false
                    );

                    if (gercekDususMesafesi >=
                        MINIMUM_GERCEK_DUSUS)
                    {
                        if (kilitliDususRotasi ==
                            DususRotasi.Kisa)
                        {
                            KisaDususInisiniBaslat();

                            /*
                             * İnişin gerçekleştiği ilk karede de
                             * hareket ve zıplama uygulanmasın.
                             */
                            hareketKilitliMi = true;
                            kosuyorMu = false;
                            yon = Vector3.zero;
                            yatayHareket = Vector3.zero;
                        }
                        else if (
                            kilitliDususRotasi ==
                            DususRotasi.Uzun
                        )
                        {
                            UzunDususVeKalkmayiBaslat();

                            hareketKilitliMi = true;
                            kosuyorMu = false;
                            yon = Vector3.zero;
                            yatayHareket = Vector3.zero;
                        }
                    }
                }

                kilitliDususRotasi =
                    DususRotasi.Yok;

                ziplamaIleHavayaCiktiMi =
                    false;

                havaAnimasyonuBasladiMi =
                    false;

                ziplamaYatayHizi =
                    Vector3.zero;
            }

            sonYerdekiYukseklik =
                transform.position.y;

            if (dikeyHiz.y < 0f)
            {
                dikeyHiz.y = -2f;
            }

            // SPACE
            if (!hareketKilitliMi &&
                !egiliyorMu &&
                Input.GetKeyDown(KeyCode.Space))
            {
                bool duvaraTirmanmaBasladi =
                    false;

                /*
                 * Tırmanma artık yalnızca koşarken değil;
                 * dururken, yürürken ve koşarken çalışır.
                 *
                 * Hareket tuşu basılıysa gidilen yönü,
                 * karakter duruyorsa baktığı yönü kontrol eder.
                 */
                if (!merdivenAlanindaMi)
                {
                    Vector3 tirmanmaKontrolYonu;

                    if (yon.magnitude >= 0.1f)
                    {
                        tirmanmaKontrolYonu =
                            yon.normalized;
                    }
                    else
                    {
                        tirmanmaKontrolYonu =
                            transform.forward;

                        tirmanmaKontrolYonu.y = 0f;
                        tirmanmaKontrolYonu.Normalize();
                    }

                    duvaraTirmanmaBasladi =
                        DuvarTirmanmaKontrolu(
                            tirmanmaKontrolYonu
                        );
                }

                if (duvaraTirmanmaBasladi)
                {
                    return;
                }

                // Normal zıplama
                ziplamaIleHavayaCiktiMi =
                    true;

                kilitliDususRotasi =
                    DususRotasi.Yok;

                dikeyHiz.y =
                    ziplamaGucu;

                bool hareketEdiyorMu =
                    yon.magnitude >= 0.1f;

                if (hareketEdiyorMu)
                {
                    if (kosuyorMu)
                    {
                        ziplamaYatayHizi =
                            yon.normalized *
                            kosarakZiplamaIleriHizi;
                    }
                    else
                    {
                        ziplamaYatayHizi =
                            yon.normalized *
                            yuruyerekZiplamaIleriHizi;
                    }
                }
                else
                {
                    ziplamaYatayHizi =
                        Vector3.zero;
                }

                yatayHareket =
                    ziplamaYatayHizi;

                if (animator != null)
                {
                    animator.SetBool(
                        "IsFalling",
                        false
                    );

                    animator.SetBool(
                        "IsStairMoving",
                        false
                    );

                    if (kosuyorMu &&
                        hareketEdiyorMu)
                    {
                        animator.SetTrigger(
                            "RunJump"
                        );
                    }
                    else
                    {
                        animator.SetTrigger(
                            "Jump"
                        );
                    }
                }
            }

            // Merdiven animasyonu
            bool merdivendeHareketEdiyorMu =
                merdivenAlanindaMi &&
                yon.magnitude >= 0.1f &&
                !hareketKilitliMi &&
                !egiliyorMu &&
                dikeyHiz.y <= 0f;

            /*
             * Hareket yönü rampanın yukarı yönünün tersiyse
             * karakter merdivenden iniyor demektir.
             */
            bool merdivendenIniyorMu = false;

            if (merdivendeHareketEdiyorMu &&
                merdivenYukariYonu.sqrMagnitude > 0.001f)
            {
                Vector3 yatayMerdivenYukariYonu =
                    merdivenYukariYonu;

                yatayMerdivenYukariYonu.y = 0f;

                if (yatayMerdivenYukariYonu.sqrMagnitude > 0.001f)
                {
                    yatayMerdivenYukariYonu.Normalize();

                    merdivendenIniyorMu =
                        Vector3.Dot(
                            yon.normalized,
                            yatayMerdivenYukariYonu
                        ) < 0f;
                }
            }

            bool egilerekHareketEdiyorMu =
                egiliyorMu &&
                yon.magnitude >= 0.1f &&
                !hareketKilitliMi;

            if (animator != null)
            {
                float egilmeAnimasyonHizi =
                    egilerekHareketEdiyorMu &&
                    egilirkenShiftBasiliMi
                    ? egilerekAnimasyonHizCarpani
                    : 1f;

                animator.SetBool(
                    "IsCrouchWalking",
                    egilerekHareketEdiyorMu
                );

                animator.SetFloat(
                    "CrouchSpeedMultiplier",
                    egilmeAnimasyonHizi
                );

                animator.SetBool(
                    "IsOnStairs",
                    merdivenAlanindaMi
                );

                animator.SetBool(
                    "IsStairMoving",
                    merdivendeHareketEdiyorMu
                );

                animator.SetBool(
                    "IsStairDescending",
                    merdivendenIniyorMu
                );

                if (egiliyorMu)
                {
                    animator.SetBool(
                        "isWalking",
                        false
                    );

                    animator.SetBool(
                        "isRunning",
                        false
                    );
                }
                else if (merdivenAlanindaMi)
                {
                    animator.SetBool(
                        "isWalking",
                        false
                    );

                    animator.SetBool(
                        "isRunning",
                        false
                    );
                }
                else
                {
                    animator.SetBool(
                        "isRunning",
                        yon.magnitude >= 0.1f &&
                        kosuyorMu
                    );

                    animator.SetBool(
                        "isWalking",
                        yon.magnitude >= 0.1f &&
                        !kosuyorMu
                    );
                }
            }
        }

        // KARAKTER HAVADAYKEN
        else
        {
            if (!havadaMi)
            {
                havadaMi = true;

                dususBaslangicY =
                    sonYerdekiYukseklik;

                yuvarlanmisBaslangicY =
                    Mathf.RoundToInt(
                        dususBaslangicY
                    );

                havaAnimasyonuBasladiMi =
                    false;

                if (yuvarlanmisBaslangicY > 6)
                {
                    kilitliDususRotasi =
                        DususRotasi.Uzun;

                    secilenDususRotasi =
                        ziplamaIleHavayaCiktiMi
                        ? "ZIPLAMA SONRASI UZUN DÜŞÜŞ"
                        : "UZUN DÜŞÜŞ";
                }
                else if (
                    yuvarlanmisBaslangicY >= 2
                )
                {
                    kilitliDususRotasi =
                        DususRotasi.Kisa;

                    secilenDususRotasi =
                        ziplamaIleHavayaCiktiMi
                        ? "ZIPLAMA SONRASI KISA DÜŞÜŞ"
                        : "KISA DÜŞÜŞ";
                }
                else
                {
                    kilitliDususRotasi =
                        DususRotasi.Yok;

                    secilenDususRotasi =
                        "DÜŞÜŞ YOK";
                }
            }

            if (animator != null)
            {
                animator.SetBool(
                    "isWalking",
                    false
                );

                animator.SetBool(
                    "isRunning",
                    false
                );

                animator.SetBool(
                    "IsStairMoving",
                    false
                );

                animator.SetBool(
                    "IsStairDescending",
                    false
                );

                animator.SetBool(
                    "IsCrouchWalking",
                    false
                );

                animator.SetFloat(
                    "CrouchSpeedMultiplier",
                    1f
                );
            }

            // Havada ileri momentum ve A/D kontrolü
            if (ziplamaIleHavayaCiktiMi)
            {
                Vector3 havaYanalHareket =
                    cam.right *
                    yatay *
                    havadaSagaSolaHizi;

                havaYanalHareket.y = 0f;

                yatayHareket =
                    ziplamaYatayHizi +
                    havaYanalHareket;
            }

            gercekDususMesafesi =
                Mathf.Max(
                    0f,
                    dususBaslangicY -
                    transform.position.y
                );

            if (!havaAnimasyonuBasladiMi &&
                dikeyHiz.y < 0f &&
                gercekDususMesafesi >=
                MINIMUM_GERCEK_DUSUS &&
                animator != null)
            {
                if (kilitliDususRotasi ==
                    DususRotasi.Kisa)
                {
                    animator.CrossFadeInFixedTime(
                        kisaDususHavaState,
                        0.05f,
                        0
                    );

                    havaAnimasyonuBasladiMi =
                        true;
                }
                else if (
                    kilitliDususRotasi ==
                    DususRotasi.Uzun
                )
                {
                    animator.CrossFadeInFixedTime(
                        uzunDususHavaState,
                        0.05f,
                        0
                    );

                    havaAnimasyonuBasladiMi =
                        true;
                }
            }
        }

        // Yer çekimi
        dikeyHiz.y +=
            yercekimi * Time.deltaTime;

        Vector3 toplamHareket =
            yatayHareket + dikeyHiz;

        controller.Move(
            toplamHareket *
            Time.deltaTime
        );
    }

    private void SigaraStateIsimleriniBul()
    {
        sigaraYakmaState =
            IlkBulunanAnimatorState(
                sigaraYakmaState,
                new string[]
                {
                    "Base Layer.sigara yakma",
                    "Base Layer.Sigara yakma"
                }
            );

        sigaraIcmeState =
            IlkBulunanAnimatorState(
                sigaraIcmeState,
                new string[]
                {
                    "Base Layer.sigara içme",
                    "Base Layer.sigara İçme",
                    "Base Layer.sigara icme"
                }
            );

        sigaraSondurmeState =
            IlkBulunanAnimatorState(
                sigaraSondurmeState,
                new string[]
                {
                    "Base Layer.sigara söndürme",
                    "Base Layer.sigara Söndürme",
                    "Base Layer.sigara sondurme"
                }
            );
    }

    private int IlkBulunanAnimatorState(
        int varsayilanState,
        string[] olasiTamYollar)
    {
        foreach (string tamYol in olasiTamYollar)
        {
            int stateHash =
                Animator.StringToHash(tamYol);

            if (animator.HasState(0, stateHash))
            {
                return stateHash;
            }
        }

        return varsayilanState;
    }

    // VİRGÜL İLE SİGARA AKIŞINI BAŞLATIR, E İLE BİTİRİR
    private void SigaraGirdisiniKontrolEt()
    {
        float yaziHedefAlfasi =
            sondurmeYazisiniGoster ? 1f : 0f;

        sondurmeYazisiAlfasi =
            Mathf.MoveTowards(
                sondurmeYazisiAlfasi,
                yaziHedefAlfasi,
                Time.deltaTime * 4f
            );

        if (sigaraAkisiAktifMi)
        {
            if (!sondurmeYazisiniGoster &&
                !sigaraSondurmeIsteniyorMu &&
                Time.time - sigaraBaslangicZamani >=
                sondurmeYazisiGecikmesi)
            {
                sondurmeYazisiniGoster = true;
            }

            if (sondurmeYazisiniGoster &&
                Input.GetKeyDown(sigaraSondurmeTusu))
            {
                sigaraSondurmeIsteniyorMu = true;
                sondurmeYazisiniGoster = false;
            }

            return;
        }

        if (SigaraBaslatmaTusunaBasildiMi())
        {
            string baslatamamaNedeni =
                SigaraBaslatamamaNedeni();

            if (string.IsNullOrEmpty(
                    baslatamamaNedeni))
            {
                Debug.Log(
                    "< > | tuşu algılandı. " +
                    "Sigara yakma başlatılıyor."
                );

                StartCoroutine(SigaraAkisi());
            }
            else
            {
                Debug.LogWarning(
                    "Sigara başlatılamadı: " +
                    baslatamamaNedeni
                );
            }
        }
    }

    private bool SigaraBaslatmaTusunaBasildiMi()
    {
        /*
         * Türkçe klavyedeki aynı fiziksel tuş Unity ayarına
         * ve Shift/AltGr durumuna göre Less, Greater, Pipe veya
         * Backslash olarak gelebilir. Hepsini kabul ediyoruz.
         */
        if (Input.GetKeyDown(KeyCode.Less) ||
            Input.GetKeyDown(KeyCode.Greater) ||
            Input.GetKeyDown(KeyCode.Pipe) ||
            Input.GetKeyDown(KeyCode.Backslash))
        {
            return true;
        }

        string yazilanKarakterler =
            Input.inputString;

        return
            yazilanKarakterler.Contains("<") ||
            yazilanKarakterler.Contains(">") ||
            yazilanKarakterler.Contains("|") ||
            yazilanKarakterler.Contains("\\");
    }

    private void SigaraSonrasiAnimatoruTemizle()
    {
        if (sigaraSonrasiAnimatorTemizlemeKaresi <= 0 ||
            animator == null ||
            sigaraAkisiAktifMi)
        {
            return;
        }

        AnimatorStateInfo durum =
            animator.GetCurrentAnimatorStateInfo(0);

        bool sigaraStateindeMi =
            durum.fullPathHash == sigaraYakmaState ||
            durum.fullPathHash == sigaraIcmeState ||
            durum.fullPathHash == sigaraSondurmeState;

        if (sigaraStateindeMi ||
            animator.IsInTransition(0))
        {
            animator.Play(
                idleState,
                0,
                0f
            );

            animator.Update(0f);
        }

        sigaraSonrasiAnimatorTemizlemeKaresi--;
    }

    private string SigaraBaslatamamaNedeni()
    {
        if (animator == null)
        {
            return "Karakterde Animator bulunamadı.";
        }

        if (controller == null)
        {
            return "Karakterde CharacterController bulunamadı.";
        }

        if (!animator.HasState(0, sigaraYakmaState))
        {
            return
                "Base Layer'da 'sigara yakma' state'i bulunamadı.";
        }

        if (!animator.HasState(0, sigaraIcmeState))
        {
            return
                "Base Layer'da 'sigara içme' veya " +
                "'sigara İçme' state'i bulunamadı.";
        }

        if (!animator.HasState(0, sigaraSondurmeState))
        {
            return
                "Base Layer'da 'sigara söndürme' state'i bulunamadı.";
        }

        if (!controller.isGrounded || havadaMi)
        {
            return "Karakter yerde değil.";
        }

        if (egiliyorMu)
        {
            return "Karakter eğilmiş durumda.";
        }

        if (kalkiyorMu)
        {
            return "Kalkma animasyonu henüz bitmedi.";
        }

        if (AnimatorKalkmaAnimasyonuOynuyorMu())
        {
            return "Animator'daki kalkma animasyonu henüz bitmedi.";
        }

        if (duvaraTirmaniyorMu)
        {
            return "Duvar tırmanma devam ediyor.";
        }

        if (merdivenAlanindaMi)
        {
            return "Karakter merdiven rampasının üzerinde.";
        }

        if (kisaDususInisAnimasyonuOynuyorMu)
        {
            return "Kısa düşüş iniş animasyonu devam ediyor.";
        }

        if (uzunDususKalkmaAnimasyonuOynuyorMu)
        {
            return "Uzun düşüşten kalkma animasyonu devam ediyor.";
        }

        return "";
    }

    private IEnumerator SigaraAkisi()
    {
        sigaraAkisiAktifMi = true;
        sigaraSondurmeIsteniyorMu = false;
        sondurmeYazisiniGoster = false;
        sigaraBaslangicZamani = Time.time;

        dikeyHiz.y = -2f;
        ziplamaYatayHizi = Vector3.zero;

        TumSigaraModelleriniGizle();

        /*
         * Paket ilk kareden itibaren hazır olur. Yakma animasyonundaki
         * el cebinden çıktığında görünür durumda olduğu için çok kısa
         * animasyonlarda model gösterme anı kaçmaz.
         */
        ModelAktifliginiAyarla(
            sigaraPaketiModeli,
            true
        );

        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetBool("IsStairMoving", false);
        animator.SetBool("IsStairDescending", false);
        animator.SetBool("IsCrouchWalking", false);
        animator.SetBool("IsCrouching", false);

        // Sigara yakma animasyonu bir kez oynar.
        yield return SigaraAnimasyonunuBirKezOynat(
            sigaraYakmaState,
            false
        );

        /*
         * Loop Time kullanılmıyor. İçme animasyonu her seferinde
         * yumuşak geçişle kontrollü olarak yeniden başlatılır.
         * Oyuncu E'ye basana kadar içmeye devam eder.
         */
        while (!sigaraSondurmeIsteniyorMu)
        {
            yield return SigaraAnimasyonunuBirKezOynat(
                sigaraIcmeState,
                true
            );

            if (sigaraSondurmeIsteniyorMu)
            {
                break;
            }

            float beklemeSuresi =
                Random.Range(
                    Mathf.Min(
                        nefeslerArasiBekleme.x,
                        nefeslerArasiBekleme.y
                    ),
                    Mathf.Max(
                        nefeslerArasiBekleme.x,
                        nefeslerArasiBekleme.y
                    )
                );

            while (beklemeSuresi > 0f &&
                   !sigaraSondurmeIsteniyorMu)
            {
                beklemeSuresi -= Time.deltaTime;
                yield return null;
            }
        }

        sondurmeYazisiniGoster = false;

        if (aktifDumanAkisi != null)
        {
            StopCoroutine(aktifDumanAkisi);
            aktifDumanAkisi = null;
        }

        // E'ye basıldıktan sonra mevcut nefes biter ve söndürme oynar.
        yield return SigaraAnimasyonunuBirKezOynat(
            sigaraSondurmeState,
            false
        );

        TumSigaraModelleriniGizle();

        /*
         * Söndürme bittikten sonra sigara state'inde takılma
         * ihtimalini tamamen kaldırır. Animator ve hareket
         * değişkenleri kesin olarak normal duruma döner.
        */
        sigaraSondurmeIsteniyorMu = false;
        sondurmeYazisiniGoster = false;

        havadaMi = false;
        ziplamaIleHavayaCiktiMi = false;
        havaAnimasyonuBasladiMi = false;
        kalkiyorMu = false;

        kilitliDususRotasi =
            DususRotasi.Yok;

        dikeyHiz =
            new Vector3(0f, -2f, 0f);

        ziplamaYatayHizi =
            Vector3.zero;

        sonYerdekiYukseklik =
            transform.position.y;

        animator.speed = 1f;

        /*
         * Animator'ın söndürme klibinde tuttuğu bütün animasyon
         * bağlarını ve pozu temizler.
         */
        animator.Rebind();
        animator.Update(0f);

        animator.applyRootMotion = false;
        animator.SetBool("IsFalling", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetBool("IsStairMoving", false);
        animator.SetBool("IsStairDescending", false);
        animator.SetBool("IsCrouchWalking", false);
        animator.SetBool("IsCrouching", false);

        /*
         * CrossFade yerine Play kullanmak söndürme state'inin
         * son karesinde kalmasını kesin olarak engeller.
         */
        animator.Play(
            idleState,
            0,
            0f
        );

        animator.Update(0f);

        if (controller.enabled)
        {
            controller.Move(
                Vector3.down * 0.03f
            );
        }

        /*
         * Sonraki birkaç karede de sigara state'i kalırsa Update
         * tarafındaki güvenlik kontrolü tekrar Idle'a alır.
         */
        sigaraSonrasiAnimatorTemizlemeKaresi = 3;
        sigaraAkisiAktifMi = false;
    }

    private IEnumerator SigaraAnimasyonunuBirKezOynat(
        int stateHash,
        bool dumanCikar)
    {
        animator.CrossFadeInFixedTime(
            stateHash,
            sigaraAnimasyonGecisSuresi,
            0,
            0f
        );

        float girisGuvenlikSuresi = 1.25f;
        bool hedefDurumaGirdiMi = false;

        /*
         * Aynı içme state'i yeniden başlatılırken Animator önce
         * kendi içinde geçiş yapar. Geçiş tamamlanıp zaman
         * yeniden sıfıra yaklaşana kadar bekliyoruz.
         */
        while (girisGuvenlikSuresi > 0f)
        {
            AnimatorStateInfo durum =
                animator.GetCurrentAnimatorStateInfo(0);

            if (!animator.IsInTransition(0) &&
                durum.fullPathHash == stateHash &&
                durum.normalizedTime < 0.55f)
            {
                hedefDurumaGirdiMi = true;
                break;
            }

            girisGuvenlikSuresi -= Time.deltaTime;
            yield return null;
        }

        if (!hedefDurumaGirdiMi)
        {
            Debug.LogError(
                "Sigara animasyonuna girilemedi. " +
                "Animator state adlarını kontrol et."
            );

            sigaraSondurmeIsteniyorMu = true;
            yield break;
        }

        bool buNefesteDumanCiktiMi = false;
        bool yakmaModelleriTakipEdilecekMi =
            stateHash == sigaraYakmaState;
        bool paketGosterildiMi = false;
        bool sigaraGosterildiMi = false;
        bool paketGizlendiMi = false;
        bool cakmakGosterildiMi = false;
        bool cakmakGizlendiMi = false;

        while (true)
        {
            AnimatorStateInfo durum =
                animator.GetCurrentAnimatorStateInfo(0);

            bool halenHedefDurumdaMi =
                durum.fullPathHash == stateHash;

            if (halenHedefDurumdaMi &&
                yakmaModelleriTakipEdilecekMi)
            {
                SigaraYakmaModelleriniGuncelle(
                    durum.normalizedTime,
                    ref paketGosterildiMi,
                    ref sigaraGosterildiMi,
                    ref paketGizlendiMi,
                    ref cakmakGosterildiMi,
                    ref cakmakGizlendiMi
                );
            }

            if (halenHedefDurumdaMi &&
                dumanCikar &&
                !buNefesteDumanCiktiMi &&
                durum.normalizedTime >=
                dumanCikisAnimasyonOrani)
            {
                AgizdanDumanCikar();
                buNefesteDumanCiktiMi = true;
            }

            if (halenHedefDurumdaMi &&
                durum.normalizedTime >= 0.98f &&
                !animator.IsInTransition(0))
            {
                break;
            }

            if (!halenHedefDurumdaMi &&
                !animator.IsInTransition(0))
            {
                break;
            }

            yield return null;
        }

        if (yakmaModelleriTakipEdilecekMi)
        {
            // Yakma bittikten sonra elde yalnızca sigara kalır.
            ModelAktifliginiAyarla(sigaraModeli, true);
            ModelAktifliginiAyarla(cakmakModeli, false);
            ModelAktifliginiAyarla(sigaraPaketiModeli, false);
        }
    }

    private void SigaraYakmaModelleriniGuncelle(
        float animasyonOrani,
        ref bool paketGosterildiMi,
        ref bool sigaraGosterildiMi,
        ref bool paketGizlendiMi,
        ref bool cakmakGosterildiMi,
        ref bool cakmakGizlendiMi)
    {
        if (!paketGosterildiMi &&
            animasyonOrani >= paketCikarmaAni)
        {
            ModelAktifliginiAyarla(
                sigaraPaketiModeli,
                true
            );
            paketGosterildiMi = true;
        }

        if (!sigaraGosterildiMi &&
            animasyonOrani >= sigaraPakettenAlmaAni)
        {
            ModelAktifliginiAyarla(
                sigaraModeli,
                true
            );
            sigaraGosterildiMi = true;
        }

        if (!paketGizlendiMi &&
            animasyonOrani >= paketiCebeKoymaAni)
        {
            ModelAktifliginiAyarla(
                sigaraPaketiModeli,
                false
            );
            paketGizlendiMi = true;
        }

        if (!cakmakGosterildiMi &&
            animasyonOrani >= cakmakCikarmaAni)
        {
            ModelAktifliginiAyarla(
                cakmakModeli,
                true
            );
            cakmakGosterildiMi = true;
        }

        if (!cakmakGizlendiMi &&
            animasyonOrani >= cakmakGizlemeAni)
        {
            ModelAktifliginiAyarla(
                cakmakModeli,
                false
            );
            cakmakGizlendiMi = true;
        }
    }

    private void TumSigaraModelleriniGizle()
    {
        ModelAktifliginiAyarla(sigaraModeli, false);
        ModelAktifliginiAyarla(cakmakModeli, false);
        ModelAktifliginiAyarla(sigaraPaketiModeli, false);
    }

    private void SigaraModelleriniHazirla()
    {
        sigaraModeli = SigaraModeliniSahneyeHazirla(
            sigaraModeli,
            sigaraBaglamaNoktasi,
            HumanBodyBones.RightIndexIntermediate,
            HumanBodyBones.RightHand,
            "Sigara Modeli (Calisma Aninda)",
            sigaraHedefUzunlugu,
            sigaraYerelKonumDuzeltmesi,
            sigaraYerelDonusDuzeltmesi
        );

        cakmakModeli = SigaraModeliniSahneyeHazirla(
            cakmakModeli,
            cakmakBaglamaNoktasi,
            HumanBodyBones.LeftMiddleProximal,
            HumanBodyBones.LeftHand,
            "Cakmak Modeli (Calisma Aninda)",
            cakmakHedefUzunlugu,
            cakmakYerelKonumDuzeltmesi,
            cakmakYerelDonusDuzeltmesi
        );

        sigaraPaketiModeli = SigaraModeliniSahneyeHazirla(
            sigaraPaketiModeli,
            sigaraPaketiBaglamaNoktasi,
            HumanBodyBones.LeftMiddleProximal,
            HumanBodyBones.LeftHand,
            "Sigara Paketi Modeli (Calisma Aninda)",
            paketHedefUzunlugu,
            paketYerelKonumDuzeltmesi,
            paketYerelDonusDuzeltmesi
        );
    }

    private GameObject SigaraModeliniSahneyeHazirla(
        GameObject modelKaynagi,
        Transform ozelBaglamaNoktasi,
        HumanBodyBones oncelikliKemik,
        HumanBodyBones yedekKemik,
        string ornekAdi,
        float hedefUzunluk,
        Vector3 yerelKonumDuzeltmesi,
        Vector3 yerelDonusDuzeltmesi)
    {
        if (modelKaynagi == null)
        {
            return null;
        }

        bool zatenSahnedeMi =
            modelKaynagi.scene.IsValid();

        GameObject modelOrnegi = modelKaynagi;

        if (!zatenSahnedeMi)
        {
            // Project panelinden verilen FBX veya prefabı sahneye üretir.
            modelOrnegi = Instantiate(modelKaynagi);
            modelOrnegi.name = ornekAdi;
        }

        // Ölçüm yapılabilmesi için model geçici olarak aktif olmalıdır.
        modelOrnegi.SetActive(true);

        Renderer[] modelRendererlari =
            modelOrnegi.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer modelRendereri in modelRendererlari)
        {
            modelRendereri.enabled = true;
        }

        /*
         * Hierarchy'de karakterin altında elle yerleştirilmiş bir model
         * varsa mevcut konumunu bozmayız. Prefab ise veya model karakterin
         * dışında duruyorsa seçilen/otomatik bulunan ele bağlarız.
         */
        bool karakterinAltindaMi =
            modelOrnegi.transform.IsChildOf(transform);

        if (!zatenSahnedeMi || !karakterinAltindaMi)
        {
            Transform hedef = ozelBaglamaNoktasi;

            if (hedef == null && animator != null)
            {
                if (animator.isHuman)
                {
                    hedef = animator.GetBoneTransform(
                        oncelikliKemik
                    );
                }

                /*
                 * Avatar parmak eşlemesi eksik olsa bile Mixamo'nun
                 * RightHandIndex2 / LeftHandMiddle1 isimlerini arar.
                 */
                if (hedef == null)
                {
                    hedef = ModelKemiginiIsimleBul(
                        oncelikliKemik
                    );
                }

                /*
                 * Bazı Mixamo Avatar ayarlarında parmak kemikleri
                 * eşleştirilmemiş olabilir. O durumda el/bilek kemiği
                 * yalnızca yedek bağlantı olarak kullanılır.
                 */
                if (hedef == null)
                {
                    if (animator.isHuman)
                    {
                        hedef = animator.GetBoneTransform(
                            yedekKemik
                        );
                    }

                    if (hedef == null)
                    {
                        hedef = ModelKemiginiIsimleBul(
                            yedekKemik
                        );
                    }
                }
            }

            if (hedef == null)
            {
                hedef = transform;

                Debug.LogWarning(
                    ornekAdi +
                    " için el kemiği bulunamadı. " +
                    "Inspector'dan bir bağlama noktası ver."
                );
            }

            modelOrnegi.transform.SetParent(
                hedef,
                false
            );

            modelOrnegi.transform.localPosition =
                yerelKonumDuzeltmesi;
            modelOrnegi.transform.localRotation =
                Quaternion.Euler(
                    yerelDonusDuzeltmesi
                );
        }

        if (!zatenSahnedeMi &&
            modelleriOtomatikBoyutlandir)
        {
            ModeliHedefBoyutaGetir(
                modelOrnegi,
                modelRendererlari,
                hedefUzunluk,
                ornekAdi
            );
        }

        return modelOrnegi;
    }

    private Transform ModelKemiginiIsimleBul(
        HumanBodyBones arananKemik)
    {
        string[] olasiSonEkler;

        switch (arananKemik)
        {
            case HumanBodyBones.RightIndexIntermediate:
                olasiSonEkler = new string[]
                {
                    "RightHandIndex2",
                    "RightIndexIntermediate"
                };
                break;

            case HumanBodyBones.LeftMiddleProximal:
                olasiSonEkler = new string[]
                {
                    "LeftHandMiddle1",
                    "LeftMiddleProximal"
                };
                break;

            case HumanBodyBones.RightHand:
                olasiSonEkler = new string[]
                {
                    "RightHand"
                };
                break;

            case HumanBodyBones.LeftHand:
                olasiSonEkler = new string[]
                {
                    "LeftHand"
                };
                break;

            default:
                olasiSonEkler = new string[]
                {
                    arananKemik.ToString()
                };
                break;
        }

        Transform[] butunKemikler =
            GetComponentsInChildren<Transform>(true);

        foreach (Transform kemik in butunKemikler)
        {
            foreach (string sonEk in olasiSonEkler)
            {
                if (kemik.name.EndsWith(
                    sonEk,
                    System.StringComparison.OrdinalIgnoreCase
                ))
                {
                    return kemik;
                }
            }
        }

        return null;
    }

    private void ModeliHedefBoyutaGetir(
        GameObject model,
        Renderer[] modelRendererlari,
        float hedefUzunluk,
        string modelAdi)
    {
        if (modelRendererlari == null ||
            modelRendererlari.Length == 0)
        {
            Debug.LogError(
                modelAdi +
                " içinde Mesh Renderer bulunamadı. " +
                "Doğru FBX/prefab dosyasını verdiğinden emin ol."
            );
            return;
        }

        Bounds toplamSinir =
            modelRendererlari[0].bounds;

        for (int i = 1; i < modelRendererlari.Length; i++)
        {
            toplamSinir.Encapsulate(
                modelRendererlari[i].bounds
            );
        }

        float mevcutEnUzunKenar = Mathf.Max(
            toplamSinir.size.x,
            toplamSinir.size.y,
            toplamSinir.size.z
        );

        if (mevcutEnUzunKenar <= 0.00001f)
        {
            Debug.LogError(
                modelAdi +
                " boyutu ölçülemedi. FBX Mesh ayarlarını kontrol et."
            );
            return;
        }

        float boyutCarpani =
            hedefUzunluk / mevcutEnUzunKenar;

        model.transform.localScale *=
            boyutCarpani;
    }

    private void ModelAktifliginiAyarla(
        GameObject model,
        bool aktif)
    {
        if (model != null && model.activeSelf != aktif)
        {
            model.SetActive(aktif);
        }

        if (model != null && aktif)
        {
            Renderer[] modelRendererlari =
                model.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer modelRendereri in modelRendererlari)
            {
                modelRendereri.enabled = true;
            }
        }
    }

    // AĞIZ NOKTASINI VE KODLA ÜRETİLEN DUMAN PARÇACIKLARINI HAZIRLAR
    private void AgizDumanSisteminiHazirla()
    {
        if (agizDumanNoktasi == null)
        {
            Transform kafa =
                KafaKemiginiOtomatikBul();

            if (kafa != null)
            {
                GameObject otomatikAgizNoktasi =
                    new GameObject(
                        "Agiz Duman Noktasi"
                    );

                otomatikAgizNoktasi.transform.position =
                    kafa.position +
                    transform.forward * 0.10f +
                    Vector3.down * 0.055f;

                otomatikAgizNoktasi.transform.rotation =
                    transform.rotation;

                otomatikAgizNoktasi.transform.SetParent(
                    kafa,
                    true
                );

                agizDumanNoktasi =
                    otomatikAgizNoktasi.transform;
            }
        }

        if (agizDumanNoktasi == null)
        {
            Debug.LogWarning(
                "Ağız duman noktası oluşturulamadı. " +
                "Inspector'daki Agiz Duman Noktasi alanına " +
                "ağızda duran bir Transform ver."
            );

            return;
        }

        GameObject dumanObjesi =
            new GameObject("Agiz Dumani");

        dumanObjesi.transform.SetParent(
            agizDumanNoktasi,
            false
        );

        dumanObjesi.transform.localPosition =
            Vector3.zero;

        dumanObjesi.transform.localRotation =
            Quaternion.identity;

        agizDumanParticleSystem =
            dumanObjesi.AddComponent<ParticleSystem>();

        agizDumanParticleSystem.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        ParticleSystem.MainModule anaAyarlar =
            agizDumanParticleSystem.main;

        anaAyarlar.loop = false;
        anaAyarlar.playOnAwake = false;
        anaAyarlar.duration = 1f;
        anaAyarlar.startLifetime = 1.8f;
        anaAyarlar.startSpeed = 0f;
        anaAyarlar.startSize = 0.07f;
        anaAyarlar.startColor =
            new Color(0.82f, 0.84f, 0.86f, 0.55f);
        anaAyarlar.simulationSpace =
            ParticleSystemSimulationSpace.World;
        anaAyarlar.maxParticles = 300;

        ParticleSystem.EmissionModule yayilma =
            agizDumanParticleSystem.emission;
        yayilma.enabled = false;

        ParticleSystem.ShapeModule sekil =
            agizDumanParticleSystem.shape;
        sekil.enabled = false;

        ParticleSystem.NoiseModule dalgalanma =
            agizDumanParticleSystem.noise;
        dalgalanma.enabled = true;
        dalgalanma.strength = 0.08f;
        dalgalanma.frequency = 0.45f;
        dalgalanma.scrollSpeed = 0.25f;
        dalgalanma.damping = true;

        Gradient dumanRengi = new Gradient();
        dumanRengi.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(
                    new Color(0.92f, 0.93f, 0.94f),
                    0f
                ),
                new GradientColorKey(
                    new Color(0.60f, 0.63f, 0.66f),
                    1f
                )
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.68f, 0.12f),
                new GradientAlphaKey(0.30f, 0.60f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        ParticleSystem.ColorOverLifetimeModule renkGecisi =
            agizDumanParticleSystem.colorOverLifetime;
        renkGecisi.enabled = true;
        renkGecisi.color =
            new ParticleSystem.MinMaxGradient(
                dumanRengi
            );

        AnimationCurve buyumeEgrisi =
            new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f, 1.8f)
            );

        ParticleSystem.SizeOverLifetimeModule buyume =
            agizDumanParticleSystem.sizeOverLifetime;
        buyume.enabled = true;
        buyume.size =
            new ParticleSystem.MinMaxCurve(
                1f,
                buyumeEgrisi
            );

        ParticleSystemRenderer dumanCizici =
            dumanObjesi.GetComponent<
                ParticleSystemRenderer
            >();

        dumanCizici.renderMode =
            ParticleSystemRenderMode.Billboard;

        dumanCizici.shadowCastingMode =
            ShadowCastingMode.Off;

        dumanCizici.receiveShadows = false;
        dumanCizici.lightProbeUsage =
            LightProbeUsage.Off;

        DumanMateryaliniHazirla(
            dumanCizici
        );

        agizDumanParticleSystem.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    // URP/Built-in UYUMLU ŞEFFAF DUMAN MATERYALİ OLUŞTURUR
    private void DumanMateryaliniHazirla(
        ParticleSystemRenderer dumanCizici)
    {
        Shader dumanShaderi =
            Shader.Find(
                "Universal Render Pipeline/Particles/Unlit"
            );

        if (dumanShaderi == null)
        {
            dumanShaderi =
                Shader.Find(
                    "Particles/Standard Unlit"
                );
        }

        if (dumanShaderi == null)
        {
            dumanShaderi =
                Shader.Find("Sprites/Default");
        }

        if (dumanShaderi == null)
        {
            Debug.LogError(
                "Duman shader'i bulunamadı. " +
                "Duman materyali oluşturulamadı."
            );

            return;
        }

        dumanMateryali =
            new Material(dumanShaderi);

        dumanMateryali.name =
            "Runtime Sigara Duman Materyali";

        DumanDokusunuOlustur();

        if (dumanMateryali.HasProperty("_Surface"))
        {
            dumanMateryali.SetFloat(
                "_Surface",
                1f
            );
        }

        if (dumanMateryali.HasProperty("_Blend"))
        {
            dumanMateryali.SetFloat(
                "_Blend",
                0f
            );
        }

        if (dumanMateryali.HasProperty("_SrcBlend"))
        {
            dumanMateryali.SetInt(
                "_SrcBlend",
                (int)BlendMode.SrcAlpha
            );
        }

        if (dumanMateryali.HasProperty("_DstBlend"))
        {
            dumanMateryali.SetInt(
                "_DstBlend",
                (int)BlendMode.OneMinusSrcAlpha
            );
        }

        if (dumanMateryali.HasProperty("_ZWrite"))
        {
            dumanMateryali.SetInt(
                "_ZWrite",
                0
            );
        }

        if (dumanMateryali.HasProperty("_BaseColor"))
        {
            dumanMateryali.SetColor(
                "_BaseColor",
                Color.white
            );
        }

        if (dumanMateryali.HasProperty("_Color"))
        {
            dumanMateryali.SetColor(
                "_Color",
                Color.white
            );
        }

        if (dumanDokusu != null)
        {
            if (dumanMateryali.HasProperty("_BaseMap"))
            {
                dumanMateryali.SetTexture(
                    "_BaseMap",
                    dumanDokusu
                );
            }

            if (dumanMateryali.HasProperty("_MainTex"))
            {
                dumanMateryali.SetTexture(
                    "_MainTex",
                    dumanDokusu
                );
            }
        }

        dumanMateryali.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );

        dumanMateryali.DisableKeyword(
            "_ALPHATEST_ON"
        );

        dumanMateryali.DisableKeyword(
            "_ALPHAPREMULTIPLY_ON"
        );

        dumanMateryali.SetOverrideTag(
            "RenderType",
            "Transparent"
        );

        dumanMateryali.SetShaderPassEnabled(
            "ShadowCaster",
            false
        );

        dumanMateryali.renderQueue =
            (int)RenderQueue.Transparent;

        dumanCizici.material =
            dumanMateryali;
    }

    private void DumanDokusunuOlustur()
    {
        const int dokuBoyutu = 64;

        dumanDokusu =
            new Texture2D(
                dokuBoyutu,
                dokuBoyutu,
                TextureFormat.RGBA32,
                false
            );

        dumanDokusu.name =
            "Runtime Yumushak Duman Dokusu";

        dumanDokusu.wrapMode =
            TextureWrapMode.Clamp;

        dumanDokusu.filterMode =
            FilterMode.Bilinear;

        for (int y = 0;
             y < dokuBoyutu;
             y++)
        {
            for (int x = 0;
                 x < dokuBoyutu;
                 x++)
            {
                float normalX =
                    (x + 0.5f) /
                    dokuBoyutu * 2f - 1f;

                float normalY =
                    (y + 0.5f) /
                    dokuBoyutu * 2f - 1f;

                float merkezdenUzaklik =
                    Mathf.Sqrt(
                        normalX * normalX +
                        normalY * normalY
                    );

                float yumusaklik =
                    Mathf.Clamp01(
                        1f - merkezdenUzaklik
                    );

                yumusaklik =
                    yumusaklik *
                    yumusaklik *
                    (3f - 2f * yumusaklik);

                float dalga =
                    Mathf.Lerp(
                        0.82f,
                        1f,
                        Mathf.PerlinNoise(
                            x * 0.11f,
                            y * 0.11f
                        )
                    );

                float alfa =
                    yumusaklik * dalga;

                dumanDokusu.SetPixel(
                    x,
                    y,
                    new Color(1f, 1f, 1f, alfa)
                );
            }
        }

        dumanDokusu.Apply();
    }

    private Transform KafaKemiginiOtomatikBul()
    {
        /*
         * Avatar Humanoid ise Hierarchy kapalı olsa bile Unity kafa
         * kemiğini doğrudan verir.
         */
        if (animator != null && animator.isHuman)
        {
            Transform humanoidKafa =
                animator.GetBoneTransform(
                    HumanBodyBones.Head
                );

            if (humanoidKafa != null)
            {
                return humanoidKafa;
            }
        }

        /*
         * Avatar eşleştirmesi yoksa mixamorig9:Hips dalı dahil
         * karakterin bütün kapalı çocuklarını isimle tarar.
         */
        return IsmindeKafaGecenTransformuBul(
            transform
        );
    }

    private Transform IsmindeKafaGecenTransformuBul(
        Transform kok)
    {
        string kucukIsim =
            kok.name.ToLowerInvariant();

        if (kucukIsim.Contains("head") ||
            kucukIsim.Contains("kafa"))
        {
            return kok;
        }

        for (int i = 0;
             i < kok.childCount;
             i++)
        {
            Transform bulunan =
                IsmindeKafaGecenTransformuBul(
                    kok.GetChild(i)
                );

            if (bulunan != null)
            {
                return bulunan;
            }
        }

        return null;
    }

    private void AgizdanDumanCikar()
    {
        if (agizDumanParticleSystem == null ||
            agizDumanNoktasi == null)
        {
            return;
        }

        /*
         * Eski sistem bütün parçacıkları tek karede çıkarıp
         * baloncuk gibi gösteriyordu. Artık aynı miktar duman
         * belirlenen süre boyunca ağızdan akarak çıkar.
         */
        if (aktifDumanAkisi != null)
        {
            StopCoroutine(aktifDumanAkisi);
        }

        aktifDumanAkisi =
            StartCoroutine(UzunAgizDumaniCikar());
    }

    private IEnumerator UzunAgizDumaniCikar()
    {
        float akisSuresi =
            Mathf.Max(0.5f, dumanAkisSuresi);

        int toplamParcacikSayisi =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    birNefesteDumanParcacigi *
                    dumanYogunlukCarpani
                ),
                12,
                120
            );

        int cikanParcacikSayisi = 0;
        float gecenSure = 0f;

        while (gecenSure < akisSuresi)
        {
            gecenSure += Time.deltaTime;

            float akisOrani =
                Mathf.Clamp01(
                    gecenSure / akisSuresi
                );

            int buAnaKadarCikmasiGereken =
                Mathf.FloorToInt(
                    toplamParcacikSayisi *
                    akisOrani
                );

            while (cikanParcacikSayisi <
                   buAnaKadarCikmasiGereken)
            {
                TekDumanParcacigiCikar();
                cikanParcacikSayisi++;
            }

            yield return null;
        }

        while (cikanParcacikSayisi <
               toplamParcacikSayisi)
        {
            TekDumanParcacigiCikar();
            cikanParcacikSayisi++;
        }

        aktifDumanAkisi = null;
    }

    private void TekDumanParcacigiCikar()
    {
        if (agizDumanParticleSystem == null ||
            agizDumanNoktasi == null)
        {
            return;
        }

        ParticleSystem.EmitParams parcacik =
            new ParticleSystem.EmitParams();

        parcacik.position =
            agizDumanNoktasi.position +
            Random.insideUnitSphere * 0.010f;

        /*
         * Duman ağızdan ileri doğru uzar, sonra yavaşça yukarı
         * kıvrılır. Parçacıkların her biri hafif farklı gider.
         */
        parcacik.velocity =
            transform.forward *
            Random.Range(0.10f, 0.20f) +
            Vector3.up *
            Random.Range(0.055f, 0.13f) +
            transform.right *
            Random.Range(-0.025f, 0.025f);

        parcacik.startLifetime =
            Random.Range(1.80f, 2.80f);

        parcacik.startSize =
            Random.Range(0.035f, 0.075f);

        parcacik.startColor =
            new Color(
                0.86f,
                0.88f,
                0.90f,
                Random.Range(0.42f, 0.62f)
            );

        agizDumanParticleSystem.Emit(
            parcacik,
            1
        );
    }

    private void OnDestroy()
    {
        if (dumanMateryali != null)
        {
            Destroy(dumanMateryali);
        }

        if (dumanDokusu != null)
        {
            Destroy(dumanDokusu);
        }
    }

    // SOL ÜSTTEKİ SÖNDÜRME BİLDİRİMİ
    private void OnGUI()
    {
        if (sondurmeYazisiAlfasi <= 0.01f)
        {
            return;
        }

        float arayuzOlcegi =
            Mathf.Clamp(
                Screen.height / 1080f,
                0.80f,
                1.35f
            );

        Rect panel = new Rect(
            20f * arayuzOlcegi,
            20f * arayuzOlcegi,
            390f * arayuzOlcegi,
            58f * arayuzOlcegi
        );

        Color eskiRenk = GUI.color;

        GUI.color =
            new Color(
                0.02f,
                0.02f,
                0.02f,
                0.76f * sondurmeYazisiAlfasi
            );

        GUI.Box(panel, GUIContent.none);

        GUIStyle yaziStili =
            new GUIStyle(GUI.skin.label);
        yaziStili.fontSize =
            Mathf.RoundToInt(19f * arayuzOlcegi);
        yaziStili.alignment =
            TextAnchor.MiddleLeft;
        yaziStili.normal.textColor = Color.white;

        GUI.color =
            new Color(1f, 1f, 1f, sondurmeYazisiAlfasi);

        GUI.Label(
            new Rect(
                panel.x + 16f * arayuzOlcegi,
                panel.y,
                145f * arayuzOlcegi,
                panel.height
            ),
            "Söndürmek için",
            yaziStili
        );

        GUIStyle tusStili =
            new GUIStyle(GUI.skin.box);
        tusStili.fontSize =
            Mathf.RoundToInt(21f * arayuzOlcegi);
        tusStili.fontStyle = FontStyle.Bold;
        tusStili.alignment = TextAnchor.MiddleCenter;
        tusStili.normal.textColor = Color.black;

        GUI.color =
            new Color(
                1f,
                0.78f,
                0.12f,
                sondurmeYazisiAlfasi
            );

        GUI.Box(
            new Rect(
                panel.x + 158f * arayuzOlcegi,
                panel.y + 10f * arayuzOlcegi,
                40f * arayuzOlcegi,
                38f * arayuzOlcegi
            ),
            "E",
            tusStili
        );

        GUI.color =
            new Color(1f, 1f, 1f, sondurmeYazisiAlfasi);

        GUI.Label(
            new Rect(
                panel.x + 210f * arayuzOlcegi,
                panel.y,
                165f * arayuzOlcegi,
                panel.height
            ),
            "tuşuna basın",
            yaziStili
        );

        GUI.color = eskiRenk;
    }

    void LateUpdate()
    {
        if (!duvaraTirmaniyorMu ||
            !duvarOnundeKilitlemeAktif)
        {
            return;
        }

        /*
         * Animator Root Motion veya MatchTarget karakteri duvarın
         * içine çekse bile, tutunma aşamasında gövdeyi duvar
         * düzleminin dışında tutar.
         */
        float duvaraOlanMesafe =
            Vector3.Dot(
                transform.position -
                aktifDuvarNoktasi,
                aktifDuvarDisNormali
            );

        if (duvaraOlanMesafe <
            duvardanMinimumGövdeMesafesi)
        {
            float disariItmeMiktari =
                duvardanMinimumGövdeMesafesi -
                duvaraOlanMesafe;

            transform.position +=
                aktifDuvarDisNormali *
                disariItmeMiktari;
        }
    }

    // AYAĞIN ALTINDA MERDİVEN LAYER’I VAR MI?
    private void MerdivenZeminKontrolu()
    {
        /*
         * Unity'deki "Merdiven" Layer'ını
         * ismiyle otomatik bulur.
         */
        int merdivenLayerNumarasi =
            LayerMask.NameToLayer("Merdiven");

        if (merdivenLayerNumarasi == -1)
        {
            Debug.LogError(
                "'Merdiven' isimli Layer bulunamadı!"
            );

            merdivenAlanindaMi = false;
            merdivenYukariYonu = Vector3.zero;
            return;
        }

        int merdivenMaskesi =
            1 << merdivenLayerNumarasi;

        Vector3 kontrolBaslangici =
            transform.position +
            Vector3.up * 0.6f;

        float kontrolYaricapi =
            controller.radius * 0.75f;

        /*
         * Karakterin ayağının altına küre gönderir.
         * Rampanın Layer'ı Merdiven ise algılar.
         */
        merdivenAlanindaMi =
            Physics.SphereCast(
                kontrolBaslangici,
                kontrolYaricapi,
                Vector3.down,
                out RaycastHit merdivenVurusu,
                merdivenKontrolMesafesi + 0.6f,
                merdivenMaskesi,
                QueryTriggerInteraction.Ignore
            );

        if (merdivenAlanindaMi)
        {
            /*
             * Yüzey normalinden rampanın en dik yukarı giden
             * yönünü bulur. Rampa hangi yöne dönük olursa
             * olsun çıkma ve inme doğru algılanır.
             */
            merdivenYukariYonu =
                Vector3.ProjectOnPlane(
                    Vector3.up,
                    merdivenVurusu.normal
                );

            if (merdivenYukariYonu.sqrMagnitude > 0.001f)
            {
                merdivenYukariYonu.Normalize();
            }
            else
            {
                merdivenYukariYonu = Vector3.zero;
            }
        }
        else
        {
            merdivenYukariYonu = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetBool(
                "IsOnStairs",
                merdivenAlanindaMi
            );

            if (!merdivenAlanindaMi)
            {
                animator.SetBool(
                    "IsStairMoving",
                    false
                );
            }
        }
    }
    // KOŞARAK DUVARA TIRMANMA KONTROLÜ
    private bool DuvarTirmanmaKontrolu(
        Vector3 hareketYonu)
    {
        if (tirmanilabilirDuvarMaskesi == 0)
        {
            if (duvarTirmanmaDebug)
            {
                Debug.LogWarning(
                    "Tırmanma başlamadı: " +
                    "TirmanilabilirDuvar Layer'ı bulunamadı."
                );
            }

            return false;
        }

        hareketYonu.y = 0f;

        if (hareketYonu.sqrMagnitude < 0.01f)
        {
            return false;
        }

        hareketYonu.Normalize();

        Vector3 rayBaslangici =
            transform.position +
            Vector3.up *
            duvarKontrolYuksekligi;

        if (duvarTirmanmaDebug)
        {
            Debug.DrawRay(
                rayBaslangici,
                hareketYonu * duvarKontrolMesafesi,
                Color.yellow,
                2f
            );
        }

        RaycastHit duvarVurusu;

        /*
         * Tek bir ince ışın yerine küçük bir küre gönderiyoruz.
         * Böylece karakter duvara tam milimetrik bakmasa da
         * duvarı algılayabiliyor.
         */
        if (!Physics.SphereCast(
            rayBaslangici,
            duvarKontrolYaricapi,
            hareketYonu,
            out duvarVurusu,
            duvarKontrolMesafesi,
            tirmanilabilirDuvarMaskesi,
            QueryTriggerInteraction.Ignore))
        {
            if (duvarTirmanmaDebug)
            {
                Debug.Log(
                    "Tırmanma başlamadı: Önünde " +
                    "TirmanilabilirDuvar Layer'ında bir duvar yok."
                );
            }

            return false;
        }

        Vector3 duvarIciYonu =
            -duvarVurusu.normal;

        duvarIciYonu.y = 0f;

        if (duvarIciYonu.sqrMagnitude < 0.01f)
        {
            return false;
        }

        duvarIciYonu.Normalize();

        RaycastHit duvarUstuVurusu =
            new RaycastHit();

        bool duvarUstuBulunduMu = false;

        /*
         * Duvar çok ince veya kalın olabilir. Bu yüzden yalnızca
         * tek noktadan değil, ön yüzden içeri doğru birkaç noktadan
         * aşağı ışın gönderiyoruz.
         */
        float enBuyukAramaPayi =
            Mathf.Max(
                duvarUstuAramaPayi,
                duvarUstundeIlerleme
            );

        const int aramaAdimiSayisi = 7;

        for (int adim = 0;
             adim < aramaAdimiSayisi;
             adim++)
        {
            float aramaOrani =
                adim /
                (float)(aramaAdimiSayisi - 1);

            float iceriPay =
                Mathf.Lerp(
                    0.03f,
                    enBuyukAramaPayi,
                    aramaOrani
                );

            Vector3 ustRayBaslangici =
                duvarVurusu.point +
                duvarIciYonu * iceriPay;

            ustRayBaslangici.y =
                transform.position.y +
                maksimumDuvarYuksekligi +
                0.5f;

            if (Physics.Raycast(
                    ustRayBaslangici,
                    Vector3.down,
                    out RaycastHit bulunanUstYuzey,
                    maksimumDuvarYuksekligi + 1f,
                    tirmanilabilirDuvarMaskesi,
                    QueryTriggerInteraction.Ignore) &&
                bulunanUstYuzey.normal.y >= 0.65f)
            {
                duvarUstuVurusu =
                    bulunanUstYuzey;

                duvarUstuBulunduMu =
                    true;

                break;
            }
        }

        if (!duvarUstuBulunduMu)
        {
            if (duvarTirmanmaDebug)
            {
                Debug.Log(
                    "Tırmanma başlamadı: Duvarın üst yüzeyi bulunamadı. " +
                    "Duvarın Box Collider'ı ve Layer'ı kontrol edilmeli."
                );
            }

            return false;
        }

        /*
         * Karakter evin içindeyse, doğrudan üstünde tavan/çatı
         * bulunur. Böyle bir durumda duvar tırmanmayı başlatma.
         */
        if (BasininUstundeTavanVarMi(
                duvarUstuVurusu.point.y))
        {
            return false;
        }

        float duvarYuksekligi =
            duvarUstuVurusu.point.y -
            transform.position.y;

        if (duvarYuksekligi <
            minimumDuvarYuksekligi ||
            duvarYuksekligi >
            maksimumDuvarYuksekligi)
        {
            if (duvarTirmanmaDebug)
            {
                Debug.Log(
                    "Tırmanma başlamadı: Duvar yüksekliği " +
                    duvarYuksekligi.ToString("F2") +
                    " metre. İzin verilen aralık: " +
                    minimumDuvarYuksekligi.ToString("F2") +
                    " - " +
                    maksimumDuvarYuksekligi.ToString("F2") +
                    " metre."
                );
            }

            return false;
        }

        Vector3 hedefPozisyon =
            duvarUstuVurusu.point +
            duvarIciYonu *
            duvarUstundeIlerleme;

        hedefPozisyon.y += 0.05f;

        /*
         * Duvarın üstünde karakterin sığacağı boşluk var mı?
         * Tavan veya başka bir nesne varsa tırmanmayı başlatma.
         */
        Vector3 kapsulAltNoktasi =
            hedefPozisyon +
            controller.center +
            Vector3.up *
            (-controller.height * 0.5f +
             controller.radius +
             controller.skinWidth);

        Vector3 kapsulUstNoktasi =
            hedefPozisyon +
            controller.center +
            Vector3.up *
            (controller.height * 0.5f -
             controller.radius);

        Collider[] ustTaraftakiColliderlar =
            Physics.OverlapCapsule(
                kapsulAltNoktasi,
                kapsulUstNoktasi,
                controller.radius * 0.9f,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        bool ustTarafDoluMu = false;
        string engelOlanNesne = "";

        foreach (Collider bulunanCollider in
                 ustTaraftakiColliderlar)
        {
            if (bulunanCollider == null)
                continue;

            /*
             * Karakterin kendi Character Controller veya Mesh
             * Collider'ı boşluk engeli değildir.
             */
            bool karakterinKendisiMi =
                bulunanCollider.transform == transform ||
                bulunanCollider.transform.IsChildOf(transform);

            if (karakterinKendisiMi)
                continue;

            /*
             * Karakterin üzerine çıkacağı duvar/çatı collider'ı da
             * kapsülün altına değebilir. Onu engel saymıyoruz.
             */
            int bulunanLayerMaskesi =
                1 << bulunanCollider.gameObject.layer;

            bool tirmanilanYuzeyMi =
                (bulunanLayerMaskesi &
                 tirmanilabilirDuvarMaskesi) != 0;

            if (tirmanilanYuzeyMi)
                continue;

            ustTarafDoluMu = true;
            engelOlanNesne =
                bulunanCollider.gameObject.name;
            break;
        }

        if (ustTarafDoluMu)
        {
            if (duvarTirmanmaDebug)
            {
                Debug.Log(
                    "Tırmanma başlamadı: Duvarın üstünde " +
                    "karakterin sığacağı boş alan yok. Engel: " +
                    engelOlanNesne
                );
            }

            return false;
        }

        if (duvarTirmanmaDebug)
        {
            Debug.Log(
                "DUVAR TIRMANMA BAŞLADI. Duvar yüksekliği: " +
                duvarYuksekligi.ToString("F2") +
                " metre."
            );
        }

        StartCoroutine(
            DuvaraTirman(
                duvarVurusu,
                duvarUstuVurusu,
                hedefPozisyon
            )
        );

        return true;
    }

    // KARAKTER EVİN İÇİNDE Mİ? ÜSTÜNDE TAVAN VAR MI?
    private bool BasininUstundeTavanVarMi(
        float bulunanDuvarUstuY)
    {
        float karakterBasY =
            transform.position.y +
            controller.center.y +
            controller.height * 0.5f;

        /*
         * Işını aşağıdan yukarı göndermiyoruz. Unity'deki Plane
         * gibi tek yüzlü çatılar alttan görünmeyebilir. Bu yüzden
         * çatının üstünden aşağı doğru tarama yapıyoruz.
         */
        float taramaBaslangicY =
            Mathf.Max(
                bulunanDuvarUstuY + 0.75f,
                karakterBasY + 1f
            );

        Vector3 taramaBaslangici =
            new Vector3(
                transform.position.x,
                taramaBaslangicY,
                transform.position.z
            );

        float taramaMesafesi =
            taramaBaslangicY -
            karakterBasY;

        float taramaYaricapi =
            Mathf.Max(
                0.05f,
                controller.radius * 0.35f
            );

        RaycastHit[] bulunanYuzeyler =
            Physics.SphereCastAll(
                taramaBaslangici,
                taramaYaricapi,
                Vector3.down,
                taramaMesafesi,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        foreach (RaycastHit bulunanYuzey in
                 bulunanYuzeyler)
        {
            Collider bulunanCollider =
                bulunanYuzey.collider;

            if (bulunanCollider == null)
                continue;

            bool karakterinKendisiMi =
                bulunanCollider.transform == transform ||
                bulunanCollider.transform.IsChildOf(transform);

            if (karakterinKendisiMi)
                continue;

            /*
             * Yalnızca karakterin baş seviyesinin üzerindeki
             * yüzeyler tavan sayılır. Alttaki zemin önemsenmez.
             */
            if (bulunanYuzey.point.y >
                karakterBasY + 0.05f)
            {
                if (duvarTirmanmaDebug)
                {
                    Debug.Log(
                        "Tırmanma iptal: Karakterin üstünde " +
                        "tavan/çatı var. Engel: " +
                        bulunanCollider.gameObject.name
                    );
                }

                return true;
            }
        }

        return false;
    }

    private IEnumerator DuvaraTirman(
        RaycastHit duvarVurusu,
        RaycastHit duvarUstuVurusu,
        Vector3 hedefPozisyon)
    {
        duvaraTirmaniyorMu = true;

        dikeyHiz = Vector3.zero;
        ziplamaYatayHizi = Vector3.zero;

        Vector3 duvarIciYonu =
            -duvarVurusu.normal;

        duvarIciYonu.y = 0f;
        duvarIciYonu.Normalize();

        Quaternion duvaraBakanDonus =
            Quaternion.LookRotation(
                duvarIciYonu
            );

        transform.rotation =
            duvaraBakanDonus;

        controller.enabled = false;

        aktifDuvarNoktasi =
            duvarVurusu.point;

        aktifDuvarDisNormali =
            duvarVurusu.normal.normalized;

        duvardanMinimumGövdeMesafesi =
            controller.radius + 0.20f;

        duvarOnundeKilitlemeAktif =
            true;

        /*
         * Önce karakteri duvarın tam önüne hizala.
         * Bu aşamada yukarı taşımıyoruz.
         */
        Vector3 hizalamaBaslangici =
            transform.position;

        Vector3 duvarOnuPozisyonu =
            duvarVurusu.point +
            duvarVurusu.normal *
            duvardanMinimumGövdeMesafesi;

        duvarOnuPozisyonu.y =
            hizalamaBaslangici.y;

        float hizalamaSuresi = 0.12f;
        float hizalamaGecenSure = 0f;

        while (hizalamaGecenSure <
               hizalamaSuresi)
        {
            hizalamaGecenSure +=
                Time.deltaTime;

            float hizalamaOrani =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        hizalamaGecenSure /
                        hizalamaSuresi
                    )
                );

            transform.position =
                Vector3.Lerp(
                    hizalamaBaslangici,
                    duvarOnuPozisyonu,
                    hizalamaOrani
                );

            yield return null;
        }

        transform.position =
            duvarOnuPozisyonu;

        if (animator != null)
        {
            animator.speed = 1f;

            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("IsStairMoving", false);

            /*
             * MatchTarget yalnızca Root Motion açıkken çalışır.
             * Tırmanma bitince yeniden kapatılacak.
             */
            animator.applyRootMotion = true;

            animator.CrossFadeInFixedTime(
                duvarTirmanmaState,
                0.05f,
                0
            );

            /*
             * Animator'ın gerçekten duvar tırmanma durumuna
             * girmesini bekle.
             */
            float durumaGirisBekleme = 0.5f;
            bool tirmanmaDurumunaGirdiMi = false;

            while (durumaGirisBekleme > 0f)
            {
                AnimatorStateInfo durum =
                    animator.GetCurrentAnimatorStateInfo(0);

                if (durum.fullPathHash ==
                    duvarTirmanmaState)
                {
                    tirmanmaDurumunaGirdiMi = true;
                    break;
                }

                durumaGirisBekleme -=
                    Time.deltaTime;

                yield return null;
            }

            if (tirmanmaDurumunaGirdiMi)
            {
                /*
                 * Ölçülen gerçek duvar kenarı.
                 * Sağ el animasyonun ilk bölümünde tam buraya
                 * ulaşacak. Böylece karakter çatının üstüne
                 * fırlamak yerine kenara tutunacak.
                 */
                Vector3 sagElHedefi =
                    duvarUstuVurusu.point +
                    duvarVurusu.normal * 0.04f +
                    transform.right * 0.12f;

                MatchTargetWeightMask elMaskesi =
                    new MatchTargetWeightMask(
                        new Vector3(
                            0f,
                            1f,
                            0f
                        ),
                        0f
                    );

                animator.MatchTarget(
                    sagElHedefi,
                    duvaraBakanDonus,
                    AvatarTarget.RightHand,
                    elMaskesi,
                    0.05f,
                    0.38f
                );

                bool ustuneCikmaEslestirildiMi =
                    false;

                float guvenlikSuresi =
                    Mathf.Max(
                        2.5f,
                        duvarTirmanmaSuresi + 1f
                    );

                while (guvenlikSuresi > 0f)
                {
                    AnimatorStateInfo durum =
                        animator.GetCurrentAnimatorStateInfo(0);

                    if (durum.fullPathHash !=
                        duvarTirmanmaState &&
                        !animator.IsInTransition(0))
                    {
                        break;
                    }

                    float animasyonOrani =
                        durum.normalizedTime;

                    /*
                     * El kenara ulaştıktan sonra animasyonun kalan
                     * hareketini duvarın üstündeki fiziksel hedefe
                     * uydur.
                     */
                    if (!ustuneCikmaEslestirildiMi &&
                        !animator.isMatchingTarget &&
                        animasyonOrani >= 0.50f &&
                        animasyonOrani < 0.90f)
                    {
                        /*
                         * El kenara tutundu. Artık animasyonun
                         * yukarı ve ileri çekme bölümüne izin ver.
                         */
                        duvarOnundeKilitlemeAktif =
                            false;

                        float eslestirmeBaslangici =
                            Mathf.Clamp(
                                animasyonOrani + 0.01f,
                                0.51f,
                                0.75f
                            );

                        MatchTargetWeightMask kokMaskesi =
                            new MatchTargetWeightMask(
                                Vector3.one,
                                0f
                            );

                        animator.MatchTarget(
                            hedefPozisyon,
                            duvaraBakanDonus,
                            AvatarTarget.Root,
                            kokMaskesi,
                            eslestirmeBaslangici,
                            0.96f
                        );

                        ustuneCikmaEslestirildiMi =
                            true;
                    }

                    if (animasyonOrani >= 0.98f &&
                        !animator.isMatchingTarget)
                    {
                        break;
                    }

                    guvenlikSuresi -=
                        Time.deltaTime;

                    yield return null;
                }
            }

            /*
             * Tırmanma bitti. Bundan sonraki bütün hareketleri
             * tekrar CharacterController yönetecek.
             */
            animator.applyRootMotion = false;
        }

        duvarOnundeKilitlemeAktif =
            false;

        transform.position =
            hedefPozisyon;

        transform.rotation =
            duvaraBakanDonus;

        controller.enabled = true;

        dikeyHiz =
            Vector3.zero;

        havadaMi = false;
        ziplamaIleHavayaCiktiMi = false;
        havaAnimasyonuBasladiMi = false;

        kilitliDususRotasi =
            DususRotasi.Yok;

        sonYerdekiYukseklik =
            transform.position.y;

        if (animator != null)
        {
            animator.CrossFadeInFixedTime(
                idleState,
                0.1f,
                0
            );
        }

        duvaraTirmaniyorMu = false;
    }

    // UZUN DÜŞÜŞ + GET UP BİTENE KADAR BÜTÜN KONTROLLERİ KİLİTLER
    private void UzunDususVeKalkmayiBaslat()
    {
        if (uzunDususKalkmaAnimasyonuOynuyorMu)
        {
            return;
        }

        uzunDususKalkmaAnimasyonuOynuyorMu = true;
        ziplamaYatayHizi = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("IsStairMoving", false);
            animator.SetBool("IsCrouchWalking", false);

            animator.CrossFadeInFixedTime(
                uzunDususYereInisState,
                0.05f,
                0
            );
        }

        StartCoroutine(
            UzunDususVeKalkmaninBitmesiniBekle()
        );
    }

    private IEnumerator UzunDususVeKalkmaninBitmesiniBekle()
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(2.5f);
            uzunDususKalkmaAnimasyonuOynuyorMu = false;
            yield break;
        }

        float guvenlikSuresi = 8f;
        bool uzunDususDurumunaGirdiMi = false;
        bool getUpDurumunaGirdiMi = false;

        while (guvenlikSuresi > 0f)
        {
            AnimatorStateInfo durum =
                animator.GetCurrentAnimatorStateInfo(0);

            bool uzunDususDurumundaMi =
                durum.fullPathHash ==
                uzunDususYereInisState;

            bool getUpDurumundaMi =
                durum.fullPathHash ==
                getUpState;

            if (uzunDususDurumundaMi)
            {
                uzunDususDurumunaGirdiMi = true;
            }

            if (getUpDurumundaMi)
            {
                getUpDurumunaGirdiMi = true;

                if (durum.normalizedTime >= 0.98f &&
                    !animator.IsInTransition(0))
                {
                    break;
                }
            }

            // Get up başka bir state'e geçtiyse animasyon tamamlanmıştır.
            if (getUpDurumunaGirdiMi &&
                !getUpDurumundaMi &&
                !animator.IsInTransition(0))
            {
                break;
            }

            /*
             * Uzun düşüş oynadı fakat Animator'da get up bağlantısı
             * yoksa karakter sonsuza kadar kilitli kalmasın.
             */
            if (uzunDususDurumunaGirdiMi &&
                !uzunDususDurumundaMi &&
                !getUpDurumundaMi &&
                !getUpDurumunaGirdiMi &&
                !animator.IsInTransition(0))
            {
                break;
            }

            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("IsStairMoving", false);
            animator.SetBool("IsCrouchWalking", false);

            guvenlikSuresi -= Time.deltaTime;
            yield return null;
        }

        animator.CrossFadeInFixedTime(
            idleState,
            0.08f,
            0
        );

        uzunDususKalkmaAnimasyonuOynuyorMu = false;
    }

    // KISA DÜŞÜŞ YERE İNİŞİNİ BAŞLATIR VE KONTROLLERİ KİLİTLER
    private void KisaDususInisiniBaslat()
    {
        if (kisaDususInisAnimasyonuOynuyorMu)
        {
            return;
        }

        kisaDususInisAnimasyonuOynuyorMu =
            true;

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("IsStairMoving", false);

            animator.CrossFadeInFixedTime(
                kisaDususYereInisState,
                0.05f,
                0
            );
        }

        StartCoroutine(
            KisaDususInisAnimasyonununBitmesiniBekle()
        );
    }

    // DÜŞÜŞ KISA ANİMASYONU BİTMEDEN HAREKET KİLİDİNİ AÇMAZ
    private IEnumerator
        KisaDususInisAnimasyonununBitmesiniBekle()
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(1f);

            kisaDususInisAnimasyonuOynuyorMu =
                false;

            yield break;
        }

        float durumaGirisBeklemeSuresi = 0.75f;
        bool inisDurumunaGirdiMi = false;

        /*
         * CrossFade başladıktan sonra Animator'ın gerçekten
         * 'düşüş kısa' durumuna girmesini bekle.
         */
        while (durumaGirisBeklemeSuresi > 0f)
        {
            AnimatorStateInfo durum =
                animator.GetCurrentAnimatorStateInfo(0);

            if (durum.fullPathHash ==
                kisaDususYereInisState)
            {
                inisDurumunaGirdiMi = true;
                break;
            }

            durumaGirisBeklemeSuresi -=
                Time.deltaTime;

            yield return null;
        }

        if (inisDurumunaGirdiMi)
        {
            while (true)
            {
                AnimatorStateInfo durum =
                    animator.GetCurrentAnimatorStateInfo(0);

                bool halenInisDurumunda =
                    durum.fullPathHash ==
                    kisaDususYereInisState;

                /*
                 * Normalized Time 1 olduğunda animasyonun tamamı
                 * oynanmıştır. 0.98'de çıkmak geçişi yumuşatır.
                 */
                if (halenInisDurumunda &&
                    durum.normalizedTime >= 0.98f &&
                    !animator.IsInTransition(0))
                {
                    break;
                }

                /*
                 * Animator zaten başka duruma geçtiyse geçişin
                 * tamamlanmasını bekleyip kilidi aç.
                 */
                if (!halenInisDurumunda &&
                    !animator.IsInTransition(0))
                {
                    break;
                }

                animator.SetBool("isWalking", false);
                animator.SetBool("isRunning", false);

                yield return null;
            }
        }
        else
        {
            // Animator durumu bulunamazsa karakter sonsuza dek kilitlenmesin.
            yield return new WaitForSeconds(1f);
        }

        animator.CrossFadeInFixedTime(
            idleState,
            0.08f,
            0
        );

        kisaDususInisAnimasyonuOynuyorMu =
            false;
    }

    private bool AnimatorKalkmaAnimasyonuOynuyorMu()
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorStateInfo mevcutDurum =
            animator.GetCurrentAnimatorStateInfo(0);

        bool mevcutDurumKalkmaMi =
            mevcutDurum.shortNameHash == kalkmaStateHash ||
            mevcutDurum.fullPathHash == getUpState;

        if (mevcutDurumKalkmaMi)
        {
            return true;
        }

        /*
         * Animator Kalkma/get up state'ine geçiş yapıyorsa, state tam
         * başlamadan önceki geçiş karelerinde de hareket açılmasın.
         */
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo sonrakiDurum =
                animator.GetNextAnimatorStateInfo(0);

            bool sonrakiDurumKalkmaMi =
                sonrakiDurum.shortNameHash == kalkmaStateHash ||
                sonrakiDurum.fullPathHash == getUpState;

            if (sonrakiDurumKalkmaMi)
            {
                return true;
            }
        }

        return false;
    }

    // KALKMA ANİMASYONUNU BEKLER
    private IEnumerator KalkmaAnimasyonununBitmesiniBekle()
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(
                kalkmaAnimasyonSuresi
            );

            kalkiyorMu = false;
            yield break;
        }

        float girisBeklemeSuresi = 2f;
        bool kalkmaDurumunaGirdiMi = false;

        while (girisBeklemeSuresi > 0f)
        {
            AnimatorStateInfo durum =
                animator.GetCurrentAnimatorStateInfo(0);

            if (durum.shortNameHash ==
                kalkmaStateHash)
            {
                kalkmaDurumunaGirdiMi = true;
                break;
            }

            girisBeklemeSuresi -=
                Time.deltaTime;

            yield return null;
        }

        if (kalkmaDurumunaGirdiMi)
        {
            while (true)
            {
                AnimatorStateInfo durum =
                    animator.GetCurrentAnimatorStateInfo(0);

                bool halenKalkmaDurumunda =
                    durum.shortNameHash ==
                    kalkmaStateHash;

                if (!halenKalkmaDurumunda &&
                    !animator.IsInTransition(0))
                {
                    break;
                }

                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(
                kalkmaAnimasyonSuresi
            );
        }

        kalkiyorMu = false;
    }

    private void AnimasyonIsimleriniKontrolEt()
    {
        if (!animator.HasState(
            0,
            kisaDususHavaState))
        {
            Debug.LogError(
                "'kısa düşüş yol' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            uzunDususHavaState))
        {
            Debug.LogError(
                "'falling' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            kisaDususYereInisState))
        {
            Debug.LogError(
                "'düşüş kısa' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            uzunDususYereInisState))
        {
            Debug.LogError(
                "'uzun düşüş' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            getUpState))
        {
            Debug.LogError(
                "'get up' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            duvarTirmanmaState))
        {
            Debug.LogError(
                "'duvar tırmanma' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            merdivenCikmaState))
        {
            Debug.LogError(
                "'merdiven çıkma' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            merdivenInmeState))
        {
            Debug.LogError(
                "'merdiven inme' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            egilerekYurumeState))
        {
            Debug.LogError(
                "'eğilerek yürüme' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            sigaraYakmaState))
        {
            Debug.LogError(
                "'sigara yakma' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            sigaraIcmeState))
        {
            Debug.LogError(
                "'sigara içme' bulunamadı."
            );
        }

        if (!animator.HasState(
            0,
            sigaraSondurmeState))
        {
            Debug.LogError(
                "'sigara söndürme' bulunamadı."
            );
        }
    }
}