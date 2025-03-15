using UnityEngine;
using UnityEngine.UIElements;

namespace HUD
{
    public class HUDController : MonoBehaviour
    {
        public UIDocument uiDocument;
        public PlayerController playerController;

        private VisualElement healthFill;

        private void Start()
        {
            // Get the root VisualElement from your UIDocument
            var root = uiDocument.rootVisualElement;

            // Find the fill element by class or name
            healthFill = root.Q<VisualElement>(className: "healthbarFill");

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
    }
}