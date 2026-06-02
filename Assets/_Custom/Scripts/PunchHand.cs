using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PunchHand : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El script de combate del jugador principal. Se auto-asignará buscando en los objetos padres si se deja vacío.")]
    public PlayerCombat playerCombat;

    private void Start()
    {
        // Si no está asignado, lo busca automáticamente en los objetos padres
        if (playerCombat == null)
        {
            playerCombat = GetComponentInParent<PlayerCombat>();
        }
        
        // Asegurar que el Collider2D de la mano esté en modo Trigger para detectar colisiones sin empujar físicamente de golpe
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo aplicamos daño si el jugador está realizando activamente el tajo (puñetazo)
        if (playerCombat != null && playerCombat.IsPunching)
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Dirección del knockback: va desde el centro del jugador hacia la posición del enemigo
                Vector2 playerPosition = playerCombat.transform.position;
                Vector2 enemyPosition = other.transform.position;
                
                // Vector de dirección normalizado
                Vector2 knockbackDir = (enemyPosition - playerPosition).normalized;

                // Si por alguna razón están exactamente en el mismo punto, empujamos hacia arriba por defecto
                if (knockbackDir == Vector2.zero)
                {
                    knockbackDir = Vector2.up;
                }

                // Infligir daño y pasar la dirección del empuje con fuerza personalizada
                enemy.TakeDamage(playerCombat.CurrentDamage, knockbackDir, playerCombat.CurrentKnockbackForce);
            }
        }
    }
}
