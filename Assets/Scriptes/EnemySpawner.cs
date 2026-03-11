using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Réglages de l'Apparition")]
    public GameObject enemyPrefab;    // Glisse ton préfabriqué d'ennemi ici
    public float spawnRadius = 12f;   // Distance à laquelle ils apparaissent (assez loin pour être hors écran)
    public float initialSpawnInterval = 2.0f; // Temps entre deux ennemis au début (en secondes)

    [Header("Évolution de la Difficulté")]
    // Plus ce chiffre est haut, plus le jeu devient dur rapidement
    public float difficultyScaling = 0.05f;

    [Header("Stats de base des Ennemis")]
    public float baseSpeed = 2f;
    public int baseDamage = 10;
    public int baseHealth = 100;

    private float timer = 0f;
    private float gameTime = 0f;
    private float currentSpawnInterval;
    private Transform player;

    void Start()
    {
        // On cherche le joueur pour savoir où faire apparaître les monstres autour de lui
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        currentSpawnInterval = initialSpawnInterval;
    }

    void Update()
    {
        if (player == null) return;

        // On compte le temps qui passe
        gameTime += Time.deltaTime;
        timer += Time.deltaTime;

        // Quand le timer atteint l'intervalle, on crée un ennemi
        if (timer >= currentSpawnInterval)
        {
            SpawnEnemy();
            timer = 0f;

            // On réduit l'intervalle pour que le prochain arrive plus vite
            // La formule "Temps de base / (1 + difficulté * temps)" permet une progression douce
            currentSpawnInterval = initialSpawnInterval / (1f + (gameTime * difficultyScaling * 0.1f));

            // On ne descend pas en dessous de 0.2s pour ne pas faire planter le PC avec trop d'ennemis
            if (currentSpawnInterval < 0.2f) currentSpawnInterval = 0.2f;
        }
    }

    void SpawnEnemy()
    {
        // 1. Calculer une position aléatoire sur un cercle autour du joueur
        Vector2 spawnPos = GetRandomPositionAroundPlayer();

        // 2. Créer l'ennemi dans le jeu
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // 3. Calculer les stats de cet ennemi selon le temps écoulé
        // Multiplicateur de difficulté : +5% de vie/dégâts toutes les minutes environ (selon difficultyScaling)
        float multiplier = 1f + (gameTime * difficultyScaling);

        // La vitesse augmente plus lentement pour ne pas être injouable trop vite
        float scaledSpeed = baseSpeed * (1f + (gameTime * 0.01f));
        int scaledHealth = Mathf.RoundToInt(baseHealth * multiplier);
        int scaledDamage = Mathf.RoundToInt(baseDamage * multiplier);

        // 4. Injecter les stats dans l'ennemi qui vient de naître
        EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
        Health hp = newEnemy.GetComponent<Health>();

        if (ai != null)
        {
            ai.speed = scaledSpeed;
            ai.damageAmount = scaledDamage;
        }

        if (hp != null)
        {
            hp.maxHealth = scaledHealth;
            hp.currentHealth = scaledHealth; // On le met full vie à sa création
        }
    }

    Vector2 GetRandomPositionAroundPlayer()
    {
        // On choisit un angle au hasard entre 0 et 360 degrés
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);

        // On calcule la direction (X et Y) basée sur cet angle
        Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * spawnRadius;

        // On l'ajoute à la position actuelle du joueur
        return (Vector2)player.position + offset;
    }
}