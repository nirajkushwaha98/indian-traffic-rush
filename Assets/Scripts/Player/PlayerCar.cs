using System;
using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Arcade player car: lane-based steering with smooth lateral motion, auto-cruise with gas/brake,
    /// nitro, body roll/pitch, and a physics-driven tumble when crashing.
    /// Collision uses a kinematic Rigidbody + trigger BoxCollider against traffic colliders.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerCar : MonoBehaviour
    {
        public VehicleData Data { get; private set; }
        public VehicleVisual Visual { get; private set; }
        public GarageService.Stats Stats { get; private set; }

        public float Speed { get; private set; }           // m/s
        public float SpeedKmh => Speed * GameConstants.MsToKmh;
        public float TopSpeed => Stats.topSpeedKmh * GameConstants.KmhToMs;
        public int Lane { get; private set; } = 1;
        public int PreviousLane { get; private set; } = 1;
        public float LateralVelocity => lateralVel;
        public bool IsChangingLane => Mathf.Abs(transform.position.x - GameConstants.LaneX(Lane)) > 0.25f;
        public bool Crashed { get; private set; }
        public bool Braking { get; private set; }
        public bool Accelerating { get; private set; }

        public float Nitro { get; private set; }
        public bool NitroActive { get; private set; }
        public float CruiseKmh { get; set; } = 70f;

        public bool Invulnerable => invulnTimer > 0f;
        public float InvulnerableTime => invulnTimer;
        public float Length => Visual != null ? Visual.length : 4f;
        public float Width => Visual != null ? Visual.width : 1.8f;

        public event Action<TrafficVehicle> HitTraffic;
        public event Action NitroStarted;

        GameConfig cfg;
        Rigidbody rb;
        BoxCollider col;
        float lateralVel;
        float invulnTimer;
        float roll, pitch, yaw;
        float lastHitTime = -10f;
        TrafficVehicle lastHit;

        public void Setup(VehicleData data, Color paint, GameConfig config)
        {
            cfg = config;
            Data = data;
            Stats = GarageService.GetStats(data);

            var model = VehicleBuilder.Build(data.shape, paint, true);
            model.transform.SetParent(transform, false);
            Visual = model.GetComponent<VehicleVisual>();

            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.mass = 1200f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(Visual.width * 0.92f, Visual.height * 0.9f, Visual.length * 0.95f);
            col.center = new Vector3(0, Visual.height * 0.5f, 0);

            Nitro = cfg.nitroStart;
            Speed = CruiseKmh * GameConstants.KmhToMs * 0.5f;
            transform.position = new Vector3(GameConstants.LaneX(Lane), 0f, transform.position.z);
        }

        public void SetStartSpeed(float kmh) => Speed = kmh * GameConstants.KmhToMs;

        /// <summary>Temporary immunity to traffic hits (revive grace period; also used by automated tests).</summary>
        public void SetInvulnerable(float seconds) => invulnTimer = seconds;

        public void RequestLane(int dir)
        {
            if (Crashed || dir == 0) return;
            int target = GameConstants.ClampLane(Lane + dir);
            if (target == Lane) return;
            PreviousLane = Lane;
            Lane = target;
            AudioManager.I?.Play(Sfx.Whoosh, 0.25f, UnityEngine.Random.Range(0.9f, 1.1f));
        }

        public bool TryNitro()
        {
            if (Crashed || NitroActive || Nitro < cfg.nitroMinToActivate) return false;
            NitroActive = true;
            NitroStarted?.Invoke();
            return true;
        }

        public void AddNitro(float amount) => Nitro = Mathf.Clamp01(Nitro + amount);

        /// <summary>Advance one frame. Returns metres travelled forward.</summary>
        public float Tick(float dt, bool gas, bool brake)
        {
            if (Crashed) return 0f;
            if (invulnTimer > 0f) invulnTimer -= dt;

            Accelerating = gas && !brake;
            Braking = brake;

            float top = TopSpeed;
            float cruise = Mathf.Min(CruiseKmh * GameConstants.KmhToMs, top);
            float target = Accelerating ? top : (Braking ? cfg.minSpeedKmh * GameConstants.KmhToMs : cruise);
            float accel = Stats.acceleration;

            if (NitroActive)
            {
                target = top * cfg.nitroSpeedMultiplier;
                accel *= 2.6f;
                Nitro -= cfg.nitroDrainPerSecond * dt;
                if (Nitro <= 0f) { Nitro = 0f; NitroActive = false; }
            }
            else
            {
                Nitro = Mathf.Clamp01(Nitro + cfg.nitroPassivePerSecond * dt);
            }

            if (Speed < target)
            {
                float headroom = Mathf.Clamp(1f - Speed / (top * 1.6f), 0.25f, 1f);
                Speed = Mathf.Min(target, Speed + accel * headroom * dt);
            }
            else if (Speed > target)
            {
                float decel = Braking ? Stats.braking : (NitroActive ? 0f : cfg.coastDecel + (Speed - target) * 0.4f);
                Speed = Mathf.Max(target, Speed - decel * dt);
            }

            // Lateral motion
            var p = transform.position;
            float targetX = GameConstants.LaneX(Lane);
            float smooth = Mathf.Clamp(2.2f / Mathf.Max(1f, Stats.handling), 0.08f, 0.3f);
            float newX = Mathf.SmoothDamp(p.x, targetX, ref lateralVel, smooth, Stats.handling * 1.4f, dt);
            float dz = Speed * dt;
            transform.position = new Vector3(newX, 0f, p.z + dz);

            // Body motion (visual only)
            float targetRoll = Mathf.Clamp(-lateralVel * 1.1f, -7f, 7f);
            float targetYaw = Mathf.Clamp(lateralVel * 1.6f, -12f, 12f);
            float targetPitch = Braking ? 2.2f : (Accelerating || NitroActive ? -1.6f : 0f);
            roll = Util.Damp(roll, targetRoll, 10f, dt);
            yaw = Util.Damp(yaw, targetYaw, 12f, dt);
            pitch = Util.Damp(pitch, targetPitch, 6f, dt);
            if (Visual != null)
            {
                Visual.body.localRotation = Quaternion.Euler(pitch, 0f, roll);
                Visual.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                Visual.SpinWheels(Speed, dt);
                Visual.SetBrake(Braking);
            }
            return dz;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Crashed || Invulnerable) return;
            var tv = other.GetComponentInParent<TrafficVehicle>();
            if (tv == null || !tv.gameObject.activeInHierarchy) return;
            if (tv == lastHit && Time.time - lastHitTime < 0.8f) return;
            lastHit = tv;
            lastHitTime = Time.time;
            HitTraffic?.Invoke(tv);
        }

        /// <summary>
        /// True if the contact is a glancing side-swipe (player steered into a vehicle beside it) rather
        /// than a head-on/rear-end hit.
        /// </summary>
        public bool IsSideSwipe(TrafficVehicle tv)
        {
            float overlapX = (Width + tv.Width) * 0.5f - Mathf.Abs(transform.position.x - tv.X);
            float overlapZ = (Length + tv.Length) * 0.5f - Mathf.Abs(transform.position.z - tv.Z);
            return Mathf.Abs(lateralVel) > 1.5f && overlapX < overlapZ * 0.6f && overlapX < 0.9f;
        }

        /// <summary>Bounce back into the previous lane after a side-swipe.</summary>
        public void BounceBack(float speedFactor)
        {
            int back = PreviousLane;
            PreviousLane = Lane;
            Lane = back;
            lateralVel = -lateralVel * 0.5f;
            Speed *= speedFactor;
            invulnTimer = 0.5f;
        }

        public void Crash(Vector3 otherVelocity)
        {
            if (Crashed) return;
            Crashed = true;
            NitroActive = false;
            col.isTrigger = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Physics.SyncTransforms();
            var v = new Vector3(lateralVel * 0.5f, 0f, Speed * 0.55f) + otherVelocity * 0.3f;
            rb.linearVelocity = v;
            rb.AddForce(Vector3.up * 5.5f + new Vector3(UnityEngine.Random.Range(-2f, 2f), 0, 0), ForceMode.VelocityChange);
            rb.AddTorque(new Vector3(UnityEngine.Random.Range(-4f, -1.5f), UnityEngine.Random.Range(-6f, 6f), UnityEngine.Random.Range(-5f, 5f)), ForceMode.VelocityChange);
            Speed = 0f;
            if (Visual != null) Visual.SetBrake(true);
        }

        /// <summary>Reset after an ad-revive: upright, centre lane, short invulnerability.</summary>
        public void Revive(float invulnerability)
        {
            Crashed = false;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
            col.isTrigger = true;
            var p = transform.position;
            Lane = PreviousLane = 1;
            lateralVel = 0f;
            transform.SetPositionAndRotation(new Vector3(GameConstants.LaneX(1), 0f, p.z), Quaternion.identity);
            Physics.SyncTransforms();
            Speed = CruiseKmh * GameConstants.KmhToMs * 0.6f;
            invulnTimer = invulnerability;
            Nitro = Mathf.Max(Nitro, 0.5f);
        }
    }
}
