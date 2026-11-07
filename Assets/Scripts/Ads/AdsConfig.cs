using UnityEngine;

namespace ITR
{
    /// <summary>
    /// Monetization settings. Ads stay OFF until <see cref="adsEnabled"/> is ticked, real AdMob IDs are entered
    /// and the Google Mobile Ads plugin is installed with the ADMOB_ENABLED scripting define.
    /// </summary>
    [CreateAssetMenu(menuName = "ITR/Ads Config", fileName = "AdsConfig")]
    public class AdsConfig : ScriptableObject
    {
        public bool adsEnabled = false;
        [Tooltip("Use Google's official test ad units while developing.")]
        public bool useTestAdUnits = true;

        [Header("Production IDs (from your AdMob console)")]
        public string androidAppId = "";
        public string rewardedAdUnitId = "";
        public string interstitialAdUnitId = "";

        [Header("Interstitial limits")]
        public int interstitialEveryNGameOvers = 3;
        public float interstitialMinSeconds = 150f;

        [Header("Rewards")]
        public int menuRewardCoins = 200;

        // Google's public test ad units (safe to use during development).
        public const string TestRewardedAndroid = "ca-app-pub-3940256099942544/5224354917";
        public const string TestInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";

        public string RewardedId => useTestAdUnits ? TestRewardedAndroid : rewardedAdUnitId;
        public string InterstitialId => useTestAdUnits ? TestInterstitialAndroid : interstitialAdUnitId;

        public bool HasValidIds =>
            useTestAdUnits || (IsValidUnit(rewardedAdUnitId) && IsValidUnit(interstitialAdUnitId));

        static bool IsValidUnit(string id) => !string.IsNullOrEmpty(id) && id.StartsWith("ca-app-pub-") && id.Contains("/");
    }
}
