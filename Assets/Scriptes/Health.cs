using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI (Optionnel)")]
    public Image redFillImage; // La petite barre au-dessus de l'ennemi/joueur

    // Ces variables sont remplies automatiquement par le Spawner pour le Boss
    [HideInInspector] public Image barreBossExterne;
    [HideInInspector] public GameObject rootBarreBoss;

    public GameObject xpPrefab; // Cristal d'XP à lâcher à la mort

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateUI()
    {
        float ratio = (float)currentHealth / maxHealth;

        // Mise à jour de la barre classique (au-dessus de la tête ou HUD joueur)
        if (redFillImage != null)
        {
            redFillImage.fillAmount = ratio;
        }

        // Mise à jour de la barre de Boss (si c'est un boss)
        if (barreBossExterne != null)
        {
            barreBossExterne.fillAmount = ratio;
        }
    }

    void Die()
    {
        if (gameObject.CompareTag("Enemy"))
        {
            // Si c'était un boss, on cache la grande barre à sa mort
            if (rootBarreBoss != null) rootBarreBoss.SetActive(false);

            // Drop d'XP
            if (xpPrefab != null) Instantiate(xpPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
        else if (gameObject.CompareTag("Player"))
        {
            // Ici tu peux ajouter ton écran de Game Over
            Debug.Log("JOUEUR MORT");
            Time.timeScale = 0f;
        }
    }
}