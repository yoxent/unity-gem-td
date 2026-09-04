using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct CharacterWeaponSlot
{
    [Tooltip("Socket the weapon is parented to.")]
    public Transform socket;
    public GameObject weaponPrefab;
    [Tooltip("Local position of the weapon on the socket.")]
    public Vector3 position;
    [Tooltip("Local euler angles of the weapon on the socket.")]
    public Vector3 rotation;
}

/// <summary>
/// Edit-mode helper: for each slot, instantiates <see cref="CharacterWeaponSlot.weaponPrefab"/>
/// onto <see cref="CharacterWeaponSlot.socket"/> and applies local position/rotation.
/// </summary>
[ExecuteAlways]
public sealed class CharacterWeaponAssigner : MonoBehaviour
{
    [SerializeField] CharacterWeaponSlot[] slots;

    [SerializeField, HideInInspector] GameObject[] spawnedWeapons;
    [SerializeField, HideInInspector] GameObject[] spawnedFromPrefabs;

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall -= ApplyIfAlive;
        EditorApplication.delayCall += ApplyIfAlive;
#else
        Apply();
#endif
    }

#if UNITY_EDITOR
    void ApplyIfAlive()
    {
        if (this == null)
            return;
        Apply();
    }
#endif

    void Apply()
    {
#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(this) || !gameObject.scene.IsValid())
            return;
#endif
        if (slots == null)
            slots = Array.Empty<CharacterWeaponSlot>();

        TrimOrphanedSpawns(slots.Length);
        EnsureSpawnArrays(slots.Length);

        for (var i = 0; i < slots.Length; i++)
            ApplySlot(i, slots[i]);
    }

    void ApplySlot(int index, CharacterWeaponSlot slot)
    {
        if (slot.socket == null || slot.weaponPrefab == null)
        {
            ClearSpawnedAt(index);
            return;
        }

        var spawned = spawnedWeapons[index];
        if (spawned == null)
            spawned = FindExistingInstance(slot.socket, slot.weaponPrefab);

        var prefabMatches = spawned != null && IsInstanceOfPrefab(spawned, slot.weaponPrefab);
        if (spawned == null || !prefabMatches)
        {
            ClearSpawnedAt(index);
            spawned = InstantiateWeapon(slot.weaponPrefab, slot.socket);
        }
        else if (spawned.transform.parent != slot.socket)
        {
            spawned.transform.SetParent(slot.socket, false);
        }

        spawnedWeapons[index] = spawned;
        spawnedFromPrefabs[index] = slot.weaponPrefab;

        if (spawned == null)
            return;

        var t = spawned.transform;
        t.localPosition = slot.position;
        t.localRotation = Quaternion.Euler(slot.rotation);
        t.localScale = Vector3.one;
    }

    void EnsureSpawnArrays(int length)
    {
        if (spawnedWeapons == null || spawnedWeapons.Length != length)
        {
            var next = new GameObject[length];
            if (spawnedWeapons != null)
            {
                var copy = spawnedWeapons.Length < length ? spawnedWeapons.Length : length;
                for (var i = 0; i < copy; i++)
                    next[i] = spawnedWeapons[i];
            }
            spawnedWeapons = next;
        }

        if (spawnedFromPrefabs == null || spawnedFromPrefabs.Length != length)
        {
            var next = new GameObject[length];
            if (spawnedFromPrefabs != null)
            {
                var copy = spawnedFromPrefabs.Length < length ? spawnedFromPrefabs.Length : length;
                for (var i = 0; i < copy; i++)
                    next[i] = spawnedFromPrefabs[i];
            }
            spawnedFromPrefabs = next;
        }
    }

    void TrimOrphanedSpawns(int keepCount)
    {
        if (spawnedWeapons == null)
            return;

        for (var i = keepCount; i < spawnedWeapons.Length; i++)
            ClearSpawnedAt(i);
    }

    GameObject FindExistingInstance(Transform socket, GameObject prefab)
    {
        if (socket == null || prefab == null)
            return null;

        for (var i = 0; i < socket.childCount; i++)
        {
            var child = socket.GetChild(i).gameObject;
            if (IsInstanceOfPrefab(child, prefab))
                return child;
        }

        return null;
    }

    static bool IsInstanceOfPrefab(GameObject instance, GameObject prefab)
    {
        if (instance == null || prefab == null)
            return false;
#if UNITY_EDITOR
        var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
        if (source == prefab)
            return true;
#endif
        return instance.name == prefab.name || instance.name == prefab.name + "(Clone)";
    }

    static GameObject InstantiateWeapon(GameObject prefab, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance != null)
                return instance;
        }
#endif
        return Instantiate(prefab, parent);
    }

    void ClearSpawnedAt(int index)
    {
        if (spawnedWeapons == null || index < 0 || index >= spawnedWeapons.Length)
            return;

        var spawned = spawnedWeapons[index];
        if (spawned != null)
        {
#if UNITY_EDITOR
            if (!PrefabUtility.IsPartOfPrefabAsset(spawned))
            {
                if (!Application.isPlaying)
                    DestroyImmediate(spawned);
                else
                    Destroy(spawned);
            }
#else
            Destroy(spawned);
#endif
        }

        spawnedWeapons[index] = null;
        if (spawnedFromPrefabs != null && index < spawnedFromPrefabs.Length)
            spawnedFromPrefabs[index] = null;
    }
}
