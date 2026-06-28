using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLoader : MonoBehaviour
{
    /// <summary>Called by the Timeline Signal Receiver at cutscene end.</summary>
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
