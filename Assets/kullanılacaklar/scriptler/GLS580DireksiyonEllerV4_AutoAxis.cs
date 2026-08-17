using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(32720)]
public class GLS580DireksiyonEller : MonoBehaviour
{
    [Header("DIREKSIYON")]
    [Tooltip("Static_Body > SteeringWheel ROOT. Yanlislikla SteeringWheel_Mesh verirsen script parent ROOT'u otomatik kullanir.")]
    public Transform steeringWheel;

    [Range(1f, 180f)]
    public float maksimumAci = 90f;

    public float donusHizi = 240f;
    public float merkezeDonusHizi = 320f;

    [Tooltip("A ve D ters calisiyorsa ac.")]
    public bool yonuTersCevir = false;

    [Header("ELLER")]
    public Animator animator;

    [Tooltip("SteeringWheel > LeftHandGrip. Bos birakabilirsin.")]
    public Transform leftHandGrip;

    [Tooltip("SteeringWheel > RightHandGrip. Bos birakabilirsin.")]
    public Transform rightHandGrip;

    [Tooltip("Ellerin direksiyona ne kadar kilitlenecegi.")]
    [Range(0f, 1f)]
    public float elIKAgirligi = 1f;

    [Tooltip("El bilegini grip rotasyonuna ne kadar dondursun. Ilk testte 0 kullan.")]
    [Range(0f, 1f)]
    public float elRotasyonAgirligi = 0f;

    [Tooltip("Gripleri direksiyon geometrisine gore otomatik 9 ve 3 yonlerine koyar.")]
    public bool gripleriOtomatikYerlestir = true;

    [Range(0.55f, 0.95f)]
    public float gripYariCapCarpani = 0.78f;

    [Tooltip("Iki eli birlikte biraz yukari/asagi kaydirir.")]
    public float gripDikeyOffset = 0.02f;

    [Header("ARAC")]
    [Tooltip("Bos birakabilirsin. GLS580BasitSistem otomatik bulunur.")]
    public MonoBehaviour basitSistem;

    [Tooltip("Bos birakabilirsin. BasitSistem driveRoot'tan okunur.")]
    public Transform vehicleRoot;

    [Header("DEBUG")]
    [SerializeField] private bool playerInside;
    [SerializeField] private bool busy;
    [SerializeField] private float currentAngle;
    [SerializeField] private Vector3 direksiyonEkseniLocal;
    [SerializeField] private Vector3 direksiyonSagLocal;

    private Transform steeringRoot;
    private Vector3 baslangicLocalPosition;
    private Quaternion baslangicLocalRotation;

    private Type basitType;
    private FieldInfo fPlayerInside;
    private FieldInfo fBusy;
    private FieldInfo fDriveRoot;

    private readonly List<Vector3> wheelPoints = new List<Vector3>();

    private Transform leftUpperArm;
    private Transform leftLowerArm;
    private Transform leftHand;

    private Transform rightUpperArm;
    private Transform rightLowerArm;
    private Transform rightHand;

    private float currentIKWeight;

    private void OnValidate()
    {
        // Inspector'da yanlislikla SteeringWheel_Mesh verilirse ROOT'a cevir.
        if (steeringWheel != null &&
            steeringWheel.name.Contains("SteeringWheel_Mesh") &&
            steeringWheel.parent != null)
        {
            steeringWheel = steeringWheel.parent;
        }
    }

    private void Awake()
    {
        OtomatikBul();
        SteeringRootuDuzelt();

        if (steeringRoot != null)
        {
            baslangicLocalPosition = steeringRoot.localPosition;
            baslangicLocalRotation = steeringRoot.localRotation;
        }

        KemikleriBul();
    }

    private void Start()
    {
        OtomatikBul();
        SteeringRootuDuzelt();
        KemikleriBul();

        if (steeringRoot == null)
        {
            Debug.LogError(
                "GLS580DireksiyonEller: SteeringWheel ROOT bulunamadi. " +
                "Hierarchy'deki Static_Body > SteeringWheel objesini Steering Wheel alanina ver.",
                this);
            enabled = false;
            return;
        }

        baslangicLocalPosition = steeringRoot.localPosition;
        baslangicLocalRotation = steeringRoot.localRotation;

        if (!DireksiyonGeometrisiniOku())
        {
            Debug.LogError(
                "GLS580DireksiyonEller: Direksiyon geometrisi okunamadi.",
                steeringRoot);
            enabled = false;
            return;
        }

        direksiyonEkseniLocal = EnInceEkseniBul(wheelPoints);

        if (direksiyonEkseniLocal.sqrMagnitude < 0.5f)
            direksiyonEkseniLocal = Vector3.forward;

        direksiyonEkseniLocal.Normalize();

        SagVeYukariYonunuHesapla();

        if (gripleriOtomatikYerlestir)
            GripleriYerlestir();

        Debug.Log(
            "GLS580DireksiyonEller: Hazir. Steering ROOT = " +
            steeringRoot.name +
            " | Donus ekseni = " +
            direksiyonEkseniLocal,
            steeringRoot);
    }

    private void Update()
    {
        OtomatikBul();
        BasitDurumuOku();

        if (steeringRoot == null)
            return;

        float input = 0f;

        // Sadece tamamen aracin icindeyken.
        if (playerInside && !busy)
        {
            if (Input.GetKey(KeyCode.A))
                input -= 1f;

            if (Input.GetKey(KeyCode.D))
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

        float hedefIK =
            playerInside && !busy
                ? elIKAgirligi
                : 0f;

        currentIKWeight =
            Mathf.MoveTowards(
                currentIKWeight,
                hedefIK,
                6f * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (steeringRoot == null)
            return;

        // KRITIK:
        // Pozisyonu her kare eski pivot pozisyonunda sabitliyoruz.
        // Direksiyon saga/sola KAYAMAZ; sadece ROOT kendi merkezinde DONER.
        steeringRoot.localPosition = baslangicLocalPosition;

        steeringRoot.localRotation =
            baslangicLocalRotation *
            Quaternion.AngleAxis(
                currentAngle,
                direksiyonEkseniLocal);

        // Animator IK Pass'e bagli degiliz.
        // Animasyon bittikten sonra iki kolu matematiksel Two-Bone IK ile direksiyona getir.
        if (currentIKWeight > 0.001f)
        {
            if (leftHandGrip != null)
            {
                KolIKCoz(
                    leftUpperArm,
                    leftLowerArm,
                    leftHand,
                    leftHandGrip,
                    true,
                    currentIKWeight);
            }

            if (rightHandGrip != null)
            {
                KolIKCoz(
                    rightUpperArm,
                    rightLowerArm,
                    rightHand,
                    rightHandGrip,
                    false,
                    currentIKWeight);
            }
        }
    }

    // ============================================================
    // DIREKSIYON ROOT
    // ============================================================

    private void SteeringRootuDuzelt()
    {
        if (steeringWheel == null)
        {
            GameObject go = GameObject.Find("SteeringWheel");
            if (go != null)
                steeringWheel = go.transform;
        }

        if (steeringWheel == null)
            return;

        Transform t = steeringWheel;

        // Screenshot'taki sorun:
        // Steering Wheel alanina SteeringWheel_Mesh verilmis.
        // Onu ASLA dondurmuyoruz; parent SteeringWheel ROOT'u donduruyoruz.
        if (t.name.Contains("SteeringWheel_Mesh") &&
            t.parent != null)
        {
            t = t.parent;
        }

        Transform walker = t;

        while (walker != null)
        {
            if (walker.name == "SteeringWheel")
            {
                t = walker;
                break;
            }

            walker = walker.parent;
        }

        steeringRoot = t;
        steeringWheel = steeringRoot;
    }

    // ============================================================
    // DIREKSIYON GEOMETRISI / GERCEK MIL EKSENI
    // ============================================================

    private bool DireksiyonGeometrisiniOku()
    {
        wheelPoints.Clear();

        if (steeringRoot == null)
            return false;

        MeshFilter[] filters =
            steeringRoot.GetComponentsInChildren<MeshFilter>(true);

        HashSet<string> unique = new HashSet<string>();

        foreach (MeshFilter mf in filters)
        {
            if (mf == null || mf.sharedMesh == null)
                continue;

            Mesh mesh = mf.sharedMesh;

            if (!mesh.isReadable)
                continue;

            Vector3[] verts = mesh.vertices;
            HashSet<int> used = new HashSet<int>();

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                int[] tris = mesh.GetTriangles(s);

                for (int i = 0; i < tris.Length; i++)
                    used.Add(tris[i]);
            }

            foreach (int index in used)
            {
                if (index < 0 || index >= verts.Length)
                    continue;

                Vector3 world =
                    mf.transform.TransformPoint(verts[index]);

                Vector3 local =
                    steeringRoot.InverseTransformPoint(world);

                string key =
                    Mathf.RoundToInt(local.x * 10000f) + "_" +
                    Mathf.RoundToInt(local.y * 10000f) + "_" +
                    Mathf.RoundToInt(local.z * 10000f);

                if (unique.Add(key))
                    wheelPoints.Add(local);
            }
        }

        return wheelPoints.Count >= 30;
    }

    private Vector3 EnInceEkseniBul(List<Vector3> pts)
    {
        Vector3 mean = Vector3.zero;

        for (int i = 0; i < pts.Count; i++)
            mean += pts[i];

        mean /= Mathf.Max(1, pts.Count);

        double[,] a = new double[3, 3];

        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 p = pts[i] - mean;

            a[0, 0] += p.x * p.x;
            a[0, 1] += p.x * p.y;
            a[0, 2] += p.x * p.z;

            a[1, 0] += p.y * p.x;
            a[1, 1] += p.y * p.y;
            a[1, 2] += p.y * p.z;

            a[2, 0] += p.z * p.x;
            a[2, 1] += p.z * p.y;
            a[2, 2] += p.z * p.z;
        }

        double[,] eigenVectors =
        {
            { 1.0, 0.0, 0.0 },
            { 0.0, 1.0, 0.0 },
            { 0.0, 0.0, 1.0 }
        };

        for (int iter = 0; iter < 40; iter++)
        {
            int pIndex = 0;
            int qIndex = 1;
            double maxOff = Math.Abs(a[0, 1]);

            if (Math.Abs(a[0, 2]) > maxOff)
            {
                maxOff = Math.Abs(a[0, 2]);
                pIndex = 0;
                qIndex = 2;
            }

            if (Math.Abs(a[1, 2]) > maxOff)
            {
                maxOff = Math.Abs(a[1, 2]);
                pIndex = 1;
                qIndex = 2;
            }

            if (maxOff < 1e-12)
                break;

            double app = a[pIndex, pIndex];
            double aqq = a[qIndex, qIndex];
            double apq = a[pIndex, qIndex];

            double phi =
                0.5 * Math.Atan2(
                    2.0 * apq,
                    aqq - app);

            double c = Math.Cos(phi);
            double s = Math.Sin(phi);

            for (int k = 0; k < 3; k++)
            {
                double aik = a[pIndex, k];
                double aqk = a[qIndex, k];

                a[pIndex, k] = c * aik - s * aqk;
                a[qIndex, k] = s * aik + c * aqk;
            }

            for (int k = 0; k < 3; k++)
            {
                double akp = a[k, pIndex];
                double akq = a[k, qIndex];

                a[k, pIndex] = c * akp - s * akq;
                a[k, qIndex] = s * akp + c * akq;
            }

            for (int k = 0; k < 3; k++)
            {
                double vip = eigenVectors[k, pIndex];
                double viq = eigenVectors[k, qIndex];

                eigenVectors[k, pIndex] =
                    c * vip - s * viq;

                eigenVectors[k, qIndex] =
                    s * vip + c * viq;
            }
        }

        int min = 0;

        if (a[1, 1] < a[min, min])
            min = 1;

        if (a[2, 2] < a[min, min])
            min = 2;

        return new Vector3(
            (float)eigenVectors[0, min],
            (float)eigenVectors[1, min],
            (float)eigenVectors[2, min]).normalized;
    }

    private void SagVeYukariYonunuHesapla()
    {
        Vector3 worldRight =
            vehicleRoot != null
                ? vehicleRoot.right
                : Vector3.right;

        Vector3 rightLocal =
            steeringRoot.InverseTransformDirection(worldRight);

        rightLocal =
            Vector3.ProjectOnPlane(
                rightLocal,
                direksiyonEkseniLocal);

        if (rightLocal.sqrMagnitude < 0.001f)
        {
            rightLocal =
                Vector3.Cross(
                    direksiyonEkseniLocal,
                    Vector3.up);
        }

        direksiyonSagLocal = rightLocal.normalized;
    }

    // ============================================================
    // GRIPLER
    // ============================================================

    private void GripleriYerlestir()
    {
        if (steeringRoot == null ||
            wheelPoints.Count == 0)
            return;

        if (leftHandGrip == null)
        {
            Transform found =
                steeringRoot.Find("LeftHandGrip");

            if (found != null)
                leftHandGrip = found;
        }

        if (rightHandGrip == null)
        {
            Transform found =
                steeringRoot.Find("RightHandGrip");

            if (found != null)
                rightHandGrip = found;
        }

        if (leftHandGrip == null)
        {
            GameObject go = new GameObject("LeftHandGrip");
            go.transform.SetParent(steeringRoot, false);
            leftHandGrip = go.transform;
        }

        if (rightHandGrip == null)
        {
            GameObject go = new GameObject("RightHandGrip");
            go.transform.SetParent(steeringRoot, false);
            rightHandGrip = go.transform;
        }

        // Gripler mutlaka SteeringWheel ROOT'un child'i olsun.
        if (leftHandGrip.parent != steeringRoot)
            leftHandGrip.SetParent(steeringRoot, true);

        if (rightHandGrip.parent != steeringRoot)
            rightHandGrip.SetParent(steeringRoot, true);

        Vector3 upLocal =
            Vector3.Cross(
                direksiyonEkseniLocal,
                direksiyonSagLocal).normalized;

        // Up yonunu dunya yukarisina yakin olacak sekilde cevir.
        if (Vector3.Dot(
                steeringRoot.TransformDirection(upLocal),
                Vector3.up) < 0f)
        {
            upLocal = -upLocal;
        }

        float radius = 0f;

        for (int i = 0; i < wheelPoints.Count; i++)
        {
            float x =
                Mathf.Abs(
                    Vector3.Dot(
                        wheelPoints[i],
                        direksiyonSagLocal));

            if (x > radius)
                radius = x;
        }

        radius *= gripYariCapCarpani;

        Vector3 yukari =
            upLocal * gripDikeyOffset;

        // Sol 9, sag 3 konumu.
        leftHandGrip.localPosition =
            -direksiyonSagLocal * radius +
            yukari;

        rightHandGrip.localPosition =
            direksiyonSagLocal * radius +
            yukari;

        // Rotation ilk testte kullanilmiyor (elRotasyonAgirligi=0).
        Quaternion rot =
            Quaternion.LookRotation(
                direksiyonEkseniLocal,
                upLocal);

        leftHandGrip.localRotation = rot;
        rightHandGrip.localRotation = rot;
    }

    // ============================================================
    // MANUEL TWO-BONE ARM IK
    // IK PASS GEREKTIRMEZ
    // ============================================================

    private void KemikleriBul()
    {
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

        if (animator != null && animator.isHuman)
        {
            leftUpperArm =
                animator.GetBoneTransform(
                    HumanBodyBones.LeftUpperArm);

            leftLowerArm =
                animator.GetBoneTransform(
                    HumanBodyBones.LeftLowerArm);

            leftHand =
                animator.GetBoneTransform(
                    HumanBodyBones.LeftHand);

            rightUpperArm =
                animator.GetBoneTransform(
                    HumanBodyBones.RightUpperArm);

            rightLowerArm =
                animator.GetBoneTransform(
                    HumanBodyBones.RightLowerArm);

            rightHand =
                animator.GetBoneTransform(
                    HumanBodyBones.RightHand);
        }

        // Humanoid mapping yoksa Mixamo isimlerinden fallback.
        if (leftUpperArm == null)
            leftUpperArm = TransformBul("mixamorig:LeftArm", "LeftArm");

        if (leftLowerArm == null)
            leftLowerArm = TransformBul("mixamorig:LeftForeArm", "LeftForeArm");

        if (leftHand == null)
            leftHand = TransformBul("mixamorig:LeftHand", "LeftHand");

        if (rightUpperArm == null)
            rightUpperArm = TransformBul("mixamorig:RightArm", "RightArm");

        if (rightLowerArm == null)
            rightLowerArm = TransformBul("mixamorig:RightForeArm", "RightForeArm");

        if (rightHand == null)
            rightHand = TransformBul("mixamorig:RightHand", "RightHand");
    }

    private Transform TransformBul(params string[] names)
    {
        if (animator == null)
            return null;

        Transform[] all =
            animator.GetComponentsInChildren<Transform>(true);

        foreach (string wanted in names)
        {
            foreach (Transform t in all)
            {
                if (t != null && t.name == wanted)
                    return t;
            }
        }

        return null;
    }

    private void KolIKCoz(
        Transform upper,
        Transform lower,
        Transform hand,
        Transform target,
        bool solKol,
        float weight)
    {
        if (upper == null ||
            lower == null ||
            hand == null ||
            target == null)
            return;

        Vector3 shoulder = upper.position;
        Vector3 elbow = lower.position;
        Vector3 handPos = hand.position;
        Vector3 targetPos = target.position;

        float upperLen =
            Vector3.Distance(shoulder, elbow);

        float lowerLen =
            Vector3.Distance(elbow, handPos);

        if (upperLen < 0.001f ||
            lowerLen < 0.001f)
            return;

        Vector3 toTarget =
            targetPos - shoulder;

        float distance =
            toTarget.magnitude;

        if (distance < 0.001f)
            return;

        Vector3 dir =
            toTarget / distance;

        float minReach =
            Mathf.Abs(upperLen - lowerLen) + 0.001f;

        float maxReach =
            upperLen + lowerLen - 0.001f;

        float d =
            Mathf.Clamp(
                distance,
                minReach,
                maxReach);

        // Mevcut dirsek tarafini koru; kol ters kirilmasin.
        Vector3 currentElbow =
            elbow - shoulder;

        Vector3 bend =
            currentElbow -
            Vector3.Project(
                currentElbow,
                dir);

        if (bend.sqrMagnitude < 0.0001f)
        {
            Vector3 vehicleDown =
                vehicleRoot != null
                    ? -vehicleRoot.up
                    : Vector3.down;

            Vector3 vehicleSide =
                vehicleRoot != null
                    ? vehicleRoot.right
                    : Vector3.right;

            bend =
                vehicleDown +
                vehicleSide * (solKol ? -0.35f : 0.35f);

            bend =
                Vector3.ProjectOnPlane(
                    bend,
                    dir);
        }

        bend.Normalize();

        float cosShoulder =
            Mathf.Clamp(
                (upperLen * upperLen +
                 d * d -
                 lowerLen * lowerLen) /
                (2f * upperLen * d),
                -1f,
                1f);

        float along =
            cosShoulder * upperLen;

        float side =
            Mathf.Sqrt(
                Mathf.Max(
                    0f,
                    upperLen * upperLen -
                    along * along));

        Vector3 desiredElbow =
            shoulder +
            dir * along +
            bend * side;

        // Ust kolu dirsege cevir.
        Vector3 currentUpperDir =
            lower.position - upper.position;

        Vector3 desiredUpperDir =
            desiredElbow - upper.position;

        if (currentUpperDir.sqrMagnitude > 0.0001f &&
            desiredUpperDir.sqrMagnitude > 0.0001f)
        {
            Quaternion delta =
                Quaternion.FromToRotation(
                    currentUpperDir,
                    desiredUpperDir);

            Quaternion desiredRot =
                delta * upper.rotation;

            upper.rotation =
                Quaternion.Slerp(
                    upper.rotation,
                    desiredRot,
                    weight);
        }

        // Parent rotasyonu sonrasi pozisyonlar guncellendi.
        Vector3 currentLowerDir =
            hand.position - lower.position;

        Vector3 desiredLowerDir =
            targetPos - lower.position;

        if (currentLowerDir.sqrMagnitude > 0.0001f &&
            desiredLowerDir.sqrMagnitude > 0.0001f)
        {
            Quaternion delta =
                Quaternion.FromToRotation(
                    currentLowerDir,
                    desiredLowerDir);

            Quaternion desiredRot =
                delta * lower.rotation;

            lower.rotation =
                Quaternion.Slerp(
                    lower.rotation,
                    desiredRot,
                    weight);
        }

        // Bilek rotasyonu istege bagli.
        if (elRotasyonAgirligi > 0.001f)
        {
            hand.rotation =
                Quaternion.Slerp(
                    hand.rotation,
                    target.rotation,
                    weight * elRotasyonAgirligi);
        }
    }

    // ============================================================
    // BASIT SISTEM
    // ============================================================

    private void OtomatikBul()
    {
        SteeringRootuDuzelt();

        if (steeringRoot != null)
        {
            if (leftHandGrip == null)
            {
                Transform found =
                    steeringRoot.Find("LeftHandGrip");

                if (found != null)
                    leftHandGrip = found;
            }

            if (rightHandGrip == null)
            {
                Transform found =
                    steeringRoot.Find("RightHandGrip");

                if (found != null)
                    rightHandGrip = found;
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

        if (basitSistem == null)
            return;

        Type currentType =
            basitSistem.GetType();

        if (basitType == currentType)
            return;

        basitType = currentType;

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

        if (vehicleRoot == null &&
            fDriveRoot != null)
        {
            vehicleRoot =
                fDriveRoot.GetValue(
                    basitSistem) as Transform;
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

            if (vehicleRoot == null &&
                fDriveRoot != null)
            {
                vehicleRoot =
                    fDriveRoot.GetValue(
                        basitSistem) as Transform;
            }
        }
        catch { }
    }

    private void OnDisable()
    {
        if (steeringRoot != null)
        {
            steeringRoot.localPosition =
                baslangicLocalPosition;

            steeringRoot.localRotation =
                baslangicLocalRotation;
        }

        currentAngle = 0f;
        currentIKWeight = 0f;
    }
}