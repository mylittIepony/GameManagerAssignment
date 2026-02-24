using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class InventoryItemData : ScriptableObject
{
    [Header("info")]
    public string itemName = "new item";
    [TextArea(2, 4)]
    public string itemType = "misc";
    [TextArea(3, 6)]
    public string description = "item description here";

    [Header("visuals")]
    public Sprite inventoryIcon;
    public GameObject worldPrefab;

    [Header("settings")]
    public bool isStackable = false;
    public int maxStackSize = 1;


}