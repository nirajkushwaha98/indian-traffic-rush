using UnityEngine;

namespace ITR
{
    /// <summary>One pooled AI vehicle. Movement is driven centrally by <see cref="TrafficManager"/>.</summary>
    public class TrafficVehicle : MonoBehaviour
    {
        public TrafficVehicleData Data { get; private set; }
        public VehicleVisual Visual { get; private set; }

        public int lane;
        public int fromLane;
        public bool changingLane;
        public float laneT;
        public float speed;          // m/s current
        public float cruiseSpeed;    // m/s preferred
        public bool passedByPlayer;
        public bool hitByPlayer;
        public bool wrecked;
        public float nextLaneDecision;

        public float Length => Visual.length;
        public float Width => Visual.width;
        public float Z => transform.position.z;
        public float X => transform.position.x;
        public float Front => Z + Length * 0.5f;
        public float Rear => Z - Length * 0.5f;

        public bool Occupies(int l) => lane == l || (changingLane && fromLane == l);

        public void Init(TrafficVehicleData data, VehicleVisual visual)
        {
            Data = data;
            Visual = visual;
            visual.transform.SetParent(transform, false);
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(visual.width * 0.95f, visual.height * 0.9f, visual.length * 0.96f);
            col.center = new Vector3(0, visual.height * 0.5f, 0);
        }

        public void Activate(int laneIndex, float z, float speedMs)
        {
            lane = fromLane = laneIndex;
            changingLane = false;
            laneT = 0f;
            speed = cruiseSpeed = speedMs;
            passedByPlayer = hitByPlayer = wrecked = false;
            nextLaneDecision = Random.Range(2f, 6f);
            transform.SetPositionAndRotation(new Vector3(GameConstants.LaneX(laneIndex), 0f, z), Quaternion.identity);
            Visual.body.localRotation = Quaternion.identity;
            Visual.SetIndicators(false, false);
            gameObject.SetActive(true);
        }

        public void StartLaneChange(int target)
        {
            fromLane = lane;
            lane = target;
            changingLane = true;
            laneT = 0f;
        }

        /// <summary>Moves forward and (if changing lanes) sideways with a smooth S-curve.</summary>
        public void Move(float dt, float time)
        {
            var p = transform.position;
            p.z += speed * dt;
            float yaw = 0f;
            if (changingLane)
            {
                laneT += dt / 1.6f;
                float t = Mathf.Clamp01(laneT);
                float s = t * t * (3f - 2f * t);
                p.x = Mathf.Lerp(GameConstants.LaneX(fromLane), GameConstants.LaneX(lane), s);
                yaw = Mathf.Sin(t * Mathf.PI) * 9f * Mathf.Sign(lane - fromLane);
                bool blink = (time % 0.5f) < 0.25f;
                Visual.SetIndicators(lane < fromLane && blink, lane > fromLane && blink);
                if (t >= 1f)
                {
                    changingLane = false;
                    fromLane = lane;
                    Visual.SetIndicators(false, false);
                }
            }
            transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }
    }
}
