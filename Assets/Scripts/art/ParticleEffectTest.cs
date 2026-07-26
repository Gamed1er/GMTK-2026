using UnityEngine;

public class ParticleEffectTest : MonoBehaviour
{
    [SerializeField] private string effectName = "Spark";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ParticleEffectManager.Instance.Play(
                effectName,
                transform.position
            );
        }
    }
}