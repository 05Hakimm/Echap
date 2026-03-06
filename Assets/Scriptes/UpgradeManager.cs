using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

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
    public class UpgradeDefinition
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
    public string movementVariableName = "moveSpeed"; // Nom de ta variable float dans ton script de mouvement

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

    [Header("Base de données des pouvoirs")]
    public List<UpgradeDefinition> availableUpgrades;

    private Dictionary<string, int> currentUpgradeLevels = new Dictionary<string, int>();
    private PlayerCombat playerCombat;
    private Health playerHealth;
    private LevelSystem levelSystem;

    void Awake()
    {
        foreach (var def in availableUpgrades)
        {
            if (!string.IsNullOrEmpty(def.upgradeID))
                currentUpgradeLevels[def.upgradeID] = -1;
        }

        if (modelCommun) modelCommun.SetActive(false);
        if (modelRare) modelRare.SetActive(false);
        if (modelEpique) modelEpique.SetActive(false);
        if (modelLegendaire) modelLegendaire.SetActive(false);
        if (modelMythique) modelMythique.SetActive(false);
    }

    void Start() { Invoke("ForceCloseAtStart", 0.15f); }

    void ForceCloseAtStart()
    {
        if (RefreshPlayerReferences()) levelSystem.CloseUpgradeMenu();
        else { gameObject.SetActive(false); Time.timeScale = 1f; }
    }

    bool RefreshPlayerReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerCombat = playerObj.GetComponent<PlayerCombat>();
            playerHealth = playerObj.GetComponent<Health>();
            levelSystem = playerObj.GetComponent<LevelSystem>();
            return playerCombat != null && playerHealth != null && levelSystem != null;
        }
        return false;
    }

    public void GenerateUpgradeChoices()
    {
        gameObject.SetActive(true);
        if (!RefreshPlayerReferences()) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetCustomCursor(cursorNormal);

        List<UpgradeDefinition> possibleChoices = new List<UpgradeDefinition>();
        foreach (var def in availableUpgrades)
        {
            int nextLevel = currentUpgradeLevels[def.upgradeID] + 1;
            if (def.useScalingInsteadOfList || nextLevel < def.levels.Count)
                possibleChoices.Add(def);
        }

        for (int i = 0; i < possibleChoices.Count; i++)
        {
            UpgradeDefinition temp = possibleChoices[i];
            int randomIndex = Random.Range(i, possibleChoices.Count);
            possibleChoices[i] = possibleChoices[randomIndex];
            possibleChoices[randomIndex] = temp;
        }

        for (int i = 0; i < activeButtons.Length; i++)
        {
            if (i >= possibleChoices.Count) { activeButtons[i].gameObject.SetActive(false); continue; }

            activeButtons[i].gameObject.SetActive(true);
            UpgradeDefinition chosenDef = possibleChoices[i];
            int nextLevelIndex = currentUpgradeLevels[chosenDef.upgradeID] + 1;

            float valToApply = chosenDef.useScalingInsteadOfList ? chosenDef.valuePerLevel : chosenDef.levels[nextLevelIndex].value;

            string titleStr = chosenDef.title;
            string levelStr = "LVL " + (nextLevelIndex + 1);
            string descStr = "";

            if (chosenDef.useScalingInsteadOfList)
            {
                float displayVal = chosenDef.valuePerLevel;
                if (chosenDef.type == UpgradeType.DamagePercentage || chosenDef.type == UpgradeType.AttackRate)
                    displayVal *= 100f;
                descStr = string.Format(chosenDef.scalingDescription, displayVal);
            }
            else
            {
                descStr = chosenDef.levels[nextLevelIndex].description;
            }

            ApplyVisualRarity(activeButtons[i], chosenDef.rarity);
            UpdateAllTextComponents(activeButtons[i].gameObject, titleStr, descStr, levelStr);

            Image[] images = activeButtons[i].GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.gameObject.name == "Icon") img.sprite = chosenDef.icon;
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
            if (txt != null) { txt.text = content; }
        }
    }

    void ApplyVisualRarity(Button targetButton, Rarity rarity)
    {
        GameObject model = GetModelByRarity(rarity);
        if (model == null) return;

        Image targetBg = targetButton.GetComponent<Image>();
        Image modelBg = model.GetComponent<Image>();
        if (targetBg && modelBg)
        {
            targetBg.sprite = modelBg.sprite;
            targetBg.color = modelBg.color;
        }

        string[] partsToSync = { "Sword_Bas", "Sword_Milieu", "Sword_Haut" };
        Image[] targetImages = targetButton.GetComponentsInChildren<Image>(true);
        Image[] modelImages = model.GetComponentsInChildren<Image>(true);

        foreach (string partName in partsToSync)
        {
            Image tImg = System.Array.Find(targetImages, x => x.gameObject.name == partName);
            Image mImg = System.Array.Find(modelImages, x => x.gameObject.name == partName);
            if (tImg != null && mImg != null)
            {
                tImg.sprite = mImg.sprite;
                tImg.color = mImg.color;
            }
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

    void SetCustomCursor(Texture2D tex) { if (tex != null) Cursor.SetCursor(tex, Vector2.zero, CursorMode.Auto); }

    void ApplyUpgrade(UpgradeDefinition def, float val)
    {
        currentUpgradeLevels[def.upgradeID]++;
        Debug.Log("<color=green>AMÉLIORATION APPLIQUÉE :</color> " + def.title + " (+" + val + ")");

        switch (def.type)
        {
            case UpgradeType.MoveSpeed:
                var move = playerCombat.GetComponent(movementScriptName);
                if (move != null)
                {
                    // Utilise maintenant la variable configurée (moveSpeed par défaut)
                    var field = move.GetType().GetField(movementVariableName);
                    if (field != null)
                    {
                        float currentVal = (float)field.GetValue(move);
                        field.SetValue(move, currentVal + val);
                        Debug.Log("NOUVELLE VITESSE (" + movementVariableName + ") : " + (currentVal + val));
                    }
                    else { Debug.LogError("Variable '" + movementVariableName + "' introuvable dans " + movementScriptName); }
                }
                else { Debug.LogError("Script " + movementScriptName + " introuvable sur le joueur !"); }
                break;

            case UpgradeType.DamagePercentage:
                playerCombat.damageMultiplier += val;
                Debug.Log("NOUVEAU MULTIPLICATEUR DE DÉGÂTS : " + playerCombat.damageMultiplier);
                break;

            case UpgradeType.MaxHealth:
                playerHealth.maxHealth += (int)val;
                playerHealth.currentHealth += (int)val;
                Debug.Log("NOUVELLE VIE MAX : " + playerHealth.maxHealth);
                break;

            case UpgradeType.AttackRate:
                playerCombat.attackRate *= (1f - val);
                Debug.Log("NOUVELLE VITESSE D'ATTAQUE : " + playerCombat.attackRate);
                break;

            case UpgradeType.Knockback:
                playerCombat.knockbackForce += val;
                Debug.Log("NOUVEAU RECUL : " + playerCombat.knockbackForce);
                break;

            case UpgradeType.AttackRange:
                playerCombat.attackRange += val;
                Debug.Log("NOUVELLE PORTÉE : " + playerCombat.attackRange);
                break;
        }
        levelSystem.CloseUpgradeMenu();
    }
}