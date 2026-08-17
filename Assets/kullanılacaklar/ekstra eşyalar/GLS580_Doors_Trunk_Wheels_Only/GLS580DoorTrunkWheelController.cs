using UnityEngine;

/// <summary>
/// Dört kapı ile bagajı açıp kapatır; dört ayrı tekerleği döndürür.
/// Araç gövdesini hareket ettirmez. Daha sonra sürüş sisteminden
/// SetSteering ve RotateWheelsFromDistance metotları çağrılabilir.
/// </summary>
public sealed class GLS580DoorTrunkWheelController : MonoBehaviour
{
    [Header("Kapılar ve bagaj")]
    [SerializeField] private Transform doorFL;
    [SerializeField] private Transform doorFR;
    [SerializeField] private Transform doorRL;
    [SerializeField] private Transform doorRR;
    [SerializeField] private Transform trunk;

    [Header("Tekerlekler")]
    [SerializeField] private Transform wheelFL;
    [SerializeField] private Transform wheelFR;
    [SerializeField] private Transform wheelRL;
    [SerializeField] private Transform wheelRR;

    [Header("Açılma açıları")]
    [SerializeField] private Vector3 doorFLOpenEuler = new Vector3(0f, -70f, 0f);
    [SerializeField] private Vector3 doorFROpenEuler = new Vector3(0f, 70f, 0f);
    [SerializeField] private Vector3 doorRLOpenEuler = new Vector3(0f, -70f, 0f);
    [SerializeField] private Vector3 doorRROpenEuler = new Vector3(0f, 70f, 0f);
    [SerializeField] private Vector3 trunkOpenEuler = new Vector3(-72f, 0f, 0f);

    [Header("Hareket ayarları")]
    [Min(0.1f)] [SerializeField] private float partMovementSpeed = 5f;
    [Range(0f, 50f)] [SerializeField] private float maximumSteeringAngle = 32f;
    [Min(0.01f)] [SerializeField] private float visualWheelRadius = 0.52f;

    [Header("Klavye testi")]
    [SerializeField] private bool demoKeyboardControls = true;
    [Min(1f)] [SerializeField] private float demoWheelSpinSpeed = 240f;

    private Quaternion doorFLClosed;
    private Quaternion doorFRClosed;
    private Quaternion doorRLClosed;
    private Quaternion doorRRClosed;
    private Quaternion trunkClosed;
    private Quaternion wheelFLBase;
    private Quaternion wheelFRBase;
    private Quaternion wheelRLBase;
    private Quaternion wheelRRBase;

    private bool doorFLOpen;
    private bool doorFROpen;
    private bool doorRLOpen;
    private bool doorRROpen;
    private bool trunkOpen;

    private float wheelSpinAngle;
    private float steeringAngle;
    private float requestedSteeringAngle;

    private void Awake()
    {
        AutoFindParts();

        if (doorFL != null) doorFLClosed = doorFL.localRotation;
        if (doorFR != null) doorFRClosed = doorFR.localRotation;
        if (doorRL != null) doorRLClosed = doorRL.localRotation;
        if (doorRR != null) doorRRClosed = doorRR.localRotation;
        if (trunk != null) trunkClosed = trunk.localRotation;

        if (wheelFL != null) wheelFLBase = wheelFL.localRotation;
        if (wheelFR != null) wheelFRBase = wheelFR.localRotation;
        if (wheelRL != null) wheelRLBase = wheelRL.localRotation;
        if (wheelRR != null) wheelRRBase = wheelRR.localRotation;
    }

    private void Update()
    {
        if (demoKeyboardControls)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleDoorFL();
            if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleDoorFR();
            if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleDoorRL();
            if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleDoorRR();
            if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleTrunk();

            float driveInput = Input.GetAxisRaw("Vertical");
            float steerInput = Input.GetAxisRaw("Horizontal");
            wheelSpinAngle = Mathf.Repeat(
                wheelSpinAngle + driveInput * demoWheelSpinSpeed * Time.deltaTime,
                360f);
            requestedSteeringAngle = steerInput * maximumSteeringAngle;
        }

        steeringAngle = Mathf.Lerp(
            steeringAngle,
            requestedSteeringAngle,
            1f - Mathf.Exp(-8f * Time.deltaTime));

        RotatePart(doorFL, doorFLClosed, doorFLOpenEuler, doorFLOpen);
        RotatePart(doorFR, doorFRClosed, doorFROpenEuler, doorFROpen);
        RotatePart(doorRL, doorRLClosed, doorRLOpenEuler, doorRLOpen);
        RotatePart(doorRR, doorRRClosed, doorRROpenEuler, doorRROpen);
        RotatePart(trunk, trunkClosed, trunkOpenEuler, trunkOpen);
        ApplyWheelRotations();
    }

    private void RotatePart(
        Transform part,
        Quaternion closedRotation,
        Vector3 openEuler,
        bool isOpen)
    {
        if (part == null) return;

        Quaternion target = isOpen
            ? closedRotation * Quaternion.Euler(openEuler)
            : closedRotation;

        part.localRotation = Quaternion.Slerp(
            part.localRotation,
            target,
            1f - Mathf.Exp(-partMovementSpeed * Time.deltaTime));
    }

    private void ApplyWheelRotations()
    {
        Quaternion spin = Quaternion.AngleAxis(wheelSpinAngle, Vector3.right);
        Quaternion steer = Quaternion.AngleAxis(steeringAngle, Vector3.up);

        if (wheelFL != null) wheelFL.localRotation = wheelFLBase * steer * spin;
        if (wheelFR != null) wheelFR.localRotation = wheelFRBase * steer * spin;
        if (wheelRL != null) wheelRL.localRotation = wheelRLBase * spin;
        if (wheelRR != null) wheelRR.localRotation = wheelRRBase * spin;
    }

    private void AutoFindParts()
    {
        if (doorFL == null) doorFL = FindChildRecursive(transform, "Door_FL");
        if (doorFR == null) doorFR = FindChildRecursive(transform, "Door_FR");
        if (doorRL == null) doorRL = FindChildRecursive(transform, "Door_RL");
        if (doorRR == null) doorRR = FindChildRecursive(transform, "Door_RR");
        if (trunk == null) trunk = FindChildRecursive(transform, "Trunk");

        if (wheelFL == null) wheelFL = FindChildRecursive(transform, "Wheel_FL");
        if (wheelFR == null) wheelFR = FindChildRecursive(transform, "Wheel_FR");
        if (wheelRL == null) wheelRL = FindChildRecursive(transform, "Wheel_RL");
        if (wheelRR == null) wheelRR = FindChildRecursive(transform, "Wheel_RR");
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        foreach (Transform child in root)
        {
            if (child.name == childName) return child;

            Transform result = FindChildRecursive(child, childName);
            if (result != null) return result;
        }

        return null;
    }

    public void SetSteering(float angleDegrees)
    {
        requestedSteeringAngle = Mathf.Clamp(
            angleDegrees,
            -maximumSteeringAngle,
            maximumSteeringAngle);
    }

    public void AddWheelSpin(float angleDegrees)
    {
        wheelSpinAngle = Mathf.Repeat(wheelSpinAngle + angleDegrees, 360f);
    }

    public void RotateWheelsFromDistance(float travelledDistanceMetres)
    {
        if (visualWheelRadius <= 0f) return;

        float angle = travelledDistanceMetres / visualWheelRadius * Mathf.Rad2Deg;
        AddWheelSpin(angle);
    }

    public void ToggleDoorFL() => doorFLOpen = !doorFLOpen;
    public void ToggleDoorFR() => doorFROpen = !doorFROpen;
    public void ToggleDoorRL() => doorRLOpen = !doorRLOpen;
    public void ToggleDoorRR() => doorRROpen = !doorRROpen;
    public void ToggleTrunk() => trunkOpen = !trunkOpen;

    public void CloseAll()
    {
        doorFLOpen = false;
        doorFROpen = false;
        doorRLOpen = false;
        doorRROpen = false;
        trunkOpen = false;
    }
}
