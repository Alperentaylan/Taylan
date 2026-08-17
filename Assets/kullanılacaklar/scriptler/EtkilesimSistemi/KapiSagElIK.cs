using UnityEngine;

/*
 * Animator ile aynı GameObject üzerinde çalışır. KapiGirisSistemi bu
 * bileşeni çalışma anında otomatik ekler; Inspector'dan eklemek gerekmez.
 */
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class KapiSagElIK : MonoBehaviour
{
    private Animator animator;
    private Vector3 hedefNoktasi;
    private float elAgirligi;
    private float bakisAgirligi;
    private bool aktif;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void HedefiAyarla(
        Vector3 dunyaNoktasi,
        float yeniElAgirligi,
        float yeniBakisAgirligi)
    {
        hedefNoktasi = dunyaNoktasi;
        elAgirligi = Mathf.Clamp01(yeniElAgirligi);
        bakisAgirligi = Mathf.Clamp01(yeniBakisAgirligi);
        aktif = elAgirligi > 0.001f || bakisAgirligi > 0.001f;
    }

    public void Kapat()
    {
        aktif = false;
        elAgirligi = 0f;
        bakisAgirligi = 0f;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        float gercekElAgirligi = aktif ? elAgirligi : 0f;
        float gercekBakisAgirligi = aktif ? bakisAgirligi : 0f;

        animator.SetIKPositionWeight(
            AvatarIKGoal.RightHand,
            gercekElAgirligi
        );

        animator.SetIKRotationWeight(
            AvatarIKGoal.RightHand,
            0f
        );

        if (gercekElAgirligi > 0f)
        {
            animator.SetIKPosition(
                AvatarIKGoal.RightHand,
                hedefNoktasi
            );
        }

        animator.SetLookAtWeight(gercekBakisAgirligi);

        if (gercekBakisAgirligi > 0f)
        {
            animator.SetLookAtPosition(hedefNoktasi);
        }
    }

    void OnDisable()
    {
        Kapat();
    }
}
