using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using RPG.Combat; // for LimbType, EquipmentType, EquipmentData, etc.

public class OverworldMenuManager : MonoBehaviour, IPlayerDependency
{
    [Header("Player (auto-find if null)")]
    public CharacterCombat player;
    private Inventory playerInventory;
    private PlayerHunger playerHunger;
    private PlayerMind playerMind;

    [Header("Dev Equipment Pool (assign equipment ScriptableObjects here for testing)")]
    public List<EquipmentData> availableEquipments = new List<EquipmentData>();

    private OverworldSkillExecutor overworldSkillExecutor;

    private bool isOpen = false;
    private int selectedTab = 0;
    private readonly string[] tabs = new string[] { "Stats", "Items", "Equipment", "Skills", "Settings", "Exit" };

    #pragma warning disable CS0414
    private int selectedEquipmentIndex = -1;
    private LimbType selectedLimbForEquip = LimbType.RightArm;
    private EquipmentType selectedSlotType = EquipmentType.Weapon;
    #pragma warning restore CS0414

    [Header("Audio Settings")]
    public AudioMixer audioMixer;

    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";

    public void SetPlayer(GameObject playerObj)
    {
        player = playerObj.GetComponent<CharacterCombat>();

        playerInventory = playerObj.GetComponent<Inventory>();
        if (playerInventory == null)
            playerInventory = playerObj.AddComponent<Inventory>();

        playerHunger = playerObj.GetComponent<PlayerHunger>();
        playerMind = playerObj.GetComponent<PlayerMind>();

        overworldSkillExecutor = playerObj.GetComponent<OverworldSkillExecutor>();
        if (overworldSkillExecutor == null)
            overworldSkillExecutor = playerObj.AddComponent<OverworldSkillExecutor>();
    }

    void Start()
    {
        // Load saved options
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Don't touch the player if it's not assigned yet
        if (player == null)
            return;

        overworldSkillExecutor = player.GetComponent<OverworldSkillExecutor>();
        if (overworldSkillExecutor == null)
        {
            overworldSkillExecutor = player.gameObject.AddComponent<OverworldSkillExecutor>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        Time.timeScale = isOpen ? 0f : 1f;

        // Reset some UI state when opening
        if (isOpen)
        {
            selectedTab = 0;
            selectedEquipmentIndex = -1;
        }
    }

    void OnGUI()
    {
        if (!isOpen) return;

        float width = 520;
        float height = 520;
        float left = (Screen.width - width) / 2f;
        float top = (Screen.height - height) / 2f;

        GUIStyle header = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 14 };
        GUIStyle small = new GUIStyle(GUI.skin.label) { fontSize = 12 };

        GUILayout.BeginArea(new Rect(left, top, width, height), GUI.skin.window);
        GUILayout.Label("<b>OVERWORLD MENU</b>", header);

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        for (int i = 0; i < tabs.Length; i++)
        {
            if (GUILayout.Button(tabs[i], GUILayout.Width((width - 20) / tabs.Length)))
            {
                selectedTab = i;
                // Reset some states when switching tabs
                selectedEquipmentIndex = -1;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        switch (selectedTab)
        {
            case 0: DrawStatsTab(button, small); break;
            case 1: DrawItemsTab(button, small); break;
            case 2: DrawEquipmentTab(button, small); break;
            case 3: DrawSkillsTab(button, small); break;
            case 4: DrawSettingsTab(button, small); break;
            case 5: DrawExitTab(button, small); break;
        }

        GUILayout.FlexibleSpace();

        // Close menu quick button
        if (GUILayout.Button("Close Menu (Esc)", GUILayout.Height(30)))
        {
            ToggleMenu();
        }

        GUILayout.EndArea();
    }

    private Vector2 statsScroll;
    void DrawStatsTab(GUIStyle button, GUIStyle small)
    {
        if (player == null)
        {
            GUILayout.Label("No player assigned.", small);
            return;
        }

        if (player.bodyParts == null || player.bodyParts.Count == 0)
        {
            GUILayout.Label("(No body parts found)", small);
            return;
        }

        GUILayout.Label("<b>Overall Stats</b>", small);
        GUILayout.Space(4);

        // Calculate total HP from body parts
        int totalHP = 0;
        int currentHP = 0;
        foreach (var bp in player.bodyParts)
        {
            totalHP += bp.maxHP;
            currentHP += bp.currentHP;
        }

        // Display Overall Stats
        GUILayout.Label($"HP: {currentHP}/{totalHP}");

        // Mind (current / max)
        if (playerMind != null)
        {
            float maxMind = player.overallStats.mind;
            GUILayout.Label($"Mind: {playerMind.currentMind:F0}/{maxMind}");
        }
        else
        {
            GUILayout.Label($"Mind: {player.overallStats.mind}");
        }

        // Hunger (current / max)
        if (playerHunger != null)
        {
            GUILayout.Label($"Hunger: {playerHunger.currentHunger:F0}/{playerHunger.maxHunger} ({playerHunger.hungerStage})");
        }
        else if (player.isPlayerControlled)
        {
            string hungerStage = PlayerGameState.CurrentHungerStage.ToString();
            GUILayout.Label($"Hunger: {hungerStage}");
        }

        GUILayout.Label($"Luck: {player.overallStats.luck}");
        GUILayout.Label($"Agility: {player.overallStats.agility}");
        GUILayout.Label($"Evasion: {player.overallStats.evasion}");
        GUILayout.Label($"Accuracy: {player.overallStats.accuracy}");

        GUILayout.Space(8);
        GUILayout.Label("<b>Per-Limb Stats</b>", small);
        GUILayout.Space(4);

        statsScroll = GUILayout.BeginScrollView(statsScroll, GUILayout.Height(300));

        foreach (var bp in player.bodyParts)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>{bp.partName}</b> ({bp.limbType})");
            GUILayout.Label($"HP: {bp.currentHP}/{bp.EffectiveMaxHP}");
            GUILayout.Label($"Attack: {player.ResolveStat(bp.attack)}");
            GUILayout.Label($"Magic Attack: {player.ResolveStat(bp.magicAttack)}");
            GUILayout.Label($"Defense: {player.ResolveStat(bp.defense)}");
            GUILayout.Label($"Magic Defense: {player.ResolveStat(bp.magicDefense)}");
            GUILayout.Label($"Functional: {(bp.IsFunctional ? "✅" : "❌")}");
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        GUILayout.EndScrollView();
    }

    // ------------------------------------------
    // Items Tab
    // ------------------------------------------
    private Vector2 itemsScroll;
    void DrawItemsTab(GUIStyle button, GUIStyle small)
    {
        if (player == null)
        {
            GUILayout.Label("No player assigned.", small);
            return;
        }

        GUILayout.Label("<b>Inventory Items</b>", small);

        if (playerInventory == null || playerInventory.GetSlots().Count == 0)
        {
            GUILayout.Label("(No items in inventory)", small);
            return;
        }

        itemsScroll = GUILayout.BeginScrollView(itemsScroll, GUILayout.Height(330));

        ItemData toUse = null; // store which item we’ll use after loop

        foreach (var slot in playerInventory.GetSlots())
        {
            if (slot == null || slot.data == null) continue;
            if (!slot.IsItem) continue;

            var it = slot.AsItem();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{it.itemName} x{slot.quantity}", GUILayout.Width(220));
            GUILayout.Label($"AP:{it.apCost}", GUILayout.Width(50));

            if (GUILayout.Button("Use (Self)", button, GUILayout.Width(120)))
            {
                toUse = it;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        GUILayout.EndScrollView();

        // ✅ Perform item usage AFTER the loop
        if (toUse != null)
        {
            int qty = playerInventory.GetQuantity(toUse);
            if (qty > 0)
            {
                BodyPart targetPart = toUse.targetWholeBody ? null : player.GetRandomFunctionalBodyPart();
                player.ApplyItemEffect(toUse, null, player, targetPart);

                playerInventory.Remove(toUse, 1);
                Debug.Log($"[Menu] Used {toUse.itemName} on {player.characterName}");
            }
        }
    }

    // ------------------------------------------
    // Equipment Tab
    // ------------------------------------------
    private Vector2 equipScroll;
    private Vector2 equippedScroll;

    void DrawEquipmentTab(GUIStyle button, GUIStyle small)
    {
        if (player == null)
        {
            GUILayout.Label("No player assigned.", small);
            return;
        }

        // ========================================
        // SECTION 1: Currently Equipped Items
        // ========================================
        GUILayout.Label("<b>Currently Equipped</b>", small);
        GUILayout.Space(4);

        equippedScroll = GUILayout.BeginScrollView(equippedScroll, GUILayout.Height(150));

        foreach (var bodyPart in player.bodyParts)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>{bodyPart.partName}</b> ({bodyPart.limbType})", small);

            // Weapon Slot
            if (bodyPart.weaponSlot != null && !bodyPart.weaponSlot.IsEmpty)
            {
                var weapon = bodyPart.weaponSlot.equippedItem;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  🗡 {weapon.equipmentName}", GUILayout.Width(200));
                if (GUILayout.Button("Unequip", button, GUILayout.Width(80)))
                {
                    var removed = player.UnequipItem(bodyPart.limbType, EquipmentType.Weapon);
                    if (removed != null) playerInventory.Add(removed, 1);
                }
                GUILayout.EndHorizontal();
            }
            else if (bodyPart.limbType == LimbType.LeftArm || bodyPart.limbType == LimbType.RightArm)
            {
                GUILayout.Label("  🗡 (No weapon)", small);
            }

            // Armor Slot
            if (bodyPart.armorSlot != null && !bodyPart.armorSlot.IsEmpty)
            {
                var armor = bodyPart.armorSlot.equippedItem;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  🛡 {armor.equipmentName}", GUILayout.Width(200));
                if (GUILayout.Button("Unequip", button, GUILayout.Width(80)))
                {
                    var removed = player.UnequipItem(bodyPart.limbType, EquipmentType.Armor);
                    if (removed != null) playerInventory.Add(removed, 1);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("  🛡 (No armor)", small);
            }

            // Accessory Slot
            if (bodyPart.accessorySlot != null && !bodyPart.accessorySlot.IsEmpty)
            {
                var accessory = bodyPart.accessorySlot.equippedItem;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  💍 {accessory.equipmentName}", GUILayout.Width(200));
                if (GUILayout.Button("Unequip", button, GUILayout.Width(80)))
                {
                    var removed = player.UnequipItem(bodyPart.limbType, EquipmentType.Accessory);
                    if (removed != null) playerInventory.Add(removed, 1);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("  💍 (No accessory)", small);
            }

            GUILayout.EndVertical();
            GUILayout.Space(2);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(10);

        // ========================================
        // SECTION 2: Equipment Inventory
        // ========================================
        GUILayout.Label("<b>Equipment Inventory</b>", small);
        GUILayout.Space(4);

        if (playerInventory == null || playerInventory.GetSlots().Count == 0)
        {
            GUILayout.Label("(No equipment in inventory)", small);
            return;
        }

        EquipmentData toEquip = null;

        equipScroll = GUILayout.BeginScrollView(equipScroll, GUILayout.Height(150));

        foreach (var slot in playerInventory.GetSlots())
        {
            if (slot == null || slot.data == null) continue;
            if (!slot.IsEquipment) continue;

            var eq = slot.AsEquipment();

            bool canEquipClass = player.CanEquipItem(eq);
            LimbType limb = FindFirstEligibleLimbFor(player, eq);
            bool hasValidLimb = limb != (LimbType)(-1);

            bool canEquip = canEquipClass && hasValidLimb;

            GUILayout.BeginHorizontal();
            
            // Show equipment type icon
            string icon = eq.equipmentType == EquipmentType.Weapon ? "🗡" : 
                        eq.equipmentType == EquipmentType.Armor ? "🛡" : "💍";
            
            GUILayout.Label($"{icon} {eq.equipmentName} x{slot.quantity}", GUILayout.Width(220));

            GUI.enabled = canEquip;

            if (GUILayout.Button("Equip", GUILayout.Width(80)))
            {
                toEquip = eq;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
        
        GUILayout.EndScrollView();

        // ✅ Perform equip AFTER the loop
        if (toEquip != null)
        {
            if (!player.CanEquipItem(toEquip))
            {
                Debug.LogWarning($"[Equip] {player.characterClass} cannot equip {toEquip.equipmentName}");
                return;
            }

            LimbType target = FindFirstEligibleLimbFor(player, toEquip);
            if (target == (LimbType)(-1))
            {
                Debug.LogWarning("[Equip] No eligible limb found for " + toEquip.equipmentName);
                return;
            }

            // Two-handed weapon check
            if (toEquip.equipmentType == EquipmentType.Weapon &&
                toEquip.weaponHandType == WeaponHandType.TwoHanded)
            {
                var left = player.bodyParts.Find(p => p.limbType == LimbType.LeftArm);
                var right = player.bodyParts.Find(p => p.limbType == LimbType.RightArm);

                if (left == null || right == null || !left.IsFunctional || !right.IsFunctional)
                {
                    Debug.LogWarning("[Equip] Cannot equip two-handed weapon — both arms must be functional.");
                    return;
                }
            }

            // Unequip old equipment
            var oldEquip = player.UnequipItem(target, toEquip.equipmentType);
            if (oldEquip != null)
                playerInventory.Add(oldEquip, 1);

            // Equip new item
            var original = toEquip.limbSlot;
            toEquip.limbSlot = target;

            bool equipped = player.EquipItem(toEquip);

            toEquip.limbSlot = original;

            if (equipped)
            {
                playerInventory.Remove(toEquip, 1);
            }
            else
            {
                Debug.LogWarning("[Equip] Equip failed, item not removed from inventory.");
            }
        }
    }

    private LimbType FindFirstEligibleLimbFor(CharacterCombat c, EquipmentData eq)
    {
        if (c == null || eq == null) return (LimbType)(-1);

        // First pass: prefer an eligible limb whose slot is empty (so we don't force-unequip another limb)
        foreach (var bp in c.bodyParts)
        {
            if (!bp.CanEquip(eq)) continue;

            // For weapons, ensure the limb is actually an arm and functional
            if (eq.equipmentType == EquipmentType.Weapon)
            {
                if (bp.limbType != LimbType.LeftArm && bp.limbType != LimbType.RightArm) continue;
                if (!bp.IsFunctional) continue;
                if (bp.weaponSlot != null && bp.weaponSlot.IsEmpty) return bp.limbType;
            }
            else
            {
                // non-weapon slots: prefer empty
                EquipmentSlot slot = eq.equipmentType == EquipmentType.Armor ? bp.armorSlot : bp.accessorySlot;
                if (slot != null && slot.IsEmpty) return bp.limbType;
            }
        }

        // Second pass: no empty slot, pick the first eligible limb (same as before)
        foreach (var bp in c.bodyParts)
        {
            if (!bp.CanEquip(eq)) continue;

            if (eq.equipmentType == EquipmentType.Weapon)
            {
                if (bp.limbType != LimbType.LeftArm && bp.limbType != LimbType.RightArm) continue;
                if (!bp.IsFunctional) continue;
                return bp.limbType;
            }
            else
            {
                return bp.limbType;
            }
        }

        return (LimbType)(-1);
    }

    private bool IsLimbEligibleForEquip(LimbType limb, EquipmentData item)
    {
        if (item == null || player == null) return false;

        var bodyPart = player.bodyParts.Find(bp => bp.limbType == limb);
        if (bodyPart == null) return false;

        return bodyPart.CanEquip(item);
    }

    // ------------------------------------------
    // Skills Tab (basic placeholder — you should expand to pull from your skill lists)
    // ------------------------------------------
    private Vector2 skillsScroll;
    void DrawSkillsTab(GUIStyle button, GUIStyle small)
    {
        GUILayout.Label("<b>Overworld Skills</b>", small);

        if (player == null || overworldSkillExecutor == null)
        {
            GUILayout.Label("No player or skill executor found.", small);
            return;
        }

        // ✅ Collect all skills from body parts
        List<SkillBase> allSkills = new List<SkillBase>();
        foreach (var bodyPart in player.bodyParts)
        {
            if (bodyPart == null || bodyPart.limbSkills == null) continue;
            foreach (var skill in bodyPart.limbSkills)
            {
                if (skill != null && !allSkills.Contains(skill))
                    allSkills.Add(skill);
            }
        }

        // ✅ Filter only overworld-usable skills
        List<SkillBase> overworldSkills = new List<SkillBase>();
        foreach (var s in allSkills)
        {
            // Assuming you have an enum like SkillUsageContext.Overworld or .Both
            if (s.usageContext == SkillUsageContext.OverworldOnly || 
                s.usageContext == SkillUsageContext.Both)
            {
                overworldSkills.Add(s);
            }
        }

        if (overworldSkills.Count == 0)
        {
            GUILayout.Label("(No overworld-usable skills available)", small);
            return;
        }

        skillsScroll = GUILayout.BeginScrollView(skillsScroll, GUILayout.Height(340));
        SkillBase skillToUse = null;

        foreach (var skill in overworldSkills)
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label($"{skill.skillName}", GUILayout.Width(180));
            GUILayout.Label($"Mind Cost: {skill.mindCost}", GUILayout.Width(80));
            GUILayout.Label($"Usage: {skill.usageContext}", GUILayout.Width(100));

            if (GUILayout.Button("Use", button, GUILayout.Width(100)))
            {
                skillToUse = skill;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        GUILayout.EndScrollView();

        if (skillToUse != null)
        {
            overworldSkillExecutor.UseSkill(skillToUse);
        }
    }

    private System.Collections.IEnumerator TemporaryInvisibilityDev(float seconds)
    {
        var enemies = FindObjectsOfType<MonoBehaviour>(); // we don't know exact type in this scope; better to find EnemyOverworldAI
        var eAIs = FindObjectsOfType<MonoBehaviour>();
        // Prefer specific type if present
        var specific = FindObjectsOfType(typeof(MonoBehaviour));
        // For safety, try to find EnemyOverworldAI by name/type
        var enemyAIs = FindObjectsOfType<Component>();
        var set = FindObjectsOfType<MonoBehaviour>();
        // Attempt to toggle a common flag if your enemies expose it (e.g., canDetectPlayer)
        var enemyScripts = FindObjectsOfType<MonoBehaviour>();
        foreach (var es in enemyScripts)
        {
            var t = es.GetType();
            var field = t.GetField("canDetectPlayer");
            if (field != null)
            {
                field.SetValue(es, false);
            }
        }

        yield return new WaitForSecondsRealtime(seconds);

        foreach (var es in enemyScripts)
        {
            var t = es.GetType();
            var field = t.GetField("canDetectPlayer");
            if (field != null)
            {
                field.SetValue(es, true);
            }
        }

        Debug.Log("[Menu] Invisibility ended (dev)");
    }

    void DrawSettingsTab(GUIStyle button, GUIStyle small)
    {
        GUILayout.Label("<b>Audio Settings</b>", small);
        GUILayout.Space(10);

        // --- BGM ---
        GUILayout.Label($"BGM Volume: {bgmVolume:F2}", small);
        float newBgm = GUILayout.HorizontalSlider(bgmVolume, 0f, 1f);

        if (!Mathf.Approximately(newBgm, bgmVolume))
        {
            bgmVolume = newBgm;
            ApplyBGMVolume(bgmVolume);
        }

        GUILayout.Space(8);

        // --- SFX ---
        GUILayout.Label($"SFX Volume: {sfxVolume:F2}", small);
        float newSfx = GUILayout.HorizontalSlider(sfxVolume, 0f, 1f);

        if (!Mathf.Approximately(newSfx, sfxVolume))
        {
            sfxVolume = newSfx;
            ApplySFXVolume(sfxVolume);
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Reset Audio to Defaults", button))
        {
            bgmVolume = 1f;
            sfxVolume = 1f;

            ApplyBGMVolume(bgmVolume);
            ApplySFXVolume(sfxVolume);
        }
    }

    void ApplyBGMVolume(float value)
    {
        audioMixer.SetFloat(BGM_KEY, LinearToDecibel(value));
        PlayerPrefs.SetFloat(BGM_KEY, value);
        PlayerPrefs.Save();
    }

    void ApplySFXVolume(float value)
    {
        audioMixer.SetFloat(SFX_KEY, LinearToDecibel(value));
        PlayerPrefs.SetFloat(SFX_KEY, value);
        PlayerPrefs.Save();
    }

    float LinearToDecibel(float value)
    {
        if (value <= 0.0001f)
            return -80f; // silent
        return Mathf.Log10(value) * 20f;
    }

    void DrawExitTab(GUIStyle button, GUIStyle small)
    {
        GUILayout.Label("<b>Exit to Main Menu</b>", small);
        GUILayout.Label("Returning to main menu will lose unsaved progress (dev).", small);
        if (GUILayout.Button("Return to Main Menu", button))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main Menu");
        }
    }
}
