using UnityEngine;

[CreateAssetMenu(fileName = "CharacterPrefabLibrary", menuName = "Game/Character Prefab Library")]
public class CharacterPrefabLibrary : ScriptableObject
{
    [Header("Overworld Prefabs")]
    public GameObject knightOverworld;
    public GameObject mageOverworld;
    public GameObject archerOverworld;
    public GameObject paladinOverworld;

    [Header("Battle Prefabs")]
    public GameObject knightBattle;
    public GameObject mageBattle;
    public GameObject archerBattle;
    public GameObject paladinBattle;
}
