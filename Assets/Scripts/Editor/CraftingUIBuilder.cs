using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Builds the crafting UI (canvas, window, recipe slot and ingredient prefabs) in the open scene.
/// All RecipeData assets of the project are added to the window. Run via Tools > Crafting > Create Crafting UI.
/// Tools > Crafting > Add Storage Tab adds only the Cooking / Storage tabs to the window already in the scene.
/// Tools > Crafting > Add Item Info Panels also splits the right half of both tabs into "how to cook" and sale info.
/// </summary>
public static class CraftingUIBuilder
{
    internal const string SpritesFolder = "Assets/Sprites/UI";
    internal const string PrefabsFolder = "Assets/Prefab/UI";
    internal const string FillSpritePath = SpritesFolder + "/RoundedFill.png";
    internal const string FrameSpritePath = SpritesFolder + "/RoundedFrame.png";
    private const int SpriteSize = 64;
    private const int CornerRadius = 20;
    private const int FrameThickness = 3;
    private const int StorageColumns = 4;
    private static readonly Vector2 StorageCellSize = new Vector2(104f, 124f);
    private static readonly Vector2 WindowSize = new Vector2(1100f, 820f);

    private static readonly Color WindowColor = new Color32(0xFF, 0xF3, 0xE4, 0xFF);
    private static readonly Color PanelColor = Color.white;
    private static readonly Color SlotFrameColor = new Color32(0xEE, 0xDD, 0xCC, 0xFF);
    private static readonly Color SelectedFrameColor = new Color32(0xE0, 0x96, 0x3C, 0xFF);
    private static readonly Color AccentColor = new Color32(0xE8, 0xA2, 0x4C, 0xFF);
    private static readonly Color CloseButtonColor = new Color32(0xF2, 0xDB, 0xC2, 0xFF);
    private static readonly Color TitleColor = new Color32(0x6B, 0x42, 0x26, 0xFF);
    private static readonly Color LabelColor = new Color32(0x8C, 0x6A, 0x50, 0xFF);

    private static Sprite _fill;
    private static Sprite _frame;

    [MenuItem("Tools/Crafting/Create Crafting UI")]
    public static void Create()
    {
        EnsureFolder(SpritesFolder);
        EnsureFolder(PrefabsFolder);
        _fill = GetOrCreateRoundedSprite(FillSpritePath, frameOnly: false);
        _frame = GetOrCreateRoundedSprite(FrameSpritePath, frameOnly: true);

        IngredientView ingredientPrefab = SavePrefab<IngredientView>(BuildIngredient(), "IngredientView");
        RecipeSlotView slotPrefab = SavePrefab<RecipeSlotView>(BuildSlot(), "RecipeSlot");

        GameObject canvas = BuildCanvas();
        PutUnderGameUI(canvas);
        CraftingWindow window = BuildWindow(canvas.transform, slotPrefab, ingredientPrefab);
        BuildStorageTab(window, SaveStorageItemPrefab());
        SplitCraftingDetails(window);
        window.gameObject.SetActive(false);

        EnsureEventSystem();
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, $"{PrefabsFolder}/CraftingCanvas.prefab", InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(canvas, "Create Crafting UI");
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Selection.activeGameObject = window.gameObject;

        Debug.Log("Crafting UI created. The window is inactive until a CraftingStation opens it.", window);
    }

    /// <summary>Adds (or rebuilds) the Cooking / Storage tabs of the CraftingWindow that is already in the open scene.</summary>
    [MenuItem("Tools/Crafting/Add Storage Tab")]
    public static void AddStorageTab()
    {
        if (EditWindowInScene(window => BuildStorageTab(window, SaveStorageItemPrefab()), out CraftingWindow edited))
            Debug.Log("Storage tab added to the crafting window.", edited);
    }

    /// <summary>
    /// Updates the CraftingWindow that is already in the open scene: rebuilds the Storage tab and splits the right half
    /// of both tabs into "how to cook" (top) and sale price / interest (bottom).
    /// </summary>
    [MenuItem("Tools/Crafting/Add Item Info Panels")]
    public static void AddItemInfoPanels()
    {
        bool done = EditWindowInScene(window =>
        {
            BuildStorageTab(window, SaveStorageItemPrefab());
            SplitCraftingDetails(window);
        }, out CraftingWindow edited);

        if (done)
            Debug.Log("Item info panels added to the crafting window.", edited);
    }

    private static bool EditWindowInScene(System.Action<CraftingWindow> build, out CraftingWindow window)
    {
        window = Object.FindFirstObjectByType<CraftingWindow>(FindObjectsInactive.Include);
        if (window == null)
        {
            Debug.LogError("No CraftingWindow in the scene. Use Tools > Crafting > Create Crafting UI first.");
            return false;
        }

        EnsureFolder(SpritesFolder);
        EnsureFolder(PrefabsFolder);
        _fill = GetOrCreateRoundedSprite(FillSpritePath, frameOnly: false);
        _frame = GetOrCreateRoundedSprite(FrameSpritePath, frameOnly: true);

        // Children of a prefab instance can't be reparented, so the prefab asset itself is edited.
        GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(window);
        if (instanceRoot != null)
        {
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                build(contents.GetComponentInChildren<CraftingWindow>(true));
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
        else
        {
            build(window);
        }

        EditorSceneManager.MarkSceneDirty(window.gameObject.scene);
        return true;
    }

    private static StorageItemView SaveStorageItemPrefab()
    {
        return SavePrefab<StorageItemView>(BuildStorageItem(), "StorageItemView");
    }

    // Replaces the window title with "Cooking" / "Storage" tab buttons. The recipe grid and details are moved
    // into CraftingPage, the storage grid is put into StoragePage at the same place; only one page is shown at a time.
    private static void BuildStorageTab(CraftingWindow window, StorageItemView itemPrefab)
    {
        var root = (RectTransform)window.transform;
        root.anchoredPosition = new Vector2(0f, root.anchoredPosition.y);

        // Leftovers of the title and of earlier builds.
        foreach (string name in new[] { "Title", "Tabs", "Storage", "StoragePage" })
        {
            Transform old = root.Find(name);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }

        var craftingPage = (RectTransform)root.Find("CraftingPage");
        if (craftingPage == null)
        {
            craftingPage = new GameObject("CraftingPage", typeof(RectTransform)).GetComponent<RectTransform>();
            craftingPage.SetParent(root, false);
            Stretch(craftingPage);
            craftingPage.SetSiblingIndex(0);
        }

        // The page fills the whole window, so the same anchors and offsets keep the same layout.
        foreach (string name in new[] { "Recipes", "Details" })
        {
            Transform child = root.Find(name);
            if (child != null)
                child.SetParent(craftingPage, false);
        }

        // Tabs
        var tabs = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        tabs.SetParent(root, false);
        SetRect(tabs, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -74f), new Vector2(-100f, -14f));
        var tabsLayout = tabs.GetComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 12f;
        tabsLayout.childAlignment = TextAnchor.MiddleLeft;
        tabsLayout.childControlWidth = true;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = false;
        tabsLayout.childForceExpandHeight = true;

        Button craftingTab = CreateTab("CookingTab", tabs, "Cooking");
        Button storageTab = CreateTab("StorageTab", tabs, "Storage");

        // Storage page
        var storagePage = new GameObject("StoragePage", typeof(RectTransform)).GetComponent<RectTransform>();
        storagePage.SetParent(root, false);
        Stretch(storagePage);
        storagePage.SetSiblingIndex(craftingPage.GetSiblingIndex() + 1);

        // Item grid on the left half, info about the selected item on the right half.
        RectTransform body = CreateImage("Body", storagePage, _fill, PanelColor).rectTransform;
        SetRect(body, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(20f, 20f), new Vector2(-10f, -86f));

        var scrollObject = new GameObject("Items",typeof(RectTransform), typeof(ScrollRect));
        var scrollRect = (RectTransform)scrollObject.transform;
        scrollRect.SetParent(body, false);
        SetRect(scrollRect, Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -16f));

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(scrollRect, false);
        Stretch(viewport);

        var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;

        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = StorageCellSize;
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = StorageColumns;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        TMP_Text emptyText = CreateText("EmptyText", body, "Empty", 28f, LabelColor, FontStyles.Italic, TextAlignmentOptions.Center);
        Stretch(emptyText.rectTransform);

        var serialized = new SerializedObject(window);
        var ingredientPrefab = (IngredientView)serialized.FindProperty("_ingredientPrefab").objectReferenceValue;
        RecipeInfoView recipeInfo = BuildRecipeInfo(storagePage, ingredientPrefab);
        SetRect((RectTransform)recipeInfo.transform, new Vector2(0.5f, 0.5f), Vector2.one, new Vector2(10f, 5f), new Vector2(-20f, -86f));
        ItemSaleInfoView saleInfo = BuildSaleInfo(storagePage);
        SetRect((RectTransform)saleInfo.transform, new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(10f, 20f), new Vector2(-20f, -5f));

        storagePage.gameObject.SetActive(false);

        serialized.FindProperty("_craftingTab").objectReferenceValue = craftingTab;
        serialized.FindProperty("_storageTab").objectReferenceValue = storageTab;
        serialized.FindProperty("_craftingPage").objectReferenceValue = craftingPage.gameObject;
        serialized.FindProperty("_storagePage").objectReferenceValue = storagePage.gameObject;
        serialized.FindProperty("_activeTabColor").colorValue = AccentColor;
        serialized.FindProperty("_inactiveTabColor").colorValue = CloseButtonColor;
        serialized.FindProperty("_activeTabTextColor").colorValue = Color.white;
        serialized.FindProperty("_inactiveTabTextColor").colorValue = TitleColor;
        serialized.FindProperty("_storageItemPrefab").objectReferenceValue = itemPrefab;
        serialized.FindProperty("_storageContainer").objectReferenceValue = content;
        serialized.FindProperty("_storageEmptyLabel").objectReferenceValue = emptyText.gameObject;
        serialized.FindProperty("_storageRecipeInfo").objectReferenceValue = recipeInfo;
        serialized.FindProperty("_storageSaleInfo").objectReferenceValue = saleInfo;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Cooking tab: the recipe details keep the top half of the right side, the sale info of the result takes the bottom half.
    private static void SplitCraftingDetails(CraftingWindow window)
    {
        ((RectTransform)window.transform).sizeDelta = WindowSize;

        var serialized = new SerializedObject(window);
        var craftButton = (Button)serialized.FindProperty("_craftButton").objectReferenceValue;
        var details = (RectTransform)craftButton.transform.parent;
        SetRect(details, new Vector2(0.5f, 0.5f), Vector2.one, new Vector2(10f, 5f), new Vector2(-20f, -86f));

        // Squeezed to fit half of the window height.
        var layout = details.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 14, 12);
        layout.spacing = 8f;
        SetChildHeight(details, "Name", 36f);
        SetChildHeight(details, "ResultIcon", 60f);
        SetChildHeight(details, "IngredientsLabel", 26f);
        SetChildHeight(details, "Ingredients", 90f);
        SetChildHeight(details, "CraftButton", 64f);
        Transform icon = details.Find("ResultIcon/Icon");
        if (icon != null)
            ((RectTransform)icon).sizeDelta = new Vector2(60f, 60f);

        Transform old = details.parent.Find("SaleInfo");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        ItemSaleInfoView saleInfo = BuildSaleInfo(details.parent);
        SetRect((RectTransform)saleInfo.transform, new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(10f, 20f), new Vector2(-20f, -5f));

        serialized.FindProperty("_craftingSaleInfo").objectReferenceValue = saleInfo;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetChildHeight(Transform parent, string name, float height)
    {
        Transform child = parent.Find(name);
        if (child == null)
            return;

        var element = child.GetComponent<LayoutElement>();
        if (element == null)
            element = child.gameObject.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
    }

    private static RecipeInfoView BuildRecipeInfo(Transform parent, IngredientView ingredientPrefab)
    {
        RectTransform panel = CreateImage("RecipeInfo", parent, _fill, PanelColor).rectTransform;

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 14, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text nameText = CreateText("Name", panel, string.Empty, 32f, TitleColor, FontStyles.Bold, TextAlignmentOptions.Center);
        AddLayoutElement(nameText.gameObject, height: 36f);

        var iconHolder = new GameObject("ResultIcon", typeof(RectTransform)).GetComponent<RectTransform>();
        iconHolder.SetParent(panel, false);
        AddLayoutElement(iconHolder.gameObject, height: 60f);
        Image icon = CreateImage("Icon", iconHolder, null, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.rectTransform.sizeDelta = new Vector2(60f, 60f);

        TMP_Text label = CreateText("IngredientsLabel", panel, "Ingredients", 24f, LabelColor, FontStyles.Normal, TextAlignmentOptions.Center);
        AddLayoutElement(label.gameObject, height: 26f);

        var ingredients = new GameObject("Ingredients", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        ingredients.SetParent(panel, false);
        AddLayoutElement(ingredients.gameObject, height: 90f);
        var ingredientsLayout = ingredients.GetComponent<HorizontalLayoutGroup>();
        ingredientsLayout.spacing = 24f;
        ingredientsLayout.childAlignment = TextAnchor.MiddleCenter;
        ingredientsLayout.childControlWidth = false;
        ingredientsLayout.childControlHeight = false;
        ingredientsLayout.childForceExpandWidth = false;
        ingredientsLayout.childForceExpandHeight = false;

        TMP_Text noRecipe = CreateText("NoRecipeText", panel, "Can't be cooked", 24f, LabelColor, FontStyles.Italic, TextAlignmentOptions.Center);
        AddLayoutElement(noRecipe.gameObject, height: 30f);
        noRecipe.gameObject.SetActive(false);

        var view = panel.gameObject.AddComponent<RecipeInfoView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_nameText").objectReferenceValue = nameText;
        serialized.FindProperty("_icon").objectReferenceValue = icon;
        serialized.FindProperty("_ingredientPrefab").objectReferenceValue = ingredientPrefab;
        serialized.FindProperty("_ingredientsContainer").objectReferenceValue = ingredients;
        serialized.FindProperty("_ingredientsLabel").objectReferenceValue = label.gameObject;
        serialized.FindProperty("_noRecipeLabel").objectReferenceValue = noRecipe.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return view;
    }

    private static ItemSaleInfoView BuildSaleInfo(Transform parent)
    {
        RectTransform panel = CreateImage("SaleInfo", parent, _fill, PanelColor).rectTransform;

        TMP_Text title = CreateText("Title", panel, "Sale", 30f, TitleColor, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -56f), new Vector2(-24f, -12f));

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
        content.SetParent(panel, false);
        SetRect(content, Vector2.zero, Vector2.one, new Vector2(28f, 16f), new Vector2(-28f, -64f));
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text price = CreateInfoRow("PriceRow", content, "Sell price");
        TMP_Text interest = CreateInfoRow("InterestRow", content, "Interest");

        Image bar = CreateImage("InterestBar", content, _fill, SlotFrameColor);
        bar.pixelsPerUnitMultiplier = 3f;
        bar.raycastTarget = false;
        AddLayoutElement(bar.gameObject, height: 18f);
        Image fill = CreateImage("Fill", bar.transform, _fill, AccentColor);
        fill.pixelsPerUnitMultiplier = 3f;
        fill.raycastTarget = false;
        SetRect(fill.rectTransform, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

        TMP_Text emptyText = CreateText("EmptyText", panel, "Select an item", 26f, LabelColor, FontStyles.Italic, TextAlignmentOptions.Center);
        SetRect(emptyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 16f), new Vector2(-24f, -64f));
        emptyText.gameObject.SetActive(false);

        var view = panel.gameObject.AddComponent<ItemSaleInfoView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_priceText").objectReferenceValue = price;
        serialized.FindProperty("_interestText").objectReferenceValue = interest;
        serialized.FindProperty("_interestFill").objectReferenceValue = fill.rectTransform;
        serialized.FindProperty("_content").objectReferenceValue = content.gameObject;
        serialized.FindProperty("_emptyLabel").objectReferenceValue = emptyText.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return view;
    }

    // "Label ........ value" row; returns the value text.
    private static TMP_Text CreateInfoRow(string name, Transform parent, string label)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        AddLayoutElement(row, height: 40f);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreateText("Label", row.transform, label, 26f, LabelColor, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        return CreateText("Value", row.transform, "0", 28f, TitleColor, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
    }

    private static Button CreateTab(string name, Transform parent, string text)
    {
        Button tab = CreateButton(name, parent, _fill, CloseButtonColor);
        tab.image.pixelsPerUnitMultiplier = 0.6f;
        tab.gameObject.AddComponent<LayoutElement>().preferredWidth = 200f;

        TMP_Text label = CreateText("Text", tab.transform, text, 30f, TitleColor, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return tab;
    }

    private static GameObject BuildStorageItem()
    {
        Image background = CreateImage("StorageItemView", null, _fill, Color.white);
        background.rectTransform.sizeDelta = StorageCellSize;
        var button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        Image border = CreateImage("Border", background.transform, _frame, SlotFrameColor);
        border.raycastTarget = false;
        Stretch(border.rectTransform);

        Image selectedFrame = CreateImage("SelectedFrame", background.transform, _frame, SelectedFrameColor);
        selectedFrame.raycastTarget = false;
        Stretch(selectedFrame.rectTransform);
        selectedFrame.gameObject.SetActive(false);

        Image icon = CreateImage("Icon", background.transform, null, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-28f, -66f), new Vector2(28f, -10f));

        TMP_Text name = CreateText("Name", background.transform, "Item", 16f, LabelColor, FontStyles.Normal, TextAlignmentOptions.Center);
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
        SetRect(name.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(6f, 28f), new Vector2(-6f, 50f));

        TMP_Text count = CreateText("Count", background.transform, "x1", 22f, TitleColor, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(count.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 4f), new Vector2(0f, 30f));

        var view = background.gameObject.AddComponent<StorageItemView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_icon").objectReferenceValue = icon;
        serialized.FindProperty("_nameText").objectReferenceValue = name;
        serialized.FindProperty("_countText").objectReferenceValue = count;
        serialized.FindProperty("_selectedFrame").objectReferenceValue = selectedFrame.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return background.gameObject;
    }

    private static GameObject BuildCanvas()
    {
        var canvasObject = new GameObject("CraftingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject;
    }

    private static CraftingWindow BuildWindow(Transform parent, RecipeSlotView slotPrefab, IngredientView ingredientPrefab)
    {
        RectTransform root = CreateImage("CraftingWindow", parent, _fill, WindowColor).rectTransform;
        root.sizeDelta = WindowSize;
        var window = root.gameObject.AddComponent<CraftingWindow>();

        // Header
        TMP_Text title = CreateText("Title", root, "Cooking", 34f, TitleColor, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -80f), new Vector2(-100f, -10f));

        Button closeButton = CreateButton("CloseButton", root, _fill, CloseButtonColor);
        closeButton.image.pixelsPerUnitMultiplier = 0.45f;
        SetRect((RectTransform)closeButton.transform, Vector2.one, Vector2.one, new Vector2(-76f, -70f), new Vector2(-20f, -14f));
        TMP_Text closeText = CreateText("Text", closeButton.transform, "X", 28f, TitleColor, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(closeText.rectTransform);

        // Recipe grid (left)
        var scrollObject = new GameObject("Recipes", typeof(RectTransform), typeof(ScrollRect));
        var scrollRect = (RectTransform)scrollObject.transform;
        scrollRect.SetParent(root, false);
        SetRect(scrollRect, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(20f, 20f), new Vector2(-10f, -86f));

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(scrollRect, false);
        Stretch(viewport);

        var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;

        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(254f, 112f);
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        // Details (right)
        RectTransform details = CreateImage("Details", root, _fill, PanelColor).rectTransform;
        SetRect(details, new Vector2(0.5f, 0f), Vector2.one, new Vector2(10f, 20f), new Vector2(-20f, -86f));

        var layout = details.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 24, 20);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text nameText = CreateText("Name", details, "Recipe", 32f, TitleColor, FontStyles.Bold, TextAlignmentOptions.Center);
        AddLayoutElement(nameText.gameObject, height: 40f);

        var iconHolder = new GameObject("ResultIcon", typeof(RectTransform)).GetComponent<RectTransform>();
        iconHolder.SetParent(details, false);
        AddLayoutElement(iconHolder.gameObject, height: 72f);
        Image resultIcon = CreateImage("Icon", iconHolder, null, Color.white);
        resultIcon.preserveAspect = true;
        resultIcon.raycastTarget = false;
        resultIcon.rectTransform.sizeDelta = new Vector2(72f, 72f);

        TMP_Text label = CreateText("IngredientsLabel", details, "Ingredients", 24f, LabelColor, FontStyles.Normal, TextAlignmentOptions.Center);
        AddLayoutElement(label.gameObject, height: 30f);

        var ingredients = new GameObject("Ingredients", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        ingredients.SetParent(details, false);
        AddLayoutElement(ingredients.gameObject, height: 90f);
        var ingredientsLayout = ingredients.GetComponent<HorizontalLayoutGroup>();
        ingredientsLayout.spacing = 24f;
        ingredientsLayout.childAlignment = TextAnchor.MiddleCenter;
        ingredientsLayout.childControlWidth = false;
        ingredientsLayout.childControlHeight = false;
        ingredientsLayout.childForceExpandWidth = false;
        ingredientsLayout.childForceExpandHeight = false;

        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(details, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;

        Button craftButton = CreateButton("CraftButton", details, _fill, AccentColor);
        AddLayoutElement(craftButton.gameObject, height: 76f);
        ColorBlock colors = craftButton.colors;
        colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
        craftButton.colors = colors;
        TMP_Text craftText = CreateText("Text", craftButton.transform, "Cook", 32f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(craftText.rectTransform);

        // References
        var serialized = new SerializedObject(window);
        SerializedProperty recipes = serialized.FindProperty("_recipes");
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(RecipeData)}");
        recipes.arraySize = guids.Length;
        for (int i = 0; i < guids.Length; i++)
            recipes.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecipeData>(AssetDatabase.GUIDToAssetPath(guids[i]));

        serialized.FindProperty("_slotPrefab").objectReferenceValue = slotPrefab;
        serialized.FindProperty("_slotsContainer").objectReferenceValue = content;
        serialized.FindProperty("_nameText").objectReferenceValue = nameText;
        serialized.FindProperty("_resultIcon").objectReferenceValue = resultIcon;
        serialized.FindProperty("_ingredientPrefab").objectReferenceValue = ingredientPrefab;
        serialized.FindProperty("_ingredientsContainer").objectReferenceValue = ingredients;
        serialized.FindProperty("_craftButton").objectReferenceValue = craftButton;
        serialized.FindProperty("_closeButton").objectReferenceValue = closeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return window;
    }

    private static GameObject BuildSlot()
    {
        Image background = CreateImage("RecipeSlot", null, _fill, Color.white);
        background.rectTransform.sizeDelta = new Vector2(254f, 112f);
        var button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        Image border = CreateImage("Border", background.transform, _frame, SlotFrameColor);
        border.raycastTarget = false;
        Stretch(border.rectTransform);

        Image selectedFrame = CreateImage("SelectedFrame", background.transform, _frame, SelectedFrameColor);
        selectedFrame.raycastTarget = false;
        Stretch(selectedFrame.rectTransform);
        selectedFrame.gameObject.SetActive(false);

        Image icon = CreateImage("Icon", background.transform, null, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.rectTransform.sizeDelta = new Vector2(64f, 64f);

        var slot = background.gameObject.AddComponent<RecipeSlotView>();
        var serialized = new SerializedObject(slot);
        serialized.FindProperty("_background").objectReferenceValue = background;
        serialized.FindProperty("_icon").objectReferenceValue = icon;
        serialized.FindProperty("_selectedFrame").objectReferenceValue = selectedFrame.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return background.gameObject;
    }

    private static GameObject BuildIngredient()
    {
        var root = new GameObject("IngredientView", typeof(RectTransform)).GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(70f, 90f);

        Image icon = CreateImage("Icon", root, null, Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-24f, -52f), new Vector2(24f, -4f));

        TMP_Text count = CreateText("Count", root, "0/1", 22f, LabelColor, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRect(count.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 32f));

        var view = root.gameObject.AddComponent<IngredientView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_icon").objectReferenceValue = icon;
        serialized.FindProperty("_countText").objectReferenceValue = count;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root.gameObject;
    }

    private static T SavePrefab<T>(GameObject instance, string name) where T : Component
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabsFolder}/{name}.prefab");
        Object.DestroyImmediate(instance);
        return prefab.GetComponent<T>();
    }

    /// <summary>Puts a new canvas under the scene's <see cref="GameUI"/> root, if there is one.</summary>
    internal static void PutUnderGameUI(GameObject canvas)
    {
        GameUI root = Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (root != null)
            canvas.transform.SetParent(root.transform, false);
    }

    internal static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    internal static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null)
            go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        if (sprite != null)
            image.type = Image.Type.Sliced;

        return image;
    }

    internal static Button CreateButton(string name, Transform parent, Sprite sprite, Color color)
    {
        Image image = CreateImage(name, parent, sprite, color);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    internal static TMP_Text CreateText(string name, Transform parent, string text, float size, Color color,
        FontStyles style, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void AddLayoutElement(GameObject go, float height)
    {
        var element = go.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
    }

    internal static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    internal static void Stretch(RectTransform rect)
    {
        SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    internal static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // White rounded rectangle (or only its outline), 9-sliced so it stretches to any size.
    internal static Sprite GetOrCreateRoundedSprite(string path, bool frameOnly)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
            return existing;

        var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                float distance = DistanceInside(x + 0.5f, y + 0.5f);
                float alpha = Mathf.Clamp01(distance + 0.5f);
                if (frameOnly)
                    alpha *= Mathf.Clamp01(FrameThickness - distance + 0.5f);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spriteBorder = new Vector4(CornerRadius + 2, CornerRadius + 2, CornerRadius + 2, CornerRadius + 2);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Distance from the point to the rounded-rect edge, positive inside.
    private static float DistanceInside(float x, float y)
    {
        float cx = Mathf.Clamp(x, CornerRadius, SpriteSize - CornerRadius);
        float cy = Mathf.Clamp(y, CornerRadius, SpriteSize - CornerRadius);
        float edge = Mathf.Min(Mathf.Min(x, SpriteSize - x), Mathf.Min(y, SpriteSize - y));

        bool inCorner = !Mathf.Approximately(cx, x) && !Mathf.Approximately(cy, y);
        return inCorner ? CornerRadius - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) : edge;
    }
}
