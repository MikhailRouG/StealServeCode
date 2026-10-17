using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static CraftingUIBuilder;

/// <summary>
/// Builds the item index UI (canvas with <see cref="IndexWindow"/>, category row and slot prefabs) in the open scene.
/// Categories of an existing window keep their order; other IndexCategoryData assets of the project are added after them.
/// The canvas goes under the scene's <see cref="GameUI"/> root; running it again rebuilds the UI in the same place.
/// Run via Tools > Index > Create Index UI.
/// </summary>
public static class IndexUIBuilder
{
    private const string CanvasName = "IndexCanvas";
    private const int Columns = 4;
    private static readonly Vector2 WindowSize = new Vector2(1160f, 680f);
    private static readonly Vector2 SlotSize = new Vector2(128f, 156f);
    private const float SlotSpacing = 10f;
    private const float PictureWidth = 150f;

    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.45f);
    private static readonly Color WindowColor = new Color32(0x2F, 0x32, 0x38, 0xFF);
    private static readonly Color HeaderColor = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
    private static readonly Color DarkPanelColor = new Color32(0x1C, 0x1E, 0x23, 0xFF);
    private static readonly Color BarBackColor = new Color32(0x11, 0x13, 0x18, 0xFF);
    private static readonly Color DetailsColor = new Color32(0x4A, 0x4D, 0x55, 0xFF);
    private static readonly Color RewardsColor = new Color32(0x1E, 0x3A, 0x22, 0xFF);
    private static readonly Color RewardsFrameColor = new Color32(0x2E, 0xAA, 0x3C, 0xFF);
    private static readonly Color RewardBoxColor = new Color32(0x2A, 0x2D, 0x33, 0xFF);
    private static readonly Color GreenColor = new Color32(0x3C, 0xC8, 0x3C, 0xFF);
    private static readonly Color PriceColor = new Color32(0x6C, 0xF0, 0x6C, 0xFF);
    private static readonly Color BlueColor = new Color32(0x2E, 0x9B, 0xE6, 0xFF);
    private static readonly Color CloseColor = new Color32(0xD9, 0x30, 0x2A, 0xFF);
    private static readonly Color ScrollbarColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color ScrollHandleColor = new Color(1f, 1f, 1f, 0.45f);

    private static Sprite _fill;
    private static Sprite _frame;

    [MenuItem("Tools/Index/Create Index UI")]
    public static void Create()
    {
        EnsureFolder(SpritesFolder);
        EnsureFolder(PrefabsFolder);
        _fill = GetOrCreateRoundedSprite(FillSpritePath, frameOnly: false);
        _frame = GetOrCreateRoundedSprite(FrameSpritePath, frameOnly: true);

        List<IndexCategoryData> categories = CollectCategories();

        // A rebuilt canvas takes the old one's place in the hierarchy.
        Transform oldParent = null;
        int oldSiblingIndex = -1;
        IndexWindow existing = Object.FindFirstObjectByType<IndexWindow>(FindObjectsInactive.Include);
        if (existing != null)
        {
            oldParent = existing.transform.parent;
            oldSiblingIndex = existing.transform.GetSiblingIndex();
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        IndexSlotView slotPrefab = SaveSlotPrefab();
        IndexCategoryView categoryPrefab = SaveCategoryPrefab();
        GameObject canvas = BuildCanvas();
        if (oldParent != null)
        {
            canvas.transform.SetParent(oldParent, false);
            canvas.transform.SetSiblingIndex(oldSiblingIndex);
        }
        else
        {
            PutUnderGameUI(canvas);
        }
        BuildWindow(canvas, categories, categoryPrefab, slotPrefab);

        EnsureEventSystem();
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, $"{PrefabsFolder}/{CanvasName}.prefab", InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(canvas, "Create Index UI");
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Selection.activeGameObject = canvas;

        Debug.Log("Index UI created. Press I in Play mode to open it.", canvas);
    }

    // Keeps the order set on the window already in the scene and appends categories that aren't on it yet.
    private static List<IndexCategoryData> CollectCategories()
    {
        var categories = new List<IndexCategoryData>();

        IndexWindow existing = Object.FindFirstObjectByType<IndexWindow>(FindObjectsInactive.Include);
        if (existing != null)
        {
            SerializedProperty list = new SerializedObject(existing).FindProperty("_categories");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue is IndexCategoryData category && !categories.Contains(category))
                    categories.Add(category);
            }
        }

        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(IndexCategoryData)}"))
        {
            var category = AssetDatabase.LoadAssetAtPath<IndexCategoryData>(AssetDatabase.GUIDToAssetPath(guid));
            if (category != null && !categories.Contains(category))
                categories.Add(category);
        }

        return categories;
    }

    private static GameObject BuildCanvas()
    {
        var canvasObject = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 11;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject;
    }

    private static void BuildWindow(GameObject canvas, List<IndexCategoryData> categories,
        IndexCategoryView categoryPrefab, IndexSlotView slotPrefab)
    {
        Image panel = CreateImage("IndexPanel", canvas.transform, null, DimColor);
        Stretch(panel.rectTransform);

        RectTransform window = CreateImage("Window", panel.transform, _fill, WindowColor).rectTransform;
        window.sizeDelta = WindowSize;

        // Header
        Image header = CreateImage("Header", window, _fill, HeaderColor);
        SetRect(header.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -100f), Vector2.zero);
        TMP_Text title = CreateText("Title", header.transform, "Index", 56f, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        SetRect(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(32f, 0f), new Vector2(-120f, 0f));

        Button closeButton = CreateButton("CloseButton", header.transform, _fill, CloseColor);
        closeButton.image.pixelsPerUnitMultiplier = 0.6f;
        SetRect((RectTransform)closeButton.transform, Vector2.one, Vector2.one, new Vector2(-90f, -86f), new Vector2(-18f, -14f));
        TMP_Text closeText = CreateText("Text", closeButton.transform, "X", 48f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(closeText.rectTransform);

        // Scroll list of categories
        RectTransform list = CreateImage("Categories", window, _fill, DarkPanelColor).rectTransform;
        SetRect(list, Vector2.zero, new Vector2(0f, 1f), new Vector2(20f, 110f), new Vector2(800f, -120f));
        ScrollRect scroll = BuildScrollList(list);

        (RectTransform unlockedFill, TMP_Text unlockedText) = BuildBar("Unlocked", window, BlueColor, "Unlocked: 0/0");
        SetRect((RectTransform)unlockedFill.parent, Vector2.zero, Vector2.zero, new Vector2(20f, 20f), new Vector2(800f, 90f));

        // Selected item
        RectTransform details = CreateImage("Details", window, _fill, DetailsColor).rectTransform;
        SetRect(details, Vector2.one, Vector2.one, new Vector2(-340f, -430f), new Vector2(-20f, -120f));

        Image detailsIcon = CreateImage("Icon", details, null, Color.white);
        detailsIcon.preserveAspect = true;
        detailsIcon.raycastTarget = false;
        SetRect(detailsIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-80f, -180f), new Vector2(80f, -20f));

        TMP_Text detailsName = CreateText("Name", details, "???", 34f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(detailsName.rectTransform, Vector2.zero, Vector2.right, new Vector2(8f, 90f), new Vector2(-8f, 130f));
        TMP_Text detailsLevel = CreateText("Level", details, "Lv. 1", 28f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(detailsLevel.rectTransform, Vector2.zero, Vector2.right, new Vector2(8f, 55f), new Vector2(-8f, 90f));
        TMP_Text detailsPrice = CreateText("Price", details, "$0", 32f, PriceColor, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(detailsPrice.rectTransform, Vector2.zero, Vector2.right, new Vector2(8f, 15f), new Vector2(-8f, 55f));

        // Reward of the selected item's category
        RectTransform rewards = CreateImage("Rewards", window, _fill, RewardsColor).rectTransform;
        SetRect(rewards, Vector2.right, Vector2.right, new Vector2(-340f, 20f), new Vector2(-20f, 250f));
        Image rewardsFrame = CreateImage("Frame", rewards, _frame, RewardsFrameColor);
        rewardsFrame.raycastTarget = false;
        Stretch(rewardsFrame.rectTransform);

        TMP_Text rewardsLabel = CreateText("Label", rewards, "Rewards:", 26f, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        SetRect(rewardsLabel.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(18f, -46f), new Vector2(-18f, -8f));

        TMP_Text moneyText = CreateRewardBox("Money", rewards, "$", PriceColor, new Vector2(-110f, -150f), new Vector2(-10f, -50f));
        TMP_Text speedText = CreateRewardBox("Speed", rewards, "SPD", BlueColor, new Vector2(10f, -150f), new Vector2(110f, -50f));

        Button claimButton = CreateButton("ClaimButton", rewards, _fill, GreenColor);
        SetRect((RectTransform)claimButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 14f), new Vector2(120f, 74f));
        ColorBlock claimColors = claimButton.colors;
        claimColors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        claimButton.colors = claimColors;
        TMP_Text claimText = CreateText("Text", claimButton.transform, "LOCKED", 32f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(claimText.rectTransform);

        // References
        var indexWindow = canvas.AddComponent<IndexWindow>();
        var serialized = new SerializedObject(indexWindow);

        SerializedProperty categoriesProperty = serialized.FindProperty("_categories");
        categoriesProperty.arraySize = categories.Count;
        for (int i = 0; i < categories.Count; i++)
            categoriesProperty.GetArrayElementAtIndex(i).objectReferenceValue = categories[i];

        serialized.FindProperty("_panel").objectReferenceValue = panel.gameObject;
        serialized.FindProperty("_closeButton").objectReferenceValue = closeButton;
        serialized.FindProperty("_categoryPrefab").objectReferenceValue = categoryPrefab;
        serialized.FindProperty("_slotPrefab").objectReferenceValue = slotPrefab;
        serialized.FindProperty("_scroll").objectReferenceValue = scroll;
        serialized.FindProperty("_categoriesContainer").objectReferenceValue = scroll.content;
        serialized.FindProperty("_unlockedFill").objectReferenceValue = unlockedFill;
        serialized.FindProperty("_unlockedText").objectReferenceValue = unlockedText;
        serialized.FindProperty("_detailsIcon").objectReferenceValue = detailsIcon;
        serialized.FindProperty("_detailsName").objectReferenceValue = detailsName;
        serialized.FindProperty("_detailsLevel").objectReferenceValue = detailsLevel;
        serialized.FindProperty("_detailsPrice").objectReferenceValue = detailsPrice;
        serialized.FindProperty("_rewardMoneyText").objectReferenceValue = moneyText;
        serialized.FindProperty("_rewardSpeedText").objectReferenceValue = speedText;
        serialized.FindProperty("_claimButton").objectReferenceValue = claimButton;
        serialized.FindProperty("_claimText").objectReferenceValue = claimText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Vertical scroll view with a thin scrollbar on the right; category rows go into its content.
    private static ScrollRect BuildScrollList(RectTransform parent)
    {
        var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
        var scrollRect = (RectTransform)scrollObject.transform;
        scrollRect.SetParent(parent, false);
        SetRect(scrollRect, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-28f, -12f));

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(scrollRect, false);
        Stretch(viewport);

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image track = CreateImage("Scrollbar", parent, _fill, ScrollbarColor);
        track.pixelsPerUnitMultiplier = 4f;
        SetRect(track.rectTransform, Vector2.right, Vector2.one, new Vector2(-22f, 12f), new Vector2(-10f, -12f));
        var scrollbar = track.gameObject.AddComponent<Scrollbar>();

        var slidingArea = new GameObject("SlidingArea", typeof(RectTransform)).GetComponent<RectTransform>();
        slidingArea.SetParent(track.transform, false);
        Stretch(slidingArea);

        Image handle = CreateImage("Handle", slidingArea, _fill, ScrollHandleColor);
        handle.pixelsPerUnitMultiplier = 4f;
        Stretch(handle.rectTransform);

        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        return scroll;
    }

    // Row: category picture with the title on the left, item grid and progress bar on the right.
    private static IndexCategoryView SaveCategoryPrefab()
    {
        var root = new GameObject("IndexCategory", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        root.layer = LayerMask.NameToLayer("UI");
        var rowLayout = root.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        Image picture = CreateImage("Picture", root.transform, _fill, GreenColor);
        picture.raycastTarget = false;
        var pictureElement = picture.gameObject.AddComponent<LayoutElement>();
        pictureElement.minWidth = PictureWidth;
        pictureElement.preferredWidth = PictureWidth;
        pictureElement.minHeight = SlotSize.y + 70f;

        TMP_Text title = CreateText("Title", picture.transform, "Category", 32f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -60f), new Vector2(-4f, -8f));
        title.enableAutoSizing = true;
        title.fontSizeMin = 16f;
        title.fontSizeMax = 32f;

        var column = new GameObject("Items", typeof(RectTransform), typeof(VerticalLayoutGroup));
        column.layer = LayerMask.NameToLayer("UI");
        column.transform.SetParent(root.transform, false);
        column.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var columnLayout = column.GetComponent<VerticalLayoutGroup>();
        columnLayout.spacing = 10f;
        columnLayout.childControlWidth = true;
        columnLayout.childControlHeight = true;
        columnLayout.childForceExpandWidth = true;
        columnLayout.childForceExpandHeight = false;

        var slots = new GameObject("Slots", typeof(RectTransform), typeof(GridLayoutGroup));
        slots.layer = LayerMask.NameToLayer("UI");
        slots.transform.SetParent(column.transform, false);
        var grid = slots.GetComponent<GridLayoutGroup>();
        grid.cellSize = SlotSize;
        grid.spacing = new Vector2(SlotSpacing, SlotSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;

        (RectTransform progressFill, TMP_Text progressText) = BuildBar("Progress", column.transform, GreenColor, "0/0");
        var barElement = progressFill.parent.gameObject.AddComponent<LayoutElement>();
        barElement.minHeight = 56f;
        barElement.preferredHeight = 56f;

        var view = root.AddComponent<IndexCategoryView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_picture").objectReferenceValue = picture;
        serialized.FindProperty("_title").objectReferenceValue = title;
        serialized.FindProperty("_slotsContainer").objectReferenceValue = slots.transform;
        serialized.FindProperty("_progressFill").objectReferenceValue = progressFill;
        serialized.FindProperty("_progressText").objectReferenceValue = progressText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab<IndexCategoryView>(root, "IndexCategory");
    }

    // Dark bar with a colored fill (its width is set by IndexWindow.SetBar) and a centered label. The caller places fill.parent.
    private static (RectTransform fill, TMP_Text text) BuildBar(string name, Transform parent, Color color, string label)
    {
        Image back = CreateImage(name, parent, _fill, BarBackColor);
        back.raycastTarget = false;

        Image fill = CreateImage("Fill", back.transform, _fill, color);
        fill.raycastTarget = false;
        SetRect(fill.rectTransform, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

        TMP_Text text = CreateText("Text", back.transform, label, 38f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return (fill.rectTransform, text);
    }

    // Square with a colored symbol on top and the reward amount below; returns the amount text.
    private static TMP_Text CreateRewardBox(string name, Transform parent, string symbol, Color symbolColor, Vector2 offsetMin, Vector2 offsetMax)
    {
        Image box = CreateImage(name, parent, _fill, RewardBoxColor);
        box.raycastTarget = false;
        SetRect(box.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), offsetMin, offsetMax);

        TMP_Text icon = CreateText("Symbol", box.transform, symbol, 40f, symbolColor, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(icon.rectTransform, new Vector2(0f, 0.4f), Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text amount = CreateText("Amount", box.transform, "0", 24f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(amount.rectTransform, Vector2.zero, new Vector2(1f, 0.4f), new Vector2(2f, 2f), new Vector2(-2f, 0f));
        amount.enableAutoSizing = true;
        amount.fontSizeMin = 14f;
        amount.fontSizeMax = 24f;
        return amount;
    }

    private static IndexSlotView SaveSlotPrefab()
    {
        Image background = CreateImage("IndexSlot", null, _fill, Color.gray);
        background.rectTransform.sizeDelta = SlotSize;
        var button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        Image icon = CreateImage("Icon", background.transform, null, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetRect(icon.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 12f), new Vector2(-14f, -42f));

        TMP_Text name = CreateText("Name", background.transform, "???", 22f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(name.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(6f, -40f), new Vector2(-6f, -6f));
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.enableAutoSizing = true;
        name.fontSizeMin = 12f;
        name.fontSizeMax = 22f;

        Image selectedFrame = CreateImage("SelectedFrame", background.transform, _frame, Color.white);
        selectedFrame.raycastTarget = false;
        Stretch(selectedFrame.rectTransform);
        selectedFrame.gameObject.SetActive(false);

        var view = background.gameObject.AddComponent<IndexSlotView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_background").objectReferenceValue = background;
        serialized.FindProperty("_icon").objectReferenceValue = icon;
        serialized.FindProperty("_nameText").objectReferenceValue = name;
        serialized.FindProperty("_selectedFrame").objectReferenceValue = selectedFrame.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab<IndexSlotView>(background.gameObject, "IndexSlot");
    }

    private static T SavePrefab<T>(GameObject instance, string name) where T : Component
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabsFolder}/{name}.prefab");
        Object.DestroyImmediate(instance);
        return prefab.GetComponent<T>();
    }
}
