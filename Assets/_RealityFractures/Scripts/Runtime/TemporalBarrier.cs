using UnityEngine;

namespace RealityFractures
{
    public sealed class TemporalBarrier : MonoBehaviour
    {
        [SerializeField] private TimeLayer protectedLayer = TimeLayer.Present;
        [SerializeField] private TemporalPuzzleController puzzleController;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseScaleAmount = 0.05f;

        private Vector3 initialScale;
        private Vector3 initialLocalPosition;
        private bool isActive = true;

        public bool IsActive => isActive;

        private void Awake()
        {
            if (puzzleController == null)
            {
                puzzleController = FindFirstObjectByType<TemporalPuzzleController>();
            }
        }

        private void Start()
        {
            initialScale = transform.localScale;
            initialLocalPosition = transform.localPosition;

            if (puzzleController != null)
            {
                if (protectedLayer == TimeLayer.Past)
                {
                    puzzleController.PastOrbBarrierChanged += OnBarrierStateChanged;
                    isActive = puzzleController.IsOrbBlocked(TimeLayer.Past);
                }
                else if (protectedLayer == TimeLayer.Present)
                {
                    puzzleController.PresentOrbBarrierChanged += OnBarrierStateChanged;
                    isActive = puzzleController.IsOrbBlocked(TimeLayer.Present);
                }
                else if (protectedLayer == TimeLayer.Future)
                {
                    puzzleController.FutureOrbBarrierChanged += OnBarrierStateChanged;
                    isActive = puzzleController.IsOrbBlocked(TimeLayer.Future);
                }
                gameObject.SetActive(isActive);
            }
        }

        private void OnDestroy()
        {
            if (puzzleController != null)
            {
                if (protectedLayer == TimeLayer.Past)
                {
                    puzzleController.PastOrbBarrierChanged -= OnBarrierStateChanged;
                }
                else if (protectedLayer == TimeLayer.Present)
                {
                    puzzleController.PresentOrbBarrierChanged -= OnBarrierStateChanged;
                }
                else if (protectedLayer == TimeLayer.Future)
                {
                    puzzleController.FutureOrbBarrierChanged -= OnBarrierStateChanged;
                }
            }
        }

        private void Update()
        {
            if (isActive)
            {
                float newY = initialLocalPosition.y + Mathf.Sin(Time.time * 2.5f) * 0.015f;
                transform.localPosition = new Vector3(initialLocalPosition.x, newY, initialLocalPosition.z);
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount;
                transform.localScale = initialScale * pulse;
                transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
            }
        }

        private void OnBarrierStateChanged(bool activeState)
        {
            isActive = activeState;
            gameObject.SetActive(activeState);
        }
    }
}
