using UnityEngine;
using System;

/// <summary>
/// 開發用作弊碼（支援多組序列同時偵測）。
///
/// 目前支援：
///   S → K → I → P  跳過目前階段（白天/夜晚）
///   F → O → O → D  食物設成 100000
///   S → H → I → P  船血量與上限設成 100000
/// </summary>
public class CheatCode : MonoBehaviour
{
    // ── 序列定義 ──────────────────────────────────────────

    private class Sequence
    {
        public KeyCode[] keys;
        public Action    action;
        public int       progress;
    }

    private Sequence[] sequences;

    private void Awake()
    {
        sequences = new[]
        {
            new Sequence
            {
                keys   = new[] { KeyCode.S, KeyCode.K, KeyCode.I, KeyCode.P },
                action = () =>
                {
                    GameManager.Instance?.CheatSkipPhase();
                    Debug.Log("[Cheat] SKIP");
                }
            },
            new Sequence
            {
                keys   = new[] { KeyCode.F, KeyCode.O, KeyCode.O, KeyCode.D },
                action = () =>
                {
                    ResourceManager.Instance?.CheatSetFood();
                    Debug.Log("[Cheat] FOOD");
                }
            },
            new Sequence
            {
                keys   = new[] { KeyCode.S, KeyCode.H, KeyCode.I, KeyCode.P },
                action = () =>
                {
                    ResourceManager.Instance?.CheatSetShipHP();
                    Debug.Log("[Cheat] SHIP");
                }
            },
        };
    }

    // ── Update ────────────────────────────────────────────

    private void Update()
    {
        if (!Input.anyKeyDown) return;

        foreach (var seq in sequences)
        {
            KeyCode expected = seq.keys[seq.progress];

            if (Input.GetKeyDown(expected))
            {
                seq.progress++;
                if (seq.progress >= seq.keys.Length)
                {
                    seq.progress = 0;
                    seq.action?.Invoke();
                }
            }
            else
            {
                // 按錯：重設；若剛好是該序列第一個鍵，從 1 開始
                seq.progress = Input.GetKeyDown(seq.keys[0]) ? 1 : 0;
            }
        }
    }
}
