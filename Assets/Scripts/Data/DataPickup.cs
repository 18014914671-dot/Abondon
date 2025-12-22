using System.Collections;
using UnityEngine;

public class DataPickup : MonoBehaviour
{
    [Header("Data")]
    public DataItem dataItem;

    [Header("Respawn (STG usually OFF)")]
    public bool respawn = false;                  // ✅ 默认不刷新
    [Min(0.1f)] public float respawnSeconds = 10f;

    [Header("Optional: Popup UI")]
    public DataPopupUI dataPopupUI;

    private Collider2D[] _colliders;
    private SpriteRenderer[] _renderers;

    private bool _collected;

    private void Awake()
    {
        _colliders = GetComponentsInChildren<Collider2D>(true);
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (dataPopupUI == null)
            dataPopupUI = FindFirstObjectByType<DataPopupUI>();
    }

    private void OnEnable()
    {
        _collected = false;
        SetActivePickup(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        TryCollect();
    }

    private void TryCollect()
    {
        if (_collected) return;
        _collected = true;

        // 先禁用碰撞与显示，防止同一帧重复触发
        SetActivePickup(false);

        if (dataItem == null || string.IsNullOrEmpty(dataItem.id))
        {
            Debug.LogWarning("[DataPickup] Missing dataItem or id.");
            _collected = false;
            SetActivePickup(true);
            return;
        }

        var inv = PlayerInventory.Instance ?? FindFirstObjectByType<PlayerInventory>();
        if (inv == null)
        {
            Debug.LogError("[DataPickup] PlayerInventory not found.");
            _collected = false;
            SetActivePickup(true);
            return;
        }

        int before = inv.GetCount(dataItem.id);
        inv.Add(dataItem.id, 1);
        bool firstTime = (before == 0);

        if (firstTime && dataPopupUI != null)
        {
            dataPopupUI.Show(dataItem.displayName, dataItem.icon, dataItem.description);
        }

        // ✅ STG：吃了就消失
        if (respawn)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnSeconds);
        _collected = false;
        SetActivePickup(true);
    }

    private void SetActivePickup(bool active)
    {
        if (_colliders != null)
            foreach (var c in _colliders) if (c != null) c.enabled = active;

        if (_renderers != null)
            foreach (var r in _renderers) if (r != null) r.enabled = active;
    }
}
