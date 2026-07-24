using UnityEngine;

namespace RealityFractures
{
    [RequireComponent(typeof(Collider))]
    public sealed class CollectibleFragment : MonoBehaviour
    {
        [SerializeField] private TimeLayer layer;
        [SerializeField] private GameStateController gameState;
        [SerializeField] private ParticleSystem collectEffect;
        [SerializeField] private AudioSource collectAudio;
        [SerializeField] private float proximityCollectDistance = 0f;

        private bool isCollected;

        public TimeLayer Layer => layer;

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

        private void Update()
        {
            if (!isCollected && WasPrimaryPressReleased() && WasPressedOnThisFragment())
            {
                Collect();
                return;
            }

            if (isCollected || proximityCollectDistance <= 0f || Camera.main == null)
            {
                return;
            }

            float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
            if (distance <= proximityCollectDistance)
            {
                Collect();
            }
        }

        public void Collect()
        {
            if (isCollected)
            {
                return;
            }

            isCollected = true;

            if (collectEffect != null)
            {
                collectEffect.transform.SetParent(null, true);
                collectEffect.Play();
            }

            if (collectAudio != null)
            {
                collectAudio.Play();
            }

            gameState?.CollectFragment(layer);
            gameObject.SetActive(false);
        }

        private bool WasPressedOnThisFragment()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }

            Vector2 screenPosition = Input.touchCount > 0
                ? Input.GetTouch(0).position
                : (Vector2)Input.mousePosition;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform == transform)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WasPrimaryPressReleased()
        {
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).phase == TouchPhase.Ended;
            }

            return Input.GetMouseButtonUp(0);
        }
    }
}
