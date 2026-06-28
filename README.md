# Damnare-Prototype/Demo
The Code Design Documentation for Damnare

<img src="https://github.com/user-attachments/assets/1dac9800-41d3-4dfb-b0f1-e7abcf1a6b75"/>
<!-- About the game -->
<td width="70%" valign="top" style="padding:15px;">
  <h2>About </h2>
  <p style="max-width:700px;">
    Damnare is a turn based RPG with limb mechanics referenced from the game Fear and Hunger. The game takes in other RPG elements and mish mash them into 1 game. This game was solo developed by myself so all of the assets used are not mine. The game is still in demo/prototype. (Development duration 3 months+)
  </p>
  <a href="https://lemun8.itch.io/damnare-demo">
    <img src="https://img.shields.io/badge/Itch.io-FA5C5C?style=for-the-badge&logo=itch.io&logoColor=white" />
  </a>
</td>


##  Scripts and Features
Here are some of the main script that is used to manage the game.
<br>
**Core Combat System**:
|  Script       | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| `CharacterCombat.cs` | Core character component managing all combat-related data and mechanics. Manages character stats (HP, Mind, AP, Agility, Luck, Evasion, Accuracy), Maintains body part system (limb-based combat), Handles equipment (weapons, armor, accessories per limb), etc. |
| `CombatManager.cs` | Orchestrates turn-based combat flow and manages all combat interactions. Combat state management (Setup, PlayerPlanning, Execution, Victory, Defeat), Turn order calculation based on Agility, Action execution (attacks, skills, items, healing, buffs, debuffs), etc. |
| `BodyPart.cs`  | Represents individual body parts in the limb-based combat system. Individual HP tracking per limb, Equipment slots (weapon, armor, accessory), Limb-specific skills, etc. |
| `PlayerActionPlanner.cs`  | Manages player action selection and planning during combat. Guides player through action selection workflow, Manages UI state transitions (body part → action type → skill/item → target), Queues player actions for execution, etc. |
<br>

<br> **Player Systems Script**:
|  Script       | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| `PlayerMind.cs` | Manages player's Mind resource in the overworld. Mind decay over time (configurable rate per minute), Persistence via PlayerGameState, Dynamic max Mind based on character class (Mage: 150, others: 100), etc. |
| `PlayerHunger.cs` | Implements hunger mechanics affecting player survival. Hunger decay over time, Hunger stage management (Normal, Hunger, Greater Hunger), HP penalty application based on hunger level, etc. |
| `PlayerGameState.cs`  | Static runtime state manager for player data persistence. Save/load player Mind and Hunger to PlayerPrefs, Character state initialization per class, Scene-to-scene data persistence, etc. |
<br>

<br> **Overworld & Scene Management**:
|  Script       | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| `OverworldSceneManager.cs` | Manages overworld scene setup and player spawning. Player character instantiation based on selected class, Spawn point management, Player state application (Mind, Hunger, HP), etc. |
| `OverworldSkillExecutor.cs` | Executes skills outside of combat (healing). Skill execution in overworld context, Mind cost deduction, Healing skill application to all body parts, etc |
| `EnemyOverworldAI.cs` | Controls enemy behavior in the overworld. Patrol and chase AI states, Player detection, Combat initiation, etc. |
<br>

<br> **UI Systems Script**:
|  Script       | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| `CombatUIManager.cs` | Renders and manages combat UI using OnGUI. Display action selection menus (body parts, skills, items, targets), Show player AP and Mind in real-time, Display status effects on player and enemies, etc. |
| `OverworldMenuManager.cs` | Full-featured pause menu for overworld gameplay. Tab-based navigation (Stats, Items, Equipment, Skills, Settings, Exit), Real-time stat display (HP, Mind, Hunger), Inventory management (use items), etc. |
| `DamagePopupManager.cs` | Displays floating damage/healing numbers and status text. Render damage numbers (red), Render healing numbers (green with + prefix), Show miss/evade messages, etc. |
| `NotificationManager.cs` | Displays temporary on-screen notifications. Queue-based notification system, Auto-dismiss after timeout, Visual feedback for game events (hunger stage changes, item usage, etc). |
<br>

<br> **Data Systems Script**:
|  Script       | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| `SkillBase.cs` | Defines all skill data as ScriptableObjects. Skill name, description, AP and Mind costs, Damage range (min/max), etc. |
| `ItemData.cs` | Defines consumable items. Item name, description, AP cost to use, Heal amount, etc. |
| `EquipmentData.cs` | Defines weapons, armor, and accessories. Equipment name, type (Weapon, Armor, Accessory), Stat modifiers (ATK, MATK, DEF, MDEF), Limb slot assignment, etc. |
| `CharacterPrefabLibrary.cs` | Maps character classes to their prefabs. Centralized character prefab registry, Used by scene managers to instantiate correct character, Supports character selection flow. |
<br>

<br> **Inventory System Script**:
|  Script       | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| `Inventory.cs` | Manages player inventory with items and equipment. Add/remove items with quantity tracking, Stacking system, Slot-based storage, Query inventory for specific items, Support for both ItemData and EquipmentData. |
<br>

<br> **Supporting Systems Script**:
|  Script       | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| `StatusEffect.cs` | Represents active buffs, debuffs, and DOTs. Effect type (buff/debuff), Stat modifiers (ATK, MATK, DEF, MDEF, AGI), DOT type (Bleed, Burn, Poison), etc. |
| `EquipmentSlot.cs` | Container for equipped items on body parts. Hold reference to equipped item, Slot type identification, Equip/unequip operations, etc. |
| `TurnAction.cs` | Represents a queued combat action. Actor (who performs the action), Skill or Item to use, Target character and body part, etc. |
<br>

##  System Design
Other than individual scripts, the game relies on several interconnected systems to handle several mechanics. Below is an overview of how the core mechanics are engineered.

#### 1. Limb-Based Combat Architecture
Instead of treating characters as single HP entities, the combat system implements a granular body part system where each limb is an independent combat unit.

*   **How it works:** Each CharacterCombat contains a list of BodyPart components. Every body part maintains its own HP, equipment slots (weapon/armor/accessory), and skill list. During combat, players select which limb to use for actions and which enemy limb to target.
*   **Blackout Mechanic:** When a body part's HP reaches zero, it enters a "blackout" state—unable to perform actions or equip items until healed. This creates tactical depth: targeting enemy weapon arms disables their attacks, while protecting your own limbs becomes critical.
*   **Equipment Integration:** Each BodyPart has three EquipmentSlot instances. Stat calculations in CharacterCombat.ResolveStat() aggregate base stats + equipment bonuses + active status effects, allowing per-limb stat modifiers to affect overall combat effectiveness.
  
#### 2. Turn-Based Action Queue & Execution Pipeline
The combat flow separates player planning from execution, using a queue-based system to handle all actions in proper turn order.

*   **Planning Phase:** PlayerActionPlanner.cs guides the player through a multi-step selection process (body part → action type → skill/item → target), validating AP and Mind costs at each step. Actions are stored as TurnAction objects in a queue.
*   **Execution Phase:** Once planning ends, CombatManager.cs processes each queued action sequentially via coroutines, handling accuracy checks, damage calculations, status effect applications, and UI feedback (damage popups, animations).
*   **Turn Order System:** Combat participants are sorted by Agility stat at the start of each round. Higher Agility characters act first, with the order recalculated each turn to account for stat changes from buffs/debuffs.

#### 3. Resource Management Triad: HP, Mind, and Hunger
The game implements three interconnected resource systems that create strategic depth in both combat and exploration.

*   **HP (Health Points):** Distributed across body parts. Total character HP is the sum of all limb HP values. Damage targets specific limbs, and healing can restore individual parts or the whole body.
*   **Mind Resource:** Managed by PlayerMind.cs in the overworld and CharacterCombat.currentMind in combat. Magic skills consume Mind, which decays over time in the overworld at a configurable rate. Max Mind is character-specific (Mage: 150, others: 100).
*   **Hunger System:** PlayerHunger.cs tracks hunger decay over time, transitioning through stages (Normal → Hunger → Greater Hunger). Low hunger applies an HP penalty via CharacterCombat.ApplyHungerHPClamp(), reducing effective max HP for all body parts. Starvation (hunger = 0) triggers game over.

#### 4. ScriptableObject-Driven Data Pipeline
Skills, items, and equipment are defined as ScriptableObjects rather than hardcoded classes, creating a data-driven architecture that separates design from implementation.

*   **How it works:** Designers create SkillBase, ItemData, and EquipmentData assets in the Unity Editor, configuring properties like damage ranges, AP costs, Mind costs, target types, and usage contexts (Combat/Overworld/Both).
*   **Runtime Usage:** When a skill is used, CombatManager.ExecuteSkillAction() reads the ScriptableObject's properties to determine behavior: Is it a damage skill? Healing skill? Buff skill? The same data structure supports both combat and overworld contexts.
*   **Flexibility:** The usageContext enum allows skills to work in combat only, overworld only, or both. For example, healing skills work everywhere, while attack skills are combat-only. OverworldSkillExecutor.cs and CombatManager.cs both consume the same SkillBase data.

#### 5. Dynamic Stat Resolution with Layered Modifiers
Character stats are calculated on-the-fly by aggregating multiple modifier sources, rather than storing final values.

*   **Calculation Layers:**
    -  Base Stats: Defined on BodyPart (attack, defense, etc.)
    -  Equipment Bonuses: Each equipped item adds stat modifiers
    -  Status Effects: Active buffs/debuffs apply temporary modifiers
    -  Hunger Penalty: Low hunger reduces effective max HP
*   **Implementation:** CharacterCombat.ResolveStat() accepts a base stat value and iterates through all active StatusEffect instances, summing their modifiers. Equipment stats are queried from EquipmentSlot.equippedItem on each body part.
*   **Example Flow:**
Final Attack = Base Attack (20)
              + Weapon Bonus (15)
              + Buff Effect (+5)
              + Debuff Effect (-3)
              = 37

#### 6. Persistent State Management Across Scenes
The game uses a static singleton pattern (PlayerGameState) combined with PlayerPrefs to maintain player state across scene transitions (Overworld ↔ Combat).

*   **State Flow:**
    -  Overworld: PlayerMind and PlayerHunger continuously update PlayerGameState.CurrentMind and PlayerGameState.CurrentHunger
    -  Scene Transition: Before loading combat, values are saved to PlayerPrefs via PlayerGameState.Save()
    -  Combat Scene: CombatManager loads values and applies them to the player's CharacterCombat.currentMind
    -  Return to Overworld: OverworldSceneManager.ApplySavedStateToPlayer() restores saved Mind/Hunger, and PlayerMind.Start() initializes from the loaded value
*   **Character-Specific Initialization:** PlayerGameState.InitializeMindForCharacter() sets Mind to the character's max value (from overallStats.mind) on new games, ensuring Mages start with 150 Mind while others start with 100.

#### 7. UI-Driven Validation and Feedback
The UI layer validates player actions before queuing them, providing instant visual feedback on invalid choices.

*   **Validation Points:**
    -  CombatUIManager grays out skills when the player lacks sufficient AP or Mind
    -  Body parts that are blacked out or already used show visual indicators [BLACKOUT] or [Used]
    -  Equipment manager disables "Equip" buttons when the player's class cannot use an item or no eligible limb exists
*   **Color-Coded Resources:**
    -  Mind display turns yellow (<50%), red (<25%)
    -  Hunger stages color-coded in notifications
    -  Damage popups (red), healing popups (green), miss/evade (gray/cyan)
*   **Real-Time Updates:** OnGUI() renders every frame while menus are open, so Mind decay and Hunger changes are visible in real-time without closing/reopening menus.

#### 8. Enemy AI Decision Tree
Enemy behavior in combat is driven by a weighted decision system that evaluates available options based on cost and context.

*   **Decision Flow:**
    -  CombatManager.PlanEnemyTurn() gathers all functional body parts
    -  For each limb, filter skills by AP cost and Mind requirements
    -  Randomly select a skill from affordable options
    -  Choose a random target from living enemies
    -  Select a random functional body part on the target
    -  Queue action for execution
*   **Randomization:** Enemy AI intentionally uses random selection to keep combat unpredictable. This prevents players from exploiting deterministic patterns.
*   **Constraint Handling:** If an enemy has no affordable skills (low AP/Mind), they skip their turn. Blacked-out limbs are excluded from selection.
