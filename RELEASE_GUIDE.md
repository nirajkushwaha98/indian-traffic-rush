# Indian Traffic Rush — Release Guide (Google Play)

## 1. Identity & versioning
| Setting | Current value | Where |
|---|---|---|
| Application ID | `com.indiantrafficrush.game` (**placeholder — change to a domain you own before first upload; it can never change afterwards**) | Project Settings › Player › Android › Other › Package Name |
| Version name | `1.0.0` | Player › Version |
| Version code | `1` (increase by 1 for every upload) | Player › Android › Bundle Version Code |
| Min SDK | API 25 (Android 7.1) | set by `ITR ▸ Setup Project` |
| Target SDK | Automatic (highest installed — API 36 with the Hub SDK) | Play requires API 35+ |
| Scripting backend | IL2CPP, ARM64 + ARMv7 | required for Play 64-bit rule |
| Orientation | **Portrait only (9:16)** — manifest `screenOrientation=portrait`, `resizeableActivity=false`; never rotates | set by `ITR ▸ Setup Project` |

## 2. App icon & splash
`Assets/Textures/AppIcon.png` and `SplashLogo.png` are **generated placeholders** (original road+car art, no text). Replace them with final 1024×1024 art (keep the same file names) and re-run `ITR ▸ Setup Project`, or set them in Player settings. For adaptive icons, add foreground/background layers under Player › Android › Icon.

## 3. Build
From the editor: **ITR ▸ Build ▸ Android APK (test)** or **Android App Bundle (release)**.
From a terminal:
```bash
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "." -buildTarget Android -executeMethod ITR.EditorTools.BuildScript.BuildAab -logFile build.log
```
Output: `Builds/Android/IndianTrafficRush-<version>.aab`.

### Release signing (required for AAB)
Create an upload keystore once (keep it and its passwords safe and out of git):
```bash
keytool -genkeypair -v -keystore itr-upload.keystore -alias itr -keyalg RSA -keysize 2048 -validity 10000
```
Then set these environment variables before building: `ITR_KEYSTORE` (path), `ITR_KEYSTORE_PASS`, `ITR_KEY_ALIAS`, `ITR_KEY_PASS`. Enrol in **Play App Signing** when creating the app.

### JDK note (this machine)
The Hub's embedded OpenJDK module folder is missing from Unity 6000.3.8f1. The build script accepts `ITR_JDK=<JDK 17 home>` (Homebrew `openjdk@17` works). Alternatively re-add the "OpenJDK" sub-module in Unity Hub.

## 4. AdMob (optional — OFF by default)
Ads never run and never grant rewards until all of these are done:
1. Create an AdMob app + a **Rewarded** and an **Interstitial** ad unit.
2. Install the official **Google Mobile Ads Unity plugin** (github.com/googleads/googleads-mobile-unity releases, or OpenUPM `com.google.ads.mobile`). Run *Assets › External Dependency Manager › Android Resolver › Resolve*.
3. In *Assets › Google Mobile Ads › Settings* enter your **Android App ID**.
4. Add scripting define **`ADMOB_ENABLED`** (Player › Other Settings › Scripting Define Symbols). If the plugin is a UPM package with its own assembly definition, add its assembly to `Assets/Scripts/ITR.Runtime.asmdef` references.
5. Open `Assets/ScriptableObjects/AdsConfig.asset`: tick **adsEnabled**; keep **useTestAdUnits** ticked while testing (Google's public test units); for release untick it and paste your real rewarded / interstitial unit IDs.
6. Interstitials: at most one every `interstitialEveryNGameOvers` (3) game-overs and `interstitialMinSeconds` (150 s); never right after a rewarded ad; never during a race.

Rewarded placements: **Revive** (once per race), **2× coins** on the game-over screen, **Free coins** on the main menu. Buttons are hidden whenever no ad is actually loaded.

## 5. Privacy policy & Data safety
Play requires a privacy policy URL for every app; with ads it is mandatory to disclose ad SDK data collection.
- Without ads: the game stores progress only on the device (`persistentDataPath/itr_save.json`), collects no personal data, makes no network calls.
- With AdMob: disclose device identifiers (Advertising ID), approximate location (IP), app interactions and diagnostics collected by Google Mobile Ads; link Google's policy; implement consent (UMP SDK) for EEA/UK users; declare `com.google.android.gms.permission.AD_ID`.
- Set the URL in `Assets/Resources/GameAssets.asset › privacyPolicyUrl` (opened from Settings › Privacy Policy). A starter text is in `PRIVACY_POLICY_TEMPLATE.md`.
- Content rating questionnaire: cartoon vehicle crashes, no blood, no user-generated content → typically PEGI 3/7, Everyone. Target audience: avoid declaring under-13 if you enable personalised ads.

## 6. Play Store listing checklist
- [ ] App name (≤30 chars): *Indian Traffic Rush*
- [ ] Short description (≤80), full description (≤4000)
- [ ] Hi-res icon 512×512 PNG
- [ ] Feature graphic 1024×500
- [ ] ≥2 phone screenshots (portrait 9:16, e.g. 1080×1920) — `ITR_SHOTS` test captures are a starting point; 7" and 10" tablet screenshots recommended
- [ ] Optional promo video (YouTube)
- [ ] Category: Racing; tags; contact email; privacy policy URL
- [ ] Data safety form, content rating, target audience, ads declaration ("contains ads" only once ads are enabled)
- [ ] Internal testing track → closed testing (new personal accounts need 12+ testers for 14 days) → production
- [ ] Test on a low-end device (Settings › Graphics › LOW lowers render scale, disables shadows & MSAA)

## 7. Third-party content
All models, textures, sounds and music are generated by the project's own code (VehicleBuilder, SceneryFactory, AudioSynth, ProjectSetup). Shop names, city names and slogans are generic. No third-party logos, characters, music or paid assets are included.
