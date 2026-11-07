using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ITR
{
    /// <summary>
    /// Reads keyboard, gamepad and touch (Unity Input System). Swipes change lanes / fire nitro; on-screen
    /// buttons feed the same flags. Touches that start on HUD buttons never count as swipes.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        public int LaneRequest { get; private set; }     // -1 / 0 / +1, consumed each frame
        public bool NitroRequest { get; private set; }
        public bool PauseRequest { get; private set; }
        public bool HornRequest { get; private set; }
        public bool Gas => gasHeld || keyGas;
        public bool Brake => brakeHeld || keyBrake;

        // Written by HUD buttons
        [System.NonSerialized] public bool gasHeld, brakeHeld;
        bool keyGas, keyBrake;
        int uiLane; bool uiNitro, uiHorn;

        /// <summary>Minimum swipe length as a fraction of the screen's short side (portrait: width).</summary>
        public const float SwipeMinPixelsFraction = 0.07f;
        readonly List<RectTransform> blockers = new List<RectTransform>();

        struct TouchTrack
        {
            public Vector2 start;
            public float time;
            public bool consumed;
            public bool blocked;
        }
        readonly Dictionary<int, TouchTrack> touches = new Dictionary<int, TouchTrack>();

        public void RegisterBlocker(RectTransform rt) { if (rt != null) blockers.Add(rt); }
        public void UiSteer(int dir) => uiLane = dir;
        public void UiNitro() => uiNitro = true;
        public void UiHorn() => uiHorn = true;

        bool IsBlocked(Vector2 screenPos)
        {
            foreach (var b in blockers)
                if (b != null && b.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(b, screenPos, null))
                    return true;
            return false;
        }

        /// <summary>Called once per frame by the GameManager before the player ticks.</summary>
        public void Poll()
        {
            int lane = uiLane; uiLane = 0;
            bool nitro = uiNitro; uiNitro = false;
            bool horn = uiHorn; uiHorn = false;
            bool pause = false;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) lane = -1;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) lane = 1;
                if (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame) nitro = true;
                if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) pause = true;
                if (kb.hKey.wasPressedThisFrame) horn = true;
                keyGas = kb.upArrowKey.isPressed || kb.wKey.isPressed;
                keyBrake = kb.downArrowKey.isPressed || kb.sKey.isPressed;
            }
            else { keyGas = keyBrake = false; }

            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame) lane = -1;
                if (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame) lane = 1;
                if (pad.buttonSouth.wasPressedThisFrame) nitro = true;
                if (pad.startButton.wasPressedThisFrame) pause = true;
                keyGas |= pad.rightTrigger.isPressed;
                keyBrake |= pad.leftTrigger.isPressed;
            }

            bool swipeMode = SaveSystem.Data.settings.controlMode == (int)ControlMode.Swipe;
            var ts = Touchscreen.current;
            if (ts != null)
            {
                float minDist = Mathf.Max(30f, Mathf.Min(Screen.width, Screen.height) * SwipeMinPixelsFraction);
                foreach (var t in ts.touches)
                {
                    int id = t.touchId.ReadValue();
                    var phase = t.phase.ReadValue();
                    var pos = t.position.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        touches[id] = new TouchTrack { start = pos, time = Time.unscaledTime, blocked = IsBlocked(pos) };
                    }
                    else if ((phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Ended)
                             && touches.TryGetValue(id, out var tr))
                    {
                        if (!tr.consumed && !tr.blocked && swipeMode)
                        {
                            var d = pos - tr.start;
                            if (d.magnitude >= minDist && Time.unscaledTime - tr.time < 0.6f)
                            {
                                if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) lane = d.x > 0 ? 1 : -1;
                                else if (d.y > 0) nitro = true;
                                tr.consumed = true;
                                touches[id] = tr;
                            }
                        }
                        // One-hand play: a quick tap on the left/right half of the road steers that way.
                        if (phase == UnityEngine.InputSystem.TouchPhase.Ended && !tr.consumed && !tr.blocked && lane == 0
                            && (pos - tr.start).magnitude < minDist && Time.unscaledTime - tr.time < 0.35f)
                            lane = pos.x < Screen.width * 0.5f ? -1 : 1;
                        if (phase == UnityEngine.InputSystem.TouchPhase.Ended) touches.Remove(id);
                    }
                    else if (phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        touches.Remove(id);
                    }
                }
            }

            LaneRequest = lane;
            NitroRequest = nitro;
            PauseRequest = pause;
            HornRequest = horn;
        }

        public void ClearHeld()
        {
            gasHeld = brakeHeld = false;
            touches.Clear();
        }
    }
}
