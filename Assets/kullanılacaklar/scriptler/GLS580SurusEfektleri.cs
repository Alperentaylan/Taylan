using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[DefaultExecutionOrder(32000)]
public class GLS580SurusEfektleri : MonoBehaviour
{
    [Header("ARAC - ELLE DOLDURMAK ZORUNDA DEGIL")]
    [Tooltip("Kod GLS580BasitSistem.driveRoot'u bulursa bu alani otomatik olarak GERCEK DriveRoot ile degistirir.")]
    public Transform vehicleRoot;

    [Tooltip("Kod gercek DriveRoot'taki Rigidbody'yi otomatik kullanir.")]
    public Rigidbody vehicleRigidbody;

    public Transform wheelRL;
    public Transform wheelRR;

    [Header("DRIFT - GTA TARZI")]
    public KeyCode elFreniTusu = KeyCode.Space;

    [Tooltip("Drift/iz/duman/ses icin minimum hiz.")]
    public float minimumDriftHiziKmh = 10f;

    [Tooltip("Space + A/D ilk anda govde ile gidis yonu arasinda acilan kayma acisi.")]
    [Range(0f, 30f)]
    public float driftBaslangicAcisi = 15f;

    [Tooltip("Drift tam oturdugunda maksimum kayma acisi.")]
    [Range(10f, 55f)]
    public float maksimumDriftAcisi = 38f;

    [Tooltip("A/D drift yonunde tutuldukca kayma acisinin buyume hizi.")]
    public float driftAciArtisHizi = 85f;

    [Tooltip("Ters direksiyon verince araci toplama hizi.")]
    public float counterSteerToparlama = 120f;

    [Tooltip("Direksiyon birakilinca drift aniden kesilmesin.")]
    public float direksiyonBirakincaToparlama = 20f;

    [Tooltip("Kayma sifira cok yaklasinca ters yone direksiyon tutuluyorsa drift o tarafa aktarilabilir.")]
    public bool driftYonDegistirmeAktif = true;

    [Tooltip("BasitSistem el freni fren gucu driftte buna dusurulur. Bu IVME EKLEMEZ; sadece hizi yavas yavas azaltir.")]
    public float driftHizKaybi = 0.45f;

    [Tooltip("Drift girisindeki hizi tavan yapar. Space ile hizlanmayi engeller.")]
    public bool driftGirisHiziniAsma = true;

    [Header("SESLER - INDIRDIKLERINI SURUKLE")]
    public AudioClip engineStartClip;
    public AudioClip engineLoopClip;
    public AudioClip handbrakeClip;
    [FormerlySerializedAs("tireSquealClip")]
    public AudioClip driftClip;

    [Range(0f, 1f)] public float engineStartVolume = 0.9f;
    [Range(0f, 1f)] public float engineIdleVolume = 0.32f;
    [Range(0f, 1f)] public float engineMaxVolume = 0.80f;
    [Range(0.3f, 2f)] public float engineIdlePitch = 0.82f;
    [Range(0.5f, 2.5f)] public float engineMaxPitch = 1.55f;
    public float engineMaxSpeedKmh = 180f;
    [Range(0f, 1f)] public float handbrakeVolume = 0.9f;
    [Range(0f, 1f)] public float driftSoundMaxVolume = 0.88f;

    [Header("ZEMIN")]
    public float zeminAramaMesafesi = 2.5f;
    public float rayBaslangicYuksekligi = 0.45f;
    public float fallbackTekerYaricapi = 0.38f;
    public float zeminYuzeyOffset = 0.025f;
    public LayerMask groundMask = ~0;

    [Header("LASTIK IZI")]
    public bool skidMarkAktif = true;
    public float skidMarkWidth = 0.13f;
    [Range(0f, 1f)] public float skidMarkAlpha = 0.95f;
    [FormerlySerializedAs("skidMarkTime")]
    public float skidMarkLife = 30f;
    public float skidMarkMinDistance = 0.025f;
    public int maxSkidSegments = 3000;

    [Header("DUMAN")]
    public bool tireSmokeAktif = true;
    public float smokeRate = 70f;
    public float smokeLifetimeMin = 0.70f;
    public float smokeLifetimeMax = 1.40f;
    public float smokeSizeMin = 0.22f;
    public float smokeSizeMax = 0.50f;

    [Header("GLS580 BASIT SISTEM")]
    [Tooltip("Bos birakabilirsin. Runtime'da otomatik bulunur.")]
    public MonoBehaviour basitSistem;

    [Header("KOD KONTROL")]
    [SerializeField] private string kodKontrol = "TAM SISTEM: GTA DRIFT + MOTOR + MARS + EL FRENI + DRIFT SESI + IZ + DUMAN";

    [Header("DEBUG - PLAY MODDA")]
    [SerializeField] private bool playerInside;
    [SerializeField] private bool busy;
    [SerializeField] private float speedKmh;
    [SerializeField] private float prePhysicsSpeedKmh;
    [SerializeField] private float driftGirisHiziKmh;
    [SerializeField] private float steerInput;
    [SerializeField] private bool drifting;
    [SerializeField] private bool counterSteering;
    [SerializeField] private float driftDirection;
    [SerializeField] private float driftKaymaAcisi;
    [SerializeField] private float currentDriveSpeedMps;
    [SerializeField] private bool efektAktif;
    [SerializeField] private bool solRayZeminBuldu;
    [SerializeField] private bool sagRayZeminBuldu;
    [SerializeField] private int skidSegmentSayisi;
    [SerializeField] private int aktifDumanParcacigi;
    [SerializeField] private string runtimeVehicleRoot = "-";

    // ----------------------------------------------------------------
    // BasitSistem reflection
    // ----------------------------------------------------------------
    private Type basitType;
    private FieldInfo fPlayerInside;
    private FieldInfo fBusy;
    private FieldInfo fDriveRoot;
    private FieldInfo fCurrentDriveSpeed;
    private FieldInfo fHandbrakePower;
    private FieldInfo fCurrentSteerAngle;
    private FieldInfo fWheelBase;

    // ----------------------------------------------------------------
    // PRE / POST PHYSICS DRIFT
    // ----------------------------------------------------------------
    private GLS580SurusEfektleri_PrePhysics prePhysicsHelper;

    private bool previousPhysicsHandbrake;
    private float driftEntrySpeedAbs;
    private float prePhysicsSignedSpeed;

    private Quaternion movementRotationBeforeBasit;
    private Vector3 movementUpBeforeBasit;
    private float signedSlipBeforeBasit;
    private bool temporaryMovementRotationApplied;

    private float originalHandbrakePower;
    private bool originalHandbrakePowerCaptured;
    private bool handbrakeOverrideActive;

    // ----------------------------------------------------------------
    // Transform speed fallback
    // ----------------------------------------------------------------
    private Vector3 lastVehiclePosition;
    private bool lastVehiclePositionReady;
    private float transformSpeedKmh;

    // ----------------------------------------------------------------
    // Audio
    // ----------------------------------------------------------------
    private AudioSource oneShotSource;
    private AudioSource engineSource;
    private AudioSource driftSource;
    private bool previousPlayerInside;
    private bool previousHandbrake;
    private bool engineRunning;
    private Coroutine engineStartRoutine;

    // ----------------------------------------------------------------
    // Ground
    // ----------------------------------------------------------------
    private struct GroundInfo
    {
        public Vector3 point;
        public Vector3 normal;
        public bool rayHit;
    }

    private GroundInfo leftGround;
    private GroundInfo rightGround;

    // ----------------------------------------------------------------
    // Skid mesh
    // ----------------------------------------------------------------
    private class SkidSegment
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 v3;
        public float birth;
    }

    private readonly List<SkidSegment> skidSegments =
        new List<SkidSegment>();

    private GameObject skidRoot;
    private Mesh skidMesh;
    private MeshFilter skidMeshFilter;
    private MeshRenderer skidMeshRenderer;
    private Material skidMaterial;

    private bool leftPrevValid;
    private bool rightPrevValid;
    private Vector3 leftPrevPoint;
    private Vector3 rightPrevPoint;

    // ----------------------------------------------------------------
    // Smoke
    // ----------------------------------------------------------------
    private ParticleSystem smokeSystem;
    private ParticleSystemRenderer smokeRenderer;
    private Material smokeMaterial;
    private Texture2D smokeTexture;
    private float leftSmokeAccumulator;
    private float rightSmokeAccumulator;

    private void Awake()
    {
        OtomatikBul();
        PrePhysicsHelperHazirla();
        HizFallbackBaslat();
        AudioHazirla();
        SkidSisteminiHazirla();
        DumanSisteminiHazirla();
    }

    private void Start()
    {
        OtomatikBul();
        PrePhysicsHelperHazirla();
        HizFallbackBaslat();
        AudioHazirla();
        SkidSisteminiHazirla();
        DumanSisteminiHazirla();

        DurumuOku();
        previousPlayerInside = playerInside;
    }

    private void Update()
    {
        OtomatikBul();
        DurumuOku();
        TransformHiziniGuncelle();
        DebugHiziniGuncelle();

        leftGround = ZeminBul(wheelRL);
        rightGround = ZeminBul(wheelRR);

        solRayZeminBuldu = leftGround.rayHit;
        sagRayZeminBuldu = rightGround.rayHit;

        bool handbrake =
            playerInside &&
            !busy &&
            Input.GetKey(elFreniTusu);

        efektAktif =
            handbrake &&
            speedKmh >= minimumDriftHiziKmh;

        if (playerInside && !previousPlayerInside)
            MotoruBaslat();

        if (!playerInside && previousPlayerInside)
            MotoruKapat();

        previousPlayerInside = playerInside;

        if (handbrake && !previousHandbrake)
        {
            if (handbrakeClip != null &&
                oneShotSource != null)
            {
                oneShotSource.PlayOneShot(
                    handbrakeClip,
                    handbrakeVolume);
            }
        }

        previousHandbrake = handbrake;

        MotorSesiniGuncelle();
        DriftSesiniGuncelle();
        SkidIzleriniGuncelle();
        DumanGuncelle();

        skidSegmentSayisi =
            skidSegments.Count;

        if (smokeSystem != null)
            aktifDumanParcacigi =
                smokeSystem.particleCount;
    }

    // =================================================================
    // POST PHYSICS - BASIT SISTEMDEN SONRA
    // =================================================================
    private void FixedUpdate()
    {
        OtomatikBul();
        DurumuOku();

        if (!temporaryMovementRotationApplied ||
            !drifting ||
            !playerInside ||
            busy ||
            vehicleRigidbody == null ||
            vehicleRoot == null)
        {
            temporaryMovementRotationApplied = false;
            return;
        }

        float speed =
            Mathf.Max(
                0f,
                BasitHiziniOkuMps());

        // Drift baslangic hizini TAVAN kabul et.
        if (driftGirisHiziniAsma &&
            driftEntrySpeedAbs > 0.01f)
        {
            speed =
                Mathf.Min(
                    speed,
                    driftEntrySpeedAbs);
        }

        BasitHiziniYaz(speed);

        currentDriveSpeedMps = speed;
        speedKmh = speed * 3.6f;

        float steerAngle =
            CurrentSteerAngleOku();

        float wheelBase =
            Mathf.Max(
                0.1f,
                WheelBaseOku());

        float baseYawDegrees = 0f;

        if (speed > 0.03f &&
            Mathf.Abs(steerAngle) > 0.001f)
        {
            float yawRadians =
                (speed / wheelBase) *
                Mathf.Tan(
                    steerAngle *
                    Mathf.Deg2Rad) *
                Time.fixedDeltaTime;

            baseYawDegrees =
                yawRadians *
                Mathf.Rad2Deg;
        }

        // BasitSistem araci SADECE BIR KERE MovePosition ile ilerletti.
        // Burada MovePosition YOK.
        //
        // Sadece govdeyi hareket yonunden drift acisi kadar ayiriyoruz:
        // D drift -> hareket govdenin solunda, arka taraf sola acilir.
        // A drift -> tersi.
        Quaternion movementAfterSteer =
            Quaternion.AngleAxis(
                baseYawDegrees,
                movementUpBeforeBasit) *
            movementRotationBeforeBasit;

        Quaternion bodyFinalRotation =
            Quaternion.AngleAxis(
                -signedSlipBeforeBasit,
                movementUpBeforeBasit) *
            movementAfterSteer;

        // Ikinci bir translasyon yok; sadece final body heading.
        vehicleRigidbody.MoveRotation(
            bodyFinalRotation);

        // BasitSistem yatay velocity'yi zaten temizliyor.
        // Burada ekstra velocity/force eklemiyoruz.
        temporaryMovementRotationApplied = false;
    }

    // =================================================================
    // PRE PHYSICS - BASIT SISTEMDEN ONCE
    // =================================================================
    private void PrePhysicsHelperHazirla()
    {
        if (prePhysicsHelper == null)
        {
            prePhysicsHelper =
                GetComponent<GLS580SurusEfektleri_PrePhysics>();

            if (prePhysicsHelper == null)
            {
                prePhysicsHelper =
                    gameObject.AddComponent<GLS580SurusEfektleri_PrePhysics>();
            }
        }

        prePhysicsHelper.owner = this;
    }

    internal void PrePhysicsTick()
    {
        OtomatikBul();
        DurumuOku();

        temporaryMovementRotationApplied = false;

        if (!playerInside ||
            busy ||
            vehicleRoot == null ||
            vehicleRigidbody == null)
        {
            DriftStateTemizle();
            HandbrakePowerOverride(false);
            return;
        }

        prePhysicsSignedSpeed =
            BasitHiziniOkuMps();

        prePhysicsSpeedKmh =
            Mathf.Abs(
                prePhysicsSignedSpeed) *
            3.6f;

        steerInput = 0f;

        if (Input.GetKey(KeyCode.A))
            steerInput -= 1f;

        if (Input.GetKey(KeyCode.D))
            steerInput += 1f;

        bool handbrake =
            Input.GetKey(elFreniTusu) &&
            prePhysicsSpeedKmh >= minimumDriftHiziKmh &&
            prePhysicsSignedSpeed > 0.05f;

        HandbrakePowerOverride(handbrake);

        if (!handbrake)
        {
            DriftStateTemizle();
            return;
        }

        drifting = true;

        float dt =
            Time.fixedDeltaTime;

        if (!previousPhysicsHandbrake)
        {
            driftEntrySpeedAbs =
                Mathf.Abs(
                    prePhysicsSignedSpeed);

            driftGirisHiziKmh =
                driftEntrySpeedAbs *
                3.6f;

            driftDirection =
                Mathf.Abs(steerInput) > 0.01f
                    ? Mathf.Sign(steerInput)
                    : 0f;

            driftKaymaAcisi =
                driftDirection != 0f
                    ? driftBaslangicAcisi
                    : 0f;
        }

        previousPhysicsHandbrake = true;

        // Space once basildi, sonra direksiyon verildi.
        if (driftDirection == 0f &&
            Mathf.Abs(steerInput) > 0.01f)
        {
            driftDirection =
                Mathf.Sign(steerInput);

            driftKaymaAcisi =
                Mathf.Max(
                    driftKaymaAcisi,
                    driftBaslangicAcisi);
        }

        bool sameDirection =
            driftDirection != 0f &&
            Mathf.Abs(steerInput) > 0.01f &&
            Mathf.Sign(steerInput) ==
            Mathf.Sign(driftDirection);

        bool oppositeDirection =
            driftDirection != 0f &&
            Mathf.Abs(steerInput) > 0.01f &&
            Mathf.Sign(steerInput) !=
            Mathf.Sign(driftDirection);

        counterSteering =
            oppositeDirection;

        if (sameDirection)
        {
            driftKaymaAcisi =
                Mathf.MoveTowards(
                    driftKaymaAcisi,
                    maksimumDriftAcisi,
                    driftAciArtisHizi *
                    dt);
        }
        else if (oppositeDirection)
        {
            driftKaymaAcisi =
                Mathf.MoveTowards(
                    driftKaymaAcisi,
                    0f,
                    counterSteerToparlama *
                    dt);

            // Bir drift bitince direksiyon hala ters tarafta tutuluyorsa
            // diger yone powerslide gecisi yap.
            if (driftYonDegistirmeAktif &&
                driftKaymaAcisi <= 0.5f)
            {
                driftDirection =
                    Mathf.Sign(steerInput);

                driftKaymaAcisi =
                    driftBaslangicAcisi *
                    0.55f;

                counterSteering = false;
            }
        }
        else
        {
            // Direksiyon yoksa drift yavasca toplansin, aniden snap olmasin.
            driftKaymaAcisi =
                Mathf.MoveTowards(
                    driftKaymaAcisi,
                    0f,
                    direksiyonBirakincaToparlama *
                    dt);
        }

        driftKaymaAcisi =
            Mathf.Clamp(
                driftKaymaAcisi,
                0f,
                maksimumDriftAcisi);

        // HIZ KAPAGI: BasitSistem calismadan ONCE bile giris hizini gecemez.
        if (driftGirisHiziniAsma &&
            driftEntrySpeedAbs > 0.01f &&
            prePhysicsSignedSpeed > driftEntrySpeedAbs)
        {
            prePhysicsSignedSpeed =
                driftEntrySpeedAbs;

            BasitHiziniYaz(
                driftEntrySpeedAbs);
        }

        // ----------------------------------------------------------------
        // EN ONEMLI KISIM:
        //
        // Gercek BODY heading = mevcut arac yonu.
        // Driftte hareket yonunu BODY'den ayiriyoruz.
        //
        // D (+1): signedSlip = -angle
        // hareket yonu body'nin SOLUNDA kalir.
        // BasitSistem tek MovePosition'ini bu yonle yapar.
        //
        // Sonra POST FixedUpdate body'yi tekrar +angle dondurur.
        //
        // SONUC:
        // - Tek MovePosition
        // - Ekstra mesafe yok
        // - Space basinca hiz artamaz
        // - Govde ile momentum yonu farkli -> gercek powerslide goruntusu
        // ----------------------------------------------------------------
        movementUpBeforeBasit =
            vehicleRoot.up.sqrMagnitude > 0.001f
                ? vehicleRoot.up.normalized
                : Vector3.up;

        Quaternion bodyRotation =
            vehicleRigidbody.rotation;

        signedSlipBeforeBasit =
            -driftDirection *
            driftKaymaAcisi;

        movementRotationBeforeBasit =
            Quaternion.AngleAxis(
                signedSlipBeforeBasit,
                movementUpBeforeBasit) *
            bodyRotation;

        // BasitSistem FixedUpdate'i birazdan bu rotation ile:
        // currentDriveSpeed * forward * dt kadar TEK hareket yapacak.
        vehicleRigidbody.rotation =
            movementRotationBeforeBasit;

        temporaryMovementRotationApplied =
            true;
    }

    private void DriftStateTemizle()
    {
        drifting = false;
        counterSteering = false;
        previousPhysicsHandbrake = false;
        driftDirection = 0f;
        driftKaymaAcisi = 0f;
        driftEntrySpeedAbs = 0f;
        driftGirisHiziKmh = 0f;
        temporaryMovementRotationApplied = false;
    }

    // =================================================================
    // HANDBRAKE POWER
    // =================================================================
    private void HandbrakePowerOverride(bool active)
    {
        if (basitSistem == null ||
            fHandbrakePower == null)
            return;

        try
        {
            if (!originalHandbrakePowerCaptured)
            {
                originalHandbrakePower =
                    Convert.ToSingle(
                        fHandbrakePower.GetValue(
                            basitSistem));

                originalHandbrakePowerCaptured =
                    true;
            }

            if (active)
            {
                // Negatif deger yok -> HIZLANDIRMA YOK.
                fHandbrakePower.SetValue(
                    basitSistem,
                    Mathf.Max(
                        0f,
                        driftHizKaybi));

                handbrakeOverrideActive =
                    true;
            }
            else
            {
                HandbrakePowerRestore();
            }
        }
        catch { }
    }

    internal void HandbrakePowerRestore()
    {
        if (!handbrakeOverrideActive ||
            !originalHandbrakePowerCaptured ||
            basitSistem == null ||
            fHandbrakePower == null)
            return;

        try
        {
            fHandbrakePower.SetValue(
                basitSistem,
                originalHandbrakePower);
        }
        catch { }

        handbrakeOverrideActive =
            false;
    }

    // =================================================================
    // BASIT SISTEM VALUES
    // =================================================================
    private bool BasitHiziMevcut()
    {
        return
            basitSistem != null &&
            fCurrentDriveSpeed != null;
    }

    private float BasitHiziniOkuMps()
    {
        if (!BasitHiziMevcut())
            return transformSpeedKmh / 3.6f;

        try
        {
            return Convert.ToSingle(
                fCurrentDriveSpeed.GetValue(
                    basitSistem));
        }
        catch
        {
            return transformSpeedKmh / 3.6f;
        }
    }

    private void BasitHiziniYaz(float speedMps)
    {
        if (!BasitHiziMevcut())
            return;

        try
        {
            fCurrentDriveSpeed.SetValue(
                basitSistem,
                speedMps);
        }
        catch { }
    }

    private float CurrentSteerAngleOku()
    {
        if (basitSistem == null ||
            fCurrentSteerAngle == null)
        {
            return
                steerInput * 20f;
        }

        try
        {
            return Convert.ToSingle(
                fCurrentSteerAngle.GetValue(
                    basitSistem));
        }
        catch
        {
            return
                steerInput * 20f;
        }
    }

    private float WheelBaseOku()
    {
        if (basitSistem == null ||
            fWheelBase == null)
        {
            return 2.95f;
        }

        try
        {
            return Convert.ToSingle(
                fWheelBase.GetValue(
                    basitSistem));
        }
        catch
        {
            return 2.95f;
        }
    }

    // =================================================================
    // SPEED FALLBACK / DEBUG
    // =================================================================
    private void HizFallbackBaslat()
    {
        if (vehicleRoot == null)
            return;

        lastVehiclePosition =
            vehicleRoot.position;

        lastVehiclePositionReady =
            true;
    }

    private void TransformHiziniGuncelle()
    {
        if (vehicleRoot == null)
            return;

        if (!lastVehiclePositionReady)
        {
            HizFallbackBaslat();
            return;
        }

        float dt =
            Mathf.Max(
                Time.deltaTime,
                0.0001f);

        Vector3 delta =
            vehicleRoot.position -
            lastVehiclePosition;

        lastVehiclePosition =
            vehicleRoot.position;

        float raw =
            Vector3.ProjectOnPlane(
                delta,
                Vector3.up).magnitude /
            dt *
            3.6f;

        transformSpeedKmh =
            Mathf.Lerp(
                transformSpeedKmh,
                raw,
                1f -
                Mathf.Exp(
                    -8f *
                    dt));
    }

    private void DebugHiziniGuncelle()
    {
        if (BasitHiziMevcut())
        {
            currentDriveSpeedMps =
                BasitHiziniOkuMps();

            speedKmh =
                Mathf.Abs(
                    currentDriveSpeedMps) *
                3.6f;
        }
        else
        {
            currentDriveSpeedMps =
                transformSpeedKmh /
                3.6f;

            speedKmh =
                transformSpeedKmh;
        }

        runtimeVehicleRoot =
            vehicleRoot != null
                ? vehicleRoot.name
                : "NULL";
    }

    // =================================================================
    // GROUND
    // =================================================================
    private GroundInfo ZeminBul(Transform wheel)
    {
        GroundInfo info =
            new GroundInfo();

        Vector3 up =
            vehicleRoot != null &&
            vehicleRoot.up.sqrMagnitude > 0.001f
                ? vehicleRoot.up.normalized
                : Vector3.up;

        if (wheel == null)
        {
            info.point =
                vehicleRoot != null
                    ? vehicleRoot.position
                    : transform.position;

            info.normal = up;
            info.rayHit = false;
            return info;
        }

        Vector3 origin =
            wheel.position +
            up *
            rayBaslangicYuksekligi;

        RaycastHit[] hits =
            Physics.RaycastAll(
                origin,
                -up,
                zeminAramaMesafesi,
                groundMask,
                QueryTriggerInteraction.Ignore);

        float bestDistance =
            float.MaxValue;

        RaycastHit best =
            new RaycastHit();

        bool found = false;

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider col =
                hits[i].collider;

            if (col == null)
                continue;

            if (vehicleRoot != null &&
                (
                    col.transform == vehicleRoot ||
                    col.transform.IsChildOf(
                        vehicleRoot)
                ))
            {
                continue;
            }

            if (hits[i].distance <
                bestDistance)
            {
                bestDistance =
                    hits[i].distance;

                best =
                    hits[i];

                found = true;
            }
        }

        if (found)
        {
            info.point =
                best.point +
                best.normal *
                zeminYuzeyOffset;

            info.normal =
                best.normal.normalized;

            info.rayHit = true;

            return info;
        }

        // Collider yoksa fallback.
        info.point =
            wheel.position -
            up *
            fallbackTekerYaricapi +
            up *
            zeminYuzeyOffset;

        info.normal = up;
        info.rayHit = false;

        return info;
    }

    // =================================================================
    // SKID MARK
    // =================================================================
    private void SkidSisteminiHazirla()
    {
        if (skidRoot != null)
            return;

        skidRoot =
            new GameObject(
                "_GLS580_SKIDMARK_MESH");

        skidRoot.transform.position =
            Vector3.zero;

        skidRoot.transform.rotation =
            Quaternion.identity;

        skidMeshFilter =
            skidRoot.AddComponent<MeshFilter>();

        skidMeshRenderer =
            skidRoot.AddComponent<MeshRenderer>();

        skidMesh =
            new Mesh();

        skidMesh.name =
            "GLS580_Runtime_SkidMarks";

        skidMesh.indexFormat =
            IndexFormat.UInt32;

        skidMeshFilter.sharedMesh =
            skidMesh;

        skidMaterial =
            YeniSkidMaterial();

        skidMeshRenderer.sharedMaterial =
            skidMaterial;

        skidMeshRenderer.shadowCastingMode =
            ShadowCastingMode.Off;

        skidMeshRenderer.receiveShadows =
            false;
    }

    private Material YeniSkidMaterial()
    {
        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit");

        if (shader == null)
            shader =
                Shader.Find("Unlit/Color");

        if (shader == null)
            shader =
                Shader.Find("Sprites/Default");

        Material mat =
            new Material(shader);

        Color c =
            new Color(
                0.004f,
                0.004f,
                0.004f,
                1f);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", c);

        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", 0f);

        mat.renderQueue = 2450;

        return mat;
    }

    private void SkidIzleriniGuncelle()
    {
        bool active =
            skidMarkAktif &&
            efektAktif;

        bool dirty = false;

        if (active)
        {
            dirty |=
                TekerIziEkle(
                    leftGround,
                    ref leftPrevValid,
                    ref leftPrevPoint);

            dirty |=
                TekerIziEkle(
                    rightGround,
                    ref rightPrevValid,
                    ref rightPrevPoint);
        }
        else
        {
            leftPrevValid = false;
            rightPrevValid = false;
        }

        float now =
            Time.time;

        int removeCount = 0;

        while (removeCount <
               skidSegments.Count)
        {
            if (now -
                skidSegments[removeCount].birth <=
                skidMarkLife)
            {
                break;
            }

            removeCount++;
        }

        if (removeCount > 0)
        {
            skidSegments.RemoveRange(
                0,
                removeCount);

            dirty = true;
        }

        if (skidSegments.Count >
            maxSkidSegments)
        {
            int extra =
                skidSegments.Count -
                maxSkidSegments;

            skidSegments.RemoveRange(
                0,
                extra);

            dirty = true;
        }

        if (dirty)
            SkidMeshRebuild();
    }

    private bool TekerIziEkle(
        GroundInfo ground,
        ref bool previousValid,
        ref Vector3 previousPoint)
    {
        Vector3 current =
            ground.point;

        if (!previousValid)
        {
            previousPoint = current;
            previousValid = true;
            return false;
        }

        Vector3 travel =
            current -
            previousPoint;

        float distance =
            travel.magnitude;

        if (distance <
            skidMarkMinDistance)
        {
            return false;
        }

        if (distance > 3f)
        {
            previousPoint = current;
            return false;
        }

        Vector3 normal =
            ground.normal.sqrMagnitude > 0.001f
                ? ground.normal.normalized
                : Vector3.up;

        Vector3 direction =
            travel.normalized;

        Vector3 side =
            Vector3.Cross(
                normal,
                direction);

        if (side.sqrMagnitude < 0.001f)
        {
            side =
                vehicleRoot != null
                    ? vehicleRoot.right
                    : Vector3.right;
        }

        side.Normalize();

        float half =
            skidMarkWidth *
            0.5f;

        SkidSegment seg =
            new SkidSegment();

        seg.v0 =
            previousPoint -
            side * half;

        seg.v1 =
            previousPoint +
            side * half;

        seg.v2 =
            current -
            side * half;

        seg.v3 =
            current +
            side * half;

        seg.birth =
            Time.time;

        skidSegments.Add(seg);

        previousPoint =
            current;

        return true;
    }

    private void SkidMeshRebuild()
    {
        int count =
            skidSegments.Count;

        Vector3[] vertices =
            new Vector3[count * 4];

        Color[] colors =
            new Color[count * 4];

        int[] triangles =
            new int[count * 12];

        Color vertexColor =
            new Color(
                1f,
                1f,
                1f,
                skidMarkAlpha);

        for (int i = 0;
             i < count;
             i++)
        {
            SkidSegment s =
                skidSegments[i];

            int v =
                i * 4;

            int t =
                i * 12;

            vertices[v + 0] = s.v0;
            vertices[v + 1] = s.v1;
            vertices[v + 2] = s.v2;
            vertices[v + 3] = s.v3;

            colors[v + 0] = vertexColor;
            colors[v + 1] = vertexColor;
            colors[v + 2] = vertexColor;
            colors[v + 3] = vertexColor;

            triangles[t + 0] = v + 0;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;

            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 1;

            // Iki tarafli.
            triangles[t + 6] = v + 0;
            triangles[t + 7] = v + 1;
            triangles[t + 8] = v + 2;

            triangles[t + 9] = v + 2;
            triangles[t + 10] = v + 1;
            triangles[t + 11] = v + 3;
        }

        skidMesh.Clear();
        skidMesh.vertices = vertices;
        skidMesh.colors = colors;
        skidMesh.triangles = triangles;
        skidMesh.RecalculateBounds();
    }

    // =================================================================
    // SMOKE
    // =================================================================
    private void DumanSisteminiHazirla()
    {
        if (smokeSystem != null)
            return;

        GameObject go =
            new GameObject(
                "_GLS580_TIRE_SMOKE");

        smokeSystem =
            go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main =
            smokeSystem.main;

        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace =
            ParticleSystemSimulationSpace.World;
        main.maxParticles = 1200;

        ParticleSystem.EmissionModule emission =
            smokeSystem.emission;

        emission.enabled = false;

        smokeRenderer =
            smokeSystem.GetComponent<ParticleSystemRenderer>();

        smokeRenderer.renderMode =
            ParticleSystemRenderMode.Billboard;
        smokeRenderer.shadowCastingMode =
            ShadowCastingMode.Off;
        smokeRenderer.receiveShadows =
            false;
        smokeRenderer.sortingOrder =
            100;

        smokeTexture =
            DumanTextureOlustur();

        smokeMaterial =
            DumanMaterialOlustur(
                smokeTexture);

        smokeRenderer.sharedMaterial =
            smokeMaterial;

        smokeSystem.Play();
    }

    private Texture2D DumanTextureOlustur()
    {
        const int size = 32;

        Texture2D tex =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);

        tex.wrapMode =
            TextureWrapMode.Clamp;

        tex.filterMode =
            FilterMode.Bilinear;

        Color[] pixels =
            new Color[
                size * size];

        Vector2 center =
            new Vector2(
                (size - 1) * 0.5f,
                (size - 1) * 0.5f);

        float radius =
            size * 0.5f;

        for (int y = 0;
             y < size;
             y++)
        {
            for (int x = 0;
                 x < size;
                 x++)
            {
                float d =
                    Vector2.Distance(
                        new Vector2(x, y),
                        center) /
                    radius;

                float a =
                    Mathf.Clamp01(
                        1f - d);

                a =
                    a * a *
                    (3f - 2f * a);

                pixels[
                    y * size + x] =
                    new Color(
                        1f,
                        1f,
                        1f,
                        a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return tex;
    }

    private Material DumanMaterialOlustur(
        Texture2D texture)
    {
        Shader shader =
            Shader.Find(
                "Sprites/Default");

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Universal Render Pipeline/Particles/Unlit");
        }

        Material mat =
            new Material(shader);

        if (mat.HasProperty("_MainTex"))
            mat.SetTexture(
                "_MainTex",
                texture);

        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture(
                "_BaseMap",
                texture);

        Color c =
            new Color(
                0.82f,
                0.82f,
                0.82f,
                0.72f);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", c);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);

        mat.renderQueue =
            3000;

        return mat;
    }

    private void DumanGuncelle()
    {
        if (smokeSystem == null)
            return;

        bool active =
            tireSmokeAktif &&
            efektAktif;

        if (!active)
        {
            leftSmokeAccumulator = 0f;
            rightSmokeAccumulator = 0f;
            return;
        }

        float intensity =
            Mathf.Lerp(
                0.8f,
                1.7f,
                Mathf.Clamp01(
                    driftKaymaAcisi /
                    Mathf.Max(
                        1f,
                        maksimumDriftAcisi)));

        DumanEmitEt(
            leftGround,
            ref leftSmokeAccumulator,
            intensity);

        DumanEmitEt(
            rightGround,
            ref rightSmokeAccumulator,
            intensity);
    }

    private void DumanEmitEt(
        GroundInfo ground,
        ref float accumulator,
        float intensity)
    {
        accumulator +=
            smokeRate *
            intensity *
            Time.deltaTime;

        int count =
            Mathf.FloorToInt(
                accumulator);

        if (count <= 0)
            return;

        accumulator -=
            count;

        Vector3 normal =
            ground.normal.sqrMagnitude > 0.001f
                ? ground.normal.normalized
                : Vector3.up;

        Vector3 backward =
            vehicleRoot != null
                ? -vehicleRoot.forward
                : Vector3.back;

        for (int i = 0;
             i < count;
             i++)
        {
            ParticleSystem.EmitParams ep =
                new ParticleSystem.EmitParams();

            Vector3 random =
                UnityEngine.Random.insideUnitSphere *
                0.08f;

            random =
                Vector3.ProjectOnPlane(
                    random,
                    normal);

            ep.position =
                ground.point +
                normal * 0.06f +
                random;

            ep.velocity =
                normal *
                UnityEngine.Random.Range(
                    0.4f,
                    1.05f) +
                backward *
                UnityEngine.Random.Range(
                    0.08f,
                    0.28f) +
                UnityEngine.Random.insideUnitSphere *
                0.14f;

            ep.startLifetime =
                UnityEngine.Random.Range(
                    smokeLifetimeMin,
                    smokeLifetimeMax);

            ep.startSize =
                UnityEngine.Random.Range(
                    smokeSizeMin,
                    smokeSizeMax);

            float gray =
                UnityEngine.Random.Range(
                    0.62f,
                    0.90f);

            ep.startColor =
                new Color(
                    gray,
                    gray,
                    gray,
                    UnityEngine.Random.Range(
                        0.48f,
                        0.74f));

            ep.rotation =
                UnityEngine.Random.Range(
                    0f,
                    Mathf.PI * 2f);

            smokeSystem.Emit(
                ep,
                1);
        }
    }

    // =================================================================
    // AUDIO
    // =================================================================
    private void AudioHazirla()
    {
        if (vehicleRoot == null)
            return;

        if (oneShotSource == null)
        {
            GameObject go =
                new GameObject(
                    "_GLS580_OneShotAudio");

            go.transform.SetParent(
                vehicleRoot,
                false);

            oneShotSource =
                go.AddComponent<AudioSource>();

            oneShotSource.playOnAwake = false;
            oneShotSource.spatialBlend = 0.45f;
        }

        if (engineSource == null)
        {
            GameObject go =
                new GameObject(
                    "_GLS580_EngineAudio");

            go.transform.SetParent(
                vehicleRoot,
                false);

            engineSource =
                go.AddComponent<AudioSource>();

            engineSource.playOnAwake = false;
            engineSource.loop = true;
            engineSource.spatialBlend = 0.4f;
        }

        if (driftSource == null)
        {
            GameObject go =
                new GameObject(
                    "_GLS580_DriftAudio");

            go.transform.SetParent(
                vehicleRoot,
                false);

            driftSource =
                go.AddComponent<AudioSource>();

            driftSource.playOnAwake = false;
            driftSource.loop = true;
            driftSource.spatialBlend = 0.55f;
            driftSource.volume = 0f;
        }
    }

    private void MotoruBaslat()
    {
        if (engineStartRoutine != null)
            StopCoroutine(
                engineStartRoutine);

        engineStartRoutine =
            StartCoroutine(
                MotorBaslatRutini());
    }

    private IEnumerator MotorBaslatRutini()
    {
        engineRunning = false;

        if (engineSource != null)
            engineSource.Stop();

        if (engineStartClip != null &&
            oneShotSource != null)
        {
            oneShotSource.PlayOneShot(
                engineStartClip,
                engineStartVolume);

            yield return
                new WaitForSeconds(
                    Mathf.Max(
                        0.05f,
                        engineStartClip.length *
                        0.80f));
        }

        if (!playerInside)
            yield break;

        engineRunning = true;

        if (engineSource != null &&
            engineLoopClip != null)
        {
            engineSource.clip =
                engineLoopClip;

            engineSource.volume =
                engineIdleVolume;

            engineSource.pitch =
                engineIdlePitch;

            engineSource.Play();
        }
    }

    private void MotoruKapat()
    {
        if (engineStartRoutine != null)
        {
            StopCoroutine(
                engineStartRoutine);

            engineStartRoutine =
                null;
        }

        engineRunning = false;

        if (engineSource != null)
            engineSource.Stop();

        if (driftSource != null)
            driftSource.Stop();
    }

    private void MotorSesiniGuncelle()
    {
        if (engineSource == null ||
            !playerInside ||
            !engineRunning ||
            engineLoopClip == null)
            return;

        if (engineSource.clip !=
            engineLoopClip)
        {
            engineSource.clip =
                engineLoopClip;
        }

        if (!engineSource.isPlaying)
            engineSource.Play();

        float speed01 =
            Mathf.Clamp01(
                speedKmh /
                Mathf.Max(
                    1f,
                    engineMaxSpeedKmh));

        float throttle =
            Input.GetKey(KeyCode.W)
                ? 1f
                : 0f;

        float rpm =
            Mathf.Clamp01(
                Mathf.Max(
                    speed01,
                    throttle * 0.62f));

        engineSource.pitch =
            Mathf.MoveTowards(
                engineSource.pitch,
                Mathf.Lerp(
                    engineIdlePitch,
                    engineMaxPitch,
                    rpm),
                1.8f *
                Time.deltaTime);

        engineSource.volume =
            Mathf.MoveTowards(
                engineSource.volume,
                Mathf.Lerp(
                    engineIdleVolume,
                    engineMaxVolume,
                    Mathf.Max(
                        speed01,
                        throttle * 0.55f)),
                1.4f *
                Time.deltaTime);
    }

    private void DriftSesiniGuncelle()
    {
        if (driftSource == null)
            return;

        bool active =
            driftClip != null &&
            efektAktif;

        float targetVolume =
            active
                ? Mathf.Lerp(
                    0.55f,
                    driftSoundMaxVolume,
                    Mathf.Clamp01(
                        driftKaymaAcisi /
                        Mathf.Max(
                            1f,
                            maksimumDriftAcisi)))
                : 0f;

        driftSource.volume =
            Mathf.MoveTowards(
                driftSource.volume,
                targetVolume,
                5f *
                Time.deltaTime);

        driftSource.pitch =
            Mathf.Lerp(
                0.92f,
                1.15f,
                Mathf.Clamp01(
                    speedKmh /
                    100f));

        if (driftSource.clip !=
            driftClip)
        {
            driftSource.clip =
                driftClip;
        }

        if (active &&
            !driftSource.isPlaying)
        {
            driftSource.Play();
        }

        if (!active &&
            driftSource.isPlaying &&
            driftSource.volume <= 0.01f)
        {
            driftSource.Stop();
        }
    }

    // =================================================================
    // AUTO FIND
    // =================================================================
    private void OtomatikBul()
    {
        if (basitSistem == null)
        {
            MonoBehaviour[] all =
                FindObjectsOfType<MonoBehaviour>(
                    true);

            foreach (MonoBehaviour mb in all)
            {
                if (mb != null &&
                    mb.GetType().Name ==
                    "GLS580BasitSistem")
                {
                    basitSistem = mb;
                    break;
                }
            }
        }

        if (basitSistem != null)
        {
            Type currentType =
                basitSistem.GetType();

            if (basitType != currentType)
            {
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

                fHandbrakePower =
                    currentType.GetField(
                        "handbrakePower",
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

            // Inspector'da yanlislikla Mercedes model root verilmis olsa bile
            // runtime'da BasitSistem'in GERCEK driveRoot'unu zorla kullan.
            if (fDriveRoot != null)
            {
                try
                {
                    Transform actualRoot =
                        fDriveRoot.GetValue(
                            basitSistem)
                        as Transform;

                    if (actualRoot != null)
                        vehicleRoot =
                            actualRoot;
                }
                catch { }
            }
        }

        // Ek garanti: sahnede bu isim varsa gercek surus root'u budur.
        if (vehicleRoot == null ||
            vehicleRoot.name.Contains(
                "Mercedes_GLS580"))
        {
            GameObject knownRoot =
                GameObject.Find(
                    "_GLS580_DriveRoot_V2");

            if (knownRoot != null)
                vehicleRoot =
                    knownRoot.transform;
        }

        if (vehicleRoot == null)
            vehicleRoot =
                transform;

        if (vehicleRoot != null)
        {
            Rigidbody actualBody =
                vehicleRoot.GetComponent<Rigidbody>();

            if (actualBody != null)
                vehicleRigidbody =
                    actualBody;
        }

        TekerleriOtomatikBul();
    }

    private void TekerleriOtomatikBul()
    {
        if (vehicleRoot == null)
            return;

        if (wheelRL == null ||
            !wheelRL.IsChildOf(
                vehicleRoot))
        {
            wheelRL =
                FindDeep(
                    vehicleRoot,
                    "Wheel_RL");
        }

        if (wheelRR == null ||
            !wheelRR.IsChildOf(
                vehicleRoot))
        {
            wheelRR =
                FindDeep(
                    vehicleRoot,
                    "Wheel_RR");
        }
    }

    private static Transform FindDeep(
        Transform root,
        string wanted)
    {
        if (root == null)
            return null;

        if (root.name == wanted)
            return root;

        for (int i = 0;
             i < root.childCount;
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

    private void DurumuOku()
    {
        if (basitSistem == null)
        {
            playerInside = true;
            busy = false;
            return;
        }

        try
        {
            playerInside =
                fPlayerInside != null
                    ? (bool)fPlayerInside.GetValue(
                        basitSistem)
                    : true;

            busy =
                fBusy != null
                    ? (bool)fBusy.GetValue(
                        basitSistem)
                    : false;
        }
        catch
        {
            playerInside = true;
            busy = false;
        }
    }

    private void OnDisable()
    {
        HandbrakePowerRestore();
    }

    private void OnDestroy()
    {
        HandbrakePowerRestore();

        if (skidRoot != null)
            Destroy(
                skidRoot);

        if (smokeSystem != null)
            Destroy(
                smokeSystem.gameObject);

        if (skidMaterial != null)
            Destroy(
                skidMaterial);

        if (smokeMaterial != null)
            Destroy(
                smokeMaterial);

        if (smokeTexture != null)
            Destroy(
                smokeTexture);
    }
}


// =====================================================================
// BASIT SISTEMDEN ONCE CALISAN KUCuk YARDIMCI.
// Kullanici bunu EKLEMEZ. GLS580SurusEfektleri otomatik ekler.
// =====================================================================
[DefaultExecutionOrder(-32000)]
public class GLS580SurusEfektleri_PrePhysics : MonoBehaviour
{
    [NonSerialized]
    public GLS580SurusEfektleri owner;

    private void FixedUpdate()
    {
        if (owner != null)
            owner.PrePhysicsTick();
    }

    private void OnDisable()
    {
        if (owner != null)
            owner.HandbrakePowerRestore();
    }
}