using UnityEngine;

[CreateAssetMenu(menuName = "CharacterSelect/CharacterData", fileName = "NewCharacter")]
public class CharacterData : ScriptableObject
{
    [Header("identity")]
    public string characterName = "character";
    public Sprite portrait;
    [TextArea(2, 4)]
    public string description;

    [Header("prefab")]
    public GameObject characterPrefab;

    [Header("customization")]
    public bool useCustomization = false;

    [System.Serializable]
    public class CharacterAccessorySlot
    {
        public string slotName = "Hat";
        public AccessoryData[] options;
    }
    public CharacterAccessorySlot[] accessorySlots;

    [Header("weapon loadout")]
    public bool useCharacterWeapons = false;
    public WeaponData[] characterWeapons;
}