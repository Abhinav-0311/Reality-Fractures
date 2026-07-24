using UnityEngine;
using UnityEngine.UI;

namespace RealityFractures
{
    public sealed class MinimalARUIController : MonoBehaviour
    {
        [SerializeField] private GameStateController gameState;
        [SerializeField] private Text statusText;
        [SerializeField] private Text progressText;

        private void Reset()
        {
            gameState = FindFirstObjectByType<GameStateController>();
        }

        private void OnEnable()
        {
            if (gameState == null)
            {
                return;
            }

            gameState.StateChanged += OnStateChanged;
            gameState.ProgressChanged += OnProgressChanged;
            OnStateChanged(gameState.CurrentState);
            OnProgressChanged(TimeLayer.Past, gameState.CollectedFragments, gameState.TotalFragments);
        }

        private void OnDisable()
        {
            if (gameState == null)
            {
                return;
            }

            gameState.StateChanged -= OnStateChanged;
            gameState.ProgressChanged -= OnProgressChanged;
        }

        private void OnStateChanged(RealityFracturesState state)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = state switch
            {
                RealityFracturesState.Scanning => "Find a flat surface",
                RealityFracturesState.ReadyToPlace => "Tap to open the fracture",
                RealityFracturesState.PastActive => "Collect the past fragment",
                RealityFracturesState.PresentActive => "Collect the present fragment",
                RealityFracturesState.FutureActive => "Collect the future fragment",
                RealityFracturesState.Stabilized => "Reality is stabilizing",
                RealityFracturesState.Complete => "Reality stabilized",
                _ => string.Empty
            };
        }

        private void OnProgressChanged(TimeLayer layer, int collected, int total)
        {
            if (progressText != null)
            {
                progressText.text = $"{collected}/{total}";
            }
        }
    }
}
