using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    // Where a texture is used in the scanned scenes.
    public sealed class TextureUse
    {
        public int users;
        public bool normalSlot;
        public bool in3D;
        public bool inWorldUI;
        public bool inScreenUI;
        public bool lightmap;
        public bool probe;
        public bool skybox;

        public bool IsBaked { get { return lightmap || probe; } }
        public bool OnlyScreenUI { get { return inScreenUI && !in3D && !inWorldUI && !skybox; } }
    }

    // Scene data collected once per run and shared by all checks.
    public sealed class AuditContext
    {
        public readonly VRBudgets budgets;
        public readonly XrInfo xr;
        public readonly QualitySnapshot quality;
        public readonly List<Scene> scenes = new List<Scene>();
        public readonly List<GameObject> roots = new List<GameObject>();
        // Every scene object, inactive ones included.
        public readonly List<GameObject> objects = new List<GameObject>();
        public readonly List<AuditIssue> issues = new List<AuditIssue>();

        public readonly Dictionary<Texture, TextureUse> textures = new Dictionary<Texture, TextureUse>();
        public readonly Dictionary<AudioClip, int> clips = new Dictionary<AudioClip, int>();
        public readonly Dictionary<Mesh, int> meshes = new Dictionary<Mesh, int>();
        public readonly HashSet<Mesh> colliderMeshes = new HashSet<Mesh>();
        public readonly HashSet<Mesh> particleMeshes = new HashSet<Mesh>();
        // Renderers that belong to any LOD level, and those that appear only in LOD1 and lower.
        public readonly HashSet<Renderer> lodRenderers = new HashSet<Renderer>();
        public readonly HashSet<Renderer> lowerLodRenderers = new HashSet<Renderer>();

        public long sceneTris;
        public long drawCallEstimate;
        public double textureMemoryMB;

        readonly Dictionary<Type, object> componentCache = new Dictionary<Type, object>();
        readonly Dictionary<Material, List<KeyValuePair<Texture, bool>>> materialTextures =
            new Dictionary<Material, List<KeyValuePair<Texture, bool>>>();
        readonly Dictionary<Type, PropertyInfo> mainTextureProperty = new Dictionary<Type, PropertyInfo>();

        static readonly string[] NormalPropertyNames = { "_BumpMap", "_DetailNormalMap", "_NormalMap", "_Normal", "_NormalTex" };

        public AuditContext(VRBudgets budgets)
        {
            this.budgets = budgets;
            xr = XrInfo.Detect();
            quality = QualitySnapshot.Capture();
        }

        public void Collect()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                scenes.Add(scene);
                roots.AddRange(scene.GetRootGameObjects());
            }
            foreach (var root in roots)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    objects.Add(t.gameObject);

            CollectLods();
            CollectRendererTextures();
            CollectSceneTextures();
            CollectUiTextures();
            CollectMeshesAndClips();
            CollectDependencies();
        }

        // All components of a type in the scanned scenes, inactive ones included.
        public List<T> All<T>() where T : Component
        {
            object cached;
            if (componentCache.TryGetValue(typeof(T), out cached)) return (List<T>)cached;
            var list = new List<T>();
            foreach (var root in roots) list.AddRange(root.GetComponentsInChildren<T>(true));
            componentCache[typeof(T)] = list;
            return list;
        }

        public List<Component> All(Type type)
        {
            var list = new List<Component>();
            foreach (var root in roots) list.AddRange(root.GetComponentsInChildren(type, true));
            return list;
        }

        public static bool IsActive(Component c)
        {
            if (c == null || !c.gameObject.activeInHierarchy) return false;
            var behaviour = c as Behaviour;
            if (behaviour != null) return behaviour.enabled;
            var renderer = c as Renderer;
            if (renderer != null) return renderer.enabled;
            var collider = c as Collider;
            if (collider != null) return collider.enabled;
            return true;
        }

        // GetComponentInParent skips inactive objects in 2019.4, so walk the parents by hand.
        public static T FindInParents<T>(Transform t) where T : Component
        {
            for (var current = t; current != null; current = current.parent)
            {
                var component = current.GetComponent<T>();
                if (component != null) return component;
            }
            return null;
        }

        public static Mesh MeshOf(Renderer r)
        {
            var skinned = r as SkinnedMeshRenderer;
            if (skinned != null) return skinned.sharedMesh;
            if (r is MeshRenderer)
            {
                var filter = r.GetComponent<MeshFilter>();
                return filter != null ? filter.sharedMesh : null;
            }
            return null;
        }

        public static long Tris(Mesh mesh)
        {
            if (mesh == null) return 0;
            long tris = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var topology = mesh.GetTopology(i);
                long indices = mesh.GetIndexCount(i);
                if (topology == MeshTopology.Triangles) tris += indices / 3;
                else if (topology == MeshTopology.Quads) tris += indices / 4 * 2;
            }
            return tris;
        }

        public static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var current = t; current != null; current = current.parent) parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        public static int DepthOf(Transform t)
        {
            int depth = 0;
            for (var current = t; current != null; current = current.parent) depth++;
            return depth;
        }

        // Assets under Assets/ can be edited; package, built-in and model sub-assets cannot.
        public static bool IsEditableAsset(Object asset)
        {
            if (asset == null) return false;
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)) return false;
            if (asset is Material && !AssetDatabase.IsMainAsset(asset)) return false;
            return true;
        }

        public static bool IsProjectAssetPath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.Ordinal);
        }

        // Number of shadow map passes per shadow caster: cascades for directional, 6 faces for point.
        public int ShadowPassesPerCaster()
        {
            if (quality.shadows == ShadowQuality.Disable) return 0;
            int passes = 0;
            foreach (var light in All<Light>())
            {
                if (!IsActive(light) || light.shadows == LightShadows.None || light.lightmapBakeType == LightmapBakeType.Baked) continue;
                switch (light.type)
                {
                    case LightType.Directional: passes += Mathf.Max(1, quality.shadowCascades); break;
                    case LightType.Point: passes += 6; break;
                    case LightType.Spot: passes += 1; break;
                }
            }
            return passes;
        }

        public AuditIssue Report(string checkId, Category category, Severity severity, Object target, params object[] args)
        {
            var issue = new AuditIssue
            {
                checkId = checkId,
                messageKey = checkId,
                category = category,
                severity = severity,
                target = target,
                args = args ?? new object[0]
            };
            var go = target as GameObject;
            var component = target as Component;
            if (component != null) go = component.gameObject;
            if (go != null)
            {
                issue.scene = go.scene.name;
                issue.objectPath = PathOf(go.transform);
            }
            else if (target != null)
            {
                issue.assetPath = AssetDatabase.GetAssetPath(target);
            }
            issues.Add(issue);
            return issue;
        }

        public AuditIssue ReportSetting(string checkId, Category category, Severity severity, string settingsPath, params object[] args)
        {
            var issue = Report(checkId, category, severity, null, args);
            issue.settingsPath = settingsPath;
            return issue;
        }

        void CollectLods()
        {
            foreach (var group in All<LODGroup>())
            {
                var lods = group.GetLODs();
                var inFirst = new HashSet<Renderer>();
                for (int i = 0; i < lods.Length; i++)
                {
                    foreach (var r in lods[i].renderers)
                    {
                        if (r == null) continue;
                        lodRenderers.Add(r);
                        if (i == 0) inFirst.Add(r);
                    }
                }
                for (int i = 1; i < lods.Length; i++)
                    foreach (var r in lods[i].renderers)
                        if (r != null && !inFirst.Contains(r)) lowerLodRenderers.Add(r);
            }
        }

        TextureUse UseOf(Texture texture)
        {
            TextureUse use;
            if (!textures.TryGetValue(texture, out use))
            {
                use = new TextureUse();
                textures[texture] = use;
            }
            return use;
        }

        void CollectRendererTextures()
        {
            foreach (var renderer in All<Renderer>())
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    foreach (var pair in TexturesOf(material))
                    {
                        var use = UseOf(pair.Key);
                        use.users++;
                        use.in3D = true;
                        if (pair.Value) use.normalSlot = true;
                    }
                }
            }
        }

        // Textures of a material with a flag "assigned to a normal map property".
        public List<KeyValuePair<Texture, bool>> TexturesOf(Material material)
        {
            List<KeyValuePair<Texture, bool>> list;
            if (materialTextures.TryGetValue(material, out list)) return list;
            list = new List<KeyValuePair<Texture, bool>>();
            var shader = material.shader;
            if (shader != null)
            {
                int count = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < count; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                    var name = ShaderUtil.GetPropertyName(shader, i);
                    var texture = material.GetTexture(name);
                    if (texture != null) list.Add(new KeyValuePair<Texture, bool>(texture, IsNormalProperty(shader, name)));
                }
            }
            materialTextures[material] = list;
            return list;
        }

        static bool IsNormalProperty(Shader shader, string name)
        {
            if (Array.IndexOf(NormalPropertyNames, name) >= 0) return true;
            int index = shader.FindPropertyIndex(name);
            return index >= 0 && (shader.GetPropertyFlags(index) & ShaderPropertyFlags.Normal) != 0;
        }

        void CollectSceneTextures()
        {
            var skybox = RenderSettings.skybox;
            if (skybox != null)
                foreach (var pair in TexturesOf(skybox)) UseOf(pair.Key).skybox = true;

            var lightmaps = LightmapSettings.lightmaps;
            if (lightmaps != null)
            {
                foreach (var data in lightmaps)
                {
                    if (data == null) continue;
                    if (data.lightmapColor != null) UseOf(data.lightmapColor).lightmap = true;
                    if (data.lightmapDir != null) UseOf(data.lightmapDir).lightmap = true;
                    if (data.shadowMask != null) UseOf(data.shadowMask).lightmap = true;
                }
            }

            foreach (var probe in All<ReflectionProbe>())
            {
                if (probe.bakedTexture != null) UseOf(probe.bakedTexture).probe = true;
                if (probe.customBakedTexture != null) UseOf(probe.customBakedTexture).probe = true;
            }
        }

        // uGUI is a package, so graphics are read through their "mainTexture" property by reflection.
        void CollectUiTextures()
        {
            foreach (var canvasRenderer in All<CanvasRenderer>())
            {
                var canvas = FindInParents<Canvas>(canvasRenderer.transform);
                if (canvas == null) continue;
                var root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
                bool world = root.renderMode == RenderMode.WorldSpace;
                foreach (var behaviour in canvasRenderer.GetComponents<MonoBehaviour>())
                {
                    if (behaviour == null) continue;
                    var property = MainTexturePropertyOf(behaviour.GetType());
                    if (property == null) continue;
                    Texture texture = null;
                    try
                    {
                        texture = property.GetValue(behaviour, null) as Texture;
                    }
                    catch (Exception)
                    {
                        texture = null;
                    }
                    if (texture == null) continue;
                    var use = UseOf(texture);
                    use.users++;
                    if (world) use.inWorldUI = true;
                    else use.inScreenUI = true;
                }
            }
        }

        PropertyInfo MainTexturePropertyOf(Type type)
        {
            PropertyInfo property;
            if (mainTextureProperty.TryGetValue(type, out property)) return property;
            property = type.GetProperty("mainTexture", BindingFlags.Instance | BindingFlags.Public);
            if (property != null && !typeof(Texture).IsAssignableFrom(property.PropertyType)) property = null;
            mainTextureProperty[type] = property;
            return property;
        }

        void CountMesh(Mesh mesh)
        {
            if (mesh == null) return;
            int count;
            meshes.TryGetValue(mesh, out count);
            meshes[mesh] = count + 1;
        }

        void CollectMeshesAndClips()
        {
            foreach (var filter in All<MeshFilter>()) CountMesh(filter.sharedMesh);
            foreach (var skinned in All<SkinnedMeshRenderer>()) CountMesh(skinned.sharedMesh);
            foreach (var collider in All<MeshCollider>())
                if (collider.sharedMesh != null) colliderMeshes.Add(collider.sharedMesh);
            foreach (var system in All<ParticleSystem>())
            {
                var shape = system.shape;
                if (shape.mesh != null) particleMeshes.Add(shape.mesh);
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && renderer.mesh != null) particleMeshes.Add(renderer.mesh);
            }
            foreach (var source in All<AudioSource>())
            {
                if (source.clip == null) continue;
                int count;
                clips.TryGetValue(source.clip, out count);
                clips[source.clip] = count + 1;
            }
        }

        // Assets referenced from scripts and other places the passes above do not walk.
        void CollectDependencies()
        {
            if (roots.Count == 0) return;
            Object[] dependencies;
            try
            {
                dependencies = EditorUtility.CollectDependencies(roots.ToArray());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneAudit] Could not collect scene dependencies: " + e.Message);
                return;
            }
            foreach (var dependency in dependencies)
            {
                var texture = dependency as Texture;
                if (texture != null)
                {
                    if (!(texture is RenderTexture)) UseOf(texture);
                    continue;
                }
                var sprite = dependency as Sprite;
                if (sprite != null && sprite.texture != null)
                {
                    UseOf(sprite.texture);
                    continue;
                }
                var clip = dependency as AudioClip;
                if (clip != null && !clips.ContainsKey(clip)) clips[clip] = 0;
            }
        }
    }
}
