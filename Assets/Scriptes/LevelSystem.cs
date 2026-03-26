using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ce script gère l'expérience du joueur, le passage de niveaux et l'ouverture du menu d'amélioration.
public class LevelSystem : MonoBehaviour
{
    [Header("Statistiques d'XP")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    [Header("Interface UI")]
    public Image xpFillImage;      // La barre d'XP (Image avec Fill Amount)
    public TMP_Text levelText;     // Le texte affichant le niveau actuel
    public GameObject upgradePanel; // Le panneau du menu d'amélioration (le parchemin)

    private UpgradeManager upgradeManager;

    void Start()
    {
        // On récupère le gestionnaire d'améliorations sur le panneau
        if (upgradePanel != null)
        {
            upgradeManager = upgradePanel.GetComponent<UpgradeManager>();
            upgradePanel.SetActive(false); // On cache le menu au lancement
        }

        UpdateUI();
    }

    // Fonction appelée par les cristaux d'XP ramassés
    public void AddExperience(int amount)
    {
        currentXP += amount;

        // Vérification du passage de niveau
        if (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }

        UpdateUI();
    }

    // --- FONCTION CAPITALE : Permet de forcer un niveau (ex: après avoir tué un Boss) ---
    public void ForceLevelUp()
    {
        Debug.Log("Récompense de Boss : Niveau supérieur forcé !");
        LevelUp();
    }

    // Gère la montée de niveau, le calcul de la nouvelle courbe d'XP et l'ouverture du menu
    private void LevelUp()
    {
        currentLevel++;

        // On soustrait l'XP consommée et on évite les valeurs négatives
        currentXP = Mathf.Max(0, currentXP - xpToNextLevel);

        // Augmentation de la difficulté du prochain niveau (+25%)
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.25f);

        // Ouverture du menu d'amélioration
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);

            // On fige le temps pour que le joueur puisse choisir tranquillement
            Time.timeScale = 0f;

            // On demande au manager de générer les choix de cartes
            if (upgradeManager != null)
            {
                upgradeManager.GenerateUpgradeChoices();
            }
        }
    }

    // Appelée par les boutons du menu d'amélioration pour fermer le menu et reprendre
    public void CloseUpgradeMenu()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);

            // On relance le temps du jeu
            Time.timeScale = 1f;
        }
    }

    // Met à jour les éléments visuels (Barre et Texte)
    void UpdateUI()
    {
        if (xpFillImage != null)
        {
            xpFillImage.fillAmount = (float)currentXP / xpToNextLevel;
        }

        if (levelText != null)
        {
            levelText.text = "LVL " + currentLevel;
        }
    }
}