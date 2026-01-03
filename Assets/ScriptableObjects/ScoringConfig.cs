using UnityEngine;

[CreateAssetMenu(menuName = "PropHunt/ScoringConfig")]
public class ScoringConfig : ScriptableObject
{
    public int hunterKillProp = 200;
    public int hunterDestroyDecoy = 50;
    public int propSurviveRound = 300;
}