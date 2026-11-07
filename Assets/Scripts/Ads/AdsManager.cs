using System;
using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Single entry point for monetization. Disabled by default: with no valid configuration it uses
    /// <see cref="NullAdsProvider"/>, which never reports an ad as ready and never grants rewards.
    /// </summary>
    public static class AdsManager
    {
        static IAdsProvider provider;
        static int gameOversSinceInterstitial;
        static float lastInterstitialTime = -9999f;
        static bool rewardedShownThisGameOver;

        public static bool Active { get; private set; }

        public static void Initialize()
        {
            if (provider != null) return;
            var cfg = GameAssets.I.ads;
#if ADMOB_ENABLED
            if (cfg != null && cfg.adsEnabled && cfg.HasValidIds)
            {
                provider = new AdMobAdsProvider();
                Active = true;
            }
#endif
            if (provider == null)
            {
                provider = new NullAdsProvider();
                Active = false;
            }
            provider.Initialize(cfg);
        }

        public static bool RewardedReady => provider != null && provider.IsRewardedReady;

        /// <summary>Shows a rewarded ad. <paramref name="onResult"/> receives true only for a real, completed reward.</summary>
        public static void ShowRewarded(Action<bool> onResult)
        {
            if (!RewardedReady) { onResult?.Invoke(false); return; }
            rewardedShownThisGameOver = true;
            AudioListener.pause = true;
            provider.ShowRewarded(ok =>
            {
                AudioListener.pause = false;
                onResult?.Invoke(ok);
            });
        }

        /// <summary>Called on every game over; shows an interstitial only within the configured limits.</summary>
        public static void OnGameOver(Action then)
        {
            var cfg = GameAssets.I.ads;
            gameOversSinceInterstitial++;
            bool allowed = provider != null && cfg != null && provider.IsInterstitialReady
                           && !rewardedShownThisGameOver
                           && gameOversSinceInterstitial >= Mathf.Max(1, cfg.interstitialEveryNGameOvers)
                           && Time.realtimeSinceStartup - lastInterstitialTime >= cfg.interstitialMinSeconds;
            rewardedShownThisGameOver = false;
            if (!allowed) { then?.Invoke(); return; }
            gameOversSinceInterstitial = 0;
            lastInterstitialTime = Time.realtimeSinceStartup;
            AudioListener.pause = true;
            provider.ShowInterstitial(() =>
            {
                AudioListener.pause = false;
                then?.Invoke();
            });
        }
    }
}
