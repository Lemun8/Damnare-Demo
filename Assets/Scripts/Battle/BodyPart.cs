using System.Collections.Generic;
using UnityEngine;
using RPG.Combat;

[System.Serializable]
public class BodyPart
{
    [Header("Basic Info")]
    public string partName;
    public LimbType limbType;
    public int maxHP = 10;
    public int currentHP;
    public bool IsFunctional => currentHP > 0;

    [HideInInspector] public CharacterCombat character;

    [Header("Blackout State")]
    public bool isBlackedOut = false;

    [Header("Current Stats")]
    public int attack = 5;
    public int magicAttack = 0;
    public int defense = 1;
    public int magicDefense = 1;

    [Header("Limb-Specific Skills")]
    public List<SkillBase> limbSkills = new List<SkillBase>();
    private List<SkillBase> equipmentSkills = new List<SkillBase>();

    [Header("Internal (do not edit at runtime)")]
    [SerializeField] public List<SkillBase> innateLimbSkills = new List<SkillBase>();

    [Header("Equipment Slots")]
    public EquipmentSlot armorSlot = new EquipmentSlot(EquipmentType.Armor);
    public EquipmentSlot weaponSlot = new EquipmentSlot(EquipmentType.Weapon);
    public EquipmentSlot accessorySlot = new EquipmentSlot(EquipmentType.Accessory);

    [HideInInspector] public bool usedThisTurn = false;
    [HideInInspector] public int baseAttack;
    [HideInInspector] public int baseMagicAttack;
    [HideInInspector] public int baseDefense;
    [HideInInspector] public int baseMagicDefense;
    [HideInInspector] public int baseMaxHP;

    public BodyPart(string name, int hp)
    {
        partName = name;
        maxHP = hp;
        currentHP = hp;
        attack = 5;
        magicAttack = (name == "Head") ? 10 : 0;
        defense = 1;
        magicDefense = 1;

        baseAttack = attack;
        baseMagicAttack = magicAttack;
        baseDefense = defense;
        baseMagicDefense = magicDefense;
        baseMaxHP = maxHP;
    }

    public void ResetTurnUsage() => usedThisTurn = false;

    public void EnterBlackout()
    {
        if (limbType == LimbType.Head || limbType == LimbType.Torso)
            return; // head/torso blackout = death handled elsewhere

        isBlackedOut = true;
        currentHP = 0;
        Debug.Log($"{partName} has entered blackout state — cannot be used until healed.");
    }

    public void RecoverFromBlackout(int healAmount)
    {
        if (!isBlackedOut) return;

        currentHP = Mathf.Min(healAmount, maxHP);
        isBlackedOut = false;
        Debug.Log($"{partName} has recovered from blackout with {currentHP}/{maxHP} HP.");
    }

    // --- ✅ NEW PUBLIC METHOD ---
    public bool CanEquip(EquipmentData item)
    {
        if (item == null) return false;

        switch (item.equipmentType)
        {
            case EquipmentType.Weapon:
                // Only arms can equip weapons, must match limbSlot
                return limbType == item.limbSlot;

            case EquipmentType.Armor:
                // Armor must match limbSlot exactly
                return limbType == item.limbSlot;

            case EquipmentType.Accessory:
                // Use allowedLimbs list for accessories
                return item.allowedLimbs != null && item.allowedLimbs.Contains(limbType);

            default:
                return false;
        }
    }

    private bool IsEligibleForItem(EquipmentData item)
    {
        switch (item.equipmentType)
        {
            case EquipmentType.Weapon:
                // Only arms can equip weapons, must match limbSlot
                return limbType == item.limbSlot;

            case EquipmentType.Armor:
                // Armor must match limbSlot
                return limbType == item.limbSlot;

            case EquipmentType.Accessory:
                // Use allowedLimbs list for accessories
                return item.allowedLimbs != null && item.allowedLimbs.Contains(limbType);

            default:
                return false;
        }
    }

    public void EquipItem(EquipmentData item)
    {
        if (item == null) return;

        if (!IsEligibleForItem(item))
        {
            Debug.LogWarning($"[BodyPart] ❌ {partName} is not eligible for {item.equipmentType} ({item.equipmentName})");
            return;
        }

        EquipmentSlot targetSlot = null;
        switch (item.equipmentType)
        {
            case EquipmentType.Armor:
                if (item.limbSlot != limbType)
                {
                    Debug.LogWarning($"[BodyPart] ❌ {item.equipmentName} is armor for {item.limbSlot}, can't equip on {limbType}");
                    return;
                }
                targetSlot = armorSlot;
                break;

            case EquipmentType.Weapon:
                if (item.limbSlot != limbType)
                {
                    Debug.LogWarning($"[BodyPart] ❌ Weapon {item.equipmentName} is for {item.limbSlot}, can't equip on {limbType}");
                    return;
                }
                targetSlot = weaponSlot;
                break;

            case EquipmentType.Accessory:
                targetSlot = accessorySlot;
                break;
        }

        if (targetSlot == null) return;

        // Unequip old item in that slot
        if (!targetSlot.IsEmpty)
            UnequipSlot(targetSlot);

        targetSlot.Equip(item);
        RecalculateStatsAndSkills();

        Debug.Log($"[BodyPart] ✅ Equipped {item.equipmentName} ({item.equipmentType}) on {partName}");
    }

    // returns the unequipped EquipmentData (or null if none)
    public EquipmentData UnequipSlot(EquipmentSlot slot)
    {
        if (slot == null || slot.IsEmpty) return null;

        var removed = slot.equippedItem;

        // Remove only equipment-granted skills from equipmentSkills list
        // (Recalculate will rebuild)
        slot.Unequip();
        RecalculateStatsAndSkills();

        return removed;
    }

    public List<EquipmentData> UnequipAll()
    {
        var removed = new List<EquipmentData>();
        var a = UnequipSlot(armorSlot);
        var w = UnequipSlot(weaponSlot);
        var ac = UnequipSlot(accessorySlot);
        if (a != null) removed.Add(a);
        if (w != null) removed.Add(w);
        if (ac != null) removed.Add(ac);
        return removed;
    }

    private void RecalculateStatsAndSkills()
    {
        // Ensure lists exist
        limbSkills ??= new List<SkillBase>();
        equipmentSkills ??= new List<SkillBase>();
        innateLimbSkills ??= new List<SkillBase>();

        // reset base stats
        attack = baseAttack;
        magicAttack = baseMagicAttack;
        defense = baseDefense;
        magicDefense = baseMagicDefense;

        equipmentSkills.Clear();

        ApplyItemStats(armorSlot);
        ApplyItemStats(weaponSlot);
        ApplyItemStats(accessorySlot);

        // Build final skill list from innate + equipment skills (no duplicates)
        var newSkillSet = new HashSet<SkillBase>();

        // Start from innate skills (the original skill list captured at startup)
        if (innateLimbSkills != null)
        {
            foreach (var s in innateLimbSkills)
            {
                if (s != null)
                    newSkillSet.Add(s);
            }
        }

        // Then add any equipment-granted skills
        foreach (var eqSkill in equipmentSkills)
        {
            if (eqSkill != null)
                newSkillSet.Add(eqSkill);
        }

        limbSkills = new List<SkillBase>(newSkillSet);
    }

    private void ApplyItemStats(EquipmentSlot slot)
    {
        if (slot == null || slot.IsEmpty) return;
        var item = slot.equippedItem;
        if (item == null) return;

        attack += item.attackBonus;
        magicAttack += item.magicAttackBonus;
        defense += item.defenseBonus;
        magicDefense += item.magicDefenseBonus;

        // 🔒 Prevent null-reference if the list wasn’t serialized
        if (item.grantedSkills != null)
        {
            foreach (var s in item.grantedSkills)
            {
                if (s != null && !equipmentSkills.Contains(s))
                    equipmentSkills.Add(s);
            }
        }
    }

    public int EffectiveMaxHP
    {
        get
        {
            int result = baseMaxHP;

            if (character != null && character.isPlayerControlled)
            {
                switch (PlayerGameState.CurrentHungerStage)
                {
                    case HungerStage.Hunger:
                        result = Mathf.RoundToInt(baseMaxHP * 0.8f);
                        break;

                    case HungerStage.GreaterHunger:
                        result = Mathf.RoundToInt(baseMaxHP * 0.5f);
                        break;
                }
            }

            return result;
        }
    }
}
