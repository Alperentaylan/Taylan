using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-1000)]
public class GLS580BasitSistem : MonoBehaviour
{
    [Header("SADECE BU ALANI KONTROL ET")]
    [Tooltip("Project panelindeki EnteringCar animasyon klibi.")]
    public AnimationClip enteringCarClip;

    [Tooltip("Project panelindeki Exiting Car / araçtan iniş animasyon klibi.")]
    public AnimationClip exitingCarClip;

    [Header("Etkileşim")]
    public KeyCode interactionKey = KeyCode.F;
    public float interactionDistance = 4f;

    [Header("Sürüş")]
    public float maxForwardSpeedKmh = 145f;
    public float maxReverseSpeedKmh = 25f;
    public float acceleration = 3.4f;
    public float reverseAcceleration = 2.4f;
    public float brakePower = 9f;
    public float coastSlowdown = 0.7f;
    public float handbrakePower = 16f;
    [Tooltip("Ön tekerlerin dururken ulaşacağı gerçekçi maksimum açı.")]
    public float lowSpeedSteer = 45f;
    [Tooltip("Yüksek hızda güvenlik için direksiyon açısı otomatik azalır.")]
    public float highSpeedSteer = 7f;
    [Tooltip("Ön tekerlerin sağ-sol dönme hızı (derece/sn).")]
    public float steeringVisualSpeed = 115f;

    [Header("Kapı / Animasyon")]
    public float doorAngle = -68f;
    public float doorMoveDuration = 0.38f;
    public float alignDuration = 0.25f;
    public bool useRootMotion = true;
    public float maxExitSpeedKmh = 1.5f;

    [Header("Bu modele göre düzeltilen oturma")]
    [Tooltip("Bu GLS modelinde fiziksel sol ön kapı FBX içinde Door_FR adında.")]
    public bool driverDoorIsDoorFR = true;
    [Tooltip("Karakter ters oturuyorsa 180 doğru değerdir.")]
    public float seatedCharacterYawCorrection = 180f;
    [Tooltip("Karakter kökünün araç tabanından yüksekliği.")]
    public float seatedRootHeightAboveGround = 0.12f;
    public float seatedInwardOffset = 0.42f;

    [Header("V6.1 - Kalçayı koltuğa sabitle")]
    [Tooltip("Koltuk minderinin yaklaşık yerden yüksekliği. Karakterin Hips kemiği tam buraya kilitlenir.")]
    public float hipsHeightAboveGround = 0.72f;
    [Tooltip("Şoför koltuğunu kapıdan aracın içine doğru ne kadar alacağımız.")]
    public float hipsInwardOffset = 0.48f;
    [Tooltip("Karakter öne/arkaya birkaç cm yanlışsa bunu ayarla.")]
    public float hipsForwardOffset = 0.02f;

    [Header("V6.4 - Oturuşu Inspector'dan ayarla")]
    [Tooltip("X = sağ/sol, Y = yukarı/aşağı, Z = ileri/geri. Karakter direksiyonun içindeyse Z'yi daha negatif yap.")]
    public Vector3 seatPositionAdjustment = new Vector3(0f, 0f, -0.35f);
    [Tooltip("Oturan karakterin yönünü ince ayarlamak için.")]
    public Vector3 seatRotationAdjustment = Vector3.zero;

    [Header("V6.7 - ÖN FARLAR - ELLE BAĞLA")]
    public KeyCode headlightsKey = KeyCode.H;
    public bool headlightsStartOn = false;
    [Tooltip("Sol öndeki Spot Light objesini buraya sürükle.")]
    public Light frontHeadlightLeft;
    [Tooltip("Sağ öndeki Spot Light objesini buraya sürükle.")]
    public Light frontHeadlightRight;

    [Header("V6.10 - Ön Far Gücü")]
    [Tooltip("Spot Light'ın yönünü KOD DEĞİŞTİRMEZ. Unity'de sen nasıl çevirdiysen öyle kalır.")]
    public float frontHeadlightRange = 55f;
    public float frontHeadlightIntensity = 25f;
    public float frontHeadlightSpotAngle = 60f;
    public float frontHeadlightInnerSpotAngle = 35f;

    [Header("V6.7 - ARKA FREN / GERİ VİTES - ELLE BAĞLA")]
    [Tooltip("Sol arkadaki Point Light objesini buraya sürükle.")]
    public Light rearLightLeft;
    [Tooltip("Sağ arkadaki Point Light objesini buraya sürükle.")]
    public Light rearLightRight;
    public Color brakeLightColor = new Color(1f, 0.015f, 0.005f, 1f);
    public Color reverseLightColor = Color.white;

    [Header("V6.4 - Araç Orbit Kamerası")]
    [Tooltip("Fare ile aracın etrafında dönme hassasiyeti.")]
    public float carCameraSensitivity = 3.0f;
    public float carCameraDistance = 5.6f;
    [Tooltip("Kameranın baktığı noktanın araç merkezinden yukarı ofseti.")]
    public float carCameraTargetHeight = 0.65f;
    public float carCameraMinPitch = -12f;
    public float carCameraMaxPitch = 68f;
    public bool invertCarCameraY = false;

    [Header("Yalnızca sorun olursa")]
    [Tooltip("W ile geri gidiyorsa işaretle.")]
    public bool reverseVehicleForward = false;

    [SerializeField] private Transform modelRoot;
    [SerializeField] private Transform driveRoot;
    [SerializeField] private Transform doorFL;
    [SerializeField] private Transform doorFR;
    [SerializeField] private Transform wheelFL;
    [SerializeField] private Transform wheelFR;
    [SerializeField] private Transform wheelRL;
    [SerializeField] private Transform wheelRR;
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform playerHips;
    [SerializeField] private CharacterController playerCharacterController;
    [SerializeField] private Camera carCamera;

    private Rigidbody carBody;
    private BoxCollider doorTarget;
    private GameObject greenOutline;

    private Quaternion doorClosedRelativeRotation;
    private WheelVisual wheelVisualFL;
    private WheelVisual wheelVisualFR;
    private WheelVisual wheelVisualRL;
    private WheelVisual wheelVisualRR;

    private float currentSteerAngle;
    private float frontLeftSteerAngle;
    private float frontRightSteerAngle;
    private float wheelSpinAngle;
    private float averageWheelRadius = 0.45f;
    private float currentDriveSpeed;
    private float wheelBase = 2.95f;
    private float frontTrackWidth = 1.68f;

    // V6.3: Gövde ve tekerler aynı DriveRoot ile birlikte hareket eder.
    private Vector3 lockedModelLocalPosition;
    private Quaternion lockedModelLocalRotation;
    private Vector3 lockedModelLocalScale;
    private bool modelLockedToDriveRoot;

    private bool setupComplete;
    private bool highlighted;
    private bool busy;
    private bool playerInside;
    private bool driving;

    private Transform originalPlayerParent;
    private bool originalAnimatorEnabled;
    private bool originalApplyRootMotion;
    private float originalAnimatorSpeed = 1f;

    private readonly List<SavedBehaviour> disabledPlayerScripts =
        new List<SavedBehaviour>();

    private readonly List<SavedCamera> savedCameras =
        new List<SavedCamera>();

    private PlayableGraph animationGraph;
    private AnimationClipPlayable animationPlayable;

    private AudioSource reverseAudio;
    private AudioClip reverseClip;

    private bool headlightsOn;

    private Vector3 carCameraTargetLocal;
    private float carCameraYaw;
    private float carCameraPitch = 14f;

    private Vector3 VehicleForward
    {
        get
        {
            Vector3 value = driveRoot != null ? driveRoot.forward : transform.forward;
            return reverseVehicleForward ? -value : value;
        }
    }

    private Vector3 VehicleRight
    {
        get
        {
            Vector3 value = driveRoot != null ? driveRoot.right : transform.right;
            return reverseVehicleForward ? -value : value;
        }
    }

    private class WheelVisual
    {
        public Transform wheel;
        public Transform steerPivot;
        public Transform spinPivot;
        public bool steering;
        public float radius;
    }

    private struct SavedBehaviour
    {
        public MonoBehaviour behaviour;
        public bool enabled;

        public SavedBehaviour(MonoBehaviour value, bool wasEnabled)
        {
            behaviour = value;
            enabled = wasEnabled;
        }
    }

    private struct SavedCamera
    {
        public Camera camera;
        public bool enabled;
        public bool active;

        public SavedCamera(Camera value, bool wasEnabled, bool wasActive)
        {
            camera = value;
            enabled = wasEnabled;
            active = wasActive;
        }
    }

    private void Awake()
    {
        modelRoot = transform;

        // Eski sürümde Inspector'a kaydedilen 180 derece ve Door_FR zorlamasını yok say.
        // Bu modelde fiziksel sol kapı aşağıda konumundan otomatik seçiliyor.
        seatedCharacterYawCorrection = 0f;
        driverDoorIsDoorFR = false;

        DisableOldVehicleScripts();
        FindParts();
        RemoveOldRuntimeHelpers();
        FindParts();

        if (!CreateCleanDriveRoot())
            return;

        LockWholeCarBodyToDriveRoot();

        SelectCorrectDriverDoor();
        DisableOldPhysics();
        CreateVehiclePhysics();
        CreateDoorTargetAndOutline();
        RebuildCorrectEntrySeatExitPoints();
        FindPlayer();
        CreateCarCamera();
        PrepareAssignedCarLights();
        CreateReverseAudio();
        CacheWheelVisuals();
        CalculateVehicleGeometry();

        if (doorFL != null)
        {
            doorClosedRelativeRotation =
                Quaternion.Inverse(driveRoot.rotation) * doorFL.rotation;
        }

        setupComplete =
            driveRoot != null &&
            carBody != null &&
            doorFL != null &&
            doorTarget != null &&
            wheelFL != null &&
            wheelFR != null &&
            wheelRL != null &&
            wheelRR != null &&
            playerRoot != null &&
            playerAnimator != null &&
            entryPoint != null &&
            seatPoint != null &&
            exitPoint != null;

        if (!setupComplete)
        {
            Debug.LogError(
                "GLS580 kurulumu tamamlanamadı. Sürücü kapısı, dört Wheel objesi veya Ch31_nonPBR (1) bulunamadı.",
                this);
        }

        SetOutline(false);
    }

    private void Start()
    {
        if (carCamera != null)
            carCamera.gameObject.SetActive(false);

        SetHeadlights(headlightsStartOn);
        SetRearLightsOff();
    }

    private void Update()
    {
        if (!setupComplete)
            return;

        HandleInteraction();

        if (playerInside)
        {
            HandleHeadlightsInput();
            HandleCarCameraOrbitInput();
            UpdateRearVehicleLights();
        }
        else
        {
            SetRearLightsOff();
        }

        UpdateReverseSound();
    }

    private void FixedUpdate()
    {
        if (!setupComplete)
            return;

        DriveVehicle();
        UpdateWheelVisuals();
    }

    private void LateUpdate()
    {
        // Gövde hiçbir şartta tekerlerden ayrı kalmasın.
        KeepWholeCarBodyAttached();

        if (!playerInside ||
            playerRoot == null ||
            seatPoint == null)
        {
            return;
        }

        // Karakter bağımsız hareket EDEMEZ.
        foreach (SavedBehaviour saved in disabledPlayerScripts)
        {
            if (saved.behaviour != null &&
                saved.behaviour.enabled)
            {
                saved.behaviour.enabled = false;
            }
        }

        if (playerCharacterController != null &&
            playerCharacterController.enabled)
        {
            playerCharacterController.enabled = false;
        }

        // V6.4: Oturuşu Inspector'daki X/Y/Z ile canlı ayarlayabilirsin.
        Quaternion adjustedSeatRotation = GetAdjustedSeatRotation();
        Vector3 adjustedSeatPosition = GetAdjustedSeatPosition();

        playerRoot.rotation =
            adjustedSeatRotation *
            Quaternion.Euler(
                0f,
                seatedCharacterYawCorrection,
                0f);

        // HIPS / KALÇA kemiğini ayarlanmış koltuk noktasına TAM kilitle.
        if (playerHips != null)
        {
            Vector3 correction =
                adjustedSeatPosition - playerHips.position;

            playerRoot.position += correction;
        }
        else
        {
            playerRoot.position = adjustedSeatPosition;
        }

        // EnteringCar'ın oturmuş son pozu bozulmasın.
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.applyRootMotion = false;
        }

        UpdateCarCameraOrbitTransform();
    }

    private void DisableOldVehicleScripts()
    {
        string[] oldNames =
        {
            "RealisticCarController",
            "CarDoorInteractable",
            "CarInteractionRaycaster",
            "GLS580DoorTrunkWheelController",
            "GLS580DoorTrunkController"
        };

        MonoBehaviour[] behaviours =
            Resources.FindObjectsOfTypeAll<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null ||
                !behaviour.gameObject.scene.IsValid() ||
                behaviour == this)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            for (int i = 0; i < oldNames.Length; i++)
            {
                if (typeName == oldNames[i])
                {
                    behaviour.enabled = false;
                    break;
                }
            }
        }
    }

    private void FindParts()
    {
        doorFL = FindDeepChild(modelRoot, "Door_FL");
        doorFR = FindDeepChild(modelRoot, "Door_FR");
        wheelFL = FindDeepChild(modelRoot, "Wheel_FL");
        wheelFR = FindDeepChild(modelRoot, "Wheel_FR");
        wheelRL = FindDeepChild(modelRoot, "Wheel_RL");
        wheelRR = FindDeepChild(modelRoot, "Wheel_RR");

        entryPoint = FindDeepChild(modelRoot, "EntryPoint");
        seatPoint = FindDeepChild(modelRoot, "SeatPoint");
        exitPoint = FindDeepChild(modelRoot, "ExitPoint");
    }

    private void RemoveOldRuntimeHelpers()
    {
        // Eski scriptin tekerleri altına taşıdığı pivotlardan çıkar.
        RestoreWheelParent(wheelFL);
        RestoreWheelParent(wheelFR);
        RestoreWheelParent(wheelRL);
        RestoreWheelParent(wheelRR);

        string[] helperNames =
        {
            "_Wheel_FL_DireksiyonPivot",
            "_Wheel_FR_DireksiyonPivot",
            "_Wheel_RL_DonusPivot",
            "_Wheel_RR_DonusPivot",
            "_BasitGövdeCollider",
            "_KapiEtkilesimAlani",
            "_YesilKapiCercevesi",
            "_GLS580_AracKamerasi",
            "_GLS580_DoorTarget",
            "_GLS580_GreenOutline",
            "_GLS580_EntryPoint_V4",
            "_GLS580_SeatPoint_V4",
            "_GLS580_ExitPoint_V4",
            "_GLS580_EntryPoint_V5",
            "_GLS580_SeatPoint_V5",
            "_GLS580_ExitPoint_V5",
            "_V6_2_FL_Steer",
            "_V6_2_FR_Steer",
            "_V6_2_FL_Spin",
            "_V6_2_FR_Spin",
            "_V6_2_RL_Spin",
            "_V6_2_RR_Spin"
        };

        foreach (string helperName in helperNames)
        {
            Transform helper = FindDeepChild(modelRoot, helperName);

            if (helper != null)
            {
                helper.gameObject.SetActive(false);
                Destroy(helper.gameObject);
            }
        }
    }

    private void RestoreWheelParent(Transform wheel)
    {
        if (wheel == null)
            return;

        if (wheel.parent != modelRoot)
            wheel.SetParent(modelRoot, true);
    }

    private bool CreateCleanDriveRoot()
    {
        if (wheelFL == null || wheelFR == null ||
            wheelRL == null || wheelRR == null)
        {
            return false;
        }

        if (modelRoot.parent != null &&
            modelRoot.parent.name == "_GLS580_DriveRoot_V2")
        {
            driveRoot = modelRoot.parent;
            return true;
        }

        Vector3 fl = GetRendererCenter(wheelFL);
        Vector3 fr = GetRendererCenter(wheelFR);
        Vector3 rl = GetRendererCenter(wheelRL);
        Vector3 rr = GetRendererCenter(wheelRR);

        Vector3 frontMid = (fl + fr) * 0.5f;
        Vector3 rearMid = (rl + rr) * 0.5f;
        Vector3 leftMid = (fl + rl) * 0.5f;
        Vector3 rightMid = (fr + rr) * 0.5f;

        Vector3 forward = (frontMid - rearMid).normalized;
        Vector3 right = (rightMid - leftMid).normalized;
        Vector3 up = Vector3.Cross(forward, right).normalized;

        if (forward.sqrMagnitude < 0.1f ||
            right.sqrMagnitude < 0.1f ||
            up.sqrMagnitude < 0.1f)
        {
            Debug.LogError("Araç yönleri teker merkezlerinden hesaplanamadı.", this);
            return false;
        }

        if (Vector3.Dot(up, Vector3.up) < 0f)
        {
            right = -right;
            up = -up;
        }

        right = Vector3.Cross(up, forward).normalized;
        forward = Vector3.Cross(right, up).normalized;

        GameObject rootObject = new GameObject("_GLS580_DriveRoot_V2");
        driveRoot = rootObject.transform;

        Transform oldParent = modelRoot.parent;
        driveRoot.SetParent(oldParent, true);

        driveRoot.position = (fl + fr + rl + rr) * 0.25f;
        driveRoot.rotation = Quaternion.LookRotation(forward, up);
        driveRoot.localScale = Vector3.one;

        modelRoot.SetParent(driveRoot, true);
        return true;
    }

    private void LockWholeCarBodyToDriveRoot()
    {
        if (modelRoot == null || driveRoot == null)
            return;

        // Gövde ile teker takımının ortak hareket kökü DriveRoot.
        if (modelRoot.parent != driveRoot)
            modelRoot.SetParent(driveRoot, true);

        // FBX/model objesinin üzerindeki eski Rigidbody, parent hareketini ayırabiliyordu.
        // Araç fiziğini SADECE driveRoot üzerindeki Rigidbody yönetecek.
        Rigidbody[] oldModelBodies =
            modelRoot.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody body in oldModelBodies)
        {
            if (body == null)
                continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = false;

            Destroy(body);
        }

        // Modelin mevcut doğru görünüş pozunu kaydet.
        lockedModelLocalPosition = modelRoot.localPosition;
        lockedModelLocalRotation = modelRoot.localRotation;
        lockedModelLocalScale = modelRoot.localScale;
        modelLockedToDriveRoot = true;
    }

    private void KeepWholeCarBodyAttached()
    {
        if (!modelLockedToDriveRoot ||
            modelRoot == null ||
            driveRoot == null)
        {
            return;
        }

        // Eğer başka bir runtime işlem modelin parent'ını koparırsa geri bağla.
        if (modelRoot.parent != driveRoot)
            modelRoot.SetParent(driveRoot, false);

        // Gövde, kapılar, bagaj ve Static_Body her karede teker takımının
        // hareket ettiği aynı DriveRoot pozunu takip eder.
        modelRoot.localPosition = lockedModelLocalPosition;
        modelRoot.localRotation = lockedModelLocalRotation;
        modelRoot.localScale = lockedModelLocalScale;
    }

    private void SelectCorrectDriverDoor()
    {
        Transform namedFL = FindDeepChild(modelRoot, "Door_FL");
        Transform namedFR = FindDeepChild(modelRoot, "Door_FR");

        if (namedFL == null)
        {
            doorFL = namedFR;
            return;
        }

        if (namedFR == null)
        {
            doorFL = namedFL;
            return;
        }

        Vector3 flCenter =
            driveRoot.InverseTransformPoint(GetRendererCenter(namedFL));

        Vector3 frCenter =
            driveRoot.InverseTransformPoint(GetRendererCenter(namedFR));

        // Temiz sürüş kökünde eksi X fiziksel sol taraftır.
        // İsimlere güvenmeden gerçekten solda duran ön kapıyı seç.
        doorFL = flCenter.x <= frCenter.x ? namedFL : namedFR;

        Debug.Log(
            "GLS580 fiziksel sol sürücü kapısı seçildi: " + doorFL.name,
            this);
    }

    private void DisableOldPhysics()
    {
        Collider[] oldColliders =
            modelRoot.GetComponentsInChildren<Collider>(true);

        foreach (Collider oldCollider in oldColliders)
        {
            if (oldCollider != null)
                oldCollider.enabled = false;
        }

        WheelCollider[] oldWheelColliders =
            modelRoot.GetComponentsInChildren<WheelCollider>(true);

        foreach (WheelCollider oldWheelCollider in oldWheelColliders)
        {
            if (oldWheelCollider != null)
                oldWheelCollider.enabled = false;
        }

        Rigidbody[] oldBodies =
            modelRoot.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody oldBody in oldBodies)
        {
            if (oldBody == null)
                continue;

            oldBody.isKinematic = true;
            oldBody.useGravity = false;
            oldBody.linearVelocity = Vector3.zero;
            oldBody.angularVelocity = Vector3.zero;
        }
    }

    private void CreateVehiclePhysics()
    {
        carBody = driveRoot.GetComponent<Rigidbody>();

        if (carBody == null)
            carBody = driveRoot.gameObject.AddComponent<Rigidbody>();

        carBody.mass = 2250f;
        carBody.useGravity = true;
        carBody.interpolation = RigidbodyInterpolation.Interpolate;
        carBody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        carBody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        carBody.linearDamping = 0.05f;
        carBody.angularDamping = 3f;

        Transform staticBody = FindDeepChild(modelRoot, "Static_Body");

        Renderer[] bodyRenderers =
            staticBody != null
                ? staticBody.GetComponentsInChildren<Renderer>(true)
                : modelRoot.GetComponentsInChildren<Renderer>(true);

        Bounds bodyBounds =
            CalculateLocalBounds(driveRoot, bodyRenderers);

        float groundY = CalculateWheelGroundY();
        float bodyHeight = Mathf.Max(0.5f, bodyBounds.size.y);
        float colliderBottom =
            Mathf.Min(bodyBounds.min.y + bodyHeight * 0.12f, groundY + 0.08f);

        float colliderTop =
            bodyBounds.min.y + bodyHeight * 0.76f;

        float colliderHeight =
            Mathf.Max(0.35f, colliderTop - colliderBottom);

        float zMin = bodyBounds.min.z;
        float zMax = bodyBounds.max.z;
        float zLength = zMax - zMin;

        CreateBoxCollider(
            "_Collider_Rear",
            new Vector3(
                bodyBounds.center.x,
                colliderBottom + colliderHeight * 0.5f,
                zMin + zLength * 0.17f),
            new Vector3(
                bodyBounds.size.x * 0.84f,
                colliderHeight,
                zLength * 0.36f));

        CreateBoxCollider(
            "_Collider_Middle",
            new Vector3(
                bodyBounds.center.x,
                colliderBottom + colliderHeight * 0.5f,
                zMin + zLength * 0.50f),
            new Vector3(
                bodyBounds.size.x * 0.90f,
                colliderHeight,
                zLength * 0.38f));

        CreateBoxCollider(
            "_Collider_Front",
            new Vector3(
                bodyBounds.center.x,
                colliderBottom + colliderHeight * 0.5f,
                zMin + zLength * 0.83f),
            new Vector3(
                bodyBounds.size.x * 0.84f,
                colliderHeight,
                zLength * 0.36f));

        float bottomHeight =
            Mathf.Max(0.18f, bodyBounds.size.y * 0.09f);

        CreateBoxCollider(
            "_Collider_Undercarriage",
            new Vector3(
                bodyBounds.center.x,
                groundY + bottomHeight * 0.5f + 0.02f,
                bodyBounds.center.z),
            new Vector3(
                bodyBounds.size.x * 0.72f,
                bottomHeight,
                bodyBounds.size.z * 0.86f));

        carBody.centerOfMass =
            new Vector3(
                bodyBounds.center.x,
                bodyBounds.min.y + bodyBounds.size.y * 0.32f,
                bodyBounds.center.z);
    }

    private void CreateBoxCollider(
        string objectName,
        Vector3 localCenter,
        Vector3 localSize)
    {
        GameObject holder = new GameObject(objectName);
        holder.transform.SetParent(driveRoot, false);
        holder.transform.localPosition = localCenter;
        holder.transform.localRotation = Quaternion.identity;
        holder.transform.localScale = Vector3.one;

        BoxCollider collider = holder.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = localSize;
        collider.isTrigger = false;
    }

    private float CalculateWheelGroundY()
    {
        Transform[] wheels = { wheelFL, wheelFR, wheelRL, wheelRR };
        float groundY = float.PositiveInfinity;

        foreach (Transform wheel in wheels)
        {
            Renderer renderer = wheel.GetComponentInChildren<Renderer>(true);

            if (renderer == null)
                continue;

            Vector3 centerLocal =
                driveRoot.InverseTransformPoint(renderer.bounds.center);

            Vector3 localTop =
                driveRoot.InverseTransformVector(
                    new Vector3(0f, renderer.bounds.extents.y, 0f));

            float radius = Mathf.Abs(localTop.y);
            groundY = Mathf.Min(groundY, centerLocal.y - radius);
        }

        if (float.IsInfinity(groundY))
            groundY = -0.5f;

        return groundY;
    }

    private void CreateDoorTargetAndOutline()
    {
        if (doorFL == null)
            return;

        Renderer[] doorRenderers =
            doorFL.GetComponentsInChildren<Renderer>(true);

        Bounds doorBounds =
            CalculateLocalBounds(driveRoot, doorRenderers);

        GameObject targetObject =
            new GameObject("_GLS580_DoorTarget");

        targetObject.transform.SetParent(driveRoot, false);
        targetObject.transform.localPosition = doorBounds.center;
        targetObject.transform.localRotation = Quaternion.identity;
        targetObject.transform.localScale = Vector3.one;

        doorTarget = targetObject.AddComponent<BoxCollider>();
        doorTarget.center = Vector3.zero;
        doorTarget.size =
            Vector3.Scale(
                doorBounds.size,
                new Vector3(1.15f, 1.10f, 1.25f));

        doorTarget.isTrigger = true;

        greenOutline =
            new GameObject("_GLS580_GreenOutline");

        greenOutline.transform.SetParent(targetObject.transform, false);
        greenOutline.transform.localPosition = Vector3.zero;
        greenOutline.transform.localRotation = Quaternion.identity;
        greenOutline.transform.localScale = Vector3.one;

        CreateOutlineLines(greenOutline.transform, doorTarget.size);
        greenOutline.SetActive(false);
    }

    private void CreateOutlineLines(Transform parent, Vector3 size)
    {
        Vector3 half = size * 0.5f;

        Vector3[] corners =
        {
            new Vector3(-half.x, -half.y, -half.z),
            new Vector3( half.x, -half.y, -half.z),
            new Vector3( half.x,  half.y, -half.z),
            new Vector3(-half.x,  half.y, -half.z),
            new Vector3(-half.x, -half.y,  half.z),
            new Vector3( half.x, -half.y,  half.z),
            new Vector3( half.x,  half.y,  half.z),
            new Vector3(-half.x,  half.y,  half.z)
        };

        int[,] edges =
        {
            {0,1}, {1,2}, {2,3}, {3,0},
            {4,5}, {5,6}, {6,7}, {7,4},
            {0,4}, {1,5}, {2,6}, {3,7}
        };

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        Color green = new Color(0.02f, 1f, 0.05f, 1f);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", green);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", green);

        if (material.HasProperty("_ZTest"))
            material.SetInt("_ZTest", (int)CompareFunction.Always);

        material.renderQueue = 4000;

        float width = Mathf.Max(0.025f, size.magnitude * 0.008f);

        for (int i = 0; i < 12; i++)
        {
            GameObject edge = new GameObject("Edge_" + i);
            edge.transform.SetParent(parent, false);

            LineRenderer line = edge.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, corners[edges[i, 0]]);
            line.SetPosition(1, corners[edges[i, 1]]);
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = material;
            line.startColor = green;
            line.endColor = green;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;
        }
    }

    private void RebuildCorrectEntrySeatExitPoints()
    {
        Bounds bodyBounds =
            CalculateLocalBounds(
                driveRoot,
                modelRoot.GetComponentsInChildren<Renderer>(true));

        Bounds doorBounds =
            CalculateLocalBounds(
                driveRoot,
                doorFL.GetComponentsInChildren<Renderer>(true));

        float groundY = CalculateWheelGroundY();

        entryPoint =
            CreateOrResetPoint("_GLS580_EntryPoint_V5");

        exitPoint =
            CreateOrResetPoint("_GLS580_ExitPoint_V5");

        seatPoint =
            CreateOrResetPoint("_GLS580_SeatPoint_V5");

        // Sürücü tarafı kesin olarak aracın fiziksel SOLU: driveRoot local X negatif.
        float driverSeatX =
            bodyBounds.min.x + bodyBounds.size.x * 0.27f;

        float outsideDoorX =
            bodyBounds.min.x - Mathf.Max(0.45f, bodyBounds.size.x * 0.18f);

        float seatZ =
            Mathf.Clamp(
                doorBounds.center.z + bodyBounds.size.z * 0.02f,
                bodyBounds.min.z + bodyBounds.size.z * 0.42f,
                bodyBounds.max.z - bodyBounds.size.z * 0.18f);

        // Karakter kapının dışında, zeminde ve aracın içine bakar.
        entryPoint.localPosition =
            new Vector3(
                outsideDoorX,
                groundY + 0.03f,
                doorBounds.center.z);

        entryPoint.localRotation =
            Quaternion.LookRotation(Vector3.right, Vector3.up);

        exitPoint.localPosition =
            entryPoint.localPosition +
            new Vector3(-0.30f, 0f, 0f);

        exitPoint.localRotation =
            entryPoint.localRotation;

        // V6.1: SeatPoint artık karakter ROOT'u değil HIPS/KALÇA kemiğini temsil ediyor.
        // Böylece karakter kendi kendine kayamaz; kalçası koltuğun minderine sabitlenir.
        seatPoint.localPosition =
            new Vector3(
                bodyBounds.min.x + bodyBounds.size.x * 0.27f + hipsInwardOffset * 0.10f,
                groundY + hipsHeightAboveGround,
                seatZ + hipsForwardOffset);

        seatPoint.localRotation =
            Quaternion.Euler(
                0f,
                reverseVehicleForward ? 180f : 0f,
                0f);
    }

    private Vector3 GetAdjustedSeatPosition()
    {
        if (seatPoint == null)
            return Vector3.zero;

        // Offset araç eksenlerinde uygulanır.
        return seatPoint.position +
               driveRoot.TransformDirection(seatPositionAdjustment);
    }

    private Quaternion GetAdjustedSeatRotation()
    {
        if (seatPoint == null)
            return Quaternion.identity;

        return seatPoint.rotation *
               Quaternion.Euler(seatRotationAdjustment);
    }

    private Transform CreateOrResetPoint(string pointName)
    {
        Transform point = FindDeepChild(driveRoot, pointName);

        if (point == null)
        {
            GameObject pointObject = new GameObject(pointName);
            point = pointObject.transform;
        }

        point.SetParent(driveRoot, false);
        point.localScale = Vector3.one;
        return point;
    }

    private Transform CreatePoint(string nameValue)
    {
        GameObject pointObject = new GameObject(nameValue);
        pointObject.transform.SetParent(driveRoot, false);
        return pointObject.transform;
    }

    private void FindPlayer()
    {
        GameObject exact = GameObject.Find("Ch31_nonPBR (1)");

        if (exact != null)
            playerRoot = exact.transform;

        if (playerRoot == null)
        {
            Transform[] transforms =
                Resources.FindObjectsOfTypeAll<Transform>();

            foreach (Transform candidate in transforms)
            {
                if (candidate != null &&
                    candidate.gameObject.scene.IsValid() &&
                    candidate.name.StartsWith("Ch31_nonPBR"))
                {
                    playerRoot = candidate;
                    break;
                }
            }
        }

        if (playerRoot == null)
            return;

        playerAnimator = playerRoot.GetComponent<Animator>();

        if (playerAnimator == null)
            playerAnimator = playerRoot.GetComponentInChildren<Animator>(true);

        playerHips = FindPlayerHips();

        playerCharacterController =
            playerRoot.GetComponent<CharacterController>();

        if (playerCharacterController == null)
        {
            playerCharacterController =
                playerRoot.GetComponentInChildren<CharacterController>(true);
        }
    }

    private void CreateCarCamera()
    {
        Bounds bodyBounds =
            CalculateLocalBounds(
                driveRoot,
                modelRoot.GetComponentsInChildren<Renderer>(true));

        GameObject cameraObject =
            new GameObject("_GLS580_CarCamera");

        cameraObject.transform.SetParent(driveRoot, false);

        carCamera = cameraObject.AddComponent<Camera>();
        carCamera.fieldOfView = 62f;
        carCamera.nearClipPlane = 0.05f;
        carCamera.tag = "Untagged";

        AudioListener listener =
            cameraObject.AddComponent<AudioListener>();

        listener.enabled = false;

        // Orbit kameranın baktığı araç-local hedef.
        carCameraTargetLocal =
            bodyBounds.center +
            Vector3.up * carCameraTargetHeight;

        carCameraYaw = 0f;
        carCameraPitch = 14f;

        UpdateCarCameraOrbitTransform();
        cameraObject.SetActive(false);
    }

    private void HandleCarCameraOrbitInput()
    {
        if (carCamera == null ||
            !carCamera.gameObject.activeInHierarchy)
        {
            return;
        }

        float mouseX =
            Input.GetAxis("Mouse X");

        float mouseY =
            Input.GetAxis("Mouse Y");

        carCameraYaw +=
            mouseX * carCameraSensitivity;

        float ySign =
            invertCarCameraY ? 1f : -1f;

        carCameraPitch +=
            mouseY *
            carCameraSensitivity *
            ySign;

        carCameraPitch =
            Mathf.Clamp(
                carCameraPitch,
                carCameraMinPitch,
                carCameraMaxPitch);

        // Yaw taşmasını önle; davranış değişmez.
        if (carCameraYaw > 360f)
            carCameraYaw -= 360f;
        else if (carCameraYaw < -360f)
            carCameraYaw += 360f;
    }

    private void UpdateCarCameraOrbitTransform()
    {
        if (carCamera == null ||
            driveRoot == null)
        {
            return;
        }

        Vector3 targetWorld =
            driveRoot.TransformPoint(carCameraTargetLocal);

        Quaternion localOrbit =
            Quaternion.Euler(
                carCameraPitch,
                carCameraYaw,
                0f);

        Vector3 localDirection =
            localOrbit * Vector3.back;

        Vector3 worldDirection =
            driveRoot.TransformDirection(localDirection);

        carCamera.transform.position =
            targetWorld +
            worldDirection * carCameraDistance;

        Vector3 lookDirection =
            targetWorld - carCamera.transform.position;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            carCamera.transform.rotation =
                Quaternion.LookRotation(
                    lookDirection,
                    driveRoot.up);
        }
    }

    private void PrepareAssignedCarLights()
    {
        // Bu sürüm artık HİÇBİR light aramaz.
        // Inspector'da hangi 4 ışığı verirsen sadece onları kullanır.

        PrepareOneFrontHeadlight(frontHeadlightLeft);
        PrepareOneFrontHeadlight(frontHeadlightRight);

        if (frontHeadlightLeft != null)
        {
            frontHeadlightLeft.gameObject.SetActive(true);
            frontHeadlightLeft.enabled = headlightsStartOn;
        }

        if (frontHeadlightRight != null)
        {
            frontHeadlightRight.gameObject.SetActive(true);
            frontHeadlightRight.enabled = headlightsStartOn;
        }

        headlightsOn = headlightsStartOn;

        if (rearLightLeft != null)
            rearLightLeft.enabled = false;

        if (rearLightRight != null)
            rearLightRight.enabled = false;

        Debug.Log(
            "GLS580 V6.7 ışıklar: ÖnSol=" +
            (frontHeadlightLeft != null ? frontHeadlightLeft.name : "YOK") +
            " ÖnSağ=" +
            (frontHeadlightRight != null ? frontHeadlightRight.name : "YOK") +
            " ArkaSol=" +
            (rearLightLeft != null ? rearLightLeft.name : "YOK") +
            " ArkaSağ=" +
            (rearLightRight != null ? rearLightRight.name : "YOK"),
            this);
    }

    private void PrepareOneFrontHeadlight(Light lightItem)
    {
        if (lightItem == null)
            return;

        // SADECE ışık ayarları. Transform/Rotation/Pozisyon ASLA değişmez.
        lightItem.type = LightType.Spot;
        lightItem.range = frontHeadlightRange;
        lightItem.intensity = frontHeadlightIntensity;
        lightItem.spotAngle = frontHeadlightSpotAngle;
        lightItem.innerSpotAngle =
            Mathf.Min(frontHeadlightInnerSpotAngle, frontHeadlightSpotAngle);
        lightItem.color = Color.white;
        lightItem.cullingMask = ~0;
        lightItem.shadows = LightShadows.None;
        lightItem.gameObject.SetActive(true);
    }

    private void HandleHeadlightsInput()
    {
        if (Input.GetKeyDown(headlightsKey) ||
            (headlightsKey != KeyCode.H && Input.GetKeyDown(KeyCode.H)))
        {
            SetHeadlights(!headlightsOn);
        }
    }

    private void SetHeadlights(bool enabledValue)
    {
        headlightsOn = enabledValue;

        PrepareOneFrontHeadlight(frontHeadlightLeft);
        PrepareOneFrontHeadlight(frontHeadlightRight);

        if (frontHeadlightLeft != null)
            frontHeadlightLeft.enabled = enabledValue;

        if (frontHeadlightRight != null)
            frontHeadlightRight.enabled = enabledValue;

        Debug.Log(
            "Ön farlar: " +
            (enabledValue ? "AÇIK" : "KAPALI") +
            " | Sol=" +
            (frontHeadlightLeft != null ? frontHeadlightLeft.name : "YOK") +
            " | Sağ=" +
            (frontHeadlightRight != null ? frontHeadlightRight.name : "YOK") +
            " | Intensity=" +
            frontHeadlightIntensity +
            " | Range=" +
            frontHeadlightRange,
            this);
    }

    private void UpdateRearVehicleLights()
    {
        bool sPressed = Input.GetKey(KeyCode.S);
        bool handbrakePressed = Input.GetKey(KeyCode.Space);

        // Geri gidiyorsa veya durmuşken S ile geri vitese geçiyorsa BEYAZ.
        bool reversing =
            currentDriveSpeed < -0.05f ||
            (sPressed && currentDriveSpeed <= 0.25f);

        if (reversing)
        {
            SetRearLightState(true, reverseLightColor);
            return;
        }

        // İleri giderken S = fren. Space = el freni/fren.
        bool braking =
            (sPressed && currentDriveSpeed > 0.25f) ||
            handbrakePressed;

        if (braking)
        {
            SetRearLightState(true, brakeLightColor);
            return;
        }

        SetRearLightsOff();
    }

    private void SetRearLightState(bool enabledValue, Color colorValue)
    {
        if (rearLightLeft != null)
        {
            rearLightLeft.color = colorValue;
            rearLightLeft.enabled = enabledValue;
        }

        if (rearLightRight != null)
        {
            rearLightRight.color = colorValue;
            rearLightRight.enabled = enabledValue;
        }
    }

    private void SetRearLightsOff()
    {
        if (rearLightLeft != null)
            rearLightLeft.enabled = false;

        if (rearLightRight != null)
            rearLightRight.enabled = false;
    }

    private void CreateReverseAudio()
    {
        reverseAudio = driveRoot.gameObject.AddComponent<AudioSource>();
        reverseAudio.playOnAwake = false;
        reverseAudio.loop = true;
        reverseAudio.spatialBlend = 1f;
        reverseAudio.minDistance = 2f;
        reverseAudio.maxDistance = 30f;
        reverseAudio.volume = 0.55f;

        reverseClip = GenerateReverseClip();
        reverseAudio.clip = reverseClip;
    }

    private AudioClip GenerateReverseClip()
    {
        const int sampleRate = 44100;
        const float duration = 1f;

        int count = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float time = i / (float)sampleRate;
            float cycle = time % 0.5f;

            if (cycle >= 0.18f)
                continue;

            float envelope = 1f;

            if (cycle < 0.01f)
                envelope = cycle / 0.01f;
            else if (cycle > 0.17f)
                envelope = (0.18f - cycle) / 0.01f;

            data[i] =
                Mathf.Sin(2f * Mathf.PI * 900f * time) *
                0.35f *
                Mathf.Clamp01(envelope);
        }

        AudioClip clip =
            AudioClip.Create(
                "GLS580_Reverse_Beep",
                count,
                1,
                sampleRate,
                false);

        clip.SetData(data, 0);
        return clip;
    }

    private void CacheWheelVisuals()
    {
        wheelVisualFL = CreateSafeWheelVisual(wheelFL, true, "_V6_2_FL");
        wheelVisualFR = CreateSafeWheelVisual(wheelFR, true, "_V6_2_FR");
        wheelVisualRL = CreateSafeWheelVisual(wheelRL, false, "_V6_2_RL");
        wheelVisualRR = CreateSafeWheelVisual(wheelRR, false, "_V6_2_RR");

        WheelVisual[] visuals =
        {
            wheelVisualFL,
            wheelVisualFR,
            wheelVisualRL,
            wheelVisualRR
        };

        float radiusSum = 0f;
        int radiusCount = 0;

        foreach (WheelVisual visual in visuals)
        {
            if (visual == null)
                continue;

            radiusSum += visual.radius;
            radiusCount++;
        }

        if (radiusCount > 0)
            averageWheelRadius = radiusSum / radiusCount;
    }

    private void CalculateVehicleGeometry()
    {
        if (driveRoot == null ||
            wheelFL == null || wheelFR == null ||
            wheelRL == null || wheelRR == null)
        {
            return;
        }

        Vector3 fl = driveRoot.InverseTransformPoint(GetRendererCenter(wheelFL));
        Vector3 fr = driveRoot.InverseTransformPoint(GetRendererCenter(wheelFR));
        Vector3 rl = driveRoot.InverseTransformPoint(GetRendererCenter(wheelRL));
        Vector3 rr = driveRoot.InverseTransformPoint(GetRendererCenter(wheelRR));

        Vector3 frontMid = (fl + fr) * 0.5f;
        Vector3 rearMid = (rl + rr) * 0.5f;

        wheelBase = Mathf.Max(1.8f, Mathf.Abs(frontMid.z - rearMid.z));
        frontTrackWidth = Mathf.Max(1.2f, Mathf.Abs(fr.x - fl.x));

        Debug.Log(
            "GLS580 direksiyon geometrisi: Wheelbase=" +
            wheelBase.ToString("0.00") +
            "m Track=" +
            frontTrackWidth.ToString("0.00") +
            "m",
            this);
    }

    private WheelVisual CreateSafeWheelVisual(
        Transform wheel,
        bool steering,
        string prefix)
    {
        if (wheel == null)
            return null;

        Renderer renderer =
            wheel.GetComponentInChildren<Renderer>(true);

        Vector3 center =
            renderer != null
                ? renderer.bounds.center
                : wheel.position;

        float radius =
            renderer != null
                ? Mathf.Max(0.15f, renderer.bounds.extents.y)
                : 0.45f;

        Transform steerPivot = null;

        // SADECE ÖN TEKERLERE sağ-sol pivotu oluştur.
        if (steering)
        {
            GameObject steerObject =
                new GameObject(prefix + "_Steer");

            steerPivot = steerObject.transform;
            steerPivot.SetParent(driveRoot, true);
            steerPivot.position = center;
            steerPivot.rotation = driveRoot.rotation;
            steerPivot.localScale = Vector3.one;
        }

        // Dört tekerde de yalnızca yuvarlanma için spin pivotu var.
        GameObject spinObject =
            new GameObject(prefix + "_Spin");

        Transform spinPivot = spinObject.transform;

        if (steerPivot != null)
            spinPivot.SetParent(steerPivot, true);
        else
            spinPivot.SetParent(driveRoot, true);

        spinPivot.position = center;
        spinPivot.rotation = driveRoot.rotation;
        spinPivot.localScale = Vector3.one;

        // World position/rotation/scale aynen korunur.
        wheel.SetParent(spinPivot, true);

        WheelVisual visual = new WheelVisual();
        visual.wheel = wheel;
        visual.steerPivot = steerPivot;
        visual.spinPivot = spinPivot;
        visual.steering = steering;
        visual.radius = radius;

        return visual;
    }

    private void HandleInteraction()
    {
        if (busy)
        {
            SetOutline(false);
            return;
        }

        if (playerInside)
        {
            SetOutline(false);

            if (Input.GetKeyDown(interactionKey))
                TryExit();

            return;
        }

        Camera viewCamera = FindActivePlayerCamera();

        if (viewCamera == null || doorTarget == null)
        {
            SetOutline(false);
            return;
        }

        bool onDoor = CrosshairHitsDoor(viewCamera);
        SetOutline(onDoor);

        if (onDoor && Input.GetKeyDown(interactionKey))
            StartCoroutine(EnterRoutine());
    }

    private bool CrosshairHitsDoor(Camera viewCamera)
    {
        if (playerRoot == null || doorTarget == null)
            return false;

        Bounds bounds = doorTarget.bounds;

        // Mesafeyi kameradan değil KARAKTERDEN ölç.
        // Üçüncü şahıs kamera uzakta olduğu için önceki sürüm burada sürekli false dönüyordu.
        Vector3 closestPoint = bounds.ClosestPoint(playerRoot.position);
        float playerDistance = Vector3.Distance(playerRoot.position, closestPoint);

        if (playerDistance > interactionDistance)
            return false;

        Ray ray =
            viewCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));

        // Kamera uzakta olabilir; ray mesafesini interactionDistance ile sınırlama.
        if (doorTarget.Raycast(ray, out RaycastHit hit, 500f))
            return true;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, max.y, max.z)
        };

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        bool anyInFront = false;

        foreach (Vector3 corner in corners)
        {
            Vector3 screen = viewCamera.WorldToScreenPoint(corner);

            if (screen.z <= 0f)
                continue;

            anyInFront = true;
            minX = Mathf.Min(minX, screen.x);
            maxX = Mathf.Max(maxX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxY = Mathf.Max(maxY, screen.y);
        }

        if (!anyInFront)
            return false;

        // Kapının ekran alanına küçük tolerans ekle.
        const float screenPadding = 45f;
        minX -= screenPadding;
        maxX += screenPadding;
        minY -= screenPadding;
        maxY += screenPadding;

        Vector2 center =
            new Vector2(
                viewCamera.pixelWidth * 0.5f,
                viewCamera.pixelHeight * 0.5f);

        if (center.x >= minX &&
            center.x <= maxX &&
            center.y >= minY &&
            center.y <= maxY)
        {
            return true;
        }

        // Crosshair sistemi tam ekran merkezinde değilse yakın açı toleransı.
        Vector3 toDoor = bounds.center - viewCamera.transform.position;
        float lookAngle = Vector3.Angle(viewCamera.transform.forward, toDoor);

        return lookAngle <= 24f;
    }

    private Camera FindActivePlayerCamera()
    {
        Camera main = Camera.main;

        if (main != null &&
            main != carCamera &&
            main.isActiveAndEnabled)
        {
            return main;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);

        foreach (Camera cameraItem in cameras)
        {
            if (cameraItem != null &&
                cameraItem != carCamera &&
                cameraItem.isActiveAndEnabled)
            {
                return cameraItem;
            }
        }

        return null;
    }

    private void SetOutline(bool enabledValue)
    {
        if (highlighted == enabledValue)
            return;

        highlighted = enabledValue;

        if (greenOutline != null)
            greenOutline.SetActive(enabledValue);
    }

    private void OnGUI()
    {
        DrawDrivingHud();

        if (!highlighted || playerInside || busy)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = Mathf.Max(18, Screen.height / 45);
        style.normal.textColor = Color.white;

        Rect rect = new Rect(
            Screen.width * 0.5f - 105f,
            Screen.height * 0.5f + 45f,
            210f,
            42f);

        GUI.Box(rect, "F - Araca Bin", style);
    }

    private void DrawDrivingHud()
    {
        if (!playerInside)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = Mathf.Max(16, Screen.height / 55);
        style.normal.textColor = Color.white;

        Rect rect = new Rect(20f, 20f, 310f, 72f);
        GUI.Box(
            rect,
            "W Gaz | S Fren/Geri | A-D Direksiyon | Space El Freni | H Ön Far | Mouse Kamera\nHız: " +
            Mathf.RoundToInt(Mathf.Abs(currentDriveSpeed) * 3.6f) +
            " km/h | Direksiyon: " +
            Mathf.RoundToInt(currentSteerAngle) +
            "° | Kalça Kilidi: " +
            (playerHips != null ? "AKTİF" : "FALLBACK"),
            style);
    }

    private IEnumerator EnterRoutine()
    {
        if (busy || playerInside)
            yield break;

        busy = true;
        driving = false;
        SetOutline(false);

        SaveAndDisablePlayerScripts();

        originalPlayerParent = playerRoot.parent;
        originalAnimatorEnabled = playerAnimator.enabled;
        originalApplyRootMotion = playerAnimator.applyRootMotion;
        originalAnimatorSpeed = playerAnimator.speed;

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        yield return MovePlayer(entryPoint, alignDuration);
        yield return MoveDoor(true);
        yield return PlayEnteringAnimation();

        playerAnimator.applyRootMotion = false;

        // Son oturma pozunu sabitle, ardından karakter kökünü gerçek koltuk noktasına koy.
        if (animationPlayable.IsValid())
        {
            animationPlayable.SetTime(
                enteringCarClip != null ? enteringCarClip.length : 0f);

            animationPlayable.SetSpeed(0f);

            if (animationGraph.IsValid())
                animationGraph.Evaluate(0f);
        }

        // V6.1:
        // Animasyonun son karesi Playable üzerinde zaten speed=0.
        // Graph'ı ve Animator'ı açık bırakıyoruz; karakterin iskeleti bozulmuyor.
        playerAnimator.enabled = true;

        // Parent ETMİYORUZ. İlk kalça kilidini ayarlanmış koltuk hedefinde uygula.
        Quaternion adjustedSeatRotation = GetAdjustedSeatRotation();
        Vector3 adjustedSeatPosition = GetAdjustedSeatPosition();

        playerRoot.rotation =
            adjustedSeatRotation *
            Quaternion.Euler(0f, seatedCharacterYawCorrection, 0f);

        if (playerHips != null)
        {
            Vector3 hipsDelta =
                adjustedSeatPosition - playerHips.position;

            playerRoot.position += hipsDelta;
        }
        else
        {
            playerRoot.position = adjustedSeatPosition;
        }

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        yield return null;

        yield return MoveDoor(false);

        SwitchToCarCamera();

        playerInside = true;
        driving = true;
        busy = false;
    }

    private IEnumerator PlayEnteringAnimation()
    {
        if (enteringCarClip == null)
        {
            yield return new WaitForSeconds(0.35f);
            yield break;
        }

        DestroyAnimationGraph();

        playerAnimator.enabled = true;
        playerAnimator.applyRootMotion = useRootMotion;

        animationGraph =
            PlayableGraph.Create("GLS580_EnteringCar");

        AnimationPlayableOutput output =
            AnimationPlayableOutput.Create(
                animationGraph,
                "EnteringCarOutput",
                playerAnimator);

        animationPlayable =
            AnimationClipPlayable.Create(
                animationGraph,
                enteringCarClip);

        animationPlayable.SetApplyFootIK(true);
        output.SetSourcePlayable(animationPlayable);
        animationGraph.Play();

        float duration = Mathf.Max(0.1f, enteringCarClip.length);
        yield return new WaitForSeconds(duration);

        if (animationPlayable.IsValid())
        {
            animationPlayable.SetTime(duration);
            animationPlayable.SetSpeed(0f);

            if (animationGraph.IsValid())
                animationGraph.Evaluate(0f);
        }
    }

    private IEnumerator PlayExitingAnimation()
    {
        if (exitingCarClip == null)
        {
            // Klip bağlanmadıysa eski sistem gibi doğrudan dışarı çıkar.
            yield return new WaitForSeconds(0.15f);
            yield break;
        }

        DestroyAnimationGraph();

        playerAnimator.enabled = true;
        playerAnimator.speed = 1f;
        playerAnimator.applyRootMotion = useRootMotion;

        animationGraph =
            PlayableGraph.Create("GLS580_ExitingCar");

        AnimationPlayableOutput output =
            AnimationPlayableOutput.Create(
                animationGraph,
                "ExitingCarOutput",
                playerAnimator);

        animationPlayable =
            AnimationClipPlayable.Create(
                animationGraph,
                exitingCarClip);

        animationPlayable.SetApplyFootIK(true);
        animationPlayable.SetTime(0f);
        animationPlayable.SetSpeed(1f);

        output.SetSourcePlayable(animationPlayable);
        animationGraph.Play();

        float duration =
            Mathf.Max(0.1f, exitingCarClip.length);

        yield return new WaitForSeconds(duration);

        // Çıkış animasyonu bittikten sonra graph kapatılır.
        // Son pozda kilitlemiyoruz çünkü karakter tekrar normal kontrole geçecek.
        DestroyAnimationGraph();
    }

    private IEnumerator MovePlayer(Transform target, float duration)
    {
        Vector3 startPosition = playerRoot.position;
        Quaternion startRotation = playerRoot.rotation;

        if (duration <= 0f)
        {
            playerRoot.SetPositionAndRotation(
                target.position,
                target.rotation);

            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(time / duration));

            playerRoot.position =
                Vector3.Lerp(startPosition, target.position, t);

            playerRoot.rotation =
                Quaternion.Slerp(startRotation, target.rotation, t);

            yield return null;
        }

        playerRoot.SetPositionAndRotation(
            target.position,
            target.rotation);
    }

    private IEnumerator MoveDoor(bool open)
    {
        Quaternion closedWorld =
            driveRoot.rotation * doorClosedRelativeRotation;

        Bounds doorBounds =
            CalculateLocalBounds(
                driveRoot,
                doorFL.GetComponentsInChildren<Renderer>(true));

        Bounds bodyBounds =
            CalculateLocalBounds(
                driveRoot,
                modelRoot.GetComponentsInChildren<Renderer>(true));

        float sideSign =
            Mathf.Sign(doorBounds.center.x - bodyBounds.center.x);

        if (Mathf.Approximately(sideSign, 0f))
            sideSign = -1f;

        // Sol kapı dışarı doğru +Y ekseninde, sağ kapı -Y ekseninde açılır.
        float automaticOpenAngle =
            sideSign < 0f
                ? Mathf.Abs(doorAngle)
                : -Mathf.Abs(doorAngle);

        Quaternion target =
            open
                ? Quaternion.AngleAxis(
                    automaticOpenAngle,
                    driveRoot.up) * closedWorld
                : closedWorld;

        Quaternion start = doorFL.rotation;

        if (doorMoveDuration <= 0f)
        {
            doorFL.rotation = target;
            yield break;
        }

        float time = 0f;

        while (time < doorMoveDuration)
        {
            time += Time.deltaTime;
            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(time / doorMoveDuration));

            doorFL.rotation =
                Quaternion.Slerp(start, target, t);

            yield return null;
        }

        doorFL.rotation = target;
    }

    private void TryExit()
    {
        if (busy || !playerInside || GetSpeedKmh() > maxExitSpeedKmh)
            return;

        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        busy = true;
        driving = false;
        currentDriveSpeed = 0f;

        // Önce kapı açılır.
        yield return MoveDoor(true);

        // EnteringCar'ın donmuş graph'ını kapat.
        DestroyAnimationGraph();

        // Bu noktadan sonra LateUpdate karakteri koltuğa kilitlemesin.
        // busy=true olduğu için F ile tekrar etkileşim de olmaz.
        playerInside = false;

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        // Karakter oturduğu yerden Exiting Car animasyonunu oynatır.
        yield return PlayExitingAnimation();

        // Animasyon sonunda karakteri garanti olarak kapının dışındaki ExitPoint'e koy.
        if (originalPlayerParent != null &&
            playerRoot.parent != originalPlayerParent)
        {
            playerRoot.SetParent(originalPlayerParent, true);
        }

        playerRoot.position = exitPoint.position;
        playerRoot.rotation = exitPoint.rotation;

        playerAnimator.enabled = originalAnimatorEnabled;
        playerAnimator.applyRootMotion = originalApplyRootMotion;
        playerAnimator.speed =
            originalAnimatorSpeed <= 0f ? 1f : originalAnimatorSpeed;

        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        RestorePlayerScripts();
        RestorePlayerCameras();

        // Son olarak kapı kapanır.
        yield return MoveDoor(false);

        busy = false;
    }

    private void SaveAndDisablePlayerScripts()
    {
        disabledPlayerScripts.Clear();

        MonoBehaviour[] behaviours =
            Resources.FindObjectsOfTypeAll<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null ||
                !behaviour.gameObject.scene.IsValid() ||
                behaviour == this)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName == "KarakterHareketi" ||
                typeName == "BirinciUcuncuSahisKesin")
            {
                disabledPlayerScripts.Add(
                    new SavedBehaviour(behaviour, behaviour.enabled));

                behaviour.enabled = false;
            }
        }
    }

    private void RestorePlayerScripts()
    {
        foreach (SavedBehaviour saved in disabledPlayerScripts)
        {
            if (saved.behaviour != null)
                saved.behaviour.enabled = saved.enabled;
        }

        disabledPlayerScripts.Clear();
    }

    private void SwitchToCarCamera()
    {
        savedCameras.Clear();

        Camera[] cameras = FindObjectsOfType<Camera>(true);

        foreach (Camera cameraItem in cameras)
        {
            if (cameraItem == null || cameraItem == carCamera)
                continue;

            savedCameras.Add(
                new SavedCamera(
                    cameraItem,
                    cameraItem.enabled,
                    cameraItem.gameObject.activeSelf));

            cameraItem.enabled = false;

            AudioListener listener =
                cameraItem.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = false;
        }

        if (carCamera != null)
        {
            carCamera.gameObject.SetActive(true);
            carCamera.enabled = true;

            AudioListener listener =
                carCamera.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = true;
        }

        AudioListener[] allListeners =
            FindObjectsOfType<AudioListener>(true);

        foreach (AudioListener listener in allListeners)
        {
            if (listener == null)
                continue;

            if (carCamera != null &&
                listener.gameObject == carCamera.gameObject)
            {
                listener.enabled = true;
            }
            else
            {
                listener.enabled = false;
            }
        }
    }

    private void RestorePlayerCameras()
    {
        if (carCamera != null)
        {
            AudioListener listener =
                carCamera.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = false;

            carCamera.enabled = false;
            carCamera.gameObject.SetActive(false);
        }

        foreach (SavedCamera saved in savedCameras)
        {
            if (saved.camera == null)
                continue;

            saved.camera.gameObject.SetActive(saved.active);
            saved.camera.enabled = saved.enabled;

            AudioListener listener =
                saved.camera.GetComponent<AudioListener>();

            if (listener != null && saved.active && saved.enabled)
                listener.enabled = true;
        }

        savedCameras.Clear();
    }

    private void DriveVehicle()
    {
        if (carBody == null || driveRoot == null)
            return;

        bool w = driving && Input.GetKey(KeyCode.W);
        bool s = driving && Input.GetKey(KeyCode.S);
        bool handbrake = driving && Input.GetKey(KeyCode.Space);

        float maxForward = maxForwardSpeedKmh / 3.6f;
        float maxReverse = maxReverseSpeedKmh / 3.6f;

        if (!driving)
        {
            currentDriveSpeed =
                Mathf.MoveTowards(
                    currentDriveSpeed,
                    0f,
                    5f * Time.fixedDeltaTime);
        }
        else if (handbrake)
        {
            currentDriveSpeed =
                Mathf.MoveTowards(
                    currentDriveSpeed,
                    0f,
                    handbrakePower * Time.fixedDeltaTime);
        }
        else if (w)
        {
            // Geri giderken W önce fren olur.
            if (currentDriveSpeed < -0.35f)
            {
                currentDriveSpeed =
                    Mathf.MoveTowards(
                        currentDriveSpeed,
                        0f,
                        brakePower * Time.fixedDeltaTime);
            }
            else
            {
                currentDriveSpeed =
                    Mathf.MoveTowards(
                        currentDriveSpeed,
                        maxForward,
                        acceleration * Time.fixedDeltaTime);
            }
        }
        else if (s)
        {
            // İleri giderken S önce fren olur, durduktan sonra geri vitestir.
            if (currentDriveSpeed > 0.35f)
            {
                currentDriveSpeed =
                    Mathf.MoveTowards(
                        currentDriveSpeed,
                        0f,
                        brakePower * Time.fixedDeltaTime);
            }
            else
            {
                currentDriveSpeed =
                    Mathf.MoveTowards(
                        currentDriveSpeed,
                        -maxReverse,
                        reverseAcceleration * Time.fixedDeltaTime);
            }
        }
        else
        {
            currentDriveSpeed =
                Mathf.MoveTowards(
                    currentDriveSpeed,
                    0f,
                    coastSlowdown * Time.fixedDeltaTime);
        }

        float steerInput = 0f;

        if (driving && Input.GetKey(KeyCode.A))
            steerInput = -1f;

        if (driving && Input.GetKey(KeyCode.D))
            steerInput = 1f;

        float speedKmh = Mathf.Abs(currentDriveSpeed) * 3.6f;
        float speed01 =
            Mathf.InverseLerp(0f, maxForwardSpeedKmh, speedKmh);

        // Düşük hızda yaklaşık +/-35 derece. Hız yükseldikçe açı azalır.
        float allowedSteer =
            Mathf.Lerp(lowSpeedSteer, highSpeedSteer, speed01);

        float targetCenterSteer =
            steerInput * allowedSteer;

        currentSteerAngle =
            Mathf.MoveTowards(
                currentSteerAngle,
                targetCenterSteer,
                steeringVisualSpeed * Time.fixedDeltaTime);

        // Ackermann geometrisi:
        // İç ön teker dış ön tekere göre biraz daha fazla döner.
        CalculateAckermannAngles(
            currentSteerAngle,
            out float targetLeftAngle,
            out float targetRightAngle);

        frontLeftSteerAngle =
            Mathf.MoveTowards(
                frontLeftSteerAngle,
                targetLeftAngle,
                steeringVisualSpeed * Time.fixedDeltaTime);

        frontRightSteerAngle =
            Mathf.MoveTowards(
                frontRightSteerAngle,
                targetRightAngle,
                steeringVisualSpeed * Time.fixedDeltaTime);

        // Aracın dönüşü ön teker açılarına göre gerçek "bicycle model" hesabıyla yapılır.
        // Araç dururken gövde kendi etrafında dönmez.
        float steerRadians =
            currentSteerAngle * Mathf.Deg2Rad;

        float yawRadians = 0f;

        if (Mathf.Abs(currentDriveSpeed) > 0.03f &&
            Mathf.Abs(steerRadians) > 0.0001f)
        {
            yawRadians =
                (currentDriveSpeed / Mathf.Max(0.1f, wheelBase)) *
                Mathf.Tan(steerRadians) *
                Time.fixedDeltaTime;
        }

        float yawDegrees = yawRadians * Mathf.Rad2Deg;

        carBody.WakeUp();

        if (Mathf.Abs(yawDegrees) > 0.0001f)
        {
            carBody.MoveRotation(
                carBody.rotation *
                Quaternion.AngleAxis(yawDegrees, driveRoot.up));
        }

        Vector3 forward = VehicleForward.normalized;

        Vector3 horizontalStep =
            forward * currentDriveSpeed * Time.fixedDeltaTime;

        Vector3 verticalStep =
            Vector3.Project(carBody.linearVelocity, driveRoot.up) *
            Time.fixedDeltaTime;

        carBody.MovePosition(
            carBody.position + horizontalStep + verticalStep);

        // Yatay eski fizik hızını temizle, yalnızca düşey/gravity bileşeni kalsın.
        carBody.linearVelocity =
            Vector3.Project(carBody.linearVelocity, driveRoot.up);

        float radius = Mathf.Max(0.1f, averageWheelRadius);

        wheelSpinAngle +=
            (currentDriveSpeed / radius) *
            Mathf.Rad2Deg *
            Time.fixedDeltaTime;
    }

    private void CalculateAckermannAngles(
        float centerSteerAngle,
        out float leftAngle,
        out float rightAngle)
    {
        leftAngle = 0f;
        rightAngle = 0f;

        float absCenter = Mathf.Abs(centerSteerAngle);

        if (absCenter < 0.01f)
            return;

        float centerRad = absCenter * Mathf.Deg2Rad;
        float turnRadius =
            wheelBase / Mathf.Max(0.001f, Mathf.Tan(centerRad));

        float halfTrack = frontTrackWidth * 0.5f;

        float innerAngle =
            Mathf.Atan(
                wheelBase /
                Mathf.Max(0.05f, turnRadius - halfTrack)) *
            Mathf.Rad2Deg;

        float outerAngle =
            Mathf.Atan(
                wheelBase /
                (turnRadius + halfTrack)) *
            Mathf.Rad2Deg;

        // Güvenlik: hiçbir ön teker +/- lowSpeedSteer sınırını geçmesin.
        innerAngle = Mathf.Min(innerAngle, 45f);
        outerAngle = Mathf.Min(outerAngle, 45f);

        if (centerSteerAngle < 0f)
        {
            // A / sola dönüş: sol teker içte.
            leftAngle = -innerAngle;
            rightAngle = -outerAngle;
        }
        else
        {
            // D / sağa dönüş: sağ teker içte.
            leftAngle = outerAngle;
            rightAngle = innerAngle;
        }
    }

    private void UpdateWheelVisuals()
    {
        // A / D SADECE bu iki ön pivotu sağa-sola çevirir.
        ApplyWheelVisual(wheelVisualFL, frontLeftSteerAngle);
        ApplyWheelVisual(wheelVisualFR, frontRightSteerAngle);

        // Arka tekerlerin sağ-sol açısı DAİMA 0.
        ApplyWheelVisual(wheelVisualRL, 0f);
        ApplyWheelVisual(wheelVisualRR, 0f);
    }

    private void ApplyWheelVisual(
        WheelVisual visual,
        float steerAngle)
    {
        if (visual == null || visual.spinPivot == null)
            return;

        // Ön tekerse en fazla -45 / +45.
        if (visual.steerPivot != null)
        {
            float clampedSteer =
                Mathf.Clamp(steerAngle, -45f, 45f);

            visual.steerPivot.localRotation =
                Quaternion.Euler(0f, clampedSteer, 0f);
        }

        // Yuvarlanma ayrı eksende. A/D bununla oynamaz.
        visual.spinPivot.localRotation =
            Quaternion.AngleAxis(
                wheelSpinAngle,
                Vector3.right);
    }

    private void UpdateReverseSound()
    {
        if (reverseAudio == null)
            return;

        bool active =
            driving &&
            (Input.GetKey(KeyCode.S) || GetSignedSpeedKmh() < -0.8f) &&
            GetSignedSpeedKmh() <= 1f;

        if (active)
        {
            if (!reverseAudio.isPlaying)
                reverseAudio.Play();
        }
        else if (reverseAudio.isPlaying)
        {
            reverseAudio.Stop();
        }
    }

    private float GetSignedSpeedKmh()
    {
        return currentDriveSpeed * 3.6f;
    }

    private float GetSpeedKmh()
    {
        return Mathf.Abs(GetSignedSpeedKmh());
    }

    private Transform FindPlayerHips()
    {
        if (playerAnimator != null &&
            playerAnimator.isHuman &&
            playerAnimator.avatar != null &&
            playerAnimator.avatar.isValid)
        {
            Transform humanoidHips =
                playerAnimator.GetBoneTransform(HumanBodyBones.Hips);

            if (humanoidHips != null)
                return humanoidHips;
        }

        if (playerRoot == null)
            return null;

        Transform[] all =
            playerRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform candidate in all)
        {
            if (candidate == null)
                continue;

            string lower =
                candidate.name.ToLowerInvariant();

            if (lower == "hips" ||
                lower.EndsWith(":hips") ||
                lower.Contains("mixamorig") && lower.EndsWith("hips"))
            {
                return candidate;
            }
        }

        Debug.LogWarning(
            "Karakter Hips kemiği otomatik bulunamadı. SeatPoint fallback kullanılacak.",
            this);

        return null;
    }

    private static Vector3 GetRendererCenter(Transform value)
    {
        Renderer renderer = value.GetComponentInChildren<Renderer>(true);
        return renderer != null ? renderer.bounds.center : value.position;
    }

    private static Bounds CalculateLocalBounds(
        Transform localRoot,
        Renderer[] renderers)
    {
        bool initialized = false;
        Bounds result = new Bounds();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null ||
                !renderer.enabled ||
                renderer is LineRenderer)
            {
                continue;
            }

            Bounds worldBounds = renderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 local = localRoot.InverseTransformPoint(corner);

                if (!initialized)
                {
                    result = new Bounds(local, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(local);
                }
            }
        }

        if (!initialized)
            result = new Bounds(Vector3.zero, Vector3.one);

        return result;
    }

    private static Transform FindDeepChild(
        Transform parent,
        string exactName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == exactName)
                return child;

            Transform found = FindDeepChild(child, exactName);

            if (found != null)
                return found;
        }

        return null;
    }

    private void DestroyAnimationGraph()
    {
        if (animationGraph.IsValid())
            animationGraph.Destroy();
    }

    private void OnDestroy()
    {
        DestroyAnimationGraph();

        if (reverseClip != null)
            Destroy(reverseClip);
    }
}