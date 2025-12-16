using TMPro;
using UnityEngine;

public class HunterHUDView : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text killCountText;

    public void SetTimer(double remainingSeconds)
    {
        timerText.text = FormatTime(remainingSeconds);
    }

    public void SetKillCount(int kills)
    {
        killCountText.text = $"Kills: {kills}";
    }

    private string FormatTime(double seconds)
    {
        int s = Mathf.CeilToInt((float)seconds);
        return $"{s / 60:00}:{s % 60:00}";
    }
}