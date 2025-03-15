using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseMenuController : MonoBehaviour
{
    public VisualElement ui;
    public VisualElement MenuPanel;

    public Button ResumeButton;
    public Button MainMenuButton;
    public Button ExitButton;

    private const string HIDE_CLASS = "hide";

    private void Awake()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
        MenuPanel = ui.Q<VisualElement>("Panel");

        // Initially hide the menu using your .hide selector
        MenuPanel.AddToClassList(HIDE_CLASS);
    }

    private void OnEnable()
    {
        ResumeButton = ui.Q<Button>("ResumeButton");
        ResumeButton.clicked += ResumeButton_clicked;

        MainMenuButton = ui.Q<Button>("MainMenuButton");
        MainMenuButton.clicked += MainMenuButton_clicked;

        ExitButton = ui.Q<Button>("ExitButton");
        ExitButton.clicked += ExitButton_clicked;
    }

    private void OnDisable()
    {
        ResumeButton.clicked -= ResumeButton_clicked;
        MainMenuButton.clicked -= MainMenuButton_clicked;
        ExitButton.clicked -= ExitButton_clicked;
    }

    private void ResumeButton_clicked()
    {
        MenuPanel.AddToClassList(HIDE_CLASS); // Hide menu
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f; // Resume game
    }

    private void MainMenuButton_clicked()
    {
        Time.timeScale = 1f; // Resume game before going back
        SceneManager.LoadScene("MainMenu");
    }

    private void ExitButton_clicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    public void ShowMenu()
    {
        MenuPanel.RemoveFromClassList(HIDE_CLASS); // Show menu
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f; // Pause game
    }
}
