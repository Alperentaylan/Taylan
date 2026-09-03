using UnityEngine;

/// <summary>
/// Humanoid Animator'in sol elini kayar kapidaki hedefe yumusakca baglar.
/// Bu component Animator ile ayni GameObject uzerinde bulunmalidir.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class SurguluKapiSolElIK : MonoBehaviour
{
    private Animator animator;
    private Transform hedef;
    private float hedefPozisyonAgirligi;
    private float hedefRotasyonAgirligi;
    private float guncelPozisyonAgirligi;
    private float guncelRotasyonAgirligi;
    private float gecisHizi = 5f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void HedefiAyarla(
        Transform yeniHedef,
        float pozisyonAgirligi,
        float rotasyonAgirligi,
        float yumusamaHizi)
    {
        hedef = yeniHedef;
        hedefPozisyonAgirligi = Mathf.Clamp01(pozisyonAgirligi);
        hedefRotasyonAgirligi = Mathf.Clamp01(rotasyonAgirligi);
        gecisHizi = Mathf.Max(0.1f, yumusamaHizi);
    }

    public void Kapat()
    {
        hedefPozisyonAgirligi = 0f;
        hedefRotasyonAgirligi = 0f;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
            return;

        guncelPozisyonAgirligi = Mathf.MoveTowards(
            guncelPozisyonAgirligi,
            hedefPozisyonAgirligi,
            Time.deltaTime * gecisHizi
        );
        guncelRotasyonAgirligi = Mathf.MoveTowards(
            guncelRotasyonAgirligi,
            hedefRotasyonAgirligi,
            Time.deltaTime * gecisHizi
        );

        animator.SetIKPositionWeight(
            AvatarIKGoal.LeftHand,
            guncelPozisyonAgirligi
        );
        animator.SetIKRotationWeight(
            AvatarIKGoal.LeftHand,
            guncelRotasyonAgirligi
        );

        if (hedef == null || guncelPozisyonAgirligi <= 0.001f)
            return;

        animator.SetIKPosition(AvatarIKGoal.LeftHand, hedef.position);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, hedef.rotation);
    }
}
