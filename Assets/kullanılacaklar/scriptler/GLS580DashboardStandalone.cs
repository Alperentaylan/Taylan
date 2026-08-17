using UnityEngine;

public class GLS580DashboardStandalone : MonoBehaviour
{
    [Header("SADECE BUNLARI BAĞLA")]
    [Tooltip("_GLS580_DriveRoot_V2 objesini buraya sürükle.")]
    public Transform vehicleRoot;

    [Tooltip("Sol ön Spot Light. İstersen boş bırak.")]
    public Light frontHeadlightLeft;

    [Tooltip("Sağ ön Spot Light. İstersen boş bırak.")]
    public Light frontHeadlightRight;

    [Tooltip("Sol arka Point Light. İstersen boş bırak.")]
    public Light rearLightLeft;

    [Tooltip("Sağ arka Point Light. İstersen boş bırak.")]
    public Light rearLightRight;

    [Header("Hız Okuma")]
    public bool invertVehicleForward = false;
    [Range(0f, 20f)]
    public float speedSmoothing = 8f;

    private Renderer[][] digits = new Renderer[3][];
    private Renderer gearD;
    private Renderer gearN;
    private Renderer gearR;
    private Renderer headlightIcon;
    private Renderer brakeIcon;

    private Vector3 lastVehiclePosition;
    private bool positionInitialized;
    private float displayedSpeedKmh;
    private float signedSpeedKmh;

    private static readonly string[] SegmentNames =
        { "A", "B", "C", "D", "E", "F", "G" };

    // A B C D E F G
    private static readonly bool[][] NumberSegments =
    {
        new[]{true, true, true, true, true, true, false},   // 0
        new[]{false,true, true, false,false,false,false},    // 1
        new[]{true, true, false,true, true, false,true},     // 2
        new[]{true, true, true, true, false,false,true},     // 3
        new[]{false,true,true,false,false,true,true},        // 4
        new[]{true,false,true,true,false,true,true},         // 5
        new[]{true,false,true,true,true,true,true},          // 6
        new[]{true,true,true,false,false,false,false},       // 7
        new[]{true,true,true,true,true,true,true},           // 8
        new[]{true,true,true,true,false,true,true},          // 9
    };

    private void Awake()
    {
        FindModelParts();

        if (vehicleRoot == null)
            AutoFindVehicleRoot();

        if (vehicleRoot != null)
        {
            lastVehiclePosition = vehicleRoot.position;
            positionInitialized = true;
        }
    }

    private void Update()
    {
        if (vehicleRoot == null)
        {
            AutoFindVehicleRoot();
            if (vehicleRoot == null)
                return;
        }

        ReadVehicleSpeed();
        UpdateSpeedDigits();
        UpdateGear();
        UpdateStatusIcons();
    }

    private void ReadVehicleSpeed()
    {
        if (!positionInitialized)
        {
            lastVehiclePosition = vehicleRoot.position;
            positionInitialized = true;
            return;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = vehicleRoot.position - lastVehiclePosition;
        lastVehiclePosition = vehicleRoot.position;

        float rawSpeed = delta.magnitude / dt * 3.6f;

        Vector3 forward = vehicleRoot.forward;
        if (invertVehicleForward)
            forward = -forward;

        signedSpeedKmh =
            Vector3.Dot(delta / dt, forward.normalized) * 3.6f;

        float lerp =
            speedSmoothing <= 0f
                ? 1f
                : 1f - Mathf.Exp(-speedSmoothing * dt);

        displayedSpeedKmh =
            Mathf.Lerp(displayedSpeedKmh, rawSpeed, lerp);
    }

    private void UpdateSpeedDigits()
    {
        int speed =
            Mathf.Clamp(
                Mathf.RoundToInt(displayedSpeedKmh),
                0,
                999);

        SetDigit(0, speed / 100);
        SetDigit(1, (speed / 10) % 10);
        SetDigit(2, speed % 10);
    }

    private void UpdateGear()
    {
        bool reverse =
            signedSpeedKmh < -0.7f ||
            (Input.GetKey(KeyCode.S) && displayedSpeedKmh < 1.5f);

        bool neutral =
            !reverse && displayedSpeedKmh < 0.7f;

        SetVisible(gearR, reverse);
        SetVisible(gearN, neutral);
        SetVisible(gearD, !reverse && !neutral);
    }

    private void UpdateStatusIcons()
    {
        bool headlights =
            IsLightOn(frontHeadlightLeft) ||
            IsLightOn(frontHeadlightRight);

        bool rearLights =
            IsLightOn(rearLightLeft) ||
            IsLightOn(rearLightRight);

        bool braking =
            Input.GetKey(KeyCode.Space) ||
            (Input.GetKey(KeyCode.S) && signedSpeedKmh > 0.7f);

        SetVisible(headlightIcon, headlights);
        SetVisible(brakeIcon, braking || rearLights);
    }

    private static bool IsLightOn(Light l)
    {
        return l != null &&
               l.enabled &&
               l.gameObject.activeInHierarchy;
    }

    private void FindModelParts()
    {
        for (int d = 0; d < 3; d++)
        {
            digits[d] = new Renderer[7];

            for (int s = 0; s < 7; s++)
            {
                Transform t =
                    FindDeepChild(
                        transform,
                        "D" + d + "_" + SegmentNames[s]);

                if (t != null)
                    digits[d][s] =
                        t.GetComponentInChildren<Renderer>(true);
            }
        }

        gearD = FindRenderer("Gear_D");
        gearN = FindRenderer("Gear_N");
        gearR = FindRenderer("Gear_R");
        headlightIcon = FindRenderer("Icon_Headlight");
        brakeIcon = FindRenderer("Icon_Brake");
    }

    private void AutoFindVehicleRoot()
    {
        Transform current = transform.parent;

        while (current != null)
        {
            if (current.name.Contains("DriveRoot"))
            {
                vehicleRoot = current;
                return;
            }

            current = current.parent;
        }
    }

    private void SetDigit(int digitIndex, int number)
    {
        if (digitIndex < 0 ||
            digitIndex >= digits.Length ||
            digits[digitIndex] == null)
            return;

        for (int s = 0; s < 7; s++)
        {
            Renderer renderer = digits[digitIndex][s];

            if (renderer != null)
                renderer.enabled = NumberSegments[number][s];
        }
    }

    private Renderer FindRenderer(string objectName)
    {
        Transform t = FindDeepChild(transform, objectName);

        return t != null
            ? t.GetComponentInChildren<Renderer>(true)
            : null;
    }

    private static void SetVisible(Renderer renderer, bool visible)
    {
        if (renderer != null)
            renderer.enabled = visible;
    }

    private static Transform FindDeepChild(
        Transform parent,
        string exactName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == exactName)
                return child;

            Transform result =
                FindDeepChild(child, exactName);

            if (result != null)
                return result;
        }

        return null;
    }
}
