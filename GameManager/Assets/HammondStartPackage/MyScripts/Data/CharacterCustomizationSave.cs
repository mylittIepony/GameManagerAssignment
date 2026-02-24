using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CharacterSelect/CustomizationSave", fileName = "CustomizationSave")]
public class CharacterCustomizationSave : ScriptableObject
{
    [Header("runtime — do not edit by hand")]
    public int selectedCharacterIndex = 0;
    public List<int> selectedWeaponIndices = new List<int>();

    [System.Serializable]
    public class AccessoryChoice
    {
        public string slotName;
        public int accessoryIndex;
    }
    public List<AccessoryChoice> accessories = new List<AccessoryChoice>();

    [System.Serializable]
    public class ColourChoice
    {
        public string rendererPath;
        public int materialIndex;
        public Color colour = Color.white;
    }
    public List<ColourChoice> colours = new List<ColourChoice>();

    const string KEY_CHAR = "CharSelect/CharIdx";
    const string KEY_WEAPON_COUNT = "CharSelect/WeaponCount";
    const string KEY_WEAPON = "CharSelect/Weapon_";
    const string KEY_ACC_COUNT = "CharSelect/AccCount";
    const string KEY_COL_COUNT = "CharSelect/ColCount";

    public void SaveToSaveManager()
    {
        SaveManager.SetInt(KEY_CHAR, selectedCharacterIndex);

        SaveManager.SetInt(KEY_WEAPON_COUNT, selectedWeaponIndices.Count);
        for (int i = 0; i < selectedWeaponIndices.Count; i++)
            SaveManager.SetInt($"{KEY_WEAPON}{i}", selectedWeaponIndices[i]);

        SaveManager.SetInt(KEY_ACC_COUNT, accessories.Count);
        for (int i = 0; i < accessories.Count; i++)
        {
            SaveManager.Set($"CharSelect/Acc{i}_slot", accessories[i].slotName);
            SaveManager.SetInt($"CharSelect/Acc{i}_idx", accessories[i].accessoryIndex);
        }

        SaveManager.SetInt(KEY_COL_COUNT, colours.Count);
        for (int i = 0; i < colours.Count; i++)
        {
            SaveManager.Set($"CharSelect/Col{i}_path", colours[i].rendererPath);
            SaveManager.SetInt($"CharSelect/Col{i}_mat", colours[i].materialIndex);
            SaveManager.SetFloat($"CharSelect/Col{i}_r", colours[i].colour.r);
            SaveManager.SetFloat($"CharSelect/Col{i}_g", colours[i].colour.g);
            SaveManager.SetFloat($"CharSelect/Col{i}_b", colours[i].colour.b);
            SaveManager.SetFloat($"CharSelect/Col{i}_a", colours[i].colour.a);
        }

        SaveManager.ForceSave();
    }

    public void LoadFromSaveManager()
    {
        selectedCharacterIndex = SaveManager.GetInt(KEY_CHAR, 0);

        selectedWeaponIndices.Clear();
        int weaponCount = SaveManager.GetInt(KEY_WEAPON_COUNT, 0);
        for (int i = 0; i < weaponCount; i++)
            selectedWeaponIndices.Add(SaveManager.GetInt($"{KEY_WEAPON}{i}", 0));

        accessories.Clear();
        int accCount = SaveManager.GetInt(KEY_ACC_COUNT, 0);
        for (int i = 0; i < accCount; i++)
            accessories.Add(new AccessoryChoice
            {
                slotName = SaveManager.Get($"CharSelect/Acc{i}_slot", ""),
                accessoryIndex = SaveManager.GetInt($"CharSelect/Acc{i}_idx", 0)
            });

        colours.Clear();
        int colCount = SaveManager.GetInt(KEY_COL_COUNT, 0);
        for (int i = 0; i < colCount; i++)
            colours.Add(new ColourChoice
            {
                rendererPath = SaveManager.Get($"CharSelect/Col{i}_path", ""),
                materialIndex = SaveManager.GetInt($"CharSelect/Col{i}_mat", 0),
                colour = new Color(
                    SaveManager.GetFloat($"CharSelect/Col{i}_r", 1f),
                    SaveManager.GetFloat($"CharSelect/Col{i}_g", 1f),
                    SaveManager.GetFloat($"CharSelect/Col{i}_b", 1f),
                    SaveManager.GetFloat($"CharSelect/Col{i}_a", 1f))
            });
    }
}