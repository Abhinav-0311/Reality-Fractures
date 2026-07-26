using UnityEngine;
using UnityEngine.EventSystems;

namespace RealityFractures
{
    [RequireComponent(typeof(Collider))]
    public sealed class TemporalLever : MonoBehaviour
    {
        [SerializeField] private TemporalPuzzleController puzzleController;
        [SerializeField] private Transform handleTransform;
        [SerializeField] private Renderer leverRenderer;
        [SerializeField] private Color activeGlowColor = new(0.36f, 0.9f, 0.95f, 1f);

        private bool isPulled = false;

        private void Awake()
        {
            if (puzzleController == null)
            {
                puzzleController = FindFirstObjectByType<TemporalPuzzleController>();
            }
            if (leverRenderer == null)
            {
                leverRenderer = GetComponent<Renderer>();
            }
            var col = GetComponent<SphereCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<SphereCollider>();
                col.radius = 0.75f;
                col.isTrigger = false;
            }
        }

        private void Update()
        {
            if (isPulled || Camera.main == null)
            {
                return;
            }

            if (WasPrimaryPressReleased() && !IsPointerOverUI() && WasPressedOnThisLever())
            {
                if (!isPulled)
                {
                    Temporal3DPuzzleTransitionController.EnterPuzzleMode(TimeLayer.Past, Pull);
                }
            }
        }

        public void Pull()
        {
            if (isPulled) return;

            isPulled = true;

            if (handleTransform != null)
            {
                handleTransform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            }

            TemporalVFXHelper.SpawnEnergyWave(transform.position, new Color(0.95f, 0.65f, 0.2f));
            TemporalVFXHelper.SpawnParticleBurst(transform.position + Vector3.up * 0.08f, new Color(0.95f, 0.7f, 0.2f), 35);

            if (puzzleController != null)
            {
                puzzleController.PullPastLever();
            }
        }

        private bool WasPressedOnThisLever()
        {
            Ray ray = Camera.main.ScreenPointToRay(GetPointerPosition());
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
            if (EventSystem.current == null) return false;
            if (Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
