using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Saranggola.View;

public static class ProjectSetup
{
    public const string BootScene = "Assets/Generated/Boot.unity";
    private const string Resources = "Assets/Resources/Generated";
    private static readonly string[] Names = { "Low", "Med", "High" };

    [MenuItem("Saranggola/Set Up Project")]
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Generated");
        Directory.CreateDirectory(Resources);
        AssetDatabase.Refresh();
        var renderer = FindOrCreate<UniversalRendererData>(Resources + "/Renderer.asset");
        var pipelines = new UniversalRenderPipelineAsset[3];
        for (var i = 0; i < pipelines.Length; i++)
        {
            string pipelinePath = $"{Resources}/{Names[i]}.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            var data = new SerializedObject(pipeline);
            var renderers = data.FindProperty("m_RendererDataList") ?? throw new InvalidOperationException("URP renderer list was unavailable.");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            SetInt(data, "m_DefaultRendererIndex", 0);
            data.ApplyModifiedPropertiesWithoutUndo();
            pipeline.renderScale = i == 0 ? .75f : i == 1 ? .9f : 1f;
            pipeline.shadowDistance = i == 0 ? 0 : 30;
            pipeline.shadowCascadeCount = 2;
            pipeline.supportsMainLightShadows = i > 0;
            pipeline.supportsAdditionalLightShadows = false;
            pipeline.useSRPBatcher = true;
            pipeline.maxAdditionalLightsCount = 4;
            EditorUtility.SetDirty(pipeline);
            pipelines[i] = pipeline;
        }
        GraphicsSettings.defaultRenderPipeline = pipelines[1];
        ConfigureQuality(pipelines);
        SetInputAndShaders();

        PlayerSettings.productName = "Saranggola Royale";
        PlayerSettings.companyName = "Saranggola Royale";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "org.saranggola.royale");
        PlayerSettings.bundleVersion = "0.3.0";
        PlayerSettings.Android.bundleVersionCode = 3;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScene) == null)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Boot").AddComponent<GameBootstrap>();
            if (!EditorSceneManager.SaveScene(scene, BootScene)) throw new InvalidOperationException("Could not save the Boot scene.");
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Saranggola Boot scene and Android settings are ready.");
    }

    [MenuItem("Saranggola/Build Android APK")]
    public static void BuildAndroid()
    {
        Run();
        var args = Environment.GetCommandLineArgs();
        var at = Array.IndexOf(args, "-apk");
        var output = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Build/SaranggolaRoyale-Unity.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        EditorUserBuildSettings.buildAppBundle = false;
        var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { BootScene },
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        if (build.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Android build failed: {build.summary.result}");
    }

    private static T FindOrCreate<T>(string path) where T : ScriptableObject
    {
        var item = AssetDatabase.LoadAssetAtPath<T>(path);
        if (item != null) return item;
        item = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static void ConfigureQuality(UniversalRenderPipelineAsset[] pipelines)
    {
        var data = SerializedSettings("QualitySettings", "ProjectSettings/QualitySettings.asset");
        var levels = data.FindProperty("m_QualitySettings") ?? throw new InvalidOperationException("Unity quality tiers were unavailable.");
        levels.arraySize = 3;
        for (var i = 0; i < levels.arraySize; i++)
        {
            var tier = levels.GetArrayElementAtIndex(i);
            tier.FindPropertyRelative("name").stringValue = Names[i];
            var custom = tier.FindPropertyRelative("customRenderPipeline");
            if (custom != null) custom.objectReferenceValue = pipelines[i];
            var vSync = tier.FindPropertyRelative("vSyncCount");
            if (vSync != null) vSync.intValue = 0;
        }
        SetInt(data, "m_CurrentQuality", 1);
        data.ApplyModifiedPropertiesWithoutUndo();
        QualitySettings.SetQualityLevel(1, true);
    }

    private static void SetInputAndShaders()
    {
        var player = SerializedSettings("PlayerSettings", "ProjectSettings/ProjectSettings.asset");
        var input = player.FindProperty("activeInputHandler") ?? player.FindProperty("m_ActiveInputHandler");
        if (input == null) throw new InvalidOperationException("Unity did not expose the active input handler.");
        input.intValue = 1;
        player.ApplyModifiedPropertiesWithoutUndo();
        var settings = SerializedSettings("GraphicsSettings", "ProjectSettings/GraphicsSettings.asset");
        var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
        foreach (var name in new[] { "Saranggola/Toon", "Saranggola/Outline", "Saranggola/Island Surface", "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit" })
        {
            var shader = Shader.Find(name);
            if (shader == null || shaders == null) continue;
            bool included = false;
            for (var i = 0; i < shaders.arraySize; i++)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) { included = true; break; }
            if (included) continue;
            var element = shaders.arraySize++;
            shaders.GetArrayElementAtIndex(element).objectReferenceValue = shader;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SerializedObject SerializedSettings(string className, string path)
    {
        var settings = AssetDatabase.LoadAllAssetsAtPath(path);
        var root = settings.Length > 0 ? settings[0] : Unsupported.GetSerializedAssetInterfaceSingleton(className);
        if (root == null) throw new InvalidOperationException($"Unity settings are unavailable at {path}.");
        return new SerializedObject(root);
    }

    private static void SetInt(SerializedObject data, string name, int value)
    {
        var field = data.FindProperty(name);
        if (field != null) field.intValue = value;
    }
}
