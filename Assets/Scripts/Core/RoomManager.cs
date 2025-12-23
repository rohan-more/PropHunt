using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using Random = UnityEngine.Random;

namespace Core
{
    public enum PlayerType
    {
        PROP,
        HUNTER
    }

    public class RoomManager : MonoBehaviourPunCallbacks
    {
        public static RoomManager Instance;
        public PhotonView _photonView;
        private GameObject spawnPositions;

        private List<Vector3> playerPositions = new List<Vector3>();
        private bool hasSpawned = false; // prevent duplicate spawns
        private bool sceneReady = false;
        private bool joinedRoom = false;
        private bool playerTypeChosen = false;
        void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            
            Debug.Log("[RoomManager] Awake - Instance created");
        }
        
        public override void OnEnable()
        {
            base.OnEnable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.buildIndex != 2)
            {
                return;
            }
            
            if (SceneManager.GetActiveScene().buildIndex != 2)
            {
                Debug.LogError("Player instantiated outside Gameplay");
                return;
            }

            Debug.Log("[RoomManager] OnSceneLoaded - game scene loaded");
            sceneReady = true;
            playerPositions.Clear();
            GetSpawnPositions();
            TrySpawnLocalControllerIfReady();
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            Debug.Log("[RoomManager] OnJoinedRoom callback received");
            joinedRoom = true;
            //TrySpawnLocalControllerIfReady();
        }

// central spawn gate
        public void TrySpawnLocalControllerIfReady()
        {
            if (!sceneReady)
            {
                Debug.Log("[RoomManager] TrySpawnLocalControllerIfReady: scene not ready yet");
                return;
            }

            if (!joinedRoom)
            {
                Debug.Log("[RoomManager] TrySpawnLocalControllerIfReady: not joined room yet");
                return;
            }

            if (hasSpawned)
            {
                Debug.Log("[RoomManager] TrySpawnLocalControllerIfReady: already spawned");
                return;
            }

            Debug.Log("[RoomManager] Conditions met -> spawning local controller");
            CreateController();
        }

        private void GetSpawnPositions()
        {
            spawnPositions = GameObject.Find("PlayerSpawnPoints");
            if (spawnPositions == null)
            {
                Debug.LogError("[RoomManager] GetSpawnPositions: 'PlayerSpawnPoints' GameObject not found in scene.");
                return;
            }

            playerPositions.Clear();
            foreach (Transform child in spawnPositions.transform)
            {
                playerPositions.Add(child.position);
            }

            Debug.Log($"[RoomManager] GetSpawnPositions: found {playerPositions.Count} spawn positions.");
        }

        void CreateController()
        {
            if (hasSpawned)
                return;

            // get local Photon player
            Player photonLocal = PhotonNetwork.LocalPlayer;

            // find this player's type in your custom list
            PlayerType localType = PlayerType.HUNTER; // default fallback
            
            if (photonLocal.CustomProperties.TryGetValue(PlayerPropertyKeys.PlayerType, out object typeObj))
            {
                localType = (PlayerType)typeObj;
            }


            Debug.Log($"[RoomManager] Spawning local player. Type = {localType}");

            if (localType == PlayerType.PROP)
            {
                SpawnHider();
            }
            else
            {
                SpawnSeeker();
            }
            
            hasSpawned = true;
        }
        

        void SpawnHider()
        {
            if (playerPositions.Count == 0)
            {
                Debug.LogError("[RoomManager] SpawnHider: no spawn positions available.");
                return;
            }

            int randomIndex = Random.Range(0, playerPositions.Count);
            Vector3 spawnPos = playerPositions[randomIndex];
            // remove chosen spawn so others don't reuse same spot locally
            playerPositions.RemoveAt(randomIndex);

            Debug.Log($"[RoomManager] SpawnHider: Instantiating TP_Player and TP_Camera at {spawnPos}");
            SpawnNetworkPrefab("PhotonPrefabs/TP_Player", spawnPos, Quaternion.identity);
            SpawnNetworkPrefab("PhotonPrefabs/TP_Camera", spawnPos, Quaternion.identity);
        }

        void SpawnSeeker()
        {
            if (playerPositions.Count == 0)
            {
                Debug.LogError("[RoomManager] SpawnSeeker: no spawn positions available.");
                return;
            }

            int randomIndex = Random.Range(0, playerPositions.Count);
            Vector3 spawnPos = playerPositions[randomIndex];
            playerPositions.RemoveAt(randomIndex);

            Debug.Log($"[RoomManager] SpawnSeeker: Instantiating FP_Player_Rigged at {spawnPos}");
            SpawnNetworkPrefab("PhotonPrefabs/FP_Player_Rigged", spawnPos, Quaternion.identity);
        }

        static bool IsGameplayScene()
        {
            return SceneManager.GetActiveScene().buildIndex == 2;
        }

        GameObject SpawnNetworkPrefab(string resourcePath, Vector3 pos, Quaternion rot)
        {
            Debug.Log($"[Spawn] Attempt PhotonNetwork.Instantiate('{resourcePath}') at {pos}");
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !IsGameplayScene())
            {
                Debug.LogError("[Spawn] Photon not connected or not in room. Aborting instantiate.");
                return null;
            }

            GameObject go = null;
            try
            {
                go = PhotonNetwork.Instantiate(resourcePath, pos, rot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Spawn] PhotonNetwork.Instantiate threw: {ex}");
            }

            if (go == null)
            {
                Debug.LogError(
                    $"[Spawn] PhotonNetwork.Instantiate returned null for '{resourcePath}'. Check Resources folder and path.");
            }
            else
            {
                var pv = go.GetComponent<PhotonView>();
                Debug.Log(
                    $"[Spawn] Instantiated '{go.name}' ViewID={(pv != null ? pv.ViewID.ToString() : "no PV")} IsMine={(pv != null ? pv.IsMine.ToString() : "n/a")} Owner={(pv?.Owner?.NickName ?? "n/a")}");
            }

            return go;
        }
    }
}