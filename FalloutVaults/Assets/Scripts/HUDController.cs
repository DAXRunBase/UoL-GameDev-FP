using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace HUD
{
    public class HUDController : MonoBehaviour
    {
        public UIDocument uiDocument;
        public PlayerController playerController;

        private VisualElement healthFill;
        private VisualElement deathMessage;

        private void Start()
        {
            // Get the root VisualElement from your UIDocument
            var root = uiDocument.rootVisualElement;

            // Find the fill element by class or name
            healthFill = root.Q<VisualElement>(className: "healthbarFill");

            deathMessage = root.Q<Label>("DeathMessage");
            deathMessage.style.opacity = 0;

            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();
        }

        private void Update()
        {
            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            float healthPercent = playerController.Health / playerController.MaxHealth;

            // Update health fill width percentage
            healthFill.style.width = Length.Percent(healthPercent * 100);
        }

        public void FadeInDeathMessage(float duration = 2f)
        {
            StartCoroutine(FadeInRoutine(duration));
        }

        private IEnumerator FadeInRoutine(float duration)
        {
            float elapsed = 0f;
            deathMessage.style.opacity = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / duration);
                deathMessage.style.opacity = alpha;
                yield return null;
            }

            deathMessage.style.opacity = 1f;
        }

        public void ResetMessage()
        {
            deathMessage.style.opacity = 0f;
        }
    }
}