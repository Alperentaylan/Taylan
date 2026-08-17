
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RealisticCarController : MonoBehaviour
{
    [Header("Teker Meshleri")]
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;

    [Header("Wheel Collider'lar")]
    public WheelCollider colliderFL;
    public WheelCollider colliderFR;
    public WheelCollider colliderRL;
    public WheelCollider colliderRR;

    [Header("Araç Ayarları")]
    public bool autoSetupOnStart = true;
    public bool applyRecommendedMass = true;
    public float recommendedMass = 2300f;
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.55f, 0.1f);

    [Header("Motor ve Hız")]
    public float forwardMotorTorque = 2400f;
    public float reverseMotorTorque = 1300f;
    public float maxForwardSpeedKmh = 150f;
    public float maxReverseSpeedKmh = 28f;
    public float throttleRiseSpeed = 1.25f;
    public float throttleReleaseSpeed = 1.8f;

    [Header("Fren")]
    public float serviceBrakeTorque = 5200f;
    public float handbrakeTorque = 9000f;
    public float coastBrakeMin = 35f;
    public float coastBrakeMax = 260f;
    public float parkingBrakeTorque = 2500f;

    [Header("Direksiyon")]
    public float lowSpeedSteerAngle = 33f;
    public float highSpeedSteerAngle = 8f;
    public float steerResponse = 4.5f;

    [Header("Teker Fizik")]
    public float wheelMass = 35f;
    public float suspensionDistance = 0.20f;
    public float suspensionSpring = 36000f;
    public float suspensionDamper = 5000f;
    public float sidewaysStiffness = 1.45f;
    public float forwardStiffness = 1.25f;

    [Header("Geri Vites Sesi")]
    public AudioSource reverseBeepSource;

    public bool DriverActive { get; private set; }
    public float SignedSpeedKmh { get; private set; }
    public float AbsoluteSpeedKmh => Mathf.Abs(SignedSpeedKmh);

    private Rigidbody rb;
    private float throttle;
    private float steer;
    private Quaternion offsetFL;
    private Quaternion offsetFR;
    private Quaternion offsetRL;
    private Quaternion offsetRR;
    private bool wheelOffsetsCached;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (applyRecommendedMass)
            rb.mass = recommendedMass;

        rb.centerOfMass += centerOfMassOffset;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (autoSetupOnStart && !AllWheelCollidersAssigned())
            AutoSetupWheelColliders();

        ConfigureAllWheelColliders();
        CacheWheelRotationOffsets();
        SetDriverActive(false);
    }

    private void Update()
    {
        UpdateWheelVisuals();
        UpdateReverseBeep();
    }

    private void FixedUpdate()
    {
        SignedSpeedKmh = Vector3.Dot(rb.linearVelocity, transform.forward) * 3.6f;

        if (!DriverActive)
        {
            ApplyMotorTorque(0f);
            ApplyBrakeTorque(parkingBrakeTorque, parkingBrakeTorque);
            return;
        }

        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);
        bool handbrake = Input.GetKey(KeyCode.Space);

        float targetThrottle = 0f;
        float brakeInput = 0f;

        // W: araç geri gidiyorsa önce fren, durunca ileri.
        if (w)
        {
            if (SignedSpeedKmh < -1.0f)
                brakeInput = 1f;
            else
                targetThrottle = 1f;
        }

        // S: araç ileri gidiyorsa fren, durunca geri vites.
        if (s)
        {
            if (SignedSpeedKmh > 1.0f)
                brakeInput = 1f;
            else
                targetThrottle = -1f;
        }

        float response = Mathf.Abs(targetThrottle) > Mathf.Abs(throttle)
            ? throttleRiseSpeed
            : throttleReleaseSpeed;

        throttle = Mathf.MoveTowards(throttle, targetThrottle, response * Time.fixedDeltaTime);

        float steerTarget = 0f;
        if (Input.GetKey(KeyCode.A)) steerTarget -= 1f;
        if (Input.GetKey(KeyCode.D)) steerTarget += 1f;
        steer = Mathf.MoveTowards(steer, steerTarget, steerResponse * Time.fixedDeltaTime);

        float speed01 = Mathf.InverseLerp(0f, maxForwardSpeedKmh, AbsoluteSpeedKmh);
        float maxSteerNow = Mathf.Lerp(lowSpeedSteerAngle, highSpeedSteerAngle, speed01);
        float steerAngle = steer * maxSteerNow;

        if (colliderFL != null) colliderFL.steerAngle = steerAngle;
        if (colliderFR != null) colliderFR.steerAngle = steerAngle;

        float motorTorque = 0f;

        if (brakeInput <= 0.01f && !handbrake)
        {
            if (throttle > 0f && SignedSpeedKmh < maxForwardSpeedKmh)
            {
                float torqueFactor = Mathf.Clamp01(1f - (SignedSpeedKmh / maxForwardSpeedKmh));
                motorTorque = throttle * forwardMotorTorque * Mathf.Lerp(0.35f, 1f, torqueFactor);
            }
            else if (throttle < 0f && SignedSpeedKmh > -maxReverseSpeedKmh)
            {
                float torqueFactor = Mathf.Clamp01(1f - (Mathf.Abs(SignedSpeedKmh) / maxReverseSpeedKmh));
                motorTorque = throttle * reverseMotorTorque * Mathf.Lerp(0.45f, 1f, torqueFactor);
            }
        }

        ApplyMotorTorque(motorTorque);

        float coastBrake = 0f;
        if (!w && !s && !handbrake)
        {
            float coast01 = Mathf.InverseLerp(0f, maxForwardSpeedKmh, AbsoluteSpeedKmh);
            coastBrake = Mathf.Lerp(coastBrakeMin, coastBrakeMax, coast01);
        }

        float normalBrake = brakeInput * serviceBrakeTorque + coastBrake;
        float rearBrake = normalBrake;

        if (handbrake)
        {
            ApplyMotorTorque(0f);
            rearBrake = Mathf.Max(rearBrake, handbrakeTorque);
        }

        ApplyBrakeTorque(normalBrake, rearBrake);
    }

    public void SetDriverActive(bool active)
    {
        DriverActive = active;

        if (!active)
        {
            throttle = 0f;
            steer = 0f;
            ApplyMotorTorque(0f);
            ApplyBrakeTorque(parkingBrakeTorque, parkingBrakeTorque);

            if (reverseBeepSource != null)
                reverseBeepSource.Stop();
        }
    }

    private void ApplyMotorTorque(float torque)
    {
        // GLS için dört çeker davranışı.
        if (colliderFL != null) colliderFL.motorTorque = torque * 0.25f;
        if (colliderFR != null) colliderFR.motorTorque = torque * 0.25f;
        if (colliderRL != null) colliderRL.motorTorque = torque * 0.25f;
        if (colliderRR != null) colliderRR.motorTorque = torque * 0.25f;
    }

    private void ApplyBrakeTorque(float frontTorque, float rearTorque)
    {
        if (colliderFL != null) colliderFL.brakeTorque = frontTorque;
        if (colliderFR != null) colliderFR.brakeTorque = frontTorque;
        if (colliderRL != null) colliderRL.brakeTorque = rearTorque;
        if (colliderRR != null) colliderRR.brakeTorque = rearTorque;
    }

    private void UpdateReverseBeep()
    {
        if (reverseBeepSource == null)
            return;

        bool reverseGearActive =
            DriverActive &&
            (throttle < -0.05f || SignedSpeedKmh < -0.8f);

        if (reverseGearActive)
        {
            if (!reverseBeepSource.isPlaying)
                reverseBeepSource.Play();
        }
        else if (reverseBeepSource.isPlaying)
        {
            reverseBeepSource.Stop();
        }
    }

    private void CacheWheelRotationOffsets()
    {
        if (!AllWheelCollidersAssigned())
            return;

        if (wheelFL != null) offsetFL = Quaternion.Inverse(colliderFL.transform.rotation) * wheelFL.rotation;
        if (wheelFR != null) offsetFR = Quaternion.Inverse(colliderFR.transform.rotation) * wheelFR.rotation;
        if (wheelRL != null) offsetRL = Quaternion.Inverse(colliderRL.transform.rotation) * wheelRL.rotation;
        if (wheelRR != null) offsetRR = Quaternion.Inverse(colliderRR.transform.rotation) * wheelRR.rotation;

        wheelOffsetsCached = true;
    }

    private void UpdateWheelVisuals()
    {
        if (!wheelOffsetsCached)
            CacheWheelRotationOffsets();

        UpdateOneWheel(colliderFL, wheelFL, offsetFL);
        UpdateOneWheel(colliderFR, wheelFR, offsetFR);
        UpdateOneWheel(colliderRL, wheelRL, offsetRL);
        UpdateOneWheel(colliderRR, wheelRR, offsetRR);
    }

    private static void UpdateOneWheel(WheelCollider wc, Transform mesh, Quaternion rotationOffset)
    {
        if (wc == null || mesh == null)
            return;

        wc.GetWorldPose(out Vector3 position, out Quaternion rotation);
        mesh.position = position;
        mesh.rotation = rotation * rotationOffset;
    }

    private bool AllWheelCollidersAssigned()
    {
        return colliderFL != null && colliderFR != null &&
               colliderRL != null && colliderRR != null;
    }

    private void ConfigureAllWheelColliders()
    {
        ConfigureWheel(colliderFL);
        ConfigureWheel(colliderFR);
        ConfigureWheel(colliderRL);
        ConfigureWheel(colliderRR);
    }

    private void ConfigureWheel(WheelCollider wc)
    {
        if (wc == null)
            return;

        wc.mass = wheelMass;
        wc.suspensionDistance = suspensionDistance;

        JointSpring spring = wc.suspensionSpring;
        spring.spring = suspensionSpring;
        spring.damper = suspensionDamper;
        spring.targetPosition = 0.5f;
        wc.suspensionSpring = spring;

        WheelFrictionCurve forward = wc.forwardFriction;
        forward.stiffness = forwardStiffness;
        wc.forwardFriction = forward;

        WheelFrictionCurve sideways = wc.sidewaysFriction;
        sideways.stiffness = sidewaysStiffness;
        wc.sidewaysFriction = sideways;

        wc.ConfigureVehicleSubsteps(5f, 12, 15);
    }

    [ContextMenu("AUTO SETUP WHEEL COLLIDERS")]
    public void AutoSetupWheelColliders()
    {
        wheelFL ??= FindDeepChild(transform, "Wheel_FL");
        wheelFR ??= FindDeepChild(transform, "Wheel_FR");
        wheelRL ??= FindDeepChild(transform, "Wheel_RL");
        wheelRR ??= FindDeepChild(transform, "Wheel_RR");

        colliderFL = CreateOrUpdateWheelCollider("WC_FL", wheelFL, colliderFL);
        colliderFR = CreateOrUpdateWheelCollider("WC_FR", wheelFR, colliderFR);
        colliderRL = CreateOrUpdateWheelCollider("WC_RL", wheelRL, colliderRL);
        colliderRR = CreateOrUpdateWheelCollider("WC_RR", wheelRR, colliderRR);

        ConfigureAllWheelColliders();
        wheelOffsetsCached = false;
        CacheWheelRotationOffsets();
    }

    private WheelCollider CreateOrUpdateWheelCollider(
        string objectName,
        Transform wheelMesh,
        WheelCollider existing)
    {
        if (wheelMesh == null)
        {
            Debug.LogWarning(objectName + " oluşturulamadı: teker mesh bulunamadı.", this);
            return existing;
        }

        WheelCollider wc = existing;

        if (wc == null)
        {
            Transform old = transform.Find(objectName);
            GameObject go;

            if (old != null)
                go = old.gameObject;
            else
            {
                go = new GameObject(objectName);
                go.transform.SetParent(transform, false);
            }

            wc = go.GetComponent<WheelCollider>();
            if (wc == null)
                wc = go.AddComponent<WheelCollider>();
        }

        Renderer renderer = wheelMesh.GetComponentInChildren<Renderer>();
        Vector3 worldCenter = renderer != null ? renderer.bounds.center : wheelMesh.position;
        float worldRadius = renderer != null ? renderer.bounds.extents.y : 0.45f;

        wc.transform.localPosition = transform.InverseTransformPoint(worldCenter);
        wc.transform.localRotation = Quaternion.identity;

        float rootScaleY = Mathf.Max(0.0001f, transform.lossyScale.y);
        wc.radius = Mathf.Max(0.1f, worldRadius / rootScaleY);

        return wc;
    }

    private static Transform FindDeepChild(Transform parent, string exactName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == exactName)
                return child;

            Transform result = FindDeepChild(child, exactName);
            if (result != null)
                return result;
        }

        return null;
    }
}
