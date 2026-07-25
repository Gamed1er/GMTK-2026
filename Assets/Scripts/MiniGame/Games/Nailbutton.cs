using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 單根釘子的按鈕。每點一下切換一張「打入進度」圖片（套用該圖片原始尺寸），
/// 並讓釘子圖片往下移動指定像素（模擬被敲進去的視覺效果），
/// 點滿指定次數後回報給 PatchHoleMinigame，並停用自己（不可再點）。
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))]
public class NailButton : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("釘子從『尚未打入』到『完全釘牢』的圖片序列，長度應等於 hitsRequired + 1（第0張=初始狀態），每張圖會套用其原始尺寸")]
    [SerializeField] private Image nailImage;
    [SerializeField] private Sprite[] hitStageSprites; // index 0 = 初始, 最後一張 = 完全釘牢

    [Header("Hammer Visual")]
    [Tooltip("每次點擊時，釘子圖片往下移動的像素（模擬被敲進去）")]
    [SerializeField] private float hitMoveDownPixels = 40f;

    [Header("Audio")]
    [Tooltip("每次點擊（敲釘子）播放的音效")]
    [SerializeField] private AudioClip hammerHitSfx;
    [Tooltip("敲釘子音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float hammerHitSfxVolume = 1f;

    private Button button;
    private AudioSource audioSource;
    private RectTransform nailImageRect;
    private int hitsRequired;
    private int currentHits;
    private Action<NailButton> onFullyHammered;

    private void Awake()
    {
        button = GetComponent<Button>();
        audioSource = GetComponent<AudioSource>();
        button.onClick.AddListener(OnClicked);

        if (nailImage != null)
            nailImageRect = nailImage.rectTransform;
    }

    /// <summary>由 PatchHoleMinigame 生成後立刻呼叫</summary>
    public void Init(int hitsRequired, Action<NailButton> onFullyHammered)
    {
        this.hitsRequired = Mathf.Max(1, hitsRequired);
        this.onFullyHammered = onFullyHammered;
        currentHits = 0;

        UpdateSprite();
    }

    private void OnClicked()
    {
        if (currentHits >= hitsRequired) return; // 保險，理論上打滿後按鈕已停用

        PlaySfx(hammerHitSfx, hammerHitSfxVolume);

        currentHits++;
        UpdateSprite();
        MoveNailDown();

        if (currentHits >= hitsRequired)
        {
            button.interactable = false;
            onFullyHammered?.Invoke(this);
        }
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    private void UpdateSprite()
    {
        if (nailImage == null || hitStageSprites == null || hitStageSprites.Length == 0) return;

        // 依目前打擊次數對應到圖片索引，並確保不超出陣列範圍
        int index = Mathf.Clamp(currentHits, 0, hitStageSprites.Length - 1);
        nailImage.sprite = hitStageSprites[index];

        // 套用該圖片的原始尺寸（每個階段的圖片大小可能不同）
        nailImage.SetNativeSize();
    }

    private void MoveNailDown()
    {
        if (nailImageRect == null) return;

        Vector2 pos = nailImageRect.anchoredPosition;
        pos.y -= hitMoveDownPixels;
        nailImageRect.anchoredPosition = pos;
    }
}