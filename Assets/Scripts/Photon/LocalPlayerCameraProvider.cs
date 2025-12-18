using UnityEngine;

public class LocalPlayerCameraProvider : MonoBehaviour
{
    public static LocalPlayerCameraProvider Instance { get; private set; }

    public Camera LocalCamera { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(Camera cam)
    {
        LocalCamera = cam;
    }

    public bool HasCamera => LocalCamera != null;
}