using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RealityFractures
{
    public sealed class ARPlacementController : MonoBehaviour
    {
        private static readonly List<ARRaycastHit> Hits = new();

        [Header("AR")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARAnchorManager anchorManager;

        [Header("Placement")]
        [SerializeField] private GameObject placementIndicator;
        [SerializeField] private GameObject fracturePrefab;
        [SerializeField] private GameStateController gameState;

        private Pose latestPlacementPose;
        private bool hasValidPlacementPose;
        private bool hasPlacedFracture;
        private GameObject placedFracture;

        private void Reset()
        {
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
            planeManager = FindFirstObjectByType<ARPlaneManager>();
            anchorManager = FindFirstObjectByType<ARAnchorManager>();
            gameState = FindFirstObjectByType<GameStateController>();
        }

        private void Update()
        {
            if (hasPlacedFracture)
            {
                HandleZoomInput();
                return;
            }

            UpdatePlacementPose();
            UpdatePlacementIndicator();

            if (hasValidPlacementPose && WasPrimaryPressReleased() && !IsPointerOverUI())
            {
                PlaceFracture();
            }
        }

        private void UpdatePlacementPose()
        {
            Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
            hasValidPlacementPose = raycastManager != null
                && raycastManager.Raycast(screenCenter, Hits, TrackableType.PlaneWithinPolygon);

#if UNITY_EDITOR
            if (!hasValidPlacementPose && Camera.main != null)
            {
                latestPlacementPose = new Pose(
                    Camera.main.transform.position + Camera.main.transform.forward * 1.4f + Vector3.down * 0.25f,
                    Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f)
                );
                hasValidPlacementPose = true;
                gameState?.PlaneAvailabilityChanged(true);
                return;
            }
#endif

            if (!hasValidPlacementPose)
            {
                gameState?.PlaneAvailabilityChanged(HasTrackablePlane());
                return;
            }

            latestPlacementPose = Hits[0].pose;
            gameState?.PlaneAvailabilityChanged(true);
        }

        private void UpdatePlacementIndicator()
        {
            if (placementIndicator == null)
            {
                return;
            }

            placementIndicator.SetActive(hasValidPlacementPose);
            if (hasValidPlacementPose)
            {
                placementIndicator.transform.SetPositionAndRotation(latestPlacementPose.position, latestPlacementPose.rotation);
            }
        }

        private void PlaceFracture()
        {
            if (fracturePrefab == null)
            {
                Debug.LogWarning("Reality Fractures: no fracture prefab assigned.");
                return;
            }

            GameObject fracture = Instantiate(fracturePrefab, latestPlacementPose.position, latestPlacementPose.rotation);
            fracture.AddComponent<ARAnchor>();
            placedFracture = fracture;

            hasPlacedFracture = true;
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }

            SetPlaneVisualization(false);
            gameState?.FracturePlaced();
        }

        public void ZoomIn()
        {
            if (placedFracture != null)
            {
                placedFracture.transform.localScale = Vector3.ClampMagnitude(placedFracture.transform.localScale * 1.25f, 5.0f);
            }
        }

        public void ZoomOut()
        {
            if (placedFracture != null)
            {
                Vector3 newScale = placedFracture.transform.localScale * 0.8f;
                if (newScale.magnitude < 0.2f) newScale = Vector3.one * 0.2f;
                placedFracture.transform.localScale = newScale;
            }
        }

        private void HandleZoomInput()
        {
            if (placedFracture == null) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.005f)
            {
                float factor = 1f + scroll * 2.5f;
                Vector3 s = placedFracture.transform.localScale * factor;
                if (s.magnitude > 5.0f) s = s.normalized * 5.0f;
                if (s.magnitude < 0.2f) s = Vector3.one * 0.2f;
                placedFracture.transform.localScale = s;
            }

            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                Vector2 prev0 = t0.position - t0.deltaPosition;
                Vector2 prev1 = t1.position - t1.deltaPosition;

                float prevDist = (prev0 - prev1).magnitude;
                float curDist = (t0.position - t1.position).magnitude;
                float diff = (curDist - prevDist) * 0.005f;

                Vector3 s = placedFracture.transform.localScale * (1f + diff);
                if (s.magnitude > 5.0f) s = s.normalized * 5.0f;
                if (s.magnitude < 0.2f) s = Vector3.one * 0.2f;
                placedFracture.transform.localScale = s;
            }
        }

        private bool HasTrackablePlane()
        {
            if (planeManager == null)
            {
                return false;
            }

            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane.trackingState == TrackingState.Tracking)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetPlaneVisualization(bool isVisible)
        {
            if (planeManager == null)
            {
                return;
            }

            foreach (ARPlane plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(isVisible);
            }

            planeManager.enabled = isVisible;
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
