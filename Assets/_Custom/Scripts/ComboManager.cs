using System;
using System.Collections.Generic;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    [Header("Configuración de Cargas")]
    [Tooltip("Máximo número de cargas para golpes fuertes.")]
    public int maxCharges = 3;
    [Tooltip("Cuántos golpes débiles conectados se necesitan para obtener 1 carga.")]
    public int hitsPerCharge = 4;
    
    [Header("Tiempos del Combo")]
    [Tooltip("Tiempo límite en segundos entre ataques para que se considere parte del mismo combo.")]
    public float comboExpiryTime = 0.8f;
    [Tooltip("Tiempo mínimo de espera para detectar un 'golpe pausado' en el combo (Ruta Rítmica).")]
    public float rhythmicPauseThreshold = 0.35f;

    private int currentCharges = 1; // Empezamos con 1 carga para no frustrar al jugador al inicio
    private int currentHitCounter = 0;

    // Registro de los últimos inputs para calcular el combo
    private List<AttackInputRecord> inputHistory = new List<AttackInputRecord>();
    private float lastInputTime = 0f;

    public int CurrentCharges => currentCharges;
    public int MaxCharges => maxCharges;

    private struct AttackInputRecord
    {
        public bool isStrong;
        public float delaySinceLast;
    }

    void OnEnable()
    {
        GameEvents.OnEnemyHit += OnEnemyHit;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyHit -= OnEnemyHit;
    }

    void Start()
    {
        // Notificar el estado inicial de las cargas
        GameEvents.TriggerStrongChargesChanged(currentCharges, maxCharges);
    }

    private void OnEnemyHit(Enemy enemy, float damage)
    {
        // Solo sumamos carga si no estamos al máximo
        if (currentCharges < maxCharges)
        {
            currentHitCounter++;
            if (currentHitCounter >= hitsPerCharge)
            {
                currentHitCounter = 0;
                currentCharges = Mathf.Min(currentCharges + 1, maxCharges);
                GameEvents.TriggerStrongChargesChanged(currentCharges, maxCharges);
            }
        }
    }

    /// <summary>
    /// Intenta registrar un ataque. Retorna true si es un ataque válido (en caso de fuerte, si tiene cargas).
    /// Devuelve si este ataque califica como un Finisher.
    /// </summary>
    public bool RegisterAttackInput(bool isStrong, out bool isFinisher)
    {
        isFinisher = false;
        float currentTime = Time.time;
        float delay = currentTime - lastInputTime;

        // Si ha pasado demasiado tiempo, limpiamos el historial del combo
        if (delay > comboExpiryTime)
        {
            inputHistory.Clear();
        }

        // Si es un ataque fuerte, requiere consumir una carga
        if (isStrong)
        {
            if (currentCharges <= 0)
            {
                return false; // Sin cargas, no se puede hacer
            }
            
            // Consumir carga
            currentCharges--;
            GameEvents.TriggerStrongChargesChanged(currentCharges, maxCharges);
        }

        // Registrar en el historial
        AttackInputRecord record = new AttackInputRecord
        {
            isStrong = isStrong,
            delaySinceLast = inputHistory.Count == 0 ? 0f : delay
        };
        inputHistory.Add(record);
        lastInputTime = currentTime;

        // Evaluar si se completó algún Combo Finisher
        isFinisher = CheckForFinisher();

        return true;
    }

    private bool CheckForFinisher()
    {
        if (inputHistory.Count < 3) return false;

        // El último golpe DEBE ser Fuerte para ser un Finisher
        int lastIndex = inputHistory.Count - 1;
        if (!inputHistory[lastIndex].isStrong) return false;

        // Tomamos los últimos 3 golpes
        var hit1 = inputHistory[lastIndex - 2];
        var hit2 = inputHistory[lastIndex - 1];
        var hit3 = inputHistory[lastIndex];

        // Combo A: Débil -> Débil -> Fuerte
        if (!hit1.isStrong && !hit2.isStrong && hit3.isStrong)
        {
            // Limpiamos el combo tras el Finisher para que no se encadenen indefinidamente
            inputHistory.Clear();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Comprueba si el combo actual contiene una pausa rítmica (para efectos visuales/lógicos adicionales).
    /// </summary>
    public bool HasRhythmicPause()
    {
        if (inputHistory.Count < 2) return false;
        // Si el penúltimo golpe tuvo un delay mayor al threshold, es un golpe pausado rítmicamente
        return inputHistory[inputHistory.Count - 1].delaySinceLast >= rhythmicPauseThreshold;
    }
}
