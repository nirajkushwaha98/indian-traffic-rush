using System;
using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    public enum GameState { Countdown, Playing, Paused, Crashing, GameOver }

    /// <summary>
    /// Owns the race: state machine, explicit per-frame update order (input → player → traffic → coins →
    /// road), scoring, difficulty, crashes, revive, pause and committing results to the save.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public GameState State { get; private set; } = GameState.Countdown;
        public RunStats Stats { get; } = new RunStats();
        public GameConfig Cfg { get; private set; }
        public PlayerCar Player { get; private set; }
        public PlayerEffects Effects { get; private set; }
        public PlayerInputHandler Input { get; private set; }
        public TrafficManager Traffic { get; private set; }
        public CoinManager Coins { get; private set; }
        public RoadManager Road { get; private set; }
        public CameraRig Rig { get; private set; }
        public float Difficulty { get; private set; }
        public float ComboTimeLeft => comboTimer;

        // Results (valid in GameOver)
        public RunReward Reward { get; private set; }
        public int CoinsEarnedTotal { get; private set; }
        public bool NewBest { get; private set; }
        public bool Revived { get; private set; }
        public bool CoinsDoubled { get; private set; }
        public List<MissionProgress> CompletedMissions { get; } = new List<MissionProgress>();

        public event Action<GameState> StateChanged;
        public event Action<int> CountdownTick;            // 3,2,1,0(GO)
        public event Action<int, int> NearMiss;            // combo, points
        public event Action Overtake;
        public event Action CoinCollected;
        public event Action SideHit;

        float countdown;
        int lastCount;
        float comboTimer;
        float crashTimer;
        Transform ground;
        bool committed;
        readonly RunStats committedPart = new RunStats();

        public bool AutoStart = true;

        public void Init(GameConfig cfg, PlayerCar player, PlayerEffects fx, PlayerInputHandler input,
            TrafficManager traffic, CoinManager coins, RoadManager road, CameraRig rig)
        {
            I = this;
            Cfg = cfg; Player = player; Effects = fx; Input = input;
            Traffic = traffic; Coins = coins; Road = road; Rig = rig;

            Player.HitTraffic += OnHitTraffic;
            Traffic.Overtaken += OnOvertaken;
            Coins.Collected += OnCoin;

            var g = new GameObject("GroundCollider");
            var box = g.AddComponent<BoxCollider>();
            box.size = new Vector3(80f, 1f, 260f);
            g.transform.position = new Vector3(0, -0.5f, 0);
            ground = g.transform;
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            Time.timeScale = 1f;
        }

        public void Begin()
        {
            countdown = 3.2f;
            lastCount = 4;
            Player.CruiseKmh = Cfg.startCruiseKmh;
            Player.SetStartSpeed(0f);
            SetState(GameState.Countdown);
        }

        void SetState(GameState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            switch (State)
            {
                case GameState.Countdown: UpdateCountdown(dt); break;
                case GameState.Playing: UpdatePlaying(dt); break;
                case GameState.Crashing: UpdateCrashing(dt); break;
            }
        }

        void UpdateCountdown(float dt)
        {
            countdown -= dt;
            int c = Mathf.CeilToInt(countdown - 0.2f);
            if (c != lastCount && c >= 0)
            {
                lastCount = c;
                CountdownTick?.Invoke(c);
                AudioManager.I?.Play(c == 0 ? Sfx.Go : Sfx.Beep);
            }
            Traffic.Tick(dt, Player, 0f);
            Road.Tick(Player.transform.position.z);
            if (countdown <= 0.2f)
            {
                Player.SetStartSpeed(Cfg.startCruiseKmh * 0.55f);
                SetState(GameState.Playing);
            }
        }

        void UpdatePlaying(float dt)
        {
            Input.Poll();
            if (Input.PauseRequest) { Pause(); return; }
            if (Input.LaneRequest != 0) Player.RequestLane(Input.LaneRequest);
            if (Input.NitroRequest && Player.TryNitro()) Stats.nitroUses++;
            if (Input.HornRequest) AudioManager.I?.Play(Sfx.Horn, 0.8f);

            Stats.time += dt;
            Difficulty = Mathf.Clamp01(Stats.time / Mathf.Max(1f, Cfg.difficultyRampSeconds));
            Player.CruiseKmh = Mathf.Lerp(Cfg.startCruiseKmh, Cfg.maxCruiseKmh, Difficulty);

            float dz = Player.Tick(dt, Input.Gas, Input.Brake);
            Stats.distance += dz;
            Stats.topSpeedKmh = Mathf.Max(Stats.topSpeedKmh, Player.SpeedKmh);
            float mult = (1f + Mathf.Max(0f, Player.SpeedKmh - 60f) / 50f) * (Player.NitroActive ? Cfg.nitroScoreMultiplier : 1f);
            Stats.score += dz * mult;

            if (comboTimer > 0f)
            {
                comboTimer -= dt;
                if (comboTimer <= 0f) Stats.combo = 0;
            }

            if (State != GameState.Playing) return; // a hit may have crashed us inside physics callbacks
            Traffic.Tick(dt, Player, Difficulty);
            Coins.Tick(dt, Player, Traffic);
            Road.Tick(Player.transform.position.z);
            FollowGround();

            if (FloatingOrigin.ShouldShift(Player.transform.position.z))
            {
                float shift = Mathf.Floor(Player.transform.position.z / GameConstants.SegmentLength) * GameConstants.SegmentLength;
                FloatingOrigin.Shift(shift);
            }
        }

        void UpdateCrashing(float dt)
        {
            crashTimer -= Time.unscaledDeltaTime;
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, Time.unscaledDeltaTime * 0.6f);
            Traffic.Tick(dt, Player, Difficulty);
            Road.Tick(Player.transform.position.z);
            FollowGround();
            if (crashTimer <= 0f)
            {
                Time.timeScale = 1f;
                EnterGameOver();
            }
        }

        void FollowGround()
        {
            var p = Player.transform.position;
            ground.position = new Vector3(0f, -0.5f, p.z);
        }

        // ------------------------------------------------------------------ events

        void OnHitTraffic(TrafficVehicle tv)
        {
            if (State != GameState.Playing) return;
            var contact = (Player.transform.position + tv.transform.position) * 0.5f + Vector3.up * 0.6f;
            if (Player.IsSideSwipe(tv))
            {
                Player.BounceBack(0.75f);
                tv.hitByPlayer = true;
                Effects.OnSideSwipe(contact);
                Rig.Shake(0.1f, 0.25f); // gentle: a scrape should not blur the view
                Stats.sideHits++;
                Stats.combo = 0; comboTimer = 0f;
                SideHit?.Invoke();
                return;
            }
            Crash(tv, contact);
        }

        void Crash(TrafficVehicle tv, Vector3 contact)
        {
            tv.hitByPlayer = true;
            tv.wrecked = true;
            Player.Crash(new Vector3(0, 0, tv.speed));
            Effects.OnCrash(contact);
            Rig.Shake(0.35f, 0.5f);
            AudioManager.I?.Play(Sfx.Horn, 0.6f, tv.Data.shape == VehicleShape.Truck || tv.Data.shape == VehicleShape.Bus ? 0.7f : 1.1f);
            Time.timeScale = 0.35f;
            crashTimer = 2.4f;
            Stats.combo = 0;
            SetState(GameState.Crashing);
        }

        void OnOvertaken(TrafficVehicle v, bool near, float clearance)
        {
            if (State != GameState.Playing) return;
            Stats.overtakes++;
            Stats.score += Cfg.overtakePoints;
            Player.AddNitro(Cfg.nitroPerOvertake);
            Overtake?.Invoke();
            if (!near) return;

            Stats.nearMisses++;
            Stats.combo = comboTimer > 0f ? Stats.combo + 1 : 1;
            Stats.maxCombo = Mathf.Max(Stats.maxCombo, Stats.combo);
            comboTimer = Cfg.comboWindow;
            int pts = Cfg.nearMissPoints * Stats.combo;
            Stats.score += pts;
            Player.AddNitro(Cfg.nitroPerNearMiss);
            AudioManager.I?.Play(Sfx.NearMiss, 0.8f, 1f + Stats.combo * 0.04f);
            if (UnityEngine.Random.value < 0.5f)
            {
                float pitch = v.Data.shape == VehicleShape.Truck || v.Data.shape == VehicleShape.Bus ? 0.72f :
                              (v.Data.shape == VehicleShape.Motorcycle || v.Data.shape == VehicleShape.AutoRickshaw ? 1.35f : 1f);
                AudioManager.I?.Play(Sfx.Horn, 0.5f, pitch);
            }
            NearMiss?.Invoke(Stats.combo, pts);
        }

        void OnCoin(Vector3 pos)
        {
            Stats.coins++;
            Stats.score += 5;
            AudioManager.I?.Play(Sfx.Coin, 0.6f, UnityEngine.Random.Range(0.97f, 1.06f));
            CoinCollected?.Invoke();
        }

        // ------------------------------------------------------------------ flow

        public void Pause()
        {
            if (State != GameState.Playing && State != GameState.Countdown) return;
            pausedFrom = State;
            Time.timeScale = 0f;
            AudioManager.I?.PauseMusic(true);
            Effects.SetEngineMuted(true);
            Input.ClearHeld();
            SetState(GameState.Paused);
        }

        GameState pausedFrom = GameState.Playing;

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            AudioManager.I?.PauseMusic(false);
            Effects.SetEngineMuted(false);
            SetState(pausedFrom);
        }

        void EnterGameOver()
        {
            CommitRun();
            SetState(GameState.GameOver);
        }

        /// <summary>Writes the run (or the part since the last revive) into the save. Safe to call repeatedly.</summary>
        public void CommitRun()
        {
            if (committed) return;
            committed = true;
            var d = SaveSystem.Data;

            var delta = new RunStats
            {
                distance = Stats.distance - committedPart.distance,
                coins = Stats.coins - committedPart.coins,
                nearMisses = Stats.nearMisses - committedPart.nearMisses,
                overtakes = Stats.overtakes - committedPart.overtakes,
                nitroUses = Stats.nitroUses - committedPart.nitroUses,
                score = Stats.score,
            };
            var totalReward = RewardCalculator.Calculate(Stats, Cfg);
            int alreadyPaid = CoinsEarnedTotal;
            int pay = Mathf.Max(0, totalReward.Total - alreadyPaid);
            Reward = totalReward;
            CoinsEarnedTotal = totalReward.Total;

            d.stats.totalDistance += delta.distance;
            d.stats.totalNearMisses += delta.nearMisses;
            d.stats.totalOvertakes += delta.overtakes;
            d.stats.nitroUses += delta.nitroUses;
            if (!Revived) d.stats.runs++;
            d.stats.bestNearMissCombo = Mathf.Max(d.stats.bestNearMissCombo, Stats.maxCombo);
            d.bestSpeedKmh = Mathf.Max(d.bestSpeedKmh, Stats.topSpeedKmh);
            d.bestDistance = Mathf.Max(d.bestDistance, Stats.distance);
            NewBest = Stats.Score > d.bestScore;
            if (NewBest) d.bestScore = Stats.Score;

            Wallet.Add(pay, false);
            CompletedMissions.AddRange(MissionManager.ReportRun(delta, Stats, !Revived));
            SaveSystem.Save();

            committedPart.distance = Stats.distance;
            committedPart.coins = Stats.coins;
            committedPart.nearMisses = Stats.nearMisses;
            committedPart.overtakes = Stats.overtakes;
            committedPart.nitroUses = Stats.nitroUses;
        }

        public bool CanRevive => State == GameState.GameOver && !Revived && AdsManager.RewardedReady;
        public bool CanDoubleCoins => State == GameState.GameOver && !CoinsDoubled && CoinsEarnedTotal > 0 && AdsManager.RewardedReady;

        public void RequestRevive(Action<bool> done)
        {
            if (!CanRevive) { done?.Invoke(false); return; }
            AdsManager.ShowRewarded(ok =>
            {
                if (ok && State == GameState.GameOver) DoRevive();
                done?.Invoke(ok);
            });
        }

        /// <summary>Continues the run after a verified rewarded ad (also used by tests).</summary>
        public void DoRevive()
        {
            Revived = true;
            committed = false;
            Traffic.ClearAround(Player.transform.position.z, 140f);
            Player.Revive(Cfg.reviveInvulnerability);
            Effects.OnRevive();
            Rig.Snap();
            countdown = 2.2f;
            lastCount = 3;
            SetState(GameState.Countdown);
        }

        public void RequestDoubleCoins(Action<bool> done)
        {
            if (!CanDoubleCoins) { done?.Invoke(false); return; }
            AdsManager.ShowRewarded(ok =>
            {
                if (ok && !CoinsDoubled)
                {
                    CoinsDoubled = true;
                    Wallet.Add(CoinsEarnedTotal);
                }
                done?.Invoke(ok);
            });
        }

        public void Restart()
        {
            bool afterGameOver = State == GameState.GameOver;
            CommitRunIfStarted();
            Time.timeScale = 1f;
            if (afterGameOver) AdsManager.OnGameOver(() => AppRoot.LoadScene(GameConstants.GameScene));
            else AppRoot.LoadScene(GameConstants.GameScene);
        }

        public void Home()
        {
            bool afterGameOver = State == GameState.GameOver;
            CommitRunIfStarted();
            Time.timeScale = 1f;
            if (afterGameOver) AdsManager.OnGameOver(() => AppRoot.LoadScene(GameConstants.MenuScene));
            else AppRoot.LoadScene(GameConstants.MenuScene);
        }

        void CommitRunIfStarted()
        {
            if (Stats.distance > 1f) CommitRun();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && State == GameState.Playing) Pause();
        }
    }
}
