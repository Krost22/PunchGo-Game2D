using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using MoreMountains.Feedbacks; // Requiere el asset Feel

public class PlayerCombat : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("La acción del Input System para atacar (ej. el botón B o la Barra Espaciadora).")]
    public InputActionReference attackAction;

    [Header("Configuración de la Mano")]
    [Tooltip("El objeto vacío que actúa como pivote (centro del jugador). La Mano (cuadrado) debe ser hija de este pivote.")]
    public Transform handPivot;
    [Tooltip("El daño de cada puñetazo.")]
    public float punchDamage = 1f;
    [Tooltip("El tiempo que tarda en dar el tajo de media luna (muy rápido para mayor impacto).")]
    public float punchDuration = 0.15f;
    [Tooltip("El cooldown entre puñetazos.")]
    public float cooldown = 0.4f;
    
    [Header("Game Feel")]
    [Tooltip("El MMF_Player que contiene los feedbacks del golpe (Scale, Wiggle, Sonido de swoosh, etc.).")]
    public MMF_Player punchFeedback;

    private bool isPunching = false;
    private float nextPunchTime = 0f;

    // Propiedad pública para saber si el jugador está en medio de un ataque activo
    public bool IsPunching => isPunching;
    
    [Header("Configuración de Ángulos (Tajo)")]
    [Tooltip("Ángulo inicial del golpe. Si la mano está a la derecha (X=1.2), 0 grados inicia en el costado derecho.")]
    public float startAngle = 0f;
    [Tooltip("Ángulo final del golpe. 180 grados termina en el costado izquierdo, logrando un semicírculo.")]
    public float endAngle = 180f;

    void OnEnable()
    {
        if (attackAction != null)
        {
            attackAction.action.Enable();
            attackAction.action.performed += OnAttackPerformed;
        }
    }

    void OnDisable()
    {
        if (attackAction != null)
        {
            attackAction.action.Disable();
            attackAction.action.performed -= OnAttackPerformed;
        }
    }

    private void Start()
    {
        // Posición inicial de reposo
        if (handPivot != null)
        {
            handPivot.localRotation = Quaternion.Euler(0, 0, startAngle);
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (Time.time >= nextPunchTime && !isPunching && handPivot != null)
        {
            StartCoroutine(PunchRoutine());
        }
    }

    IEnumerator PunchRoutine()
    {
        isPunching = true;
        nextPunchTime = Time.time + cooldown;

        // Disparar la "Jugosidad" del Asset Feel!
        if (punchFeedback != null)
        {
            punchFeedback.PlayFeedbacks();
        }

        // Lógica matemática para animar el tajo por código de manera fluida y responsiva
        float elapsedTime = 0f;

        while (elapsedTime < punchDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Usamos un SmoothStep para que el golpe acelere y desacelere, haciéndolo más visceral
            float t = elapsedTime / punchDuration;
            t = t * t * (3f - 2f * t);

            // Rotamos el pivote en Z (Eje de rotación 2D)
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
            handPivot.localRotation = Quaternion.Euler(0, 0, currentAngle);

            yield return null; // Esperar al siguiente frame
        }

        // Asegurar posición final exacta
        handPivot.localRotation = Quaternion.Euler(0, 0, endAngle);

        // Pequeño retardo (una fracción de segundo) manteniendo la pose al final del golpe 
        // para dar "peso" antes de retraer el brazo
        yield return new WaitForSeconds(0.1f);

        // Volver la mano rápidamente a la posición inicial
        handPivot.localRotation = Quaternion.Euler(0, 0, startAngle);
        
        isPunching = false;
    }
}
