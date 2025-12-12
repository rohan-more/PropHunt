using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if UNITY_EDITOR
    using UnityEditor;
    using System.Net;
#endif

public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public PhotonView photonView;
    public GameObject crosshairUI;
    public GameObject gunModel;
    public AudioListener audioListener;
    public Animator animator;
    public Camera playerCamera;

    [Header("Camera")]
    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    [Header("Crosshair")]
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;
    private Image crosshairObject;

    [Header("Zoom")]
    public bool enableZoom = true;
    public bool holdToZoom = false;
    public KeyCode zoomKey = KeyCode.Mouse1;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;
    private bool isZoomed = false;

    [Header("Movement")]
    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;

    [Header("Sprint")]
    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;
    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;
    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;

    [Header("Jump")]
    public bool enableJump = true;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpPower = 5f;
    private bool isGrounded = false;

    [Header("Crouch")]
    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;
    private bool isCrouched = false;
    private Vector3 originalScale;

    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);
    private Vector3 jointOriginalPos;
    private float timer = 0;
    private bool isWalking = false;

    // internal camera rotation
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    private const string layerName = "HideFromCamera";

    // Safety / debugging flags
    [Header("Debug/Options")]
    [Tooltip("If true, make non-owned rigidbodies kinematic to prevent remote physics control.")]
    public bool makeRemoteRigidbodiesKinematic = true;

    private void Awake()
    {
        // Ensure photonView assigned
        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
            Debug.Log($"[FPC] Awake: photonView was null, fetched via GetComponent -> {photonView != null}");
        }

        if (photonView == null)
        {
            Debug.LogError("[FPC] Awake: No PhotonView found on the GameObject. This prefab must have a PhotonView.");
            return;
        }

        // defensive: ensure rb assigned
        if (rb == null) rb = GetComponent<Rigidbody>();

        // set up crosshair reference safely
        crosshairObject = GetComponentInChildren<Image>();
        originalScale = transform.localScale;
        if (joint != null) jointOriginalPos = joint.localPosition;

        // Ownership debug log
        Debug.Log($"[FPC] Awake: ViewID={photonView.ViewID} IsMine={photonView.IsMine} ActorNumber={(photonView.Owner != null ? photonView.Owner.ActorNumber.ToString() : "n/a")} GameObject={name}");

        // Per-owner setup
        if (photonView.IsMine)
        {
            // Ensure local player's camera and audio are enabled
            if (playerCamera != null) playerCamera.enabled = true;
            if (audioListener != null) audioListener.enabled = true;
            if (crosshairUI != null) crosshairUI.SetActive(true);
            if (gunModel != null) gunModel.SetActive(true);
            Debug.Log("[FPC] Awake: This is the local player. Enabled camera/audio/UI.");
        }
        else
        {
            // Non-owned: disable local-only components so remote players don't react to this client's input
            if (playerCamera != null) playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
            if (crosshairUI != null) crosshairUI.SetActive(false);
            if (gunModel != null) gunModel.SetActive(false);

            if (makeRemoteRigidbodiesKinematic && rb != null)
            {
                rb.isKinematic = true; // prevents remote physics from being controlled by this client
                Debug.Log("[FPC] Awake: Marked remote rigidbody as kinematic to avoid physics control by non-owner.");
            }

            Debug.Log("[FPC] Awake: This is a remote player. Disabled camera/audio/UI.");
        }

        // set FOV
        if (playerCamera != null) playerCamera.fieldOfView = fov;

        // sprint initial values
        sprintCooldownReset = sprintCooldown;
        if (!unlimitedSprint) sprintRemaining = sprintDuration;
    }

    void Start()
    {
        if (lockCursor)
            Cursor.lockState = CursorLockMode.Locked;

        // Double-check ownership & camera state and log
        Debug.Log($"[FPC] Start: ViewID={photonView.ViewID} IsMine={photonView.IsMine}");

        // If this is not the owner, ensure camera is off for remote players (important if some logic previously toggled the camera)
        if (!photonView.IsMine)
        {
            if (playerCamera != null) playerCamera.enabled = false;   // keep disabled for remote
            if (audioListener != null) audioListener.enabled = false;
            if (crosshairUI != null) crosshairUI.SetActive(false);
            if (gunModel != null) gunModel.SetActive(false);

            // include remote layer in culling mask to hide remote player visuals from local camera if needed
            if (playerCamera != null)
            {
                IncludeLayerInCullingMask(playerCamera, layerName);
            }
        }
        else
        {
            // local player: find minimap camera if present
            MinimapCameraController camera = GameObject.FindGameObjectWithTag("MinimapCamera")?.GetComponent<MinimapCameraController>();
            if (camera != null)
            {
                camera.playerTransform = transform;
                Debug.Log("[FPC] Start: Minimap camera assigned.");
            }
        }

        // crosshair sprite setup
        if (crosshair)
        {
            if (crosshairObject != null)
            {
                crosshairObject.sprite = crosshairImage;
                crosshairObject.color = crosshairColor;
            }
            else
            {
                Debug.LogWarning("[FPC] Start: crosshairObject not found in children.");
            }
        }
        else if (crosshairObject != null)
        {
            crosshairObject.gameObject.SetActive(false);
        }

        // sprint bar UI initialization
        sprintBarCG = GetComponentInChildren<CanvasGroup>();
        if (useSprintBar && sprintBarBG != null && sprintBar != null)
        {
            sprintBarBG.gameObject.SetActive(true);
            sprintBar.gameObject.SetActive(true);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            sprintBarWidth = screenWidth * sprintBarWidthPercent;
            sprintBarHeight = screenHeight * sprintBarHeightPercent;

            sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
            sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);

            if (hideBarWhenFull && sprintBarCG != null) sprintBarCG.alpha = 0;
        }
        else
        {
            if (sprintBarBG != null) sprintBarBG.gameObject.SetActive(false);
            if (sprintBar != null) sprintBar.gameObject.SetActive(false);
        }
    }

    [PunRPC]
    void RPC_DestroyProp(int targetPropID)
    {
        PhotonView pv = PhotonView.Find(targetPropID);
        if (pv != null)
        {
            Debug.Log($"[FPC] RPC_DestroyProp destroying {pv.gameObject.name} ID:{pv.ViewID}");
            PhotonNetwork.Destroy(pv.gameObject);
        }
        else
        {
            Debug.LogWarning($"[FPC] RPC_DestroyProp: could not find PhotonView {targetPropID}");
        }
    }

    void IncludeLayerInCullingMask(Camera camera, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[FPC] IncludeLayerInCullingMask: Layer '{layerName}' not found.");
            return;
        }
        camera.cullingMask |= 1 << layer;
    }

    private void Update()
    {
        // Always defensive-check photonView here
        if (photonView == null)
        {
            Debug.LogWarning("[FPC] Update: photonView is null, skipping update.");
            return;
        }

        // Only process input for the owning client
        if (!photonView.IsMine) return;

        // Camera rotation
        if (cameraCanMove)
        {
            yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;

            if (!invertCamera)
                pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
            else
                pitch += mouseSensitivity * Input.GetAxis("Mouse Y");

            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
            transform.localEulerAngles = new Vector3(0, yaw, 0);
            if (playerCamera != null)
                playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        // Zoom handling (only local)
        if (enableZoom)
        {
            if (Input.GetKeyDown(zoomKey) && !holdToZoom && !isSprinting)
                isZoomed = !isZoomed;

            if (holdToZoom && !isSprinting)
            {
                if (Input.GetKeyDown(zoomKey)) isZoomed = true;
                if (Input.GetKeyUp(zoomKey)) isZoomed = false;
            }

            if (playerCamera != null)
            {
                if (isZoomed)
                    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, zoomStepTime * Time.deltaTime);
                else if (!isZoomed && !isSprinting)
                    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, zoomStepTime * Time.deltaTime);
            }
        }

        // Sprint logic only for local player
        if (enableSprint)
        {
            if (isSprinting)
            {
                isZoomed = false;
                if (playerCamera != null) playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFOV, sprintFOVStepTime * Time.deltaTime);

                if (!unlimitedSprint)
                {
                    sprintRemaining -= 1 * Time.deltaTime;
                    if (sprintRemaining <= 0)
                    {
                        isSprinting = false;
                        isSprintCooldown = true;
                    }
                }
            }
            else
            {
                sprintRemaining = Mathf.Clamp(sprintRemaining + 1 * Time.deltaTime, 0, sprintDuration);
            }

            if (isSprintCooldown)
            {
                sprintCooldown -= 1 * Time.deltaTime;
                if (sprintCooldown <= 0) isSprintCooldown = false;
            }
            else sprintCooldown = sprintCooldownReset;

            if (useSprintBar && !unlimitedSprint && sprintBar != null)
            {
                float sprintRemainingPercent = sprintRemaining / sprintDuration;
                sprintBar.transform.localScale = new Vector3(sprintRemainingPercent, 1f, 1f);
            }
        }

        // Jump input
        if (enableJump && Input.GetKeyDown(jumpKey) && isGrounded) Jump();

        // Crouch input
        if (enableCrouch)
        {
            if (!holdToCrouch && Input.GetKeyDown(crouchKey)) Crouch();

            if (holdToCrouch)
            {
                if (Input.GetKeyDown(crouchKey)) { isCrouched = false; Crouch(); }
                else if (Input.GetKeyUp(crouchKey)) { isCrouched = true; Crouch(); }
            }
        }

        CheckGround();

        if (enableHeadBob) HeadBob();
    }

    void FixedUpdate()
    {
        if (photonView == null)
        {
            Debug.LogWarning("[FPC] FixedUpdate: photonView is null, skipping.");
            return;
        }

        // Only the owning client should drive physics input
        if (!photonView.IsMine) return;

        if (!playerCanMove) return;

        Vector3 targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // walking determination
        if ((targetVelocity.x != 0 || targetVelocity.z != 0) && isGrounded) isWalking = true;
        else isWalking = false;

        if (animator != null) animator.SetBool("IsWalking", isWalking);

        // Sprint movement
        if (enableSprint && Input.GetKey(sprintKey) && sprintRemaining > 0f && !isSprintCooldown)
        {
            targetVelocity = transform.TransformDirection(targetVelocity) * sprintSpeed;
            ApplyVelocityChange(targetVelocity, true);
        }
        else
        {
            if (hideBarWhenFull && sprintRemaining == sprintDuration && sprintBarCG != null) sprintBarCG.alpha -= 3 * Time.deltaTime;
            targetVelocity = transform.TransformDirection(targetVelocity) * walkSpeed;
            ApplyVelocityChange(targetVelocity, false);
        }
    }

    private void ApplyVelocityChange(Vector3 targetVelocity, bool sprinting)
    {
        if (rb == null) return;

        Vector3 velocity = rb.velocity;
        Vector3 velocityChange = (targetVelocity - velocity);
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = 0;

        if (velocityChange.x != 0 || velocityChange.z != 0)
        {
            if (sprinting) isSprinting = true;
            if (isCrouched) Crouch();
            if (hideBarWhenFull && !unlimitedSprint && sprintBarCG != null) sprintBarCG.alpha += 5 * Time.deltaTime;
        }

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void CheckGround()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (transform.localScale.y * .5f), transform.position.z);
        Vector3 direction = transform.TransformDirection(Vector3.down);
        float distance = .75f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
        {
            Debug.DrawRay(origin, direction * distance, Color.red);
            isGrounded = true;
        }
        else isGrounded = false;
    }

    private void Jump()
    {
        if (!isGrounded) return;

        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            isGrounded = false;
        }

        if (isCrouched && !holdToCrouch) Crouch();
    }

    private void Crouch()
    {
        if (isCrouched)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed /= speedReduction;
            isCrouched = false;
        }
        else
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;
            isCrouched = true;
        }
    }

    private void HeadBob()
    {
        if (isWalking)
        {
            if (isSprinting) timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            else if (isCrouched) timer += Time.deltaTime * (bobSpeed * speedReduction);
            else timer += Time.deltaTime * bobSpeed;

            if (joint != null)
            {
                joint.localPosition = new Vector3(
                    jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x,
                    jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y,
                    jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
            }
        }
        else
        {
            timer = 0;
            if (joint != null)
            {
                joint.localPosition = new Vector3(
                    Mathf.Lerp(joint.localPosition.x, jointOriginalPos.x, Time.deltaTime * bobSpeed),
                    Mathf.Lerp(joint.localPosition.y, jointOriginalPos.y, Time.deltaTime * bobSpeed),
                    Mathf.Lerp(joint.localPosition.z, jointOriginalPos.z, Time.deltaTime * bobSpeed));
            }
        }
    }
}
