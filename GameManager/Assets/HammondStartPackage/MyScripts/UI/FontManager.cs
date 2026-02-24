using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class FontManager : MonoBehaviour
{
    [Header("fonts")]
    public TMP_FontAsset headingFont;
    public TMP_FontAsset bodyFont;
    public TMP_FontAsset accentFont;
    public float headingThreshold = 72f;
    public float accentThreshold = 24f;
    public bool replaceDefaultMaterial = true;

    [Header("behaviour")]
    public bool applyOnAwake = true;
    public bool includeInactive = true;

    void Awake()
    {
        if (applyOnAwake)
            ApplyFonts();
    }


    public void ApplyFonts()
    {
        TMP_Text[] allText = GetComponentsInChildren<TMP_Text>(includeInactive);

        int replaced = 0;

        foreach (TMP_Text text in allText)
        {
            FontRole role = DetermineRole(text);

            if (role == FontRole.None)
                continue;

            TMP_FontAsset targetFont = GetFontForRole(role);
            if (targetFont == null) continue;

            if (text.font == targetFont) continue;

            text.font = targetFont;

            if (replaceDefaultMaterial)
                text.fontSharedMaterial = targetFont.material;

            replaced++;
        }

        Debug.Log($"applied fonts to {replaced} text components under '{gameObject.name}'.");
    }


    public List<FontPreviewEntry> PreviewChanges()
    {
        List<FontPreviewEntry> preview = new List<FontPreviewEntry>();
        TMP_Text[] allText = GetComponentsInChildren<TMP_Text>(includeInactive);

        foreach (TMP_Text text in allText)
        {
            FontRole role = DetermineRole(text);
            if (role == FontRole.None) continue;

            TMP_FontAsset targetFont = GetFontForRole(role);
            if (targetFont == null || text.font == targetFont) continue;

            preview.Add(new FontPreviewEntry
            {
                textComponent = text,
                currentFont = text.font,
                targetFont = targetFont,
                role = role
            });
        }

        return preview;
    }

    FontRole DetermineRole(TMP_Text text)
    {

        FontTag tag = text.GetComponent<FontTag>();
        if (tag != null)
            return tag.role;

        float size = text.fontSize;

        if (size >= headingThreshold)
            return FontRole.Heading;

        if (accentFont != null && size >= accentThreshold)
            return FontRole.Accent;

        return FontRole.Body;
    }

    TMP_FontAsset GetFontForRole(FontRole role)
    {
        return role switch
        {
            FontRole.Heading => headingFont,
            FontRole.Body => bodyFont,
            FontRole.Accent => accentFont != null ? accentFont : bodyFont,
            _ => null
        };
    }

    public struct FontPreviewEntry
    {
        public TMP_Text textComponent;
        public TMP_FontAsset currentFont;
        public TMP_FontAsset targetFont;
        public FontRole role;
    }
}

public enum FontRole
{
    None,      
    Heading,    
    Body,      
    Accent   
}