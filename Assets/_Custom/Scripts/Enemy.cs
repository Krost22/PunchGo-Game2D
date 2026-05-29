using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [Header("Estadísticas del Enemigo")]
    public float maxHealth = 3f;
    public float currentHealth;
    public float baseSpeed = 2f;
    
    [Header("Movimiento Ondulado (Juicy)")]
    [Tooltip("Frecuencia de la ondulación de lado a lado. 0 para bajar en línea recta.")]
    public float waveFrequency = 3f;
    [Tooltip("Amplitud de la ondulación lateral.")]
    public float waveAmplitude = 0.6f;

    [Header("Reacción al Impacto (Knockback)")]
    public float knockbackForce = 12f;
    public float knockbackDuration = 0.12f;

    [Header("Game Feel")]
    [Tooltip("Efectos al ser golpeado (Flash blanco, chispas de partículas, vibración sutil).")]
    public MMF_Player hitFeedback;
    [Tooltip("Efectos al morir (Gran explosión, shake fuerte de cámara, freeze frame).")]
    public MMF_Player deathFeedback;

    private Rigidbody2D rb;
    private Vector3 spawnPos;
    private float birthTime;
    private bool isKnockedBack = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        spawnPos = transform.position;
        birthTime = Time.time;
        
        // Configuraciones de física necesarias para 2D Top-Down
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void FixedUpdate()
    {
        if (isKnockedBack) return;

        // 1. Desplazamiento constante hacia abajo
        float newY = rb.position.y - baseSpeed * Time.fixedDeltaTime;

        // 2. Movimiento sinusoidal para añadir dinamismo visual
        float newX = rb.position.x;
        if (waveAmplitude > 0f)
        {
            float age = Time.time - birthTime;
            newX = spawnPos.x + Mathf.Sin(age * waveFrequency) * waveAmplitude;
        }

        rb.MovePosition(new Vector2(newX, newY));
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection)
    {
        currentHealth -= damage;

        // 1. Activar feedbacks de golpe
        if (hitFeedback != null)
        {
            hitFeedback.PlayFeedbacks();
        }

        // 2. Aplicar retroceso físico (Knockback)
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(KnockbackRoutine(knockbackDirection));
        }

        // 3. Evaluar muerte
        if (currentHealth <= 0)
            Die();
    }

    IEnumerator KnockbackRoutine(Vector2 direction)
    {
        isKnockedBack = true;
        
        // Aplicamos un impulso de velocidad en la dirección del golpe
        rb.linearVelocity = direction * knockbackForce;

        yield return new WaitForSeconds(knockbackDuration);

        // Frenar al enemigo y volver al movimiento normal
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
        
        // Ajustamos la posición de spawn inicial en X para que la onda de seno continúe
        // de forma natural desde su nueva posición tras el knockback.
        spawnPos.x = transform.position.x;
    }

    private void Die()
    {
        if (deathFeedback != null)
        {
            // Reproducir feedback de muerte (ej: explosión de partículas)
            deathFeedback.PlayFeedbacks();
        }

        // Desactivamos colliders y sprites para evitar doble impacto mientras se reproducen los feedbacks finales
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        // Destruir al enemigo después de una milésima para asegurar que el feedback de muerte se dispare
        Destroy(gameObject, 0.05f);
    }
}
