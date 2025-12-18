using System.IO;
using Photon.Pun;
using UnityEngine;

namespace Core
{
    public class PropVisualController : MonoBehaviourPun
    {
        [SerializeField] private MeshFilter playerMesh;
        [SerializeField] private MeshRenderer playerMeshRenderer;

        [Header("Collider Settings")]
        [SerializeField] private bool useMeshCollider = true;
        [SerializeField] private bool colliderIsTrigger = false;

        [Header("Clone")]
        [SerializeField] private KeyCode cloneInput = KeyCode.LeftControl;

        private string currentMeshId;

        private void OnEnable()
        {
            Events.SelectedObjectType += SwapMesh;
            Events.SelectedObject += SwapMesh;
        }

        private void OnDisable()
        {
            Events.SelectedObjectType -= SwapMesh;
            Events.SelectedObject -= SwapMesh;
        }

        private void SwapMesh(int viewID, string meshName)
        {
            if (photonView.ViewID != viewID)
                return;

            ApplyMesh(meshName);
            photonView.RPC(nameof(RPC_ApplyMesh), RpcTarget.OthersBuffered, viewID, meshName);

            currentMeshId = meshName;
        }

        private void SwapMesh(int viewID, MeshName meshName)
        {
            if (photonView.ViewID != viewID)
                return;

            string meshId = meshName.ToString().ToLower();
            ApplyMesh(meshId);
            photonView.RPC(nameof(RPC_ApplyMesh), RpcTarget.OthersBuffered, viewID, meshId);

            currentMeshId = meshId;
        }

        [PunRPC]
        private void RPC_ApplyMesh(int targetViewID, string meshName)
        {
            if (photonView.ViewID != targetViewID)
                return;

            ApplyMesh(meshName);
        }

        private void ApplyMesh(string meshName)
        {
            playerMesh.mesh = MeshManager.Instance.GetMeshByName(meshName);
            playerMeshRenderer.material = MeshManager.Instance.GetMaterialByName(meshName);
            UpdateCollider();
        }

        private void UpdateCollider()
        {
            if (playerMesh == null || playerMesh.mesh == null)
                return;

            foreach (var col in GetComponents<Collider>())
                Destroy(col);

            if (useMeshCollider)
            {
                MeshCollider meshCol = gameObject.AddComponent<MeshCollider>();
                meshCol.sharedMesh = playerMesh.mesh;
                meshCol.convex = true;
                meshCol.isTrigger = colliderIsTrigger;
            }
            else
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.center = playerMesh.mesh.bounds.center;
                box.size = playerMesh.mesh.bounds.size;
                box.isTrigger = colliderIsTrigger;
            }
        }

        private void Update()
        {
            if (!photonView.IsMine)
                return;

            if (Input.GetKeyDown(cloneInput))
            {
                TrySpawnClone();
            }
        }

        private void TrySpawnClone()
        {
            if (string.IsNullOrEmpty(currentMeshId))
            {
                Debug.Log("[Prop] No mesh equipped yet.");
                return;
            }

            string prefabName = "Networked_" + currentMeshId;
            PhotonNetwork.Instantiate(
                Path.Combine("PhotonPrefabs", prefabName),
                transform.position,
                Quaternion.identity
            );
        }
    }
}
