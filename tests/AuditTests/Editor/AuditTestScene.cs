using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityTools.SceneOptimization;
using A = AuditTestsEditor.AuditTestAssets;
using Object = UnityEngine.Object;

namespace AuditTestsEditor
{
    // Builds AuditSeed (and AuditSeedB) with known problems and clean controls next to them.
    public static class AuditTestScene
    {
        public const string SeedPath = A.Root + "/Scenes/AuditSeed.unity";
        public const string SeedBPath = A.Root + "/Scenes/AuditSeedB.unity";
        const string FakeGuid = "deadbeefdeadbeefdeadbeefdeadbeef";

        static Dictionary<string, GameObject> made;

        public static void Build(string variant)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            made = new Dictionary<string, GameObject>();
            BuildMain(variant);
#pragma warning disable 0618
            Lightmapping.realtimeGI = variant == "project";
#pragma warning restore 0618
            EditorSceneManager.SaveScene(scene, SeedPath);

            var sceneB = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            made = new Dictionary<string, GameObject>();
            var b = Obj("B/TwoD");
            var source = b.AddComponent<AudioSource>();
            source.clip = A.A5;
            source.spatialBlend = 0f;
            EditorSceneManager.SaveScene(sceneB, SeedBPath);

            // A missing script cannot be made through the API: swap the script guid in the saved file.
            var missingGuid = AssetDatabase.AssetPathToGUID("Assets/AuditTests/Runtime/MissingLater.cs");
            var text = File.ReadAllText(SeedPath);
            File.WriteAllText(SeedPath, text.Replace("guid: " + missingGuid, "guid: " + FakeGuid));
            AssetDatabase.ImportAsset(SeedPath, ImportAssetOptions.ForceSynchronousImport);

            EditorSceneManager.OpenScene(SeedPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(SeedBPath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByPath(SeedPath));

            // One viewpoint in the middle looking every way, so that every run estimates the frame budget (2.8.0).
            var set = ViewpointSet.Load(ViewpointSet.LoadedScenePaths());
            set.points.Clear();
            // A zone left by the zone shots (3.0.0) is not the seed's.
            set.zone.Clear();
            set.AddManual(new Vector3(0f, 1.7f, 0f), 0f).hasYaw = false;
            set.Save();
            Object.DestroyImmediate(set);
        }

        static void BuildMain(string variant)
        {
            // Camera and listeners.
            var main = Obj("Main Camera");
            var camera = main.AddComponent<Camera>();
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.stereoTargetEye = StereoTargetEyeMask.Both;
            var mainListener = main.AddComponent<AudioListener>();
            main.AddComponent<PostProcessLayer>();
            var looseListener = Obj("Audio/ListenerNoCam").AddComponent<AudioListener>();
            if (variant == "no-listener")
            {
                mainListener.enabled = false;
                looseListener.enabled = false;
            }

            Cam("Cam/Far", c => c.farClipPlane = 2000f);
            Cam("Cam/Near", c => c.nearClipPlane = 0.001f);
            Cam("Cam/Deferred", c => c.renderingPath = RenderingPath.DeferredShading);
            Cam("Cam/NoMsaa", c => c.allowMSAA = false);
            Cam("Cam/ToTexture", c =>
            {
                c.targetTexture = A.TargetTexture;
                c.allowHDR = true;
                c.farClipPlane = 5000f;
                c.nearClipPlane = 0.001f;
                c.allowMSAA = false;
            });
            Cam("Cam/Effect", c => c.gameObject.AddComponent<ImageEffectStub>());

            // Draw calls.
            Cube("DC/Five", A.Plain).GetComponent<MeshRenderer>().sharedMaterials = new[] { A.Plain, A.Plain, A.Plain, A.Plain, A.Plain };
            var two = Obj("DC/Two");
            two.AddComponent<MeshFilter>().sharedMesh = A.TwoSub;
            two.AddComponent<MeshRenderer>().sharedMaterials = new[] { A.Plain, A.Plain };
            Cube("DC/Candidate", A.Plain);
            var staticCube = Cube("DC/Static", A.Plain);
            GameObjectUtility.SetStaticEditorFlags(staticCube, StaticEditorFlags.BatchingStatic);
            Cube("DC/Moved", A.Plain).AddComponent<Rigidbody>().isKinematic = true;
            Obj("DC/UnderScript").AddComponent<Mover>();
            Cube("DC/UnderScript/Child", A.Plain);
            foreach (var n in new[] { "A", "B", "C", "D" }) Cube("Inst/" + n, A.NoInst);
            foreach (var n in new[] { "A", "B", "C" }) Cube("Inst3/" + n, A.NoInst3);
            foreach (var n in new[] { "A", "B", "C", "D" }) Cube("InstBuiltin/" + n, null);

            // Meshes.
            MeshObj("Mesh/12k", A.Grid12k, A.Plain);
            MeshObj("Mesh/30k", A.Grid30k, A.Plain);
            var lod = Obj("Mesh/Lod");
            var lod0 = MeshObj("Mesh/Lod/LOD0", A.Grid30k, A.Plain);
            var lod1 = MeshObj("Mesh/Lod/LOD1", A.Grid12k, A.Plain);
            lod.AddComponent<LODGroup>().SetLODs(new[]
            {
                new LOD(0.5f, new Renderer[] { lod0.GetComponent<Renderer>() }),
                new LOD(0.1f, new Renderer[] { lod1.GetComponent<Renderer>() })
            });
            MeshObj("Mesh/ReadWrite", A.ObjReadWrite, A.Plain);
            MeshObj("Mesh/ReadWriteCollider", A.ObjReadWriteCollider, A.Plain).AddComponent<MeshCollider>().sharedMesh = A.ObjReadWriteCollider;
            MeshObj("Mesh/NotOptimized", A.ObjNotOptimized, A.Plain);
            MeshObj("Mesh/Clean", A.ObjClean, A.Plain);
            Skinned("Mesh/Skin8", A.Skin8, 8, false);

            // 3D assets.
            MeshObj("Asset/Channels", A.Channels, A.Plain);
            MeshObj("Asset/Split", A.Split, A.Plain);
            MeshObj("Asset/Degenerate", A.Degenerate, A.Plain);
            MeshObj("Asset/DupA", A.DupA, A.Plain);
            MeshObj("Asset/DupB", A.DupB, A.Plain);
            MeshObj("Asset/FarPivot", A.FarPivot, A.Plain);
            MeshObj("Asset/Index32", A.ObjIndex32, A.Plain);
            MeshObj("Asset/Big12k", A.ObjBig, A.Plain);
            MeshObj("Asset/Scaled", A.ObjScaled, A.Plain);
            MeshObj("Asset/Animated", A.ObjAnimated, A.Plain);
            MeshObj("Asset/Embedded", A.ObjEmbedded, A.Plain);
            Cube("Asset/Huge", A.Plain).transform.localScale = new Vector3(100f, 100f, 100f);
            MeshObj("Asset/DenseSmall", A.Grid12k, A.Plain).transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Lod("Lod/Weak", A.Grid12k, A.Grid12k, A.Plain, A.Plain, 0.5f, 0f);
            Lod("Lod/Late", A.Grid12k, A.DupA, A.Plain, A.Plain, 0.05f, 0.01f);
            Lod("Lod/Mats", A.Grid12k, A.DupA, A.Plain, A.NoInst3, 0.5f, 0.1f);
            Cube("Tex/DenseSmall", A.Dense).transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Cube("Tex/BigWall", A.Wall).transform.localScale = new Vector3(20f, 20f, 0.2f);
            Cube("Tex/Npot", A.Npot);
            Cube("Tex/DupA", A.TexDupA);
            Cube("Tex/DupB", A.TexDupB);
            Cube("Tex/Metal", A.Metal);
            Cube("Tex/AlphaFull", A.AlphaFull);
            Cube("Mat/DupA", A.MatDupA);
            Cube("Mat/DupB", A.MatDupB);
            Cube("Mat/FadeBig", A.Fade).transform.localScale = new Vector3(5f, 5f, 0.1f);
            Cube("Mat/FadeSmall", A.FadeSmall);
            Cube("Mat/KeyNormal", A.KeyNormal);

            // Textures.
            Cube("Tex/Big4096", A.Big4096);
            Cube("Tex/Big8192", A.Big8192);
            Cube("Tex/Small1024", A.Small1024);
            Cube("Tex/FakeNormal", A.FakeNormal);
            Cube("Tex/Normal", A.Normal);
            for (int i = 0; i < 50; i++) Cube("Tex/NoMipsMany/" + i, A.NoMips);
            Cube("Tex/Raw", A.Raw);
            Cube("Tex/Rgba32", A.Rgba32);
            Cube("Tex/Readable", A.Readable);
            RenderSettings.skybox = A.Sky;

            // UI.
            var overlay = Canvas("UI/Overlay", RenderMode.ScreenSpaceOverlay, null);
            var raw = Obj("UI/Overlay/Raw");
            raw.transform.SetParent(overlay.transform, false);
            raw.AddComponent<RectTransform>();
            raw.AddComponent<RawImage>().texture = A.TOverlayOnly;
            Canvas("UI/ScreenCamera", RenderMode.ScreenSpaceCamera, camera);
            Canvas("UI/World", RenderMode.WorldSpace, camera);

            // Lighting.
            Light("Light/Directional", LightType.Directional, LightmapBakeType.Realtime, LightShadows.Soft);
            Light("Light/PointRealtime", LightType.Point, LightmapBakeType.Realtime, LightShadows.Hard);
            Light("Light/SpotMixed", LightType.Spot, LightmapBakeType.Mixed, LightShadows.Soft);
            var gi = MeshObj("GI/NoUv1", A.Grid12k, A.Plain);
            GameObjectUtility.SetStaticEditorFlags(gi, StaticEditorFlags.ContributeGI);
            gi.GetComponent<MeshRenderer>().receiveGI = ReceiveGI.Lightmaps;
            var giProbes = MeshObj("GI/Probes", A.Grid12k, A.Plain);
            GameObjectUtility.SetStaticEditorFlags(giProbes, StaticEditorFlags.ContributeGI);
            giProbes.GetComponent<MeshRenderer>().receiveGI = ReceiveGI.LightProbes;
            Lightmapped("GI/Uv1Good", A.Uv1Good);
            Lightmapped("GI/Uv1Outside", A.Uv1Outside);
            Lightmapped("GI/Uv1Overlap", A.Uv1Overlap);
            Lightmapped("GI/ModelNoUv", A.ObjLmNoUv);
            Lightmapped("GI/ModelNoUv2", A.ObjLmNoUv);
            Lightmapped("GI/ModelGen", A.ObjLmGen);

            // Physics.
            Concave("Phys/ConcaveDynamic").AddComponent<Rigidbody>().isKinematic = false;
            Concave("Phys/ConcaveKinematic").AddComponent<Rigidbody>().isKinematic = true;
            Obj("Phys/Body").AddComponent<Rigidbody>().isKinematic = false;
            Concave("Phys/Body/Child");
            MeshObj("Phys/RenderMesh12k", A.Grid12k, A.Plain).AddComponent<MeshCollider>().sharedMesh = A.Grid12k;

            // Animators.
            Animator("Anim/Default", AnimatorCullingMode.AlwaysAnimate, false);
            Animator("Anim/RootMotion", AnimatorCullingMode.CullUpdateTransforms, true);
            Animator("Anim/RootMotionAgent", AnimatorCullingMode.CullUpdateTransforms, true).gameObject.AddComponent<NavMeshAgent>();
            Animator("Anim/RootMotionScript", AnimatorCullingMode.CullUpdateTransforms, true).gameObject.AddComponent<RootMotionHandler>();
            Skinned("Anim/Offscreen", A.Skin8, 8, true);
            Animator("Anim/Heavy", AnimatorCullingMode.CullUpdateTransforms, false);
            Skinned("Anim/Heavy/Body", A.Grid16kSkin, 1, false);

            var prefabSource = new GameObject("PrefabAnim");
            var prefabAnimator = prefabSource.AddComponent<UnityEngine.Animator>();
            prefabAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            prefabAnimator.applyRootMotion = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(prefabSource, A.Root + "/Prefabs/PrefabAnim.prefab");
            Object.DestroyImmediate(prefabSource);
            PrefabUtility.InstantiatePrefab(prefab);

            // Audio.
            Source("Audio/Long35", A.A35, 1f, 128);
            Source("Audio/Pcm15", A.A15Pcm, 1f, 128);
            Source("Audio/Short5", A.A5, 1f, 128);
            Source("Audio/NoClip", null, 1f, 128);
            Source("Audio/TwoD", A.A5, 0f, 128);
            for (int i = 0; i < 6; i++) Source("Audio/Prio/" + i, A.A5, 1f, 0);

            // Post processing stubs.
            Obj("PP/Volume").AddComponent<PostProcessVolume>().sharedProfile = A.ProfileOn;
            Obj("PP/VolumeOff").AddComponent<PostProcessVolume>().sharedProfile = A.ProfileOff;

            // Reflection probes.
            Probe("Probe/Realtime", ReflectionProbeMode.Realtime, ReflectionProbeRefreshMode.ViaScripting);
            Probe("Probe/EveryFrame", ReflectionProbeMode.Realtime, ReflectionProbeRefreshMode.EveryFrame);
            Probe("Probe/Baked", ReflectionProbeMode.Baked, ReflectionProbeRefreshMode.OnAwake);

            // General.
            Obj("Gen/Empty");
            Obj("Comma, \"Quote\"");
            Cube("Gen/Mirrored", A.Plain).transform.localScale = new Vector3(-1f, 1f, 1f);
            var deep = "Deep";
            for (int i = 2; i <= 12; i++) deep += "/D" + i;
            Obj(deep);
            Obj("Gen/Missing").AddComponent<MissingLater>();
            var inactiveMesh = MeshObj("Gen/InactiveHeavy", A.Grid12k, A.Plain);
            inactiveMesh.SetActive(false);
            var inactiveTex = Cube("Gen/InactiveTex", A.Big4096);
            inactiveTex.SetActive(false);

            if (variant == "counts")
            {
                for (int i = 0; i < 16; i++) Animator("Count/Anim/" + i, AnimatorCullingMode.CullUpdateTransforms, false);
                for (int i = 0; i < 21; i++) Source("Count/Audio/" + i, A.A5, 1f, 128);
                for (int i = 0; i < 101; i++)
                    GameObjectUtility.SetStaticEditorFlags(Cube("Count/Static/" + i, A.Plain), StaticEditorFlags.OccludeeStatic);
            }
            if (variant == "steamvr-in-scene")
            {
                var type = Type.GetType("Valve.VR.SteamVR_PlayArea, AuditTests.SteamVRStub");
                if (type != null) Obj("SteamVR/PlayArea").AddComponent(type);
                else Debug.LogError("[AuditTests] SteamVR stub type is not compiled");
            }
        }

        // ---- helpers ----

        static GameObject Obj(string path)
        {
            GameObject existing;
            if (made.TryGetValue(path, out existing)) return existing;
            int slash = path.LastIndexOf('/');
            var go = new GameObject(slash >= 0 ? path.Substring(slash + 1) : path);
            if (slash >= 0) go.transform.SetParent(Obj(path.Substring(0, slash)).transform, false);
            made[path] = go;
            return go;
        }

        static GameObject Place(string path, GameObject go)
        {
            int slash = path.LastIndexOf('/');
            go.name = slash >= 0 ? path.Substring(slash + 1) : path;
            if (slash >= 0) go.transform.SetParent(Obj(path.Substring(0, slash)).transform, false);
            made[path] = go;
            return go;
        }

        static GameObject Cube(string path, Material material)
        {
            var go = Place(path, GameObject.CreatePrimitive(PrimitiveType.Cube));
            if (material != null) go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static GameObject MeshObj(string path, Mesh mesh, Material material)
        {
            var go = Obj(path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static void Lightmapped(string path, Mesh mesh)
        {
            var go = MeshObj(path, mesh, A.Plain);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            go.GetComponent<MeshRenderer>().receiveGI = ReceiveGI.Lightmaps;
        }

        static void Lod(string path, Mesh lod0, Mesh lod1, Material m0, Material m1, float t0, float t1)
        {
            var group = Obj(path).AddComponent<LODGroup>();
            var r0 = MeshObj(path + "/LOD0", lod0, m0).GetComponent<Renderer>();
            var r1 = MeshObj(path + "/LOD1", lod1, m1).GetComponent<Renderer>();
            group.SetLODs(new[] { new LOD(t0, new[] { r0 }), new LOD(t1, new[] { r1 }) });
        }

        static GameObject Concave(string path)
        {
            var go = Obj(path);
            go.AddComponent<MeshCollider>().sharedMesh = A.ObjClean;
            return go;
        }

        static void Cam(string path, Action<Camera> setup)
        {
            var c = Obj(path).AddComponent<Camera>();
            c.allowHDR = false;
            c.allowMSAA = true;
            c.nearClipPlane = 0.05f;
            c.farClipPlane = 500f;
            c.stereoTargetEye = StereoTargetEyeMask.Both;
            setup(c);
        }

        static Canvas Canvas(string path, RenderMode mode, Camera worldCamera)
        {
            var go = Obj(path);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = mode;
            canvas.worldCamera = worldCamera;
            return canvas;
        }

        static void Light(string path, LightType type, LightmapBakeType bake, LightShadows shadows)
        {
            var light = Obj(path).AddComponent<Light>();
            light.type = type;
            light.lightmapBakeType = bake;
            light.shadows = shadows;
        }

        static UnityEngine.Animator Animator(string path, AnimatorCullingMode culling, bool rootMotion)
        {
            var animator = Obj(path).AddComponent<UnityEngine.Animator>();
            animator.cullingMode = culling;
            animator.applyRootMotion = rootMotion;
            return animator;
        }

        static void Skinned(string path, Mesh mesh, int bones, bool offscreen)
        {
            var go = Obj(path);
            var renderer = go.AddComponent<SkinnedMeshRenderer>();
            var transforms = new Transform[bones];
            for (int i = 0; i < bones; i++)
            {
                var bone = new GameObject("Bone" + i);
                bone.transform.SetParent(go.transform, false);
                transforms[i] = bone.transform;
            }
            renderer.sharedMesh = mesh;
            renderer.bones = transforms;
            renderer.rootBone = transforms[0];
            renderer.sharedMaterial = A.Plain;
            renderer.updateWhenOffscreen = offscreen;
        }

        static void Source(string path, AudioClip clip, float spatial, int priority)
        {
            var source = Obj(path).AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = spatial;
            source.priority = priority;
        }

        static void Probe(string path, ReflectionProbeMode mode, ReflectionProbeRefreshMode refresh)
        {
            var probe = Obj(path).AddComponent<ReflectionProbe>();
            probe.mode = mode;
            probe.refreshMode = refresh;
        }
    }
}
