using UnityEngine;
using UnityEngine.EventSystems;

namespace RealityFractures
{
    [RequireComponent(typeof(Collider))]
    public sealed class CyberneticTerminal : MonoBehaviour
    {
        [SerializeField] private TemporalPuzzleController puzzleController;
        [SerializeField] private Renderer terminalRenderer;
        [SerializeField] private Color unlockedGlowColor = new(0.2f, 1f, 0.8f, 1f);

        private bool isUnlocked = false;

        private void Awake()
        {
            if (puzzleController == null)
            {
                puzzleController = FindFirstObjectByType<TemporalPuzzleController>();
            }
            if (terminalRenderer == null)
            {
                terminalRenderer = GetComponent<Renderer>();
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
            if (isUnlocked || Camera.main == null)
            {
                return;
            }

            if (WasPrimaryPressReleased() && !IsPointerOverUI() && WasPressedOnThisTerminal())
            {
                if (!isUnlocked)
                {
                    Temporal3DPuzzleTransitionController.EnterPuzzleMode(TimeLayer.Future, Unlock);
                }
            }
        }

        public void Unlock()
        {
            if (isUnlocked) return;

            isUnlocked = true;

            if (terminalRenderer != null && terminalRenderer.material != null && terminalRenderer.material.HasProperty("_EmissionColor"))
            {
                terminalRenderer.material.SetColor("_EmissionColor", unlockedGlowColor * 0.3f);
            }

            TemporalVFXHelper.SpawnEnergyWave(transform.position, new Color(0.2f, 0.7f, 1.0f));
            TemporalVFXHelper.SpawnParticleBurst(transform.position + Vector3.up * 0.08f, new Color(0.2f, 0.75f, 1.0f), 35);

            if (puzzleController != null)
            {
                puzzleController.UnlockFutureTerminal();
            }
        }

        private bool WasPressedOnThisTerminal()
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
