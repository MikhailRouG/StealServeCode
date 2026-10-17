using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds the speed stat text to the HUD: a copy of the experience text named "Speed", placed under it
/// and assigned to <see cref="PlayerUI"/>. Run via Tools > UI > Add Speed Text.
/// </summary>
public static class PlayerUIBuilder
{
    private const float Spacing = 8f;

    [MenuItem("Tools/UI/Add Speed Text")]
    public static void AddSpeedText()
    {
        PlayerUI playerUI = Object.FindFirstObjectByType<PlayerUI>(FindObjectsInactive.Include);
        if (playerUI == null)
        {
            Debug.LogError("No PlayerUI in the scene.");
            return;
        }

        var serialized = new SerializedObject(playerUI);
        SerializedProperty speedText = serialized.FindProperty("_speedText");
        if (speedText.objectReferenceValue != null)
        {
            Debug.Log("PlayerUI already has a speed text.", speedText.objectReferenceValue);
            return;
        }

        TMP_Text template = FindTemplate(serialized);
        if (template == null)
        {
            Debug.LogError("PlayerUI has no experience, level or money text to copy.", playerUI);
            return;
        }

        GameObject copy = Object.Instantiate(template.gameObject, template.transform.parent);
        copy.name = "Speed";
        copy.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);

        // The money text carries PlayerUI itself; the copy must not.
        foreach (PlayerUI extra in copy.GetComponents<PlayerUI>())
            Object.DestroyImmediate(extra);

        var rect = (RectTransform)copy.transform;
        var templateRect = (RectTransform)template.transform;
        rect.anchoredPosition = templateRect.anchoredPosition - new Vector2(0f, templateRect.rect.height + Spacing);

        var text = copy.GetComponent<TMP_Text>();
        text.text = "100";

        Undo.RegisterCreatedObjectUndo(copy, "Add Speed Text");
        Undo.RecordObject(playerUI, "Add Speed Text");
        speedText.objectReferenceValue = text;
        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(copy.scene);
        Selection.activeGameObject = copy;
        Debug.Log("Speed text added under " + template.transform.parent.name + ".", copy);
    }

    // The lowest of the HUD texts, so the speed goes under the experience.
    private static TMP_Text FindTemplate(SerializedObject playerUI)
    {
        foreach (string property in new[] { "_experienceText", "_levelText", "_moneyText" })
        {
            if (playerUI.FindProperty(property).objectReferenceValue is TMP_Text text)
                return text;
        }

        return null;
    }
}
