using System.Collections.Generic;
using UnityEngine;
using RPG.Combat;

public class PlayerActionPlanner : MonoBehaviour
{
    public CharacterCombat player { get; private set; }
    public CombatUIManager uiManager { get; private set; }

    private CombatManager combatManager;
    private List<TurnAction> plannedActions = new List<TurnAction>();

    private BodyPart selectedBodyPart;
    private SkillBase selectedSkill;
    private ItemData selectedItem;
    private CharacterCombat selectedTarget;
    private BodyPart selectedTargetPart;

    // Called externally (by BattleSceneManager)
    public void Initialize(CharacterCombat playerCombat, CombatUIManager ui)
    {
        player = playerCombat;
        uiManager = ui;
        combatManager = FindObjectOfType<CombatManager>();
        uiManager.Setup(this);

        Debug.Log("[Planner] Initialized with player: " + player.characterName);
    }

    public void BeginPlanning()
    {
        if (player == null)
        {
            Debug.LogError("[Planner] No player assigned!");
            return;
        }

        Debug.Log("[Planner] Begin planning for " + player.characterName);
        plannedActions.Clear();
        player.ResetAP();
        StartBodyPartSelection();
    }

    void StartBodyPartSelection()
    {
        selectedBodyPart = null;
        selectedSkill = null;
        selectedItem = null;
        selectedTarget = null;
        selectedTargetPart = null;
        uiManager.ShowBodyParts(player.bodyParts);
    }

    public void SelectBodyPart(BodyPart part)
    {
        if (part.usedThisTurn)
        {
            Debug.Log($"[Planner] {part.partName} already acted this turn!");
            return;
        }

        selectedBodyPart = part;

        // Always go to the action type menu now
        uiManager.ShowActionTypeMenu(part);
    }

    public void OpenSkillsForLimb(BodyPart part)
    {
        selectedBodyPart = part;
        uiManager.ShowSkills(part.limbSkills);
    }

    public void OpenInventoryForLimb(BodyPart part)
    {
        selectedBodyPart = part;

        var usableItems = new List<ItemData>();

        if (player.inventory != null && player.inventory.slots != null)
        {
            foreach (var slot in player.inventory.slots)
            {
                if (slot.data == null) continue;

                // 👇 Explicitly cast ScriptableObject to ItemData
                ItemData it = slot.data as ItemData;
                if (it == null) continue;

                // 🧱 Filter: Only items that are usable in combat
                if (player.isInCombat && it.useType == ItemUseType.OverworldOnly) continue;
                if (!player.isInCombat && it.useType == ItemUseType.CombatOnly) continue;

                // 🦾 Check if limb can use the item
                if (part.limbType == LimbType.LeftArm && it.usableByLeftArm)
                    usableItems.Add(it);
                else if (part.limbType == LimbType.RightArm && it.usableByRightArm)
                    usableItems.Add(it);
            }
        }

        uiManager.ShowItems(usableItems);
    }

    public void SelectSkill(SkillBase skill)
    {
        if (skill.apCost > player.currentAP)
        {
            Debug.Log("Not enough AP!");
            return;
        }

        if (skill.isMagic && player.currentMind < skill.mindCost)
        {
            Debug.Log("Not enough Mind!");
            return;
        }

        selectedSkill = skill;

        // ✅ Determine valid targets based on skill type
        List<CharacterCombat> potentialTargets = new List<CharacterCombat>();

        if (skill.targetType == TargetType.Self)
        {
            // 🧩 Handle Escape Skill special case
            int apCost = skill is EscapeSkill ? player.maxAP : skill.apCost;

            if (player.currentAP < apCost)
            {
                Debug.Log("Not enough AP!");
                return;
            }

            TurnAction selfAction = new TurnAction
            {
                actor = player,
                bodyPart = selectedBodyPart,
                skill = selectedSkill,
                target = player,
                targetPart = null, // no specific limb needed
                priority = selectedSkill.priority
            };

            plannedActions.Add(selfAction);
            player.currentAP -= apCost;
            selectedBodyPart.usedThisTurn = true;

            Debug.Log($"[Planner] Queued SELF skill {skill.skillName} using {selectedBodyPart.partName} on self. AP cost: {apCost}");

            if (player.currentAP > 0)
                StartBodyPartSelection();
            else
                FinishPlanning();

            return;
        }

        // ✅ Healing skills should target self/allies, not enemies
        if (skill.isHealingSkill)
        {
            // For now we assume no allies, so only self
            potentialTargets.Add(player);
        }
        else
        {
            // Offensive → enemies
            potentialTargets = combatManager.GetEnemies(player);
        }

        // ✅ For entire-body targeting, skip limb selection
        if (skill.targetType == TargetType.EntireCharacter)
        {
            foreach (var target in potentialTargets)
            {
                TurnAction action = new TurnAction
                {
                    actor = player,
                    bodyPart = selectedBodyPart,
                    skill = selectedSkill,
                    target = target,
                    targetPart = null,
                    priority = selectedSkill.priority
                };

                plannedActions.Add(action);
                player.currentAP -= selectedSkill.apCost;
                selectedBodyPart.usedThisTurn = true;

                Debug.Log($"[Planner] Queued WHOLE-BODY skill {skill.skillName} using {selectedBodyPart.partName} on {target.characterName}");
            }

            if (player.currentAP > 0)
                StartBodyPartSelection();
            else
                FinishPlanning();
        }
        else
        {
            // ✅ SingleBodyPart → show selection UI for limbs
            uiManager.ShowTargetSelection(potentialTargets);
        }
    }

    public void SelectItem(ItemData item)
    {
        if (player.currentAP < item.apCost)
        {
            Debug.Log("Not enough AP for item!");
            return;
        }

        if (player.inventory == null)
        {
            Debug.LogWarning("[Planner] Player has no inventory!");
            return;
        }

        int qty = player.inventory.GetQuantity(item);
        if (qty <= 0)
        {
            Debug.LogWarning($"[Planner] Cannot queue {item.itemName} — none remaining!");
            return;
        }

        // 🧾 Immediately consume 1 item to prevent multiple arms using it
        bool removed = player.inventory.Remove(item, 1);
        if (!removed)
        {
            Debug.LogWarning($"[Planner] Failed to remove {item.itemName} from inventory!");
            return;
        }

        selectedItem = item;

        // Determine valid targets
        var targets = new List<CharacterCombat>();
        if (item.targetType == ItemTargetType.Self)
            targets.Add(player);
        else if (item.targetType == ItemTargetType.Ally)
            targets.Add(player); // TODO: replace with ally list later
        else // Enemy
            targets = combatManager.GetEnemies(player);

        // ✅ If the item targets the whole body, skip limb selection and queue it immediately
        if (item.targetWholeBody)
        {
            foreach (var target in targets)
            {
                TurnAction action = new TurnAction
                {
                    actor = player,
                    bodyPart = selectedBodyPart,
                    item = selectedItem,
                    target = target,
                    targetPart = null,
                    priority = 0
                };

                plannedActions.Add(action);
                player.currentAP -= selectedItem.apCost;
                selectedBodyPart.usedThisTurn = true;

                Debug.Log($"[Planner] Queued WHOLE-BODY ITEM {selectedItem.itemName} using {selectedBodyPart.partName} " +
                        $"on {target.characterName}'s entire body | Remaining AP: {player.currentAP}");
            }

            // Proceed to next action or finish
            if (player.currentAP > 0)
                StartBodyPartSelection();
            else
                FinishPlanning();
        }
        else
        {
            uiManager.ShowTargetSelection(targets);
        }
    }

    public void SelectTarget(CharacterCombat target, BodyPart targetPart)
    {
        selectedTarget = target;
        selectedTargetPart = targetPart;

        if (selectedItem != null)
        {
            TurnAction action = new TurnAction
            {
                actor = player,
                bodyPart = selectedBodyPart,
                item = selectedItem,
                target = selectedTarget,
                targetPart = selectedItem.targetWholeBody ? null : selectedTargetPart,
                priority = 0
            };

            plannedActions.Add(action);

            player.currentAP -= selectedItem.apCost;
            selectedBodyPart.usedThisTurn = true;

            Debug.Log($"[Planner] Queued ITEM {selectedItem.itemName} using {selectedBodyPart.partName} " +
                    $"on {(selectedItem.targetWholeBody ? "whole body" : selectedTargetPart?.partName)} " +
                    $"of {selectedTarget.characterName} | Remaining AP: {player.currentAP}");
        }
        else if (selectedSkill != null)
        {
            TurnAction action = new TurnAction
            {
                actor = player,
                bodyPart = selectedBodyPart,
                skill = selectedSkill,
                target = selectedTarget,
                targetPart = selectedTargetPart,
                priority = selectedSkill.priority
            };

            plannedActions.Add(action);
            player.currentAP -= selectedSkill.apCost;
            selectedBodyPart.usedThisTurn = true;
        }

        if (player.currentAP > 0)
            StartBodyPartSelection();
        else
            FinishPlanning();
    }


    public void RestartSelection()
    {
        Debug.Log("[Planner] Restarting selection");
        selectedBodyPart = null;
        selectedSkill = null;
        selectedItem = null;
        selectedTarget = null;
        selectedTargetPart = null;
        uiManager.ShowBodyParts(player.bodyParts);
    }

    public void FinishPlanning()
    {
        Debug.Log("[Planner] Finished planning");
        combatManager.ReceivePlannedActions(plannedActions);
    }

    public CharacterCombat GetCurrentActor()
    {
        return player;
    }
}
