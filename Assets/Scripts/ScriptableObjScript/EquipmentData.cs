using System.Collections.Generic;
using UnityEngine;
using RPG.Combat;

public enum WeaponHandType
{
    OneHanded,
    TwoHanded
}

[CreateAssetMenu(fileName = "New Equipment", menuName = "RPG/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("Basic Info")]
    public string equipmentName;
    public EquipmentType equipmentType;

    [Header("Weapon Settings")]
    public WeaponHandType weaponHandType = WeaponHandType.OneHanded;

    [Tooltip("For weapons & armor: determines which limb this fits. For accessories, this determines allowed limb(s).")]
    public LimbType limbSlot; // For Weapon/Armor
    public List<LimbType> allowedLimbs; // For Accessory or special cases

    [Header("Stat Bonuses")]
    public int attackBonus;
    public int magicAttackBonus;
    public int defenseBonus;
    public int magicDefenseBonus;
    public int mindBonus;

    [Header("Skills Granted")]
    public List<SkillBase> grantedSkills = new List<SkillBase>();
}
