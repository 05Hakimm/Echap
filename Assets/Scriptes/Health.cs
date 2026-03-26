using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Ce script gère la vie, les dégâts et la séquence de mort précise.
public class Health : MonoBehaviour
{
    [Header("Statistiques")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Interface Classique")]
    public Image redFillImage;

    [Header("Séquence de Mort (Joueur)")]
    [Tooltip("Glisse ici ton fichier bleu (Animator Controller)")]
    public RuntimeAnimatorController deathAnimController;
    public string deathTriggerName = "Die"; // Le nom du Trigger dans ton Animator
    public MonoBehaviour[] scriptsADesactiver; // Mouvement, Combat, etc.

    [Header("UI Boss (Géré par le Spawner)")]
    [HideInInspector] public Image barreBossExterne;
    [HideInInspector] public GameObject rootBarreBoss;

    [Header("Objets")]
    public GameObject xpPrefab;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();

        if (currentHealth <= 0) Die();
    }

    void UpdateUI()
    {
        float ratio = (float)currentHealth / maxHealth;
        if (redFillImage != null) redFillImage.fillAmount = ratio;
        if (barreBossExterne != null) barreBossExterne.fillAmount = ratio;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (gameObject.CompareTag("Enemy"))
        {
            // Mort Ennemi / Boss
            if (rootBarreBoss != null)
            {
                // Nettoyage radical de l'UI Boss via le spawner
                EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
                if (spawner != null) spawner.NotifyBossDeath();

                LevelSystem levelSystem = Object.FindAnyObjectByType<LevelSystem>();
                if (levelSystem != null) levelSystem.ForceLevelUp();
            }

            if (xpPrefab != null) Instantiate(xpPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else if (gameObject.CompareTag("Player"))
        {
            // --- MORT DU JOUEUR : ACTION IMMEDIATE ---
            Time.timeScale = 0f; // FREEZE DU JEU IMMEDIAT
            StartCoroutine(SequenceMortJoueur());
        }
    }

    IEnumerator SequenceMortJoueur()
    {
        // 1. On coupe les scripts pour ne plus pouvoir bouger/attaquer
        foreach (var script in scriptsADesactiver)
        {
            if (script != null) script.enabled = false;
        }

        // 2. Création de l'effet d'animation SUR le joueur
        if (deathAnimController != null)
        {
            GameObject deathEffect = new GameObject("EffetMort_Joueur");
            deathEffect.transform.position = transform.position;

            // On s'assure que l'échelle est positive
            deathEffect.transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), 1f);

            SpriteRenderer sr = deathEffect.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 32767; // MAXIMUM pour passer devant tout

            SpriteRenderer playerSr = GetComponent<SpriteRenderer>();
            if (playerSr != null) sr.sortingLayerName = playerSr.sortingLayerName;

            Animator anim = deathEffect.AddComponent<Animator>();
            anim.runtimeAnimatorController = deathAnimController;

            // L'animation doit ignorer la pause (TimeScale 0)
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;

            // ON FORCE LE TRIGGER
            anim.SetTrigger(deathTriggerName);

            Debug.Log("Animation de mort lancée.");
        }

        // 3. ATTENTE SYNCHRONISÉE (14 frames à 13 fps ~ 1.08s)
        // On attend 1.1s pour être sûr que l'anim est finie avant de cacher le perso
        yield return new WaitForSecondsRealtime(1.1f);

        // 4. DISPARITION DU SPRITE DU JOUEUR (Pile à la fin de l'anim)
        SpriteRenderer finalSr = GetComponent<SpriteRenderer>();
        if (finalSr != null) finalSr.enabled = false;

        Debug.Log("Séquence terminée : Le joueur a disparu après son animation.");
    }
}