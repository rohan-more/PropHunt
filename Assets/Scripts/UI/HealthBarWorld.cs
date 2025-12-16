using UnityEngine;
using UnityEngine.UI;

public class HealthBarWorld : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 1.8f, 0);
    [SerializeField] private int maxHealth = 100;
    private Transform target;
    
    public void Initialize(Transform followTarget, int maxHealth)
    {
        target = followTarget;
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
    }

    public void SetHealth(int value)
    {
        slider.value = value;
    }

    private void LateUpdate()
    {
        if (!target) return;
        if (!LocalPlayerCameraProvider.Instance.HasCamera) return;

        Camera cam = LocalPlayerCameraProvider.Instance.LocalCamera;

        transform.position = target.position + Vector3.up * 1.8f;
        transform.forward = cam.transform.forward;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}