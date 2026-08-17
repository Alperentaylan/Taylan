using System;
using UnityEngine;

/// <summary>
/// GTA tarzi 3. sahis / 1. sahis kamera gecisi.
///
/// ONEMLI:
/// - KarakterHareketi ASLA kapatilmaz.
/// - Animator ASLA kapatilmaz.
/// - Mevcut yuru/kos/ziplama/crouch/fall/sigara animasyonlari calismaya devam eder.
/// - FPS kamerasi animasyonlu Head bone'una parent edilmez; titreme almaz.
/// - Buna ragmen Head / Neck / Chest kemiklerine LateUpdate'ta additive look uygulanir.
/// - Mouse ile once kafa/ust govde bakar; limit asilirsa govde yumusakca kameraya yetisir.
/// - Hareket ederken govde kameraya daha hizli yetisir (GTA hissi).
/// - Hafif head-bob, strafe roll, landing kick ve sprint FOV vardir.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(12000)]
public sealed class BirinciUcuncuSahisKesin : MonoBehaviour
{
    [Header("OYUNCU")]
    [Tooltip("CharacterController bulunan ana oyuncu nesnesi.")]
    [SerializeField] private CharacterController characterController;

    [Tooltip("Donecek ana oyuncu koku. Genellikle CharacterController bulunan nesne.")]
    [SerializeField] private Transform oyuncuKoku;

    [Tooltip("Mevcut KarakterHareketi scripti. Bos birakilirsa otomatik bulunur.")]
    [SerializeField] private KarakterHareketi karakterHareketi;

    [Tooltip("Karakter Animator'u. Bos birakilirsa otomatik bulunur.")]
    [SerializeField] private Animator animator;

    [Header("KAMERALAR")]
    [Tooltip("Ucuncu sahis kamera rig'i. Karakterin ana nesnesini buraya verme.")]
    [SerializeField] private GameObject ucuncuSahisKameraRoot;

    [SerializeField] private Camera ucuncuSahisKamera;
    [SerializeField] private Camera birinciSahisKamera;

    [Header("FPS KAMERA KONUMU")]
    [Tooltip("CharacterController tepesinden kameranin ne kadar asagida olacagi.")]
    [Min(0f)]
    [SerializeField] private float tepedenAsagiMesafe = 0.17f;

    [Tooltip("Kamerayi yuzun biraz onune alir. Tek mesh karakterde kafa icini gormeyi azaltir.")]
    [SerializeField] private float kameraIleriMesafe = 0.085f;

    [SerializeField] private Vector3 kameraKonumDuzeltmesi = Vector3.zero;

    [Tooltip("FPS kamera near clip. Yuze cok yakin geometri clipping'ini azaltir.")]
    [SerializeField] private float fpsNearClip = 0.06f;

    [Header("GOZ ANCHOR")]
    [Tooltip("Humanoid Animator'da LeftEye / RightEye otomatik bulunur ve FPS kamera iki gozun ortasini takip eder.")]
    [SerializeField] private bool gozlereSabitle = true;

    [SerializeField] private Transform solGozKemigi;
    [SerializeField] private Transform sagGozKemigi;

    [Tooltip("Kamerayi gozlerin biraz onune cikarir; goz/dudak/yanak gorunmesini engeller.")]
    [SerializeField] private float gozlerdenOneCikma = 0.18f;

    [SerializeField] private float gozYukariDuzeltme = 0.004f;

    [Tooltip("Kosu animasyonunda goz noktasini yumusak takip eder.")]
    [SerializeField] private float gozTakipYumusaklik = 38f;

    [Tooltip("Eye bone yoksa Head bone'dan tahmini goz konumu.")]
    [SerializeField] private Vector3 kafaFallbackGozOffset = new Vector3(0f, 0.075f, 0.105f);

    [Header("FARE BAKISI")]
    [Min(0.01f)]
    [SerializeField] private float fareHassasiyeti = 2.2f;

    [Range(10f, 89f)]
    [SerializeField] private float yukariBakmaSiniri = 80f;

    [Range(-89f, -10f)]
    [SerializeField] private float asagiBakmaSiniri = -78f;

    [Tooltip("Karakter sabitken kafa kamerayla govdeden en fazla kac derece ayrilabilir.")]
    [Range(10f, 85f)]
    [SerializeField] private float maksimumSerbestKafaYaw = 52f;

    [Tooltip("Hareket ederken kafa/govde arasinda birakilan daha kucuk yaw.")]
    [Range(0f, 45f)]
    [SerializeField] private float hareketKafaYawLimiti = 20f;

    [Tooltip("Kafa yaw limitini asinca govde kameraya bu hizla yetisir.")]
    [SerializeField] private float govdeDonusHizi = 8.5f;

    [Tooltip("WASD basiliyken govde kameraya daha hizli yetisir.")]
    [SerializeField] private float hareketGovdeDonusHizi = 11f;

    [Tooltip("Idle durumda fareyle sadece kafayi cevir; limitte govde donmeye baslasin.")]
    [SerializeField] private bool idleSerbestBakis = true;

    [Header("KAFA / BOYUN / GOGUS ANIMASYON KATMANI")]
    [Tooltip("Humanoid Animator ise Head/Neck/Chest otomatik bulunur.")]
    [SerializeField] private bool kafaKemikleriniOtomatikBul = true;

    [Tooltip("Humanoid degilse elle kafa bone verebilirsin.")]
    [SerializeField] private Transform kafaKemigi;

    [SerializeField] private Transform boyunKemigi;
    [SerializeField] private Transform gogusKemigi;

    [Tooltip("Toplam yaw dagilimi. Head agirlikli.")]
    [Range(0f, 1f)]
    [SerializeField] private float kafaYawAgirligi = 0.58f;

    [Range(0f, 1f)]
    [SerializeField] private float boyunYawAgirligi = 0.28f;

    [Range(0f, 1f)]
    [SerializeField] private float gogusYawAgirligi = 0.14f;

    [Range(0f, 1f)]
    [SerializeField] private float kafaPitchAgirligi = 0.62f;

    [Range(0f, 1f)]
    [SerializeField] private float boyunPitchAgirligi = 0.25f;

    [Range(0f, 1f)]
    [SerializeField] private float gogusPitchAgirligi = 0.13f;

    [Tooltip("Kemik bakis hareketini yumusatir.")]
    [SerializeField] private float kemikBakisYumusaklik = 14f;

    [Tooltip("Kafa bone'unun yaw yonu tersse -1 yap.")]
    [SerializeField] private float kemikYawYon = 1f;

    [Tooltip("Kafa bone'unun pitch yonu tersse -1 yap.")]
    [SerializeField] private float kemikPitchYon = 1f;

    [Header("GTA HEAD BOB")]
    [SerializeField] private bool headBobAktif = true;

    [SerializeField] private float yurumeBobFrekans = 7.2f;
    [SerializeField] private float kosmaBobFrekans = 10.2f;

    [SerializeField] private float yurumeBobYukseklik = 0.018f;
    [SerializeField] private float kosmaBobYukseklik = 0.029f;

    [SerializeField] private float bobYanMiktar = 0.012f;

    [Tooltip("Bob'un giris/cikis yumusakligi.")]
    [SerializeField] private float bobYumusaklik = 9f;

    [Header("STRAFE / HAREKET KAMERA TEPKISI")]
    [SerializeField] private bool hareketTiltAktif = true;

    [Tooltip("A/D ile kameranin hafif yana yatmasi.")]
    [SerializeField] private float maksimumStrafeRoll = 1.25f;

    [Tooltip("W/S ile cok hafif pitch.")]
    [SerializeField] private float maksimumHareketPitch = 0.55f;

    [SerializeField] private float tiltYumusaklik = 7f;

    [Header("LANDING KICK")]
    [SerializeField] private bool landingKickAktif = true;

    [Tooltip("Yere iniste minimum asagi hiz.")]
    [SerializeField] private float landingMinimumDususHizi = 3f;

    [Tooltip("Sert iniste kamera ne kadar asagi otursun.")]
    [SerializeField] private float maksimumLandingDip = 0.055f;

    [Tooltip("Sert iniste kamera pitch kick.")]
    [SerializeField] private float maksimumLandingPitch = 2.1f;

    [SerializeField] private float landingToparlama = 8f;

    [Header("SPRINT FOV")]
    [SerializeField] private bool sprintFovAktif = true;

    [SerializeField] private float normalFov = 72f;
    [SerializeField] private float kosmaFov = 78f;
    [SerializeField] private float fovYumusaklik = 6f;

    [Header("GECIS")]
    [SerializeField] private KeyCode gecisTusu = KeyCode.V;
    [SerializeField] private bool oyunBaslangicindaBirinciSahis;

    [Tooltip("FPS'e giris/cikis aninda mevcut bakis acisini koru.")]
    [SerializeField] private bool gecisteBakisiKoru = true;

    [Header("DEBUG - PLAY MODDA")]
    [SerializeField] private bool birinciSahisAktif;
    [SerializeField] private bool animatorAktif;
    [SerializeField] private bool humanoidAnimator;
    [SerializeField] private float kameraYaw;
    [SerializeField] private float govdeYaw;
    [SerializeField] private float kafaYaw;
    [SerializeField] private float pitch;
    [SerializeField] private float yatayHiz;
    [SerializeField] private bool hareketVar;
    [SerializeField] private bool yerde;
    [SerializeField] private string bulunanKafa = "-";
    [SerializeField] private string bulunanBoyun = "-";
    [SerializeField] private string bulunanGogus = "-";
    [SerializeField] private string bulunanSolGoz = "-";
    [SerializeField] private string bulunanSagGoz = "-";
    [SerializeField] private string kameraAnchorKaynagi = "-";

    private Transform birinciSahisRig;

    // Absolute camera yaw in world space.
    private float viewYaw;

    // Camera relative yaw against body.
    private float relativeHeadYaw;

    // Smoothed additive bone values.
    private float smoothBoneYaw;
    private float smoothBonePitch;

    private float bobTime;
    private Vector3 currentBobOffset;

    private float currentStrafeRoll;
    private float currentMovePitch;

    private bool oncekiFrameYerde;
    private float oncekiDikeyHiz;
    private float landingDip;
    private float landingPitchKick;

    private float eskiFov = 60f;
    private float eskiNearClip = 0.3f;
    private Vector3 smoothEyeWorldPosition;
    private bool eyePositionReady;
    private float modelYuzYawOffset;
    private bool modelYuzYonuHazir;

    public bool BirinciSahisAktifMi
    {
        get { return birinciSahisAktif; }
    }

    private void Awake()
    {
        ReferanslariBul();

        if (characterController == null ||
            oyuncuKoku == null)
        {
            Debug.LogError(
                "BirinciUcuncuSahisKesin: CharacterController veya oyuncu koku bulunamadi.",
                this);

            enabled = false;
            return;
        }

        if (karakterHareketi == null)
        {
            Debug.LogError(
                "BirinciUcuncuSahisKesin: KarakterHareketi bulunamadi. " +
                "Mevcut hareket/animasyon scriptini KAPATMIYORUZ; alana mevcut KarakterHareketi'ni ver.",
                this);

            enabled = false;
            return;
        }

        BirinciSahisRigOlustur();
        KemikleriBul();
        gozlereSabitle = true;
        ModelinGercekYuzYonunuHazirla();

        viewYaw =
            GovdeninYuzYawDegeri();

        pitch = 0f;

        oncekiFrameYerde =
            characterController.isGrounded;

        oncekiDikeyHiz =
            characterController.velocity.y;

        ModDegistir(
            oyunBaslangicindaBirinciSahis,
            true);
    }

    private void Update()
    {
        // Baska bir kamera/gecis scripti yanlislikla kapatsa bile
        // hareket ve Animator hicbir kare kapali kalmasin.
        if (karakterHareketi != null && !karakterHareketi.enabled)
            karakterHareketi.enabled = true;

        if (animator != null && !animator.enabled)
            animator.enabled = true;

        if (Input.GetKeyDown(
                gecisTusu))
        {
            ModDegistir(
                !birinciSahisAktif,
                false);
        }

        if (!birinciSahisAktif)
            return;

        // KarakterHareketi ve Animator acik kalir.
        FareBakisiGTA();
        HareketBilgisiniGuncelle();
        LandingKickGuncelle();
        FovGuncelle();
    }

    private void LateUpdate()
    {
        if (!birinciSahisAktif ||
            oyuncuKoku == null ||
            birinciSahisRig == null)
        {
            return;
        }

        // KarakterHareketi A/D vb. ile root rotation degistirdiyse
        // burada GTA FPS body-follow mantigini SON katman olarak uygula.
        GovdeTakibiniUygula();

        HeadBobVeTiltGuncelle();

        // Animator bu frame pozunu verdikten sonra kafa/boyun/gogus
        // animasyonunun USTUNE additive mouse look uygula.
        KemikBakisiniUygula();

        // Kemikler donduruldukten SONRA gozlerin son dunya konumunu oku.
        // Aksi halde kamera bir kare geriden gelip ense/kulak gosterebilir.
        KameraRiginiSabitle();

        DebugGuncelle();
    }

    // ============================================================
    // REFERENCES
    // ============================================================
    private void ReferanslariBul()
    {
        if (characterController == null)
        {
            characterController =
                GetComponent<CharacterController>();

            if (characterController == null)
            {
                characterController =
                    GetComponentInParent<CharacterController>();
            }

            if (characterController == null)
            {
                characterController =
                    GetComponentInChildren<CharacterController>(
                        true);
            }
        }

        if (oyuncuKoku == null &&
            characterController != null)
        {
            oyuncuKoku =
                characterController.transform;
        }

        if (karakterHareketi == null &&
            oyuncuKoku != null)
        {
            karakterHareketi =
                oyuncuKoku.GetComponent<
                    KarakterHareketi>();

            if (karakterHareketi == null)
            {
                karakterHareketi =
                    oyuncuKoku.GetComponentInChildren<
                        KarakterHareketi>(
                            true);
            }
        }

        if (animator == null &&
            oyuncuKoku != null)
        {
            animator =
                oyuncuKoku.GetComponent<Animator>();

            if (animator == null)
            {
                animator =
                    oyuncuKoku.GetComponentInChildren<Animator>(
                        true);
            }
        }

        KameralariBulVeyaOlustur();
    }

    private void KameralariBulVeyaOlustur()
    {
        Camera[] sahnedekiKameralar =
            FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (ucuncuSahisKamera == null)
        {
            Camera main = Camera.main;
            if (main != null)
                ucuncuSahisKamera = main;
            else if (sahnedekiKameralar.Length > 0)
                ucuncuSahisKamera = sahnedekiKameralar[0];
        }

        if (birinciSahisKamera == null)
        {
            for (int i = 0; i < sahnedekiKameralar.Length; i++)
            {
                Camera aday = sahnedekiKameralar[i];
                if (aday == null || aday == ucuncuSahisKamera)
                    continue;

                string ad = aday.name.ToLowerInvariant();
                if (ad.Contains("fps") ||
                    ad.Contains("birincisahis") ||
                    ad.Contains("birinci sahis"))
                {
                    birinciSahisKamera = aday;
                    break;
                }
            }
        }

        // Sahnede ayri FPS Camera yoksa MainCamera'nin goruntu ayarlarini
        // kopyalayarak kendimiz olustururuz. Elle Inspector atamasi gerekmez.
        if (birinciSahisKamera == null ||
            birinciSahisKamera == ucuncuSahisKamera)
        {
            GameObject fpsKameraObjesi =
                new GameObject("BirinciSahisKamera_Otomatik");

            birinciSahisKamera =
                fpsKameraObjesi.AddComponent<Camera>();

            if (ucuncuSahisKamera != null)
            {
                birinciSahisKamera.CopyFrom(ucuncuSahisKamera);

                if (ucuncuSahisKamera.GetComponent<AudioListener>() != null)
                    fpsKameraObjesi.AddComponent<AudioListener>();
            }
        }

        if (ucuncuSahisKameraRoot == null &&
            ucuncuSahisKamera != null &&
            ucuncuSahisKamera.transform.parent != null)
        {
            ucuncuSahisKameraRoot =
                ucuncuSahisKamera.transform.parent.gameObject;
        }
    }

    private void KemikleriBul()
    {
        if (!kafaKemikleriniOtomatikBul)
        {
            DebugKemikAdlari();
            return;
        }

        humanoidAnimator =
            animator != null &&
            animator.avatar != null &&
            animator.avatar.isValid &&
            animator.avatar.isHuman;

        if (!humanoidAnimator)
        {
            Debug.LogWarning(
                "BirinciUcuncuSahisKesin: Animator Humanoid degil; " +
                "Mixamo kemikleri isimlerinden otomatik bulunacak.",
                this);
        }

        try
        {
            if (humanoidAnimator && kafaKemigi == null)
            {
                kafaKemigi =
                    animator.GetBoneTransform(
                        HumanBodyBones.Head);
            }

            if (humanoidAnimator && boyunKemigi == null)
            {
                boyunKemigi =
                    animator.GetBoneTransform(
                        HumanBodyBones.Neck);
            }

            if (humanoidAnimator && gogusKemigi == null)
            {
                gogusKemigi =
                    animator.GetBoneTransform(
                        HumanBodyBones.Chest);

                if (gogusKemigi == null)
                {
                    gogusKemigi =
                        animator.GetBoneTransform(
                            HumanBodyBones.UpperChest);
                }
            }

            if (humanoidAnimator && solGozKemigi == null)
            {
                solGozKemigi = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            }

            if (humanoidAnimator && sagGozKemigi == null)
            {
                sagGozKemigi = animator.GetBoneTransform(HumanBodyBones.RightEye);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "BirinciUcuncuSahisKesin: Humanoid kemik otomatik bulma basarisiz: " +
                e.Message,
                this);
        }

        KemikleriIsimdenBul();
        DebugKemikAdlari();
    }

    private void KemikleriIsimdenBul()
    {
        if (oyuncuKoku == null)
            return;

        Transform[] kemikler =
            oyuncuKoku.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < kemikler.Length; i++)
        {
            Transform aday = kemikler[i];
            if (aday == null)
                continue;

            string ad = aday.name.ToLowerInvariant();

            // mixamorig9:Head gibi adlari yakalar; HeadShoulder'i yakalamaz.
            bool tamHead =
                ad == "head" || ad.EndsWith(":head") || ad.EndsWith("_head");
            bool tamNeck =
                ad == "neck" || ad.EndsWith(":neck") || ad.EndsWith("_neck");
            bool chest =
                ad.EndsWith(":spine2") || ad.EndsWith("_spine2") ||
                ad == "chest" || ad.EndsWith(":chest");

            if (kafaKemigi == null && tamHead)
                kafaKemigi = aday;

            if (boyunKemigi == null && tamNeck)
                boyunKemigi = aday;

            if (gogusKemigi == null && chest)
                gogusKemigi = aday;
        }
    }

    private void DebugKemikAdlari()
    {
        bulunanKafa =
            kafaKemigi != null
                ? kafaKemigi.name
                : "BULUNAMADI";

        bulunanBoyun =
            boyunKemigi != null
                ? boyunKemigi.name
                : "BULUNAMADI";

        bulunanGogus =
            gogusKemigi != null
                ? gogusKemigi.name
                : "BULUNAMADI";

        bulunanSolGoz =
            solGozKemigi != null
                ? solGozKemigi.name
                : "BULUNAMADI";

        bulunanSagGoz =
            sagGozKemigi != null
                ? sagGozKemigi.name
                : "BULUNAMADI";
    }

    private void ModelinGercekYuzYonunuHazirla()
    {
        if (oyuncuKoku == null || kafaKemigi == null)
            return;

        // Humanoid LeftEye/RightEye yoksa ekrandaki modelde bulunan
        // "sol goz" / "sag goz" objelerini otomatik referans olarak kullan.
        Transform[] tumTransformlar =
            oyuncuKoku.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < tumTransformlar.Length; i++)
        {
            Transform aday = tumTransformlar[i];
            if (aday == null || aday == kafaKemigi)
                continue;

            string ad = aday.name.ToLowerInvariant()
                .Replace("ð", "g")
                .Replace("ý", "i")
                .Replace("þ", "s")
                .Replace("ö", "o")
                .Replace("ü", "u");

            if (!ad.Contains("goz") && !ad.Contains("eye"))
                continue;

            if (solGozKemigi == null &&
                (ad.Contains("sol") || ad.Contains("left")))
                solGozKemigi = aday;

            if (sagGozKemigi == null &&
                (ad.Contains("sag") || ad.Contains("right")))
                sagGozKemigi = aday;
        }

        if (solGozKemigi == null || sagGozKemigi == null)
            return;

        Vector3 gozOrtasi =
            (GozMerkeziniAl(solGozKemigi) + GozMerkeziniAl(sagGozKemigi)) * 0.5f;

        Vector3 gercekYuzIleri =
            Vector3.ProjectOnPlane(
                gozOrtasi - kafaKemigi.position,
                oyuncuKoku.up);

        if (gercekYuzIleri.sqrMagnitude < 0.000001f)
            return;

        gercekYuzIleri.Normalize();

        float gercekYuzYaw =
            Mathf.Atan2(gercekYuzIleri.x, gercekYuzIleri.z) * Mathf.Rad2Deg;

        modelYuzYawOffset =
            Mathf.DeltaAngle(oyuncuKoku.eulerAngles.y, gercekYuzYaw);

        modelYuzYonuHazir = true;
        DebugKemikAdlari();
    }

    private static Vector3 GozMerkeziniAl(Transform goz)
    {
        if (goz == null)
            return Vector3.zero;

        Renderer gozRenderer = goz.GetComponentInChildren<Renderer>(true);
        return gozRenderer != null ? gozRenderer.bounds.center : goz.position;
    }

    private float GovdeninYuzYawDegeri()
    {
        return oyuncuKoku.eulerAngles.y +
               (modelYuzYonuHazir ? modelYuzYawOffset : 0f);
    }

    // ============================================================
    // CAMERA RIG
    // ============================================================
    private void BirinciSahisRigOlustur()
    {
        Transform eskiRig =
            oyuncuKoku.Find(
                "FP_Rig_Kesin");

        if (eskiRig != null)
        {
            birinciSahisRig =
                eskiRig;
        }
        else
        {
            GameObject rigObjesi =
                new GameObject(
                    "FP_Rig_Kesin");

            birinciSahisRig =
                rigObjesi.transform;

            birinciSahisRig.SetParent(
                oyuncuKoku,
                false);
        }

        if (birinciSahisKamera != null)
        {
            eskiFov =
                birinciSahisKamera.fieldOfView;

            eskiNearClip =
                birinciSahisKamera.nearClipPlane;

            birinciSahisKamera.transform.SetParent(
                birinciSahisRig,
                false);

            birinciSahisKamera.transform.localPosition =
                Vector3.zero;

            birinciSahisKamera.transform.localRotation =
                Quaternion.identity;
        }

        KameraRiginiSabitle();
    }

    private void KameraRiginiSabitle()
    {
        if (birinciSahisRig == null || oyuncuKoku == null)
            return;

        Vector3 hedefWorld;
        bool ikiGozVar = gozlereSabitle && solGozKemigi != null && sagGozKemigi != null;

        Quaternion bakisRotasyonu = Quaternion.Euler(pitch, viewYaw, 0f);
        // Kamera baktigi yonun onune yerlestirilir. Boylece modelin FBX
        // forward ekseni ters olsa bile kamera yeniden kafaya dogru bakmaz.
        Vector3 bakisIleri = bakisRotasyonu * Vector3.forward;

        if (ikiGozVar)
        {
            Vector3 gozOrtasi =
                (GozMerkeziniAl(solGozKemigi) + GozMerkeziniAl(sagGozKemigi)) * 0.5f;

            // Eski componentte serialize edilmis 0.075 gibi degerler kalsa
            // bile kamerayi kafanin disina cikarmayi garanti et.
            float guvenliOneCikma = Mathf.Max(gozlerdenOneCikma, 0.36f);
            hedefWorld = gozOrtasi + bakisIleri * guvenliOneCikma + oyuncuKoku.up * gozYukariDuzeltme;
            kameraAnchorKaynagi = "LeftEye + RightEye";
        }
        else if (gozlereSabitle && kafaKemigi != null)
        {
            Vector3 bakisSag = bakisRotasyonu * Vector3.right;
            float guvenliOneCikma = Mathf.Max(gozlerdenOneCikma, 0.36f);
            hedefWorld = kafaKemigi.position
                + bakisSag * kafaFallbackGozOffset.x
                + oyuncuKoku.up * kafaFallbackGozOffset.y
                + bakisIleri * (kafaFallbackGozOffset.z + guvenliOneCikma);
            kameraAnchorKaynagi = "Head Fallback";
        }
        else
        {
            Vector3 kafaYuksekligi =
                characterController.transform.TransformPoint(
                    characterController.center +
                    Vector3.up *
                    (characterController.height * 0.5f - tepedenAsagiMesafe));

            // Son emniyet: kemik/goz bulunamasa bile kamerayi 45 cm
            // baktigi yone tasir; ense icinde kalmasi fiziksel olarak mumkun olmaz.
            hedefWorld = kafaYuksekligi
                + bakisIleri * Mathf.Max(kameraIleriMesafe, 0.45f)
                + oyuncuKoku.TransformVector(kameraKonumDuzeltmesi);
            kameraAnchorKaynagi = "CharacterController Fallback";
        }

        // Bob kamera anchor'ina local bakis uzayinda eklenir.
        hedefWorld += bakisRotasyonu * currentBobOffset;
        hedefWorld -= oyuncuKoku.up * landingDip;

        if (!eyePositionReady)
        {
            smoothEyeWorldPosition = hedefWorld;
            eyePositionReady = true;
        }
        else
        {
            float takip = 1f - Mathf.Exp(-Mathf.Max(0.1f, gozTakipYumusaklik) * Time.deltaTime);
            smoothEyeWorldPosition = Vector3.Lerp(smoothEyeWorldPosition, hedefWorld, takip);

            // Kosu/head-turn sirasinda yumusatma kamerayi kafanin icinde
            // birakmasin: anchor'dan en fazla 2.5 cm geride kalabilir.
            const float maksimumAnchorGecikmesi = 0.025f;
            Vector3 anchorFarki = hedefWorld - smoothEyeWorldPosition;
            if (anchorFarki.sqrMagnitude > maksimumAnchorGecikmesi * maksimumAnchorGecikmesi)
            {
                smoothEyeWorldPosition =
                    hedefWorld - anchorFarki.normalized * maksimumAnchorGecikmesi;
            }
        }

        birinciSahisRig.position = smoothEyeWorldPosition;

        float finalPitch = pitch + currentMovePitch + landingPitchKick;
        float finalRoll = currentStrafeRoll;
        birinciSahisRig.rotation = Quaternion.Euler(finalPitch, viewYaw, finalRoll);

        if (birinciSahisKamera != null)
        {
            birinciSahisKamera.transform.localPosition = Vector3.zero;
            birinciSahisKamera.transform.localRotation = Quaternion.identity;
        }
    }


    // ============================================================
    // GTA MOUSE LOOK + BODY FOLLOW
    // ============================================================
    private void FareBakisiGTA()
    {
        float fareX =
            Input.GetAxisRaw(
                "Mouse X") *
            fareHassasiyeti;

        float fareY =
            Input.GetAxisRaw(
                "Mouse Y") *
            fareHassasiyeti;

        viewYaw +=
            fareX;

        pitch -=
            fareY;

        pitch =
            Mathf.Clamp(
                pitch,
                asagiBakmaSiniri,
                yukariBakmaSiniri);
    }

    private void GovdeTakibiniUygula()
    {
        float bodyYaw =
            GovdeninYuzYawDegeri();

        float rawHeadYaw =
            Mathf.DeltaAngle(
                bodyYaw,
                viewYaw);

        float inputX =
            Input.GetAxisRaw(
                "Horizontal");

        float inputZ =
            Input.GetAxisRaw(
                "Vertical");

        bool moving =
            Mathf.Abs(
                inputX) >
                0.05f ||
            Mathf.Abs(
                inputZ) >
                0.05f ||
            yatayHiz >
                0.20f;

        float allowedHeadYaw =
            moving
                ? hareketKafaYawLimiti
                : maksimumSerbestKafaYaw;

        bool bodyShouldFollow =
            moving ||
            !idleSerbestBakis ||
            Mathf.Abs(
                rawHeadYaw) >
                allowedHeadYaw;

        if (bodyShouldFollow)
        {
            float desiredBodyYaw =
                viewYaw;

            // Idle free-look'ta body hemen tamamen kameraya donmesin.
            // Sadece kafa limitinin disinda kalan aciyi kapatsin.
            if (!moving &&
                idleSerbestBakis)
            {
                desiredBodyYaw =
                    viewYaw -
                    Mathf.Sign(
                        rawHeadYaw) *
                    allowedHeadYaw;
            }
            else if (moving)
            {
                desiredBodyYaw =
                    viewYaw -
                    Mathf.Clamp(
                        rawHeadYaw,
                        -allowedHeadYaw,
                        allowedHeadYaw);
            }

            float followSpeed =
                moving
                    ? hareketGovdeDonusHizi
                    : govdeDonusHizi;

            float newBodyYaw =
                Mathf.LerpAngle(
                    bodyYaw,
                    desiredBodyYaw,
                    1f -
                    Mathf.Exp(
                        -followSpeed *
                        Time.deltaTime));

            oyuncuKoku.rotation =
                Quaternion.Euler(
                    0f,
                    newBodyYaw -
                    (modelYuzYonuHazir ? modelYuzYawOffset : 0f),
                    0f);

            bodyYaw =
                newBodyYaw;
        }

        relativeHeadYaw =
            Mathf.DeltaAngle(
                bodyYaw,
                viewYaw);

        relativeHeadYaw =
            Mathf.Clamp(
                relativeHeadYaw,
                -maksimumSerbestKafaYaw,
                maksimumSerbestKafaYaw);

        kafaYaw =
            relativeHeadYaw;

        govdeYaw =
            bodyYaw;

        kameraYaw =
            viewYaw;
    }

    // ============================================================
    // ADDITIVE HEAD / NECK / CHEST
    // ============================================================
    private void KemikBakisiniUygula()
    {
        if (animator == null ||
            !animator.enabled)
            return;

        float targetYaw =
            relativeHeadYaw *
            kemikYawYon;

        float targetPitch =
            pitch *
            kemikPitchYon;

        float blend =
            1f -
            Mathf.Exp(
                -kemikBakisYumusaklik *
                Time.deltaTime);

        smoothBoneYaw =
            Mathf.Lerp(
                smoothBoneYaw,
                targetYaw,
                blend);

        smoothBonePitch =
            Mathf.Lerp(
                smoothBonePitch,
                targetPitch,
                blend);

        Vector3 worldUp =
            oyuncuKoku.up;

        Vector3 worldRight =
            oyuncuKoku.right;

        // Parent'tan child'a dagitim.
        // Chest -> Neck -> Head.
        KemigeAdditiveBakisUygula(
            gogusKemigi,
            smoothBoneYaw *
            gogusYawAgirligi,
            smoothBonePitch *
            gogusPitchAgirligi,
            worldUp,
            worldRight);

        KemigeAdditiveBakisUygula(
            boyunKemigi,
            smoothBoneYaw *
            boyunYawAgirligi,
            smoothBonePitch *
            boyunPitchAgirligi,
            worldUp,
            worldRight);

        KemigeAdditiveBakisUygula(
            kafaKemigi,
            smoothBoneYaw *
            kafaYawAgirligi,
            smoothBonePitch *
            kafaPitchAgirligi,
            worldUp,
            worldRight);
    }

    private static void KemigeAdditiveBakisUygula(
        Transform bone,
        float yawAmount,
        float pitchAmount,
        Vector3 worldUp,
        Vector3 worldRight)
    {
        if (bone == null)
            return;

        Quaternion yawRotation =
            Quaternion.AngleAxis(
                yawAmount,
                worldUp);

        Quaternion pitchRotation =
            Quaternion.AngleAxis(
                pitchAmount,
                worldRight);

        bone.rotation =
            yawRotation *
            pitchRotation *
            bone.rotation;
    }

    // ============================================================
    // MOVEMENT / HEAD BOB / TILT
    // ============================================================
    private void HareketBilgisiniGuncelle()
    {
        Vector3 velocity =
            characterController.velocity;

        Vector3 planar =
            Vector3.ProjectOnPlane(
                velocity,
                Vector3.up);

        yatayHiz =
            planar.magnitude;

        hareketVar =
            yatayHiz >
            0.12f ||
            Mathf.Abs(
                Input.GetAxisRaw(
                    "Horizontal")) >
                0.05f ||
            Mathf.Abs(
                Input.GetAxisRaw(
                    "Vertical")) >
                0.05f;

        yerde =
            characterController.isGrounded;

        animatorAktif =
            animator != null &&
            animator.enabled;
    }

    private void HeadBobVeTiltGuncelle()
    {
        float dt =
            Mathf.Max(
                Time.deltaTime,
                0.0001f);

        Vector3 targetBob =
            Vector3.zero;

        bool running =
            Input.GetKey(
                KeyCode.LeftShift) &&
            hareketVar;

        if (headBobAktif &&
            hareketVar &&
            characterController.isGrounded)
        {
            float frequency =
                running
                    ? kosmaBobFrekans
                    : yurumeBobFrekans;

            float height =
                running
                    ? kosmaBobYukseklik
                    : yurumeBobYukseklik;

            float speedFactor =
                Mathf.Clamp01(
                    yatayHiz /
                    3.5f);

            speedFactor =
                Mathf.Max(
                    speedFactor,
                    0.35f);

            bobTime +=
                dt *
                frequency;

            float vertical =
                Mathf.Sin(
                    bobTime * 2f) *
                height *
                speedFactor;

            float horizontal =
                Mathf.Sin(
                    bobTime) *
                bobYanMiktar *
                speedFactor;

            targetBob =
                new Vector3(
                    horizontal,
                    vertical,
                    0f);
        }
        else
        {
            bobTime = 0f;
        }

        currentBobOffset =
            Vector3.Lerp(
                currentBobOffset,
                targetBob,
                1f -
                Mathf.Exp(
                    -bobYumusaklik *
                    dt));

        float targetRoll = 0f;
        float targetMovePitch = 0f;

        if (hareketTiltAktif)
        {
            float horizontal =
                Input.GetAxisRaw(
                    "Horizontal");

            float vertical =
                Input.GetAxisRaw(
                    "Vertical");

            targetRoll =
                -horizontal *
                maksimumStrafeRoll;

            targetMovePitch =
                -vertical *
                maksimumHareketPitch;
        }

        float tiltBlend =
            1f -
            Mathf.Exp(
                -tiltYumusaklik *
                dt);

        currentStrafeRoll =
            Mathf.Lerp(
                currentStrafeRoll,
                targetRoll,
                tiltBlend);

        currentMovePitch =
            Mathf.Lerp(
                currentMovePitch,
                targetMovePitch,
                tiltBlend);
    }

    // ============================================================
    // LANDING
    // ============================================================
    private void LandingKickGuncelle()
    {
        bool nowGrounded =
            characterController.isGrounded;

        float verticalVelocity =
            characterController.velocity.y;

        if (landingKickAktif &&
            !oncekiFrameYerde &&
            nowGrounded &&
            oncekiDikeyHiz <
                -landingMinimumDususHizi)
        {
            float impact01 =
                Mathf.InverseLerp(
                    landingMinimumDususHizi,
                    14f,
                    Mathf.Abs(
                        oncekiDikeyHiz));

            landingDip =
                maksimumLandingDip *
                impact01;

            landingPitchKick =
                maksimumLandingPitch *
                impact01;
        }

        landingDip =
            Mathf.Lerp(
                landingDip,
                0f,
                1f -
                Mathf.Exp(
                    -landingToparlama *
                    Time.deltaTime));

        landingPitchKick =
            Mathf.Lerp(
                landingPitchKick,
                0f,
                1f -
                Mathf.Exp(
                    -landingToparlama *
                    Time.deltaTime));

        oncekiFrameYerde =
            nowGrounded;

        oncekiDikeyHiz =
            verticalVelocity;
    }

    // ============================================================
    // FOV
    // ============================================================
    private void FovGuncelle()
    {
        if (!sprintFovAktif ||
            birinciSahisKamera == null)
            return;

        bool running =
            Input.GetKey(
                KeyCode.LeftShift) &&
            hareketVar &&
            characterController.isGrounded;

        float target =
            running
                ? kosmaFov
                : normalFov;

        birinciSahisKamera.fieldOfView =
            Mathf.Lerp(
                birinciSahisKamera.fieldOfView,
                target,
                1f -
                Mathf.Exp(
                    -fovYumusaklik *
                    Time.deltaTime));
    }

    // ============================================================
    // MODE SWITCH
    // ============================================================
    private void ModDegistir(
        bool birinciSahisaGec,
        bool ilkKurulum)
    {
        birinciSahisAktif =
            birinciSahisaGec;

        if (birinciSahisAktif)
        {
            float bodyYaw =
                GovdeninYuzYawDegeri();

            if (!gecisteBakisiKoru ||
                ilkKurulum)
            {
                viewYaw =
                    bodyYaw;

                pitch = 0f;
            }
            else
            {
                // Ucuncu sahistan FPS'e gecerken root yonunden basla.
                // Mevcut body direction korunur.
                viewYaw =
                    bodyYaw;
            }

            relativeHeadYaw = 0f;
            smoothBoneYaw = 0f;
            smoothBonePitch = 0f;
            eyePositionReady = false;

            if (birinciSahisKamera != null)
            {
                eskiFov =
                    birinciSahisKamera.fieldOfView;

                eskiNearClip =
                    birinciSahisKamera.nearClipPlane;

                birinciSahisKamera.nearClipPlane =
                    fpsNearClip;

                if (sprintFovAktif)
                {
                    birinciSahisKamera.fieldOfView =
                        normalFov;
                }
            }
        }
        else
        {
            smoothBoneYaw = 0f;
            smoothBonePitch = 0f;

            if (birinciSahisKamera != null)
            {
                birinciSahisKamera.nearClipPlane =
                    eskiNearClip;

                if (!sprintFovAktif)
                {
                    birinciSahisKamera.fieldOfView =
                        eskiFov;
                }
            }
        }

        // KRITIK:
        // karakterHareketi.enabled DEGISTIRILMEZ.
        // animator.enabled DEGISTIRILMEZ.
        KameraDurumlariniAyarla();

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }

    private void KameraDurumlariniAyarla()
    {
        // Root veya GameObject kapatmak ayni hiyerarsideki Animator,
        // hareket scripti ve yardimci sistemleri de kapatabilir.
        // Bu nedenle yalnizca Camera/AudioListener componentleri degisir.
        if (ucuncuSahisKameraRoot != null &&
            !ucuncuSahisKameraRoot.activeSelf)
            ucuncuSahisKameraRoot.SetActive(true);

        if (ucuncuSahisKamera != null &&
            !ucuncuSahisKamera.gameObject.activeSelf)
            ucuncuSahisKamera.gameObject.SetActive(true);

        if (birinciSahisKamera != null &&
            !birinciSahisKamera.gameObject.activeSelf)
            birinciSahisKamera.gameObject.SetActive(true);

        if (ucuncuSahisKamera != null)
            ucuncuSahisKamera.enabled = !birinciSahisAktif;

        if (birinciSahisKamera != null)
            birinciSahisKamera.enabled = birinciSahisAktif;

        AudioListener ucuncuListener =
            ucuncuSahisKamera != null
                ? ucuncuSahisKamera.GetComponent<AudioListener>()
                : null;

        AudioListener birinciListener =
            birinciSahisKamera != null
                ? birinciSahisKamera.GetComponent<AudioListener>()
                : null;

        if (ucuncuListener != null)
            ucuncuListener.enabled = !birinciSahisAktif;

        if (birinciListener != null)
            birinciListener.enabled = birinciSahisAktif;

        if (ucuncuSahisKamera != null)
        {
            ucuncuSahisKamera.tag =
                birinciSahisAktif
                    ? "Untagged"
                    : "MainCamera";
        }

        if (birinciSahisKamera != null)
        {
            birinciSahisKamera.tag =
                birinciSahisAktif
                    ? "MainCamera"
                    : "Untagged";
        }
    }

    // ============================================================
    // DEBUG
    // ============================================================
    private void DebugGuncelle()
    {
        animatorAktif =
            animator != null &&
            animator.enabled;

        humanoidAnimator =
            animator != null &&
            animator.avatar != null &&
            animator.avatar.isValid &&
            animator.avatar.isHuman;

        DebugKemikAdlari();
    }

    private void OnDisable()
    {
        if (birinciSahisKamera != null)
        {
            birinciSahisKamera.nearClipPlane =
                eskiNearClip;

            birinciSahisKamera.fieldOfView =
                eskiFov;
        }

    }
}
