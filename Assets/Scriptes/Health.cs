using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// gestion vie et mort
public class Health : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public Image redFillImage;

    [Header("Mort Joueur")]
    public RuntimeAnimatorController deathAnimController;
    public string deathTriggerName = "Die";
    public MonoBehaviour[] scriptsADesactiver;

    [Header("Effet Noir")]
    public float vitesseNoir = 3f;
    public float opaciteMax = 1.0f;

    [Header("Boss")]
    [HideInInspector] public Image barreBossExterne;
    [HideInInspector] public GameObject rootBarreBoss;

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
            if (rootBarreBoss != null)
            {
                EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
                if (spawner != null) spawner.NotifyBossDeath();
                Object.FindAnyObjectByType<LevelSystem>()?.ForceLevelUp();
            }
            if (xpPrefab != null) Instantiate(xpPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else if (gameObject.CompareTag("Player"))
        {
            // lance la mort
            StartCoroutine(SequenceMortJoueur());
        }
    }

    IEnumerator SequenceMortJoueur()
    {
        // stop les scripts
        foreach (var s in scriptsADesactiver) if (s != null) s.enabled = false;

        // force le perso devant le futur noir
        SpriteRenderer pSr = GetComponent<SpriteRenderer>();
        if (pSr != null) pSr.sortingOrder = 30001;

        // creation fond noir
        GameObject black = new GameObject("Mort_Noir");
        SpriteRenderer fadeSr = black.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.black);
        tex.Apply();
        fadeSr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        fadeSr.sortingOrder = 30000; // sous le perso
        fadeSr.color = new Color(0, 0, 0, 0);

        Camera cam = Camera.main;
        if (cam != null)
        {
            black.transform.SetParent(cam.transform);
            black.transform.localPosition = new Vector3(0, 0, 10);
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            black.transform.localScale = new Vector3(w * 1.5f, h * 1.5f, 1f);

            // meme calque que le joueur
            if (pSr != null) fadeSr.sortingLayerName = pSr.sortingLayerName;
        }

        // ralenti instant + noir progressif (1.1s)
        float t = 0;
        float dureeRalenti = 1.1f;

        while (t < dureeRalenti)
        {
            t += Time.unscaledDeltaTime;
            float progression = t / dureeRalenti;

            // noir de + en + (0 a 1)
            fadeSr.color = new Color(0, 0, 0, progression);

            // ralenti brutal direct (0.2) puis va vers 0
            Time.timeScale = Mathf.Lerp(0.2f, 0f, progression);

            yield return null;
        }

        Time.timeScale = 0f; // freeze total

        // lance l'anim de mort
        if (deathAnimController != null)
        {
            GameObject effect = new GameObject("EffetMort");
            effect.transform.position = transform.position;
            effect.transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), 1f);
            SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 30002; // tout devant le perso et le noir

            if (pSr != null) sr.sortingLayerName = pSr.sortingLayerName;

            Animator anim = effect.AddComponent<Animator>();
            anim.runtimeAnimatorController = deathAnimController;
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            anim.SetTrigger(deathTriggerName);
        }

        // attente fin anim (1.1s)
        yield return new WaitForSecondsRealtime(1.1f);

        // disparition du perso
        if (pSr != null) pSr.enabled = false;
    }
}