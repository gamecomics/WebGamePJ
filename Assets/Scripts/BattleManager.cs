using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using static PlayerState;

public class BattleManager : MonoBehaviourPunCallbacks
{
    public static BattleManager Instance;

    private Dictionary<int, PlayerState> playerStates = new Dictionary<int, PlayerState>();

    // 라운드가 진행 중인지, 액션 선택 대기 중인지 등
    private bool isRoundRunning = false;
    private int actionsReceived = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 방에 있는 플레이어들로 상태 초기화 (예: 기본체력 5)
        foreach (var p in PhotonNetwork.PlayerList)
        {
            playerStates[p.ActorNumber] = new PlayerState(p.ActorNumber, maxHp: 5);
        }

        StartNewRound();
    }

    public void StartNewRound()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        isRoundRunning = true;
        actionsReceived = 0;

        // 액션 초기화
        foreach (var kvp in playerStates)
        {
            kvp.Value.selectedAction = PlayerAction.None;
        }

        // 클라이언트들에게 "액션 선택 UI 열어라" 신호
        photonView.RPC(nameof(RPC_BeginRoundClient), RpcTarget.All);
    }

    [PunRPC]
    void RPC_BeginRoundClient()
    {
        Debug.Log("새 라운드 시작 - 액션 선택 UI 열기");
        // TODO: 각 클라이언트에서 버튼(UI) 활성화
    }

    // 각 플레이어가 액션을 선택하면 이 함수를 호출하게 하면 됨
    public void LocalPlayerSelectAction(PlayerAction action)
    {
        // 로컬에서 유효한 액션인지도 체크 (ex. 공격인데 ammo 0이면 막기 등)
        photonView.RPC(nameof(RPC_SubmitAction), RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber, (int)action);
    }

    [PunRPC]
    void RPC_SubmitAction(int actorNumber, int actionInt, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !isRoundRunning) return;

        if (!playerStates.ContainsKey(actorNumber)) return;

        var state = playerStates[actorNumber];
        var action = (PlayerAction)actionInt;

        // 서버(마스터)에서도 유효성 체크
        if (action == PlayerAction.Attack && state.ammo <= 0)
        {
            // 공격기회 없는데 공격 고르면 그냥 장전으로 처리하거나,
            // None으로 처리하는 등 정책 정하면 됨
            action = PlayerAction.Reload;
        }

        state.selectedAction = action;
        actionsReceived++;

        if (actionsReceived >= playerStates.Count)
        {
            // 둘 다 액션 제출 완료 → 라운드 판정
            ResolveRound();
        }
    }

    void ResolveRound()
    {
        isRoundRunning = false;

        // 플레이어 2명이라고 가정
        var players = new List<PlayerState>(playerStates.Values);
        var p1 = players[0];
        var p2 = players[1];

        ApplyActions(p1, p2);
        ApplyActions(p2, p1); // 순서와 관계없이 같은 규칙이면 두 번 호출해도 OK

        // 죽었는지 체크
        int winnerActor = -1;
        if (p1.IsDead && !p2.IsDead) winnerActor = p2.actorNumber;
        else if (!p1.IsDead && p2.IsDead) winnerActor = p1.actorNumber;
        else if (p1.IsDead && p2.IsDead) winnerActor = 0; // 비김 처리용

        // 클라들에게 결과 전송
        photonView.RPC(nameof(RPC_RoundResult), RpcTarget.All,
            p1.hp, p1.ammo, (int)p1.selectedAction,
            p2.hp, p2.ammo, (int)p2.selectedAction,
            winnerActor);

        if (winnerActor == -1)
        {
            // 아무도 안죽었으면 다음 라운드
            StartNewRound();
        }
        else
        {
            Debug.Log("게임 종료");
        }
    }

    void ApplyActions(PlayerState self, PlayerState enemy)
    {
        if (self.IsDead) return;

        switch (self.selectedAction)
        {
            case PlayerAction.Attack:
                if (self.ammo > 0)
                {
                    self.ammo--;

                    // 상대 액션에 따른 판정
                    if (enemy.selectedAction == PlayerAction.Dodge)
                    {
                        // 회피 성공 → 데미지 없음
                    }
                    else
                    {
                        // 공격 성공
                        enemy.hp -= 1;
                    }
                }
                break;

            case PlayerAction.Reload:
                self.ammo++;
                break;

            case PlayerAction.Dodge:
                // 적이 공격할 때만 의미 있으므로 여기는 아무 것도 안 해도 됨
                break;
        }
    }

    [PunRPC]
    void RPC_RoundResult(
        int p1Hp, int p1Ammo, int p1Action,
        int p2Hp, int p2Ammo, int p2Action,
        int winnerActor)
    {
        Debug.Log($"라운드 결과 - P1 HP:{p1Hp}, Ammo:{p1Ammo} / P2 HP:{p2Hp}, Ammo:{p2Ammo}");

        // TODO: UI 갱신 (체력바, 남은 탄수, 이번 라운드에 쓴 행동 등)

        if (winnerActor == -1)
        {
            // 다음 라운드 시작은 Master에서 StartNewRound()가 다시 호출
        }
        else
        {
            // 승패 UI
            if (winnerActor == 0)
            {
                Debug.Log("무승부!");
            }
            else if (winnerActor == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                Debug.Log("승리!");
            }
            else
            {
                Debug.Log("패배...");
            }
        }
    }
}
