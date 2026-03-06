using UnityEngine;
using UnityEngine.UI;

// Ce script gère l'XP du joueur, la barre bleue et l'arrêt du temps pour le menu
public class LevelSystem : MonoBehaviour
{
    [Header("Statistiques XP")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    [Header("Interface UI")]
    public Image xpFillImage;      // L'image de remplissage (la barre bleue)
    public GameObject upgradePanel; // Le fond du menu

    private UpgradeManager upgradeManager;

    void Start()
    {
        // On cherche le gestionnaire d'améliorations sur l'UpgradePanel
        if (upgradePanel != null)
        {
            upgradeManager = upgradePanel.GetComponent<UpgradeManager>();
            upgradePanel.SetActive(false);
        }

        UpdateUI();
    }

    // Appelée quand le joueur ramasse un objet d'XP
    public void AddExperience(int amount)
    {
        currentXP += amount;

        if (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }

        UpdateUI();
    }

    void LevelUp()
    {
        currentLevel++;
        currentXP -= xpToNextLevel;

        // On augmente l'exigence pour le prochain niveau (+25%)
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.25f);

        // On affiche le menu et on fige le jeu
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            Time.timeScale = 0f;

            if (upgradeManager != null)
            {
                upgradeManager.GenerateUpgradeChoices();
            }
        }
    }

    // Appelé par le bouton d'amélioration pour reprendre la partie
    public void CloseUpgradeMenu()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    void UpdateUI()
    {
        if (xpFillImage != null)
        {
            xpFillImage.fillAmount = (float)currentXP / xpToNextLevel;
        }
    }
}