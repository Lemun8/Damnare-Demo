using System.Collections.Generic;
using UnityEngine;
using RPG.Combat;

public class CombatUIManager : MonoBehaviour
{
    private PlayerActionPlanner planner;
    private CombatManager combatManager;

    private List<BodyPart> currentBodyParts;
    private List<SkillBase> currentSkills;
    private List<ItemData> currentItems;
    private List<CharacterCombat> currentTargets;

    private BodyPart pendingBodyPart; // to store the selected limb for menu display

    private enum UIState { None, SelectBodyPart, ChooseActionType, SelectSkill, SelectItem, SelectTarget }
    private UIState state = UIState.None;

    public void Setup(PlayerActionPlanner p)
    {
        planner = p;
        combatManager = FindObjectOfType<CombatManager>();
    }

    public void ShowBodyParts(List<BodyPart> parts)
    {
        currentBodyParts = parts;
        state = UIState.SelectBodyPart;
    }

    public void ShowActionTypeMenu(BodyPart part)
    {
        pendingBodyPart = part;
        state = UIState.ChooseActionType;
    }

    public void ShowSkills(List<SkillBase> skills)
    {
        currentSkills = skills;
        state = UIState.SelectSkill;
    }

    public void ShowItems(List<ItemData> items)
    {
        // Filter out items that are not usable in combat
        if (planner != null && planner.GetCurrentActor() != null)
        {
            bool inCombat = planner.GetCurrentActor().isInCombat;
            currentItems = items?.FindAll(i =>
                (inCombat && i.useType != ItemUseType.OverworldOnly) ||
                (!inCombat && i.useType != ItemUseType.CombatOnly)
            );
        }
        else
        {
            currentItems = items;
        }
        state = UIState.SelectItem;
    }

    public void ShowTargetSelection(List<CharacterCombat> targets)
    {
        currentTargets = targets;
        state = UIState.SelectTarget;
    }

    public void ClearUI()
    {
        currentBodyParts = null;
        currentSkills = null;
        currentItems = null;
        currentTargets = null;
        state = UIState.None;
    }

    void OnGUI()
    {
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14
        };

        GUILayout.BeginArea(new Rect(10, 10, 350, Screen.height - 20), GUI.skin.window);
        // GUILayout.Label("<b>DEV COMBAT UI</b>", headerStyle);

        if (combatManager != null && combatManager.state == CombatState.PlayerPlanning)
        {
            var actor = planner.GetCurrentActor();
            if (actor != null)
            {
                DrawPlayerAP(actor, headerStyle);
                DrawPlayerMind(actor, headerStyle);
                GUILayout.Space(6);
                GUILayout.Label("<b>Player Status:</b>", headerStyle);
                DrawStatusEffects(actor, GUI.skin.label);
            }

            switch (state)
            {
                case UIState.SelectBodyPart:
                    GUILayout.Label("<b>Select Body Part:</b>", headerStyle);

                    if (currentBodyParts != null)
                    {
                        foreach (var part in currentBodyParts)
                        {
                            // build label with status
                            string label = $"{part.partName} ({part.currentHP}/{part.EffectiveMaxHP})";

                            // if limb is blacked out, append info and gray out
                            bool isDisabled = !part.IsFunctional || part.usedThisTurn || part.isBlackedOut;

                            if (part.isBlackedOut)
                                label += " <color=grey>[BLACKOUT]</color>";
                            else if (part.usedThisTurn)
                                label += " <color=yellow>[Used]</color>";

                            // darken button visually if grayed out
                            if (isDisabled)
                                GUI.enabled = false;

                            if (GUILayout.Button(label, buttonStyle))
                            {
                                if (!isDisabled)
                                {
                                    ShowActionTypeMenu(part);
                                }
                            }

                            GUI.enabled = true; // always re-enable for next loop
                        }
                    }
                    break;

                case UIState.ChooseActionType:
                    GUILayout.Label($"<b>{pendingBodyPart.partName}</b>", headerStyle);

                    if (GUILayout.Button("Use Skill", buttonStyle))
                    {
                        planner.OpenSkillsForLimb(pendingBodyPart); // go directly to skill list
                    }

                    bool isArm = pendingBodyPart.limbType == LimbType.LeftArm || pendingBodyPart.limbType == LimbType.RightArm;

                    GUI.enabled = isArm;
                    if (GUILayout.Button("Open Inventory", buttonStyle))
                    {
                        planner.OpenInventoryForLimb(pendingBodyPart);
                    }
                    GUI.enabled = true;

                    if (GUILayout.Button("Back", buttonStyle))
                    {
                        ShowBodyParts(planner.GetCurrentActor().bodyParts);
                    }
                    break;

                case UIState.SelectSkill:
                GUILayout.Label("<b>Select Skill or Item:</b>", headerStyle);

                if (currentSkills != null && actor != null)
                {
                    foreach (var skill in currentSkills)
                    {
                        int apCost = skill is EscapeSkill ? actor.maxAP : skill.apCost;

                        bool hasEnoughAP = actor.currentAP >= apCost;
                        bool hasEnoughMind = !skill.isMagic || actor.currentMind >= skill.mindCost;
                        bool canUse = hasEnoughAP && hasEnoughMind;

                        GUI.enabled = canUse;

                        string label = $"{skill.skillName} (AP {apCost}";
                        if (skill.isMagic)
                            label += $", Mind {skill.mindCost}";
                        label += ")";

                        if (!canUse)
                            label += " <color=red>[Not enough]</color>";

                        if (GUILayout.Button(label, buttonStyle))
                        {
                            planner.SelectSkill(skill);
                        }

                        GUI.enabled = true;
                    }
                }
                break;

                case UIState.SelectItem:
                    GUILayout.Label("<b>Select Item:</b>", headerStyle);
                    if (currentItems != null)
                    {
                        foreach (var item in currentItems)
                        {
                            if (GUILayout.Button($"{item.itemName} (AP {item.apCost})", buttonStyle))
                            {
                                planner.SelectItem(item);
                            }
                        }
                    }
                    break;

                case UIState.SelectTarget:
                GUILayout.Label("<b>Select Target:</b>", headerStyle);

                if (currentTargets != null)
                {
                    foreach (var target in currentTargets)
                    {
                        GUILayout.Label($"<b>{target.characterName}</b>", headerStyle);

                        // 👁️ Show enemy status effects
                        DrawStatusEffects(target, GUI.skin.label);

                        foreach (var part in target.bodyParts)
                        {
                            if (!part.IsFunctional) continue;

                            if (GUILayout.Button(
                                $" - {part.partName} ({part.currentHP}/{part.EffectiveMaxHP})",
                                buttonStyle))
                            {
                                planner.SelectTarget(target, part);
                            }
                        }

                        GUILayout.Space(6);
                    }
                }
                break;
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Finish Turn", buttonStyle))
            {
                planner.FinishPlanning();
                ClearUI();
            }

            if (state != UIState.SelectBodyPart)
            {
                if (GUILayout.Button("Cancel", buttonStyle))
                {
                    planner.RestartSelection();
                }
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();
    }

    void DrawPlayerAP(CharacterCombat actor, GUIStyle headerStyle)
    {
        GUILayout.Space(6);
        GUILayout.Label(
            $"<b>AP Remaining:</b> <color=cyan>{actor.currentAP} / {actor.maxAP}</color>",
            headerStyle
        );
    }

    void DrawPlayerMind(CharacterCombat actor, GUIStyle headerStyle)
    {
        int maxMind = actor.overallStats.mind;
        float currentMind = actor.currentMind;
        
        string mindColor = "cyan";
        
        // Color code based on mind percentage
        float mindPercent = maxMind > 0 ? (float)currentMind / maxMind : 0f;
        if (mindPercent <= 0.25f)
            mindColor = "red";
        else if (mindPercent <= 0.5f)
            mindColor = "yellow";
        
        GUILayout.Label(
            $"<b>Mind:</b> <color={mindColor}>{currentMind} / {maxMind}</color>",
            headerStyle
        );
    }

    void DrawStatusEffects(CharacterCombat character, GUIStyle smallStyle)
    {
        if (character.activeEffects == null || character.activeEffects.Count == 0)
        {
            GUILayout.Label("<i>No Status Effects</i>", smallStyle);
            return;
        }

        foreach (var eff in character.activeEffects)
        {
            // DOTs
            if (eff.dotType != DOTType.None)
            {
                string limb = eff.targetPart != null ? eff.targetPart.partName : "Body";
                GUILayout.Label(
                    $"🩸 {eff.dotType} ({limb}) · {eff.remainingTurns}T",
                    smallStyle
                );
                continue;
            }

            // DEBUFFS
            if (eff.isDebuff)
            {
                string limb = eff.targetPart != null ? eff.targetPart.partName : "Body";

                if (eff.atk != 0)
                    GUILayout.Label(
                        $"⬇ ATK {eff.atk} ({limb}) · {eff.remainingTurns}T",
                        smallStyle
                    );

                if (eff.matk != 0)
                    GUILayout.Label(
                        $"⬇ MATK {eff.matk} ({limb}) · {eff.remainingTurns}T",
                        smallStyle
                    );

                if (eff.def != 0)
                    GUILayout.Label(
                        $"⬇ DEF {eff.def} ({limb}) · {eff.remainingTurns}T",
                        smallStyle
                    );

                if (eff.mdef != 0)
                    GUILayout.Label(
                        $"⬇ MDEF {eff.mdef} ({limb}) · {eff.remainingTurns}T",
                        smallStyle
                    );

                if (eff.agi != 0)
                    GUILayout.Label(
                        $"⬇ AGI {eff.agi} ({limb}) · {eff.remainingTurns}T",
                        smallStyle
                    );
            }
        }
    }
}
