using UnityEngine;

public class GLS580DigitalClusterV5 : MonoBehaviour
{
    [Header("SADECE ARAC KOKU")]
    public Transform vehicleRoot;

    [Header("ISTEGE BAGLI ON FARLAR")]
    public Light frontHeadlightLeft;
    public Light frontHeadlightRight;

    [Header("YON")]
    [Tooltip("D/R ters algilaniyorsa ac.")]
    public bool invertForward = false;

    public float speedSmoothing = 8f;

    private Renderer[][] digits;
    private Renderer gearD;
    private Renderer gearN;
    private Renderer gearR;
    private Renderer lowBeamIndicator;

    private Vector3 lastPosition;
    private bool initialized;
    private float speedKmh;
    private float signedSpeedKmh;
    private bool reverseLatched;

    private static readonly string[] Segments =
    {
        "A", "B", "C", "D", "E", "F", "G"
    };

    private static readonly bool[][] Numbers =
    {
        new[]{true, true, true, true, true, true, false},     // 0
        new[]{false, true, true, false, false, false, false}, // 1
        new[]{true, true, false, true, true, false, true},    // 2
        new[]{true, true, true, true, false, false, true},    // 3
        new[]{false, true, true, false, false, true, true},   // 4
        new[]{true, false, true, true, false, true, true},    // 5
        new[]{true, false, true, true, true, true, true},     // 6
        new[]{true, true, true, false, false, false, false},  // 7
        new[]{true, true, true, true, true, true, true},      // 8
        new[]{true, true, true, true, false, true, true}      // 9
    };

    private void Awake()
    {
        BindAll();
        ResetPosition();
    }

    private void OnEnable()
    {
        // Play Mode'da script yeniden derlenirse / obje tekrar acilirsa
        // private diziler bos kalmasin.
        BindAll();
        ResetPosition();
    }

    private void Start()
    {
        BindAll();
        ResetPosition();
    }

    private void Update()
    {
        if (vehicleRoot == null)
            return;

        // Unity hot-reload veya prefab degisikliginde referanslar kaybolursa
        // kendini otomatik yeniden baglar.
        if (!BindingsReady())
            BindAll();

        if (!initialized)
        {
            ResetPosition();
            return;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        Vector3 delta =
            vehicleRoot.position - lastPosition;

        lastPosition =
            vehicleRoot.position;

        float rawSpeed =
            delta.magnitude / dt * 3.6f;

        float blend =
            speedSmoothing <= 0f
                ? 1f
                : 1f - Mathf.Exp(-speedSmoothing * dt);

        speedKmh =
            Mathf.Lerp(
                speedKmh,
                rawSpeed,
                blend);

        Vector3 forward =
            invertForward
                ? -vehicleRoot.forward
                : vehicleRoot.forward;

        if (forward.sqrMagnitude > 0.0001f)
        {
            signedSpeedKmh =
                Vector3.Dot(
                    delta / dt,
                    forward.normalized) * 3.6f;
        }
        else
        {
            signedSpeedKmh = 0f;
        }

        int speedNumber =
            Mathf.Clamp(
                Mathf.RoundToInt(speedKmh),
                0,
                999);

        SetDigit(0, speedNumber / 100);
        SetDigit(1, (speedNumber / 10) % 10);
        SetDigit(2, speedNumber % 10);

        // R kilidi:
        // geri giderken bir kare R olup hemen D'ye donmesin.
        if (Input.GetKey(KeyCode.W))
        {
            reverseLatched = false;
        }
        else if (
            Input.GetKey(KeyCode.S) &&
            (
                reverseLatched ||
                speedKmh < 3f ||
                signedSpeedKmh < -0.2f
            ))
        {
            reverseLatched = true;
        }

        if (signedSpeedKmh < -0.7f)
            reverseLatched = true;

        bool reverse = reverseLatched;

        bool neutral =
            !reverse &&
            speedKmh < 0.7f &&
            !Input.GetKey(KeyCode.W);

        Show(gearR, reverse);
        Show(gearN, neutral);
        Show(gearD, !reverse && !neutral);

        bool headlightsOn =
            IsOn(frontHeadlightLeft) ||
            IsOn(frontHeadlightRight);

        Show(
            lowBeamIndicator,
            headlightsOn);
    }

    private void BindAll()
    {
        digits = new Renderer[3][];

        for (int digit = 0; digit < 3; digit++)
        {
            digits[digit] =
                new Renderer[7];

            for (int segment = 0; segment < 7; segment++)
            {
                Transform found =
                    FindDeep(
                        transform,
                        "Speed_D" +
                        digit +
                        "_" +
                        Segments[segment]);

                if (found != null)
                {
                    digits[digit][segment] =
                        found.GetComponentInChildren<Renderer>(true);
                }
            }
        }

        gearD =
            FindRenderer("Gear_D");

        gearN =
            FindRenderer("Gear_N");

        gearR =
            FindRenderer("Gear_R");

        lowBeamIndicator =
            FindRenderer("LowBeamIndicator");
    }

    private bool BindingsReady()
    {
        if (digits == null ||
            digits.Length != 3)
        {
            return false;
        }

        for (int d = 0; d < 3; d++)
        {
            if (digits[d] == null ||
                digits[d].Length != 7)
            {
                return false;
            }
        }

        return true;
    }

    private void ResetPosition()
    {
        if (vehicleRoot == null)
        {
            initialized = false;
            return;
        }

        lastPosition =
            vehicleRoot.position;

        initialized = true;
    }

    private void SetDigit(
        int digitIndex,
        int number)
    {
        // NullReferenceException'i tamamen engelle.
        if (digits == null)
            return;

        if (digitIndex < 0 ||
            digitIndex >= digits.Length)
        {
            return;
        }

        Renderer[] digit =
            digits[digitIndex];

        if (digit == null)
            return;

        number =
            Mathf.Clamp(
                number,
                0,
                9);

        for (int segment = 0;
             segment < 7;
             segment++)
        {
            if (segment >= digit.Length)
                break;

            Renderer renderer =
                digit[segment];

            if (renderer != null)
            {
                renderer.enabled =
                    Numbers[number][segment];
            }
        }
    }

    private Renderer FindRenderer(
        string objectName)
    {
        Transform found =
            FindDeep(
                transform,
                objectName);

        if (found == null)
            return null;

        return found.GetComponentInChildren<Renderer>(true);
    }

    private static bool IsOn(
        Light lightObject)
    {
        return
            lightObject != null &&
            lightObject.enabled &&
            lightObject.gameObject.activeInHierarchy;
    }

    private static void Show(
        Renderer renderer,
        bool show)
    {
        if (renderer != null)
            renderer.enabled = show;
    }

    private static Transform FindDeep(
        Transform root,
        string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0;
             i < root.childCount;
             i++)
        {
            Transform result =
                FindDeep(
                    root.GetChild(i),
                    objectName);

            if (result != null)
                return result;
        }

        return null;
    }
}