using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropHUDView : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text survivalScoreText;
    [SerializeField] private TMP_Text playerName;
    
    
    public void SetPlayerName(string name)
    {
        playerName.text = name;
    }

    public void InitializeHealth(int maxHealth)
    {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
    }

    public void SetHealth(int health)
    {
        healthSlider.value = health;
    }

    public void SetTimer(double remainingSeconds)
    {
        timerText.text = FormatTime(remainingSeconds);
    }

    public void SetSurvivalScore(int score)
    {
        survivalScoreText.text = $"Score: {score}";
    }

    private string FormatTime(double seconds)
    {
        int s = Mathf.CeilToInt((float)seconds);
        return $"{s / 60:00}:{s % 60:00}";
    }
}