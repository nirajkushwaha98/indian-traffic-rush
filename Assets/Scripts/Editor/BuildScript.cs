using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ITR.EditorTools
{
    /// <summary>
    /// Android builds (menu or command line).
    ///   APK (test):     -executeMethod ITR.EditorTools.BuildScript.BuildApk
    ///   AAB (release):  -executeMethod ITR.EditorTools.BuildScript.BuildAab
    /// Release signing comes from environment variables (never committed):
    ///   ITR_KEYSTORE, ITR_KEYSTORE_PASS, ITR_KEY_ALIAS, ITR_KEY_PASS
    /// Optional: ITR_JDK to point Unity at a JDK 17 if the Hub-installed one is missing.
    /// </summary>
    public static class BuildScript
    {
        static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem("ITR/Build/Android APK (test)")]
        public static void BuildApk() => Build(false);

        [MenuItem("ITR/Build/Android App Bundle (release)")]
        public static void BuildAab() => Build(true);

        static void Build(bool aab)
        {
            ProjectSetup.Run();
            ApplyJdkOverride();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = aab;

            bool signed = ConfigureSigning();
            if (aab && !signed)
            {
                Fail("Release AAB needs a keystore. Set ITR_KEYSTORE, ITR_KEYSTORE_PASS, ITR_KEY_ALIAS, ITR_KEY_PASS (see RELEASE_GUIDE.md).");
                return;
            }

            Directory.CreateDirectory("Builds/Android");
            string path = "Builds/Android/IndianTrafficRush-" + PlayerSettings.bundleVersion + (aab ? ".aab" : ".apk");
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log("[ITR] Build " + s.result + " → " + path + "  (" + (s.totalSize / (1024 * 1024)) + " MB, " + s.totalErrors + " errors, " + s.totalTime + ")");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        static bool ConfigureSigning()
        {
            string ks = Environment.GetEnvironmentVariable("ITR_KEYSTORE");
            if (string.IsNullOrEmpty(ks) || !File.Exists(ks))
            {
                PlayerSettings.Android.useCustomKeystore = false; // debug keystore (test builds only)
                return false;
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = ks;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("ITR_KEYSTORE_PASS");
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("ITR_KEY_ALIAS");
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("ITR_KEY_PASS");
            return true;
        }

        static void ApplyJdkOverride()
        {
            string jdk = Environment.GetEnvironmentVariable("ITR_JDK");
            if (string.IsNullOrEmpty(jdk)) return;
            var t = Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");
            var prop = t?.GetProperty("jdkRootPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(null, jdk);
                Debug.Log("[ITR] Using JDK at " + jdk);
            }
            else
            {
                EditorPrefs.SetString("JdkPath", jdk);
                EditorPrefs.SetBool("JdkUseEmbedded", false);
            }
        }

        static void Fail(string msg)
        {
            Debug.LogError("[ITR] " + msg);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
