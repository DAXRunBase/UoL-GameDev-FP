using UnityEngine;
using UnityEngine.InputSystem;

public class MyPlayerInput : MonoBehaviour
{
    [Header("Player Input states")]
    public Vector2 move;
    public bool jump;
    public bool sprint;

    private PauseMenuController pauseMenuController;

    private void Start()
    {
        pauseMenuController = FindObjectOfType<PauseMenuController>();
    }

    public void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        jump = value.isPressed;
    }

    public void OnSprint(InputValue value)
    {
        sprint = value.isPressed;
    }

    public void OnExit(InputValue value)
    {
        pauseMenuController.ShowMenu();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus && Time.timeScale > 0f)
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }
}
