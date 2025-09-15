using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PauseMenu : MonoBehaviour
{
    //panels
    public GameObject pauseMenu;
    public GameObject settingsMenu;

    //Buttons
    public Button resume;
    public Button backToMenu;
    public Button settings;
    public Button returntopause;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        resume.onClick.AddListener(ReturnToGame);
        settings.onClick.AddListener(SettingsMenu);
        backToMenu.onClick.AddListener(QuitGame);
        returntopause.onClick.AddListener(Pausemenu);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !pauseMenu.activeSelf) // Press Escape for pause 
        {
            Pausemenu();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape) && pauseMenu.activeSelf)
        {
            ReturnToGame();
            return;
        }
    }
    private void Pausemenu()
    {
        Time.timeScale = 0.0f;
        pauseMenu.SetActive(true);
        settingsMenu.SetActive(false);

    }
    private void SettingsMenu()
    {
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }
    private void ReturnToGame()
    {
        Time.timeScale = 1.0f;
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);
    }
    private void QuitGame()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("MainMenu");
    }
}

