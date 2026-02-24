using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitch : MonoBehaviour
{
    [Header("weapons")]
    public GameObject[] weapons;

    [Header("switching")]
    public float switchDelay = 0.2f;

    [Header("fx")]
    public GameObject[] switchFXPrefabs;
    public Transform fxPoint;
    public float switchFXDestroyTime = 1f;

    public int CurrentIndex { get; private set; }
    public GameObject CurrentWeapon => (weapons != null && CurrentIndex < weapons.Length) ? weapons[CurrentIndex] : null;
    public event Action<int> OnWeaponChanged;

    float _nextSwitchTime;

    void OnEnable()
    {
        if (weapons != null && weapons.Length > 0)
        {
            foreach (GameObject w in weapons)
                if (w != null) w.SetActive(false);
            SelectWeapon(CurrentIndex);
        }
    }

    void Update()
    {
        if (PauseManager.IsPaused) return;
        float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
        if (scroll > 0f) TrySwitchRelative(1);
        else if (scroll < 0f) TrySwitchRelative(-1);
    }

    void TrySwitchRelative(int dir)
    {
        if (weapons == null || weapons.Length == 0) return;
        if (Time.time < _nextSwitchTime) return;
        SelectWeapon((CurrentIndex + dir + weapons.Length) % weapons.Length);
    }

    public void SelectWeapon(int index)
    {
        if (weapons == null || weapons.Length == 0) return;
        index = Mathf.Clamp(index, 0, weapons.Length - 1);

        for (int i = 0; i < weapons.Length; i++)
            if (weapons[i] != null) weapons[i].SetActive(i == index);

        CurrentIndex = index;
        _nextSwitchTime = Time.time + switchDelay;
        OnWeaponChanged?.Invoke(index);
        SpawnSwitchFX();
    }

    public void EquipLoadout(GameObject[] newWeapons, int startIndex = 0)
    {
        if (weapons != null)
            foreach (GameObject w in weapons)
                if (w != null) w.SetActive(false);

        weapons = newWeapons;
        SelectWeapon(startIndex);
    }

    void SpawnSwitchFX()
    {
        if (switchFXPrefabs == null) return;
        Transform point = fxPoint != null ? fxPoint : transform;
        foreach (GameObject fx in switchFXPrefabs)
        {
            if (fx == null) continue;
            GameObject spawned = Instantiate(fx, point.position, point.rotation);
            Destroy(spawned, switchFXDestroyTime);
        }
    }
}