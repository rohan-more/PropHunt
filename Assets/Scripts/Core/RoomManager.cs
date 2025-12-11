using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using Photon.Realtime;
using Random = UnityEngine.Random;

namespace Core
{
    
    public enum PlayerType {PROP, HUNTER}
    
    public class RoomManager : MonoBehaviourPunCallbacks
    {
        public static RoomManager Instance;
        public PhotonView _photonView;
        public Dictionary<MyPlayer, PlayerType> PlayerList;
        private GameObject spawnPositions;
        public PlayerType _playerType;
        public bool _forcePlayerType;
        private List<Vector3> playerPositions = new List<Vector3>();
        void Awake()
        {
            if(Instance)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;

            PlayerList = new Dictionary<MyPlayer, PlayerType>();
        }

        public PlayerType GetPlayerType(string playerName)
        {
            return PlayerList.Keys.Where(player => player.NickName == playerName).Select(player => player.Type).FirstOrDefault();
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
            if(scene.buildIndex == 2) // We're in the game scene
            {
                if(PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "ScoreManager"), Vector3.one,
                        Quaternion.identity);
                }
                GetSpawnPositions();
                CreateController();
            }
        }

        private void GetSpawnPositions()
        {
            spawnPositions = GameObject.Find("PlayerSpawnPoints");
            foreach (Transform child in spawnPositions.transform)
            {
                playerPositions.Add(child.transform.position);
            }
            
        }
        
        void CreateController()
        {
            List<Player> players = PhotonNetwork.PlayerList.ToList();
            Dictionary<MyPlayer, PlayerType>.KeyCollection playerKeys = RoomManager.Instance.PlayerList.Keys;

            foreach (var item in playerKeys)
            {
                RoomManager.Instance.PlayerList.TryGetValue(item, out PlayerType type);
                if (item.IsLocal)
                {
                    if (item.Type != PlayerType.PROP)
                    {
                        CreateSeekers();
                    }
                    else
                    {
                        CreateHider();
                    }
                }
            }
            Cursor.lockState = CursorLockMode.Locked;
        }

        void CreateHider()
        {
            int randomIndex = UnityEngine.Random.Range(0, playerPositions.Count);
            Vector3 spawnPos = playerPositions[randomIndex];
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "TP_Player"), spawnPos, Quaternion.identity);
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "TP_Camera"), spawnPos, Quaternion.identity);
        }

        void CreateSeekers()
        {
            int randomIndex = UnityEngine.Random.Range(0, playerPositions.Count);
            Vector3 spawnPos = playerPositions[randomIndex];
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "FP_Player_Rigged"), spawnPos, Quaternion.identity);
        }
        
    }
}
