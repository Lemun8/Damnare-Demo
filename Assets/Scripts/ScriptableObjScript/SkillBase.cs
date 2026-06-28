using UnityEngine;

public enum SkillAnimationType
{
    None,
    WeaponAttack,
    MeleeAttack,
    CastSpell,
    Buff
}

public enum SkillUsageContext
{
    BattleOnly,
    OverworldOnly,
    Both
}

public enum TargetType { SingleBodyPart, EntireCharacter, Self }

public enum DOTType { None, Poison, Burn, Bleed }

[CreateAssetMenu(fileName = "NewSkill", menuName = "Combat/Skill")]
public class SkillBase : ScriptableObject
{
    public string skillName;
    public int apCost;
    public int priority;
    public TargetType targetType;
    public int power;
    public bool isMagic;

    [Header("Destruction Settings")]
    [Tooltip("If true, this skill can permanently destroy limbs when their HP reaches 0.")]
    public bool canDestroyLimb = false;

    [Header("Usage Context")]
    public SkillUsageContext usageContext = SkillUsageContext.Both;

    [Header("Hit Properties")]
    [Range(1, 100)] public float accuracy = 100f;

    [Header("Mind Cost (for magic skills)")]
    public int mindCost = 0;

    [Header("Healing Properties")]
    public bool isHealingSkill = false;
    public int minHeal = 0;
    public int maxHeal = 0;

    [Header("Buff Skill Settings")]
    public bool isBuffSkill = false;  // ✅ Mark this as a buff-type skill

    [Tooltip("Duration of buff in turns. Set 0 for permanent.")]
    public int buffDuration = 0;

    // These can be positive (buff) or negative (debuff)
    public int attackBuff = 0;
    public int magicAttackBuff = 0;
    public int defenseBuff = 0;
    public int magicDefenseBuff = 0;
    public int agilityBuff = 0;

    [Header("Debuff Skill Settings")]
    public bool isDebuffSkill = false;
    
    public bool canApplyDebuff = false;
    
    public int debuffDuration = 0;
    [Range(0f, 100f)]
    public float debuffApplyChance = 0f;

    public int attackDebuff = 0;
    public int magicAttackDebuff = 0;
    public int defenseDebuff = 0;
    public int magicDefenseDebuff = 0;
    public int agilityDebuff = 0;

    [Header("Damage Over Time (DOT)")]
    public DOTType dotType = DOTType.None;
    public int dotDuration = 0;

    [Header("Status Effect Proc (On Hit)")]
    public bool canApplyStatus = false;

    [Range(0f, 100f)]
    public float statusApplyChance = 0f;

    public DOTType statusDOTType = DOTType.None;
    public int statusDuration = 0;

    [Header("Animation")]
    public SkillAnimationType animationType = SkillAnimationType.None;

    [Header("Visual Effects")]
    public GameObject vfxPrefab;
    public bool spawnOnCaster = false;
    public bool spawnOnTarget = true;
    public float vfxLifetime = 2f;

    [Header("Animation fallback (seconds)")]
    [Tooltip("If the Animator does not enter the expected state, wait this many seconds before continuing")]
    public float animationDuration = 1f;

    [Header("Sound Effects")]
    public AudioClip castSFX;     // plays when skill is used

    [Range(0f, 1f)]
    public float sfxVolume = 1f;
}
