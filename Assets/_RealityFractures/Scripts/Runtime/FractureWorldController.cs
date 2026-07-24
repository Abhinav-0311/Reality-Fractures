using System.Collections;
using UnityEngine;

namespace RealityFractures
{
    public sealed class FractureWorldController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private GameStateController gameState;

        [Header("Time Layers")]
        [SerializeField] private GameObject pastWorld;
        [SerializeField] private GameObject presentWorld;
        [SerializeField] private GameObject futureWorld;

        [Header("Feedback")]
        [SerializeField] private ParticleSystem transitionEffect;
        [SerializeField] private ParticleSystem stabilizeEffect;
        [SerializeField] private AudioSource transitionAudio;
        [SerializeField] private float revealDuration = 0.45f;

        private Coroutine revealRoutine;

        private void Awake()
        {
            if (gameState == null)
            {
                gameState = FindFirstObjectByType<GameStateController>();
            }
        }

        private void Reset()
        {
            gameState = FindFirstObjectByType<GameStateController>();
        }

        private void OnEnable()
        {
            if (gameState != null)
            {
                gameState.StateChanged += OnStateChanged;
                OnStateChanged(gameState.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (gameState != null)
            {
                gameState.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(RealityFracturesState state)
        {
            switch (state)
            {
                case RealityFracturesState.PastActive:
                    ShowLayer(pastWorld);
                    break;
                case RealityFracturesState.PresentActive:
                    ShowLayer(presentWorld);
                    break;
                case RealityFracturesState.FutureActive:
                    ShowLayer(futureWorld);
                    break;
                case RealityFracturesState.Stabilized:
                    Stabilize();
                    break;
            }
        }

        private void ShowLayer(GameObject activeWorld)
        {
            SetActiveLayer(activeWorld);

            if (transitionEffect != null)
            {
                transitionEffect.Play();
            }

            if (transitionAudio != null)
            {
                transitionAudio.Play();
            }

            if (activeWorld != null)
            {
                if (revealRoutine != null)
                {
                    StopCoroutine(revealRoutine);
                }

                revealRoutine = StartCoroutine(Reveal(activeWorld.transform));
            }
        }

        private void SetActiveLayer(GameObject activeWorld)
        {
            if (pastWorld != null)
            {
                pastWorld.SetActive(pastWorld == activeWorld);
            }

            if (presentWorld != null)
            {
                presentWorld.SetActive(presentWorld == activeWorld);
            }

            if (futureWorld != null)
            {
                futureWorld.SetActive(futureWorld == activeWorld);
            }
        }

        private IEnumerator Reveal(Transform target)
        {
            Vector3 finalScale = target.localScale;
            target.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / revealDuration);
                target.localScale = Vector3.LerpUnclamped(Vector3.zero, finalScale, Smooth(t));
                yield return null;
            }

            target.localScale = finalScale;
        }

        private void Stabilize()
        {
            SetActiveLayer(futureWorld);

            if (stabilizeEffect != null)
            {
                stabilizeEffect.Play();
            }

            StartCoroutine(CompleteAfterDelay(1.25f));
        }

        private IEnumerator CompleteAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameState?.CompleteExperience();
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
