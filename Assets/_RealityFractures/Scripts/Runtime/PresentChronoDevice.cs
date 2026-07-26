using UnityEngine;
using UnityEngine.EventSystems;

namespace RealityFractures
{
    [RequireComponent(typeof(Collider))]
    public sealed class PresentChronoDevice : MonoBehaviour
    {
        [SerializeField] private TemporalPuzzleController puzzleController;
        [SerializeField] private Transform energyRing;
        [SerializeField] private float rotationSpeed = 60f;

        private bool isRiftOpen = false;

        private void Awake()
        {
            if (puzzleController == null)
            {
                puzzleController = FindFirstObjectByType<TemporalPuzzleController>();
            }
            var col = GetComponent<SphereCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<SphereCollider>();
                col.radius = 0.75f;
                col.isTrigger = false;
            }
        }

        private void Start()
        {
            if (puzzleController != null)
            {
                puzzleController.RiftOpened += OnRiftOpened;
            }
        }

        private void OnDestroy()
        {
            if (puzzleController != null)
            {
                puzzleController.RiftOpened -= OnRiftOpened;
            }
        }

        private void Update()
        {
            if (isRiftOpen && energyRing != null)
            {
                energyRing.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }

            if (Camera.main == null) return;

            if (WasPrimaryPressReleased() && !IsPointerOverUI() && WasPressedOnThisDevice())
            {
                if (puzzleController != null)
                {
                    if (!isRiftOpen)
                    {
                        Temporal3DPuzzleTransitionController.EnterPuzzleMode(TimeLayer.Present, () => puzzleController.TouchPresentDevice());
                    }
                    else if (puzzleController.CurrentPhase == TemporalPuzzleController.RiftPhase.FutureSynced_ReturnToPresent)
                    {
                        Temporal3DPuzzleTransitionController.EnterPuzzleMode(TimeLayer.Present, () => {
                            TemporalVFXHelper.SpawnRealityFireworks(transform.position);
                            puzzleController.TouchPresentDevice();
                        });
                    }
                    else
                    {
                        if (puzzleController.CurrentPhase == TemporalPuzzleController.RiftPhase.DeviceRepaired)
                        {
                            TemporalVFXHelper.SpawnRealityFireworks(transform.position);
                        }
                        puzzleController.TouchPresentDevice();
                    }
                }
            }
        }

        private void OnRiftOpened()
        {
            isRiftOpen = true;
            rotationSpeed = 180f; // Spin faster when rift opens!
            TemporalVFXHelper.SpawnEnergyWave(transform.position, new Color(0.2f, 0.95f, 0.6f));
            TemporalVFXHelper.SpawnParticleBurst(transform.position + Vector3.up * 0.08f, new Color(0.2f, 0.95f, 0.6f), 40);
        }

        private bool WasPressedOnThisDevice()
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
