using UnityEngine;

/// <summary>
/// 開發用作弊碼。
/// 在遊戲中依序按下 S → K → I → P 即可跳過目前階段（白天/夜晚）。
/// 掛在任意常駐 GameObject 上（例如 GameManager）。
/// </summary>
public class CheatCode : MonoBehaviour
{
    private static readonly KeyCode[] k_Sequence =
    {
        KeyCode.S, KeyCode.K, KeyCode.I, KeyCode.P
    };

    private int _progress = 0;   // 目前已輸入到第幾個字元

    private void Update()
    {
        // 有任何非預期的字母鍵被按下時，重設進度（避免誤觸累積）
        if (Input.anyKeyDown)
        {
            KeyCode expected = k_Sequence[_progress];

            if (Input.GetKeyDown(expected))
            {
                _progress++;
                if (_progress >= k_Sequence.Length)
                {
                    _progress = 0;
                    Activate();
                }
            }
            else
            {
                // 按錯了：重設，但如果按的剛好是序列第一個，從 1 開始
                _progress = Input.GetKeyDown(k_Sequence[0]) ? 1 : 0;
            }
        }
    }

    private void Activate()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.CheatSkipPhase();
        Debug.Log("[Cheat] SKIP 作弊碼觸發");
    }
}
