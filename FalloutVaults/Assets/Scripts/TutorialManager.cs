using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] public MyPlayerInput playerInput;
    [SerializeField] public TextMeshProUGUI tutorialText;

    private bool hasMoved = false;
    private bool hasMovedLeft = false;
    private bool hasMovedRight = false;
    private bool hasSprinted = false;
    private bool hasJumped = false;

    private void Start()
    {
        tutorialText.text = "Press [A], [D] on the keyboard, or [Left], [Right] on your D-pad to move";
        StartCoroutine(FadeInText());
    }

    private void Update()
    {
        if (!hasMoved)
        {
            if (playerInput.move == Vector2.left)
            {
                hasMovedLeft = true;
            }
            else if (playerInput.move == Vector2.right)
            {
                hasMovedRight = true;
            }

            if (hasMovedLeft && hasMovedRight)
            {
                hasMoved = true;
                StartCoroutine(SwitchTutorialText("Press [SHIFT] to sprint"));
            }
        }
        else if (!hasSprinted)
        {
            if (playerInput.sprint)
            {
                hasSprinted = true;
                StartCoroutine(SwitchTutorialText("Press [SPACE] to jump"));
            }
        }
        else if (!hasJumped)
        {
            if (playerInput.jump)
            {
                hasJumped = true;
                StartCoroutine(SwitchTutorialText("")); // Hide text at the end
            }
        }
    }

    // Coroutine to fade out text, change it, and fade back in
    private IEnumerator SwitchTutorialText(string newText)
    {
        yield return StartCoroutine(FadeOutText());
        tutorialText.text = newText;
        yield return StartCoroutine(FadeInText());
    }

    // Coroutine to fade out text
    private IEnumerator FadeOutText()
    {
        Color textColor = tutorialText.color;
        float fadeDuration = 0.5f; // Duration of fade effect

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            textColor.a = Mathf.Lerp(1, 0, t / fadeDuration);
            tutorialText.color = textColor;
            yield return null;
        }

        textColor.a = 0;
        tutorialText.color = textColor;
    }

    // Coroutine to fade in text
    private IEnumerator FadeInText()
    {
        Color textColor = tutorialText.color;
        float fadeDuration = 0.5f; // Duration of fade effect

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            textColor.a = Mathf.Lerp(0, 1, t / fadeDuration);
            tutorialText.color = textColor;
            yield return null;
        }

        textColor.a = 1;
        tutorialText.color = textColor;
    }
}
