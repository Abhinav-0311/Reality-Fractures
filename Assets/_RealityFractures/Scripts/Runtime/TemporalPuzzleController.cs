using System;
using UnityEngine;

namespace RealityFractures
{
    public sealed class TemporalPuzzleController : MonoBehaviour
    {
        public enum RiftPhase
        {
            Dormant,                     // Phase 0: Present Device is dormant. Click to OPEN RIFT!
            RiftOpened_GoToPast,         // Phase 1: Rift opened! Go to Past Era and pull the Ancient Lever!
            PastSynced_GoToFuture,       // Phase 2: Past Orb collected! Go to Future Era and unlock Cyber-Terminal!
            FutureSynced_ReturnToPresent,// Phase 3: Future Orb collected! Return to Present Era to repair Device!
            DeviceRepaired               // Phase 4: Present Orb collected! Device repaired & Rift Sealed!
        }

        [SerializeField] private RiftPhase currentPhase = RiftPhase.Dormant;
        [SerializeField] private bool isPastLeverPulled = false;
        [SerializeField] private bool isFutureTerminalUnlocked = false;

        public event Action<string> PuzzleStatusUpdated;
        public event Action<bool> PastOrbBarrierChanged;
        public event Action<bool> FutureOrbBarrierChanged;
        public event Action<bool> PresentOrbBarrierChanged;
        public event Action RiftOpened;

        public RiftPhase CurrentPhase => currentPhase;
        public bool IsPastLeverPulled => isPastLeverPulled;
        public bool IsFutureTerminalUnlocked => isFutureTerminalUnlocked;

        private ProceduralAudioFX audioFX;

        private void Awake()
        {
            currentPhase = RiftPhase.Dormant;
            isPastLeverPulled = false;
            isFutureTerminalUnlocked = false;
            audioFX = GetComponent<ProceduralAudioFX>();
        }

        public void TouchPresentDevice()
        {
            if (audioFX != null)
            {
                audioFX.PlayRiftOpenSound();
            }

            PresentOrbBarrierChanged?.Invoke(false); // Drop Present Emerald Orb shield
            PuzzleStatusUpdated?.Invoke("[PUZZLE SOLVED] Present Device Activated! Emerald Chrono-Core is Unlocked!");
            Debug.Log("[TemporalPuzzleController] Present puzzle solved! Emerald Orb barrier dropped.");
        }

        public void PullPastLever()
        {
            if (isPastLeverPulled) return;

            isPastLeverPulled = true;
            PastOrbBarrierChanged?.Invoke(false); // Drop Past Amber Orb shield

            if (audioFX != null)
            {
                audioFX.PlayPuzzleSolveSound();
            }

            PuzzleStatusUpdated?.Invoke("[PUZZLE SOLVED] Ancient Altar Activated! Amber Chrono-Core is Unlocked!");
            Debug.Log("[TemporalPuzzleController] Past lever pulled! Amber Orb barrier dropped.");
        }

        public void UnlockFutureTerminal()
        {
            if (isFutureTerminalUnlocked) return;

            isFutureTerminalUnlocked = true;
            FutureOrbBarrierChanged?.Invoke(false); // Drop Future Cyan Orb shield

            if (audioFX != null)
            {
                audioFX.PlayPuzzleSolveSound();
            }

            PuzzleStatusUpdated?.Invoke("[PUZZLE SOLVED] Cybernetic Lock Bypassed! Cyan Chrono-Core is Unlocked!");
            Debug.Log("[TemporalPuzzleController] Future terminal unlocked! Cyan Orb barrier dropped.");
        }

        public void OnOrbCollected(TimeLayer layer)
        {
            if (audioFX != null)
            {
                if (layer == TimeLayer.Present)
                {
                    audioFX.PlayVictoryChordSound();
                }
                else
                {
                    audioFX.PlayOrbCollectSound();
                }
            }

            switch (layer)
            {
                case TimeLayer.Past:
                    currentPhase = RiftPhase.PastSynced_GoToFuture;
                    PuzzleStatusUpdated?.Invoke("[PAST SYNCED] Conduit aligned! Now travel to FUTURE ERA to bypass the Cybernetic Lock!");
                    break;
                case TimeLayer.Future:
                    currentPhase = RiftPhase.FutureSynced_ReturnToPresent;
                    PresentOrbBarrierChanged?.Invoke(false); // Unlock Present Emerald Orb for Device repair
                    PuzzleStatusUpdated?.Invoke("[FUTURE SYNCED] Lock bypassed! Return to PRESENT ERA to repair the Central Device!");
                    break;
                case TimeLayer.Present:
                    currentPhase = RiftPhase.DeviceRepaired;
                    PuzzleStatusUpdated?.Invoke("REALITY STABILIZED | Central Device Repaired & Rift Sealed!");
                    break;
            }
        }

        public bool IsOrbBlocked(TimeLayer layer)
        {
            return layer switch
            {
                TimeLayer.Past => currentPhase == RiftPhase.Dormant || !isPastLeverPulled,
                TimeLayer.Future => currentPhase == RiftPhase.Dormant || currentPhase == RiftPhase.RiftOpened_GoToPast || !isFutureTerminalUnlocked,
                _ => currentPhase != RiftPhase.FutureSynced_ReturnToPresent // Present orb only unlocked in Phase 3
            };
        }

        public void NotifyOrbBlocked(TimeLayer layer)
        {
            if (currentPhase == RiftPhase.Dormant)
            {
                PuzzleStatusUpdated?.Invoke("[DEVICE DORMANT] Touch the Central Device in PRESENT ERA first to open the Rift!");
                return;
            }

            string msg = layer switch
            {
                TimeLayer.Past => "[ORB LOCKED] Pull the Ancient Altar Lever first to unlock the Amber Orb!",
                TimeLayer.Future => currentPhase == RiftPhase.RiftOpened_GoToPast
                    ? "[ORB LOCKED] Go to PAST ERA and align the Ancient Conduit first!"
                    : "[ORB LOCKED] Activate the Cybernetic Terminal first to unlock the Cyan Orb!",
                _ => "[DEVICE CORE LOCKED] Sync both PAST and FUTURE eras first before repairing the Present Device!"
            };
            PuzzleStatusUpdated?.Invoke(msg);
        }

        public void NotifyPhaseInstruction()
        {
            string msg = currentPhase switch
            {
                RiftPhase.Dormant => "[INSTRUCTION] Touch the Central Device in PRESENT ERA to open the Rift!",
                RiftPhase.RiftOpened_GoToPast => "[INSTRUCTION] Travel to PAST ERA and pull the Ancient Altar Lever!",
                RiftPhase.PastSynced_GoToFuture => "[INSTRUCTION] Travel to FUTURE ERA and activate the Cybernetic Terminal!",
                RiftPhase.FutureSynced_ReturnToPresent => "[INSTRUCTION] Return to PRESENT ERA and repair the Central Device!",
                _ => "REALITY STABILIZED | Central Device Repaired!"
            };
            PuzzleStatusUpdated?.Invoke(msg);
        }
    }
}
