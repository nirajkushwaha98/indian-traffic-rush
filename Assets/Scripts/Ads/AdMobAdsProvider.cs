// Compiled only after installing the Google Mobile Ads Unity plugin and adding the scripting define ADMOB_ENABLED
// (Project Settings > Player > Other Settings > Scripting Define Symbols). See RELEASE_GUIDE.md.
#if ADMOB_ENABLED
using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace ITR
{
    public class AdMobAdsProvider : IAdsProvider
    {
        AdsConfig config;
        RewardedAd rewarded;
        InterstitialAd interstitial;
        bool initialized;

        public void Initialize(AdsConfig cfg)
        {
            config = cfg;
            MobileAds.Initialize(_ =>
            {
                initialized = true;
                LoadRewarded();
                LoadInterstitial();
            });
        }

        public bool IsRewardedReady => initialized && rewarded != null && rewarded.CanShowAd();
        public bool IsInterstitialReady => initialized && interstitial != null && interstitial.CanShowAd();

        void LoadRewarded()
        {
            rewarded?.Destroy();
            rewarded = null;
            RewardedAd.Load(config.RewardedId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[ITR] Rewarded ad failed to load: " + error);
                    return;
                }
                rewarded = ad;
            });
        }

        void LoadInterstitial()
        {
            interstitial?.Destroy();
            interstitial = null;
            InterstitialAd.Load(config.InterstitialId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[ITR] Interstitial failed to load: " + error);
                    return;
                }
                interstitial = ad;
            });
        }

        public void ShowRewarded(Action<bool> onFinished)
        {
            if (!IsRewardedReady) { onFinished?.Invoke(false); return; }
            bool earned = false;
            var ad = rewarded;
            ad.OnAdFullScreenContentClosed += () =>
            {
                MainThread.Run(() => { onFinished?.Invoke(earned); LoadRewarded(); });
            };
            ad.OnAdFullScreenContentFailed += _ =>
            {
                MainThread.Run(() => { onFinished?.Invoke(false); LoadRewarded(); });
            };
            ad.Show(_ => earned = true);
        }

        public void ShowInterstitial(Action onClosed)
        {
            if (!IsInterstitialReady) { onClosed?.Invoke(); return; }
            var ad = interstitial;
            ad.OnAdFullScreenContentClosed += () => MainThread.Run(() => { onClosed?.Invoke(); LoadInterstitial(); });
            ad.OnAdFullScreenContentFailed += _ => MainThread.Run(() => { onClosed?.Invoke(); LoadInterstitial(); });
            ad.Show();
        }
    }
}
#endif
