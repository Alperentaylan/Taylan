using UnityEngine;

/// <summary>
/// V tuşuyla üçüncü şahıs ve birinci şahıs kamera arasında geçiş yapar.
/// Bu scripti karakterin ana nesnesine ekle.
/// </summary>
public class KameraGecis : MonoBehaviour
{
    [Header("Kameralar")]
    [Tooltip("Mevcut üçüncü şahıs kameranın kendisi veya kamera rig'inin ana nesnesi.")]
    public GameObject ucuncuSahisKameraRoot;

    [Tooltip("Mevcut üçüncü şahıs Camera bileşeni.")]
    public Camera ucuncuSahisKamera;

    [Tooltip("Karakterin göz hizasına koyduğun birinci şahıs kamerası.")]
    public Camera birinciSahisKamera;

    [Header("Birinci Şahıs Dönüşü")]
    [Tooltip("Karakterin sağa-sola dönecek ana Transform'u.")]
    public Transform karakterGovdesi;

    [Tooltip("Birinci şahıs kamerasının bağlı olduğu göz hizasındaki boş nesne.")]
    public Transform birinciSahisNoktasi;

    public float fareHassasiyeti = 2.2f;
    public float yukariBakmaSiniri = 80f;
    public float asagiBakmaSiniri = -80f;

    [Header("Geçiş")]
    public KeyCode gecisTusu = KeyCode.V;
    public bool oyunBaslangicindaBirinciSahis = false;

    [Header("Birinci Şahısta Gizlenecek Parçalar")]
    [Tooltip("Kafanın kameraya girmemesi için saç, kafa, yüz gibi Renderer'ları buraya ekleyebilirsin.")]
    public Renderer[] birinciSahistaGizlenecekler;

    private bool birinciSahisAktif;
    private float dikeyAci;

    private void Awake()
    {
        if (karakterGovdesi == null)
            karakterGovdesi = transform;

        if (birinciSahisKamera != null &&
            birinciSahisNoktasi != null)
        {
            birinciSahisKamera.transform.SetParent(
                birinciSahisNoktasi,
                false);

            birinciSahisKamera.transform.localPosition =
                Vector3.zero;

            birinciSahisKamera.transform.localRotation =
                Quaternion.identity;
        }

        GorunumuDegistir(oyunBaslangicindaBirinciSahis);
    }

    private void Update()
    {
        if (Input.GetKeyDown(gecisTusu))
        {
            GorunumuDegistir(!birinciSahisAktif);
        }

        if (birinciSahisAktif)
        {
            BirinciSahisBakis();
        }
    }

    private void BirinciSahisBakis()
    {
        if (karakterGovdesi == null ||
            birinciSahisNoktasi == null)
        {
            return;
        }

        float fareX =
            Input.GetAxis("Mouse X") *
            fareHassasiyeti;

        float fareY =
            Input.GetAxis("Mouse Y") *
            fareHassasiyeti;

        karakterGovdesi.Rotate(
            Vector3.up * fareX,
            Space.World);

        dikeyAci -= fareY;

        dikeyAci = Mathf.Clamp(
            dikeyAci,
            asagiBakmaSiniri,
            yukariBakmaSiniri);

        birinciSahisNoktasi.localRotation =
            Quaternion.Euler(
                dikeyAci,
                0f,
                0f);
    }

    private void GorunumuDegistir(bool birinciSahisaGec)
    {
        birinciSahisAktif =
            birinciSahisaGec;

        if (ucuncuSahisKameraRoot != null)
        {
            ucuncuSahisKameraRoot.SetActive(
                !birinciSahisAktif);
        }

        if (birinciSahisKamera != null)
        {
            birinciSahisKamera.gameObject.SetActive(
                birinciSahisAktif);
        }

        if (ucuncuSahisKamera != null)
        {
            ucuncuSahisKamera.tag =
                birinciSahisAktif
                    ? "Untagged"
                    : "MainCamera";
        }

        if (birinciSahisKamera != null)
        {
            birinciSahisKamera.tag =
                birinciSahisAktif
                    ? "MainCamera"
                    : "Untagged";
        }

        if (birinciSahistaGizlenecekler != null)
        {
            foreach (Renderer parca in birinciSahistaGizlenecekler)
            {
                if (parca != null)
                {
                    parca.enabled =
                        !birinciSahisAktif;
                }
            }
        }

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }
}