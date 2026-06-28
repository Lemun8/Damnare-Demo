using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Combat;

public enum CombatState
{
    WaitingForSetup,
    PlayerPlanning,
    EnemyTurn,
    Executing,
    Finished
}

public class CombatManager : MonoBehaviour
{
    [Header("Combatants")]
    public List<CharacterCombat> combatants = new List<CharacterCombat>();

    [Header("Turn Data")]
    private Queue<TurnAction> actionQueue = new Queue<TurnAction>();
    public CombatState state = CombatState.WaitingForSetup;

    private PlayerActionPlanner playerPlanner;
    private int roundNumber = 1;

    private bool combatInitialized = false;

    void Start()
    {
        // Wait for BattleSceneManager to finish instantiating characters
        StartCoroutine(WaitForCombatSetup());
    }

    IEnumerator WaitForCombatSetup()
    {
        while (!combatInitialized)
        {
            combatants.Clear();
            combatants.AddRange(FindObjectsOfType<CharacterCombat>());

            bool hasPlayer = combatants.Exists(c => c.isPlayerControlled);
            bool hasEnemy = combatants.Exists(c => !c.isPlayerControlled);

            if (hasPlayer && hasEnemy)
            {
                combatInitialized = true;
                foreach (var c in combatants)
                {
                    c.isInCombat = true;
                }
                InitializeCombat();
                break;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    void InitializeCombat()
    {
        playerPlanner = FindObjectOfType<PlayerActionPlanner>();
        state = CombatState.PlayerPlanning;

        FocusPlayerCamera();

        StartCoroutine(CombatLoop());
    }

    IEnumerator CombatLoop()
    {
        while (state != CombatState.Finished)
        {
            Debug.Log($"\n=== 🌀 ROUND {roundNumber} START ===");

            // Reset
            foreach (var c in combatants)
            {
                if (c == null) continue;
                c.ResetAP();
                foreach (var part in c.bodyParts)
                    part.ResetTurnUsage();
            }

            actionQueue.Clear();

            // Player planning
            CharacterCombat player = combatants.Find(c => c.isPlayerControlled);
            if (player != null && player.IsAlive)
            {
                state = CombatState.PlayerPlanning;
                FocusPlayerCamera();
                Debug.Log("[Player] 📝 Planning actions...");
                playerPlanner.BeginPlanning();
                yield return new WaitUntil(() => state != CombatState.PlayerPlanning);
            }

            // Enemy planning (fill actionQueue)
            state = CombatState.EnemyTurn;
            foreach (var enemy in combatants)
            {
                if (enemy == null || enemy.isPlayerControlled || !enemy.IsAlive) continue;
                PlanEnemyActions(enemy);
            }

            // If player planned actions, they should have been added via ReceivePlannedActions -> actionQueue.Enqueue
            state = CombatState.Executing;
            FocusBattlefieldCamera();
            Debug.Log("=== ⚔ ACTION EXECUTION PHASE ===");

            // Sort queued actions by priority/agility then re-build the queue
            var sorted = new List<TurnAction>(actionQueue);
            sorted.Sort((a, b) =>
            {
                // 1️⃣ Priority check
                if (a.priority != b.priority)
                    return b.priority.CompareTo(a.priority);

                // 2️⃣ Agility check
                if (a.actor.agility != b.actor.agility)
                    return b.actor.agility.CompareTo(a.actor.agility);

                // 3️⃣ Coin flip (random order if equal)
                return Random.value < 0.5f ? -1 : 1;
            });

            actionQueue = new Queue<TurnAction>(sorted);

            // Process queue sequentially, waiting for each to fully finish
            while (actionQueue.Count > 0)
            {
                var act = actionQueue.Dequeue();
                if (act == null || act.actor == null) continue;

                yield return StartCoroutine(ExecuteActionCoroutine(act));

                // 🔧 Immediately check if anyone died during the action
                if (HasCombatEndedMidTurn())
                {
                    Debug.Log("⚔️ Combat ended early due to death mid-turn.");
                    yield break; // Exit CombatLoop early
                }
            }

            // ✅ apply DOT end of turn
            foreach (var c in combatants)
            {
                if (c == null || !c.IsAlive) continue;
                c.ProcessStatusEffects();    // <-- DOT tick here
            }

            // now check victory AFTER DOT
            if (CheckEndCombat())
            {
                state = CombatState.Finished;
                break;
            }

            Debug.Log($"=== 🌀 ROUND {roundNumber} END ===\n");
            roundNumber++;
            yield return null;
        }

        Debug.Log("=== 🏁 COMBAT FINISHED ===");
    }

    private void PlanEnemyActions(CharacterCombat enemy)
    {
        int safety = 50;
        while (enemy.currentAP > 0 && safety-- > 0)
        {
            var availableLimbs = enemy.bodyParts.FindAll(p => p.IsFunctional && !p.usedThisTurn && p.limbSkills.Count > 0);
            if (availableLimbs.Count == 0) break;

            BodyPart chosenLimb = availableLimbs[Random.Range(0, availableLimbs.Count)];
            var affordableSkills = chosenLimb.limbSkills.FindAll(s => s.apCost <= enemy.currentAP && (!s.isMagic || enemy.currentMind >= s.mindCost));
            if (affordableSkills.Count == 0) break;

            SkillBase chosenSkill = affordableSkills[Random.Range(0, affordableSkills.Count)];
            var potentialTargets = combatants.FindAll(c => c.isPlayerControlled && c.IsAlive);
            if (potentialTargets.Count == 0) break;

            CharacterCombat chosenTarget = potentialTargets[Random.Range(0, potentialTargets.Count)];
            var targetParts = chosenTarget.bodyParts.FindAll(p => p.IsFunctional);
            if (targetParts.Count == 0) break;

            BodyPart chosenTargetPart = targetParts[Random.Range(0, targetParts.Count)];

            TurnAction enemyAction = new TurnAction
            {
                actor = enemy,
                bodyPart = chosenLimb,
                skill = chosenSkill,
                target = chosenTarget,
                targetPart = chosenTargetPart,
                priority = chosenSkill.priority
            };

            actionQueue.Enqueue(enemyAction);
            enemy.currentAP -= chosenSkill.apCost;
            chosenLimb.usedThisTurn = true;
        }
    }

    public List<CharacterCombat> GetEnemies(CharacterCombat requester)
    {
        List<CharacterCombat> enemies = new List<CharacterCombat>();
        foreach (var c in combatants)
        {
            if (c != requester && c.IsAlive)
                enemies.Add(c);
        }
        return enemies;
    }

    public void ReceivePlannedActions(List<TurnAction> planned)
    {
        if (planned == null || planned.Count == 0)
        {
            Debug.LogWarning("[Player] ⚠ No actions planned.");
            state = CombatState.EnemyTurn;
            return;
        }

        foreach (var t in planned) actionQueue.Enqueue(t);
        Debug.Log($"[Player] ➕ Enqueued {planned.Count} player actions.");
        state = CombatState.EnemyTurn;
    }

    private IEnumerator ExecuteActionCoroutine(TurnAction act)
    {
        if (act == null || act.actor == null) yield break;
        if (act.target != null && !act.target.IsAlive) yield break;

        if (act.bodyPart == null || !act.bodyPart.IsFunctional || act.bodyPart.isBlackedOut)
        {
            Debug.Log(
                $"⛔ Action cancelled: {act.actor.characterName}'s {act.bodyPart?.partName} is no longer functional."
            );
            yield break; // ⬅️ SKIP EVERYTHING (no animation, no VFX)
        }

        // 🧴 ITEM actions — now play item animation properly
        if (act.IsItem)
        {
            var item = act.item;
            var actor = act.actor;

            if (item == null || actor == null)
                yield break;

            Debug.Log($"{actor.characterName} is using item {item.itemName}...");

            bool animationDone = false;

            // 🔧 Play the item's animation and wait for it to finish
            yield return actor.StartCoroutine(
                actor.PlayItemAnimationAndWait(item.animationType, item.animationDuration, () => animationDone = true)
            );
            yield return new WaitUntil(() => animationDone);

            // 🔧 Spawn item VFX
            SpawnItemVFX(act);

            // 🔧 Apply item effect *after* animation
            actor.ApplyItemEffect(item, act.bodyPart, act.target, act.targetPart);

            NotificationManager.Show($"{actor.characterName} uses item {item.itemName}");

            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        NotificationManager.Show($"{act.actor.characterName} uses {act.skill.skillName}");

        // 🧠 Mind cost check for skills
        if (act.skill.isMagic)
        {
            if (act.actor.currentMind < act.skill.mindCost)
            {
                Debug.LogWarning($"[Mind Check] ❌ {act.actor.characterName} doesn't have enough Mind to cast {act.skill.skillName}.");
                yield break;
            }

            act.actor.currentMind -= act.skill.mindCost;
            Debug.Log($"[Mind Check] 🧠 {act.actor.characterName} used {act.skill.mindCost} Mind to cast {act.skill.skillName}. Remaining Mind: {act.actor.currentMind}");
        }

        // 🎯 Accuracy check
        if (!CheckHit(act))
        {
            Debug.Log($"{act.actor.characterName}'s {act.skill.skillName} missed {act.target.characterName}!");
            DamagePopupManager.Instance.ShowMiss(act.target.transform.position);

            bool missAnimDone = false;

            if (act.actor != null)
            {
                yield return act.actor.StartCoroutine(
                    act.actor.PlaySkillAnimationAndWait(
                        act.skill.animationType,
                        act.skill.animationDuration,
                        () => missAnimDone = true
                    )
                );
                yield return new WaitUntil(() => missAnimDone);
            }

            // Optional: MISS VFX here
            // SpawnMissVFX(act);

            yield return new WaitForSeconds(0.1f); // tiny buffer
            yield break; // now safe to advance queue
        }

        // 🔧 Play skill animation
        bool skillAnimationDone = false;
        if (act.actor != null)
        {
            yield return act.actor.StartCoroutine(
                act.actor.PlaySkillAnimationAndWait(act.skill.animationType, act.skill.animationDuration, () => skillAnimationDone = true)
            );
            yield return new WaitUntil(() => skillAnimationDone);
        }

        // 🔧 Spawn VFX for skill
        SpawnSkillVFX(act);

        // 🔊 Play skill SFX
        if (BattleAudioManager.Instance != null)
        {
            BattleAudioManager.Instance.PlaySFX(
                act.skill.castSFX,
                act.skill.sfxVolume
            );
        }

        // Apply skill effect
        // 🔧 Handle Escape skill separately
        if (act.skill is EscapeSkill escapeSkill)
        {
            yield return StartCoroutine(HandleEscapeSkill(act.actor, escapeSkill));
        }
        else
        {
            // Normal skill processing
            yield return StartCoroutine(ApplySkillEffect(act));
        }

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator ApplySkillEffect(TurnAction act)
    {
        if (act == null || act.actor == null || act.target == null || act.skill == null) yield break;

        // if (act.bodyPart == null || !act.bodyPart.IsFunctional)
        // {
        //     Debug.Log(
        //         $"⛔ Action canceled: {act.actor.characterName}'s {act.bodyPart?.partName ?? "UNKNOWN"} is no longer functional."
        //     );
        //     yield break;
        // }

        SkillBase skill = act.skill;

        // 🩹 Healing Skill
        if (skill.isHealingSkill)
        {
            // int mindPower = Mathf.Max(1, act.actor.currentMind);
            int healAmount = Random.Range(skill.minHeal, skill.maxHeal + 1);
            // healAmount += Mathf.RoundToInt(mindPower * 0.3f); // mind scaling

            if (skill.targetType == TargetType.EntireCharacter)
            {
                foreach (var part in act.target.bodyParts)
                {
                    // ✅ Revive blackout limbs
                    if (part.isBlackedOut)
                    {
                        part.RecoverFromBlackout(healAmount);
                    }
                    else if (part.currentHP > 0)
                    {
                        part.currentHP = Mathf.Min(part.maxHP, part.currentHP + healAmount);
                    }

                    Debug.Log($"{act.actor.characterName} healed {act.target.characterName}'s {part.partName} for {healAmount} HP.");
                    
                    // Show heal popup
                    if (act.target.damagePopupAnchor != null)
                        DamagePopupManager.Instance.ShowHeal(act.target.damagePopupAnchor.position, healAmount);
                }
            }
            else if (skill.targetType == TargetType.SingleBodyPart && act.targetPart != null)
            {
                var part = act.targetPart;

                // ✅ Revive blackout limb
                if (part.isBlackedOut)
                {
                    part.RecoverFromBlackout(healAmount);
                }
                else
                {
                    part.currentHP = Mathf.Min(part.maxHP, part.currentHP + healAmount);
                }

                Debug.Log($"{act.actor.characterName} healed {act.target.characterName}'s {part.partName} for {healAmount} HP.");
                
                // Show heal popup
                if (act.target.damagePopupAnchor != null)
                    DamagePopupManager.Instance.ShowHeal(act.target.damagePopupAnchor.position, healAmount);
            }

            yield break;
        }

        // 🛡️ Buff Skill
        if (skill.isBuffSkill)
        {
            ApplyBuff(act.target, skill);
            yield break;
        }

       // DOT skill
        if (skill.dotType != DOTType.None)
        {
            // If an explicit limb was targeted, only apply to that limb.
            if (act.targetPart != null)
            {
                var existing = act.target.activeEffects.Find(e => e.dotType == skill.dotType && e.targetPart == act.targetPart);
                if (existing != null)
                {
                    // refresh duration but do NOT stack
                    existing.remainingTurns = skill.dotDuration;
                    Debug.Log($"{skill.dotType} refreshed on {act.target.characterName}'s {act.targetPart.partName}");
                }
                else
                {
                    StatusEffect eff = new StatusEffect()
                    {
                        source = skill,
                        dotType = skill.dotType,
                        remainingTurns = skill.dotDuration,
                        targetPart = act.targetPart
                    };

                    act.target.activeEffects.Add(eff);
                    DamagePopupManager.Instance.ShowStatus(
                        act.target.transform.position,
                        skill.dotType.ToString()
                    );

                    Debug.Log($"{act.target.characterName}'s {act.targetPart.partName} afflicted with {skill.dotType} for {skill.dotDuration} turns");
                }
            }
            else
            {
                // No single limb targeted — skill targeted entire character.
                // Create one effect per limb (but don't stack same DOT on the same limb).
                foreach (var part in act.target.bodyParts)
                {
                    var existing = act.target.activeEffects.Find(e => e.dotType == skill.dotType && e.targetPart == part);
                    if (existing != null)
                    {
                        existing.remainingTurns = skill.dotDuration;
                        Debug.Log($"{skill.dotType} refreshed on {act.target.characterName}'s {part.partName}");
                    }
                    else
                    {
                        StatusEffect eff = new StatusEffect()
                        {
                            source = skill,
                            dotType = skill.dotType,
                            remainingTurns = skill.dotDuration,
                            targetPart = part
                        };
                        act.target.activeEffects.Add(eff);
                        DamagePopupManager.Instance.ShowStatus(
                            act.target.transform.position,
                            skill.dotType.ToString()
                        );
                        Debug.Log($"{act.target.characterName}'s {part.partName} afflicted with {skill.dotType} for {skill.dotDuration} turns");
                    }
                }
            }
            yield break;
        }

        // Debuff skill
        if (skill.isDebuffSkill)
        {
            // Build a StatusEffect where the stat deltas are NEGATIVE (so adding them reduces stats)
            StatusEffect eff = new StatusEffect()
            {
                source = skill,
                isDebuff = true,
                remainingTurns = skill.debuffDuration,

                // store NEGATIVE deltas so `part.attack += eff.atk` reduces the stat
                atk  = -Mathf.Abs(skill.attackDebuff),
                matk = -Mathf.Abs(skill.magicAttackDebuff),   // fixed field name
                def  = -Mathf.Abs(skill.defenseDebuff),
                mdef = -Mathf.Abs(skill.magicDefenseDebuff),
                agi  = -Mathf.Abs(skill.agilityDebuff),
            };

            // Apply immediate stat changes (apply same deltas to current stats)
            foreach (var part in act.target.bodyParts)
            {
                part.attack += eff.atk;
                part.magicAttack += eff.matk;
                part.defense += eff.def;
                part.magicDefense += eff.mdef;
            }

            act.target.agility += eff.agi;

            // Add to active effects list so it can be removed later
            act.target.activeEffects.Add(eff);

            Debug.Log($"{act.target.characterName} is inflicted with DEBUFF from {skill.skillName} for {skill.debuffDuration} turns!");
            yield break;
        }

        // 💥 Damage Skill
        int attackPower, defense;

        if (skill.isMagic)
        {
            attackPower = act.target.ResolveStat(act.bodyPart.magicAttack);
            defense = (act.targetPart != null ? act.target.ResolveStat(act.targetPart.magicDefense) : 0);
        }
        else
        {
            attackPower = act.actor.ResolveStat(act.bodyPart.attack);
            defense = (act.targetPart != null ? act.target.ResolveStat(act.targetPart.defense) : 0);
        }

        float baseDamage = (attackPower * attackPower) / (float)(attackPower + defense + 1);
        baseDamage *= skill.power;

        bool isCritical = CheckCritical(act.actor);
        if (isCritical) baseDamage *= 1.5f;

        int finalDamage = Mathf.Max(0, Mathf.RoundToInt(baseDamage));

        // ===== NEW BRANCH =====
        if (skill.targetType == TargetType.EntireCharacter)
        {
            foreach (var part in act.target.bodyParts)
            {
                if (!CheckHitOnPart(act, part))
                    continue; // limb evaded → no damage, no VFX
                    
                act.target.TakeDamage(part, finalDamage, skill, skill.isMagic);
                TryApplyStatusEffect(act, part);
                TryApplyDebuff(act);

                if (finalDamage > 0 && CameraShake.Instance != null)
                {
                    float strength = Mathf.Clamp(finalDamage * 0.02f, 0.15f, 0.5f);
                    CameraShake.Instance.Shake(0.15f, strength);
                }
            }
        }
        else // single part
        {
            act.target.TakeDamage(act.targetPart, finalDamage, skill, skill.isMagic);
            TryApplyStatusEffect(act, act.targetPart);
            TryApplyDebuff(act);

            if (finalDamage > 0 && CameraShake.Instance != null)
            {
                float strength = Mathf.Clamp(finalDamage * 0.02f, 0.15f, 0.5f);
                CameraShake.Instance.Shake(0.15f, strength);
            }
        }

        // 🩸 Check for death after applying damage
        if (!act.target.IsAlive)
        {
            act.target.TriggerDeathAnimation();
            Debug.Log($"{act.target.characterName} died mid-turn.");

            actionQueue.Clear();
            state = CombatState.Finished;

            if (act.target.isPlayerControlled)
            {
                Debug.Log("☠ Player defeated. Game Over.");
                StartCoroutine(ReturnToOverworld());
            }
            else
            {
                Debug.Log("🎉 Enemy defeated. Victory!");
                if (EnemyDataContainer.Instance != null)
                    EnemyDataContainer.Instance.lastEnemyDefeated = true;

                StartCoroutine(ReturnToOverworld());
            }

            yield break;
        }

        // 🩸 Play "TakeDamage" animation and wait
        if (act.target != null && act.target.IsAlive)
            yield return act.target.StartCoroutine(act.target.PlayTakeDamageAnimationAndWait(0.6f));
    }

    private void TryApplyStatusEffect(TurnAction act, BodyPart targetPart)
    {
        SkillBase skill = act.skill;
        if (!skill.canApplyStatus) return;
        if (skill.statusDOTType == DOTType.None) return;
        if (targetPart == null) return;

        float roll = Random.Range(0f, 100f);
        if (roll > skill.statusApplyChance)
        {
            Debug.Log(
                $"🩸 Status Proc FAILED: {skill.skillName} → {skill.statusDOTType} | Roll={roll:F1}"
            );
            return;
        }

        var existing = act.target.activeEffects.Find(e =>
            e.dotType == skill.statusDOTType &&
            e.targetPart == targetPart
        );

        if (existing != null)
        {
            existing.remainingTurns = skill.statusDuration;

            DamagePopupManager.Instance.ShowStatus(
                act.target.transform.position,
                skill.dotType.ToString()
            );
            
            Debug.Log(
                $"🩸 {skill.statusDOTType} refreshed on {act.target.characterName}'s {targetPart.partName}"
            );
        }
        else
        {
            StatusEffect eff = new StatusEffect()
            {
                source = skill,
                dotType = skill.statusDOTType,
                remainingTurns = skill.statusDuration,
                targetPart = targetPart
            };

            act.target.activeEffects.Add(eff);

            DamagePopupManager.Instance.ShowStatus(
                act.target.transform.position,
                skill.dotType.ToString()
            );

            Debug.Log(
                $"🩸 {skill.skillName} applied {skill.statusDOTType} to {act.target.characterName}'s {targetPart.partName} " +
                $"({skill.statusDuration} turns)"
            );
        }
    }

    private void TryApplyDebuff(TurnAction act)
    {
        SkillBase skill = act.skill;

        if (!skill.canApplyDebuff) return;

        float roll = Random.Range(0f, 100f);
        if (roll > skill.debuffApplyChance)
        {
            Debug.Log(
                $"🧪 Debuff Proc FAILED: {skill.skillName} | Roll={roll:F1}"
            );
            return;
        }

        StatusEffect eff = new StatusEffect()
        {
            source = skill,
            isDebuff = true,
            remainingTurns = skill.debuffDuration,

            atk  = -Mathf.Abs(skill.attackDebuff),
            matk = -Mathf.Abs(skill.magicAttackDebuff),
            def  = -Mathf.Abs(skill.defenseDebuff),
            mdef = -Mathf.Abs(skill.magicDefenseDebuff),
            agi  = -Mathf.Abs(skill.agilityDebuff),
        };

        // Apply immediately
        foreach (var part in act.target.bodyParts)
        {
            part.attack += eff.atk;
            part.magicAttack += eff.matk;
            part.defense += eff.def;
            part.magicDefense += eff.mdef;
        }

        act.target.agility += eff.agi;

        act.target.activeEffects.Add(eff);

        Debug.Log(
            $"🧪 {skill.skillName} applied DEBUFF to {act.target.characterName} " +
            $"({skill.debuffDuration} turns)"
        );
        DamagePopupManager.Instance.ShowStatus(
            act.target.transform.position,
            "Weakened"
        );
    }

    private bool CheckCritical(CharacterCombat attacker)
    {
        float critChance = 5f + (attacker.luck * 0.5f);  // Example formula
        return Random.Range(0f, 100f) < critChance;
    }

    private bool CheckHit(TurnAction act)
    {
        // 🎯 SPECIAL HEAD EVASION RULE
        if (act.targetPart != null && act.targetPart.limbType == LimbType.Head)
        {
            bool hasFunctionalLeg = act.target.bodyParts.Exists(p =>
                (p.limbType == LimbType.LeftLeg || p.limbType == LimbType.RightLeg) &&
                p.IsFunctional
            );

            if (hasFunctionalLeg)
            {
                float roll = Random.Range(0f, 100f);
                bool hit = roll >= 95f; // 95% evasion → 5% hit chance

                Debug.Log(
                    $"🧠 Head Evasion Check: Legs OK → 95% EVADE | Roll={roll} → {(hit ? "🎯 HIT" : "💨 EVADED")}"
                );

                return hit;
            }
        }

        // === NORMAL HIT CALCULATION ===
        float skillAccuracy = act.skill.accuracy;
        float attackerAccuracy = act.actor.accuracy;
        float defenderEvasion = Mathf.Max(1, act.target.evasion);

        float finalHitChance = skillAccuracy * (attackerAccuracy / defenderEvasion);
        finalHitChance = Mathf.Clamp(finalHitChance, 5f, 100f);

        float rollNormal = Random.Range(0f, 100f);
        bool normalHit = rollNormal < finalHitChance;

        Debug.Log(
            $"🎯 Hit Check: SkillAcc={skillAccuracy}, AttAcc={attackerAccuracy}, DefEva={defenderEvasion} → Final={finalHitChance}% | Roll={rollNormal} → {(normalHit ? "✅ HIT" : "❌ MISS")}"
        );

        return normalHit;
    }

    private bool CheckHitOnPart(TurnAction act, BodyPart targetPart)
    {
        // 🧠 SPECIAL HEAD EVASION RULE
        if (targetPart.limbType == LimbType.Head)
        {
            bool hasFunctionalLeg = act.target.bodyParts.Exists(p =>
                (p.limbType == LimbType.LeftLeg || p.limbType == LimbType.RightLeg) &&
                p.IsFunctional
            );

            if (hasFunctionalLeg)
            {
                float roll = Random.Range(0f, 100f);
                bool hit = roll >= 95f; // 95% evasion

                Debug.Log(
                    $"🧠 Head Evasion Check: Legs OK → 95% EVADE | Roll={roll} → {(hit ? "🎯 HIT" : "💨 EVADED")}"
                );

                return hit;
            }
        }

        // === NORMAL HIT CALCULATION ===
        float skillAccuracy = act.skill.accuracy;
        float attackerAccuracy = act.actor.accuracy;
        float defenderEvasion = Mathf.Max(1, act.target.evasion);

        float finalHitChance = skillAccuracy * (attackerAccuracy / defenderEvasion);
        finalHitChance = Mathf.Clamp(finalHitChance, 5f, 100f);

        float rollNormal = Random.Range(0f, 100f);
        bool hitNormal = rollNormal < finalHitChance;

        Debug.Log(
            $"🎯 Hit Check ({targetPart.partName}): Final={finalHitChance}% | Roll={rollNormal} → {(hitNormal ? "✅ HIT" : "❌ MISS")}"
        );

        return hitNormal;
    }

    bool HasFunctionalLegs(CharacterCombat target)
    {
        bool leftLeg = target.bodyParts.Exists(p =>
            p.limbType == LimbType.LeftLeg && p.IsFunctional);

        bool rightLeg = target.bodyParts.Exists(p =>
            p.limbType == LimbType.RightLeg && p.IsFunctional);

        return leftLeg && rightLeg;
    }

    private void ApplyBuff(CharacterCombat target, SkillBase skill)
    {
        if (target == null || skill == null) return;

        // Go through each buff field and apply if non-zero
        foreach (var part in target.bodyParts)
        {
            // For simplicity, we buff every body part equally
            part.attack += skill.attackBuff;
            part.magicAttack += skill.magicAttackBuff;
            part.defense += skill.defenseBuff;
            part.magicDefense += skill.magicDefenseBuff;
        }

        // Buff overall stats
        target.agility += skill.agilityBuff;
        

        Debug.Log($"{target.characterName} received buffs from {skill.skillName}: " +
                $"+Atk {skill.attackBuff}, +MAtk {skill.magicAttackBuff}, +Def {skill.defenseBuff}, +MDef {skill.magicDefenseBuff}, +Agi {skill.agilityBuff}");

        // If it’s temporary, start coroutine to remove after duration
        if (skill.buffDuration > 0)
        {
            StartCoroutine(RemoveBuffAfterDuration(target, skill, skill.buffDuration));
        }
    }

    private IEnumerator RemoveBuffAfterDuration(CharacterCombat target, SkillBase skill, int turns)
    {
        int currentRound = roundNumber;
        while (roundNumber < currentRound + turns)
            yield return null;

        foreach (var part in target.bodyParts)
        {
            part.attack -= skill.attackBuff;
            part.magicAttack -= skill.magicAttackBuff;
            part.defense -= skill.defenseBuff;
            part.magicDefense -= skill.magicDefenseBuff;
        }

        target.agility -= skill.agilityBuff;

        Debug.Log($"{target.characterName}'s {skill.skillName} buff has expired after {turns} turns.");
    }

    private IEnumerator HandleEscapeSkill(CharacterCombat actor, EscapeSkill escapeSkill)
    {
        if (actor == null) yield break;

        Debug.Log($"[Escape] {actor.characterName} is attempting to escape...");

        // 🧠 Drain ALL AP
        actor.currentAP = 0;

        // 🔢 Calculate chance
        int playerAgi = actor.agility;
        int enemyAgi = 0;

        // For now we assume one main enemy — you can adjust this if multi-enemy later
        var enemies = GetEnemies(actor);
        if (enemies.Count > 0)
            enemyAgi = enemies[0].agility;

        float baseChance = 50f;
        float diff = playerAgi - enemyAgi;
        float escapeChance = Mathf.Clamp(baseChance + diff * escapeSkill.agilityDifferenceScale, escapeSkill.minChance, escapeSkill.maxChance);

        // 🎲 Roll the result
        float roll = Random.Range(0f, 100f);
        bool success = roll <= escapeChance;

        Debug.Log($"[Escape] Chance: {escapeChance}% | Roll: {roll:F1} | Success: {success}");

        if (success)
        {
            Debug.Log($"[Escape] ✅ {actor.characterName} successfully escaped!");
            state = CombatState.Finished;
            actionQueue.Clear();
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(ReturnToOverworld());
            yield break;
        }
        else
        {
            Debug.Log($"[Escape] ❌ {actor.characterName} failed to escape!");
            yield return new WaitForSeconds(0.5f);
            // proceed to enemy turn (nothing special needed)
        }
    }

    private bool CheckEndCombat()
    {
        int alivePlayers = combatants.FindAll(c => c.isPlayerControlled && c.IsAlive).Count;
        int aliveEnemies = combatants.FindAll(c => !c.isPlayerControlled && c.IsAlive).Count;

        if (alivePlayers == 0)
        {
            Debug.Log("☠ All players defeated. Game Over.");
            return true;
        }

        if (aliveEnemies == 0)
        {
            Debug.Log("🎉 All enemies defeated. Victory!");

            // Persist the last enemy id saved by OverworldBattleTrigger (legacy flow)
            if (EnemyDataContainer.Instance != null && !string.IsNullOrEmpty(EnemyDataContainer.Instance.lastEnemyID))
            {
                if (WorldStateManager.Instance != null)
                    WorldStateManager.Instance.SetFlag("ENEMY_" + EnemyDataContainer.Instance.lastEnemyID, true);

                // still mark container flag too (keeps previous logic)
                EnemyDataContainer.Instance.lastEnemyDefeated = true;
            }

            // ✅ Return to overworld
            StartCoroutine(ReturnToOverworld());

            return true;
        }
        return false;
    }

    // 🔧 New helper: check mid-turn deaths
    private bool HasCombatEndedMidTurn()
    {
        int alivePlayers = combatants.FindAll(c => c.isPlayerControlled && c.IsAlive).Count;
        int aliveEnemies = combatants.FindAll(c => !c.isPlayerControlled && c.IsAlive).Count;

        if (alivePlayers == 0)
        {
            Debug.Log("☠ All players defeated mid-turn. Game Over.");
            actionQueue.Clear();
            state = CombatState.Finished;
            StartCoroutine(ReturnToOverworld()); // optional if you want same behavior
            return true;
        }

        if (aliveEnemies == 0)
        {
            Debug.Log("🎉 All enemies defeated mid-turn. Victory!");
            actionQueue.Clear();
            state = CombatState.Finished;

            if (EnemyDataContainer.Instance != null)
            {
                EnemyDataContainer.Instance.lastEnemyDefeated = true;

                if (WorldStateManager.Instance != null)
                    WorldStateManager.Instance.SetFlag("ENEMY_" + EnemyDataContainer.Instance.lastEnemyID, true);
            }

            StartCoroutine(ReturnToOverworld());
            return true;
        }

        return false;
    }

    private IEnumerator ReturnToOverworld()
    {
        foreach (var c in combatants)
            c.isInCombat = false;

        // Wait for victory text
        yield return new WaitForSeconds(3f);

        // --- NEW: Save full post-battle state ---
        SavePlayerStateAfterBattle();

        // --- Load the original overworld scene dynamically ---
        string sceneToLoad = ScenePositionManager.Instance.overworldSceneName;
        if (string.IsNullOrEmpty(sceneToLoad))
            sceneToLoad = "Overworld 2D";

        SceneManager.LoadScene(sceneToLoad);
    }

    private void SavePlayerStateAfterBattle()
    {
        // --- Find player combat data ---
        CharacterCombat player = combatants.Find(c => c.isPlayerControlled);
        if (player == null)
        {
            Debug.LogWarning("[Battle] No player found to save state!");
            return;
        }

        // --- 1. SAVE COMBAT STATS (HP, limbs, equipment, etc) ---
        CharacterDataContainer.Instance.SaveCharacter(player);

        // --- 2. SAVE INVENTORY ---
        Inventory inv = player.GetComponent<Inventory>();
        if (inv != null)
        {
            InventoryDataContainer.Instance.SaveInventory(inv);
        }

        // --- 3. SAVE MIND/HUNGER (using your PlayerGameState) ---
        PlayerGameState.CurrentMind = player.currentMind;
        PlayerGameState.Save();

        Debug.Log("[Battle] Player state saved after battle.");
    }

    private void SpawnSkillVFX(TurnAction act)
    {
        var data = act.skill;
        if (data.vfxPrefab == null) return;

        // decide where to place the effect(s)
        if (data.spawnOnCaster && act.actor != null)
        {
            var vfx = Instantiate(data.vfxPrefab, act.actor.transform.position, Quaternion.identity);
            Destroy(vfx, data.vfxLifetime);
        }

        if (data.spawnOnTarget && act.target != null)
        {
            var vfx = Instantiate(data.vfxPrefab, act.target.transform.position, Quaternion.identity);
            Destroy(vfx, data.vfxLifetime);
        }
    }

    private void SpawnItemVFX(TurnAction act)
    {
        if (act.item == null) return;
        var data = act.item;

        // 🧴 Spawn VFX on caster
        if (data.spawnOnCaster && act.actor != null && data.vfxPrefab != null)
        {
            var vfx = Instantiate(data.vfxPrefab, act.actor.transform.position, Quaternion.identity);
            Destroy(vfx, data.vfxLifetime);
        }

        // 🧴 Spawn VFX on target
        if (data.spawnOnTarget && act.target != null && data.vfxPrefab != null)
        {
            var vfx = Instantiate(data.vfxPrefab, act.target.transform.position, Quaternion.identity);
            Destroy(vfx, data.vfxLifetime);
        }
    }

    void FocusPlayerCamera()
    {
        var player = combatants.Find(c => c.isPlayerControlled);
        if (player != null && BattleCameraController.Instance != null)
        {
            BattleCameraController.Instance.FocusOn(player.cameraFocusPoint);
        }
    }

    void FocusBattlefieldCamera()
    {
        if (BattleCameraController.Instance != null)
            BattleCameraController.Instance.FocusBattlefield();
    }
}
