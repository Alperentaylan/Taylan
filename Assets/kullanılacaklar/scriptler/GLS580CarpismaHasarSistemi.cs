using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// TEK DOSYA / TEK ANA CLASS.
// Eski GLS580CarpismaEngelleyici ve GLS580HasarKirDarbe scriptlerinin yerini alir.
[DefaultExecutionOrder(-24000)]
public class GLS580CarpismaHasarSistemi : MonoBehaviour
{
    [Header("ARAC - OTOMATIK BULUR")]
    public Transform vehicleRoot;
    public Transform visualRoot;
    public Rigidbody vehicleRigidbody;

    [Tooltip("Bos birakabilirsin. GLS580BasitSistem otomatik bulunur.")]
    public MonoBehaviour basitSistem;

    // ============================================================
    // CARPISMA ENGELLEME
    // ============================================================
    [Header("1) CARPISMA ENGELLEME")]
    public bool carpismaEngellemeAktif = true;

    [Tooltip("Duvara yaklasirken birakilan tampon mesafe.")]
    public float guvenlikPayi = 0.055f;

    [Tooltip("Bir fizik karesinde gidilecek mesafeye ek tarama payi.")]
    public float ekstraTaramaMesafesi = 0.10f;

    public float minimumKontrolHiziKmh = 1f;
    public LayerMask engelMask = ~0;
    public bool triggerlariYoksay = true;

    [Header("YOL / KALDIRIM FILTRESI")]
    [Tooltip("Yol, rampa, kasis gibi yukariya bakan yuzeyleri duvar sayma.")]
    public bool zeminVeRampayiYoksay = true;

    [Range(0f, 1f)]
    public float zeminNormalEsigi = 0.45f;

    [Tooltip("Alcak kaldirim kenarlarini duvar gibi durdurma.")]
    public bool dusukEngelleriYoksay = true;

    [Tooltip("Bu yuksekligin altindaki engeller asilebilir kabul edilir.")]
    public float maksimumAsilabilirYukseklik = 0.32f;

    public float ustEngelKontrolYaricapi = 0.05f;
    public float ustEngelKontrolMesafesi = 0.52f;
    public float ustEngelKontrolGeriPayi = 0.14f;

    [Header("RIGIDBODY")]
    public bool rigidbodyAyarlariniGuclendir = true;
    public bool continuousDynamic = true;
    public bool interpolation = true;

    // ============================================================
    // HASAR
    // ============================================================
    [Header("2) HASAR")]
    public bool hasarAktif = true;

    [Tooltip("Bu hiz altindaki carpmalar hasar vermez. m/s")]
    public float minimumHasarHizi = 3.5f;

    [Tooltip("Bu hizda tek carpma hasari maksimuma ulasir. m/s")]
    public float maksimumHasarHizi = 22f;

    public float maksimumTekCarpmaHasari = 32f;
    public float maksimumSaglik = 100f;

    [SerializeField]
    private float saglik = 100f;

    [Tooltip("Sweep + fizik ayni carpmayi iki kez bildirirse engeller.")]
    public float ayniCarpmaTekrarKorumaSuresi = 0.22f;

    // ============================================================
    // MESH EZILMESI
    // ============================================================
    [Header("3) MESH EZILMESI")]
    public bool meshEzilmesiAktif = true;

    [Tooltip("Aciksa eski Inspector degerleri dusuk kalsa bile gozle gorulur hasar icin guclu minimum degerler kullanilir.")]
    public bool gucluHasarModu = true;

    [Tooltip("WORLD metre cinsinden ezilme yaricapi. Model scale 0.01/100 olsa da dogru calisir.")]
    public float ezilmeYaricapi = 0.75f;

    [Tooltip("WORLD metre cinsinden en sert tek carpma ezilme derinligi.")]
    public float maksimumEzilmeDerinligi = 0.18f;

    [Tooltip("WORLD metre cinsinden bir vertex'in toplam kalici sapma limiti.")]
    public float maksimumKaliciVertexEzilmesi = 0.32f;

    [Tooltip("Aciksa Inspector'daki liste eksik olsa bile VisualRoot altindaki tum MeshFilter'lari da toplar.")]
    public bool otomatikTumMeshleriTopla = true;

    [Tooltip("Elle de mesh verebilirsin; otomatik bulunanlarla birlestirilir.")]
    public MeshFilter[] hasarMeshleri;

    public string[] hasarParcaAdlari = new string[]
    {
        "Static_Body",
        "Door_FL",
        "Door_FR",
        "Door_RL",
        "Door_RR",
        "Trunk"
    };

    // ============================================================
    // CARPMA GOVDE TEPKISI
    // ============================================================
    [Header("4) CARPMA GOVDE TEPKISI")]
    [Tooltip("Yuksek hizli carpmalarda govde fizik hissi verir. Rigidbody X/Z rotasyonu kilitli oldugu icin visualRoot uzerinden katman olarak uygulanir.")]
    public bool carpmaGovdeTepkisiAktif = true;

    [Tooltip("Bu hizdan sonra rear-lift / govde savrulma etkisi belirginlesir.")]
    public float yuksekHizCarpmaEsigiKmh = 45f;

    [Tooltip("Cok sert ON carpmada arka tekerleri kaldiran maksimum nose-down pitch.")]
    [Range(0f, 28f)]
    public float maksimumOnCarpmaPitch = 17f;

    [Tooltip("Cok sert ARKA carpmada on tarafi kaldiran maksimum pitch.")]
    [Range(0f, 22f)]
    public float maksimumArkaCarpmaPitch = 11f;

    [Tooltip("Cok sert YAN carpmada maksimum govde roll.")]
    [Range(0f, 20f)]
    public float maksimumYanCarpmaRoll = 9f;

    [Tooltip("Pitch sirasinda araci biraz yukari alir; tamponun zemine gomulmesini azaltir.")]
    public float maksimumDarbeYukariOffset = 0.075f;

    [Tooltip("Darbe govde saliniminin suresi.")]
    public float darbeGovdeSuresi = 0.72f;

    [Tooltip("Carpisma sonrasi ikinci kucuk sekme miktari.")]
    [Range(0f, 0.65f)]
    public float darbeSekmeOrani = 0.28f;

    // ============================================================
    // PARCALANMA / DOKULME
    // ============================================================
    [Header("5) KOZMETIK DARBE DETAYI - ANA MESH KOPMAZ")]
    public bool parcalanmaAktif = true;

    [Tooltip("Carpma bu parcalardan birine yakin ve yeterince sertse parca kopabilir.")]
    public string[] kopabilirParcaAdlari = new string[]
    {
        "Door_FL",
        "Door_FR",
        "Door_RL",
        "Door_RR",
        "Trunk",
        "Hood",
        "Bonnet",
        "Bumper_Front",
        "Bumper_Rear"
    };

    [Tooltip("Carpma noktasinin bu mesafesindeki en yakin parca hasar toplar.")]
    public float parcaEtkiMesafesi = 1.35f;

    [Tooltip("Parcanin kopmasi icin biriken hasar.")]
    public float parcaKopmaHasarEsigi = 62f;

    [Tooltip("Tek carpmada parca hasarina eklenecek maksimum miktar.")]
    public float tekCarpmaParcaHasari = 58f;

    [Tooltip("Eski ayar. Bu surumde ANA MESH veya kapilar renderer olarak koparilmaz/gizlenmez. Sadece kozmetik debris kullanilir.")]
    public bool sifirSagliktaTamDagil = false;

    [Tooltip("Saglik bu degerin altina inince camlar kirilir/gizlenir.")]
    [Range(0f, 100f)]
    public float camKirilmaSaglikEsigi = 20f;

    [Tooltip("Saglik bu degerin altina inince arac Light componentleri bozulur.")]
    [Range(0f, 100f)]
    public float farBozulmaSaglikEsigi = 28f;

    [Tooltip("Sert carpmalarda yere dokulen kucuk parca sayisi.")]
    public int sertCarpmaDebrisSayisi = 7;

    [Tooltip("Arac tamamen bittiginde ekstra dokulecek parca sayisi.")]
    public int sifirSaglikDebrisSayisi = 22;

    public float debrisOmru = 9f;
    public float debrisItmeGucu = 4.5f;
    public float kopanParcaItmeGucu = 6.0f;

    // ============================================================
    // KAMERA DARBE
    // ============================================================
    [Header("6) KAMERA DARBE")]
    public bool kameraDarbeAktif = true;

    [Range(0f, 2f)]
    public float kameraDarbeCarpani = 1f;

    [Tooltip("Bos birakirsan arac kameralarini otomatik bulur.")]
    public Camera[] darbeKameralari;

    // ============================================================
    // KIRLENME
    // ============================================================
    [Header("7) KIRLENME")]
    public bool kirlenmeAktif = true;

    [Range(0f, 0.25f)]
    public float kirlenme100Metrede = 0.075f;

    public float kirlenmeMinimumHizKmh = 5f;

    [Range(0f, 1f)]
    [SerializeField]
    private float kirOrani = 0f;

    public Color kirRengi =
        new Color(
            0.30f,
            0.19f,
            0.075f,
            1f);

    [Range(0f, 1f)]
    public float kirRenkEtkisi = 0.82f;

    [Range(0f, 1f)]
    public float kirMatlikEtkisi = 0.88f;

    public Color hasarRengi =
        new Color(
            0.10f,
            0.10f,
            0.10f,
            1f);

    [Range(0f, 0.5f)]
    public float hasarRenkEtkisi = 0.32f;

    [Tooltip("Bos birakirsan body/door/trunk rendererlari otomatik bulunur.")]
    public Renderer[] kirRenderers;

    [Tooltip("Inspector'daki liste eksik olsa bile VisualRoot altindaki tum uygun rendererlari kirlet.")]
    public bool otomatikTumRendererlariKirlet = true;

    [Tooltip("Sert carpmada toz/kir de artsin.")]
    [Range(0f, 0.2f)]
    public float carpismadaKirArtisi = 0.035f;

    // ============================================================
    // SES
    // ============================================================
    [Header("8) CARPISMA SESI - ISTEGE BAGLI")]
    public AudioClip hafifCarpismaSesi;
    public AudioClip sertCarpismaSesi;

    [Range(0f, 1f)]
    public float carpismaSesSeviyesi = 0.9f;

    [Range(0f, 1f)]
    public float sertSesEsigi = 0.52f;

    // ============================================================
    // DEBUG
    // ============================================================
    [Header("DEBUG - PLAY MODDA")]
    [SerializeField] private bool playerInside;
    [SerializeField] private bool busy;
    [SerializeField] private float currentSpeedKmh;

    [SerializeField] private bool engelBulundu;
    [SerializeField] private string sonEngel = "-";
    [SerializeField] private float sonEngelMesafesi;
    [SerializeField] private bool sonHitZeminVeyaRampa;
    [SerializeField] private bool sonHitDusukEngel;
    [SerializeField] private string sonYoksayilanEngel = "-";

    [SerializeField] private float sonCarpmaHizi;
    [SerializeField] private float sonCarpmaSiddeti;
    [SerializeField] private string sonHasarKaynagi = "-";
    [SerializeField] private string sonCarpilanObje = "-";

    [SerializeField] private float toplamSurulenMetre;
    [SerializeField] private int aktifHasarMeshSayisi;
    [SerializeField] private int aktifKirRendererSayisi;
    [SerializeField] private int bulunanKopabilirParcaSayisi;
    [SerializeField] private int kopmusParcaSayisi;
    [SerializeField] private bool camlarKirildi;
    [SerializeField] private bool farlarBozuldu;
    [SerializeField] private float sonDarbePitch;
    [SerializeField] private float sonDarbeRoll;
    [SerializeField] private string sonDarbeYonu = "-";
    [SerializeField] private string bulunanDriveRoot = "-";
    [SerializeField] private string bulunanVisualRoot = "-";

    public float Saglik
    {
        get { return saglik; }
    }

    public float KirOrani
    {
        get { return kirOrani; }
    }

    public float HasarOrani
    {
        get
        {
            return
                1f -
                Mathf.Clamp01(
                    saglik /
                    Mathf.Max(
                        1f,
                        maksimumSaglik));
        }
    }

    // ============================================================
    // REFLECTION
    // ============================================================
    private Type basitType;
    private FieldInfo fPlayerInside;
    private FieldInfo fBusy;
    private FieldInfo fDriveRoot;
    private FieldInfo fCurrentDriveSpeed;
    private FieldInfo fCurrentSteerAngle;
    private FieldInfo fWheelBase;

    // ============================================================
    // COLLISION STATE
    // ============================================================
    private bool previousFrameBlocking;
    private Collider previousBlockingCollider;

    private float lastImpactTime = -999f;
    private Collider lastImpactCollider;

    // ============================================================
    // FALLBACK SPEED / DISTANCE
    // ============================================================
    private Vector3 lastVehiclePosition;
    private bool lastPositionReady;
    private float fallbackSpeedKmh;

    // ============================================================
    // AUDIO
    // ============================================================
    private AudioSource crashAudio;

    // ============================================================
    // MESH DAMAGE DATA
    // ============================================================
    private class MeshDamageData
    {
        public MeshFilter filter;
        public Mesh runtimeMesh;
        public Vector3[] originalVertices;
        public Vector3[] currentVertices;
    }

    private readonly List<MeshDamageData> meshDamageData =
        new List<MeshDamageData>();

    // ============================================================
    // MATERIAL DATA
    // ============================================================
    private class MaterialSlotData
    {
        public Renderer renderer;
        public int materialIndex;

        public string colorProperty;
        public Color originalColor;

        public string smoothnessProperty;
        public float originalSmoothness;

        public MaterialPropertyBlock block;
    }

    private readonly List<MaterialSlotData> materialSlots =
        new List<MaterialSlotData>();

    private class BreakablePartData
    {
        public Transform transform;
        public Transform originalParent;
        public Vector3 originalLocalPosition;
        public Quaternion originalLocalRotation;
        public Vector3 originalLocalScale;

        public Renderer[] renderers;
        public Collider[] colliders;
        public bool[] originalColliderEnabled;

        public Rigidbody addedRigidbody;
        public float damage;
        public bool detached;
    }

    private readonly List<BreakablePartData> breakableParts =
        new List<BreakablePartData>();

    private Renderer[] glassRenderers;
    private bool[] glassOriginalEnabled;

    private Light[] vehicleLights;
    private bool[] lightOriginalEnabled;

    private bool deathBreakupApplied;
    private Material debrisMaterial;

    private GLS580KameraDarbeKatmaniYeni[] cameraLayers;
    private GLS580CarpmaGovdeTepkisi crashBodyLayer;

    private void Awake()
    {
        saglik =
            Mathf.Clamp(
                saglik <= 0f
                    ? maksimumSaglik
                    : saglik,
                0f,
                maksimumSaglik);

        OtomatikBul();
        RigidbodyAyarla();
        MeshHasarSisteminiHazirla();
        ParcalanmaSisteminiHazirla();
        CarpmaGovdeSisteminiHazirla();
        KirSisteminiHazirla();
        KameraSisteminiHazirla();
        AudioHazirla();
        MesafeBaslat();
    }

    private void Start()
    {
        OtomatikBul();
        RigidbodyAyarla();
        MeshHasarSisteminiHazirla();
        ParcalanmaSisteminiHazirla();
        CarpmaGovdeSisteminiHazirla();
        KirSisteminiHazirla();
        KameraSisteminiHazirla();
        AudioHazirla();
        MesafeBaslat();

        GorunumuGuncelle();
    }

    private void Update()
    {
        OtomatikBul();
        DurumuOku();
        MesafeVeKirGuncelle();
        GorunumuGuncelle();
    }

    // ============================================================
    // BASIT SISTEMDEN ONCE ENGEL KONTROLU
    // ============================================================
    private void FixedUpdate()
    {
        if (!carpismaEngellemeAktif)
            return;

        OtomatikBul();
        DurumuOku();

        if (vehicleRoot == null ||
            vehicleRigidbody == null ||
            basitSistem == null ||
            fCurrentDriveSpeed == null)
        {
            return;
        }

        RigidbodyAyarla();

        float speedMps =
            CurrentDriveSpeedOku();

        currentSpeedKmh =
            Mathf.Abs(
                speedMps) *
            3.6f;

        engelBulundu = false;
        sonEngel = "-";
        sonEngelMesafesi = 0f;
        sonHitZeminVeyaRampa = false;
        sonHitDusukEngel = false;
        sonYoksayilanEngel = "-";

        if (currentSpeedKmh <
            minimumKontrolHiziKmh)
        {
            previousFrameBlocking = false;
            previousBlockingCollider = null;
            return;
        }

        float dt =
            Mathf.Max(
                Time.fixedDeltaTime,
                0.0001f);

        float speedAbs =
            Mathf.Abs(
                speedMps);

        float sign =
            speedMps >= 0f
                ? 1f
                : -1f;

        Vector3 up =
            vehicleRoot.up.sqrMagnitude >
            0.001f
                ? vehicleRoot.up.normalized
                : Vector3.up;

        Vector3 predictedForward =
            TahminiHareketYonu(
                speedMps,
                dt,
                up);

        Vector3 direction =
            sign > 0f
                ? predictedForward
                : -predictedForward;

        if (direction.sqrMagnitude <
            0.001f)
            return;

        direction.Normalize();

        float requestedDistance =
            speedAbs *
            dt;

        float sweepDistance =
            requestedDistance +
            Mathf.Max(
                0f,
                ekstraTaramaMesafesi) +
            Mathf.Max(
                0f,
                guvenlikPayi);

        RaycastHit[] hits =
            vehicleRigidbody.SweepTestAll(
                direction,
                sweepDistance,
                triggerlariYoksay
                    ? QueryTriggerInteraction.Ignore
                    : QueryTriggerInteraction.Collide);

        bool found =
            EnYakinGecerliHit(
                hits,
                direction,
                up,
                out RaycastHit nearestHit);

        if (!found)
        {
            previousFrameBlocking = false;
            previousBlockingCollider = null;
            return;
        }

        engelBulundu = true;

        sonEngel =
            nearestHit.collider != null
                ? nearestHit.collider.name
                : "UNKNOWN";

        sonEngelMesafesi =
            nearestHit.distance;

        Collider blockingCollider =
            nearestHit.collider;

        bool firstImpactFrame =
            !previousFrameBlocking ||
            blockingCollider !=
            previousBlockingCollider;

        if (firstImpactFrame &&
            nearestHit.distance <=
                requestedDistance +
                guvenlikPayi +
                0.03f)
        {
            HasarBildir(
                nearestHit.point,
                nearestHit.normal,
                speedAbs,
                blockingCollider,
                "Sweep Engelleyici");
        }

        previousFrameBlocking = true;
        previousBlockingCollider =
            blockingCollider;

        float allowedDistance =
            Mathf.Max(
                0f,
                nearestHit.distance -
                Mathf.Max(
                    0f,
                    guvenlikPayi));

        float allowedSpeed =
            allowedDistance /
            dt;

        allowedSpeed =
            Mathf.Min(
                allowedSpeed,
                speedAbs);

        CurrentDriveSpeedYaz(
            allowedSpeed *
            sign);

        if (allowedDistance <=
            guvenlikPayi *
            0.35f)
        {
            CurrentDriveSpeedYaz(
                0f);
        }
    }

    // ============================================================
    // NORMAL PHYSICS COLLISION - YEDEK / EK HASAR
    // ============================================================
    private void OnCollisionEnter(
        Collision collision)
    {
        if (collision == null)
            return;

        float impactSpeed =
            collision.relativeVelocity.magnitude;

        Vector3 point =
            vehicleRoot != null
                ? vehicleRoot.position
                : transform.position;

        Vector3 normal =
            Vector3.up;

        Collider other = null;

        if (collision.contactCount > 0)
        {
            ContactPoint contact =
                collision.GetContact(0);

            point =
                contact.point;

            normal =
                contact.normal;

            other =
                contact.otherCollider;
        }

        HasarBildir(
            point,
            normal,
            impactSpeed,
            other,
            "Physics OnCollisionEnter");
    }

    // ============================================================
    // HIT FILTER
    // ============================================================
    private bool EnYakinGecerliHit(
        RaycastHit[] hits,
        Vector3 direction,
        Vector3 up,
        out RaycastHit nearest)
    {
        nearest =
            new RaycastHit();

        bool found = false;

        float nearestDistance =
            float.MaxValue;

        if (hits == null)
            return false;

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            RaycastHit hit =
                hits[i];

            Collider col =
                hit.collider;

            if (!GecerliHariciCollider(
                    col))
            {
                continue;
            }

            int layerBit =
                1 <<
                col.gameObject.layer;

            if ((engelMask.value &
                 layerBit) == 0)
            {
                continue;
            }

            Vector3 normal =
                hit.normal.sqrMagnitude >
                0.001f
                    ? hit.normal.normalized
                    : Vector3.zero;

            // Yol, rampa, kasis ust yuzeyi.
            if (zeminVeRampayiYoksay &&
                normal.sqrMagnitude >
                0.001f)
            {
                float upDot =
                    Vector3.Dot(
                        normal,
                        up);

                if (upDot >=
                    zeminNormalEsigi)
                {
                    sonHitZeminVeyaRampa =
                        true;

                    sonYoksayilanEngel =
                        col.name;

                    continue;
                }
            }

            // Kaldirim kenari / alcak dik engel.
            if (dusukEngelleriYoksay &&
                !UstundeGercekEngelVar(
                    hit,
                    direction,
                    up))
            {
                sonHitDusukEngel =
                    true;

                sonYoksayilanEngel =
                    col.name;

                continue;
            }

            if (hit.distance <
                nearestDistance)
            {
                nearestDistance =
                    hit.distance;

                nearest =
                    hit;

                found =
                    true;
            }
        }

        return found;
    }

    private bool UstundeGercekEngelVar(
        RaycastHit lowHit,
        Vector3 direction,
        Vector3 up)
    {
        Vector3 castDirection =
            direction.sqrMagnitude >
            0.001f
                ? direction.normalized
                : vehicleRoot.forward;

        Vector3 origin =
            lowHit.point +
            up *
            Mathf.Max(
                0.05f,
                maksimumAsilabilirYukseklik) -
            castDirection *
            Mathf.Max(
                0f,
                ustEngelKontrolGeriPayi);

        float distance =
            Mathf.Max(
                0.10f,
                ustEngelKontrolMesafesi);

        RaycastHit[] upperHits =
            Physics.SphereCastAll(
                origin,
                Mathf.Max(
                    0.01f,
                    ustEngelKontrolYaricapi),
                castDirection,
                distance,
                engelMask,
                triggerlariYoksay
                    ? QueryTriggerInteraction.Ignore
                    : QueryTriggerInteraction.Collide);

        for (int i = 0;
             i < upperHits.Length;
             i++)
        {
            Collider col =
                upperHits[i].collider;

            if (!GecerliHariciCollider(
                    col))
            {
                continue;
            }

            Vector3 n =
                upperHits[i].normal.sqrMagnitude >
                0.001f
                    ? upperHits[i].normal.normalized
                    : Vector3.zero;

            // Yukaridaki hit yine yol/rampa ise duvar degil.
            if (n.sqrMagnitude >
                    0.001f &&
                Vector3.Dot(
                    n,
                    up) >=
                zeminNormalEsigi)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool GecerliHariciCollider(
        Collider col)
    {
        if (col == null ||
            !col.enabled)
            return false;

        if (triggerlariYoksay &&
            col.isTrigger)
            return false;

        if (vehicleRoot != null &&
            (
                col.transform ==
                vehicleRoot ||
                col.transform.IsChildOf(
                    vehicleRoot)
            ))
        {
            return false;
        }

        return true;
    }

    // ============================================================
    // HASAR
    // ============================================================
    private void HasarBildir(
        Vector3 impactPointWorld,
        Vector3 impactNormalWorld,
        float impactSpeedMps,
        Collider otherCollider,
        string sourceName)
    {
        if (!hasarAktif)
            return;

        float speed =
            Mathf.Abs(
                impactSpeedMps);

        if (speed <
            minimumHasarHizi)
            return;

        Collider colliderRef =
            otherCollider;

        if (Time.time -
                lastImpactTime <
            ayniCarpmaTekrarKorumaSuresi &&
            colliderRef ==
            lastImpactCollider)
        {
            return;
        }

        lastImpactTime =
            Time.time;

        lastImpactCollider =
            colliderRef;

        float severity =
            Mathf.InverseLerp(
                minimumHasarHizi,
                maksimumHasarHizi,
                speed);

        severity =
            Mathf.Clamp01(
                severity);

        sonCarpmaHizi =
            speed;

        sonCarpmaSiddeti =
            severity;

        sonHasarKaynagi =
            sourceName;

        sonCarpilanObje =
            otherCollider != null
                ? otherCollider.name
                : "UNKNOWN";

        float damage =
            maksimumTekCarpmaHasari *
            Mathf.Lerp(
                0.18f,
                1f,
                severity) *
            severity;

        saglik =
            Mathf.Clamp(
                saglik -
                damage,
                0f,
                maksimumSaglik);

        if (meshEzilmesiAktif)
        {
            CarpismaMeshEzTekNokta(
                impactPointWorld,
                impactNormalWorld,
                severity);
        }

        if (carpmaGovdeTepkisiAktif)
        {
            CarpmaGovdeTepkisiVer(
                impactPointWorld,
                severity,
                speed);
        }

        if (kirlenmeAktif)
        {
            kirOrani =
                Mathf.Clamp01(
                    kirOrani +
                    carpismadaKirArtisi *
                    Mathf.Lerp(
                        0.35f,
                        1f,
                        severity));
        }

        if (parcalanmaAktif)
        {
            CarpismaParcalanmaUygula(
                impactPointWorld,
                impactNormalWorld,
                severity,
                speed);
        }

        CarpismaSesiCal(
            severity);

        if (kameraDarbeAktif &&
            playerInside)
        {
            KameraDarbeVer(
                severity *
                kameraDarbeCarpani);
        }

        GorunumuGuncelle();
    }

    private void CarpismaMeshEzTekNokta(
        Vector3 impactPointWorld,
        Vector3 impactNormalWorld,
        float severity)
    {
        if (meshDamageData.Count == 0)
            return;

        // KRITIK FIX:
        // Eski kod local vertex mesafesini metre sanarak kullaniyordu.
        // FBX scale 0.01 / 100 oldugunda radius fiilen yok oluyordu.
        // Artik her vertex WORLD'e cevriliyor; tum hesap metre cinsinden.
        float baseRadius =
            gucluHasarModu
                ? Mathf.Max(
                    ezilmeYaricapi,
                    0.88f)
                : ezilmeYaricapi;

        float baseDepth =
            gucluHasarModu
                ? Mathf.Max(
                    maksimumEzilmeDerinligi,
                    0.21f)
                : maksimumEzilmeDerinligi;

        float baseMaxOffset =
            gucluHasarModu
                ? Mathf.Max(
                    maksimumKaliciVertexEzilmesi,
                    0.36f)
                : maksimumKaliciVertexEzilmesi;

        float radius =
            baseRadius *
            Mathf.Lerp(
                0.82f,
                1.28f,
                severity);

        float depth =
            baseDepth *
            Mathf.Lerp(
                0.32f,
                1f,
                severity);

        Vector3 inwardWorld;

        if (vehicleRoot != null)
        {
            inwardWorld =
                vehicleRoot.position -
                impactPointWorld;
        }
        else
        {
            inwardWorld =
                -impactNormalWorld;
        }

        if (inwardWorld.sqrMagnitude < 0.0001f)
            inwardWorld = -impactNormalWorld;

        if (inwardWorld.sqrMagnitude < 0.0001f)
            inwardWorld = Vector3.back;

        inwardWorld.Normalize();

        for (int m = 0;
             m < meshDamageData.Count;
             m++)
        {
            MeshDamageData data =
                meshDamageData[m];

            if (data == null ||
                data.filter == null ||
                data.runtimeMesh == null ||
                data.currentVertices == null)
            {
                continue;
            }

            Transform meshTransform =
                data.filter.transform;

            bool changed = false;

            for (int i = 0;
                 i < data.currentVertices.Length;
                 i++)
            {
                Vector3 currentLocal =
                    data.currentVertices[i];

                Vector3 currentWorld =
                    meshTransform.TransformPoint(
                        currentLocal);

                float distance =
                    Vector3.Distance(
                        currentWorld,
                        impactPointWorld);

                if (distance > radius)
                    continue;

                float falloff =
                    1f -
                    distance /
                    Mathf.Max(
                        0.001f,
                        radius);

                // Daha belirgin ama yumuşak merkez ezilmesi.
                falloff =
                    falloff *
                    falloff *
                    (3f -
                     2f * falloff);

                // Ana mesh ASLA gizlenmez/kopmaz.
                // Vertex sadece carpma bolgesinde aracin icine dogru katlanir.
                Vector3 wrinkleDirection =
                    vehicleRoot != null
                        ? vehicleRoot.up
                        : Vector3.up;

                float wrinkle =
                    Mathf.Sin(
                        (currentWorld.x +
                         currentWorld.z) *
                        11.5f) *
                    depth *
                    0.055f *
                    falloff *
                    severity;

                Vector3 candidateWorld =
                    currentWorld +
                    inwardWorld *
                    depth *
                    falloff +
                    wrinkleDirection *
                    wrinkle;

                Vector3 originalWorld =
                    meshTransform.TransformPoint(
                        data.originalVertices[i]);

                Vector3 totalWorldOffset =
                    candidateWorld -
                    originalWorld;

                if (totalWorldOffset.magnitude >
                    baseMaxOffset)
                {
                    candidateWorld =
                        originalWorld +
                        totalWorldOffset.normalized *
                        baseMaxOffset;
                }

                data.currentVertices[i] =
                    meshTransform.InverseTransformPoint(
                        candidateWorld);

                changed = true;
            }

            if (!changed)
                continue;

            data.runtimeMesh.vertices =
                data.currentVertices;

            data.runtimeMesh.RecalculateBounds();
            data.runtimeMesh.RecalculateNormals();
        }
    }

    // ============================================================
    // PARCALANMA / DOKULME
    // ============================================================
    private void ParcalanmaSisteminiHazirla()
    {
        if (visualRoot == null)
            return;

        if (breakableParts.Count == 0)
        {
            HashSet<Transform> unique =
                new HashSet<Transform>();

            for (int n = 0;
                 n < kopabilirParcaAdlari.Length;
                 n++)
            {
                List<Transform> found =
                    FindAllDeep(
                        visualRoot,
                        kopabilirParcaAdlari[n]);

                for (int i = 0;
                     i < found.Count;
                     i++)
                {
                    Transform t =
                        found[i];

                    if (t == null ||
                        unique.Contains(t))
                        continue;

                    unique.Add(t);

                    BreakablePartData data =
                        new BreakablePartData();

                    data.transform =
                        t;

                    data.originalParent =
                        t.parent;

                    data.originalLocalPosition =
                        t.localPosition;

                    data.originalLocalRotation =
                        t.localRotation;

                    data.originalLocalScale =
                        t.localScale;

                    data.renderers =
                        t.GetComponentsInChildren<
                            Renderer>(
                                true);

                    data.colliders =
                        t.GetComponentsInChildren<
                            Collider>(
                                true);

                    data.originalColliderEnabled =
                        new bool[
                            data.colliders.Length];

                    for (int c = 0;
                         c < data.colliders.Length;
                         c++)
                    {
                        data.originalColliderEnabled[c] =
                            data.colliders[c] != null &&
                            data.colliders[c].enabled;
                    }

                    breakableParts.Add(
                        data);
                }
            }

            bulunanKopabilirParcaSayisi =
                breakableParts.Count;
        }

        if (glassRenderers == null)
        {
            List<Renderer> glasses =
                new List<Renderer>();

            Renderer[] all =
                visualRoot.GetComponentsInChildren<
                    Renderer>(
                        true);

            for (int i = 0;
                 i < all.Length;
                 i++)
            {
                Renderer r =
                    all[i];

                if (r == null)
                    continue;

                string rn =
                    r.name.ToLowerInvariant();

                bool glass =
                    rn.Contains("glass") ||
                    rn.Contains("window") ||
                    rn.Contains("cam");

                Material[] mats =
                    r.sharedMaterials;

                for (int m = 0;
                     !glass &&
                     m < mats.Length;
                     m++)
                {
                    if (mats[m] == null)
                        continue;

                    string mn =
                        mats[m].name.ToLowerInvariant();

                    glass =
                        mn.Contains("glass") ||
                        mn.Contains("window") ||
                        mn.Contains("cam");
                }

                if (glass)
                    glasses.Add(r);
            }

            glassRenderers =
                glasses.ToArray();

            glassOriginalEnabled =
                new bool[
                    glassRenderers.Length];

            for (int i = 0;
                 i < glassRenderers.Length;
                 i++)
            {
                glassOriginalEnabled[i] =
                    glassRenderers[i] != null &&
                    glassRenderers[i].enabled;
            }
        }

        if (vehicleLights == null)
        {
            vehicleLights =
                visualRoot.GetComponentsInChildren<
                    Light>(
                        true);

            lightOriginalEnabled =
                new bool[
                    vehicleLights.Length];

            for (int i = 0;
                 i < vehicleLights.Length;
                 i++)
            {
                lightOriginalEnabled[i] =
                    vehicleLights[i] != null &&
                    vehicleLights[i].enabled;
            }
        }
    }

    private void CarpismaParcalanmaUygula(
        Vector3 impactPointWorld,
        Vector3 impactNormalWorld,
        float severity,
        float impactSpeed)
    {
        // BU ARACIN ANA GOVDESI TEK MESH.
        // Bu nedenle kapilar/camlar/rendererlar ASLA kapatilmaz,
        // transformlar ASLA arac root'undan koparilmaz.
        //
        // "Dokulme" sadece runtime'da uretilen kucuk kozmetik debris'tir.
        // Asil arac her zaman gorunur kalir ve hasar mesh ezilmesiyle verilir.

        if (severity >= 0.50f)
        {
            DebrisSac(
                impactPointWorld,
                impactNormalWorld,
                Mathf.Max(
                    2,
                    Mathf.RoundToInt(
                        sertCarpmaDebrisSayisi *
                        Mathf.Lerp(
                            0.35f,
                            0.85f,
                            severity))),
                severity);
        }

        // Renderer kapatma YOK. Cam dahil hicbir ana mesh kaybolmaz.
        camlarKirildi = false;
        kopmusParcaSayisi = 0;

        // Farlarin Light component'i bozulabilir; far mesh'i kaybolmaz.
        if (severity >= 0.62f)
        {
            YakindakiIsiklariBoz(
                impactPointWorld,
                1.35f);
        }

        if (!farlarBozuldu &&
            saglik <=
                farBozulmaSaglikEsigi)
        {
            TumIsiklariBoz();
        }

        if (saglik <= 0f &&
            !deathBreakupApplied)
        {
            deathBreakupApplied = true;

            // Araba mesh'i hala tamamen sahnede kalir.
            // Sadece ekstra kozmetik kucuk parca dokulur.
            DebrisSac(
                impactPointWorld,
                impactNormalWorld,
                Mathf.Max(
                    6,
                    sifirSaglikDebrisSayisi / 2),
                1f);
        }
    }

    private BreakablePartData EnYakinKopabilirParca(
        Vector3 point,
        out float distance)
    {
        BreakablePartData nearest =
            null;

        distance =
            float.MaxValue;

        for (int i = 0;
             i < breakableParts.Count;
             i++)
        {
            BreakablePartData part =
                breakableParts[i];

            if (part == null ||
                part.transform == null ||
                part.detached)
                continue;

            float d =
                ParcaWorldMesafesi(
                    part,
                    point);

            if (d < distance)
            {
                distance = d;
                nearest = part;
            }
        }

        return nearest;
    }

    private float ParcaWorldMesafesi(
        BreakablePartData part,
        Vector3 point)
    {
        if (part.renderers != null &&
            part.renderers.Length > 0)
        {
            float best =
                float.MaxValue;

            for (int i = 0;
                 i < part.renderers.Length;
                 i++)
            {
                Renderer r =
                    part.renderers[i];

                if (r == null)
                    continue;

                float d =
                    Mathf.Sqrt(
                        r.bounds.SqrDistance(
                            point));

                if (d < best)
                    best = d;
            }

            if (best < float.MaxValue)
                return best;
        }

        return Vector3.Distance(
            part.transform.position,
            point);
    }

    private void ParcayiKopar(
        BreakablePartData part,
        Vector3 impactPoint,
        Vector3 impactNormal,
        float severity)
    {
        // TEK-MESH ARAC KORUMASI:
        // Bu surumde gercek arac transformu/renderer'i koparilmaz.
        // Eski serialized ayarlar true kalsa bile arac gorunmez hale gelmez.
        if (part != null)
        {
            part.damage =
                Mathf.Min(
                    part.damage,
                    parcaKopmaHasarEsigi);

            part.detached = false;
        }

        if (severity >= 0.70f)
        {
            DebrisSac(
                impactPoint,
                impactNormal,
                3,
                severity);
        }
    }

    private void YakindakiIsiklariBoz(
        Vector3 point,
        float radius)
    {
        ParcalanmaSisteminiHazirla();

        if (vehicleLights == null)
            return;

        for (int i = 0;
             i < vehicleLights.Length;
             i++)
        {
            Light l =
                vehicleLights[i];

            if (l == null)
                continue;

            if (Vector3.Distance(
                    l.transform.position,
                    point) <= radius)
            {
                l.enabled = false;
            }
        }
    }

    private void TumIsiklariBoz()
    {
        ParcalanmaSisteminiHazirla();

        if (vehicleLights == null)
            return;

        for (int i = 0;
             i < vehicleLights.Length;
             i++)
        {
            if (vehicleLights[i] != null)
                vehicleLights[i].enabled = false;
        }

        farlarBozuldu = true;
    }

    private void CamlariKir()
    {
        // Tek ana mesh icinde cam materyali bulunabildigi icin Renderer.enabled=false
        // yapmak tum arabayi yok edebiliyordu. Bu nedenle renderer gizleme YOK.
        camlarKirildi = false;
    }

    private void DebrisSac(
        Vector3 point,
        Vector3 normal,
        int count,
        float severity)
    {
        if (count <= 0)
            return;

        Material mat =
            DebrisMaterialAl();

        for (int i = 0;
             i < count;
             i++)
        {
            GameObject shard =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            shard.name =
                "_GLS580_DamageDebris";

            shard.transform.position =
                point +
                UnityEngine.Random.insideUnitSphere *
                0.18f;

            shard.transform.rotation =
                UnityEngine.Random.rotation;

            shard.transform.localScale =
                new Vector3(
                    UnityEngine.Random.Range(
                        0.035f,
                        0.13f),
                    UnityEngine.Random.Range(
                        0.018f,
                        0.065f),
                    UnityEngine.Random.Range(
                        0.04f,
                        0.16f));

            Renderer r =
                shard.GetComponent<Renderer>();

            if (r != null &&
                mat != null)
            {
                r.sharedMaterial =
                    mat;
            }

            Rigidbody rb =
                shard.AddComponent<Rigidbody>();

            rb.mass =
                UnityEngine.Random.Range(
                    0.15f,
                    0.75f);

            rb.collisionDetectionMode =
                CollisionDetectionMode.Continuous;

            Vector3 away =
                -normal;

            if (away.sqrMagnitude < 0.001f)
                away =
                    UnityEngine.Random.onUnitSphere;

            away.Normalize();

            rb.linearVelocity =
                away *
                UnityEngine.Random.Range(
                    0.8f,
                    debrisItmeGucu) *
                Mathf.Lerp(
                    0.5f,
                    1f,
                    severity) +
                Vector3.up *
                UnityEngine.Random.Range(
                    0.8f,
                    2.8f) +
                UnityEngine.Random.insideUnitSphere *
                1.1f;

            rb.angularVelocity =
                UnityEngine.Random.insideUnitSphere *
                UnityEngine.Random.Range(
                    4f,
                    11f);

            Destroy(
                shard,
                debrisOmru);
        }
    }

    private Material DebrisMaterialAl()
    {
        if (debrisMaterial != null)
            return debrisMaterial;

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (shader == null)
            shader =
                Shader.Find(
                    "Standard");

        if (shader == null)
            return null;

        debrisMaterial =
            new Material(
                shader);

        Color c =
            new Color(
                0.075f,
                0.075f,
                0.075f,
                1f);

        if (debrisMaterial.HasProperty(
                "_BaseColor"))
        {
            debrisMaterial.SetColor(
                "_BaseColor",
                c);
        }

        if (debrisMaterial.HasProperty(
                "_Color"))
        {
            debrisMaterial.SetColor(
                "_Color",
                c);
        }

        if (debrisMaterial.HasProperty(
                "_Smoothness"))
        {
            debrisMaterial.SetFloat(
                "_Smoothness",
                0.18f);
        }

        return debrisMaterial;
    }

    // ============================================================
    // CARPMA GOVDE TEPKISI
    // ============================================================
    private void CarpmaGovdeSisteminiHazirla()
    {
        if (visualRoot == null)
            return;

        if (crashBodyLayer == null)
        {
            crashBodyLayer =
                visualRoot.GetComponent<
                    GLS580CarpmaGovdeTepkisi>();

            if (crashBodyLayer == null)
            {
                crashBodyLayer =
                    visualRoot.gameObject.AddComponent<
                        GLS580CarpmaGovdeTepkisi>();
            }
        }

        crashBodyLayer.sekmeOrani =
            darbeSekmeOrani;
    }

    private void CarpmaGovdeTepkisiVer(
        Vector3 impactPointWorld,
        float severity,
        float impactSpeedMps)
    {
        CarpmaGovdeSisteminiHazirla();

        if (crashBodyLayer == null ||
            vehicleRoot == null)
            return;

        Vector3 fromCenter =
            impactPointWorld -
            vehicleRoot.position;

        float frontAmount =
            Vector3.Dot(
                fromCenter,
                vehicleRoot.forward);

        float sideAmount =
            Vector3.Dot(
                fromCenter,
                vehicleRoot.right);

        float impactKmh =
            Mathf.Abs(
                impactSpeedMps) *
            3.6f;

        float highSpeed01 =
            Mathf.InverseLerp(
                yuksekHizCarpmaEsigiKmh,
                Mathf.Max(
                    yuksekHizCarpmaEsigiKmh +
                    1f,
                    maksimumHasarHizi *
                    3.6f),
                impactKmh);

        float power =
            Mathf.Clamp01(
                Mathf.Max(
                    severity * 0.65f,
                    highSpeed01));

        float pitch = 0f;
        float roll = 0f;
        float lift = 0f;

        bool mostlyFrontRear =
            Mathf.Abs(
                frontAmount) >=
            Mathf.Abs(
                sideAmount) *
            0.72f;

        if (mostlyFrontRear)
        {
            if (frontAmount >= 0f)
            {
                // ON CARPMA:
                // +X pitch = burun asagi / arka yukari.
                // Boylece arka tekerler yuksek hizda havaya kalkmis gibi gorunur.
                pitch =
                    maksimumOnCarpmaPitch *
                    Mathf.Lerp(
                        0.22f,
                        1f,
                        power) *
                    severity;

                sonDarbeYonu =
                    "ON - ARKA TEKERLER YUKARI";
            }
            else
            {
                // ARKA CARPMA: on taraf hafif yukari.
                pitch =
                    -maksimumArkaCarpmaPitch *
                    Mathf.Lerp(
                        0.20f,
                        1f,
                        power) *
                    severity;

                sonDarbeYonu =
                    "ARKA - ON TARAF YUKARI";
            }

            lift =
                maksimumDarbeYukariOffset *
                power *
                severity;
        }
        else
        {
            // Sagdan darbe -> sola, soldan darbe -> saga govde yatmasi.
            float sideSign =
                sideAmount >= 0f
                    ? -1f
                    : 1f;

            roll =
                sideSign *
                maksimumYanCarpmaRoll *
                Mathf.Lerp(
                    0.25f,
                    1f,
                    power) *
                severity;

            lift =
                maksimumDarbeYukariOffset *
                0.45f *
                power *
                severity;

            sonDarbeYonu =
                sideAmount >= 0f
                    ? "SAG YAN"
                    : "SOL YAN";
        }

        // Dusuk hizda komik sekilde havaya firlamasin.
        if (impactKmh <
            yuksekHizCarpmaEsigiKmh)
        {
            pitch *= 0.45f;
            roll *= 0.55f;
            lift *= 0.35f;
        }

        sonDarbePitch =
            pitch;

        sonDarbeRoll =
            roll;

        crashBodyLayer.sekmeOrani =
            darbeSekmeOrani;

        crashBodyLayer.DarbeVer(
            pitch,
            roll,
            lift,
            darbeGovdeSuresi,
            power);
    }

    // ============================================================
    // KAMERA DARBE
    // ============================================================
    private void KameraSisteminiHazirla()
    {
        if (darbeKameralari == null ||
            darbeKameralari.Length == 0)
        {
            List<Camera> cams =
                new List<Camera>();

            CameraBulVeEkle(
                cams,
                "_GLS580_AracKamerasi");

            CameraBulVeEkle(
                cams,
                "orta kamera araba");

            CameraBulVeEkle(
                cams,
                "uzak kamera araba");

            CameraBulVeEkle(
                cams,
                "karakter gözü kamerası");

            CameraBulVeEkle(
                cams,
                "karakter gozu kamerasi");

            darbeKameralari =
                cams.ToArray();
        }

        List<GLS580KameraDarbeKatmaniYeni> layers =
            new List<
                GLS580KameraDarbeKatmaniYeni>();

        if (darbeKameralari != null)
        {
            for (int i = 0;
                 i <
                 darbeKameralari.Length;
                 i++)
            {
                Camera cam =
                    darbeKameralari[i];

                if (cam == null)
                    continue;

                GLS580KameraDarbeKatmaniYeni layer =
                    cam.GetComponent<
                        GLS580KameraDarbeKatmaniYeni>();

                if (layer == null)
                {
                    layer =
                        cam.gameObject.AddComponent<
                            GLS580KameraDarbeKatmaniYeni>();
                }

                if (!layers.Contains(
                        layer))
                {
                    layers.Add(
                        layer);
                }
            }
        }

        cameraLayers =
            layers.ToArray();
    }

    private void KameraDarbeVer(
        float strength)
    {
        if (cameraLayers == null ||
            cameraLayers.Length == 0)
        {
            KameraSisteminiHazirla();
        }

        if (cameraLayers == null)
            return;

        for (int i = 0;
             i <
             cameraLayers.Length;
             i++)
        {
            GLS580KameraDarbeKatmaniYeni layer =
                cameraLayers[i];

            if (layer != null &&
                layer.gameObject.activeInHierarchy)
            {
                layer.DarbeVer(
                    strength);
            }
        }
    }

    private void CameraBulVeEkle(
        List<Camera> list,
        string objectName)
    {
        Camera[] all =
            FindObjectsOfType<
                Camera>(
                    true);

        for (int i = 0;
             i < all.Length;
             i++)
        {
            Camera cam =
                all[i];

            if (cam != null &&
                string.Equals(
                    cam.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!list.Contains(
                        cam))
                {
                    list.Add(
                        cam);
                }

                return;
            }
        }
    }

    // ============================================================
    // KIR / TEMIZLEME
    // ============================================================
    private void MesafeBaslat()
    {
        if (vehicleRoot == null)
            return;

        lastVehiclePosition =
            vehicleRoot.position;

        lastPositionReady =
            true;
    }

    private void MesafeVeKirGuncelle()
    {
        if (vehicleRoot == null)
            return;

        if (!lastPositionReady)
        {
            MesafeBaslat();
            return;
        }

        Vector3 current =
            vehicleRoot.position;

        float distance =
            Vector3.Distance(
                current,
                lastVehiclePosition);

        lastVehiclePosition =
            current;

        // Teleport / spawn kirlenme sayilmasin.
        if (distance >
            15f)
            return;

        float dt =
            Mathf.Max(
                Time.deltaTime,
                0.0001f);

        fallbackSpeedKmh =
            distance /
            dt *
            3.6f;

        currentSpeedKmh =
            GercekHizKmh();

        if (!kirlenmeAktif ||
            currentSpeedKmh <
            kirlenmeMinimumHizKmh)
        {
            return;
        }

        toplamSurulenMetre +=
            distance;

        kirOrani =
            Mathf.Clamp01(
                kirOrani +
                kirlenme100Metrede *
                (distance / 100f));
    }

    public void SuIleTemizle(
        float miktar)
    {
        if (miktar <= 0f)
            return;

        kirOrani =
            Mathf.Clamp01(
                kirOrani -
                miktar);

        GorunumuGuncelle();
    }

    public void TamTemizle()
    {
        kirOrani = 0f;
        GorunumuGuncelle();
    }

    public void TamirEt()
    {
        saglik =
            maksimumSaglik;

        deathBreakupApplied = false;
        camlarKirildi = false;
        farlarBozuldu = false;

        ParcalanmaSisteminiHazirla();

        for (int p = 0;
             p < breakableParts.Count;
             p++)
        {
            BreakablePartData part =
                breakableParts[p];

            if (part == null ||
                part.transform == null)
                continue;

            if (part.addedRigidbody != null)
            {
                Destroy(
                    part.addedRigidbody);

                part.addedRigidbody =
                    null;
            }

            part.transform.SetParent(
                part.originalParent,
                false);

            part.transform.localPosition =
                part.originalLocalPosition;

            part.transform.localRotation =
                part.originalLocalRotation;

            part.transform.localScale =
                part.originalLocalScale;

            Collider[] currentCols =
                part.transform.GetComponentsInChildren<
                    Collider>(
                        true);

            for (int c = 0;
                 c < currentCols.Length;
                 c++)
            {
                if (c <
                        part.originalColliderEnabled.Length &&
                    currentCols[c] != null)
                {
                    currentCols[c].enabled =
                        part.originalColliderEnabled[c];
                }
            }

            part.damage = 0f;
            part.detached = false;
        }

        kopmusParcaSayisi = 0;

        // Eski surum Play Mode'da renderer kapattiysa tamirde geri ac.
        if (visualRoot != null)
        {
            Renderer[] allVisible =
                visualRoot.GetComponentsInChildren<
                    Renderer>(
                        true);

            for (int i = 0;
                 i < allVisible.Length;
                 i++)
            {
                if (allVisible[i] != null)
                    allVisible[i].enabled = true;
            }
        }

        if (glassRenderers != null)
        {
            for (int i = 0;
                 i < glassRenderers.Length;
                 i++)
            {
                if (glassRenderers[i] != null)
                {
                    glassRenderers[i].enabled =
                        glassOriginalEnabled != null &&
                        i < glassOriginalEnabled.Length
                            ? glassOriginalEnabled[i]
                            : true;
                }
            }
        }

        if (vehicleLights != null)
        {
            for (int i = 0;
                 i < vehicleLights.Length;
                 i++)
            {
                if (vehicleLights[i] != null)
                {
                    vehicleLights[i].enabled =
                        lightOriginalEnabled != null &&
                        i < lightOriginalEnabled.Length
                            ? lightOriginalEnabled[i]
                            : true;
                }
            }
        }

        for (int i = 0;
             i <
             meshDamageData.Count;
             i++)
        {
            MeshDamageData data =
                meshDamageData[i];

            if (data == null ||
                data.runtimeMesh == null ||
                data.originalVertices == null)
                continue;

            data.currentVertices =
                (Vector3[])
                data.originalVertices.Clone();

            data.runtimeMesh.vertices =
                data.currentVertices;

            data.runtimeMesh.RecalculateBounds();
            data.runtimeMesh.RecalculateNormals();
        }

        GorunumuGuncelle();
    }

    [ContextMenu("TEST - TAM KIRLET")]
    private void TestTamKirlet()
    {
        kirOrani = 1f;
        GorunumuGuncelle();
    }

    [ContextMenu("TEST - TEMIZLE")]
    private void TestTemizle()
    {
        TamTemizle();
    }

    [ContextMenu("TEST - ORTA HASAR")]
    private void TestOrtaHasar()
    {
        saglik =
            Mathf.Max(
                0f,
                saglik -
                25f);

        sonHasarKaynagi =
            "Editor Test";

        sonCarpilanObje =
            "TEST";

        KameraDarbeVer(
            0.55f);

        GorunumuGuncelle();
    }

    [ContextMenu("TEST - TAMIR ET")]
    private void TestTamir()
    {
        TamirEt();
    }

    [ContextMenu("TEST - AGIR HASAR / EZILME")]
    private void TestTamDagit()
    {
        saglik = 0f;

        if (vehicleRoot == null)
            OtomatikBul();

        Vector3 center =
            vehicleRoot != null
                ? vehicleRoot.position
                : transform.position;

        Vector3 forward =
            vehicleRoot != null
                ? vehicleRoot.forward
                : transform.forward;

        Vector3 right =
            vehicleRoot != null
                ? vehicleRoot.right
                : transform.right;

        Vector3 frontPoint =
            center +
            forward * 2.05f +
            Vector3.up * 0.70f;

        CarpismaMeshEzTekNokta(
            frontPoint,
            -forward,
            1f);

        CarpismaMeshEzTekNokta(
            frontPoint +
            right * 0.55f,
            -forward,
            0.88f);

        CarpismaMeshEzTekNokta(
            frontPoint -
            right * 0.55f,
            -forward,
            0.88f);

        if (carpmaGovdeTepkisiAktif)
        {
            CarpmaGovdeTepkisiVer(
                frontPoint,
                1f,
                maksimumHasarHizi);
        }

        DebrisSac(
            frontPoint,
            -forward,
            10,
            1f);

        GorunumuGuncelle();
    }

    // ============================================================
    // MATERIAL
    // ============================================================
    private void KirSisteminiHazirla()
    {
        if (materialSlots.Count >
            0)
            return;

        if (otomatikTumRendererlariKirlet)
        {
            List<Renderer> merged =
                new List<Renderer>();

            if (kirRenderers != null)
            {
                for (int i = 0;
                     i < kirRenderers.Length;
                     i++)
                {
                    if (kirRenderers[i] != null &&
                        !merged.Contains(
                            kirRenderers[i]))
                    {
                        merged.Add(
                            kirRenderers[i]);
                    }
                }
            }

            Renderer[] auto =
                OtomatikRendererBul();

            for (int i = 0;
                 i < auto.Length;
                 i++)
            {
                if (auto[i] != null &&
                    !merged.Contains(
                        auto[i]))
                {
                    merged.Add(
                        auto[i]);
                }
            }

            kirRenderers =
                merged.ToArray();
        }
        else if (kirRenderers == null ||
                 kirRenderers.Length == 0)
        {
            kirRenderers =
                OtomatikRendererBul();
        }

        if (kirRenderers == null)
            return;

        string[] blacklist =
            new string[]
            {
                "glass",
                "window",
                "cam",
                "light",
                "lamp",
                "far",
                "stop",
                "emissive",
                "chrome",
                "krom",
                "tire",
                "tyre",
                "wheel",
                "rim",
                "jant"
            };

        for (int r = 0;
             r <
             kirRenderers.Length;
             r++)
        {
            Renderer renderer =
                kirRenderers[r];

            if (renderer == null)
                continue;

            Material[] mats =
                renderer.sharedMaterials;

            for (int i = 0;
                 i < mats.Length;
                 i++)
            {
                Material mat =
                    mats[i];

                if (mat == null)
                    continue;

                string lower =
                    mat.name.ToLowerInvariant();

                bool skip = false;

                for (int b = 0;
                     b <
                     blacklist.Length;
                     b++)
                {
                    if (lower.Contains(
                            blacklist[b]))
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip)
                    continue;

                string colorProp =
                    "";

                if (mat.HasProperty(
                        "_BaseColor"))
                {
                    colorProp =
                        "_BaseColor";
                }
                else if (
                    mat.HasProperty(
                        "_Color"))
                {
                    colorProp =
                        "_Color";
                }

                if (string.IsNullOrEmpty(
                        colorProp))
                    continue;

                MaterialSlotData slot =
                    new MaterialSlotData();

                slot.renderer =
                    renderer;

                slot.materialIndex =
                    i;

                slot.colorProperty =
                    colorProp;

                slot.originalColor =
                    mat.GetColor(
                        colorProp);

                if (mat.HasProperty(
                        "_Smoothness"))
                {
                    slot.smoothnessProperty =
                        "_Smoothness";

                    slot.originalSmoothness =
                        mat.GetFloat(
                            "_Smoothness");
                }
                else if (
                    mat.HasProperty(
                        "_Glossiness"))
                {
                    slot.smoothnessProperty =
                        "_Glossiness";

                    slot.originalSmoothness =
                        mat.GetFloat(
                            "_Glossiness");
                }

                slot.block =
                    new MaterialPropertyBlock();

                materialSlots.Add(
                    slot);
            }
        }

        aktifKirRendererSayisi =
            kirRenderers.Length;
    }

    private void GorunumuGuncelle()
    {
        if (materialSlots.Count ==
            0)
        {
            KirSisteminiHazirla();
        }

        float damage01 =
            HasarOrani;

        // Erken kirlenme de gorunsun:
        // %17 kir -> sadece %17 degil, yaklasik %38 gorunur kir etkisi.
        float visualDirt =
            Mathf.Pow(
                Mathf.Clamp01(
                    kirOrani),
                0.55f);

        for (int i = 0;
             i <
             materialSlots.Count;
             i++)
        {
            MaterialSlotData slot =
                materialSlots[i];

            if (slot == null ||
                slot.renderer == null ||
                slot.block == null)
                continue;

            slot.renderer.GetPropertyBlock(
                slot.block,
                slot.materialIndex);

            Color damaged =
                Color.Lerp(
                    slot.originalColor,
                    hasarRengi,
                    damage01 *
                    hasarRenkEtkisi);

            Color finalColor =
                Color.Lerp(
                    damaged,
                    kirRengi,
                    visualDirt *
                    Mathf.Max(
                        kirRenkEtkisi,
                        gucluHasarModu
                            ? 0.78f
                            : 0f));

            slot.block.SetColor(
                slot.colorProperty,
                finalColor);

            if (!string.IsNullOrEmpty(
                    slot.smoothnessProperty))
            {
                float smooth =
                    slot.originalSmoothness;

                smooth *=
                    1f -
                    visualDirt *
                    Mathf.Max(
                        kirMatlikEtkisi,
                        gucluHasarModu
                            ? 0.82f
                            : 0f);

                smooth *=
                    1f -
                    damage01 *
                    0.12f;

                slot.block.SetFloat(
                    slot.smoothnessProperty,
                    Mathf.Clamp01(
                        smooth));
            }

            slot.renderer.SetPropertyBlock(
                slot.block,
                slot.materialIndex);
        }
    }

    // ============================================================
    // MESH PREP
    // ============================================================
    private void MeshHasarSisteminiHazirla()
    {
        if (meshDamageData.Count >
            0)
            return;

        List<MeshFilter> mergedFilters =
            new List<MeshFilter>();

        if (hasarMeshleri != null)
        {
            for (int i = 0;
                 i < hasarMeshleri.Length;
                 i++)
            {
                if (hasarMeshleri[i] != null &&
                    !mergedFilters.Contains(
                        hasarMeshleri[i]))
                {
                    mergedFilters.Add(
                        hasarMeshleri[i]);
                }
            }
        }

        MeshFilter[] autoFilters =
            otomatikTumMeshleriTopla
                ? TumMeshleriBul()
                : OtomatikMeshBul();

        if (autoFilters != null)
        {
            for (int i = 0;
                 i < autoFilters.Length;
                 i++)
            {
                if (autoFilters[i] != null &&
                    !mergedFilters.Contains(
                        autoFilters[i]))
                {
                    mergedFilters.Add(
                        autoFilters[i]);
                }
            }
        }

        hasarMeshleri =
            mergedFilters.ToArray();

        if (hasarMeshleri.Length == 0)
            return;

        HashSet<MeshFilter> unique =
            new HashSet<MeshFilter>();

        for (int i = 0;
             i <
             hasarMeshleri.Length;
             i++)
        {
            MeshFilter filter =
                hasarMeshleri[i];

            if (filter == null ||
                filter.sharedMesh == null ||
                unique.Contains(
                    filter))
            {
                continue;
            }

            unique.Add(
                filter);

            Mesh source =
                filter.sharedMesh;

            if (!source.isReadable)
            {
                Debug.LogWarning(
                    "GLS580CarpismaHasarSistemi: Mesh Read/Write kapali, ezilme atlandi: " +
                    source.name,
                    filter);

                continue;
            }

            Mesh runtime =
                Instantiate(
                    source);

            runtime.name =
                source.name +
                "_RuntimeDamage";

            filter.sharedMesh =
                runtime;

            MeshDamageData data =
                new MeshDamageData();

            data.filter =
                filter;

            data.runtimeMesh =
                runtime;

            data.originalVertices =
                runtime.vertices;

            data.currentVertices =
                (Vector3[])
                data.originalVertices.Clone();

            meshDamageData.Add(
                data);
        }

        aktifHasarMeshSayisi =
            meshDamageData.Count;
    }

    private MeshFilter[] TumMeshleriBul()
    {
        List<MeshFilter> result =
            new List<MeshFilter>();

        Transform root =
            visualRoot != null
                ? visualRoot
                : vehicleRoot;

        if (root == null)
            return result.ToArray();

        MeshFilter[] all =
            root.GetComponentsInChildren<
                MeshFilter>(
                    true);

        for (int i = 0;
             i < all.Length;
             i++)
        {
            MeshFilter f =
                all[i];

            if (f == null ||
                f.sharedMesh == null)
                continue;

            string n =
                f.name.ToLowerInvariant();

            // Teker ve direksiyon meshlerini ezme listesine alma.
            if (n.Contains("wheel") ||
                n.Contains("tire") ||
                n.Contains("tyre") ||
                n.Contains("jant") ||
                n.Contains("steering") ||
                n.Contains("direksiyon"))
            {
                continue;
            }

            result.Add(f);
        }

        return result.ToArray();
    }

    private MeshFilter[] OtomatikMeshBul()
    {
        List<MeshFilter> result =
            new List<MeshFilter>();

        Transform root =
            visualRoot != null
                ? visualRoot
                : vehicleRoot;

        if (root == null)
            return result.ToArray();

        for (int n = 0;
             n <
             hasarParcaAdlari.Length;
             n++)
        {
            List<Transform> parts =
                FindAllDeep(
                    root,
                    hasarParcaAdlari[n]);

            for (int p = 0;
                 p <
                 parts.Count;
                 p++)
            {
                MeshFilter[] filters =
                    parts[p].GetComponentsInChildren<
                        MeshFilter>(
                            true);

                for (int f = 0;
                     f <
                     filters.Length;
                     f++)
                {
                    if (filters[f] != null &&
                        !result.Contains(
                            filters[f]))
                    {
                        result.Add(
                            filters[f]);
                    }
                }
            }
        }

        return result.ToArray();
    }

    private Renderer[] OtomatikRendererBul()
    {
        List<Renderer> result =
            new List<Renderer>();

        Transform root =
            visualRoot != null
                ? visualRoot
                : vehicleRoot;

        if (root == null)
            return result.ToArray();

        if (otomatikTumRendererlariKirlet)
        {
            Renderer[] all =
                root.GetComponentsInChildren<
                    Renderer>(
                        true);

            for (int i = 0;
                 i < all.Length;
                 i++)
            {
                Renderer r =
                    all[i];

                if (r == null)
                    continue;

                string n =
                    r.name.ToLowerInvariant();

                if (n.Contains("wheel") ||
                    n.Contains("tire") ||
                    n.Contains("tyre") ||
                    n.Contains("jant") ||
                    n.Contains("steering") ||
                    n.Contains("direksiyon"))
                {
                    continue;
                }

                if (!result.Contains(r))
                    result.Add(r);
            }

            return result.ToArray();
        }

        for (int n = 0;
             n < hasarParcaAdlari.Length;
             n++)
        {
            List<Transform> parts =
                FindAllDeep(
                    root,
                    hasarParcaAdlari[n]);

            for (int p = 0;
                 p < parts.Count;
                 p++)
            {
                Renderer[] renderers =
                    parts[p].GetComponentsInChildren<
                        Renderer>(
                            true);

                for (int r = 0;
                     r < renderers.Length;
                     r++)
                {
                    if (renderers[r] != null &&
                        !result.Contains(
                            renderers[r]))
                    {
                        result.Add(
                            renderers[r]);
                    }
                }
            }
        }

        return result.ToArray();
    }

    // ============================================================
    // AUDIO
    // ============================================================
    private void AudioHazirla()
    {
        if (vehicleRoot == null ||
            crashAudio != null)
            return;

        GameObject go =
            new GameObject(
                "_GLS580_CrashAudio");

        go.transform.SetParent(
            vehicleRoot,
            false);

        crashAudio =
            go.AddComponent<
                AudioSource>();

        crashAudio.playOnAwake =
            false;

        crashAudio.spatialBlend =
            0.75f;

        crashAudio.minDistance =
            2f;

        crashAudio.maxDistance =
            55f;
    }

    private void CarpismaSesiCal(
        float severity)
    {
        if (crashAudio == null)
            AudioHazirla();

        if (crashAudio == null)
            return;

        AudioClip clip =
            severity >=
            sertSesEsigi
                ? sertCarpismaSesi
                : hafifCarpismaSesi;

        if (clip == null)
        {
            clip =
                sertCarpismaSesi != null
                    ? sertCarpismaSesi
                    : hafifCarpismaSesi;
        }

        if (clip == null)
            return;

        crashAudio.pitch =
            UnityEngine.Random.Range(
                0.94f,
                1.06f);

        crashAudio.PlayOneShot(
            clip,
            carpismaSesSeviyesi *
            Mathf.Lerp(
                0.45f,
                1f,
                severity));
    }

    // ============================================================
    // BASIT SYSTEM / SPEED
    // ============================================================
    private void OtomatikBul()
    {
        BasitSistemBul();

        if (basitSistem != null &&
            fDriveRoot != null)
        {
            try
            {
                Transform root =
                    fDriveRoot.GetValue(
                        basitSistem)
                    as Transform;

                if (root != null)
                {
                    vehicleRoot =
                        root;
                }
            }
            catch { }
        }

        if (vehicleRoot == null)
        {
            GameObject go =
                GameObject.Find(
                    "_GLS580_DriveRoot_V2");

            if (go != null)
                vehicleRoot =
                    go.transform;
        }

        if (vehicleRoot == null)
            vehicleRoot =
                transform;

        if (vehicleRoot != null)
        {
            Rigidbody rb =
                vehicleRoot.GetComponent<
                    Rigidbody>();

            if (rb != null)
                vehicleRigidbody =
                    rb;
        }

        VisualRootBul();

        bulunanDriveRoot =
            vehicleRoot != null
                ? vehicleRoot.name
                : "BULUNAMADI";

        bulunanVisualRoot =
            visualRoot != null
                ? visualRoot.name
                : "BULUNAMADI";
    }

    private void BasitSistemBul()
    {
        if (basitSistem == null)
        {
            MonoBehaviour[] all =
                FindObjectsOfType<
                    MonoBehaviour>(
                        true);

            for (int i = 0;
                 i <
                 all.Length;
                 i++)
            {
                MonoBehaviour mb =
                    all[i];

                if (mb != null &&
                    mb.GetType().Name ==
                    "GLS580BasitSistem")
                {
                    basitSistem =
                        mb;

                    break;
                }
            }
        }

        if (basitSistem == null)
            return;

        Type currentType =
            basitSistem.GetType();

        if (basitType ==
            currentType)
            return;

        basitType =
            currentType;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        fPlayerInside =
            currentType.GetField(
                "playerInside",
                flags);

        fBusy =
            currentType.GetField(
                "busy",
                flags);

        fDriveRoot =
            currentType.GetField(
                "driveRoot",
                flags);

        fCurrentDriveSpeed =
            currentType.GetField(
                "currentDriveSpeed",
                flags);

        fCurrentSteerAngle =
            currentType.GetField(
                "currentSteerAngle",
                flags);

        fWheelBase =
            currentType.GetField(
                "wheelBase",
                flags);
    }

    private void DurumuOku()
    {
        if (basitSistem == null)
        {
            playerInside = false;
            busy = false;
            return;
        }

        try
        {
            playerInside =
                fPlayerInside != null
                    ? (bool)
                    fPlayerInside.GetValue(
                        basitSistem)
                    : false;

            busy =
                fBusy != null
                    ? (bool)
                    fBusy.GetValue(
                        basitSistem)
                    : false;
        }
        catch
        {
            playerInside = false;
            busy = false;
        }
    }

    private float CurrentDriveSpeedOku()
    {
        if (basitSistem == null ||
            fCurrentDriveSpeed == null)
            return 0f;

        try
        {
            return Convert.ToSingle(
                fCurrentDriveSpeed.GetValue(
                    basitSistem));
        }
        catch
        {
            return 0f;
        }
    }

    private void CurrentDriveSpeedYaz(
        float value)
    {
        if (basitSistem == null ||
            fCurrentDriveSpeed == null)
            return;

        try
        {
            fCurrentDriveSpeed.SetValue(
                basitSistem,
                value);
        }
        catch { }
    }

    private float GercekHizKmh()
    {
        if (basitSistem != null &&
            fCurrentDriveSpeed != null)
        {
            return
                Mathf.Abs(
                    CurrentDriveSpeedOku()) *
                3.6f;
        }

        return fallbackSpeedKmh;
    }

    private Vector3 TahminiHareketYonu(
        float speedMps,
        float dt,
        Vector3 up)
    {
        Vector3 forward =
            Vector3.ProjectOnPlane(
                vehicleRoot.forward,
                up);

        if (forward.sqrMagnitude <
            0.001f)
            forward =
                vehicleRoot.forward;

        forward.Normalize();

        if (fCurrentSteerAngle == null ||
            fWheelBase == null)
            return forward;

        try
        {
            float steerAngle =
                Convert.ToSingle(
                    fCurrentSteerAngle.GetValue(
                        basitSistem));

            float wheelBase =
                Mathf.Max(
                    0.1f,
                    Convert.ToSingle(
                        fWheelBase.GetValue(
                            basitSistem)));

            if (Mathf.Abs(
                    speedMps) <
                    0.03f ||
                Mathf.Abs(
                    steerAngle) <
                    0.001f)
            {
                return forward;
            }

            float yawRadians =
                (speedMps /
                 wheelBase) *
                Mathf.Tan(
                    steerAngle *
                    Mathf.Deg2Rad) *
                dt;

            return
                Quaternion.AngleAxis(
                    yawRadians *
                    Mathf.Rad2Deg,
                    up) *
                forward;
        }
        catch
        {
            return forward;
        }
    }

    private void RigidbodyAyarla()
    {
        if (!rigidbodyAyarlariniGuclendir ||
            vehicleRigidbody == null)
            return;

        vehicleRigidbody.detectCollisions =
            true;

        if (interpolation)
        {
            vehicleRigidbody.interpolation =
                RigidbodyInterpolation.Interpolate;
        }

        if (continuousDynamic)
        {
            vehicleRigidbody.collisionDetectionMode =
                vehicleRigidbody.isKinematic
                    ? CollisionDetectionMode.ContinuousSpeculative
                    : CollisionDetectionMode.ContinuousDynamic;
        }
    }

    // ============================================================
    // VISUAL ROOT
    // ============================================================
    private void VisualRootBul()
    {
        if (visualRoot != null ||
            vehicleRoot == null)
            return;

        Transform mercedes =
            FindDeepContains(
                vehicleRoot,
                "Mercedes_GLS580");

        if (mercedes != null &&
            mercedes != vehicleRoot)
        {
            Transform candidate =
                mercedes;

            while (
                candidate.parent != null &&
                candidate.parent !=
                vehicleRoot)
            {
                candidate =
                    candidate.parent;
            }

            if (candidate.parent ==
                vehicleRoot)
            {
                visualRoot =
                    candidate;

                return;
            }
        }

        Transform staticBody =
            FindDeep(
                vehicleRoot,
                "Static_Body");

        if (staticBody != null)
        {
            Transform candidate =
                staticBody;

            while (
                candidate.parent != null &&
                candidate.parent !=
                vehicleRoot)
            {
                candidate =
                    candidate.parent;
            }

            visualRoot =
                candidate.parent ==
                vehicleRoot
                    ? candidate
                    : staticBody;
        }
    }

    // ============================================================
    // FIND HELPERS
    // ============================================================
    private static Transform FindDeep(
        Transform root,
        string wanted)
    {
        if (root == null)
            return null;

        if (root.name ==
            wanted)
            return root;

        for (int i = 0;
             i <
             root.childCount;
             i++)
        {
            Transform found =
                FindDeep(
                    root.GetChild(i),
                    wanted);

            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindDeepContains(
        Transform root,
        string wanted)
    {
        if (root == null)
            return null;

        if (root.name.IndexOf(
                wanted,
                StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return root;
        }

        for (int i = 0;
             i <
             root.childCount;
             i++)
        {
            Transform found =
                FindDeepContains(
                    root.GetChild(i),
                    wanted);

            if (found != null)
                return found;
        }

        return null;
    }

    private static List<Transform> FindAllDeep(
        Transform root,
        string wanted)
    {
        List<Transform> result =
            new List<Transform>();

        FindAllDeepRecursive(
            root,
            wanted,
            result);

        return result;
    }

    private static void FindAllDeepRecursive(
        Transform root,
        string wanted,
        List<Transform> result)
    {
        if (root == null)
            return;

        if (root.name ==
            wanted)
        {
            result.Add(
                root);
        }

        for (int i = 0;
             i <
             root.childCount;
             i++)
        {
            FindAllDeepRecursive(
                root.GetChild(i),
                wanted,
                result);
        }
    }
    private void OnDestroy()
    {
        if (debrisMaterial != null)
        {
            Destroy(
                debrisMaterial);
        }
    }
}


// =====================================================================
// KAMERA DARBE KATMANI - AYNI DOSYADA, AYRI SCRIPT EKLEME GEREKMEZ.
// =====================================================================
public class GLS580KameraDarbeKatmaniYeni : MonoBehaviour
{
    public float maksimumPozisyonSarsintisi = 0.055f;
    public float maksimumRotasyonDerecesi = 2.3f;
    public float minimumSure = 0.11f;
    public float maksimumSure = 0.38f;
    public float frekans = 31f;

    private Camera cam;
    private float remaining;
    private float duration;
    private float strength;
    private float seed;

    private Matrix4x4 originalView;
    private bool viewOverridden;

    private void Awake()
    {
        cam =
            GetComponent<Camera>();

        seed =
            UnityEngine.Random.Range(
                0f,
                1000f);
    }

    public void DarbeVer(
        float newStrength)
    {
        newStrength =
            Mathf.Clamp(
                newStrength,
                0f,
                1.5f);

        strength =
            Mathf.Max(
                strength,
                newStrength);

        duration =
            Mathf.Lerp(
                minimumSure,
                maksimumSure,
                Mathf.Clamp01(
                    newStrength));

        remaining =
            Mathf.Max(
                remaining,
                duration);
    }

    private void Update()
    {
        if (remaining <= 0f)
        {
            remaining = 0f;
            strength = 0f;
            return;
        }

        remaining -=
            Time.unscaledDeltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            strength = 0f;
        }
    }

    private void OnPreCull()
    {
        if (cam == null ||
            remaining <= 0f)
            return;

        float normalized =
            duration > 0f
                ? Mathf.Clamp01(
                    remaining /
                    duration)
                : 0f;

        float envelope =
            normalized *
            normalized;

        float amp =
            strength *
            envelope;

        float t =
            Time.unscaledTime *
            frekans;

        float nx =
            Mathf.PerlinNoise(
                seed,
                t) *
            2f -
            1f;

        float ny =
            Mathf.PerlinNoise(
                seed + 13.7f,
                t * 1.11f) *
            2f -
            1f;

        float nz =
            Mathf.PerlinNoise(
                seed + 31.2f,
                t * 0.87f) *
            2f -
            1f;

        Vector3 positionOffset =
            new Vector3(
                nx,
                ny,
                nz * 0.25f) *
            maksimumPozisyonSarsintisi *
            amp;

        Quaternion rotationOffset =
            Quaternion.Euler(
                ny *
                maksimumRotasyonDerecesi *
                amp,
                nx *
                maksimumRotasyonDerecesi *
                0.65f *
                amp,
                nz *
                maksimumRotasyonDerecesi *
                amp);

        originalView =
            cam.worldToCameraMatrix;

        Matrix4x4 shake =
            Matrix4x4.TRS(
                positionOffset,
                rotationOffset,
                Vector3.one);

        cam.worldToCameraMatrix =
            shake.inverse *
            originalView;

        viewOverridden =
            true;
    }

    private void OnPostRender()
    {
        RestoreView();
    }

    private void OnDisable()
    {
        RestoreView();
    }

    private void RestoreView()
    {
        if (cam != null &&
            viewOverridden)
        {
            cam.worldToCameraMatrix =
                originalView;

            viewOverridden =
                false;
        }
    }
}


// =====================================================================
// GOVDE DARBE KATMANI
// GLS580SurusHissi (33000) LateUpdate'undan SONRA calisir.
// Transform'a ek katman uygular; onceki frame kendi offset'ini geri alir.
// Bu nedenle kamera/suspansiyon/govde scriptleriyle birikimli rotasyon yapmaz.
// =====================================================================
[DefaultExecutionOrder(37000)]
public class GLS580CarpmaGovdeTepkisi : MonoBehaviour
{
    [Range(0f, 0.65f)]
    public float sekmeOrani = 0.28f;

    private float targetPitch;
    private float targetRoll;
    private float targetLift;
    private float duration;
    private float remaining;
    private float power;

    private Quaternion lastRotationOffset =
        Quaternion.identity;

    private Vector3 lastPositionOffset =
        Vector3.zero;

    public void DarbeVer(
        float pitch,
        float roll,
        float lift,
        float sure,
        float darbeGucu)
    {
        targetPitch =
            Mathf.Abs(pitch) >
            Mathf.Abs(targetPitch)
                ? pitch
                : targetPitch;

        targetRoll =
            Mathf.Abs(roll) >
            Mathf.Abs(targetRoll)
                ? roll
                : targetRoll;

        targetLift =
            Mathf.Max(
                targetLift,
                Mathf.Abs(lift));

        duration =
            Mathf.Max(
                0.10f,
                sure);

        remaining =
            Mathf.Max(
                remaining,
                duration);

        power =
            Mathf.Max(
                power,
                Mathf.Clamp01(
                    darbeGucu));
    }

    private void LateUpdate()
    {
        // Once kendi gecen-frame katmanimizi geri al.
        Quaternion baseRotation =
            transform.localRotation *
            Quaternion.Inverse(
                lastRotationOffset);

        Vector3 basePosition =
            transform.localPosition -
            lastPositionOffset;

        lastRotationOffset =
            Quaternion.identity;

        lastPositionOffset =
            Vector3.zero;

        if (remaining <= 0f)
        {
            transform.localRotation =
                baseRotation;

            transform.localPosition =
                basePosition;

            targetPitch = 0f;
            targetRoll = 0f;
            targetLift = 0f;
            power = 0f;

            return;
        }

        remaining -=
            Time.deltaTime;

        float normalized =
            1f -
            Mathf.Clamp01(
                remaining /
                Mathf.Max(
                    0.001f,
                    duration));

        // Ilk karede tam darbe, sonra hizla sonen yay.
        float decay =
            Mathf.Exp(
                -3.6f *
                normalized);

        float rebound =
            Mathf.Sin(
                normalized *
                Mathf.PI *
                3.0f) *
            sekmeOrani *
            Mathf.Exp(
                -2.6f *
                normalized);

        float pose =
            Mathf.Clamp(
                decay +
                rebound,
                -0.45f,
                1.15f);

        float pitch =
            targetPitch *
            pose;

        float roll =
            targetRoll *
            pose;

        float lift =
            targetLift *
            Mathf.Max(
                0f,
                pose) *
            Mathf.Lerp(
                0.75f,
                1f,
                power);

        lastRotationOffset =
            Quaternion.Euler(
                pitch,
                0f,
                roll);

        lastPositionOffset =
            Vector3.up *
            lift;

        transform.localRotation =
            baseRotation *
            lastRotationOffset;

        transform.localPosition =
            basePosition +
            lastPositionOffset;

        if (remaining <= 0f)
        {
            targetPitch = 0f;
            targetRoll = 0f;
            targetLift = 0f;
            power = 0f;
        }
    }

    private void OnDisable()
    {
        transform.localRotation =
            transform.localRotation *
            Quaternion.Inverse(
                lastRotationOffset);

        transform.localPosition =
            transform.localPosition -
            lastPositionOffset;

        lastRotationOffset =
            Quaternion.identity;

        lastPositionOffset =
            Vector3.zero;

        remaining = 0f;
    }
}
