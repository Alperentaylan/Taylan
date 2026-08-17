
using UnityEngine;

public class CarInteractionRaycaster : MonoBehaviour
{
    [Header("Crosshair Raycast")]
    public Camera viewCamera;
    public float interactionDistance = 3.0f;
    public LayerMask interactionLayers = ~0;

    [Header("Tuş")]
    public KeyCode interactionKey = KeyCode.F;

    private CarDoorInteractable currentDoor;

    private void Awake()
    {
        if (viewCamera == null)
            viewCamera = Camera.main;
    }

    private void Update()
    {
        // Araçtayken F: araçtan çık.
        if (CarDoorInteractable.ActiveCar != null)
        {
            ClearCurrentHighlight();

            if (Input.GetKeyDown(interactionKey))
                CarDoorInteractable.ActiveCar.TryExit();

            return;
        }

        if (viewCamera == null || !viewCamera.isActiveAndEnabled)
        {
            viewCamera = Camera.main;
            ClearCurrentHighlight();
            return;
        }

        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        CarDoorInteractable foundDoor = null;

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                interactionLayers,
                QueryTriggerInteraction.Collide))
        {
            foundDoor = hit.collider.GetComponentInParent<CarDoorInteractable>();
        }

        if (foundDoor != currentDoor)
        {
            if (currentDoor != null)
                currentDoor.SetHighlighted(false);

            currentDoor = foundDoor;

            if (currentDoor != null)
                currentDoor.SetHighlighted(true);
        }

        if (currentDoor != null && Input.GetKeyDown(interactionKey))
            currentDoor.TryEnter();
    }

    private void ClearCurrentHighlight()
    {
        if (currentDoor != null)
            currentDoor.SetHighlighted(false);

        currentDoor = null;
    }

    private void OnDisable()
    {
        ClearCurrentHighlight();
    }
}
