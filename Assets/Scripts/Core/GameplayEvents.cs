using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameplayEvents
{
    // ───────── Combat / Damage ─────────
    public const byte PropHit            = 1;  // hunter → master
    public const byte PropKilled         = 2;  // master → internal
    public const byte DecoyDestroyed     = 3;  // hunter → master

    // ───────── Round Flow ─────────
    public const byte RoundStart         = 10;
    public const byte RoundEnd           = 11;

    // ───────── Scoring / Sync ─────────
    public const byte ScoreSnapshot      = 20; // master → all (optional)
}