using UnityEngine;
using UnityEngine.EventSystems;

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
        [SerializeField] private float bobHeight = 0.015f;
        [SerializeField] private float bobSpeed = 2.5f;
        [SerializeField] private float rotationSpeed = 35f;

        private Vector3 initialLocalPosition;
        private bool isCollected;
        private TemporalPuzzleController puzzleController;

        public TimeLayer Layer => layer;

        private void Awake()
        {
            if (gameState == null)
            {
                gameState = FindFirstObjectByType<GameStateController>();
            }
            puzzleController = FindFirstObjectByType<TemporalPuzzleController>();
        }

        private void Reset()
        {
            gameState = FindFirstObjectByType<GameStateController>();
            puzzleController = FindFirstObjectByType<TemporalPuzzleController>();
        }

        private void Start()
        {
            initialLocalPosition = transform.localPosition;
            if (puzzleController == null)
            {
                puzzleController = FindFirstObjectByType<TemporalPuzzleController>();
            }
        }

        private void Update()
        {
            if (!isCollected)
            {
                float newY = initialLocalPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.localPosition = new Vector3(initialLocalPosition.x, newY, initialLocalPosition.z);
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

                if (WasPrimaryPressReleased() && !IsPointerOverUI() && WasPressedOnThisFragment())
                {
                    TryCollect();
                    return;
                }
            }

            if (isCollected || proximityCollectDistance <= 0f || Camera.main == null)
            {
                return;
            }

            float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
            if (distance <= proximityCollectDistance)
            {
                TryCollect();
            }
        }

        private void TryCollect()
        {
            if (puzzleController != null && puzzleController.IsOrbBlocked(layer))
            {
                puzzleController.NotifyOrbBlocked(layer);
                return;
            }
            Collect();
        }

        public void Collect()
        {
            if (isCollected)
            {
                return;
            }

            isCollected = true;

            Color orbColor = layer == TimeLayer.Past ? new Color(0.95f, 0.75f, 0.2f) :
                             layer == TimeLayer.Present ? new Color(0.2f, 0.95f, 0.5f) :
                             new Color(0.2f, 0.75f, 1.0f);
            TemporalVFXHelper.SpawnParticleBurst(transform.position, orbColor, 45);
            TemporalVFXHelper.SpawnEnergyWave(transform.position, orbColor);

            if (collectEffect != null)
            {
                collectEffect.transform.SetParent(null, true);
                collectEffect.Play();
            }

            if (collectAudio != null)
            {
                collectAudio.Play();
            }

            if (puzzleController != null)
            {
                puzzleController.OnOrbCollected(layer);
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

            Ray ray = mainCamera.ScreenPointToRay(GetPointerPosition());
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                return hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);
            }

            return false;
        }

        private static Vector3 GetPointerPosition()
        {
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).position;
            }
            return Input.mousePosition;
        }

        private static bool WasPrimaryPressReleased()
        {
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).phase == TouchPhase.Ended;
            }

            return Input.GetMouseButtonUp(0);
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            if (Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
