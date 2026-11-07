using System;

namespace ITR
{
    /// <summary>Abstraction over an ad network so gameplay never depends on a specific SDK.</summary>
    public interface IAdsProvider
    {
        void Initialize(AdsConfig config);
        bool IsRewardedReady { get; }
        bool IsInterstitialReady { get; }
        /// <summary>Calls back with true ONLY when the network reports the reward was earned.</summary>
        void ShowRewarded(Action<bool> onFinished);
        void ShowInterstitial(Action onClosed);
    }

    /// <summary>
    /// Used whenever ads are disabled or the SDK is missing. Nothing is ever "ready", so the game hides
    /// ad buttons instead of granting fake rewards.
    /// </summary>
    public class NullAdsProvider : IAdsProvider
    {
        public void Initialize(AdsConfig config) { }
        public bool IsRewardedReady => false;
        public bool IsInterstitialReady => false;
        public void ShowRewarded(Action<bool> onFinished) => onFinished?.Invoke(false);
        public void ShowInterstitial(Action onClosed) => onClosed?.Invoke();
    }
}
