using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryCarrier : MonoBehaviour
{
    [Header("carry point")]
    public Transform carryPoint;
    public Vector3 carryOffset = new Vector3(0.5f, -0.3f, 0.5f);
    public Vector3 carryRotation = Vector3.zero;

    [Header("wobble")]
    public bool enableWobble = true;
    public float wobbleSpeed = 2f;
    public float wobbleAmount = 0.05f;

    [Header("drop")]
    public string dropActionName = "Drop";
    public Vector3 dropOffset = new Vector3(0f, 0.5f, 1f);
    public bool addDropForce = true;
    public float dropForce = 3f;

    [Header("fx")]
    public GameObject swapVFXPrefab;
    public float vfxLifetime = 1f;

    GameObject _heldVisual;
    float _wobbleTime;
    InputAction _dropAction;

    readonly Dictionary<InventoryItemData, List<WorldItem>> _itemSourceMap
        = new Dictionary<InventoryItemData, List<WorldItem>>();

    Transform ResolvedCarryPoint
    {
        get
        {
            if (carryPoint != null) return carryPoint;
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) carryPoint = cam.transform;
            return carryPoint;
        }
    }

    void Start()
    {
        CacheDropAction();
    }

    void CacheDropAction()
    {
        if (InputManager.Instance != null)
            _dropAction = InputManager.Instance.FindAction(dropActionName);
    }

    void Update()
    {
        if (_dropAction == null) CacheDropAction();
        if (_dropAction != null && _dropAction.WasPressedThisFrame())
            InventoryManager.Instance?.DropActiveItem();
        ApplyWobble();
    }

    void ApplyWobble()
    {
        if (_heldVisual == null || !enableWobble) return;
        _wobbleTime += Time.deltaTime * wobbleSpeed;
        Vector3 pos = carryOffset;
        pos.y += Mathf.Sin(_wobbleTime) * wobbleAmount;
        _heldVisual.transform.localPosition = pos;
    }

    public void ClearSourceMap()
    {
        _itemSourceMap.Clear();
    }

    public void RegisterPickedUpItem(InventoryItemData data, WorldItem source)
    {
        if (data == null || source == null) return;
        if (!_itemSourceMap.ContainsKey(data))
            _itemSourceMap[data] = new List<WorldItem>();
        if (!_itemSourceMap[data].Contains(source))
            _itemSourceMap[data].Add(source);
    }

    public void ShowItem(GameObject itemPrefab)
    {
        ClearHeldItem();
        Transform point = ResolvedCarryPoint;
        if (itemPrefab == null || point == null) return;

        _heldVisual = Instantiate(itemPrefab);
        _heldVisual.transform.SetParent(point, false);
        _heldVisual.transform.localPosition = carryOffset;
        _heldVisual.transform.localRotation = Quaternion.Euler(carryRotation);

        foreach (var rb in _heldVisual.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        foreach (var col in _heldVisual.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var wi in _heldVisual.GetComponentsInChildren<WorldItem>())
            Destroy(wi);
        foreach (var script in _heldVisual.GetComponentsInChildren<MonoBehaviour>())
            if (script != null) script.enabled = false;

        _wobbleTime = 0f;
        SpawnSwapVFX();
    }

    public void ClearHeldItem()
    {
        if (_heldVisual != null)
        {
            Destroy(_heldVisual);
            _heldVisual = null;
        }
    }

    public void DropHeldItem(InventoryItemData itemData, bool isLastInStack = true)
    {
        Transform point = ResolvedCarryPoint;
        if (itemData == null || point == null) return;

        Vector3 dropPos = point.position + point.TransformDirection(dropOffset);
        Vector3 force = addDropForce ? point.forward * dropForce : Vector3.zero;

        if (_itemSourceMap.TryGetValue(itemData, out List<WorldItem> list) && list.Count > 0)
        {
            WorldItem target = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            if (list.Count == 0) _itemSourceMap.Remove(itemData);
            if (target != null)
                target.DropTo(dropPos, force);
        }

        if (isLastInStack) ClearHeldItem();
    }

    void SpawnSwapVFX()
    {
        Transform point = ResolvedCarryPoint;
        if (swapVFXPrefab == null || point == null) return;
        GameObject vfx = Instantiate(swapVFXPrefab, point.position, point.rotation);
        Destroy(vfx, vfxLifetime);
    }
}