using UnityEngine;
using System.Collections;

public class OverworldSkillExecutor : MonoBehaviour
{
    private PlayerMind playerMind;
    private CharacterCombat characterCombat;

    void Awake()
    {
        playerMind = GetComponent<PlayerMind>();
        characterCombat = GetComponent<CharacterCombat>();
    }

    public void UseSkill(SkillBase skill)
    {
        if (skill == null) return;

        // ✅ 1. Check if usable in overworld
        if (skill.usageContext == SkillUsageContext.BattleOnly)
        {
            Debug.LogWarning($"{skill.skillName} cannot be used in overworld!");
            return;
        }

        // ✅ 2. Check mind cost
        if (playerMind.currentMind < skill.mindCost)
        {
            Debug.LogWarning($"Not enough Mind to use {skill.skillName}!");
            return;
        }

        playerMind.currentMind -= skill.mindCost;
        PlayerGameState.CurrentMind = playerMind.currentMind;
        Debug.Log($"Used {skill.skillName}, -{skill.mindCost} Mind (Remaining: {playerMind.currentMind})");

        // ✅ 3. Apply the skill effect
        if (skill.isHealingSkill)
        {
            StartCoroutine(ApplyHealing(skill));
        }
        else if (skill.isBuffSkill)
        {
            ApplyBuff(skill);
        }
        else
        {
            Debug.Log($"{skill.skillName} has no overworld effect defined.");
        }
    }

    private IEnumerator ApplyHealing(SkillBase skill)
    {
        if (characterCombat == null) yield break;

        int healAmount = Random.Range(skill.minHeal, skill.maxHeal + 1);
        healAmount += Mathf.RoundToInt(characterCombat.mind * 0.3f);

        foreach (var part in characterCombat.bodyParts)
        {
            if (part.isBlackedOut)
                part.RecoverFromBlackout(healAmount);
            else
                part.currentHP = Mathf.Min(part.maxHP, part.currentHP + healAmount);
        }

        Debug.Log($"Healed for {healAmount} HP on all body parts.");
        yield return null;
    }

    private void ApplyBuff(SkillBase skill)
    {
        if (characterCombat == null) return;

        // Apply stat buffs immediately (no duration system in overworld)
        foreach (var part in characterCombat.bodyParts)
        {
            part.attack += skill.attackBuff;
            part.magicAttack += skill.magicAttackBuff;
            part.defense += skill.defenseBuff;
            part.magicDefense += skill.magicDefenseBuff;
        }

        characterCombat.agility += skill.agilityBuff;

        Debug.Log($"Applied overworld buff from {skill.skillName}.");
    }
}
