using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using System.Reflection;

// Ce script gère l'affichage dynamique des 3 boutons d'amélioration sur le parchemin.
// Il injecte les sprites des modèles de rareté directement dans les cases du bouton.
public class UpgradeManager : MonoBehaviour
{
    public enum Rarity { Commun, Rare, Epique, Legendaire, Mythique }
    public enum UpgradeType { MoveSpeed, DamagePercentage, MaxHealth, AttackRange, AttackRate, Knockback }

    [System.Serializable]
    public class UpgradeLevel
    {
        [TextArea] public string description;
        public float value;
    }

    [System.Serializable]
    public class UpgradeLevelInfo
    {
        public string upgradeID;
        public string title;
        public UpgradeType type;
        public Rarity rarity;
        public Sprite icon;

        [Header("Mode Infini")]
        public bool useScalingInsteadOfList = true;
        public float valuePerLevel = 0.06f;
        public string scalingDescription = "+{0}% de bonus";

        public List<UpgradeLevel> levels;
    }

    [Header("Configuration Scripts")]
    public string movementScriptName = "PlayerMovement";
    public string movementVariableName = "moveSpeed";

    [Header("Les 3 Boutons sur le Parchemin")]
    public Button[] activeButtons;

    [Header("Modèles de Référence (Look)")]
    public GameObject modelCommun;
    public GameObject modelRare;
    public GameObject modelEpique;
    public GameObject modelLegendaire;
    public GameObject modelMythique;

    [Header("Curseurs")]
    public Texture2D cursorNormal;
    public Texture2D cursorPointer;

    [Header("Base de données")]
    public List<UpgradeLevelInfo> availableUpgrades;

    private Dictionary<string, int> currentUpgradeLevels = new Dictionary<string, int>();
    private PlayerCombat playerCombat;
    private Health playerHealth;
    private LevelSystem levelSystem;
    private bool isMenuOpen = false;

    void Awake()
    {
        foreach (var def in availableUpgrades)
        {
            if (!string.IsNullOrEmpty(def.upgradeID))
                currentUpgradeLevels[def.upgradeID] = -1;
        }

        // On désactive les modèles pour qu'ils ne soient pas visibles en jeu
        if (modelCommun) modelCommun.SetActive(false);
        if (modelRare) modelRare.SetActive(false);
        if (modelEpique) modelEpique.SetActive(false);
        if (modelLegendaire) modelLegendaire.SetActive(false);
        if (modelMythique) modelMythique.SetActive(false);
    }

    void Start()
    {
        SetCustomCursor(cursorNormal);
        Invoke("ForceCloseAtStart", 0.15f);
    }

    void Update()
    {
        if (isMenuOpen && !EventSystem.current.IsPointerOverGameObject())
        {
            SetCustomCursor(cursorNormal);
        }
    }

    void ForceCloseAtStart()
    {
        if (RefreshPlayerReferences())
        {
            isMenuOpen = false;
            levelSystem.CloseUpgradeMenu();
        }
        else
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    bool RefreshPlayerReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerCombat = playerObj.GetComponent<Health>().gameObject.GetComponent<PlayerCombat>();
            playerHealth = playerObj.GetComponent<Health>();
            levelSystem = playerObj.GetComponent<LevelSystem>();
            return playerCombat != null && playerHealth != null && levelSystem != null;
        }
        return false;
    }

    public void GenerateUpgradeChoices()
    {
        gameObject.SetActive(true);
        isMenuOpen = true;

        if (!RefreshPlayerReferences()) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetCustomCursor(cursorNormal);

        List<UpgradeLevelInfo> possibleChoices = new List<UpgradeLevelInfo>();
        foreach (var def in availableUpgrades)
        {
            int nextLevel = currentUpgradeLevels[def.upgradeID] + 1;
            if (def.useScalingInsteadOfList || nextLevel < def.levels.Count)
                possibleChoices.Add(def);
        }

        // Mélange Fisher-Yates pour un hasard total dès le niveau 1
        for (int i = 0; i < possibleChoices.Count; i++)
        {
            UpgradeLevelInfo temp = possibleChoices[i];
            int randomIndex = Random.Range(i, possibleChoices.Count);
            possibleChoices[i] = possibleChoices[randomIndex];
            possibleChoices[randomIndex] = temp;
        }

        for (int i = 0; i < activeButtons.Length; i++)
        {
            if (i >= possibleChoices.Count) { activeButtons[i].gameObject.SetActive(false); continue; }

            activeButtons[i].gameObject.SetActive(true);
            UpgradeLevelInfo chosenDef = possibleChoices[i];
            int nextLevelIndex = currentUpgradeLevels[chosenDef.upgradeID] + 1;

            float valToApply = chosenDef.useScalingInsteadOfList ? chosenDef.valuePerLevel : chosenDef.levels[nextLevelIndex].value;

            string descStr = "";
            if (chosenDef.useScalingInsteadOfList)
            {
                float disp = chosenDef.valuePerLevel;
                if (chosenDef.type == UpgradeType.DamagePercentage || chosenDef.type == UpgradeType.AttackRate) disp *= 100f;
                descStr = string.Format(chosenDef.scalingDescription, Mathf.RoundToInt(disp));
            }
            else
            {
                descStr = chosenDef.levels[nextLevelIndex].description;
            }

            // 1. APPLIQUER LA RARETÉ (Remplacement direct des sprites)
            ApplyVisualRarity(activeButtons[i], chosenDef.rarity);

            // 2. TEXTES
            UpdateAllTextComponents(activeButtons[i].gameObject, chosenDef.title, descStr, "LVL " + (nextLevelIndex + 1));

            // 3. ICÔNE
            Image[] images = activeButtons[i].GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.gameObject.name == "Icon")
                {
                    img.sprite = chosenDef.icon;
                    img.enabled = (img.sprite != null);
                }
            }

            activeButtons[i].onClick.RemoveAllListeners();
            activeButtons[i].onClick.AddListener(() => ApplyUpgrade(chosenDef, valToApply));
            AddCursorEvents(activeButtons[i]);
        }
    }

    void UpdateAllTextComponents(GameObject root, string title, string desc, string lvl)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string content = "";
            if (child.name == "TITRE") content = title;
            else if (child.name == "DESCRIPTION") content = desc;
            else if (child.name == "NIVEAU") content = lvl;
            else continue;

            TMP_Text tmp = child.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.text = content; continue; }

            Text txt = child.GetComponent<Text>();
            if (txt != null) txt.text = content;
        }
    }

    void ApplyVisualRarity(Button targetButton, Rarity rarity)
    {
        GameObject model = GetModelByRarity(rarity);
        if (model == null) return;

        // Sprite du fond
        Image targetBg = targetButton.GetComponent<Image>();
        Image modelBg = model.GetComponent<Image>();
        if (targetBg && modelBg) targetBg.sprite = modelBg.sprite;

        // Remplacement des morceaux (Haut, Milieu, Bas, etc.)
        Image[] targetImages = targetButton.GetComponentsInChildren<Image>(true);
        Image[] modelImages = model.GetComponentsInChildren<Image>(true);
        List<string> uiLabels = new List<string> { "Icon", "TITRE", "DESCRIPTION", "NIVEAU" };

        foreach (Image tImg in targetImages)
        {
            // On ignore les textes et l'icône
            if (tImg == targetBg || uiLabels.Contains(tImg.gameObject.name)) continue;

            // On cherche le sprite correspondant dans le modèle par nom
            Sprite newSprite = null;
            foreach (Image m in modelImages)
            {
                if (m.gameObject.name == tImg.gameObject.name && m.gameObject != model)
                {
                    newSprite = m.sprite;
                    break;
                }
            }

            // On remplace le sprite. S'il n'existe pas dans le modèle actuel, 
            // on désactive l'image pour éviter que le sprite de la rareté précédente ne reste.
            tImg.sprite = newSprite;
            tImg.enabled = (newSprite != null);
        }
    }

    GameObject GetModelByRarity(Rarity r)
    {
        switch (r)
        {
            case Rarity.Commun: return modelCommun;
            case Rarity.Rare: return modelRare;
            case Rarity.Epique: return modelEpique;
            case Rarity.Legendaire: return modelLegendaire;
            case Rarity.Mythique: return modelMythique;
            default: return modelCommun;
        }
    }

    void AddCursorEvents(Button btn)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry e = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        e.callback.AddListener((data) => SetCustomCursor(cursorPointer));
        trigger.triggers.Add(e);

        EventTrigger.Entry x = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        x.callback.AddListener((data) => SetCustomCursor(cursorNormal));
        trigger.triggers.Add(x);
    }

    void SetCustomCursor(Texture2D tex)
    {
        if (tex != null)
        {
            Cursor.SetCursor(tex, Vector2.zero, CursorMode.ForceSoftware);
        }
    }

    void ApplyUpgrade(UpgradeLevelInfo def, float val)
    {
        currentUpgradeLevels[def.upgradeID]++;
        isMenuOpen = false;

        switch (def.type)
        {
            case UpgradeType.MoveSpeed:
                var moveScript = playerCombat.GetComponent(movementScriptName);
                if (moveScript != null)
                {
                    FieldInfo field = moveScript.GetType().GetField(movementVariableName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        float current = (float)field.GetValue(moveScript);
                        field.SetValue(moveScript, current + val);
                    }
                    else
                    {
                        PropertyInfo prop = moveScript.GetType().GetProperty(movementVariableName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (prop != null)
                        {
                            float current = (float)prop.GetValue(moveScript);
                            prop.SetValue(moveScript, current + val);
                        }
                    }
                }
                break;
            case UpgradeType.DamagePercentage: playerCombat.damageMultiplier += val; break;
            case UpgradeType.MaxHealth: playerHealth.maxHealth += (int)val; playerHealth.currentHealth += (int)val; break;
            case UpgradeType.AttackRange: playerCombat.attackRange += val; break;
            case UpgradeType.AttackRate: playerCombat.attackRate *= (1f - val); break;
            case UpgradeType.Knockback: playerCombat.knockbackForce += val; break;
        }

        SetCustomCursor(cursorNormal);
        levelSystem.CloseUpgradeMenu();
    }
}