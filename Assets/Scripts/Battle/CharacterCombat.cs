using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using RPG.Combat;

public enum CharacterClass
{
    None,
    Knight,
    Mage,
    Archer,
    Paladin
}

[System.Serializable]
public class OverallStats
{
    public int mind = 20;
    public int luck = 0;
    public int agility = 10;

    public int evasion = 0;
    public int accuracy = 100;

    // CLONE CONSTRUCTOR
    public OverallStats(OverallStats other)
    {
        mind = other.mind;
        luck = other.luck;
        agility = other.agility;
        evasion = other.evasion;
        accuracy = other.accuracy;
    }

    // Empty constructor (Unity uses this)
    public OverallStats() { }
}

public class CharacterCombat : MonoBehaviour
{
    public Transform cameraFocusPoint;

    [Header("Identity")]
    public string characterName = "Combatant";
    public CharacterClass characterClass = CharacterClass.Knight; // new
    public bool isPlayerControlled = false;
    public bool isInCombat;

    [HideInInspector] public PlayerHunger playerHunger;
    public float hungerMaxHPModifier = 1f;

    [Header("Mind Resource")]
    public float currentMind;
    public float maxMind;

    [Header("Overall Stats")]
    public OverallStats overallStats;

    public List<StatusEffect> activeEffects = new List<StatusEffect>();

    // shortcuts
    public int mind      { get => overallStats.mind;      set => overallStats.mind = value; }
    public int agility   { get => overallStats.agility;   set => overallStats.agility = value; }
    public int luck      { get => overallStats.luck;      set => overallStats.luck = value; }
    public int evasion   { get => overallStats.evasion;   set => overallStats.evasion = value; }
    public int accuracy  { get => overallStats.accuracy;  set => overallStats.accuracy = value; }

    [Header("Action Points")]
    public int maxAP = 6;
    public int currentAP = 0;

    [Header("Body Parts")]
    public List<BodyPart> bodyParts = new List<BodyPart>();
    
    [SerializeField] private Animator animator;

    [Header("Inventory")]
    public Inventory inventory;

    public Transform damagePopupAnchor;

    public bool IsAlive
    {
        get
        {
            var head = bodyParts.Find(p => p.partName == "Head");
            var torso = bodyParts.Find(p => p.partName == "Torso");
            if (head == null || torso == null) return true;
            return head.currentHP > 0 && torso.currentHP > 0;
        }
    }

    void Awake()
    {
        CaptureInnateSkills();
        InitializeParts();
        ResetAP();
        playerHunger = GetComponent<PlayerHunger>();
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        Debug.Log($"[CharacterCombat] Animator found on {name}: {animator.runtimeAnimatorController.name}");
    }

    public void InitializeParts()
    {
        if (bodyParts == null) return;

        foreach (var p in bodyParts)
        {
            // If the saved base values are present, keep them; otherwise, initialize from the current inspector values
            if (p.baseMaxHP == 0 && p.maxHP != 0)
                p.baseMaxHP = p.maxHP;
            else if (p.baseMaxHP != 0)
                p.maxHP = p.baseMaxHP; // ensure runtime maxHP matches the stored base

            if (p.baseAttack == 0 && p.attack != 0)
                p.baseAttack = p.attack;
            else if (p.baseAttack != 0)
                p.attack = p.baseAttack;

            if (p.baseMagicAttack == 0 && p.magicAttack != 0)
                p.baseMagicAttack = p.magicAttack;
            else if (p.baseMagicAttack != 0)
                p.magicAttack = p.baseMagicAttack;

            if (p.baseDefense == 0 && p.defense != 0)
                p.baseDefense = p.defense;
            else if (p.baseDefense != 0)
                p.defense = p.baseDefense;

            if (p.baseMagicDefense == 0 && p.magicDefense != 0)
                p.baseMagicDefense = p.magicDefense;
            else if (p.baseMagicDefense != 0)
                p.magicDefense = p.baseMagicDefense;

            // innate skills store (unchanged)
            if (p.innateLimbSkills == null || p.innateLimbSkills.Count == 0)
            {
                p.innateLimbSkills = new List<SkillBase>();
                if (p.limbSkills != null)
                {
                    foreach (var s in p.limbSkills)
                        if (s != null)
                            p.innateLimbSkills.Add(s);
                }
            }

            // clamp
            p.currentHP = Mathf.Clamp(p.currentHP, 0, p.maxHP);

            p.character = this;
        }
    }

    private void CaptureInnateSkills()
    {
        if (bodyParts == null) return;

        foreach (var p in bodyParts)
        {
            // If innateLimbSkills is empty, copy the inspector-provided limbSkills into innate.
            // This preserves the original (designer) skill set.
            if (p.innateLimbSkills == null || p.innateLimbSkills.Count == 0)
            {
                p.innateLimbSkills = new List<SkillBase>();
                if (p.limbSkills != null)
                {
                    foreach (var s in p.limbSkills)
                        if (s != null) p.innateLimbSkills.Add(s);
                }
            }
        }
    }

    public void ResetAP()
    {
        currentAP = maxAP;
        foreach (var p in bodyParts)
            p.ResetTurnUsage();
    }

    public int ResolveStat(int baseValue)
    {
        float multiplier = 1f;

        // ONLY player suffers hunger
        if (isPlayerControlled)
        {
            switch (PlayerGameState.CurrentHungerStage)
            {
                case HungerStage.Hunger:
                    multiplier *= 0.8f;
                    break;
                case HungerStage.GreaterHunger:
                    multiplier *= 0.5f;
                    break;
            }
        }

        // future buffs/debuffs later

        return Mathf.RoundToInt(baseValue * multiplier);
    }

    public void ApplyHungerHPClamp()
    {
        if (!isPlayerControlled) return;

        foreach (var p in bodyParts)
        {
            int oldMax = p.maxHP;
            int newMax = p.EffectiveMaxHP;

            if (oldMax != newMax)
            {
                float ratio = (float)p.currentHP / oldMax;
                p.maxHP = newMax;
                p.currentHP = Mathf.RoundToInt(newMax * ratio);
            }

            p.currentHP = Mathf.Clamp(p.currentHP, 0, newMax);
        }
    }

    public void TakeDamage(BodyPart targetPart, int damage, SkillBase sourceSkill = null, bool isMagic = false)
    {
        if (targetPart == null) return;

        targetPart.currentHP -= damage;
        targetPart.currentHP = Mathf.Clamp(targetPart.currentHP, 0, targetPart.EffectiveMaxHP);
        Debug.Log($"{characterName}'s {targetPart.partName} took {damage} damage. Remaining HP: {targetPart.currentHP}");

        if (DamagePopupManager.Instance != null)
        {
            Vector3 pos = damagePopupAnchor != null ? damagePopupAnchor.position : this.transform.position;
            DamagePopupManager.Instance.ShowDamage(pos, damage);
        }

        // 🧠 Check for limb destruction or blackout
        if (targetPart.currentHP <= 0)
        {
            if (targetPart.limbType == LimbType.Head || targetPart.limbType == LimbType.Torso)
            {
                // Head or torso destruction = death
                Debug.Log($"{characterName} has died (head or torso destroyed).");
                TriggerDeathAnimation();
                return;
            }

            // Only limbs below
            bool canDestroy = sourceSkill != null && sourceSkill.canDestroyLimb;
            if (canDestroy)
            {
                Debug.Log($"{characterName}'s {targetPart.partName} was destroyed by {sourceSkill.skillName}!");
                // Handle destruction logic (disable, remove, etc.)
                targetPart.isBlackedOut = false; // just ensure it's not marked as blackout
                // Optionally mark it as "destroyed" if you track that
            }
            else
            {
                // Limb enters blackout
                targetPart.EnterBlackout();
            }
        }
    }

    public void PlayTakeDamageAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger("TakeDamage");
    }

    public IEnumerator PlayTakeDamageAnimationAndWait(float fallbackDuration)
    {
        if (animator == null)
            yield break;

        string stateName = "TakeDamage";
        int layer = 0;
        int stateHash = Animator.StringToHash(stateName);

        // Play the animation
        if (animator.HasState(layer, stateHash))
            animator.Play(stateHash, layer, 0f);
        else
            animator.SetTrigger(stateName);

        yield return null;

        // Wait for animation to start
        float timer = 0f;
        float maxWait = 1f;
        while (timer < maxWait)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(stateName)) break;
            timer += Time.deltaTime;
            yield return null;
        }

        // Wait for animation to finish or fallback
        float elapsed = 0f;
        float maxDuration = Mathf.Max(0.5f, fallbackDuration);
        while (animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName) &&
            animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f &&
            elapsed < maxDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Return to idle if available
        int idleHash = Animator.StringToHash("Idle");
        if (animator.HasState(layer, idleHash))
            animator.Play(idleHash, layer, 0f);
    }

    public BodyPart GetRandomFunctionalBodyPart()
    {
        var list = bodyParts.FindAll(p => p.IsFunctional);
        if (list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    public void ApplyItemEffect(ItemData item, BodyPart userPart, CharacterCombat targetChar, BodyPart targetPart)
    {
        if (item == null) return;
        CharacterCombat actualTarget = targetChar ?? this;

        // 🩹 Healing
        if (item.healAmount > 0)
        {
            if (item.targetWholeBody)
            {
                foreach (var p in actualTarget.bodyParts)
                {
                    if (p.isBlackedOut)
                    {
                        p.RecoverFromBlackout(item.healAmount);
                    }
                    else
                    {
                        p.currentHP = Mathf.Min(p.maxHP, p.currentHP + item.healAmount);
                    }
                }
                
                // Show heal popup
                if (actualTarget.damagePopupAnchor != null)
                    DamagePopupManager.Instance.ShowHeal(actualTarget.damagePopupAnchor.position, item.healAmount);
            }
            else if (targetPart != null)
            {
                if (targetPart.isBlackedOut)
                {
                    targetPart.RecoverFromBlackout(item.healAmount);
                }
                else
                {
                    targetPart.currentHP = Mathf.Min(targetPart.maxHP, targetPart.currentHP + item.healAmount);
                }
                
                // Show heal popup
                if (actualTarget.damagePopupAnchor != null)
                    DamagePopupManager.Instance.ShowHeal(actualTarget.damagePopupAnchor.position, item.healAmount);
            }
        }

        // 🧠 Mind restore (respects overworld or combat)
        if (item.mindRestore > 0)
        {
            if (!actualTarget.isInCombat) // overworld
            {
                if (actualTarget.TryGetComponent<PlayerMind>(out var mindSystem))
                {
                    mindSystem.RestoreMind(item.mindRestore);
                }
            }
            else // combat
            {
                actualTarget.currentMind = Mathf.Clamp(actualTarget.currentMind + item.mindRestore, 0, actualTarget.maxMind);
            }
        }

        if (item.hungerRestore > 0)
        {
            // only restore if not in battle
            if (actualTarget.TryGetComponent<PlayerHunger>(out var hungerSystem))
            {
                if (hungerSystem != null)
                {
                    // only if overworld (no combat)
                    if (!actualTarget.isInCombat) // <-- we use your combat flag here
                    {
                        hungerSystem.AddHunger(item.hungerRestore);
                        Debug.Log($"[Hunger] Restored {item.hungerRestore} hunger");
                    }
                    else
                    {
                        Debug.Log($"[Hunger] Cannot eat during combat!");
                    }
                }
            }
        }

        // 💥 Damage
        if (item.damageAmount > 0)
        {
            if (item.targetWholeBody)
            {
                foreach (var p in actualTarget.bodyParts)
                    actualTarget.TakeDamage(p, item.damageAmount, null, false);
            }
            else if (targetPart != null)
            {
                actualTarget.TakeDamage(targetPart, item.damageAmount, null, false);
            }
        }

        // ☠️ Status cures (DOT removal)
        if (item.curesPoison || item.curesBurn || item.curesBleed || item.curesAllDOT)
        {
            for (int i = actualTarget.activeEffects.Count - 1; i >= 0; i--)
            {
                var eff = actualTarget.activeEffects[i];

                // Skip non-DOT effects
                if (eff.dotType == DOTType.None)
                    continue;

                // Match DOT type
                bool typeMatch =
                    item.curesAllDOT ||
                    (item.curesPoison && eff.dotType == DOTType.Poison) ||
                    (item.curesBurn && eff.dotType == DOTType.Burn) ||
                    (item.curesBleed && eff.dotType == DOTType.Bleed);

                if (!typeMatch)
                    continue;

                // Match target scope
                bool targetMatch =
                    item.targetWholeBody ||
                    (targetPart != null && eff.targetPart == targetPart);

                if (!targetMatch)
                    continue;

                Debug.Log($"🧪 Removed {eff.dotType} from {actualTarget.characterName}" +
                        $"{(eff.targetPart != null ? $" ({eff.targetPart.partName})" : "")}");

                actualTarget.activeEffects.RemoveAt(i);
            }
        }

        Debug.Log($"{(userPart != null ? userPart.partName : "Unknown limb")} used {item.itemName} " +
                $"on {actualTarget.characterName}" +
                $"{(item.targetWholeBody ? " (whole body)" : targetPart != null ? $"'s {targetPart.partName}" : "")}");
    }

    public void ProcessStatusEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var eff = activeEffects[i];

            // DOT handling
            if (eff.dotType != DOTType.None)
            {
                // Prefer the exact limb the DOT was applied to
                BodyPart part = eff.targetPart;

                // Fallback: attempt to find by limbType if reference was lost, else choose torso / random
                if (part == null)
                {
                    // try to match by limbType if saved there (if you store limbType instead)
                    // otherwise prefer torso if functional, else pick random functional part
                    part = bodyParts.Find(p => p.partName == "Torso" && p.IsFunctional) ?? GetRandomFunctionalBodyPart();
                }

                if (part != null)
                {
                    float percent = 0f;
                    if (eff.dotType == DOTType.Poison) percent = 0.10f;
                    else if (eff.dotType == DOTType.Burn) percent = 0.02f;
                    else if (eff.dotType == DOTType.Bleed) percent = 0.05f;

                    int dmg = Mathf.Max(1, Mathf.RoundToInt(part.maxHP * percent));
                    // apply damage and pass eff.source so TakeDamage can react (destroy vs blackout)
                    TakeDamage(part, dmg, eff.source, eff.source != null && eff.source.isMagic);
                }
            }

            // decrement and expire
            eff.remainingTurns--;

            if (eff.remainingTurns <= 0)
            {
                // remove debuff stat edits if any
                if (eff.isDebuff)
                {
                    foreach (var part in bodyParts)
                    {
                        part.attack -= eff.atk;
                        part.magicAttack -= eff.matk;
                        part.defense -= eff.def;
                        part.magicDefense -= eff.mdef;
                    }

                    agility -= eff.agi;
                }

                activeEffects.RemoveAt(i);
            }
        }
    }

    public void UseItem(ItemData item, BodyPart userPart, CharacterCombat targetChar, BodyPart targetPart)
    {
        if (item.useType == ItemUseType.CombatOnly && !isInCombat)
        {
            Debug.LogWarning($"{item.itemName} can only be used in combat!");
            return;
        }
        if (item.useType == ItemUseType.OverworldOnly && isInCombat)
        {
            Debug.LogWarning($"{item.itemName} can only be used outside combat!");
            return;
        }
        if (item == null || userPart == null) return;

        if (currentAP < item.apCost)
        {
            Debug.LogWarning("[UseItem] Not enough AP");
            return;
        }

        // 🧾 Make sure we have an inventory and the item is available
        if (inventory == null)
        {
            Debug.LogWarning("[UseItem] No inventory found on this character!");
            return;
        }

        int qty = inventory.GetQuantity(item);
        if (qty <= 0)
        {
            Debug.LogWarning($"[UseItem] Cannot use {item.itemName} — no remaining quantity!");
            return;
        }

        // ✅ Apply the item effect
        ApplyItemEffect(item, userPart, targetChar, targetPart);

        // 🧨 Consume 1 item from inventory
        bool removed = inventory.Remove(item, 1);
        if (!removed)
        {
            Debug.LogWarning($"[UseItem] Failed to remove item {item.itemName} from inventory!");
        }
        else
        {
            Debug.Log($"[UseItem] {item.itemName} used. Remaining: {inventory.GetQuantity(item)}");
        }

        // 🧠 Consume AP and mark limb used
        currentAP = Mathf.Max(0, currentAP - item.apCost);
        userPart.usedThisTurn = true;
    }

    public bool CanEquipItem(EquipmentData item)
    {
        switch (characterClass)
        {
            case CharacterClass.Mage:
                // Mage cannot equip Two-Handed weapons or heavy armor
                if (item.weaponHandType == WeaponHandType.TwoHanded)
                    return false;
                if (item.equipmentType == EquipmentType.Armor && item.defenseBonus > 10)
                    return false;
                break;

            case CharacterClass.Archer:
                // Archer cannot equip Two-Handed weapons but can use everything else
                if (item.weaponHandType == WeaponHandType.TwoHanded)
                    return false;
                break;

            case CharacterClass.Knight:
                // Knight can equip anything
                return true;

            case CharacterClass.Paladin:
                // Paladin can equip anything too
                return true;
        }

        return true;
    }

    public bool EquipItem(EquipmentData item)
    {
        if (item == null)
            return false;

        // Class restriction
        if (!CanEquipItem(item))
            return false;

        // Weapon-specific logic
        if (item.equipmentType == EquipmentType.Weapon)
        {
            bool isTwoHanded = item.weaponHandType == WeaponHandType.TwoHanded;

            var left = bodyParts.Find(p => p.limbType == LimbType.LeftArm);
            var right = bodyParts.Find(p => p.limbType == LimbType.RightArm);

            if (isTwoHanded)
            {
                // Must have both arms functional
                if (left == null || right == null || !left.IsFunctional || !right.IsFunctional)
                    return false;

                // Unequip both arms first
                UnequipItem(LimbType.LeftArm, EquipmentType.Weapon);
                UnequipItem(LimbType.RightArm, EquipmentType.Weapon);

                right.EquipItem(item);
                return true;
            }
        }

        // Normal equipment
        BodyPart part = bodyParts.Find(p => p.limbType == item.limbSlot);
        if (part == null)
            return false;

        if (!part.CanEquip(item))
            return false;

        part.EquipItem(item);
        return true;
    }

    // Returns the unequipped EquipmentData (or null)
    public EquipmentData UnequipItem(LimbType limbType, EquipmentType slotType)
    {
        var limb = bodyParts.Find(p => p.limbType == limbType);
        if (limb == null) return null;

        switch (slotType)
        {
            case EquipmentType.Armor: return limb.UnequipSlot(limb.armorSlot);
            case EquipmentType.Weapon: return limb.UnequipSlot(limb.weaponSlot);
            case EquipmentType.Accessory: return limb.UnequipSlot(limb.accessorySlot);
        }
        return null;
    }

    private bool TwoHandedEquipped()
    {
        var left = bodyParts.Find(p => p.limbType == LimbType.LeftArm);
        var right = bodyParts.Find(p => p.limbType == LimbType.RightArm);

        return (left.weaponSlot != null && left.weaponSlot.equippedItem != null &&
            left.weaponSlot.equippedItem.weaponHandType == WeaponHandType.TwoHanded)
            || (right.weaponSlot != null && right.weaponSlot.equippedItem != null &&
            right.weaponSlot.equippedItem.weaponHandType == WeaponHandType.TwoHanded);
    }

    public void PlaySkillAnimation(SkillAnimationType type)
    {
        if (animator == null) return;

        switch (type)
        {
            case SkillAnimationType.MeleeAttack:
                animator.SetTrigger("MeleeAttack");
                break;
            case SkillAnimationType.WeaponAttack:
                animator.SetTrigger("WeaponAttack");
                break;    
            case SkillAnimationType.CastSpell:
                animator.SetTrigger("CastSpell");
                break;
            case SkillAnimationType.Buff:
                animator.SetTrigger("Buff");
                break;
        }
    }

    public IEnumerator PlaySkillAnimationAndWait(SkillAnimationType type, float fallbackDuration, System.Action onAnimationComplete)
    {
        if (animator == null)
        {
            Debug.LogError($"[CharacterCombat] Animator reference missing on {name}!");
            onAnimationComplete?.Invoke();
            yield break;
        }

        string stateName = type switch
        {
            SkillAnimationType.MeleeAttack => "MeleeAttack",
            SkillAnimationType.WeaponAttack => "WeaponAttack",
            SkillAnimationType.CastSpell => "CastSpell",
            SkillAnimationType.Buff => "Buff",
            _ => ""
        };

        int layer = 0;
        int stateHash = Animator.StringToHash(stateName);

        // --- Play or Trigger the Animation ---
        if (animator.HasState(layer, stateHash))
        {
            animator.Play(stateHash, layer, 0f);
        }
        else
        {
            animator.ResetTrigger(stateName); // clean old state
            animator.SetTrigger(stateName);
        }

        // ✅ Wait a frame to ensure Animator updates its state
        yield return null;

        // --- Wait for entry ---
        float waitToEnter = 0f;
        float maxWaitToEnter = 1.0f;
        bool entered = false;

        while (waitToEnter < maxWaitToEnter)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(stateName))
            {
                entered = true;
                break;
            }
            waitToEnter += Time.deltaTime;
            yield return null;
        }

        if (!entered)
        {
            // Debug.LogWarning($"[CharacterCombat] Animator did not enter state '{stateName}' in time. Using fallback wait {fallbackDuration:F2}s.");
            yield return new WaitForSeconds(fallbackDuration);
            onAnimationComplete?.Invoke();
            yield break;
        }

        // --- Wait for completion ---
        float elapsed = 0f;
        float maxStateDuration = Mathf.Max(0.1f, fallbackDuration * 3f);
        while (animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName) &&
            animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f &&
            elapsed < maxStateDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- Return to Idle if exists ---
        int idleHash = Animator.StringToHash("Idle");
        if (animator.HasState(layer, idleHash))
        {
            animator.Play(idleHash, layer, 0f);
        }

        onAnimationComplete?.Invoke();
    }

    public void PlayItemAnimation(ItemAnimationType type)
    {
        if (animator == null) return;

        switch (type)
        {
            case ItemAnimationType.UseItem:
                animator.SetTrigger("UseItem");
                break;
        }
    }

    public IEnumerator PlayItemAnimationAndWait(ItemAnimationType type, float fallbackDuration, System.Action onAnimationComplete)
    {
        if (animator == null)
        {
            onAnimationComplete?.Invoke();
            yield break;
        }

        string stateName = type switch
        {
            ItemAnimationType.UseItem => "UseItem",
            _ => "UseItem"
        };

        int layer = 0;
        int stateHash = Animator.StringToHash(stateName);

        if (animator.HasState(layer, stateHash))
            animator.Play(stateHash, layer, 0f);
        else
            animator.SetTrigger(stateName);

        yield return null;

        float waitToEnter = 0f;
        float maxWaitToEnter = 1.0f;
        bool entered = false;

        while (waitToEnter < maxWaitToEnter)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(stateName))
            {
                entered = true;
                break;
            }
            waitToEnter += Time.deltaTime;
            yield return null;
        }

        if (!entered)
        {
            yield return new WaitForSeconds(fallbackDuration);
            onAnimationComplete?.Invoke();
            yield break;
        }

        float elapsed = 0f;
        float maxStateDuration = Mathf.Max(0.1f, fallbackDuration * 3f);
        while (animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName) &&
            animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f &&
            elapsed < maxStateDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        int idleHash = Animator.StringToHash("Idle");
        if (animator.HasState(layer, idleHash))
            animator.Play(idleHash, layer, 0f);

        onAnimationComplete?.Invoke();
    }

    private bool hasPlayedDeathAnimation = false;

    public void TriggerDeathAnimation()
    {
        if (hasPlayedDeathAnimation || animator == null) return;

        hasPlayedDeathAnimation = true;
        Debug.Log($"☠ {characterName} is dead! Playing death animation.");

        animator.SetTrigger("Die");
    }

    public IEnumerator PlayDeathAnimationAndWait(float waitTime = 1.5f)
    {
        if (hasPlayedDeathAnimation || animator == null) yield break;

        hasPlayedDeathAnimation = true;
        animator.SetTrigger("Die");

        // Wait for the animation to begin
        yield return new WaitForSeconds(waitTime);
    }

    public void LoadInventoryFromContainer()
    {
        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
            if (inventory == null)
            {
                inventory = gameObject.AddComponent<Inventory>();
            }
        }

        // Load inventory data from container
        if (InventoryDataContainer.Instance != null)
        {
            InventoryDataContainer.Instance.LoadInto(inventory);
            Debug.Log($"[CharacterCombat] {characterName}'s inventory loaded with {inventory.slots.Count} items.");
        }
        else
        {
            Debug.LogWarning("[CharacterCombat] No InventoryDataContainer found to load inventory from.");
        }
    }
}
