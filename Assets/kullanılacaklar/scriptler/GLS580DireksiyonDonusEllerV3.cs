using System;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(32650)]
public class GLS580DireksiyonDonusEllerV3 : MonoBehaviour
{
    [Header("DIREKSIYON")]
    [Tooltip("Hierarchy: Static_Body > SteeringWheel")]
    public Transform steeringWheel;

    [Tooltip("SteeringWheel altina koyacagin SteeringAxis Empty. MAVI Z oku direksiyon miline paralel olsun.")]
    public Transform steeringAxis;

    [Tooltip("Sag/sol maksimum direksiyon acisi.")]
    [Range(1f, 180f)]
    public float maksimumAci = 90f;

    [Tooltip("A/D basiliyken donus hizi.")]
    public float donusHizi = 240f;

    [Tooltip("Tus birakilinca merkeze donus hizi.")]
    public float merkezeDonusHizi = 320f;

    [Tooltip("A/D ters calisiyorsa bunu isaretle.")]
    public bool yonuTersCevir = false;

    [Header("KONTROL")]
    public KeyCode solTus = KeyCode.A;
    public KeyCode sagTus = KeyCode.D;

    [Header("ELLER - ISTEGE BAGLI")]
    [Tooltip("Karakterdeki Humanoid Animator.")]
    public Animator animator;

    [Tooltip("SteeringWheel > LeftHandGrip")]
    public Transform leftHandGrip;

    [Tooltip("SteeringWheel > RightHandGrip")]
    public Transform rightHandGrip;

    [Range(0f, 1f)]
    public float elIKAgirligi = 1f;

    [Tooltip("Aciksa arac icindeyken eller gripleri takip eder.")]
    public bool ellerDireksiyonda = true;

    [Header("GLS580 BASIT SISTEM")]
    [Tooltip("Bos birakabilirsin. GLS580BasitSistem otomatik bulunur.")]
    public MonoBehaviour basitSistem;

    [Header("DEBUG")]
    [SerializeField] private bool playerInside;
    [SerializeField] private bool busy;
    [SerializeField] private float currentAngle;
    [SerializeField] private Vector3 kullanilanLocalAxis;

    private Quaternion baslangicLocalRotation;

    private Type basitType;
    private FieldInfo fPlayerInside;
    private FieldInfo fBusy;

    private void Awake()
    {
        OtomatikBul();

        if (steeringWheel != null)
            baslangicLocalRotation = steeringWheel.localRotation;
    }

    private void Start()
    {
        OtomatikBul();

        if (steeringWheel != null)
            baslangicLocalRotation = steeringWheel.localRotation;
    }

    private void Update()
    {
        OtomatikBul();
        BasitDurumuOku();

        if (steeringWheel == null)
            return;

        float input = 0f;

        // Binis / inis animasyonunda direksiyon kontrolu devreye girmez.
        // Sadece arac tamamen icindeyken A/D ile doner.
        if (playerInside && !busy)
        {
            if (Input.GetKey(solTus))
                input -= 1f;

            if (Input.GetKey(sagTus))
                input += 1f;
        }

        if (yonuTersCevir)
            input *= -1f;

        float hedefAci = input * maksimumAci;

        float hiz =
            Mathf.Abs(input) > 0.001f
                ? donusHizi
                : merkezeDonusHizi;

        currentAngle =
            Mathf.MoveTowards(
                currentAngle,
                hedefAci,
                hiz * Time.deltaTime);

        Vector3 axis = DireksiyonLocalEkseni();
        kullanilanLocalAxis = axis;

        steeringWheel.localRotation =
            baslangicLocalRotation *
            Quaternion.AngleAxis(currentAngle, axis);
    }

    private Vector3 DireksiyonLocalEkseni()
    {
        if (steeringWheel == null)
            return Vector3.forward;

        if (steeringAxis != null)
        {
            // Axis marker'in MAVI Z(forward) yonunu SteeringWheel local uzayina cevir.
            Vector3 local =
                steeringWheel.InverseTransformDirection(
                    steeringAxis.forward);

            if (local.sqrMagnitude > 0.0001f)
                return local.normalized;
        }

        return Vector3.forward;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        OtomatikBul();
        BasitDurumuOku();

        if (animator == null)
            return;

        bool aktif =
            ellerDireksiyonda &&
            playerInside &&
            !busy;

        float w = aktif ? elIKAgirligi : 0f;

        ElUygula(
            AvatarIKGoal.LeftHand,
            leftHandGrip,
            w);

        ElUygula(
            AvatarIKGoal.RightHand,
            rightHandGrip,
            w);
    }

    private void ElUygula(
        AvatarIKGoal goal,
        Transform grip,
        float weight)
    {
        float w =
            grip != null
                ? weight
                : 0f;

        animator.SetIKPositionWeight(goal, w);
        animator.SetIKRotationWeight(goal, w);

        if (w > 0f && grip != null)
        {
            animator.SetIKPosition(
                goal,
                grip.position);

            animator.SetIKRotation(
                goal,
                grip.rotation);
        }
    }

    private void OtomatikBul()
    {
        if (steeringWheel == null)
        {
            GameObject go =
                GameObject.Find("SteeringWheel");

            if (go != null)
                steeringWheel = go.transform;
        }

        if (steeringWheel != null)
        {
            if (steeringAxis == null)
            {
                Transform t =
                    steeringWheel.Find("SteeringAxis");

                if (t != null)
                    steeringAxis = t;
            }

            if (leftHandGrip == null)
            {
                Transform t =
                    steeringWheel.Find("LeftHandGrip");

                if (t != null)
                    leftHandGrip = t;
            }

            if (rightHandGrip == null)
            {
                Transform t =
                    steeringWheel.Find("RightHandGrip");

                if (t != null)
                    rightHandGrip = t;
            }
        }

        if (animator == null)
        {
            Animator[] all =
                FindObjectsOfType<Animator>(true);

            foreach (Animator a in all)
            {
                if (a != null && a.isHuman)
                {
                    animator = a;
                    break;
                }
            }
        }

        if (basitSistem == null)
        {
            MonoBehaviour[] all =
                FindObjectsOfType<MonoBehaviour>(true);

            foreach (MonoBehaviour mb in all)
            {
                if (mb != null &&
                    mb.GetType().Name == "GLS580BasitSistem")
                {
                    basitSistem = mb;
                    break;
                }
            }
        }

        if (basitSistem != null)
        {
            Type t = basitSistem.GetType();

            if (basitType != t)
            {
                basitType = t;

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
            }
        }
    }

    private void BasitDurumuOku()
    {
        if (basitSistem == null)
        {
            playerInside = false;
            busy = false;
            return;
        }

        try
        {
            if (fPlayerInside != null)
                playerInside =
                    (bool)fPlayerInside.GetValue(
                        basitSistem);

            if (fBusy != null)
                busy =
                    (bool)fBusy.GetValue(
                        basitSistem);
        }
        catch
        {
            // Son degeri koru.
        }
    }

    private void OnDisable()
    {
        if (steeringWheel != null)
            steeringWheel.localRotation =
                baslangicLocalRotation;

        currentAngle = 0f;
    }
}
