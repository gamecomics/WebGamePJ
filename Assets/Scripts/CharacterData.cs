using UnityEngine;

[CreateAssetMenu(menuName = "Data/Character")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public int maxHp = 5;
    public int startAmmo = 1;
    public int attackDamage = 1;
}

