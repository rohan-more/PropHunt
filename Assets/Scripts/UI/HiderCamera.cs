using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Photon.Pun;
using UnityEngine;

public class HiderCamera : MonoBehaviour
{
    [SerializeField] private Transform _targetPlayer;
    [SerializeField] private vThirdPersonCamera _tpCamera;
    [SerializeField] private Camera _camera;
    public PhotonView photonView;
    private int _targetViewID;
    private Dictionary<Func<string, bool>, MeshName> meshMap;

    public string shaderProperty = "_Thickness"; 
    public float hideValue = 0f;
    public float showValue = 0.005f;
    private List<Renderer> renderers = new();
    private HashSet<Renderer> highlightRenderers = new HashSet<Renderer>();
    private void Awake()
    {
        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
            Debug.Log($"[{nameof(HiderCamera)}] Awake: photonView auto-assigned -> {(photonView!=null)}");
        }
        if (photonView != null)
        {
            Debug.Log($"[{nameof(HiderCamera)}] Awake: ViewID={photonView.ViewID} IsMine={photonView.IsMine} Owner={(photonView.Owner!=null ? photonView.Owner.ActorNumber.ToString() : "null")}");
        }
    }
    private void Start()
    {
        meshMap = new Dictionary<Func<string, bool>, MeshName>
        {
            { str => str.Contains("vase"), MeshName.VASE },
            { str => str.Contains("chair"), MeshName.CHAIR },
            { str => str.Contains("bathtub"), MeshName.BATHTUB },
            { str => str.Contains("sack_open"), MeshName.SACK_OPEN },
            { str => str.Contains("bird_house"), MeshName.BIRD_HOUSE },
            { str => str.Contains("barrel"), MeshName.BARREL },
        };
    }

    void Update()
    {
        if (photonView == null) photonView = GetComponent<PhotonView>();
        if (!photonView.IsMine) return;

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            if (hit.collider.CompareTag("Hideable"))
            {
                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null && highlightRenderers.Add(renderer))
                {
                    ToggleShaderProperty(renderer, showValue);
                }
            }
            else if (highlightRenderers.Count > 0)
            {
                foreach (var item in highlightRenderers) ToggleShaderProperty(item, hideValue);
                highlightRenderers.Clear();
            }
        }
        else if (highlightRenderers.Count > 0)
        {
            foreach (var item in highlightRenderers) ToggleShaderProperty(item, hideValue);
            highlightRenderers.Clear();
        }

        if (Input.GetMouseButtonDown(0)) DetectObject();
    }
    public void SetTargetPlayer()
    {
        if (_tpCamera == null) return;
        if (_tpCamera.target == null) return;
        _targetPlayer = _tpCamera.target;
        PhotonView pv = _targetPlayer.gameObject.GetComponent<PhotonView>();
        _targetViewID = pv != null ? pv.ViewID : -1;
        Debug.Log($"[HiderCamera] SetTargetPlayer target={_targetPlayer.name} viewid={_targetViewID}");
    }
    
    void ToggleShaderProperty(Renderer renderer, float value)
    {
        if (renderer == null)
        {
            return;
        }
        foreach (Material material in renderer.materials)
        {
            if (material.HasProperty(shaderProperty))
            {
                float newValue = value;
                material.SetFloat(shaderProperty, newValue);
            }
        }
    }

    private void DetectObject()
    {
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Hideable"))
            {
                Mesh newMesh = hit.collider.gameObject.GetComponent<MeshFilter>().sharedMesh;

                foreach (var condition in meshMap)
                {
                    if (!condition.Key(newMesh.name))
                    {
                        continue;
                    }

                    Events.OnSelectedObject(_targetViewID, condition.Value);
                    break;
                }
            }
        }
    }
}