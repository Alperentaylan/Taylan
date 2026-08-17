using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(33000)]
public class GLS580SurusHissi : MonoBehaviour
{
    [Header("ARAC - PROJENE GORE OTOMATIK BULUR")]
    [Tooltip("Bos birakabilirsin. GLS580BasitSistem.driveRoot otomatik okunur.")]
    public Transform vehicleRoot;

    [Tooltip("Mercedes_GLS580_Doors_Trunk_Wheels_Only gibi gorunen model root'u. Bos birakabilirsin.")]
    public Transform visualRoot;

    [Tooltip("Gercek surus Rigidbody. Bos birakabilirsin.")]
    public Rigidbody vehicleRigidbody;

    [Header("1) GOVDE / SUSPANSIYON HISSi")]
    public bool govdeHareketiAktif = true;

    [Tooltip("Gazda burnun ne kadar kalkacagi.")]
    [Range(0f, 8f)]
    public float gazPitchDerece = 2.4f;

    [Tooltip("Frende burnun ne kadar one dalacagi.")]
    [Range(0f, 10f)]
    public float frenPitchDerece = 4.2f;

    [Tooltip("Virajda govdenin dis tarafa ne kadar yatacagi.")]
    [Range(0f, 10f)]
    public float virajRollDerece = 4.0f;

    [Tooltip("Tam pitch etkisi icin gereken ivme m/s^2.")]
    public float pitchTamIvme = 5.5f;

    [Tooltip("Roll etkisinin tam oldugu hiz.")]
    public float rollTamHizKmh = 75f;

    [Tooltip("Govde hareketlerinin yumusakligi.")]
    public float govdeYumusaklik = 7.5f;

    [Tooltip("Fren/gazda govdenin cok hafif asagi oturmasi.")]
    public float suspensionDikeyHareket = 0.018f;

    [Tooltip("Modelinde pitch ters gorunurse ac.")]
    public bool pitchYonuTers = false;

    [Tooltip("Modelinde roll ters gorunurse ac.")]
    public bool rollYonuTers = false;

    [Header("2) HIZ HISSi / KAMERA FOV")]
    public bool kameraFovAktif = true;

    [Tooltip("3 dis kamera. Bos birakirsan isimlerden otomatik bulur.")]
    public Camera[] disKameralar;

    [Tooltip("Karakter gozu / arac ici kamera. Bos birakirsan otomatik bulur.")]
    public Camera icKamera;

    [Tooltip("Dis kameralarda maksimum FOV artisi.")]
    public float disKameraFovArtisi = 11f;

    [Tooltip("Ic kamerada maksimum FOV artisi.")]
    public float icKameraFovArtisi = 5f;

    [Tooltip("Bu hizda maksimum FOV etkisi.")]
    public float maksimumFovHiziKmh = 160f;

    [Tooltip("S ile sert frende FOV'un kisa sure toparlanmasi.")]
    public float frenFovDaralma = 3f;

    public float fovYumusaklik = 6f;

    [Header("3) 9 ILERI OTOMATIK VITES + MOTOR DEVRI")]
    public bool otomatikVitesAktif = true;

    [Tooltip("Bos birakabilirsin. _GLS580_EngineAudio varsa onu kullanir.")]
    public AudioSource engineAudioSource;

    [Tooltip("Mevcut motor loop kaynagi bulunamazsa kendi loop sesini buraya koyabilirsin.")]
    public AudioClip engineLoopClip;

    [Range(0f, 1f)]
    public float motorIdleVolume = 0.30f;

    [Range(0f, 1f)]
    public float motorMaxVolume = 0.82f;

    [Tooltip("Motor sesinin idle pitch'i.")]
    public float motorIdlePitch = 0.78f;

    [Tooltip("Motor sesinin redline pitch'i.")]
    public float motorRedlinePitch = 1.62f;

    public float idleRpm = 650f;
    public float redlineRpm = 6200f;

    [Tooltip("Bu RPM'de ust vitese gecmeye calisir.")]
    public float upshiftRpm = 5200f;

    [Tooltip("Gaz varken RPM bunun altina inerse alt vitese gecebilir.")]
    public float downshiftRpm = 1450f;

    [Tooltip("Bir vites degisiminin hissedilen suresi.")]
    public float vitesDegisimSuresi = 0.20f;

    [Tooltip("Vites degisiminde motor pitch kisa sure ne kadar dussun.")]
    public float vitesPitchDususu = 0.18f;

    [Tooltip("Vites atarken govdeye cok hafif vuruntu.")]
    public float vitesGovdeVuruntusu = 0.55f;

    [Tooltip("Oyun icin teker yaricapi. GLS jant/lastik boyutuna gore yaklasik.")]
    public float tekerYaricapi = 0.37f;

    [Tooltip("Oyun icin final drive.")]
    public float finalDrive = 3.27f;

    [Header("9 VITES ORANLARI")]
    public float[] gearRatios = new float[]
    {
        5.35f,
        3.24f,
        2.25f,
        1.64f,
        1.21f,
        1.00f,
        0.87f,
        0.72f,
        0.60f
    };

    [Header("GLS580 BASIT SISTEM")]
    [Tooltip("Bos birakabilirsin. Otomatik bulunur.")]
    public MonoBehaviour basitSistem;

    [Header("DEBUG - PLAY MODDA")]
    [SerializeField] private bool playerInside;
    [SerializeField] private bool busy;
    [SerializeField] private float signedSpeedMps;
    [SerializeField] private float speedKmh;
    [SerializeField] private float longitudinalAcceleration;
    [SerializeField] private float steerNormalized;
    [SerializeField] private float currentPitch;
    [SerializeField] private float currentRoll;
    [SerializeField] private int currentGear = 1;
    [SerializeField] private float engineRpm;
    [SerializeField] private bool shifting;
    [SerializeField] private string aktifMotorAudio = "-";
    [SerializeField] private string bulunanVisualRoot = "-";

    private Type basitType;
    private FieldInfo fPlayerInside;
    private FieldInfo fBusy;
    private FieldInfo fDriveRoot;
    private FieldInfo fCurrentDriveSpeed;
    private FieldInfo fCurrentSteerAngle;

    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;
    private bool visualBaseCaptured;

    private readonly Dictionary<Camera, float> cameraBaseFov =
        new Dictionary<Camera, float>();

    private float previousSignedSpeed;
    private bool speedInitialized;
    private float smoothedAcceleration;
    private float shiftKick;
    private float shiftTimer;

    private Vector3 lastVehiclePosition;
    private bool fallbackPositionReady;
    private float fallbackSignedSpeed;

    private int pendingGear = -1;

    private void Awake()
    {
        OtomatikBul();
        VisualBaseYakalama();
        KameraBaseFovYakalama();
        FallbackHizBaslat();
    }

    private void Start()
    {
        OtomatikBul();
        VisualBaseYakalama();
        KameraBaseFovYakalama();
        FallbackHizBaslat();
    }

    private void Update()
    {
        OtomatikBul();
        DurumuOku();
        FallbackHiziGuncelle();
        HizVeIvmeGuncelle();
        VitesSisteminiGuncelle();
        MotorSesiniGuncelle();
        KameraFovGuncelle();
    }

    private void LateUpdate()
    {
        GovdeHareketiniGuncelle();
    }

    // ============================================================
    // 1) GOVDE / SUSPANSIYON
    // ============================================================
    private void GovdeHareketiniGuncelle()
    {
        if (!govdeHareketiAktif ||
            visualRoot == null ||
            !visualBaseCaptured)
            return;

        float dt =
            Mathf.Max(
                Time.deltaTime,
                0.0001f);

        float pitch01 =
            Mathf.Clamp(
                smoothedAcceleration /
                Mathf.Max(
                    0.1f,
                    pitchTamIvme),
                -1f,
                1f);

        // Pozitif ivme = gaz = burun kalksin.
        // Negatif ivme = fren = burun dalsin.
        float targetPitch;

        if (pitch01 >= 0f)
        {
            targetPitch =
                -pitch01 *
                gazPitchDerece;
        }
        else
        {
            targetPitch =
                -pitch01 *
                frenPitchDerece;
        }

        if (pitchYonuTers)
            targetPitch *= -1f;

        float speedFactor =
            Mathf.Clamp01(
                speedKmh /
                Mathf.Max(
                    1f,
                    rollTamHizKmh));

        float targetRoll =
            steerNormalized *
            virajRollDerece *
            speedFactor;

        if (rollYonuTers)
            targetRoll *= -1f;

        // Vites atarken cok kisa ekstra pitch kick.
        targetPitch +=
            shiftKick;

        float blend =
            1f -
            Mathf.Exp(
                -govdeYumusaklik *
                dt);

        currentPitch =
            Mathf.Lerp(
                currentPitch,
                targetPitch,
                blend);

        currentRoll =
            Mathf.Lerp(
                currentRoll,
                targetRoll,
                blend);

        float compression =
            Mathf.Clamp01(
                Mathf.Abs(
                    smoothedAcceleration) /
                Mathf.Max(
                    0.1f,
                    pitchTamIvme));

        float targetY =
            -compression *
            suspensionDikeyHareket;

        Vector3 targetPos =
            visualBaseLocalPosition +
            Vector3.up *
            targetY;

        visualRoot.localPosition =
            Vector3.Lerp(
                visualRoot.localPosition,
                targetPos,
                blend);

        visualRoot.localRotation =
            Quaternion.Slerp(
                visualRoot.localRotation,
                visualBaseLocalRotation *
                Quaternion.Euler(
                    currentPitch,
                    0f,
                    currentRoll),
                blend);

        shiftKick =
            Mathf.MoveTowards(
                shiftKick,
                0f,
                4.5f *
                Time.deltaTime);
    }

    // ============================================================
    // 2) KAMERA FOV
    // ============================================================
    private void KameraFovGuncelle()
    {
        if (!kameraFovAktif)
            return;

        KameraBaseFovYakalama();

        float speed01 =
            Mathf.Clamp01(
                speedKmh /
                Mathf.Max(
                    1f,
                    maksimumFovHiziKmh));

        speed01 =
            speed01 *
            speed01 *
            (3f - 2f * speed01);

        float braking =
            playerInside &&
            signedSpeedMps > 1f &&
            Input.GetKey(KeyCode.S)
                ? 1f
                : 0f;

        if (disKameralar != null)
        {
            for (int i = 0;
                 i < disKameralar.Length;
                 i++)
            {
                Camera cam =
                    disKameralar[i];

                if (cam == null)
                    continue;

                float baseFov =
                    BaseFov(cam);

                float target =
                    baseFov +
                    disKameraFovArtisi *
                    speed01 -
                    frenFovDaralma *
                    braking;

                cam.fieldOfView =
                    Mathf.Lerp(
                        cam.fieldOfView,
                        target,
                        1f -
                        Mathf.Exp(
                            -fovYumusaklik *
                            Time.deltaTime));
            }
        }

        if (icKamera != null)
        {
            float baseFov =
                BaseFov(
                    icKamera);

            float target =
                baseFov +
                icKameraFovArtisi *
                speed01 -
                frenFovDaralma *
                0.55f *
                braking;

            icKamera.fieldOfView =
                Mathf.Lerp(
                    icKamera.fieldOfView,
                    target,
                    1f -
                    Mathf.Exp(
                        -fovYumusaklik *
                        Time.deltaTime));
        }
    }

    private float BaseFov(Camera cam)
    {
        if (cam == null)
            return 60f;

        if (!cameraBaseFov.ContainsKey(cam))
            cameraBaseFov[cam] =
                cam.fieldOfView;

        return cameraBaseFov[cam];
    }

    private void KameraBaseFovYakalama()
    {
        if (disKameralar != null)
        {
            for (int i = 0;
                 i < disKameralar.Length;
                 i++)
            {
                Camera cam =
                    disKameralar[i];

                if (cam != null &&
                    !cameraBaseFov.ContainsKey(
                        cam))
                {
                    cameraBaseFov[
                        cam] =
                        cam.fieldOfView;
                }
            }
        }

        if (icKamera != null &&
            !cameraBaseFov.ContainsKey(
                icKamera))
        {
            cameraBaseFov[
                icKamera] =
                icKamera.fieldOfView;
        }
    }

    // ============================================================
    // 3) OTOMATIK VITES / RPM
    // ============================================================
    private void VitesSisteminiGuncelle()
    {
        if (!otomatikVitesAktif)
            return;

        gearRatiosKontrol();

        bool reverse =
            signedSpeedMps <
            -0.25f;

        if (reverse)
        {
            // Reverse icin sabit sanal oran.
            engineRpm =
                RpmHesapla(
                    Mathf.Abs(
                        signedSpeedMps),
                    4.80f);

            engineRpm =
                Mathf.Clamp(
                    engineRpm,
                    idleRpm,
                    redlineRpm);

            shifting = false;
            shiftTimer = 0f;
            pendingGear = -1;
            return;
        }

        if (currentGear < 1)
            currentGear = 1;

        if (currentGear >
            gearRatios.Length)
        {
            currentGear =
                gearRatios.Length;
        }

        float ratio =
            gearRatios[
                currentGear - 1];

        float calculatedRpm =
            RpmHesapla(
                Mathf.Max(
                    0f,
                    signedSpeedMps),
                ratio);

        // Dusuk hiz/gazda motor idle'in altina inmesin.
        float throttle =
            playerInside &&
            Input.GetKey(KeyCode.W)
                ? 1f
                : 0f;

        calculatedRpm =
            Mathf.Max(
                calculatedRpm,
                idleRpm +
                throttle *
                700f);

        engineRpm =
            Mathf.Lerp(
                engineRpm <= 0f
                    ? calculatedRpm
                    : engineRpm,
                calculatedRpm,
                1f -
                Mathf.Exp(
                    -8f *
                    Time.deltaTime));

        if (shifting)
        {
            shiftTimer -=
                Time.deltaTime;

            if (shiftTimer <= 0f)
            {
                shifting = false;
                pendingGear = -1;
            }

            return;
        }

        if (!playerInside ||
            busy)
            return;

        if (engineRpm >=
                upshiftRpm &&
            currentGear <
                gearRatios.Length)
        {
            VitesDegistir(
                currentGear + 1);
        }
        else if (
            engineRpm <=
                downshiftRpm &&
            currentGear > 1 &&
            throttle > 0.15f)
        {
            VitesDegistir(
                currentGear - 1);
        }
    }

    private void VitesDegistir(
        int yeniVites)
    {
        yeniVites =
            Mathf.Clamp(
                yeniVites,
                1,
                gearRatios.Length);

        if (yeniVites ==
            currentGear)
            return;

        pendingGear =
            yeniVites;

        currentGear =
            yeniVites;

        shifting = true;

        shiftTimer =
            Mathf.Max(
                0.05f,
                vitesDegisimSuresi);

        // Vites yukselirken cok hafif one, duserken ters yone vuruntu.
        float direction =
            yeniVites >
            currentGear
                ? 1f
                : -1f;

        // currentGear yukarida degistigi icin pending ile eskiyi karsilastiramayiz.
        // Oyun hissi icin her shift'te kisa burun hareketi yeterli.
        shiftKick =
            vitesGovdeVuruntusu;
    }

    private float RpmHesapla(
        float speedMps,
        float gearRatio)
    {
        float circumference =
            2f *
            Mathf.PI *
            Mathf.Max(
                0.05f,
                tekerYaricapi);

        float wheelRpm =
            speedMps /
            circumference *
            60f;

        return
            wheelRpm *
            Mathf.Max(
                0.1f,
                gearRatio) *
            Mathf.Max(
                0.1f,
                finalDrive);
    }

    private void gearRatiosKontrol()
    {
        if (gearRatios == null ||
            gearRatios.Length < 1)
        {
            gearRatios =
                new float[]
                {
                    5.35f,
                    3.24f,
                    2.25f,
                    1.64f,
                    1.21f,
                    1.00f,
                    0.87f,
                    0.72f,
                    0.60f
                };
        }
    }

    // ============================================================
    // MOTOR SESI
    // ============================================================
    private void MotorSesiniGuncelle()
    {
        if (!otomatikVitesAktif)
            return;

        MotorAudioBul();

        if (engineAudioSource == null)
            return;

        if (engineLoopClip != null &&
            engineAudioSource.clip !=
            engineLoopClip)
        {
            engineAudioSource.clip =
                engineLoopClip;
        }

        if (engineAudioSource.clip == null)
            return;

        engineAudioSource.loop =
            true;

        float rpm01 =
            Mathf.InverseLerp(
                idleRpm,
                redlineRpm,
                Mathf.Clamp(
                    engineRpm,
                    idleRpm,
                    redlineRpm));

        float targetPitch =
            Mathf.Lerp(
                motorIdlePitch,
                motorRedlinePitch,
                rpm01);

        if (shifting)
        {
            float shift01 =
                Mathf.Clamp01(
                    shiftTimer /
                    Mathf.Max(
                        0.05f,
                        vitesDegisimSuresi));

            targetPitch -=
                vitesPitchDususu *
                Mathf.Sin(
                    shift01 *
                    Mathf.PI);
        }

        float throttle =
            playerInside &&
            Input.GetKey(KeyCode.W)
                ? 1f
                : 0f;

        float targetVolume =
            Mathf.Lerp(
                motorIdleVolume,
                motorMaxVolume,
                Mathf.Clamp01(
                    rpm01 *
                    0.75f +
                    throttle *
                    0.35f));

        engineAudioSource.pitch =
            Mathf.MoveTowards(
                engineAudioSource.pitch,
                targetPitch,
                2.6f *
                Time.deltaTime);

        engineAudioSource.volume =
            Mathf.MoveTowards(
                engineAudioSource.volume,
                targetVolume,
                1.8f *
                Time.deltaTime);

        if (playerInside &&
            !busy &&
            !engineAudioSource.isPlaying)
        {
            engineAudioSource.Play();
        }
    }

    private void MotorAudioBul()
    {
        if (engineAudioSource != null)
        {
            aktifMotorAudio =
                engineAudioSource.name;
            return;
        }

        if (vehicleRoot == null)
            return;

        Transform found =
            FindDeep(
                vehicleRoot,
                "_GLS580_EngineAudio");

        if (found != null)
        {
            engineAudioSource =
                found.GetComponent<AudioSource>();
        }

        if (engineAudioSource == null)
        {
            AudioSource[] sources =
                vehicleRoot.GetComponentsInChildren<AudioSource>(
                    true);

            for (int i = 0;
                 i < sources.Length;
                 i++)
            {
                AudioSource source =
                    sources[i];

                if (source == null)
                    continue;

                string n =
                    source.name.ToLowerInvariant();

                if (n.Contains("engine") ||
                    n.Contains("motor"))
                {
                    engineAudioSource =
                        source;
                    break;
                }
            }
        }

        // Mevcut ses sistemi yoksa ve klip verilmis ise kendimiz olustur.
        if (engineAudioSource == null &&
            engineLoopClip != null)
        {
            GameObject go =
                new GameObject(
                    "_GLS580_EngineAudio");

            go.transform.SetParent(
                vehicleRoot,
                false);

            engineAudioSource =
                go.AddComponent<AudioSource>();

            engineAudioSource.clip =
                engineLoopClip;

            engineAudioSource.loop =
                true;

            engineAudioSource.playOnAwake =
                false;

            engineAudioSource.spatialBlend =
                0.45f;
        }

        aktifMotorAudio =
            engineAudioSource != null
                ? engineAudioSource.name
                : "BULUNAMADI";
    }

    // ============================================================
    // SPEED / ACCELERATION
    // ============================================================
    private void HizVeIvmeGuncelle()
    {
        signedSpeedMps =
            GercekSignedSpeedOku();

        speedKmh =
            Mathf.Abs(
                signedSpeedMps) *
            3.6f;

        if (!speedInitialized)
        {
            previousSignedSpeed =
                signedSpeedMps;

            speedInitialized =
                true;

            longitudinalAcceleration =
                0f;

            return;
        }

        float dt =
            Mathf.Max(
                Time.deltaTime,
                0.0001f);

        float rawAcceleration =
            (signedSpeedMps -
             previousSignedSpeed) /
            dt;

        previousSignedSpeed =
            signedSpeedMps;

        rawAcceleration =
            Mathf.Clamp(
                rawAcceleration,
                -12f,
                10f);

        smoothedAcceleration =
            Mathf.Lerp(
                smoothedAcceleration,
                rawAcceleration,
                1f -
                Mathf.Exp(
                    -7f *
                    dt));

        longitudinalAcceleration =
            smoothedAcceleration;

        steerNormalized =
            SteeringOkuNormalized();
    }

    private float GercekSignedSpeedOku()
    {
        if (basitSistem != null &&
            fCurrentDriveSpeed != null)
        {
            try
            {
                return Convert.ToSingle(
                    fCurrentDriveSpeed.GetValue(
                        basitSistem));
            }
            catch { }
        }

        return fallbackSignedSpeed;
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
                        angle / 35f,
                        -1f,
                        1f);
            }
            catch { }
        }

        float input = 0f;

        if (Input.GetKey(KeyCode.A))
            input -= 1f;

        if (Input.GetKey(KeyCode.D))
            input += 1f;

        return input;
    }

    private void FallbackHizBaslat()
    {
        if (vehicleRoot == null)
            return;

        lastVehiclePosition =
            vehicleRoot.position;

        fallbackPositionReady =
            true;
    }

    private void FallbackHiziGuncelle()
    {
        if (vehicleRoot == null)
            return;

        if (!fallbackPositionReady)
        {
            FallbackHizBaslat();
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

        fallbackSignedSpeed =
            Vector3.Dot(
                delta / dt,
                vehicleRoot.forward);
    }

    // ============================================================
    // AUTO FIND - PROJE HIERARCHY'SINE GORE
    // ============================================================
    private void OtomatikBul()
    {
        BasitSistemBul();

        // Gercek DriveRoot'u BasitSystem'den al.
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
                vehicleRoot.GetComponent<Rigidbody>();

            if (rb != null)
                vehicleRigidbody =
                    rb;
        }

        VisualRootBul();
        KameralariBul();
        MotorAudioBul();
    }

    private void BasitSistemBul()
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
                    ? (bool)fPlayerInside.GetValue(
                        basitSistem)
                    : false;

            busy =
                fBusy != null
                    ? (bool)fBusy.GetValue(
                        basitSistem)
                    : false;
        }
        catch
        {
            playerInside = false;
            busy = false;
        }
    }

    private void VisualRootBul()
    {
        if (visualRoot != null)
        {
            bulunanVisualRoot =
                visualRoot.name;

            VisualBaseYakalama();
            return;
        }

        if (vehicleRoot == null)
            return;

        // Projedeki ana model adina once bak.
        Transform named =
            FindDeepContains(
                vehicleRoot,
                "Mercedes_GLS580");

        if (named != null &&
            named != vehicleRoot)
        {
            // DriveRoot altindaki en ust Mercedes child'i kullan.
            Transform candidate =
                named;

            while (candidate.parent != null &&
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
            }
        }

        // Isim bulunamazsa Static_Body'den yukariya cik.
        if (visualRoot == null)
        {
            Transform staticBody =
                FindDeep(
                    vehicleRoot,
                    "Static_Body");

            if (staticBody != null)
            {
                Transform candidate =
                    staticBody;

                while (candidate.parent != null &&
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
                }
                else
                {
                    visualRoot =
                        staticBody;
                }
            }
        }

        bulunanVisualRoot =
            visualRoot != null
                ? visualRoot.name
                : "BULUNAMADI";

        VisualBaseYakalama();
    }

    private void VisualBaseYakalama()
    {
        if (visualRoot == null ||
            visualBaseCaptured)
            return;

        visualBaseLocalPosition =
            visualRoot.localPosition;

        visualBaseLocalRotation =
            visualRoot.localRotation;

        visualBaseCaptured =
            true;
    }

    private void KameralariBul()
    {
        if (disKameralar == null ||
            disKameralar.Length == 0)
        {
            List<Camera> list =
                new List<Camera>();

            Camera c1 =
                CameraBul(
                    "_GLS580_AracKamerasi");

            Camera c2 =
                CameraBul(
                    "orta kamera araba");

            Camera c3 =
                CameraBul(
                    "uzak kamera araba");

            if (c1 != null)
                list.Add(c1);

            if (c2 != null &&
                !list.Contains(c2))
                list.Add(c2);

            if (c3 != null &&
                !list.Contains(c3))
                list.Add(c3);

            disKameralar =
                list.ToArray();
        }

        if (icKamera == null)
        {
            icKamera =
                CameraBul(
                    "karakter gözü kamerası");

            if (icKamera == null)
            {
                icKamera =
                    CameraBul(
                        "karakter gozu kamerasi");
            }
        }

        KameraBaseFovYakalama();
    }

    private Camera CameraBul(
        string objectName)
    {
        GameObject go =
            GameObject.Find(
                objectName);

        if (go != null)
        {
            Camera c =
                go.GetComponent<Camera>();

            if (c != null)
                return c;
        }

        Camera[] all =
            FindObjectsOfType<Camera>(
                true);

        foreach (Camera c in all)
        {
            if (c != null &&
                string.Equals(
                    c.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return c;
            }
        }

        return null;
    }

    private static Transform FindDeep(
        Transform root,
        string wantedName)
    {
        if (root == null)
            return null;

        if (root.name ==
            wantedName)
            return root;

        for (int i = 0;
             i < root.childCount;
             i++)
        {
            Transform found =
                FindDeep(
                    root.GetChild(i),
                    wantedName);

            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindDeepContains(
        Transform root,
        string wantedPart)
    {
        if (root == null)
            return null;

        if (root.name.IndexOf(
                wantedPart,
                StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return root;
        }

        for (int i = 0;
             i < root.childCount;
             i++)
        {
            Transform found =
                FindDeepContains(
                    root.GetChild(i),
                    wantedPart);

            if (found != null)
                return found;
        }

        return null;
    }

    private void OnDisable()
    {
        // Editor'da component kapaninca modeli eski haline dondur.
        if (visualRoot != null &&
            visualBaseCaptured)
        {
            visualRoot.localPosition =
                visualBaseLocalPosition;

            visualRoot.localRotation =
                visualBaseLocalRotation;
        }

        foreach (
            KeyValuePair<Camera, float> pair
            in cameraBaseFov)
        {
            if (pair.Key != null)
                pair.Key.fieldOfView =
                    pair.Value;
        }
    }
}
