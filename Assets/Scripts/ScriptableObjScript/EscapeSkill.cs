using UnityEngine;

[CreateAssetMenu(fileName = "EscapeSkill", menuName = "Combat/Escape")]
public class EscapeSkill : SkillBase
{
    [Header("Escape Settings")]
    [Tooltip("How much each point of Agility difference changes the success chance.")]
    public float agilityDifferenceScale = 1f; // 1 agility = 1%

    [Tooltip("Minimum and maximum escape chance limits.")]
    public float minChance = 10f;
    public float maxChance = 95f;

    public override string ToString() => $"{skillName} (Escape Skill)";

    public EscapeSkill()
    {
        targetType = TargetType.Self;
        apCost = 0; // base (ignored during runtime)
    }
}
