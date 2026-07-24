using System.Collections.Generic;
using UnityEngine;
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

        [Header("Placement")]
        [SerializeField] private GameObject placementIndicator;
        [SerializeField] private GameObject fracturePrefab;
        [SerializeField] private GameStateController gameState;

        private Pose latestPlacementPose;
        private bool hasValidPlacementPose;
        private bool hasPlacedFracture;

        private void Reset()
        {
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
            planeManager = FindFirstObjectByType<ARPlaneManager>();
            gameState = FindFirstObjectByType<GameStateController>();
        }

        private void Update()
        {
            if (hasPlacedFracture)
            {
                return;
            }

            UpdatePlacementPose();
            UpdatePlacementIndicator();

            if (hasValidPlacementPose && WasPrimaryPressReleased())
            {
                PlaceFracture();
            }
        }

        private void UpdatePlacementPose()
        {
            Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
            hasValidPlacementPose = raycastManager != null
                && raycastManager.Raycast(screenCenter, Hits, TrackableType.PlaneWithinPolygon);

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

            hasPlacedFracture = true;
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }

            SetPlaneVisualization(false);
            gameState?.FracturePlaced();
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
    }
}
