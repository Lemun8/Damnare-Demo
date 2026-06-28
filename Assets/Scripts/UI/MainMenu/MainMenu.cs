using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button newGameButton;
    public Button optionsButton;
    public Button exitButton;

    [Header("Options Panel")]
    public GameObject optionsPanel;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Button closeOptionsButton;

    [Header("Audio")]
    public AudioMixer audioMixer;

    const string BGM_KEY = "BGMVolume";
    const string SFX_KEY = "SFXVolume";

    void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Button bindings
        newGameButton.onClick.AddListener(OnNewGame);
        optionsButton.onClick.AddListener(OpenOptions);
        exitButton.onClick.AddListener(OnExit);

        closeOptionsButton.onClick.AddListener(CloseOptions);

        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        LoadVolumes();
    }

    void OnDestroy()
    {
        newGameButton.onClick.RemoveAllListeners();
        optionsButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
        closeOptionsButton.onClick.RemoveAllListeners();
        bgmSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
    }

    // -------------------------
    // Menu actions
    // -------------------------
    void OnNewGame()
    {
        Debug.Log("🆕 New Game started — clearing all saved data");

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        PlayerGameState.ResetToDefaults();
        ResetRuntimeSingletons();

        SceneManager.LoadScene("CharacterSelection");
    }

    void OpenOptions()
    {
        optionsPanel.SetActive(true);
    }

    void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    void OnExit()
    {
        Debug.Log("❌ Exiting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // -------------------------
    // Audio
    // -------------------------
    void SetBGMVolume(float value)
    {
        audioMixer.SetFloat(BGM_KEY, LinearToDecibel(value));
        PlayerPrefs.SetFloat(BGM_KEY, value);
    }

    void SetSFXVolume(float value)
    {
        audioMixer.SetFloat(SFX_KEY, LinearToDecibel(value));
        PlayerPrefs.SetFloat(SFX_KEY, value);
    }

    void LoadVolumes()
    {
        float bgm = PlayerPrefs.GetFloat(BGM_KEY, 1f);
        float sfx = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        bgmSlider.value = bgm;
        sfxSlider.value = sfx;

        audioMixer.SetFloat(BGM_KEY, LinearToDecibel(bgm));
        audioMixer.SetFloat(SFX_KEY, LinearToDecibel(sfx));
    }

    float LinearToDecibel(float value)
    {
        if (value <= 0.0001f)
            return -80f; // silent
        return Mathf.Log10(value) * 20f;
    }

    // -------------------------
    // Runtime reset
    // -------------------------
    void ResetRuntimeSingletons()
    {
        if (CharacterDataContainer.Instance != null)
            CharacterDataContainer.Instance.playerCombatData = null;

        if (InventoryDataContainer.Instance != null)
            InventoryDataContainer.Instance.Clear();

        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.ClearAll();

        Debug.Log("🧹 Runtime singletons reset");
    }
}
