using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [Header("Break Effect")]
    [SerializeField] private ParticleSystem brokenEffectPrefab;
    [SerializeField] private Transform effectSpawnPoint;

    [Header("Object")]
    [SerializeField] private bool destroyAfterBroken = true;

    private bool isBroken;

    public void Break()
    {
        if (isBroken)
            return;

        isBroken = true;

        Vector3 spawnPosition = effectSpawnPoint != null
            ? effectSpawnPoint.position
            : transform.position;

        if (brokenEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(
                brokenEffectPrefab,
                spawnPosition,
                Quaternion.identity
            );

            effect.Play();
        }

        if (destroyAfterBroken)
        {
            Destroy(gameObject);
        }
    }
}