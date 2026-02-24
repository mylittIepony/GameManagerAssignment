using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public InventoryItemData itemData;
    public int quantity;

    public bool IsEmpty => itemData == null || quantity <= 0;

    public InventorySlot() { }

    public InventorySlot(InventoryItemData data, int qty)
    {
        itemData = data;
        quantity = qty;
    }

    public void Clear()
    {
        itemData = null;
        quantity = 0;
    }

    public bool CanStack(InventoryItemData data)
    {
        if (IsEmpty) return true;
        if (!itemData.isStackable) return false;
        return itemData == data && quantity < itemData.maxStackSize;
    }

    public int AddStack(int amount)
    {
        if (!itemData.isStackable) return amount;
        int spaceLeft = itemData.maxStackSize - quantity;
        int toAdd = Mathf.Min(amount, spaceLeft);
        quantity += toAdd;
        return amount - toAdd;
    }
}