using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// GLS580KameraSistemiV6 LateUpdate execution order = 32700.
// Bu katman ONDAN SONRA calisir ve o frame'in kamera sonucuna
// sadece kozmetik / GTA tarzi ek hareket uygular.
[DefaultExecutionOrder(35500)]
public class GLS580KameraHissi : MonoBehaviour
{
    [Header("PROJENE GORE OTOMATIK BULUR")]
    [Tooltip("Bos birakabilirsin. GLS580KameraSistemiV6 otomatik bulunur.")]
    public MonoBehaviour kameraSistemi;

    [Tooltip("Bos birakabilirsin. GLS580BasitSistem otomatik bulunur.")]
    public MonoBehaviour basitSistem;

    [Tooltip("Bos birakabilirsin. BasitSistem.driveRoot otomatik okunur.")]
    public Transform vehicleRoot;

    [Tooltip("Bos birakabilirsin. BasitSistem playerRoot otomatik okunur.")]
    public Transform playerRoot;

    [Header("1) HIZLANMA / FREN KAMERA SPRING")]
    public bool hizlanmaFrenSpringAktif = true;

    [Tooltip("Gazda dis kamera aracın gerisinde ne kadar kalsin.")]
    public float gazdaGerideKalmaMesafesi = 0.20f;

    [Tooltip("Sert frende dis kamera ne kadar one gelsin.")]
    public float frendeOneGelmeMesafesi = 0.25f;

    [Tooltip("Ic kamera icin gaz/fren hareketi daha kucuk tutulur.")]
    public float icKameraSpringCarpani = 0.22f;

    [Tooltip("Tam gaz kamera tepkisi icin gereken ivme.")]
    public float gazTamIvme = 5.0f;

    [Tooltip("Tam fren kamera tepkisi icin gereken negatif ivme.")]
    public float frenTamIvme = 7.0f;

    public float springYumusaklik = 7.5f;

    [Tooltip("Sert frende kameraya cok hafif pitch.")]
    public float frenPitchDerece = 1.15f;

    [Header("2) GTA DRIFT KAMERASI")]
    public bool driftKameraAktif = true;

    [Tooltip("Aracin baktigi yon ile gercek hareket yonu arasindaki acinin ne kadari kameraya yansisin.")]
    [Range(0f, 1.25f)]
    public float driftTakipOrani = 0.72f;

    [Tooltip("Kamera driftte en fazla bu kadar yan aci alsin.")]
    public float maksimumDriftKameraAcisi = 24f;

    [Tooltip("Bu slip acisindan sonra kamera drift moduna belirgin girer.")]
    public float driftBaslangicSlipAcisi = 4.5f;

    [Tooltip("Drift yonune giris yumusakligi.")]
    public float driftGirisYumusaklik = 7.5f;

    [Tooltip("Drift bitince kamera eski yerine donme yumusakligi.")]
    public float driftCikisYumusaklik = 5.5f;

    [Tooltip("Counter-steer verildiginde kamera daha hizli toplansin.")]
    public float counterSteerKameraToparlama = 11f;

    [Tooltip("Space basiliyken slip dusuk olsa bile drift kamera etkisine izin ver.")]
    public bool spaceDriftDestegi = true;

    [Tooltip("Normal virajda slip kamerayi saga-sola savurmasin; drift orbit yalniz Space ile calissin.")]
    public bool driftSadeceElFreninde = true;

    [Tooltip("Drift kamera acisinin bir saniyede degisebilecegi maksimum miktar.")]
    public float maksimumDriftYawDegisimHizi = 28f;

    [Header("3) VIRAJ / DIREKSIYON BAKISI")]
    public bool virajKameraAktif = true;

    [Tooltip("A/D ile kameranin aracin etrafinda hafif yan aci almasi.")]
    public float maksimumVirajKameraAcisi = 5.5f;

    [Tooltip("Virajin ilerisine bakma mesafesi.")]
    public float virajBakisYanOffset = 0.38f;

    [Tooltip("Hiz arttikca yolun daha ilerisine bakar.")]
    public float maksimumHizBakisMesafesi = 1.75f;

    public float virajYumusaklik = 6.5f;

    [Tooltip("Direksiyon kamera acisinin bir saniyede degisebilecegi maksimum miktar.")]
    public float maksimumVirajYawDegisimHizi = 10f;

    [Tooltip("Bu hizdan sonra viraj kamera orbiti azalir.")]
    public float yuksekHizStabilizasyonBaslangicKmh = 65f;

    [Tooltip("Bu hizda viraj kamera orbiti minimuma iner.")]
    public float yuksekHizStabilizasyonTamKmh = 135f;

    [Tooltip("Kamera A/D tarafinda ters hareket ederse bunu ac.")]
    public bool virajYonuTers = false;

    [Header("4) OTOMATIK ARKAYA MERKEZLEME")]
    public bool otomatikMerkezlemeAktif = true;

    [Tooltip("Fareyi biraktiktan kac saniye sonra arkaya donmeye baslasin.")]
    public float merkezlemeBeklemeSuresi = 1.7f;

    [Tooltip("Arkaya donus hizi.")]
    public float merkezlemeHizi = 2.5f;

    [Tooltip("Mouse hareketini algilama esigi.")]
    public float mouseHareketEsigi = 0.015f;

    [Tooltip("Merkez yaw. V6 sisteminde 0 direkt arka demektir.")]
    public float merkezYaw = 0f;

    [Header("4B) GERI VITES KAMERA ODAK")]
    [Tooltip("Geri giderken fare kullanilmiyorsa kamera aracin gerisini gostersin.")]
    public bool geriVitesteArkayaOdaklan = true;

    [Tooltip("V6 sisteminde aracin gerisine bakis yaw'i. Genellikle 180.")]
    public float geriVitesteMerkezYaw = 180f;

    [Tooltip("Geri viteste fare birakilinca odaklanmadan once beklenecek sure.")]
    public float geriVitesteMerkezlemeBekleme = 0.18f;

    [Tooltip("Geri viteste arkaya donus hizi.")]
    public float geriVitesteMerkezlemeHizi = 5.5f;

    [Tooltip("Bu geri hizindan sonra geri kamera odagi devreye girer (km/h).")]
    public float geriOdakMinimumKmh = 1.2f;

    [Header("5) KAMERA COLLISION")]
    public bool kameraCollisionAktif = true;

    [Tooltip("Kamera duvarlara bu yaricapta sphere-cast yapar.")]
    public float collisionYaricapi = 0.24f;

    [Tooltip("Duvara yapismasin diye ekstra pay.")]
    public float collisionDuvarPayi = 0.075f;

    [Tooltip("Kamera araca bundan daha fazla yaklasmasin.")]
    public float minimumKameraMesafesi = 1.05f;

    [Tooltip("Duvar bittikten sonra eski mesafeye donme hizi (metre/sn).")]
    public float collisionGeriAcilmaHizi = 7.5f;

    public LayerMask cameraCollisionMask = ~0;

    [Tooltip("Trigger alanlarini kamera duvari sayma.")]
    public bool triggerlariYoksay = true;

    [Header("6) KASIS / DIKEY GECIKME")]
    public bool kasisKameraAktif = true;

    [Tooltip("Arac yukari cikarken kamera hafif geride/asagida kalir.")]
    public float dikeyHizKameraCarpani = 0.035f;

    [Tooltip("Maksimum dikey kamera offseti.")]
    public float maksimumDikeyOffset = 0.11f;

    public float dikeyYumusaklik = 7f;

    [Header("7) YUKSEK HIZ MIKRO HAREKET")]
    public bool yuksekHizMikroHareketAktif = true;

    [Tooltip("Mikro hareket bu hizdan sonra baslar.")]
    public float mikroHareketBaslangicKmh = 85f;

    [Tooltip("Bu hizda maksimum olur.")]
    public float mikroHareketTamKmh = 180f;

    [Tooltip("Dis kamerada maksimum pozisyon titremesi.")]
    public float mikroPozisyon = 0.014f;

    [Tooltip("Dis kamerada maksimum rotasyon titremesi derece.")]
    public float mikroRotasyonDerece = 0.28f;

    [Tooltip("Ic kamera mikro hareket carpani.")]
    public float icKameraMikroCarpani = 0.48f;

    public float mikroFrekans = 18f;

    [Header("8) GENEL")]
    [Tooltip("Efektler sadece arac icindeyken calisir.")]
    public bool sadeceAracIcinde = true;

    [Tooltip("Dis kameralarda efekt siddeti.")]
    [Range(0f, 2f)]
    public float disKameraGenelCarpan = 1f;

    [Tooltip("Ic goz kamerasinda efekt siddeti.")]
    [Range(0f, 2f)]
    public float icKameraGenelCarpan = 0.65f;

    [Header("DEBUG - PLAY MODDA")]
    [SerializeField] private bool playerInside;
    [SerializeField] private bool busy;
    [SerializeField] private int aktifKameraIndex = -1;

    [SerializeField] private float signedSpeedMps;
    [SerializeField] private float speedKmh;
    [SerializeField] private float longitudinalAcceleration;
    [SerializeField] private float verticalVelocity;

    [SerializeField] private float steerNormalized;
    [SerializeField] private float slipAngle;
    [SerializeField] private float driftCameraYaw;
    [SerializeField] private float virajCameraYaw;
    [SerializeField] private float springOffset;

    [SerializeField] private bool counterSteering;
    [SerializeField] private bool collisionBulundu;
    [SerializeField] private string collisionObje = "-";

    [SerializeField] private float kamera1CollisionMesafe;
    [SerializeField] private float kamera2CollisionMesafe;
    [SerializeField] private float kamera3CollisionMesafe;

    [SerializeField] private string bulunanKameraSistemi = "-";
    [SerializeField] private string bulunanDriveRoot = "-";

    // ============================================================
    // BASIT SISTEM REFLECTION
    // ============================================================
    private Type basitType;
    private FieldInfo fPlayerInside;
    private FieldInfo fBusy;
    private FieldInfo fDriveRoot;
    private FieldInfo fPlayerRoot;
    private FieldInfo fCurrentDriveSpeed;
    private FieldInfo fCurrentSteerAngle;

    // ============================================================
    // KAMERA V6 REFLECTION
    // ============================================================
    private Type kameraType;

    private FieldInfo fAracKamera1;
    private FieldInfo fAracKamera2;
    private FieldInfo fAracKamera3;
    private FieldInfo fAracGozKamerasi;

    private FieldInfo fAracTakipRoot;
    private FieldInfo fDisKameraHedef;
    private FieldInfo fDisKameraHedefOffset;

    private FieldInfo fAktifAracKameraIndex;
    private FieldInfo fDisYaw;

    private Camera aracKamera1;
    private Camera aracKamera2;
    private Camera aracKamera3;
    private Camera aracGozKamerasi;

    private Transform kameraTakipRoot;
    private Transform disKameraHedef;
    private Vector3 disKameraHedefOffset =
        new Vector3(
            0f,
            1.35f,
            0f);

    // ============================================================
    // MOTION STATE
    // ============================================================
    private Vector3 lastVehiclePosition;
    private bool lastPositionReady;

    private Vector3 smoothedWorldVelocity;
    private float previousSignedSpeed;
    private bool speedReady;

    private float smoothedAcceleration;
    private float smoothedVerticalVelocity;

    private float currentSpringOffset;
    private float currentVerticalOffset;
    private float currentVirajYaw;
    private float currentDriftYaw;
    private float smoothedCameraSteer;

    private float lastMouseMoveTime = -999f;
    private float noiseSeed;

    // Camera collision state
    private class CameraCollisionState
    {
        public Camera camera;
        public float currentDistance = -1f;
    }

    private readonly Dictionary<Camera, CameraCollisionState>
        collisionStates =
            new Dictionary<Camera, CameraCollisionState>();

    private void Awake()
    {
        noiseSeed =
            UnityEngine.Random.Range(
                0f,
                1000f);

        OtomatikBul();
        HareketBaslat();
    }

    private void Start()
    {
        OtomatikBul();
        HareketBaslat();
    }

    private void Update()
    {
        OtomatikBul();
        DurumlariOku();
        HareketVerisiniGuncelle();
        MouseVeMerkezlemeGuncelle();
    }

    private void LateUpdate()
    {
        OtomatikBul();
        DurumlariOku();

        if (sadeceAracIcinde &&
            !playerInside)
        {
            return;
        }

        EfektDegerleriniGuncelle();

        // 0-2 dis kamera, 3 goz kamera.
        if (playerInside)
        {
            DisKameraEfektleriniUygula();

            if (aktifKameraIndex == 3)
            {
                IcKameraEfektiniUygula();
            }
        }
    }

    // ============================================================
    // MOTION DATA
    // ============================================================
    private void HareketBaslat()
    {
        if (vehicleRoot == null)
            return;

        lastVehiclePosition =
            vehicleRoot.position;

        lastPositionReady =
            true;

        previousSignedSpeed =
            CurrentDriveSpeedOku();

        speedReady =
            true;
    }

    private void HareketVerisiniGuncelle()
    {
        if (vehicleRoot == null)
            return;

        float dt =
            Mathf.Max(
                Time.deltaTime,
                0.0001f);

        Vector3 currentPosition =
            vehicleRoot.position;

        if (!lastPositionReady)
        {
            lastVehiclePosition =
                currentPosition;

            lastPositionReady =
                true;
        }

        Vector3 delta =
            currentPosition -
            lastVehiclePosition;

        lastVehiclePosition =
            currentPosition;

        Vector3 rawVelocity =
            delta /
            dt;

        float velBlend =
            1f -
            Mathf.Exp(
                -9f *
                dt);

        smoothedWorldVelocity =
            Vector3.Lerp(
                smoothedWorldVelocity,
                rawVelocity,
                velBlend);

        signedSpeedMps =
            CurrentDriveSpeedOku();

        speedKmh =
            Mathf.Abs(
                signedSpeedMps) *
            3.6f;

        if (!speedReady)
        {
            previousSignedSpeed =
                signedSpeedMps;

            speedReady =
                true;
        }

        float rawAcceleration =
            (signedSpeedMps -
             previousSignedSpeed) /
            dt;

        previousSignedSpeed =
            signedSpeedMps;

        rawAcceleration =
            Mathf.Clamp(
                rawAcceleration,
                -18f,
                14f);

        smoothedAcceleration =
            Mathf.Lerp(
                smoothedAcceleration,
                rawAcceleration,
                1f -
                Mathf.Exp(
                    -8f *
                    dt));

        longitudinalAcceleration =
            smoothedAcceleration;

        Vector3 up =
            vehicleRoot.up.sqrMagnitude >
            0.001f
                ? vehicleRoot.up.normalized
                : Vector3.up;

        float rawVerticalVelocity =
            Vector3.Dot(
                smoothedWorldVelocity,
                up);

        smoothedVerticalVelocity =
            Mathf.Lerp(
                smoothedVerticalVelocity,
                rawVerticalVelocity,
                1f -
                Mathf.Exp(
                    -6f *
                    dt));

        verticalVelocity =
            smoothedVerticalVelocity;

        steerNormalized =
            SteeringOkuNormalized();

        SlipAcisiniGuncelle(
            up);
    }

    private void SlipAcisiniGuncelle(
        Vector3 up)
    {
        if (vehicleRoot == null)
        {
            slipAngle = 0f;
            return;
        }

        Vector3 forward =
            Vector3.ProjectOnPlane(
                vehicleRoot.forward,
                up);

        Vector3 velocity =
            Vector3.ProjectOnPlane(
                smoothedWorldVelocity,
                up);

        if (forward.sqrMagnitude <
                0.001f ||
            velocity.sqrMagnitude <
                0.20f)
        {
            slipAngle = 0f;
            return;
        }

        forward.Normalize();
        velocity.Normalize();

        slipAngle =
            Vector3.SignedAngle(
                forward,
                velocity,
                up);

        slipAngle =
            Mathf.Clamp(
                slipAngle,
                -55f,
                55f);
    }

    // ============================================================
    // EFFECT VALUES
    // ============================================================
    private void EfektDegerleriniGuncelle()
    {
        float dt =
            Mathf.Max(
                Time.deltaTime,
                0.0001f);

        // --------------------------------------------------------
        // Accel / brake spring
        // --------------------------------------------------------
        float targetSpring = 0f;

        if (hizlanmaFrenSpringAktif)
        {
            if (longitudinalAcceleration >= 0f)
            {
                float accel01 =
                    Mathf.Clamp01(
                        longitudinalAcceleration /
                        Mathf.Max(
                            0.1f,
                            gazTamIvme));

                // Negative => camera geriye.
                targetSpring =
                    -gazdaGerideKalmaMesafesi *
                    accel01;
            }
            else
            {
                float brake01 =
                    Mathf.Clamp01(
                        -longitudinalAcceleration /
                        Mathf.Max(
                            0.1f,
                            frenTamIvme));

                // Positive => camera one.
                targetSpring =
                    frendeOneGelmeMesafesi *
                    brake01;
            }
        }

        currentSpringOffset =
            Mathf.Lerp(
                currentSpringOffset,
                targetSpring,
                1f -
                Mathf.Exp(
                    -springYumusaklik *
                    dt));

        springOffset =
            currentSpringOffset;

        // --------------------------------------------------------
        // Bump / vertical lag
        // --------------------------------------------------------
        float targetVertical = 0f;

        if (kasisKameraAktif)
        {
            targetVertical =
                Mathf.Clamp(
                    -verticalVelocity *
                    dikeyHizKameraCarpani,
                    -maksimumDikeyOffset,
                    maksimumDikeyOffset);
        }

        currentVerticalOffset =
            Mathf.Lerp(
                currentVerticalOffset,
                targetVertical,
                1f -
                Mathf.Exp(
                    -dikeyYumusaklik *
                    dt));

        // --------------------------------------------------------
        // Drift camera
        // --------------------------------------------------------
        bool handbrake =
            Input.GetKey(
                KeyCode.Space);

        float absSlip =
            Mathf.Abs(
                slipAngle);

        float driftWeight =
            Mathf.InverseLerp(
                driftBaslangicSlipAcisi,
                Mathf.Max(
                    driftBaslangicSlipAcisi +
                    0.1f,
                    22f),
                absSlip);

        if (spaceDriftDestegi &&
            handbrake &&
            speedKmh > 12f)
        {
            driftWeight =
                Mathf.Max(
                    driftWeight,
                0.32f);
        }

        bool driftKamerasiIzinli =
            !driftSadeceElFreninde || handbrake;

        if (!driftKamerasiIzinli)
            driftWeight = 0f;

        float targetDriftYaw =
            driftKameraAktif &&
            driftKamerasiIzinli
                ? Mathf.Clamp(
                    slipAngle *
                    driftTakipOrani *
                    driftWeight,
                    -maksimumDriftKameraAcisi,
                    maksimumDriftKameraAcisi)
                : 0f;

        counterSteering =
            Mathf.Abs(
                steerNormalized) >
                0.05f &&
            Mathf.Abs(
                slipAngle) >
                driftBaslangicSlipAcisi &&
            Mathf.Sign(
                steerNormalized) !=
            Mathf.Sign(
                slipAngle);

        float driftSmooth =
            counterSteering
                ? counterSteerKameraToparlama
                : (
                    Mathf.Abs(
                        targetDriftYaw) >
                    Mathf.Abs(
                        currentDriftYaw)
                        ? driftGirisYumusaklik
                        : driftCikisYumusaklik
                  );

        float driftYumusatilmisHedef =
            Mathf.Lerp(
                currentDriftYaw,
                targetDriftYaw,
                1f - Mathf.Exp(-driftSmooth * dt));

        // Slip isareti tek karede ters donse bile kamera diger tarafa
        // ziplayamaz. Boylece arabanin arkasi ekranda saga-sola atmaz.
        currentDriftYaw =
            Mathf.MoveTowards(
                currentDriftYaw,
                driftYumusatilmisHedef,
                Mathf.Max(1f, maksimumDriftYawDegisimHizi) * dt);

        driftCameraYaw =
            currentDriftYaw;

        // --------------------------------------------------------
        // Steering / corner camera
        // --------------------------------------------------------
        float speedFactor =
            Mathf.Clamp01(
                speedKmh /
                75f);

        smoothedCameraSteer =
            Mathf.Lerp(
                smoothedCameraSteer,
                steerNormalized,
                1f - Mathf.Exp(-3.2f * dt));

        float yuksekHizStabilitesi =
            Mathf.Lerp(
                1f,
                0.12f,
                Mathf.InverseLerp(
                    yuksekHizStabilizasyonBaslangicKmh,
                    Mathf.Max(
                        yuksekHizStabilizasyonBaslangicKmh + 1f,
                        yuksekHizStabilizasyonTamKmh),
                    speedKmh));

        float virajSign =
            virajYonuTers
                ? 1f
                : -1f;

        float driftSuppress =
            Mathf.Lerp(
                1f,
                0.25f,
                driftWeight);

        float targetVirajYaw =
            virajKameraAktif
                ? smoothedCameraSteer *
                  maksimumVirajKameraAcisi *
                  virajSign *
                  speedFactor *
                  driftSuppress *
                  yuksekHizStabilitesi
                : 0f;

        float virajYumusatilmisHedef =
            Mathf.Lerp(
                currentVirajYaw,
                targetVirajYaw,
                1f - Mathf.Exp(-virajYumusaklik * dt));

        currentVirajYaw =
            Mathf.MoveTowards(
                currentVirajYaw,
                virajYumusatilmisHedef,
                Mathf.Max(1f, maksimumVirajYawDegisimHizi) * dt);

        virajCameraYaw =
            currentVirajYaw;
    }

    // ============================================================
    // EXTERNAL CAMERAS
    // ============================================================
    private void DisKameraEfektleriniUygula()
    {
        Vector3 target =
            DisKameraHedefPozisyonu();

        Transform root =
            KameraTakipRoot();

        if (root == null)
            root =
                vehicleRoot;

        if (root == null)
            return;

        Vector3 up =
            root.up.sqrMagnitude >
            0.001f
                ? root.up.normalized
                : Vector3.up;

        Vector3 forward =
            Vector3.ProjectOnPlane(
                root.forward,
                up);

        if (forward.sqrMagnitude <
            0.001f)
            forward =
                root.forward;

        forward.Normalize();

        Vector3 right =
            Vector3.Cross(
                up,
                forward).normalized;

        Vector3 velocityPlanar =
            Vector3.ProjectOnPlane(
                smoothedWorldVelocity,
                up);

        Vector3 movementDirection =
            velocityPlanar.sqrMagnitude >
            0.20f
                ? velocityPlanar.normalized
                : (
                    signedSpeedMps >= 0f
                        ? forward
                        : -forward
                  );

        // Normal/yuksek hizli virajda olculen velocity yonu kucuk fizik
        // salinimlari tasir. Kameranin bakis hedefini arac yonune yaklastir;
        // el frenli driftte gercek kayma yonune daha fazla izin ver.
        Vector3 aracSeyirYonu =
            signedSpeedMps >= 0f ? forward : -forward;

        bool elFrenliDrift =
            Input.GetKey(KeyCode.Space) && speedKmh > 12f;

        float hareketYonuTakipOrani =
            elFrenliDrift
                ? 0.58f
                : Mathf.Lerp(
                    0.22f,
                    0.06f,
                    Mathf.InverseLerp(65f, 135f, speedKmh));

        movementDirection =
            Vector3.Slerp(
                aracSeyirYonu,
                movementDirection,
                hareketYonuTakipOrani).normalized;

        float speed01 =
            Mathf.Clamp01(
                speedKmh /
                120f);

        float lookAhead =
            maksimumHizBakisMesafesi *
            speed01;

        float steerLook =
            virajBakisYanOffset *
            steerNormalized *
            Mathf.Clamp01(
                speedKmh /
                55f);

        Vector3 lookTarget =
            target +
            movementDirection *
            lookAhead +
            right *
            steerLook;

        float yawOffset =
            (
                currentDriftYaw +
                currentVirajYaw
            ) *
            disKameraGenelCarpan;

        UygulaDisKamera(
            aracKamera1,
            target,
            lookTarget,
            yawOffset,
            forward,
            up,
            0);

        UygulaDisKamera(
            aracKamera2,
            target,
            lookTarget,
            yawOffset,
            forward,
            up,
            1);

        UygulaDisKamera(
            aracKamera3,
            target,
            lookTarget,
            yawOffset,
            forward,
            up,
            2);
    }

    private void UygulaDisKamera(
        Camera cam,
        Vector3 target,
        Vector3 lookTarget,
        float yawOffset,
        Vector3 forward,
        Vector3 up,
        int cameraIndex)
    {
        if (cam == null)
            return;

        Vector3 basePosition =
            cam.transform.position;

        Vector3 fromTarget =
            basePosition -
            target;

        if (fromTarget.sqrMagnitude <
            0.01f)
            return;

        // GTA drift + viraj orbit offset
        Quaternion extraYaw =
            Quaternion.AngleAxis(
                yawOffset,
                up);

        Vector3 desiredPosition =
            target +
            extraYaw *
            fromTarget;

        // Accel / brake spring
        desiredPosition +=
            forward *
            currentSpringOffset *
            disKameraGenelCarpan;

        // Bump lag
        desiredPosition +=
            up *
            currentVerticalOffset *
            disKameraGenelCarpan;

        // Camera collision
        desiredPosition =
            KameraCollisionUygula(
                cam,
                target,
                desiredPosition,
                cameraIndex);

        Vector3 look =
            lookTarget -
            desiredPosition;

        Quaternion desiredRotation =
            look.sqrMagnitude >
            0.0001f
                ? Quaternion.LookRotation(
                    look.normalized,
                    up)
                : cam.transform.rotation;

        // Brake pitch
        if (hizlanmaFrenSpringAktif &&
            longitudinalAcceleration <
            0f)
        {
            float brake01 =
                Mathf.Clamp01(
                    -longitudinalAcceleration /
                    Mathf.Max(
                        0.1f,
                        frenTamIvme));

            desiredRotation *=
                Quaternion.Euler(
                    frenPitchDerece *
                    brake01 *
                    disKameraGenelCarpan,
                    0f,
                    0f);
        }

        // High-speed micro movement
        MikroHareketUygula(
            ref desiredPosition,
            ref desiredRotation,
            disKameraGenelCarpan);

        cam.transform.position =
            desiredPosition;

        cam.transform.rotation =
            desiredRotation;
    }

    // ============================================================
    // CAMERA COLLISION
    // ============================================================
    private Vector3 KameraCollisionUygula(
        Camera cam,
        Vector3 target,
        Vector3 desiredPosition,
        int cameraIndex)
    {
        if (!kameraCollisionAktif ||
            cam == null)
        {
            return desiredPosition;
        }

        Vector3 delta =
            desiredPosition -
            target;

        float desiredDistance =
            delta.magnitude;

        if (desiredDistance <
            0.001f)
            return desiredPosition;

        Vector3 direction =
            delta /
            desiredDistance;

        CameraCollisionState state =
            CollisionStateAl(
                cam,
                desiredDistance);

        float safeDistance =
            desiredDistance;

        Collider hitCollider = null;

        RaycastHit[] hits =
            Physics.SphereCastAll(
                target,
                Mathf.Max(
                    0.01f,
                    collisionYaricapi),
                direction,
                desiredDistance,
                cameraCollisionMask,
                triggerlariYoksay
                    ? QueryTriggerInteraction.Ignore
                    : QueryTriggerInteraction.Collide);

        float nearest =
            float.MaxValue;

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider col =
                hits[i].collider;

            if (!GecerliKameraEngeli(
                    col))
                continue;

            if (hits[i].distance <
                nearest)
            {
                nearest =
                    hits[i].distance;

                hitCollider =
                    col;
            }
        }

        if (hitCollider != null)
        {
            collisionBulundu =
                true;

            collisionObje =
                hitCollider.name;

            safeDistance =
                Mathf.Max(
                    minimumKameraMesafesi,
                    nearest -
                    collisionYaricapi -
                    collisionDuvarPayi);

            // Duvara girme aninda anlik yaklas.
            state.currentDistance =
                Mathf.Min(
                    state.currentDistance,
                    safeDistance);
        }
        else
        {
            // Duvar bitince yavasca geri acil.
            state.currentDistance =
                Mathf.MoveTowards(
                    state.currentDistance,
                    desiredDistance,
                    collisionGeriAcilmaHizi *
                    Time.deltaTime);
        }

        state.currentDistance =
            Mathf.Clamp(
                state.currentDistance,
                minimumKameraMesafesi,
                desiredDistance);

        if (cameraIndex == 0)
            kamera1CollisionMesafe =
                state.currentDistance;

        if (cameraIndex == 1)
            kamera2CollisionMesafe =
                state.currentDistance;

        if (cameraIndex == 2)
            kamera3CollisionMesafe =
                state.currentDistance;

        return
            target +
            direction *
            state.currentDistance;
    }

    private CameraCollisionState CollisionStateAl(
        Camera cam,
        float desiredDistance)
    {
        if (!collisionStates.TryGetValue(
                cam,
                out CameraCollisionState state))
        {
            state =
                new CameraCollisionState();

            state.camera =
                cam;

            state.currentDistance =
                desiredDistance;

            collisionStates.Add(
                cam,
                state);
        }

        if (state.currentDistance <
            0f)
        {
            state.currentDistance =
                desiredDistance;
        }

        return state;
    }

    private bool GecerliKameraEngeli(
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

        if (playerRoot != null &&
            (
                col.transform ==
                playerRoot ||
                col.transform.IsChildOf(
                    playerRoot)
            ))
        {
            return false;
        }

        return true;
    }

    // ============================================================
    // INTERNAL CAMERA
    // ============================================================
    private void IcKameraEfektiniUygula()
    {
        if (aracGozKamerasi == null ||
            vehicleRoot == null)
            return;

        float general =
            icKameraGenelCarpan;

        Vector3 up =
            vehicleRoot.up.sqrMagnitude >
            0.001f
                ? vehicleRoot.up.normalized
                : Vector3.up;

        Vector3 forward =
            vehicleRoot.forward.normalized;

        Vector3 desiredPosition =
            aracGozKamerasi.transform.position;

        Quaternion desiredRotation =
            aracGozKamerasi.transform.rotation;

        // Ic kamerada cok daha az spring.
        desiredPosition +=
            forward *
            currentSpringOffset *
            icKameraSpringCarpani *
            general;

        desiredPosition +=
            up *
            currentVerticalOffset *
            0.55f *
            general;

        if (longitudinalAcceleration <
            0f)
        {
            float brake01 =
                Mathf.Clamp01(
                    -longitudinalAcceleration /
                    Mathf.Max(
                        0.1f,
                        frenTamIvme));

            desiredRotation *=
                Quaternion.Euler(
                    frenPitchDerece *
                    0.55f *
                    brake01 *
                    general,
                    0f,
                    0f);
        }

        MikroHareketUygula(
            ref desiredPosition,
            ref desiredRotation,
            icKameraMikroCarpani *
            general);

        aracGozKamerasi.transform.position =
            desiredPosition;

        aracGozKamerasi.transform.rotation =
            desiredRotation;
    }

    // ============================================================
    // HIGH SPEED MICRO
    // ============================================================
    private void MikroHareketUygula(
        ref Vector3 position,
        ref Quaternion rotation,
        float multiplier)
    {
        if (!yuksekHizMikroHareketAktif ||
            multiplier <= 0f)
            return;

        float speed01 =
            Mathf.InverseLerp(
                mikroHareketBaslangicKmh,
                Mathf.Max(
                    mikroHareketBaslangicKmh +
                    1f,
                    mikroHareketTamKmh),
                speedKmh);

        speed01 =
            Mathf.Clamp01(
                speed01);

        if (speed01 <= 0f)
            return;

        float t =
            Time.time *
            mikroFrekans;

        float nx =
            Mathf.PerlinNoise(
                noiseSeed,
                t) *
            2f -
            1f;

        float ny =
            Mathf.PerlinNoise(
                noiseSeed + 17.3f,
                t * 1.13f) *
            2f -
            1f;

        float nz =
            Mathf.PerlinNoise(
                noiseSeed + 41.1f,
                t * 0.91f) *
            2f -
            1f;

        float amp =
            speed01 *
            multiplier;

        Transform refTransform =
            vehicleRoot != null
                ? vehicleRoot
                : transform;

        Vector3 localNoise =
            new Vector3(
                nx * 0.12f,
                ny * 0.55f,
                nz * 0.35f);

        position +=
            refTransform.TransformDirection(
                localNoise) *
            mikroPozisyon *
            amp;

        rotation *=
            Quaternion.Euler(
                ny *
                mikroRotasyonDerece *
                amp,
                nx *
                mikroRotasyonDerece *
                0.08f *
                amp,
                nz *
                mikroRotasyonDerece *
                0.15f *
                amp);
    }

    // ============================================================
    // MOUSE / AUTO CENTER
    // ============================================================
    private void MouseVeMerkezlemeGuncelle()
    {
        if (kameraSistemi == null ||
            fDisYaw == null)
            return;

        float mouseX =
            Input.GetAxis(
                "Mouse X");

        float mouseY =
            Input.GetAxis(
                "Mouse Y");

        if (Mathf.Abs(
                mouseX) >
                mouseHareketEsigi ||
            Mathf.Abs(
                mouseY) >
                mouseHareketEsigi)
        {
            lastMouseMoveTime =
                Time.time;

            return;
        }

        if (!otomatikMerkezlemeAktif ||
            !playerInside ||
            busy ||
            aktifKameraIndex < 0 ||
            aktifKameraIndex > 2)
        {
            return;
        }

        bool geriGidiyor =
            signedSpeedMps <
                -(Mathf.Max(0.1f, geriOdakMinimumKmh) / 3.6f) ||
            (
                vehicleRoot != null &&
                Vector3.Dot(
                    smoothedWorldVelocity,
                    vehicleRoot.forward) <
                    -(Mathf.Max(0.1f, geriOdakMinimumKmh) / 3.6f)
            ) ||
            (
                Input.GetAxisRaw("Vertical") < -0.15f &&
                speedKmh < 5f
            );

        float aktifBekleme =
            geriGidiyor && geriVitesteArkayaOdaklan
                ? geriVitesteMerkezlemeBekleme
                : merkezlemeBeklemeSuresi;

        if (Time.time -
                lastMouseMoveTime <
            aktifBekleme)
        {
            return;
        }

        try
        {
            float currentYaw =
                Convert.ToSingle(
                    fDisYaw.GetValue(
                        kameraSistemi));

            float hedefMerkezYaw =
                geriGidiyor && geriVitesteArkayaOdaklan
                    ? geriVitesteMerkezYaw
                    : merkezYaw;

            float aktifMerkezlemeHizi =
                geriGidiyor && geriVitesteArkayaOdaklan
                    ? geriVitesteMerkezlemeHizi
                    : merkezlemeHizi;

            float targetYaw =
                Mathf.LerpAngle(
                    currentYaw,
                    hedefMerkezYaw,
                    1f -
                    Mathf.Exp(
                        -aktifMerkezlemeHizi *
                        Time.deltaTime));

            fDisYaw.SetValue(
                kameraSistemi,
                targetYaw);
        }
        catch { }
    }

    // ============================================================
    // CAMERA V6 DATA
    // ============================================================
    private Vector3 DisKameraHedefPozisyonu()
    {
        if (disKameraHedef != null)
            return disKameraHedef.position;

        Transform root =
            KameraTakipRoot();

        if (root != null)
        {
            return
                root.TransformPoint(
                    disKameraHedefOffset);
        }

        if (vehicleRoot != null)
        {
            return
                vehicleRoot.TransformPoint(
                    disKameraHedefOffset);
        }

        return transform.position;
    }

    private Transform KameraTakipRoot()
    {
        if (kameraTakipRoot != null)
            return kameraTakipRoot;

        if (vehicleRoot != null)
            return vehicleRoot;

        return transform;
    }

    // ============================================================
    // AUTO FIND
    // ============================================================
    private void OtomatikBul()
    {
        BasitSistemBul();
        KameraSisteminiBul();

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
                    vehicleRoot =
                        root;
            }
            catch { }
        }

        if (basitSistem != null &&
            fPlayerRoot != null &&
            playerRoot == null)
        {
            try
            {
                Transform p =
                    fPlayerRoot.GetValue(
                        basitSistem)
                    as Transform;

                if (p != null)
                    playerRoot =
                        p;
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

        if (playerRoot == null)
        {
            GameObject p =
                GameObject.Find(
                    "Ch31_nonPBR (1)");

            if (p == null)
            {
                p =
                    GameObject.Find(
                        "Ch31_nonPBR");
            }

            if (p != null)
                playerRoot =
                    p.transform;
        }

        KameraAlanlariniOku();

        bulunanKameraSistemi =
            kameraSistemi != null
                ? kameraSistemi.GetType().Name
                : "BULUNAMADI";

        bulunanDriveRoot =
            vehicleRoot != null
                ? vehicleRoot.name
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
                 i < all.Length;
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

        Type t =
            basitSistem.GetType();

        if (basitType ==
            t)
            return;

        basitType =
            t;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        fPlayerInside =
            t.GetField(
                "playerInside",
                flags);

        fBusy =
            t.GetField(
                "busy",
                flags);

        fDriveRoot =
            t.GetField(
                "driveRoot",
                flags);

        fPlayerRoot =
            t.GetField(
                "playerRoot",
                flags);

        if (fPlayerRoot == null)
        {
            fPlayerRoot =
                t.GetField(
                    "player",
                    flags);
        }

        fCurrentDriveSpeed =
            t.GetField(
                "currentDriveSpeed",
                flags);

        fCurrentSteerAngle =
            t.GetField(
                "currentSteerAngle",
                flags);
    }

    private void KameraSisteminiBul()
    {
        if (kameraSistemi == null)
        {
            MonoBehaviour[] all =
                FindObjectsOfType<
                    MonoBehaviour>(
                        true);

            for (int i = 0;
                 i < all.Length;
                 i++)
            {
                MonoBehaviour mb =
                    all[i];

                if (mb != null &&
                    mb.GetType().Name ==
                    "GLS580KameraSistemiV6")
                {
                    kameraSistemi =
                        mb;

                    break;
                }
            }
        }

        if (kameraSistemi == null)
            return;

        Type t =
            kameraSistemi.GetType();

        if (kameraType ==
            t)
            return;

        kameraType =
            t;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        fAracKamera1 =
            t.GetField(
                "aracKamera1",
                flags);

        fAracKamera2 =
            t.GetField(
                "aracKamera2",
                flags);

        fAracKamera3 =
            t.GetField(
                "aracKamera3",
                flags);

        fAracGozKamerasi =
            t.GetField(
                "aracGozKamerasi",
                flags);

        fAracTakipRoot =
            t.GetField(
                "aracTakipRoot",
                flags);

        fDisKameraHedef =
            t.GetField(
                "disKameraHedef",
                flags);

        fDisKameraHedefOffset =
            t.GetField(
                "disKameraHedefOffset",
                flags);

        fAktifAracKameraIndex =
            t.GetField(
                "aktifAracKameraIndex",
                flags);

        fDisYaw =
            t.GetField(
                "disYaw",
                flags);
    }

    private void KameraAlanlariniOku()
    {
        if (kameraSistemi == null)
            return;

        try
        {
            if (fAracKamera1 != null)
            {
                aracKamera1 =
                    fAracKamera1.GetValue(
                        kameraSistemi)
                    as Camera;
            }

            if (fAracKamera2 != null)
            {
                aracKamera2 =
                    fAracKamera2.GetValue(
                        kameraSistemi)
                    as Camera;
            }

            if (fAracKamera3 != null)
            {
                aracKamera3 =
                    fAracKamera3.GetValue(
                        kameraSistemi)
                    as Camera;
            }

            if (fAracGozKamerasi != null)
            {
                aracGozKamerasi =
                    fAracGozKamerasi.GetValue(
                        kameraSistemi)
                    as Camera;
            }

            if (fAracTakipRoot != null)
            {
                Transform root =
                    fAracTakipRoot.GetValue(
                        kameraSistemi)
                    as Transform;

                if (root != null)
                    kameraTakipRoot =
                        root;
            }

            if (fDisKameraHedef != null)
            {
                disKameraHedef =
                    fDisKameraHedef.GetValue(
                        kameraSistemi)
                    as Transform;
            }

            if (fDisKameraHedefOffset != null)
            {
                object value =
                    fDisKameraHedefOffset.GetValue(
                        kameraSistemi);

                if (value is Vector3)
                {
                    disKameraHedefOffset =
                        (Vector3)value;
                }
            }

            if (fAktifAracKameraIndex != null)
            {
                aktifKameraIndex =
                    Convert.ToInt32(
                        fAktifAracKameraIndex.GetValue(
                            kameraSistemi));
            }
        }
        catch { }
    }

    private void DurumlariOku()
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

        KameraAlanlariniOku();

        collisionBulundu =
            false;

        collisionObje =
            "-";
    }

    // ============================================================
    // BASIT VALUES
    // ============================================================
    private float CurrentDriveSpeedOku()
    {
        if (basitSistem == null ||
            fCurrentDriveSpeed == null)
        {
            if (vehicleRoot != null)
            {
                Vector3 planar =
                    Vector3.ProjectOnPlane(
                        smoothedWorldVelocity,
                        vehicleRoot.up);

                return
                    Vector3.Dot(
                        planar,
                        vehicleRoot.forward);
            }

            return 0f;
        }

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

    private float SteeringOkuNormalized()
    {
        if (basitSistem != null &&
            fCurrentSteerAngle != null)
        {
            try
            {
                float angle =
                    Convert.ToSingle(
                        fCurrentSteerAngle.GetValue(
                            basitSistem));

                return
                    Mathf.Clamp(
                        angle / 32f,
                        -1f,
                        1f);
            }
            catch { }
        }

        float input = 0f;

        if (Input.GetKey(
                KeyCode.A))
            input -= 1f;

        if (Input.GetKey(
                KeyCode.D))
            input += 1f;

        return input;
    }
}
