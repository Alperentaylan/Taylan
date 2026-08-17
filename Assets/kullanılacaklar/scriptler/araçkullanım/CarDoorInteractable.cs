
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CarDoorInteractable : MonoBehaviour
{
    public static CarDoorInteractable ActiveCar { get; private set; }

    [Header("ARABA")]
    public RealisticCarController carController;
    public Transform driverDoorPivot;
    public Transform driverDoorVisualRoot;

    [Header("KARAKTER - SADECE ANA OBJENİ VER")]
    [Tooltip("Hierarchy'deki Ch31_nonPBR (1) objesini buraya sürükle.")]
    public Transform playerRoot;

    [Tooltip("Boş bırakılabilir. Player Root verildiğinde otomatik bulunur.")]
    public Animator playerAnimator;

    [Tooltip("Boş bırakılabilir. Player Root verildiğinde otomatik bulunur.")]
    public CharacterController playerCharacterController;

    [Tooltip("Boş bırakılabilir. Karakterde Rigidbody varsa otomatik bulunur.")]
    public Rigidbody playerRigidbody;

    [Header("OTOMATİK KAPATILACAK SCRIPTLER")]
    [Tooltip("Elle doldurma. KarakterHareketi ve BirinciUcuncuSahisKesin otomatik bulunur.")]
    public bool autoFindPlayerScripts = true;

    [Header("GİRİŞ / KOLTUK / ÇIKIŞ")]
    public Transform entryPoint;
    public Transform seatPoint;
    public Transform exitPoint;

    [Header("KAMERALAR")]
    public GameObject playerCameraRoot;
    public GameObject carCameraRoot;

    [Header("ENTERINGCAR ANİMASYONU")]
    public string enterTriggerName = "EnteringCar";
    public string enterStateName = "EnteringCar";
    public float alignDuration = 0.22f;
    public float enterAnimationDuration = 2.2f;
    public bool useRootMotionDuringEnter = true;
    public bool freezeFinalAnimationPose = true;

    [Header("ŞOFÖR KAPISI")]
    public bool autoOpenDriverDoor = true;
    public Vector3 doorOpenEuler = new Vector3(0f, -68f, 0f);
    public float doorMoveDuration = 0.42f;

    [Header("ÇIKIŞ")]
    public float maxExitSpeedKmh = 1.5f;

    [Header("YEŞİL VURGU")]
    public bool createGreenOutlineAutomatically = true;
    public Color outlineColor = new Color(0.05f, 1f, 0.1f, 0.65f);
    [Range(1.001f, 1.08f)]
    public float outlineScale = 1.018f;

    public bool IsBusy { get; private set; }
    public bool IsPlayerInside { get; private set; }

    private Quaternion closedDoorRotation;
    private Transform originalPlayerParent;
    private bool originalApplyRootMotion;
    private readonly List<MonoBehaviour> automaticallyFoundScripts = new List<MonoBehaviour>();
    private readonly List<GameObject> outlineObjects = new List<GameObject>();
    private Material outlineMaterial;
    private float inputUnlockTime;

    private void Reset()
    {
        carController = GetComponentInParent<RealisticCarController>();
        driverDoorPivot = FindDeepChild(transform, "Door_FL");
        driverDoorVisualRoot = driverDoorPivot;
    }

    private void Awake()
    {
        AutoFillReferences();

        if (driverDoorPivot != null)
            closedDoorRotation = driverDoorPivot.localRotation;

        if (playerRoot != null)
            originalPlayerParent = playerRoot.parent;

        if (playerAnimator != null)
            originalApplyRootMotion = playerAnimator.applyRootMotion;

        if (carController != null)
            carController.SetDriverActive(false);

        if (carCameraRoot != null)
            carCameraRoot.SetActive(false);

        if (playerCameraRoot != null)
            playerCameraRoot.SetActive(true);

        if (createGreenOutlineAutomatically)
            BuildGreenOutline();

        SetHighlighted(false);
    }

    [ContextMenu("REFERANSLARI OTOMATİK DOLDUR")]
    public void AutoFillReferences()
    {
        if (carController == null)
            carController = GetComponentInParent<RealisticCarController>();

        if (driverDoorPivot == null)
            driverDoorPivot = FindDeepChild(transform, "Door_FL");

        if (driverDoorVisualRoot == null)
            driverDoorVisualRoot = driverDoorPivot;

        if (playerRoot != null)
        {
            if (playerAnimator == null)
                playerAnimator = playerRoot.GetComponent<Animator>();

            if (playerAnimator == null)
                playerAnimator = playerRoot.GetComponentInChildren<Animator>(true);

            if (playerCharacterController == null)
                playerCharacterController = playerRoot.GetComponent<CharacterController>();

            if (playerCharacterController == null)
                playerCharacterController = playerRoot.GetComponentInChildren<CharacterController>(true);

            if (playerRigidbody == null)
                playerRigidbody = playerRoot.GetComponent<Rigidbody>();
        }

        if (autoFindPlayerScripts)
            FindScriptsAutomatically();
    }

    private void FindScriptsAutomatically()
    {
        automaticallyFoundScripts.Clear();

        // KarakterHareketi karakter objesinin üzerinde.
        if (playerRoot != null)
        {
            MonoBehaviour[] playerBehaviours = playerRoot.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour behaviour in playerBehaviours)
            {
                if (behaviour == null)
                    continue;

                string className = behaviour.GetType().Name;

                if (className == "KarakterHareketi")
                    AddScriptOnce(behaviour);
            }
        }

        // Kamera scripti MainCamera, karakter veya başka bir sahne objesinde olabilir.
        MonoBehaviour[] allSceneBehaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in allSceneBehaviours)
        {
            if (behaviour == null || !behaviour.gameObject.scene.IsValid())
                continue;

            string className = behaviour.GetType().Name;

            if (className == "BirinciUcuncuSahisKesin")
                AddScriptOnce(behaviour);
        }

        Debug.Log(
            "Araca binince otomatik kapatılacak script sayısı: " +
            automaticallyFoundScripts.Count,
            this);
    }

    private void AddScriptOnce(MonoBehaviour behaviour)
    {
        if (behaviour == null ||
            behaviour == this ||
            behaviour is CarInteractionRaycaster ||
            behaviour is RealisticCarController)
            return;

        if (!automaticallyFoundScripts.Contains(behaviour))
            automaticallyFoundScripts.Add(behaviour);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (IsBusy || IsPlayerInside)
            highlighted = false;

        foreach (GameObject obj in outlineObjects)
        {
            if (obj != null)
                obj.SetActive(highlighted);
        }
    }

    public void TryEnter()
    {
        if (IsBusy || IsPlayerInside || ActiveCar != null)
            return;

        AutoFillReferences();

        if (playerRoot == null || playerAnimator == null ||
            entryPoint == null || seatPoint == null || exitPoint == null ||
            carController == null)
        {
            Debug.LogError(
                "Eksik alan var. Player Root, EntryPoint, SeatPoint, ExitPoint ve Car Controller atanmalı.",
                this);
            return;
        }

        StartCoroutine(EnterRoutine());
    }

    public void TryExit()
    {
        if (!IsPlayerInside || IsBusy || Time.time < inputUnlockTime)
            return;

        if (carController != null && carController.AbsoluteSpeedKmh > maxExitSpeedKmh)
        {
            Debug.Log("Araç hareket ederken inemezsin.");
            return;
        }

        StartCoroutine(ExitRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        IsBusy = true;
        SetHighlighted(false);

        carController.SetDriverActive(false);
        SetAutomaticPlayerScripts(false);

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        originalPlayerParent = playerRoot.parent;
        originalApplyRootMotion = playerAnimator.applyRootMotion;

        yield return MoveAndRotatePlayer(entryPoint.position, entryPoint.rotation, alignDuration);

        if (autoOpenDriverDoor && driverDoorPivot != null)
        {
            Quaternion openRotation = closedDoorRotation * Quaternion.Euler(doorOpenEuler);
            yield return RotateDoor(openRotation, doorMoveDuration);
        }

        playerAnimator.speed = 1f;
        playerAnimator.applyRootMotion = useRootMotionDuringEnter;
        playerAnimator.ResetTrigger(enterTriggerName);
        playerAnimator.SetTrigger(enterTriggerName);

        yield return new WaitForSeconds(enterAnimationDuration);

        playerRoot.SetParent(seatPoint, false);
        playerRoot.localPosition = Vector3.zero;
        playerRoot.localRotation = Quaternion.identity;

        if (autoOpenDriverDoor && driverDoorPivot != null)
            yield return RotateDoor(closedDoorRotation, doorMoveDuration);

        playerAnimator.applyRootMotion = false;

        if (freezeFinalAnimationPose && !string.IsNullOrWhiteSpace(enterStateName))
        {
            playerAnimator.Play(enterStateName, 0, 1f);
            playerAnimator.Update(0f);
            playerAnimator.speed = 0f;
        }

        if (playerCameraRoot != null)
            playerCameraRoot.SetActive(false);

        if (carCameraRoot != null)
            carCameraRoot.SetActive(true);

        IsPlayerInside = true;
        ActiveCar = this;
        carController.SetDriverActive(true);

        inputUnlockTime = Time.time + 0.55f;
        IsBusy = false;
    }

    private IEnumerator ExitRoutine()
    {
        IsBusy = true;
        carController.SetDriverActive(false);

        if (carCameraRoot != null)
            carCameraRoot.SetActive(false);

        if (playerCameraRoot != null)
            playerCameraRoot.SetActive(true);

        playerAnimator.speed = 1f;
        playerAnimator.applyRootMotion = originalApplyRootMotion;

        playerRoot.SetParent(originalPlayerParent, true);
        playerRoot.position = exitPoint.position;
        playerRoot.rotation = exitPoint.rotation;

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        SetAutomaticPlayerScripts(true);

        IsPlayerInside = false;
        ActiveCar = null;
        inputUnlockTime = Time.time + 0.35f;

        yield return null;
        IsBusy = false;
    }

    private void SetAutomaticPlayerScripts(bool enabledValue)
    {
        foreach (MonoBehaviour behaviour in automaticallyFoundScripts)
        {
            if (behaviour != null)
                behaviour.enabled = enabledValue;
        }
    }

    private IEnumerator MoveAndRotatePlayer(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Vector3 startPosition = playerRoot.position;
        Quaternion startRotation = playerRoot.rotation;

        if (duration <= 0f)
        {
            playerRoot.SetPositionAndRotation(targetPosition, targetRotation);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));

            playerRoot.position = Vector3.Lerp(startPosition, targetPosition, t);
            playerRoot.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        playerRoot.SetPositionAndRotation(targetPosition, targetRotation);
    }

    private IEnumerator RotateDoor(Quaternion targetRotation, float duration)
    {
        Quaternion start = driverDoorPivot.localRotation;

        if (duration <= 0f)
        {
            driverDoorPivot.localRotation = targetRotation;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));
            driverDoorPivot.localRotation = Quaternion.Slerp(start, targetRotation, t);
            yield return null;
        }

        driverDoorPivot.localRotation = targetRotation;
    }

    [ContextMenu("YEŞİL VURGUYU YENİDEN OLUŞTUR")]
    public void BuildGreenOutline()
    {
        ClearOutline();

        if (driverDoorVisualRoot == null)
        {
            Debug.LogWarning("Driver Door Visual Root atanmadı.", this);
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            return;

        outlineMaterial = new Material(shader);
        outlineMaterial.name = "Runtime_GreenDoorOutline";

        if (outlineMaterial.HasProperty("_BaseColor"))
            outlineMaterial.SetColor("_BaseColor", outlineColor);

        if (outlineMaterial.HasProperty("_Color"))
            outlineMaterial.SetColor("_Color", outlineColor);

        if (outlineMaterial.HasProperty("_Cull"))
            outlineMaterial.SetInt("_Cull", (int)CullMode.Front);

        if (outlineMaterial.HasProperty("_ZWrite"))
            outlineMaterial.SetInt("_ZWrite", 0);

        outlineMaterial.renderQueue = 3100;

        MeshRenderer[] renderers =
            driverDoorVisualRoot.GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer sourceRenderer in renderers)
        {
            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();

            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            GameObject shell =
                new GameObject(sourceRenderer.name + "_GreenOutline");

            shell.transform.SetParent(sourceRenderer.transform, false);
            shell.transform.localPosition = Vector3.zero;
            shell.transform.localRotation = Quaternion.identity;
            shell.transform.localScale = Vector3.one * outlineScale;

            MeshFilter filter = shell.AddComponent<MeshFilter>();
            filter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer renderer = shell.AddComponent<MeshRenderer>();
            int materialCount =
                Mathf.Max(1, sourceRenderer.sharedMaterials.Length);

            Material[] materials = new Material[materialCount];

            for (int i = 0; i < materialCount; i++)
                materials[i] = outlineMaterial;

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            shell.SetActive(false);
            outlineObjects.Add(shell);
        }
    }

    private void ClearOutline()
    {
        foreach (GameObject obj in outlineObjects)
        {
            if (obj == null)
                continue;

            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        outlineObjects.Clear();

        if (outlineMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(outlineMaterial);
            else
                DestroyImmediate(outlineMaterial);
        }

        outlineMaterial = null;
    }

    private void OnDestroy()
    {
        if (ActiveCar == this)
            ActiveCar = null;

        ClearOutline();
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
