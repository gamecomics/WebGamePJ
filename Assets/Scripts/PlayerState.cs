using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerState : MonoBehaviour
{
    public enum PlayerAction
    {
        None,
        Attack,
        Dodge,
        Reload
    }

    public int actorNumber;  // Photon 플레이어 식별용
    public int maxHp;
    public int hp;
    public int ammo;         // 장전된 공격 기회 수

    public PlayerAction selectedAction = PlayerAction.None;

    public bool IsDead => hp <= 0;

    public PlayerState(int actorNumber, int maxHp, int startAmmo = 0)
    {
        this.actorNumber = actorNumber;
        this.maxHp = maxHp;
        this.hp = maxHp;
        this.ammo = startAmmo;
    }
}
