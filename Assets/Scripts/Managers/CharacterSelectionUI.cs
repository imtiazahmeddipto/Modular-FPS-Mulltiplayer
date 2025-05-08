using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform characterDisplay;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;

    [Header("Characters")]
    [SerializeField] private CharacterData[] characters; //charectername= it will show in ui, pathname=file name.

    private int currentIndex = 0;

    private void Start()
    {
        leftArrow.onClick.AddListener(() => SwitchCharacter(-1));
        rightArrow.onClick.AddListener(() => SwitchCharacter(1));

        LoadSavedCharacter();
        UpdateDisplay();
    }

    private void SwitchCharacter(int direction)
    {
        currentIndex = (currentIndex + direction + characters.Length) % characters.Length;
        UpdateDisplay();
    }

    private GameObject currentCharacterObj;

    private void UpdateDisplay()
    {
        // Destroy previous character object
        if (currentCharacterObj != null)
        {
            Destroy(currentCharacterObj);
        }

        // Instantiate new character object
        currentCharacterObj = Instantiate(characters[currentIndex].characterObj, characterDisplay.transform);

        // Update character name UI
        characterNameText.text = characters[currentIndex].characterName;

        // Save selected index
        PlayerPrefs.SetInt("SelectedCharacter", currentIndex);
        PlayerPrefs.Save();
        Debug.Log("Saved character index: " + currentIndex);
    }

    private void LoadSavedCharacter()
    {
        if (PlayerPrefs.HasKey("SelectedCharacter"))
            currentIndex = PlayerPrefs.GetInt("SelectedCharacter");
    }

    public static int GetSelectedCharacterIndex()
    {
        return PlayerPrefs.HasKey("SelectedCharacter") ?
            PlayerPrefs.GetInt("SelectedCharacter") : 0;
    }
}
[System.Serializable]
public class CharacterData
{
    public string characterName;
    public GameObject characterObj;
    public string prefabPath; // Path to PhotonPrefab
}