using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using MoreMountains.Feedbacks; // Requiere el asset Feel

public class PlayerCombat : MonoBehaviour
{
    [Header("Input References")]
    [Tooltip("Acción para el golpe débil.")]
    public InputActionReference weakAttackAction;
    [Tooltip("Acción para el golpe fuerte.")]
    public InputActionReference strongAttackAction;

    [Header("Referencias de Componentes")]
    [Tooltip("El objeto vacío que actúa como pivote de la mano.")]
    public Transform handPivot;

    [Header("Golpe Débil (Weak Attack)")]
    public float weakDamage = 1f;
    public float weakKnockbackForce = 8f;
    public float weakDuration = 0.08f;
    public float weakCooldown = 0.15f;
    public float weakStartAngle = -35f;
    public float weakEndAngle = 35f;
    [Tooltip("Feedback de Feel para el golpe débil.")]
    public MMF_Player weakFeedback;

    [Header("Golpe Fuerte (Strong Attack)")]
    public float strongDamage = 3f;
    public float strongKnockbackForce = 15f;
    public float strongDuration = 0.18f;
    public float strongCooldown = 0.4f;
    public float strongStartAngle = -90f;
    public float strongEndAngle = 90f;
    [Tooltip("Feedback de Feel para el golpe fuerte.")]
    public MMF_Player strongFeedback;

    [Header("Finisher Attack")]
    public float finisherDamage = 5f;
    public float finisherKnockbackForce = 25f;
    public float finisherDuration = 0.25f;
    public float finisherStartAngle = -120f;
    public float finisherEndAngle = 120f;
    [Tooltip("Escala de tiempo durante el Finisher (ej: 0.1 para cámara lenta a 10%).")]
    public float finisherTimeScale = 0.1f;
    [Tooltip("Duración en segundos reales del efecto de cámara lenta.")]
    public float finisherSlowDuration = 0.15f;
    [Tooltip("Feedback de Feel para el Finisher.")]
    public MMF_Player finisherFeedback;

    [Header("Feedback Obsoleto (Fallback)")]
    [Tooltip("Se usará si los feedbacks específicos están vacíos.")]
    public MMF_Player punchFeedback;

    // Propiedades públicas para que PunchHand.cs las lea al impactar
    public bool IsPunching => isPunching;
    public float CurrentDamage { get; private set; }
    public float CurrentKnockbackForce { get; private set; }

    private bool isPunching = false;
    private float nextAttackTime = 0f;
    private ComboManager comboManager;

    void OnEnable()
    {
        if (weakAttackAction != null)
        {
            weakAttackAction.action.Enable();
            weakAttackAction.action.performed += OnWeakAttackPerformed;
        }
        if (strongAttackAction != null)
        {
            strongAttackAction.action.Enable();
            strongAttackAction.action.performed += OnStrongAttackPerformed;
        }
    }

    void OnDisable()
    {
        if (weakAttackAction != null)
        {
            weakAttackAction.action.Disable();
            weakAttackAction.action.performed -= OnWeakAttackPerformed;
        }
        if (strongAttackAction != null)
        {
            strongAttackAction.action.Disable();
            strongAttackAction.action.performed -= OnStrongAttackPerformed;
        }
    }

    void Start()
    {
        comboManager = GetComponent<ComboManager>();
        if (comboManager == null)
        {
            comboManager = gameObject.AddComponent<ComboManager>();
        }

        // Posición de reposo inicial
        if (handPivot != null)
        {
            handPivot.localRotation = Quaternion.Euler(0, 0, weakStartAngle);
        }
    }

    private void OnWeakAttackPerformed(InputAction.CallbackContext context)
    {
        TryAttack(isStrong: false);
    }

    private void OnStrongAttackPerformed(InputAction.CallbackContext context)
    {
        TryAttack(isStrong: true);
    }

    private void TryAttack(bool isStrong)
    {
        if (Time.unscaledTime < nextAttackTime || isPunching || handPivot == null)
            return;

        bool isFinisher = false;
        if (comboManager != null)
        {
            // Registrar entrada en el ComboManager. Si no tiene cargas para Strong, aborta.
            if (!comboManager.RegisterAttackInput(isStrong, out isFinisher))
            {
                return;
            }
        }

        StartCoroutine(PunchRoutine(isStrong, isFinisher));
    }

    private IEnumerator PunchRoutine(bool isStrong, bool isFinisher)
    {
        isPunching = true;

        float duration;
        float sAngle;
        float eAngle;
        MMF_Player feedbackToPlay;

        // Configurar los parámetros de golpe y lanzar eventos de bus
        if (isFinisher)
        {
            CurrentDamage = finisherDamage;
            CurrentKnockbackForce = finisherKnockbackForce;
            duration = finisherDuration;
            sAngle = finisherStartAngle;
            eAngle = finisherEndAngle;
            feedbackToPlay = finisherFeedback != null ? finisherFeedback : punchFeedback;

            nextAttackTime = Time.unscaledTime + strongCooldown;
            GameEvents.TriggerFinisherPerformed();
        }
        else if (isStrong)
        {
            CurrentDamage = strongDamage;
            CurrentKnockbackForce = strongKnockbackForce;
            duration = strongDuration;
            sAngle = strongStartAngle;
            eAngle = strongEndAngle;
            feedbackToPlay = strongFeedback != null ? strongFeedback : punchFeedback;

            nextAttackTime = Time.unscaledTime + strongCooldown;
            GameEvents.TriggerStrongAttack();
        }
        else
        {
            CurrentDamage = weakDamage;
            CurrentKnockbackForce = weakKnockbackForce;
            duration = weakDuration;
            sAngle = weakStartAngle;
            eAngle = weakEndAngle;
            feedbackToPlay = weakFeedback != null ? weakFeedback : punchFeedback;

            nextAttackTime = Time.unscaledTime + weakCooldown;
            GameEvents.TriggerWeakAttack();
        }

        // Cámara lenta de impacto para Finisher
        if (isFinisher)
        {
            Time.timeScale = finisherTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        // Reproducir Feedback
        if (feedbackToPlay != null)
        {
            feedbackToPlay.PlayFeedbacks();
        }

        // Animar barrido usando unscaledDeltaTime para no verse ralentizado por el Finisher
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / duration;
            t = t * t * (3f - 2f * t); // SmoothStep

            float currentAngle = Mathf.Lerp(sAngle, eAngle, t);
            handPivot.localRotation = Quaternion.Euler(0, 0, currentAngle);

            yield return null;
        }

        // Fijar ángulo final
        handPivot.localRotation = Quaternion.Euler(0, 0, eAngle);

        // Retener la pose final un instante
        if (isFinisher)
        {
            yield return new WaitForSecondsRealtime(finisherSlowDuration);
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
        }
        else
        {
            yield return new WaitForSeconds(0.04f);
        }

        // Retornar al reposo rápidamente
        float returnTime = 0f;
        float returnDuration = 0.05f;
        Quaternion startRot = handPivot.localRotation;
        Quaternion endRot = Quaternion.Euler(0, 0, weakStartAngle);

        while (returnTime < returnDuration)
        {
            returnTime += Time.unscaledDeltaTime;
            handPivot.localRotation = Quaternion.Slerp(startRot, endRot, returnTime / returnDuration);
            yield return null;
        }

        handPivot.localRotation = endRot;
        isPunching = false;
    }
}
