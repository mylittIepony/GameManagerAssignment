using UnityEngine;


[CreateAssetMenu(menuName = "CharacterSelect/AccessoryData", fileName = "NewAccessory")]
public class AccessoryData : ScriptableObject
{
    [Header("identity")]
    public string accessoryName = "accessory";
    public Sprite icon;

    [Header("slot")]
    [Tooltip("slot name must match a socket name on PlayerCustomization (like 'Hat', 'Back', 'Face')")]
    public string slotName = "Hat";

    [Header("prefab")]
    [Tooltip("prefab that will be instantiated at the matching socket transform")]
    public GameObject prefab;
}
