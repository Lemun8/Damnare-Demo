using UnityEngine;

public enum ItemTargetType
{
    Self,
    Ally,
    Enemy
}

public enum ItemUseType
{
    CombatOnly,
    OverworldOnly,
    Both
}

public enum ItemAnimationType
{
    None,
    UseItem
}

[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Usage Rules")]
    public ItemTargetType targetType;
    public bool targetWholeBody;
    public bool usableByLeftArm = true;
    public bool usableByRightArm = true;
    public int apCost = 1;

    [Header("Usage Rules")]
    public ItemUseType useType = ItemUseType.Both;

    [Header("Effects")]
    public int healAmount;
    public int mindRestore;
    public int damageAmount;
    
    public int hungerRestore;

    public bool curesPoison;
    public bool curesBurn;
    public bool curesBleed;
    public bool curesAllDOT;

    [Header("Animation Settings")]
    public ItemAnimationType animationType = ItemAnimationType.UseItem;
    public float animationDuration = 1.0f;

    [Header("Visual Effects")]
    public GameObject vfxPrefab;
    public bool spawnOnCaster = true;
    public bool spawnOnTarget = false;
    public float vfxLifetime = 2f;
}
