using UnityEngine;


[CreateAssetMenu(menuName = "CharacterSelect/WeaponData", fileName = "NewWeapon")]
public class WeaponData : ScriptableObject
{
    [Header("identity")]
    public string weaponName = "weapon lol";
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;

    [Header("prefab")]
    [Tooltip("the weapon GameObject that will be instantiated and parented to the weapon socket")]
    public GameObject weaponPrefab;
}
