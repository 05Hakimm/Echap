using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

// Ce script gère l'apparition des vagues d'ennemis et les phases de Boss.
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyEvolution
    {
        public string nomDeLaPhase;       // Nom affiché pour le Boss
        public float debutA_X_Secondes;  // Temps pour démarrer cette phase

        [Header("Animations")]
        public RuntimeAnimatorController cerveauAnimation;

        [Header("Difficulté de la Phase")]
        public float multiplicateurSante = 1f;

        [Header("Type de Phase")]
        public bool estUnBoss = false;
        public Sprite iconeBoss;       // L'icône à afficher à côté de la barre
    }

    [Header("Réglages Apparition")]
    public GameObject enemyPrefab;
    public float spawnRadius = 12f;
    public float initialSpawnInterval = 2.0f;

    [Header("Interface Boss (Barre de vie)")]
    public GameObject barreBossRoot;       // L'objet parent (Barre + Nom + Icône)
    public Image barreBossRemplissage;    // L'image rouge
    public TMP_Text nomBossTexte;         // Le texte du nom
    public Image iconeBossAffichage;      // L'image UI pour l'icône

    [Header("Phases du Jeu")]
    public List<EnemyEvolution> phasesDeJeu;

    [Header("Difficulté Globale")]
    public float difficultyScaling = 0.05f;
    public float baseSpeed = 2f;
    public int baseDamage = 10;
    public int baseHealth = 100;

    private float timer = 0f;
    private float gameTime = 0f;
    private float currentSpawnInterval;
    private Transform player;
    private List<string> phasesDeclenchees = new List<string>();

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        currentSpawnInterval = initialSpawnInterval;

        // Sécurité : On cache tout au début
        if (barreBossRoot != null) barreBossRoot.SetActive(false);
        if (nomBossTexte != null) nomBossTexte.gameObject.SetActive(false);
        if (iconeBossAffichage != null) iconeBossAffichage.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        gameTime += Time.deltaTime;
        EnemyEvolution phaseActuelle = GetCurrentPhase();

        if (phaseActuelle != null && phaseActuelle.estUnBoss)
        {
            if (!phasesDeclenchees.Contains(phaseActuelle.nomDeLaPhase))
            {
                // On nettoie la zone
                SupprimerTousLesEnnemis();
                SpawnEnemy(phaseActuelle);
                phasesDeclenchees.Add(phaseActuelle.nomDeLaPhase);

                // Activation de l'UI
                if (barreBossRoot != null) barreBossRoot.SetActive(true);
                if (nomBossTexte != null)
                {
                    nomBossTexte.text = phaseActuelle.nomDeLaPhase;
                    nomBossTexte.gameObject.SetActive(true);
                }
                if (iconeBossAffichage != null && phaseActuelle.iconeBoss != null)
                {
                    iconeBossAffichage.sprite = phaseActuelle.iconeBoss;
                    iconeBossAffichage.gameObject.SetActive(true);
                }
            }
            return;
        }

        timer += Time.deltaTime;
        if (timer >= currentSpawnInterval)
        {
            SpawnEnemy(phaseActuelle);
            timer = 0f;
            currentSpawnInterval = initialSpawnInterval / (1f + (gameTime * difficultyScaling * 0.1f));
        }
    }

    void SupprimerTousLesEnnemis()
    {
        GameObject[] ennemis = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in ennemis) Destroy(e);
    }

    EnemyEvolution GetCurrentPhase()
    {
        if (phasesDeJeu == null || phasesDeJeu.Count == 0) return null;
        EnemyEvolution phaseTrouvee = phasesDeJeu[0];
        foreach (var phase in phasesDeJeu)
        {
            if (gameTime >= phase.debutA_X_Secondes) phaseTrouvee = phase;
        }
        return phaseTrouvee;
    }


    void SpawnEnemy(EnemyEvolution phase)
    {
        if (phase == null) return;

        // Le boss spawn légèrement au-dessus du joueur pour ne pas être collé instantanément
        Vector2 spawnPos = phase.estUnBoss ? (Vector2)player.position + Vector2.up * 4f : GetRandomPositionAroundPlayer();
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // --- TAILLE DU BOSS ---
        // Réduit à 1.5f pour être juste un peu plus grand que le perso (1.0f)
        if (phase.estUnBoss) newEnemy.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        float globalMult = 1f + (gameTime * difficultyScaling);
        int scaledHealth = Mathf.RoundToInt(baseHealth * globalMult * phase.multiplicateurSante);
        float scaledSpeed = baseSpeed * (1f + (gameTime * 0.005f));
        if (phase.estUnBoss) scaledSpeed *= 1.4f;

        EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
        Health hp = newEnemy.GetComponent<Health>();

        if (ai != null)
        {
            ai.speed = scaledSpeed;
            ai.damageAmount = Mathf.RoundToInt(baseDamage * globalMult);
            ai.UpdateAppearance(phase.cerveauAnimation);

            // --- TENSION DRAMATIQUE : Arrêt temporaire si c'est un boss ---
            if (phase.estUnBoss)
            {
                StartCoroutine(BossStasisSequence(ai));
            }
        }

        if (hp != null)
        {
            hp.maxHealth = scaledHealth;
            hp.currentHealth = scaledHealth;
            if (phase.estUnBoss)
            {
                hp.barreBossExterne = barreBossRemplissage;
                hp.rootBarreBoss = barreBossRoot;
            }
        }
    }

    // Coroutine pour figer le boss pendant 2 secondes à son apparition
    IEnumerator BossStasisSequence(EnemyAI ai)
    {
        // On désactive le script d'IA pour qu'il ne bouge pas
        ai.enabled = false;

        // On s'assure que l'animation de marche est coupée (Idle)
        Animator bossAnim = ai.GetComponent<Animator>();
        if (bossAnim != null) bossAnim.SetBool("isMoving", false);

        // Pause de 2 secondes
        yield return new WaitForSeconds(2.0f);

        // On réactive l'IA pour qu'il commence la traque
        if (ai != null) ai.enabled = true;
    }

    Vector2 GetRandomPositionAroundPlayer()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        return (Vector2)player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
    }
}