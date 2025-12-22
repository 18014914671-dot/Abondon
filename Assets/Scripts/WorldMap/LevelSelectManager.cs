using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // ✨ 新增：使用 TextMeshPro

public class LevelSelectManager : MonoBehaviour
{
    [System.Serializable]
    public class PlanetSlot
    {
        [Header("这个星球在场景里的物体（3D 球体）")]
        public Transform planetTransform;

        [Header("进入这个星球要加载的场景名")]
        public string sceneName;

        [Header("在 UI 或 Debug 里显示的名字")]
        public string displayName = "Planet";

        [Header("星球说明文字")]
        [TextArea(2, 4)]
        public string description; // ✨ 新增：每个星球自己的描述文本

        [HideInInspector] public Vector3 originalScale;
        [HideInInspector] public Vector3 baseLocalPos; // 记录初始 localPosition
    }

    [Header("所有星球配置（按顺序摆）")]
    public PlanetSlot[] planets;

    [Header("父节点（一般是 PlanetsRoot）")]
    public Transform planetsRoot;

    [Header("选中星球在父节点坐标系中的“中心 X”位置（可在外部调）")]
    public float centerX = 0f;

    [Header("选中星球的放大倍数")]
    public float selectedScaleMultiplier = 1.4f;

    [Header("平滑移动插值速度")]
    public float moveLerpSpeed = 8f;

    [Header("是否在 Console 打印调试信息")]
    public bool debugLog = true;

    [Header("UI：星球名称 Text")]
    public TextMeshProUGUI planetNameText; // ✨ 新增：标题文字

    [Header("UI：星球说明 Text")]
    public TextMeshProUGUI planetDescriptionText; // ✨ 新增：描述文字

    private int currentIndex = 0;
    private Vector3 planetsRootOriginalLocalPos;

    private void Awake()
    {
        if (planetsRoot == null)
        {
            // 如果没填，就默认用自己的 Transform 当父节点
            planetsRoot = transform;
        }

        // 记录父物体的初始位置（Y/Z 保持不变，只在 X 上移动）
        planetsRootOriginalLocalPos = planetsRoot.localPosition;

        // 记录每个星球的原始缩放和初始 localPosition
        if (planets != null)
        {
            for (int i = 0; i < planets.Length; i++)
            {
                var slot = planets[i];
                if (slot.planetTransform != null)
                {
                    slot.originalScale = slot.planetTransform.localScale;
                    slot.baseLocalPos = slot.planetTransform.localPosition;
                }
            }
        }
    }

    private void Start()
    {
        currentIndex = 0;
        // 初始时直接对齐一次（不用插值）
        UpdateVisuals(true);
        // ✨ 初始时刷新一次文字
        UpdateTextForCurrentPlanet();
    }

    private void Update()
    {
        bool moved = false;

        // 左右切换
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            MoveSelection(-1);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            MoveSelection(1);
            moved = true;
        }

        // 每帧都插值，让父节点缓慢移动到目标位置
        UpdateVisuals(moved && Time.frameCount == 1 ? true : false);

        // 空格确认
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmSelection();
        }
    }

    private void MoveSelection(int delta)
    {
        if (planets == null || planets.Length == 0) return;

        currentIndex += delta;

        // 循环选择
        if (currentIndex < 0) currentIndex = planets.Length - 1;
        if (currentIndex >= planets.Length) currentIndex = 0;

        if (debugLog)
        {
            Debug.Log($"选择星球：{planets[currentIndex].displayName} (index {currentIndex})");
        }

        // ✨ 切换选中星球时更新 UI 文本
        UpdateTextForCurrentPlanet();
    }

    /// <summary>
    /// 更新星球组的位置 & 每个星球的缩放
    /// </summary>
    /// <param name="snap">true：直接跳到目标位置；false：插值过去</param>
    private void UpdateVisuals(bool snap)
    {
        if (planets == null || planets.Length == 0) return;

        // 目标：让当前星球的 baseLocalPos.x + planetsRoot.localPosition.x == centerX
        PlanetSlot current = planets[currentIndex];

        if (current.planetTransform != null)
        {
            float targetRootX =
                planetsRootOriginalLocalPos.x + (centerX - current.baseLocalPos.x);

            Vector3 targetRootPos = new Vector3(
                targetRootX,
                planetsRootOriginalLocalPos.y,
                planetsRootOriginalLocalPos.z
            );

            if (snap)
            {
                planetsRoot.localPosition = targetRootPos;
            }
            else
            {
                planetsRoot.localPosition = Vector3.Lerp(
                    planetsRoot.localPosition,
                    targetRootPos,
                    Time.deltaTime * moveLerpSpeed
                );
            }
        }

        // 缩放视觉：选中的放大，其他恢复
        for (int i = 0; i < planets.Length; i++)
        {
            var slot = planets[i];
            if (slot.planetTransform == null) continue;

            Vector3 targetScale = (i == currentIndex)
                ? slot.originalScale * selectedScaleMultiplier
                : slot.originalScale;

            slot.planetTransform.localScale = Vector3.Lerp(
                slot.planetTransform.localScale,
                targetScale,
                Time.deltaTime * moveLerpSpeed
            );
        }
    }

    /// <summary>
    /// 把当前选中星球的信息写到 UI 上
    /// </summary>
    private void UpdateTextForCurrentPlanet()
    {
        if (planets == null || planets.Length == 0) return;

        var slot = planets[currentIndex];

        if (planetNameText != null)
        {
            planetNameText.text = slot.displayName;
        }

        if (planetDescriptionText != null)
        {
            planetDescriptionText.text = slot.description;
        }
    }

    private void ConfirmSelection()
    {
        if (planets == null || planets.Length == 0) return;

        var slot = planets[currentIndex];
        if (string.IsNullOrEmpty(slot.sceneName))
        {
            Debug.LogWarning($"星球 {slot.displayName} 没有配置 sceneName！");
            return;
        }

        if (debugLog)
        {
            Debug.Log($"进入星球：{slot.displayName}，加载场景：{slot.sceneName}");
        }

        SceneManager.LoadScene(slot.sceneName);
    }
}
