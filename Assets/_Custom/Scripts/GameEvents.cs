using System;
using UnityEngine;

public static class GameEvents
{
    // Combos & Combat
    public static event Action OnWeakAttack;
    public static event Action OnStrongAttack;
    public static event Action OnFinisherPerformed;
    public static event Action<int, int> OnStrongChargesChanged; // (currentCharges, maxCharges)

    // Enemy events
    public static event Action<Enemy, float> OnEnemyHit; // (enemy, damage)
    public static event Action<Enemy> OnEnemyKilled;

    // Game loop & tension
    public static event Action<float> OnFuryChanged; // (currentFuryPercentage 0-1)
    public static event Action<float> OnArenaChanged; // (currentArenaScale)
    public static event Action OnPlayerDeath;

    public static void TriggerWeakAttack() => OnWeakAttack?.Invoke();
    public static void TriggerStrongAttack() => OnStrongAttack?.Invoke();
    public static void TriggerFinisherPerformed() => OnFinisherPerformed?.Invoke();
    public static void TriggerStrongChargesChanged(int current, int max) => OnStrongChargesChanged?.Invoke(current, max);
    
    public static void TriggerEnemyHit(Enemy enemy, float damage) => OnEnemyHit?.Invoke(enemy, damage);
    public static void TriggerEnemyKilled(Enemy enemy) => OnEnemyKilled?.Invoke(enemy);
    
    public static void TriggerFuryChanged(float fury) => OnFuryChanged?.Invoke(fury);
    public static void TriggerArenaChanged(float scale) => OnArenaChanged?.Invoke(scale);
    public static void TriggerPlayerDeath() => OnPlayerDeath?.Invoke();
}
