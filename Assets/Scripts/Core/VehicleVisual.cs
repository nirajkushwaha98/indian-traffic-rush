using UnityEngine;

namespace ITR
{
    /// <summary>References into a procedurally built vehicle (see <see cref="VehicleBuilder"/>).</summary>
    public class VehicleVisual : MonoBehaviour
    {
        public VehicleShape shape;
        public float length;
        public float width;
        public float height;
        public float wheelRadius = 0.3f;

        /// <summary>Pivot that can be rolled/pitched for body motion. Wheels are not under it.</summary>
        public Transform body;
        public Transform[] wheels = new Transform[0];
        public Renderer brakeLights;
        public Renderer headlightBeams;
        public Renderer sirenRed;
        public Renderer sirenBlue;
        public Renderer indicatorLeft;
        public Renderer indicatorRight;
        public Transform exhaust;

        float wheelAngle;

        public void SpinWheels(float speedMs, float dt)
        {
            if (wheels == null || wheels.Length == 0) return;
            wheelAngle += speedMs / Mathf.Max(0.1f, wheelRadius) * Mathf.Rad2Deg * dt;
            wheelAngle %= 360f;
            var rot = Quaternion.Euler(wheelAngle, 0f, 0f);
            for (int i = 0; i < wheels.Length; i++)
                if (wheels[i] != null) wheels[i].localRotation = rot;
        }

        public void SetBrake(bool on)
        {
            if (brakeLights != null && brakeLights.enabled != on) brakeLights.enabled = on;
        }

        public void SetHeadlights(bool on)
        {
            if (headlightBeams != null) headlightBeams.enabled = on;
        }

        public void SetIndicators(bool left, bool right)
        {
            if (indicatorLeft != null) indicatorLeft.enabled = left;
            if (indicatorRight != null) indicatorRight.enabled = right;
        }
    }
}
