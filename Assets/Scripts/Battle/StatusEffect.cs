using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// wherever StatusEffect is declared
public class StatusEffect
{
    public SkillBase source;
    public DOTType dotType = DOTType.None; // (you already have this)
    public bool isDebuff = false;
    public int remainingTurns = 0;

    // debuff stat deltas (if used)
    public int atk, matk, def, mdef, agi, mind;

    // NEW: the specific body part this effect applies to (may be null for whole-character effects)
    public BodyPart targetPart;
}
