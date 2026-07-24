using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 每晚從事件池加權隨機抽 3 個事件。
/// </summary>
public class NightEventManager : MonoBehaviour
{
    public static NightEventManager Instance { get; private set; }

    [SerializeField] private List<NightEventData> allEvents;
    [SerializeField] private int eventsPerNight = 3;

    public List<NightEventData> CurrentNightEvents { get; private set; } = new();

    /// <summary>事件抽完後觸發，NightPhaseUI 訂閱這個而非 OnNightStarted</summary>
    public event Action OnEventsReady;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.OnNightStarted += OnNightStarted;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnNightStarted -= OnNightStarted;
    }

    private void OnNightStarted()
    {
        PickEvents();
        OnEventsReady?.Invoke(); // 抽完才通知 UI
    }

    // ── Public API ────────────────────────────────────────

    public void AcceptEvent(NightEventData data)
    {
        ResourceManager.Instance.ApplyDelta(data.acceptDelta);
        Debug.Log($"[NightEventManager] Accepted: {data.type}");
    }

    public void RejectEvent(NightEventData data)
    {
        ResourceManager.Instance.ApplyDelta(data.rejectDelta);
        Debug.Log($"[NightEventManager] Rejected: {data.type}");
    }

    // ── Internal ──────────────────────────────────────────

    private void PickEvents()
    {
        CurrentNightEvents.Clear();

        // 加權隨機、不重複
        var pool = new List<NightEventData>(allEvents);
        int count = Mathf.Min(eventsPerNight, pool.Count);

        for (int i = 0; i < count; i++)
        {
            float total = 0f;
            foreach (var e in pool) total += e.spawnWeight;

            float roll = UnityEngine.Random.Range(0f, total);
            float cumulative = 0f;
            for (int j = 0; j < pool.Count; j++)
            {
                cumulative += pool[j].spawnWeight;
                if (roll <= cumulative)
                {
                    CurrentNightEvents.Add(pool[j]);
                    pool.RemoveAt(j);
                    break;
                }
            }
        }

        Debug.Log($"[NightEventManager] Picked {CurrentNightEvents.Count} events for tonight.");
    }
}
