using System;
using UnityEngine;

public class TurnAction
{
    public CharacterCombat actor;
    public BodyPart bodyPart;
    public SkillBase skill;
    public ItemData item;          // NEW: item action support
    public CharacterCombat target;
    public BodyPart targetPart;
    public int priority;

    public bool IsItem => item != null;
    public bool IsSkill => skill != null;
}
