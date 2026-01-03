using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static ScoreManager Instance;

    [Header("Scoring Config")]
    [SerializeField] private ScoringConfig scoring; // ScriptableObject

    // actorNumber → score
    private Dictionary<int, int> scores = new();

    // prevent double scoring
    private HashSet<int> scoredDecoys = new();
    private HashSet<int> killedProps = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // ─────────────────────────────────────────
    // MASTER EVENT ROUTER
    // ─────────────────────────────────────────
    public void OnEvent(EventData photonEvent)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        switch (photonEvent.Code)
        {
            case GameplayEvents.PropHit:
                HandlePropHit((object[])photonEvent.CustomData);
                break;

            case GameplayEvents.DecoyDestroyed:
                HandleDecoyDestroyed((object[])photonEvent.CustomData);
                break;

            case GameplayEvents.RoundEnd:
                HandleRoundEnd();
                break;
        }
    }

    // ─────────────────────────────────────────
    // GAMEPLAY HANDLERS (MASTER ONLY)
    // ─────────────────────────────────────────

    void HandlePropHit(object[] data)
    {
        int hunterActor = (int)data[0];
        int viewID = (int)data[1];
        int damage = (int)data[2];

        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView == null)
        {
            return;
        }

        PropHealthComponent health = targetView.GetComponent<PropHealthComponent>();
        if (health == null || health.IsDead)
        {
            return;
        }

        health.ApplyDamage(damage);
    }

    public void OnPropKilled(int hunterActor, int propActor)
    {
        if (killedProps.Contains(propActor))
        {
            return;
        }

        killedProps.Add(propActor);
        AddScoreInternal(hunterActor, scoring.hunterKillProp);
    }

    void HandleDecoyDestroyed(object[] data)
    {
        int hunterActor = (int)data[0];
        int decoyViewID = (int)data[1];
 
        if (scoredDecoys.Contains(decoyViewID))
        {
            return;
        }

        scoredDecoys.Add(decoyViewID);
        AddScoreInternal(hunterActor, scoring.hunterDestroyDecoy);
    }

    void HandleRoundEnd()
    {
        // optional: round-end bonuses, freeze scores, snapshot, etc.
    }

    // ─────────────────────────────────────────
    // SCORE MUTATION (MASTER ONLY)
    // ─────────────────────────────────────────

    private void AddScoreInternal(int actorNumber, int points)
    {
        if (!scores.ContainsKey(actorNumber))
            scores[actorNumber] = 0;

        scores[actorNumber] += points;

        photonView.RPC(
            nameof(RPC_SyncScore),
            RpcTarget.All,
            actorNumber,
            scores[actorNumber]
        );
    }

    // ─────────────────────────────────────────
    // CLIENT SYNC / READ ONLY
    // ─────────────────────────────────────────

    [PunRPC]
    void RPC_SyncScore(int actorNumber, int newScore)
    {
        scores[actorNumber] = newScore;
    }

    public int GetScore(int actorNumber)
    {
        return scores.TryGetValue(actorNumber, out var score) ? score : 0;
    }

    public void ResetScores()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        scores.Clear();
        scoredDecoys.Clear();
        killedProps.Clear();

        photonView.RPC(nameof(RPC_Reset), RpcTarget.All);
    }

    [PunRPC]
    void RPC_Reset()
    {
        scores.Clear();
        scoredDecoys.Clear();
        killedProps.Clear();
    }
}
