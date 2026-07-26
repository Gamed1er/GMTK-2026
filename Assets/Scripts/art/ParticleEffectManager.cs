using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectManager : MonoBehaviour
{
    public static ParticleEffectManager Instance { get; private set; }

    [Serializable]
    private class ParticleEffectData
    {
        public string effectName;
        public GameObject prefab;
    }

    [Header("Particle Effects")]
    [SerializeField]
    private ParticleEffectData[] effects;

    private readonly Dictionary<string, GameObject> effectTable = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildEffectTable();
    }

    private void BuildEffectTable()
    {
        effectTable.Clear();

        foreach (ParticleEffectData effect in effects)
        {
            if (effect == null)
                continue;

            if (string.IsNullOrWhiteSpace(effect.effectName))
                continue;

            if (effect.prefab == null)
                continue;

            effectTable[effect.effectName] = effect.prefab;
        }
    }

    public GameObject Play(string effectName, Vector3 position)
    {
        if (!effectTable.TryGetValue(effectName, out GameObject prefab))
        {
            Debug.LogWarning($"Particle Effect '{effectName}' not found.");
            return null;
        }

        GameObject effectObject = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        ParticleSystem particleSystem =
            effectObject.GetComponent<ParticleSystem>();

        if (particleSystem == null)
        {
            Debug.LogWarning(
                $"Particle Effect '{effectName}' prefab 沒有 ParticleSystem。",
                effectObject
            );

            Destroy(effectObject);
            return null;
        }

        particleSystem.Clear();
        particleSystem.Play();

        // 粒子播放完後自動刪除
        Destroy(effectObject, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);

        return effectObject;
    }
}