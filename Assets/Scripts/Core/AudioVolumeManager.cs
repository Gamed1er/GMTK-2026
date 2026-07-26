using UnityEngine;
using System;

/// <summary>
/// 全域音量管理：BGM / SFX 分開控制，以 PlayerPrefs 持久化。
/// 掛在任意常駐 GameObject（例如 LocalizationManager 同一個物件）。
/// 其他 AudioSource 訂閱 OnBGMChanged / OnSFXChanged 來更新音量。
/// </summary>
public class AudioVolumeManager : MonoBehaviour
{
    public static AudioVolumeManager Instance { get; private set; }

    private const string KeyBGM = "vol_bgm";
    private const string KeySFX = "vol_sfx";

    public event Action<float> OnBGMChanged;
    public event Action<float> OnSFXChanged;

    public float BGMVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BGMVolume = PlayerPrefs.GetFloat(KeyBGM, 1f);
        SFXVolume = PlayerPrefs.GetFloat(KeySFX, 1f);
    }

    public void SetBGM(float value)
    {
        BGMVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyBGM, BGMVolume);
        OnBGMChanged?.Invoke(BGMVolume);
    }

    public void SetSFX(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeySFX, SFXVolume);
        OnSFXChanged?.Invoke(SFXVolume);
    }
}
