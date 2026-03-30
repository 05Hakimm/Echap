using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyEvolution
    {
        public string nomDeLaPhase;
        public float debutA_X_Secondes;
        public RuntimeAnimatorController cerveauAnimation;
        public float multiplicateurSante = 1f;
        public bool estUnBoss = false;
        public Sprite iconeBoss;
    }

    [Header("Réglages Apparition")]
    public GameObject enemyPrefab;
    public float spawnRadius = 12f;
    public float initialSpawnInterval = 2.0f;

    [Header("Interface Boss (HIERARCHIE SPECIFIQUE)")]
    public GameObject barreBossRoot;
    public TMP_Text nomBossTexte;   
    public Image iconeBossAffichage;      
    public Image barreBossRemplissage;

    [Header("Phases du Jeu")]
    public List<EnemyEvolution> phasesDeJeu;
    public float difficultyScaling = 0.05f;
    public float baseSpeed = 2f;
    public int baseDamage = 10;
    public int baseHealth = 100;

    private float timer = 0f;
    private float gameTime = 0f;
    private float currentSpawnInterval;
    private Transform player;
    private List<string> phasesDeclenchees = new List<string>();
    private bool bossActive = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        currentSpawnInterval = initialSpawnInterval;

        HideBossUI();
    }

    void Update()
    {
        if (player == null) return;
        if (!bossActive) gameTime += Time.deltaTime;

        EnemyEvolution phaseActuelle = GetCurrentPhase();

        if (phaseActuelle != null && phaseActuelle.estUnBoss)
        {
            if (!phasesDeclenchees.Contains(phaseActuelle.nomDeLaPhase))
            {
                bossActive = true;
                SupprimerTousLesEnnemis();
                SpawnEnemy(phaseActuelle);
                phasesDeclenchees.Add(phaseActuelle.nomDeLaPhase);

              
                if (barreBossRoot != null) barreBossRoot.SetActive(true);
                if (nomBossTexte != null)
                {
                    nomBossTexte.gameObject.SetActive(true);
                    nomBossTexte.text = phaseActuelle.nomDeLaPhase;
                }
                if (iconeBossAffichage != null && phaseActuelle.iconeBoss != null)
                {
                    iconeBossAffichage.gameObject.SetActive(true);
                    iconeBossAffichage.sprite = phaseActuelle.iconeBoss;
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

    public void NotifyBossDeath()
    {
        bossActive = false;
        gameTime = 0f; 
        timer = 0f;
        phasesDeclenchees.Clear();
        HideBossUI();
    }

    void HideBossUI()
    {
        // On éteint les 3 objets séparément comme dans ta hiérarchie
        if (barreBossRoot != null) barreBossRoot.SetActive(false);
        if (nomBossTexte != null) nomBossTexte.gameObject.SetActive(false);
        if (iconeBossAffichage != null) iconeBossAffichage.gameObject.SetActive(false);
    }

    void SupprimerTousLesEnnemis()
    {
        GameObject[] ennemis = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in ennemis)
        {
            if (e.GetComponent<Health>() != null && e.GetComponent<Health>().rootBarreBoss == null)
                Destroy(e);
        }
    }

    EnemyEvolution GetCurrentPhase()
    {
        if (phasesDeJeu == null || phasesDeJeu.Count == 0) return null;
        EnemyEvolution phaseTrouvee = phasesDeJeu[0];
        foreach (var phase in phasesDeJeu) if (gameTime >= phase.debutA_X_Secondes) phaseTrouvee = phase;
        return phaseTrouvee;
    }

    void SpawnEnemy(EnemyEvolution phase)
    {
        if (phase == null || player == null) return;
        Vector2 spawnPos = phase.estUnBoss ? (Vector2)player.position + Vector2.up * 4f : GetRandomPositionAroundPlayer();
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        if (phase.estUnBoss) newEnemy.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        float globalMult = 1f + (gameTime * difficultyScaling);
        EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
        Health hp = newEnemy.GetComponent<Health>();

        if (ai != null)
        {
            ai.speed = baseSpeed * (1f + (gameTime * 0.005f));
            ai.damageAmount = Mathf.RoundToInt(baseDamage * globalMult);
            ai.UpdateAppearance(phase.cerveauAnimation);
            if (phase.estUnBoss) StartCoroutine(BossStasisSequence(ai));
        }

        if (hp != null)
        {
            hp.maxHealth = Mathf.RoundToInt(baseHealth * globalMult * phase.multiplicateurSante);
            hp.currentHealth = hp.maxHealth;
            if (phase.estUnBoss)
            {
                hp.barreBossExterne = barreBossRemplissage;
                hp.rootBarreBoss = barreBossRoot;
            }
        }
    }

    IEnumerator BossStasisSequence(EnemyAI ai)
    {
        ai.enabled = false;
        yield return new WaitForSeconds(2.0f);
        if (ai != null) ai.enabled = true;
    }

    Vector2 GetRandomPositionAroundPlayer()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        return (Vector2)player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
    }
}