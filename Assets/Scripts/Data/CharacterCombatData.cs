using System.Collections.Generic;
using UnityEngine;
using RPG.Combat;

[System.Serializable]
public class CharacterCombatData
{
    public string characterName;
    public OverallStats overallStats;

    public int currentAP;
    public int maxAP;

    public List<BodyPartData> bodyParts = new List<BodyPartData>();

    [System.Serializable]
    public class BodyPartData
    {
        public string partName;
        public LimbType limbType;

        // Equipment
        public EquipmentData equippedWeapon;
        public EquipmentData equippedArmor;
        public EquipmentData equippedAccessory;

        // Skills
        public List<SkillBase> limbSkills = new List<SkillBase>();

        // 🩸 HP + blackout info
        public int currentHP;
        public int maxHP;
        public bool isBlackedOut;

        // ----- NEW: base stats that must be restored -----
        public int baseMaxHP;
        public int baseAttack;
        public int baseMagicAttack;
        public int baseDefense;
        public int baseMagicDefense;
    }

    public CharacterCombatData(CharacterCombat combat)
    {
        if (combat == null) return;

        characterName = combat.characterName;
        overallStats = new OverallStats(combat.overallStats);
        
        currentAP = combat.currentAP;
        maxAP = combat.maxAP;

        foreach (var bp in combat.bodyParts)
        {
            var bpd = new BodyPartData
            {
                partName = bp.partName,
                limbType = bp.limbType,
                equippedWeapon = bp.weaponSlot?.equippedItem,
                equippedArmor = bp.armorSlot?.equippedItem,
                equippedAccessory = bp.accessorySlot?.equippedItem,
                limbSkills = new List<SkillBase>(),
                // 🩸 Save limb HP & blackout state
                currentHP = bp.currentHP,
                maxHP = bp.maxHP,
                isBlackedOut = bp.isBlackedOut,
                // SAVE base stats as well
                baseMaxHP = bp.baseMaxHP,
                baseAttack = bp.baseAttack,
                baseMagicAttack = bp.baseMagicAttack,
                baseDefense = bp.baseDefense,
                baseMagicDefense = bp.baseMagicDefense
            };

            if (bp.limbSkills != null)
            {
                foreach (var s in bp.limbSkills)
                    if (s != null) bpd.limbSkills.Add(s);
            }

            bodyParts.Add(bpd);
        }
    }

    public void ApplyTo(CharacterCombat combat)
    {
        if (combat == null) return;

        combat.characterName = characterName;

        // assign overallStats reference back (if you want clones, copy fields instead)
        combat.overallStats = new OverallStats(overallStats);

        // === RESTORE OVERALL STATS ===
        if (overallStats != null)
        {
            if (combat.overallStats == null)
                combat.overallStats = new OverallStats();

            combat.overallStats.mind = overallStats.mind;
            combat.overallStats.luck = overallStats.luck;
            combat.overallStats.agility = overallStats.agility;

            combat.overallStats.evasion = overallStats.evasion;
            combat.overallStats.accuracy = overallStats.accuracy;
        }
        else
        {
            // fallback safety
            combat.overallStats = new OverallStats();
        }

        // restore equipment + limb skills per bodypart
        foreach (var bp in combat.bodyParts)
        {
            var saved = bodyParts.Find(p => p.limbType == bp.limbType);
            if (saved == null) continue;

            // ----- restore base stats FIRST so InitializeParts / Recalculate uses them -----
            bp.baseMaxHP = saved.baseMaxHP;
            bp.baseAttack = saved.baseAttack;
            bp.baseMagicAttack = saved.baseMagicAttack;
            bp.baseDefense = saved.baseDefense;
            bp.baseMagicDefense = saved.baseMagicDefense;

            // restore runtime maxHP/currentHP from saved baseMaxHP if needed
            bp.maxHP = saved.maxHP;
            bp.currentHP = Mathf.Clamp(saved.currentHP, 0, saved.maxHP);

            // restore blackout state
            bp.isBlackedOut = saved.isBlackedOut;
            if (bp.isBlackedOut && bp.currentHP > 0)
                bp.isBlackedOut = false;

            // Restore innate/limb skills
            bp.innateLimbSkills = new List<SkillBase>();
            if (saved.limbSkills != null)
            {
                foreach (var s in saved.limbSkills)
                    if (s != null) bp.innateLimbSkills.Add(s);
            }

            // runtime limbSkills will be rebuilt after equipment restore
            bp.limbSkills = new List<SkillBase>(bp.innateLimbSkills);

            // restore equipment
            bp.UnequipAll();
            if (saved.equippedWeapon != null) bp.EquipItem(saved.equippedWeapon);
            if (saved.equippedArmor != null) bp.EquipItem(saved.equippedArmor);
            if (saved.equippedAccessory != null) bp.EquipItem(saved.equippedAccessory);
        }

        // Re-initialize parts (this will keep the base values we just set)
        combat.InitializeParts();
        foreach (var part in combat.bodyParts)
            part.ResetTurnUsage();

        combat.maxAP = maxAP;
        combat.currentAP = Mathf.Clamp(currentAP, 0, maxAP);

        // === SYNC PLAYER MIND FROM OVERWORLD ===
        if (combat.isPlayerControlled)
        {
            combat.maxMind = (int)combat.overallStats.mind;    
            combat.currentMind = Mathf.Clamp((int)PlayerGameState.CurrentMind, 0, combat.maxMind);
        }

        // === APPLY HUNGER MAX HP CLAMP ON SPAWN ===
        if (combat.isPlayerControlled)
        {
            combat.ApplyHungerHPClamp();
        }
    }
}
