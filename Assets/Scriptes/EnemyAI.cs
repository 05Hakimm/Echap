using UnityEngine;

// Ce script gère le comportement individuel de chaque ennemi (mouvement et attaque).
public class EnemyAI : MonoBehaviour
{
    [Header("Réglages de Base")]
    public float speed = 2f;
    public int damageAmount = 10;
    public float attackRate = 1.5f;
    public Animator anim;

    [Header("Détection")]
    private Transform player;
    private float nextAttackTime = 0f;

    [Header("Physique & Recul")]
    private Rigidbody2D rb;
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    public float knockbackDuration = 0.2f;

    // Distance à laquelle l'ennemi s'arrête pour attaquer
    private float stoppingDistance = 0.7f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();

        // On cherche le joueur pour savoir vers où courir
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    // Appelée par le EnemySpawner pour changer les animations selon la phase de jeu
    public void UpdateAppearance(RuntimeAnimatorController newController)
    {
        if (anim != null && newController != null)
        {
            anim.runtimeAnimatorController = newController;
        }
    }

    void Update()
    {
        // Si l'ennemi est en train de prendre un coup (recul), il ne bouge pas normalement
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockedBack = false;
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        if (player != null)
        {
            HandleBehavior();
        }
    }

    void HandleBehavior()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stoppingDistance)
        {
            // --- DÉPLACEMENT ---
            Vector2 direction = (player.position - transform.position).normalized;
            Vector2 newPos = Vector2.MoveTowards(rb.position, player.position, speed * Time.deltaTime);
            rb.MovePosition(newPos);

            // GESTION DE L'ORIENTATION
            // On récupère la taille actuelle (pour ne pas rétrécir le boss)
            float currentAbsX = Mathf.Abs(transform.localScale.x);

            if (direction.x > 0.1f) // Regarde à droite
            {
                transform.localScale = new Vector3(currentAbsX, transform.localScale.y, transform.localScale.z);
            }
            else if (direction.x < -0.1f) // Regarde à gauche
            {
                transform.localScale = new Vector3(-currentAbsX, transform.localScale.y, transform.localScale.z);
            }

            if (anim != null) anim.SetBool("isMoving", true);
        }
        else
        {
            // --- ATTAQUE ---
            if (anim != null) anim.SetBool("isMoving", false);

            if (Time.time >= nextAttackTime)
            {
                AttackPlayer();
                nextAttackTime = Time.time + attackRate;
            }
        }
    }

    // Reçoit une force de recul (appelé par PlayerCombat)
    public void ApplyKnockback(Vector2 force)
    {
        if (rb == null) return;
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    void AttackPlayer()
    {
        if (anim != null) anim.SetTrigger("Attack");

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
        }
    }
}