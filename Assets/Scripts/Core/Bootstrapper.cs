using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    [Header("Prefabs (optional)")]
    public PlayerInventory playerInventoryPrefab;

    private void Awake()
    {
        // 如果已经有全局库存，就不重复创建
        if (PlayerInventory.Instance != null) return;

        // 方式A：如果你有做成 prefab，就实例化 prefab
        if (playerInventoryPrefab != null)
        {
            Instantiate(playerInventoryPrefab);
            return;
        }

        // 方式B：没有 prefab 就动态创建
        var go = new GameObject("PlayerInventory");
        go.AddComponent<PlayerInventory>();
    }
}
