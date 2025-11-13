using UnityEngine;
using static PlayerState;

public class ActionButtonUI : MonoBehaviour
{
    public void OnAttackButton()
    {
        BattleManager.Instance.LocalPlayerSelectAction(PlayerAction.Attack);
    }

    public void OnDodgeButton()
    {
        BattleManager.Instance.LocalPlayerSelectAction(PlayerAction.Dodge);
    }

    public void OnReloadButton()
    {
        BattleManager.Instance.LocalPlayerSelectAction(PlayerAction.Reload);
    }
}
