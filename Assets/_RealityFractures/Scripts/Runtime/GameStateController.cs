using System;
using UnityEngine;

namespace RealityFractures
{
    public sealed class GameStateController : MonoBehaviour
    {
        [SerializeField] private RealityFracturesState initialState = RealityFracturesState.Scanning;

        public event Action<RealityFracturesState> StateChanged;
        public event Action<TimeLayer, int, int> ProgressChanged;

        public RealityFracturesState CurrentState { get; private set; }
        public int CollectedFragments { get; private set; }
        public int TotalFragments => 3;

        private void Awake()
        {
            SetState(initialState);
            PublishProgress();
        }

        public void PlaneAvailabilityChanged(bool hasTrackablePlane)
        {
            if (CurrentState == RealityFracturesState.Scanning && hasTrackablePlane)
            {
                SetState(RealityFracturesState.ReadyToPlace);
            }
            else if (CurrentState == RealityFracturesState.ReadyToPlace && !hasTrackablePlane)
            {
                SetState(RealityFracturesState.Scanning);
            }
        }

        public void FracturePlaced()
        {
            if (CurrentState == RealityFracturesState.ReadyToPlace)
            {
                SetState(RealityFracturesState.PastActive);
                PublishProgress();
            }
        }

        public void CollectFragment(TimeLayer layer)
        {
            if (!CanCollect(layer))
            {
                return;
            }

            CollectedFragments++;
            PublishProgress();

            switch (layer)
            {
                case TimeLayer.Past:
                    SetState(RealityFracturesState.PresentActive);
                    break;
                case TimeLayer.Present:
                    SetState(RealityFracturesState.FutureActive);
                    break;
                case TimeLayer.Future:
                    SetState(RealityFracturesState.Stabilized);
                    break;
            }
        }

        public void CompleteExperience()
        {
            if (CurrentState == RealityFracturesState.Stabilized)
            {
                SetState(RealityFracturesState.Complete);
            }
        }

        public void SelectTimeLayer(TimeLayer layer)
        {
            if (CurrentState == RealityFracturesState.Scanning || CurrentState == RealityFracturesState.ReadyToPlace || CurrentState == RealityFracturesState.Stabilized || CurrentState == RealityFracturesState.Complete)
            {
                return;
            }

            switch (layer)
            {
                case TimeLayer.Past:
                    SetState(RealityFracturesState.PastActive);
                    break;
                case TimeLayer.Present:
                    SetState(RealityFracturesState.PresentActive);
                    break;
                case TimeLayer.Future:
                    SetState(RealityFracturesState.FutureActive);
                    break;
            }
        }

        public void SelectPastLayer() => SelectTimeLayer(TimeLayer.Past);
        public void SelectPresentLayer() => SelectTimeLayer(TimeLayer.Present);
        public void SelectFutureLayer() => SelectTimeLayer(TimeLayer.Future);

        private bool CanCollect(TimeLayer layer)
        {
            return (CurrentState == RealityFracturesState.PastActive && layer == TimeLayer.Past)
                || (CurrentState == RealityFracturesState.PresentActive && layer == TimeLayer.Present)
                || (CurrentState == RealityFracturesState.FutureActive && layer == TimeLayer.Future);
        }

        private void SetState(RealityFracturesState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            CurrentState = nextState;
            StateChanged?.Invoke(CurrentState);
        }

        private void PublishProgress()
        {
            ProgressChanged?.Invoke(GetActiveLayer(), CollectedFragments, TotalFragments);
        }

        private TimeLayer GetActiveLayer()
        {
            return CurrentState switch
            {
                RealityFracturesState.PresentActive => TimeLayer.Present,
                RealityFracturesState.FutureActive => TimeLayer.Future,
                _ => TimeLayer.Past
            };
        }
    }
}
