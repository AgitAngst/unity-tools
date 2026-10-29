namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        // Plain names of the checks: titles of the grouped list.
        static void AddTitles()
        {
            var titles = new[]
            {
                "DC_BUDGET", "Draw call budget exceeded", "Превышен бюджет draw calls",
                "FRAME_NO_VIEWPOINTS", "Frame budget not estimated", "Бюджет кадра не оценён",
                "DC_SHADOW_CASTERS", "Shadow casters", "Объекты с тенями",
                "DC_MANY_MATERIALS", "Too many materials on a renderer", "Слишком много материалов на рендерере",
                "DC_MULTI_MATERIAL", "Several materials on a renderer", "Несколько материалов на рендерере",
                "DC_EXTRA_MATERIALS", "More materials than submeshes", "Материалов больше, чем submesh",
                "DC_STATIC_CANDIDATE", "Could be Batching Static", "Можно сделать Batching Static",
                "DC_INSTANCING_OFF", "GPU Instancing is off", "Выключен GPU Instancing",
                "DC_INSTANCING_UNKNOWN", "GPU Instancing is off (shader not checked)", "Выключен GPU Instancing (шейдер не проверен)",
                "MESH_SCENE_TRIS", "Triangle budget exceeded", "Превышен бюджет треугольников",
                "MESH_NO_LOD", "Heavy mesh without LOD", "Тяжёлый меш без LOD",
                "MESH_READ_WRITE", "Model with Read/Write", "Модель с Read/Write",
                "MESH_READ_WRITE_NEEDED", "Model with Read/Write (used by colliders or particles)", "Модель с Read/Write (нужен коллайдерам или частицам)",
                "MESH_NOT_OPTIMIZED", "Mesh not fully optimized", "Меш оптимизирован не полностью",
                "MESH_BONES_PER_VERTEX", "More than 4 bones per vertex", "Больше 4 костей на вершину",
                "MESH_QUALITY_SKIN_WEIGHTS", "Skin Weights above 2 bones", "Skin Weights больше 2 костей",
                "MESH_GPU_SKINNING_OFF", "GPU Skinning is off", "Выключен GPU Skinning",
                "TEX_SIZE", "Texture too large", "Слишком большая текстура",
                "TEX_NOT_NORMAL_MAP", "Normal map of the wrong type", "Карта нормалей не того типа",
                "TEX_NO_MIPMAPS", "Texture without mipmaps", "Текстура без mipmaps",
                "TEX_UNCOMPRESSED", "Uncompressed texture", "Несжатая текстура",
                "TEX_READ_WRITE", "Texture with Read/Write", "Текстура с Read/Write",
                "TEX_MEMORY", "Texture memory budget exceeded", "Превышен бюджет памяти текстур",
                "TEX_ANISO_DISABLED", "Anisotropic filtering off", "Анизотропная фильтрация выключена",
                "TEX_NOT_TRILINEAR", "Textures without trilinear filtering", "Текстуры без трилинейной фильтрации",
                "LIGHT_REALTIME", "Realtime point or spot light", "Realtime point или spot",
                "LIGHT_SHADOWED", "Too many shadowed lights", "Слишком много источников с тенями",
                "LIGHT_NO_UV1", "No lightmap UVs", "Нет развёртки под свет (UV1)",
                "LIGHT_UV1_OUT_OF_RANGE", "Lightmap UVs outside 0–1", "Развёртка под свет за пределами 0–1",
                "LIGHT_UV1_OVERLAP", "Overlapping lightmap UVs", "Наложения в развёртке под свет",
                "LIGHT_SHADOW_RESOLUTION", "High shadow resolution", "Высокое разрешение теней",
                "LIGHT_SHADOW_DISTANCE", "Long shadow distance", "Большая дальность теней",
                "LIGHT_SOFT_SHADOWS", "Soft shadows of a realtime or mixed light", "Мягкие тени realtime- или mixed-источника",
                "LIGHT_GAMMA_SPACE", "Gamma color space", "Цветовое пространство Gamma",
                "LIGHT_PIXEL_LIGHTS", "More than one pixel light", "Больше одного pixel light",
                "LIGHT_REALTIME_GI", "Realtime GI is on", "Включён Realtime GI",
                "PHYS_CONCAVE_DYNAMIC", "Non-convex collider on a dynamic body", "Невыпуклый коллайдер на динамическом теле",
                "PHYS_CONCAVE_KINEMATIC", "Non-convex collider on a kinematic body", "Невыпуклый коллайдер на kinematic теле",
                "PHYS_RENDER_MESH_COLLIDER", "Collider on a heavy render mesh", "Коллайдер на тяжёлом рендер-меше",
                "PHYS_SOLVER_ITERATIONS", "Many solver iterations", "Много итераций решателя",
                "PHYS_FIXED_STEP_SLOW", "Physics slower than the headset", "Физика реже частоты шлема",
                "PHYS_FIXED_STEP_FAST", "Physics faster than the headset", "Физика чаще частоты шлема",
                "ANIM_ALWAYS_ANIMATE", "Animator always animates", "Animator анимирует всегда",
                "ANIM_NO_CONTROLLER", "Animator without controller", "Animator без контроллера",
                "ANIM_ROOT_MOTION", "Root motion without a handler", "Root motion без обработчика",
                "ANIM_COUNT", "Too many active Animators", "Слишком много активных Animator",
                "ANIM_UPDATE_OFFSCREEN", "Skinned mesh updates off-screen", "SkinnedMesh обновляется за кадром",
                "ANIM_HEAVY_SKIN", "Heavy skinned mesh", "Тяжёлый SkinnedMesh",
                "ANIM_HEAVY_SKIN_CPU", "Heavy skinned mesh on the CPU", "Тяжёлый SkinnedMesh на CPU",
                "AUDIO_DECOMPRESS_LONG", "Long clip unpacked on load", "Длинный клип распаковывается при загрузке",
                "AUDIO_PCM_LARGE", "Large PCM clip", "Большой PCM-клип",
                "AUDIO_PRELOAD_LONG", "Long clip preloaded", "Длинный клип с предзагрузкой",
                "AUDIO_NO_CLIP", "AudioSource without a clip", "AudioSource без клипа",
                "AUDIO_2D_SOURCE", "2D sound", "2D-звук",
                "AUDIO_SOURCE_COUNT", "Too many AudioSources", "Слишком много AudioSource",
                "AUDIO_PRIORITY_ZERO", "Many sources with top priority", "Много источников с высшим приоритетом",
                "AUDIO_LISTENERS_MANY", "Several AudioListeners", "Несколько AudioListener",
                "AUDIO_NO_LISTENER", "No AudioListener", "Нет AudioListener",
                "AUDIO_LISTENER_NO_CAMERA", "AudioListener not on a camera", "AudioListener не на камере",
                "AUDIO_DSP_BUFFER", "DSP buffer not tuned for VR", "DSP-буфер не настроен под VR",
                "AUDIO_SPEAKER_MODE", "Surround speaker mode", "Объёмный режим динамиков",
                "AUDIO_NO_SPATIALIZER", "No spatializer", "Нет spatializer",
                "VR_NOT_ENABLED", "VR is not enabled", "VR не включён",
                "VR_NO_OPENVR", "VR without OpenVR", "VR без OpenVR",
                "VR_BOTH_SYSTEMS", "Two VR systems enabled", "Включены две VR-системы",
                "VR_STEREO_MODE", "Stereo mode is not Single Pass Instanced", "Режим stereo не Single Pass Instanced",
                "VR_DEFERRED_TIER", "Deferred rendering for Standalone", "Deferred для Standalone",
                "VR_DEFERRED_CAMERA", "VR camera in Deferred", "VR-камера в Deferred",
                "VR_MSAA", "MSAA is off", "MSAA выключен",
                "VR_MSAA_HIGH", "MSAA 8x", "MSAA 8x",
                "VR_VSYNC", "VSync is on", "Включён VSync",
                "VR_FRAME_RATE_SCRIPT", "A script sets the frame rate", "Скрипт задаёт частоту кадров",
                "VR_NO_CAMERA", "No VR camera", "Нет VR-камеры",
                "VR_CAMERA_NO_MSAA", "VR camera without MSAA", "VR-камера без MSAA",
                "VR_CAMERA_HDR", "HDR on a VR camera", "HDR на VR-камере",
                "VR_FAR_CLIP", "Far clip plane too far", "Дальняя плоскость слишком далеко",
                "VR_NEAR_CLIP", "Near clip plane too close", "Ближняя плоскость слишком близко",
                "VR_IMAGE_EFFECT", "Full-screen effect on a VR camera", "Полноэкранный эффект на VR-камере",
                "VR_CANVAS_OVERLAY", "Canvas not visible in the headset", "Canvas не виден в шлеме",
                "VR_CANVAS_CAMERA", "Canvas stuck to the head", "Canvas прилипает к голове",
                "VR_PP_LAYER", "Post-processing on a camera", "Постобработка на камере",
                "VR_PP_EFFECTS", "Post-processing effects on", "Включены эффекты постобработки",
                "VR_STEAMVR_MISSING", "SteamVR Plugin not found", "SteamVR Plugin не найден",
                "VR_STEAMVR_NOT_IN_SCENE", "No SteamVR rig in the scenes", "В сценах нет SteamVR-rig",
                "VR_PROBE_EVERY_FRAME", "Reflection probe updated every frame", "Reflection Probe каждый кадр",
                "VR_PROBE_REALTIME", "Realtime reflection probe", "Realtime Reflection Probe",
                "VR_NO_OCCLUSION", "Occlusion culling not baked", "Occlusion culling не запечён",
                "MESH_UNUSED_CHANNELS", "Unused vertex channels", "Лишние каналы вершин",
                "MESH_SPLIT_VERTICES", "Split mesh", "Рваная сетка",
                "MESH_INDEX_32", "32-bit indices without need", "32-битные индексы без нужды",
                "MESH_COMPRESSION_OFF", "Mesh compression off", "Сжатие меша выключено",
                "MESH_IMPORT_SCALE", "Import scale is not 1", "Масштаб импорта не 1",
                "MESH_EXTREME_SCALE", "Extreme scale", "Крайний масштаб",
                "MESH_DENSE_SMALL", "Small object with a dense mesh", "Мелкий объект с плотной сеткой",
                "MESH_LOD_WEAK", "Weak LOD1", "Слабый LOD1",
                "MESH_LOD_NO_CULL", "LOD without culling", "LOD без отсечения",
                "MESH_LOD_LATE", "Late LOD transition", "Поздний переход LOD",
                "MESH_LOD_MATERIALS", "LOD levels with different materials", "Разные материалы в LOD",
                "MESH_IMPORT_ANIMATION", "Animation import on a static model", "Импорт анимации у статичной модели",
                "MESH_IMPORT_CAMERAS_LIGHTS", "Cameras or lights inside the model", "Камеры или свет внутри модели",
                "MESH_EMBEDDED_MATERIALS", "Materials inside the model", "Материалы внутри модели",
                "MESH_DEGENERATE", "Zero-area triangles", "Вырожденные треугольники",
                "MESH_DUPLICATE", "Duplicate meshes", "Дубликаты мешей",
                "MESH_PIVOT_FAR", "Far pivot", "Далёкий pivot",
                "TEX_NPOT", "Non-power-of-two texture", "Текстура не степени двойки",
                "TEX_DUPLICATE", "Duplicate textures", "Дубликаты текстур",
                "TEX_DENSITY_HIGH", "Texture too dense for the object", "Текстура слишком плотная для объекта",
                "TEX_DENSITY_LOW", "Texture too sparse for the object", "Текстура слишком редкая для объекта",
                "TEX_ALPHA_UNUSED", "Unused alpha channel", "Пустая альфа",
                "TEX_SRGB_DATA", "sRGB on a data map", "sRGB у карты данных",
                "TEX_ANISO_LOW", "Low anisotropic filtering", "Низкая анизотропия",
                "TEX_STREAMING_OFF", "No mip streaming", "Без стриминга mip",
                "MAT_DUPLICATE", "Duplicate materials", "Одинаковые материалы",
                "MAT_TRANSPARENT", "Transparent material", "Прозрачный материал",
                "MAT_KEYWORD_NO_TEXTURE", "Keyword without a texture", "Ключевое слово без текстуры",
                "MAT_SIMPLE_STANDARD", "Simple Standard materials", "Простые материалы Standard",
                "MAT_DEFAULT", "Default material", "Материал по умолчанию",
                "GEN_EMPTY_LEAF", "Empty object", "Пустой объект",
                "GEN_NEGATIVE_SCALE", "Negative scale", "Отрицательный масштаб",
                "GEN_DEEP_HIERARCHY", "Deep hierarchy", "Глубокая иерархия",
                "GEN_MISSING_SCRIPTS", "Missing scripts", "Пропавшие скрипты",
                "GEN_INACTIVE_HEAVY_MESH", "Inactive object with a heavy mesh", "Неактивный объект с тяжёлым мешем",
                "GEN_INACTIVE_HEAVY_TEXTURE", "Inactive object with a large texture", "Неактивный объект с большой текстурой",
            };
            for (int i = 0; i + 2 < titles.Length; i += 3) Add(titles[i] + ".title", titles[i + 1], titles[i + 2]);
        }

        static void AddDrawCalls()
        {
            Add("DC_BUDGET", "Up to {0} draw calls per frame — viewpoint \"{2}\" (budget {1}).", "До {0} draw calls в кадре — точка обзора «{2}» (бюджет {1}).");
            Add("DC_BUDGET.fix", "Estimate at the viewpoint: headset field of view, LOD, shadow passes, without batching. See what is heavy there on Viewpoints; merge materials, use static batching and GPU Instancing, cut shadow casters.",
                "Оценка в точке обзора: поле зрения шлема, LOD, проходы теней, без батчинга. Что там тяжёлое — на «Точках обзора»; объедините материалы, включите static batching и GPU Instancing, сократите тени.");
            Add("FRAME_NO_VIEWPOINTS", "No viewpoints: triangles and draw calls per frame are not estimated. Whole scene without culling or batching: {0} triangles, {1} draw calls.",
                "Нет точек обзора: треугольники и draw calls в кадре не оценены. Вся сцена без отсечения и батчинга: {0} треугольников, {1} draw calls.");
            Add("FRAME_NO_VIEWPOINTS.fix", "Place viewpoints on the Viewpoints tab: every run then estimates them and compares the worst one with the budgets per frame.",
                "Расставьте точки на вкладке «Точки обзора»: каждый прогон оценит их и сравнит худшую с бюджетом кадра.");
            Add("DC_SHADOW_CASTERS", "{0} renderers cast shadows; each is drawn again in {1} shadow passes.", "{0} рендереров отбрасывают тени; каждый рисуется ещё в {1} проходах теней.");
            Add("DC_SHADOW_CASTERS.fix", "Turn Cast Shadows off on small and distant objects.", "Выключите Cast Shadows у мелких и дальних объектов.");
            Add("DC_MANY_MATERIALS", "{0} materials on one renderer: each is a separate draw call.", "{0} материалов на одном рендерере: каждый — отдельный draw call.");
            Add("DC_MANY_MATERIALS.fix", "Merge materials into an atlas or split the mesh.", "Объедините материалы в атлас или разделите меш.");
            Add("DC_MULTI_MATERIAL", "{0} materials on the renderer.", "{0} материала на рендерере.");
            Add("DC_MULTI_MATERIAL.fix", "Each material adds a draw call; merge them where possible.", "Каждый материал добавляет draw call; объедините, где можно.");
            Add("DC_EXTRA_MATERIALS", "{0} materials for {1} submeshes: extra materials redraw the last submesh.", "{0} материалов на {1} submesh: лишние перерисовывают последний submesh.");
            Add("DC_EXTRA_MATERIALS.fix", "Remove the extra material slots.", "Уберите лишние слоты материалов.");
            Add("DC_STATIC_CANDIDATE", "Not Batching Static, and nothing on it or its parents moves it.", "Не Batching Static, и ничто на нём и его родителях его не двигает.");
            Add("DC_STATIC_CANDIDATE.fix", "Mark it Batching Static if it never moves. Static batching uses extra memory.", "Отметьте Batching Static, если объект не двигается. Static batching расходует память.");
            Add("DC_STATIC_CANDIDATE.do", "Add the Batching Static flag", "Добавить флаг Batching Static");
            Add("DC_INSTANCING_OFF", "GPU Instancing is off, the same mesh is drawn {0} times with this material.", "GPU Instancing выключен, один меш рисуется {0} раз с этим материалом.");
            Add("DC_INSTANCING_OFF.fix", "Enable GPU Instancing on the material.", "Включите GPU Instancing у материала.");
            Add("DC_INSTANCING_OFF.do", "Enable GPU Instancing on the material", "Включить GPU Instancing у материала");
            Add("DC_INSTANCING_UNKNOWN", "GPU Instancing is off, the same mesh is drawn {0} times with this material.", "GPU Instancing выключен, один меш рисуется {0} раз с этим материалом.");
            Add("DC_INSTANCING_UNKNOWN.fix", "Check that the shader supports instancing (#pragma multi_compile_instancing), then enable it.",
                "Проверьте, что шейдер поддерживает инстансинг (#pragma multi_compile_instancing), и включите его.");
        }

        static void AddMeshes()
        {
            Add("MESH_SCENE_TRIS", "Up to {0} triangles per frame — viewpoint \"{2}\" (budget {1}).", "До {0} треугольников в кадре — точка обзора «{2}» (бюджет {1}).");
            Add("MESH_SCENE_TRIS.fix", "Estimate at the viewpoint: headset field of view and LOD. See what is heavy there on Viewpoints; add LODs, simplify meshes, bake details into normal maps.",
                "Оценка в точке обзора: поле зрения шлема и LOD. Что там тяжёлое — на «Точках обзора»; добавьте LOD, упростите меши, перенесите детали в карты нормалей.");
            Add("MESH_NO_LOD", "{0} triangles without a LODGroup (limit {1}).", "{0} треугольников без LODGroup (порог {1}).");
            Add("MESH_NO_LOD.fix", "Add a LODGroup with simplified levels or reduce the mesh.", "Добавьте LODGroup с упрощёнными уровнями или упростите меш.");
            Add("MESH_READ_WRITE", "Read/Write is enabled: the mesh is kept twice in memory.", "Включён Read/Write: меш хранится в памяти дважды.");
            Add("MESH_READ_WRITE.fix", "Disable Read/Write unless scripts read the mesh at runtime.", "Выключите Read/Write, если скрипты не читают меш в рантайме.");
            Add("MESH_READ_WRITE.do", "Disable Read/Write in the model import settings", "Выключить Read/Write в импорте модели");
            Add("MESH_READ_WRITE_NEEDED", "Read/Write is enabled; the mesh is used by a MeshCollider or ParticleSystem.", "Включён Read/Write; меш используется MeshCollider или ParticleSystem.");
            Add("MESH_READ_WRITE_NEEDED.fix", "It may be needed (scaled colliders, particle meshes); disable only after checking.",
                "Возможно, нужен (масштабированные коллайдеры, меши частиц); выключайте только после проверки.");
            Add("MESH_NOT_OPTIMIZED", "Mesh optimization is not Everything ({0}).", "Оптимизация меша — не Everything ({0}).");
            Add("MESH_NOT_OPTIMIZED.fix", "Set Optimize Mesh to Everything in the model import settings.", "Поставьте Optimize Mesh = Everything в импорте модели.");
            Add("MESH_BONES_PER_VERTEX", "Up to {0} bones per vertex: skinning costs more.", "До {0} костей на вершину: скиннинг дороже.");
            Add("MESH_BONES_PER_VERTEX.fix", "Limit skin weights to 4 bones in the model import settings.", "Ограничьте веса 4 костями в импорте модели.");
            Add("MESH_QUALITY_SKIN_WEIGHTS", "Quality Skin Weights is {0} (more than two bones).", "Skin Weights уровня качества — {0} (больше двух костей).");
            Add("MESH_QUALITY_SKIN_WEIGHTS.fix", "Set Skin Weights to 2 Bones in Quality: skinning is cheaper; check that characters still bend well.",
                "Поставьте Skin Weights = 2 Bones в Quality: скиннинг дешевле; проверьте, что персонажи по-прежнему хорошо гнутся.");
            Add("MESH_GPU_SKINNING_OFF", "GPU Skinning is off: skinned meshes are processed on the CPU.", "GPU Skinning выключен: скиннинг идёт на CPU.");
            Add("MESH_GPU_SKINNING_OFF.fix", "Enable GPU Skinning in Player Settings.", "Включите GPU Skinning в Player Settings.");
        }

        static void AddTextures()
        {
            Add("TEX_SIZE", "Texture is {0} px (limit {1}).", "Текстура {0} px (порог {1}).");
            Add("TEX_SIZE.fix", "Lower Max Size for Standalone or split the texture.", "Уменьшите Max Size для Standalone или разделите текстуру.");
            Add("TEX_NOT_NORMAL_MAP", "Used as a normal map but imported as another texture type.", "Используется как карта нормалей, но импортирована другим типом.");
            Add("TEX_NOT_NORMAL_MAP.fix", "Set Texture Type to Normal map.", "Поставьте Texture Type = Normal map.");
            Add("TEX_NOT_NORMAL_MAP.do", "Set Texture Type to Normal map", "Поставить Texture Type = Normal map");
            Add("TEX_NO_MIPMAPS", "No mipmaps: aliasing and shimmering in VR.", "Нет mipmaps: лесенка и мерцание в VR.");
            Add("TEX_NO_MIPMAPS.fix", "Enable Generate Mip Maps.", "Включите Generate Mip Maps.");
            Add("TEX_NO_MIPMAPS.do", "Enable Generate Mip Maps", "Включить Generate Mip Maps");
            Add("TEX_UNCOMPRESSED", "Uncompressed format ({0}).", "Несжатый формат ({0}).");
            Add("TEX_UNCOMPRESSED.fix", "Use {1}: DXT1 without alpha, BC7 or DXT5 with alpha, BC6H for HDR, BC5 for normal maps.",
                "Используйте {1}: DXT1 без альфы, BC7 или DXT5 с альфой, BC6H для HDR, BC5 для нормалей.");
            Add("TEX_UNCOMPRESSED.do", "Compress the texture ({1} when the format is overridden)", "Сжать текстуру ({1}, если формат задан для Standalone)");
            Add("TEX_READ_WRITE", "Read/Write is enabled: the texture is kept twice in memory.", "Включён Read/Write: текстура хранится в памяти дважды.");
            Add("TEX_READ_WRITE.fix", "Disable Read/Write unless scripts read the pixels.", "Выключите Read/Write, если скрипты не читают пиксели.");
            Add("TEX_READ_WRITE.do", "Disable Read/Write in the texture import settings", "Выключить Read/Write в импорте текстуры");
            Add("TEX_MEMORY", "Estimated texture memory: {0} MB (budget {1} MB).", "Оценка памяти текстур: {0} МБ (бюджет {1} МБ).");
            Add("TEX_MEMORY.fix", "Compress textures, lower Max Size, remove unused ones.", "Сожмите текстуры, уменьшите Max Size, уберите лишние.");
            Add("TEX_ANISO_DISABLED", "Anisotropic filtering is disabled for the quality level.", "Анизотропная фильтрация выключена на уровне качества.");
            Add("TEX_ANISO_DISABLED.fix", "Floors and walls shimmer at grazing angles in VR; use Per Texture or Forced On.",
                "Полы и стены мерцают под углом в VR; поставьте Per Texture или Forced On.");
            Add("TEX_NOT_TRILINEAR", "Texture with mipmaps filtered {0}: the mip levels show as bands and shimmer in VR.",
                "Текстура с mipmaps и фильтрацией {0}: границы уровней mip видны полосами и мерцают в VR.");
            Add("TEX_NOT_TRILINEAR.fix", "Trilinear blends mip levels smoothly and reduces shimmering in VR. Keep Point where sharp pixels are meant: it is not in the safe fixes.",
                "Trilinear плавно смешивает уровни mip и снижает мерцание в VR. Point оставьте, если резкие пиксели задуманы: его нет среди безопасных исправлений.");
            Add("TEX_NOT_TRILINEAR.do", "Set Filter Mode to Trilinear", "Поставить Filter Mode: Trilinear");
        }

        static void AddLighting()
        {
            Add("LIGHT_REALTIME", "Realtime {0} light.", "Realtime-источник {0}.");
            Add("LIGHT_REALTIME.fix", "Bake it or set it to Mixed; every realtime light adds a pass per lit object.",
                "Запеките его или поставьте Mixed; каждый realtime-источник добавляет проход по освещённым объектам.");
            Add("LIGHT_SHADOWED", "{0} realtime or mixed lights cast shadows (limit {1}): {2}.", "{0} realtime/mixed источников с тенями (порог {1}): {2}.");
            Add("LIGHT_SHADOWED.fix", "Shadow maps are expensive at headset resolutions; keep one shadowed light.",
                "Карты теней дороги при разрешении шлема; оставьте один источник с тенями.");
            Add("LIGHT_NO_UV1", "Mesh {0} is baked into lightmaps but has no lightmap UVs (UV1).", "Меш {0} запекается в lightmap, но у него нет развёртки под свет (UV1).");
            Add("LIGHT_NO_UV1.fix", "Turn on Generate Lightmap UVs in the model import settings, or add a second UV set in the 3D editor.",
                "Включите галочку Generate Lightmap UVs в импорте модели или сделайте второй UV-канал в 3D-редакторе.");
            Add("LIGHT_NO_UV1.do", "Turn on Generate Lightmap UVs in the model import settings", "Включить Generate Lightmap UVs в импорте модели");
            Add("LIGHT_UV1_OUT_OF_RANGE", "Lightmap UVs (UV1) of mesh {0} go outside 0–1: {1}% of vertices.", "Развёртка под свет (UV1) меша {0} выходит за 0–1: {1}% вершин.");
            Add("LIGHT_UV1_OUT_OF_RANGE.fix", "Lightmap UVs must fit into 0–1: repack them in the 3D editor or use Generate Lightmap UVs.",
                "Развёртка под свет должна умещаться в 0–1: перепакуйте её в 3D-редакторе или включите Generate Lightmap UVs.");
            Add("LIGHT_UV1_OVERLAP", "Lightmap UVs (UV1) of mesh {0} overlap: {1}% of lightmap texels are shared.", "Развёртка под свет (UV1) меша {0} накладывается: {1}% текселей lightmap общие.");
            Add("LIGHT_UV1_OVERLAP.fix", "Overlapping islands get each other's light and shadows: unwrap UV1 without overlaps or use Generate Lightmap UVs.",
                "Наложенные острова получают чужой свет и тени: разверните UV1 без наложений или включите Generate Lightmap UVs.");
            Add("LIGHT_SHADOW_RESOLUTION", "Shadow resolution is {0}.", "Разрешение теней — {0}.");
            Add("LIGHT_SHADOW_RESOLUTION.fix", "Lower it to save GPU time and memory.", "Понизьте, чтобы сберечь время GPU и память.");
            Add("LIGHT_SHADOW_DISTANCE", "Shadow distance is {0} m (more than {1} m).", "Дальность теней {0} м (больше {1} м).");
            Add("LIGHT_SHADOW_DISTANCE.fix", "20–40 m is enough in VR: a shorter distance gives sharper and cheaper shadows.",
                "В VR хватает 20–40 м: меньшая дальность — чётче и дешевле тени.");
            Add("LIGHT_SOFT_SHADOWS", "{0} light with Soft Shadows.", "Источник {0} с Soft Shadows.");
            Add("LIGHT_SOFT_SHADOWS.fix", "Soft shadows cost more on every shadow map: use Hard Shadows (or Hard Shadows Only in Quality); keep Soft for the main light only when the frame has headroom.",
                "Мягкие тени дороже на каждой карте теней: поставьте Hard Shadows (или Hard Shadows Only в Quality); Soft оставьте только главному источнику, если есть запас по времени кадра.");
            Add("LIGHT_SOFT_SHADOWS.do", "Set Shadow Type to Hard Shadows", "Поставить Shadow Type = Hard Shadows");
            Add("LIGHT_GAMMA_SPACE", "Color Space is Gamma.", "Color Space — Gamma.");
            Add("LIGHT_GAMMA_SPACE.fix", "Linear gives correct lighting and blending: set Color Space to Linear in Player Settings, then rebake the lighting and check the colors of textures and materials.",
                "Linear даёт правильный свет и смешивание: поставьте Color Space = Linear в Player Settings, затем перезапеките свет и проверьте цвета текстур и материалов.");
            Add("LIGHT_PIXEL_LIGHTS", "Pixel Light Count is {0}.", "Pixel Light Count = {0}.");
            Add("LIGHT_PIXEL_LIGHTS.fix", "In Forward each extra pixel light is another pass over every lit object; use 1.",
                "В Forward каждый лишний pixel light — ещё проход по освещённым объектам; поставьте 1.");
            Add("LIGHT_REALTIME_GI", "Realtime Global Illumination is on.", "Включён Realtime Global Illumination.");
            Add("LIGHT_REALTIME_GI.fix", "It costs CPU every frame; use baked GI if lighting does not change.", "Он тратит CPU каждый кадр; используйте запечённый GI, если свет не меняется.");
        }

        static void AddPhysics()
        {
            Add("PHYS_CONCAVE_DYNAMIC", "Non-convex MeshCollider under a non-kinematic Rigidbody: not supported.", "Невыпуклый MeshCollider под не-kinematic Rigidbody: не поддерживается.");
            Add("PHYS_CONCAVE_DYNAMIC.fix", "Enable Convex or use primitive colliders.", "Включите Convex или используйте примитивные коллайдеры.");
            Add("PHYS_CONCAVE_KINEMATIC", "Non-convex MeshCollider under a kinematic Rigidbody.", "Невыпуклый MeshCollider под kinematic Rigidbody.");
            Add("PHYS_CONCAVE_KINEMATIC.fix", "Allowed, but it breaks if the body becomes dynamic.", "Допустимо, но сломается, если тело станет динамическим.");
            Add("PHYS_RENDER_MESH_COLLIDER", "MeshCollider uses the render mesh: {0} triangles (more than {1}).", "MeshCollider на рендер-меше: {0} треугольников (больше {1}).");
            Add("PHYS_RENDER_MESH_COLLIDER.fix", "Use a simplified collision mesh or primitive colliders.", "Используйте упрощённый меш коллизии или примитивы.");
            Add("PHYS_SOLVER_ITERATIONS", "Default Solver Iterations is {0} (more than {1}).", "Default Solver Iterations = {0} (больше {1}).");
            Add("PHYS_SOLVER_ITERATIONS.fix", "Lower it if the scene has no complex joints.", "Понизьте, если в сцене нет сложных сочленений.");
            Add("PHYS_FIXED_STEP_SLOW", "Fixed Timestep is {0} s ({1} Hz), slower than the {2} Hz display.", "Fixed Timestep {0} с ({1} Гц) — реже, чем экран {2} Гц.");
            Add("PHYS_FIXED_STEP_SLOW.fix", "Held objects jitter; set Fixed Timestep to 1/{2} s or interpolate rigidbodies.",
                "Предметы в руках дрожат; поставьте Fixed Timestep 1/{2} с или интерполяцию Rigidbody.");
            Add("PHYS_FIXED_STEP_FAST", "Fixed Timestep is {0} s ({1} Hz), faster than the {2} Hz display.", "Fixed Timestep {0} с ({1} Гц) — чаще, чем экран {2} Гц.");
            Add("PHYS_FIXED_STEP_FAST.fix", "Extra physics steps cost CPU; 1/{2} s is enough for the headset.", "Лишние шаги физики тратят CPU; для шлема хватает 1/{2} с.");
        }

        static void AddAnimators()
        {
            Add("ANIM_ALWAYS_ANIMATE", "Culling Mode is Always Animate.", "Culling Mode = Always Animate.");
            Add("ANIM_ALWAYS_ANIMATE.fix", "Use Cull Update Transforms. Cull Completely stops off-screen doors and platforms with colliders, Animation Events and IK.",
                "Поставьте Cull Update Transforms. Cull Completely останавливает за кадром двери и платформы с коллайдерами, Animation Events и IK.");
            Add("ANIM_ALWAYS_ANIMATE.do", "Set Culling Mode to Cull Update Transforms", "Поставить Culling Mode = Cull Update Transforms");
            Add("ANIM_NO_CONTROLLER", "Animator without a controller.", "Animator без контроллера.");
            Add("ANIM_NO_CONTROLLER.fix", "Remove it unless a script assigns the controller.", "Уберите, если контроллер не назначает скрипт.");
            Add("ANIM_ROOT_MOTION", "Apply Root Motion without NavMeshAgent, CharacterController, Rigidbody or OnAnimatorMove.",
                "Apply Root Motion без NavMeshAgent, CharacterController, Rigidbody и OnAnimatorMove.");
            Add("ANIM_ROOT_MOTION.fix", "Turn it off if the object is moved by other means.", "Выключите, если объект двигают иначе.");
            Add("ANIM_COUNT", "{0} active Animators (limit {1}).", "{0} активных Animator (порог {1}).");
            Add("ANIM_COUNT.fix", "Disable animators of distant or idle objects.", "Выключайте аниматоры дальних и неподвижных объектов.");
            Add("ANIM_UPDATE_OFFSCREEN", "Update When Offscreen is on.", "Включён Update When Offscreen.");
            Add("ANIM_UPDATE_OFFSCREEN.fix", "Turn it off; fix the bounds instead if the mesh disappears at the screen edge.",
                "Выключите; если меш пропадает у края экрана — поправьте bounds.");
            Add("ANIM_UPDATE_OFFSCREEN.do", "Turn Update When Offscreen off", "Выключить Update When Offscreen");
            Add("ANIM_HEAVY_SKIN", "Skinned mesh of {0} triangles under an Animator (limit {1}).", "SkinnedMesh на {0} треугольников под Animator (порог {1}).");
            Add("ANIM_HEAVY_SKIN.fix", "Add LODs or reduce the mesh.", "Добавьте LOD или упростите меш.");
            Add("ANIM_HEAVY_SKIN_CPU", "Skinned mesh of {0} triangles under an Animator (limit {1}), skinned on the CPU.",
                "SkinnedMesh на {0} треугольников под Animator (порог {1}), скиннинг на CPU.");
            Add("ANIM_HEAVY_SKIN_CPU.fix", "Enable GPU Skinning, add LODs or reduce the mesh.", "Включите GPU Skinning, добавьте LOD или упростите меш.");
        }

        static void AddAudio()
        {
            Add("AUDIO_DECOMPRESS_LONG", "Decompress On Load for a {0} s clip: it is unpacked into RAM.", "Decompress On Load у клипа {0} с: распаковывается в память.");
            Add("AUDIO_DECOMPRESS_LONG.fix", "Use Streaming for music and ambience, Compressed In Memory for medium sounds.",
                "Для музыки и эмбиента — Streaming, для средних звуков — Compressed In Memory.");
            Add("AUDIO_DECOMPRESS_LONG.do", "Set Load Type to Streaming (60 s and longer) or Compressed In Memory",
                "Поставить Load Type = Streaming (от 60 с) или Compressed In Memory");
            Add("AUDIO_PCM_LARGE", "PCM clip of {0} MB.", "PCM-клип на {0} МБ.");
            Add("AUDIO_PCM_LARGE.fix", "Use Vorbis (quality about 70%) or ADPCM for short effects.", "Используйте Vorbis (качество около 70 %) или ADPCM для коротких эффектов.");
            Add("AUDIO_PCM_LARGE.do", "Set Compression Format to Vorbis, quality 70%", "Поставить Compression Format = Vorbis, качество 70 %");
            Add("AUDIO_PRELOAD_LONG", "Preload Audio Data for a {0} s clip.", "Preload Audio Data у клипа {0} с.");
            Add("AUDIO_PRELOAD_LONG.fix", "Turn preloading off for long clips: they slow down scene loading.", "Выключите предзагрузку длинных клипов: они замедляют загрузку сцены.");
            Add("AUDIO_PRELOAD_LONG.do", "Turn Preload Audio Data off", "Выключить Preload Audio Data");
            Add("AUDIO_NO_CLIP", "AudioSource without a clip.", "AudioSource без клипа.");
            Add("AUDIO_NO_CLIP.fix", "Fine if a script assigns the clip; otherwise remove it.", "Нормально, если клип назначает скрипт; иначе уберите.");
            Add("AUDIO_2D_SOURCE", "2D sound (Spatial Blend 0).", "2D-звук (Spatial Blend = 0).");
            Add("AUDIO_2D_SOURCE.fix", "If it is a sound of the world, make it 3D for immersion; music, UI and voice-over are fine as 2D.",
                "Если это звук мира — сделайте 3D для погружения; музыка, UI и озвучка — нормально в 2D.");
            Add("AUDIO_2D_SOURCE.do", "Set Spatial Blend to 1 (3D)", "Поставить Spatial Blend = 1 (3D)");
            Add("AUDIO_SOURCE_COUNT", "{0} active AudioSources (limit {1}; real voices {2}).", "{0} активных AudioSource (порог {1}; реальных голосов {2}).");
            Add("AUDIO_SOURCE_COUNT.fix", "Mixing costs CPU at the frame rate of the headset; pool sources and disable silent ones.", "Микширование тратит CPU на частоте кадров шлема; переиспользуйте источники и выключайте молчащие.");
            Add("AUDIO_PRIORITY_ZERO", "{0} AudioSources have priority 0 (more than {1}).", "У {0} AudioSource приоритет 0 (больше {1}).");
            Add("AUDIO_PRIORITY_ZERO.fix", "Keep the highest priority for a few key sounds.", "Оставьте высший приоритет нескольким ключевым звукам.");
            Add("AUDIO_LISTENERS_MANY", "{0} active AudioListeners in the loaded scenes.", "{0} активных AudioListener в загруженных сценах.");
            Add("AUDIO_LISTENERS_MANY.fix", "Keep exactly one, on the VR camera.", "Оставьте ровно один — на VR-камере.");
            Add("AUDIO_NO_LISTENER", "No active AudioListener in the loaded scenes.", "В загруженных сценах нет активного AudioListener.");
            Add("AUDIO_NO_LISTENER.fix", "Fine if the VR rig is created at runtime; otherwise add one to the VR camera.",
                "Нормально, если VR-rig создаётся в рантайме; иначе добавьте его на VR-камеру.");
            Add("AUDIO_LISTENER_NO_CAMERA", "AudioListener is not on a camera object.", "AudioListener не на объекте камеры.");
            Add("AUDIO_LISTENER_NO_CAMERA.fix", "Put it on the VR camera so sound follows the head.", "Перенесите на VR-камеру, чтобы звук следовал за головой.");
            Add("AUDIO_DSP_BUFFER", "DSP Buffer Size is {0}.", "DSP Buffer Size = {0}.");
            Add("AUDIO_DSP_BUFFER.fix", "Good Latency is the balance for VR; Best Latency costs CPU.", "Для VR баланс — Good Latency; Best Latency тратит CPU.");
            Add("AUDIO_SPEAKER_MODE", "Default Speaker Mode is {0}.", "Default Speaker Mode = {0}.");
            Add("AUDIO_SPEAKER_MODE.fix", "The headphones of the headset are stereo; use Stereo.", "Наушники шлема — стерео; поставьте Stereo.");
            Add("AUDIO_NO_SPATIALIZER", "No spatializer plugin is selected.", "Не выбран spatializer.");
            Add("AUDIO_NO_SPATIALIZER.fix", "An HRTF spatializer improves sound localization in VR.", "HRTF-spatializer улучшает локализацию звука в VR.");
        }

        static void AddVr()
        {
            Add("VR_NOT_ENABLED", "VR is not enabled for Standalone.", "VR не включён для Standalone.");
            Add("VR_NOT_ENABLED.fix", "Enable Virtual Reality Supported with OpenVR, or an OpenVR loader in XR Plug-in Management.",
                "Включите Virtual Reality Supported с OpenVR или загрузчик OpenVR в XR Plug-in Management.");
            Add("VR_NO_OPENVR", "VR is enabled without OpenVR ({0}).", "VR включён без OpenVR ({0}).");
            Add("VR_NO_OPENVR.fix", "Add OpenVR: Vive and Index headsets run through SteamVR.", "Добавьте OpenVR: шлемы Vive и Index работают через SteamVR.");
            Add("VR_BOTH_SYSTEMS", "Legacy VR and XR Plug-in Management are both enabled.", "Включены и legacy VR, и XR Plug-in Management.");
            Add("VR_BOTH_SYSTEMS.fix", "Keep only one of them.", "Оставьте только одну систему.");
            Add("VR_STEREO_MODE", "Stereo rendering mode is {0}.", "Режим stereo — {0}.");
            Add("VR_STEREO_MODE.fix", "Single Pass Instanced is recommended at headset resolutions; custom shaders need stereo instancing macros (UNITY_VERTEX_INPUT_INSTANCE_ID, UNITY_VERTEX_OUTPUT_STEREO).",
                "При разрешении шлема рекомендуется Single Pass Instanced; свои шейдеры должны поддерживать stereo instancing (UNITY_VERTEX_INPUT_INSTANCE_ID, UNITY_VERTEX_OUTPUT_STEREO).");
            Add("VR_DEFERRED_TIER", "Standalone graphics tier uses the Deferred rendering path.", "Графический уровень Standalone использует Deferred.");
            Add("VR_DEFERRED_TIER.fix", "Use Forward: MSAA does not work in Deferred and stereo Deferred is expensive.", "Используйте Forward: в Deferred не работает MSAA, а stereo в Deferred дорого.");
            Add("VR_DEFERRED_CAMERA", "VR camera uses the Deferred rendering path.", "VR-камера использует Deferred.");
            Add("VR_DEFERRED_CAMERA.fix", "Use Forward: MSAA does not work in Deferred.", "Используйте Forward: в Deferred не работает MSAA.");
            Add("VR_MSAA", "Anti Aliasing is off.", "Anti Aliasing выключен.");
            Add("VR_MSAA.fix", "Use 2x MSAA; 4x when the frame time has headroom (Forward only).", "Поставьте 2x MSAA; 4x — если есть запас по времени кадра (только в Forward).");
            Add("VR_MSAA_HIGH", "Anti Aliasing is {0}x.", "Anti Aliasing = {0}x.");
            Add("VR_MSAA_HIGH.fix", "8x costs much more than 4x for little gain: keep 2x–4x and raise the render resolution instead.",
                "8x намного дороже 4x при малой пользе: оставьте 2x–4x, а вместо этого поднимите разрешение рендера.");
            Add("VR_VSYNC", "VSync Count is {0}.", "VSync Count — {0}.");
            Add("VR_VSYNC.fix", "In VR the SDK of the headset sets the frame rate; VSync still caps the Game view of the editor at the monitor rate and skews the tour without a headset. Set VSync Count to Don't Sync.",
                "В VR частоту кадров задаёт SDK шлема; VSync же ограничивает Game view редактора частотой монитора и искажает замер без шлема. Поставьте VSync Count = Don't Sync.");
            Add("VR_FRAME_RATE_SCRIPT", "The script assigns {0}.", "Скрипт присваивает {0}.");
            Add("VR_FRAME_RATE_SCRIPT.fix", "With VR, Unity takes the frame rate from the SDK of the headset and ignores these values; they act without a headset — in the build window and in the Game view of the editor, where they skew the tour without a headset. Remove the assignment or make it only without VR.",
                "С VR Unity берёт частоту кадров у SDK шлема и эти значения игнорирует; действуют они без шлема — в окне сборки и в Game view редактора, где искажают замер без шлема. Уберите присваивание или делайте его только без VR.");
            Add("VR_NO_CAMERA", "No active VR camera (Target Eye set, no Target Texture).", "Нет активной VR-камеры (Target Eye задан, без Target Texture).");
            Add("VR_NO_CAMERA.fix", "Fine if the VR rig is created at runtime.", "Нормально, если VR-rig создаётся в рантайме.");
            Add("VR_CAMERA_NO_MSAA", "MSAA is off on the VR camera.", "У VR-камеры выключен MSAA.");
            Add("VR_CAMERA_NO_MSAA.fix", "Enable Allow MSAA.", "Включите Allow MSAA.");
            Add("VR_CAMERA_HDR", "HDR is on for the VR camera.", "У VR-камеры включён HDR.");
            Add("VR_CAMERA_HDR.fix", "HDR doubles render target memory and bandwidth for both eyes; turn it off unless post-processing needs it.",
                "HDR удваивает память и пропускную способность целей рендера на оба глаза; выключите, если он не нужен постобработке.");
            Add("VR_FAR_CLIP", "Far clip plane is {0} (more than {1}).", "Дальняя плоскость отсечения {0} (больше {1}).");
            Add("VR_FAR_CLIP.fix", "Use 500 or less for better depth precision.", "Поставьте 500 и меньше для точности глубины.");
            Add("VR_NEAR_CLIP", "Near clip plane is {0} (less than {1}).", "Ближняя плоскость отсечения {0} (меньше {1}).");
            Add("VR_NEAR_CLIP.fix", "A very close near plane spends depth precision and z-fighting is very visible in VR; use 0.05 or more unless hands must render closer.",
                "Слишком близкая плоскость тратит точность глубины, а z-fighting очень заметен в VR; поставьте 0.05 и больше, если руки не нужно рисовать ближе.");
            Add("VR_IMAGE_EFFECT", "Image effect script {0} on the VR camera (OnRenderImage).", "Скрипт эффекта {0} на VR-камере (OnRenderImage).");
            Add("VR_IMAGE_EFFECT.fix", "Full-screen effects run for both eyes at a high resolution; remove or simplify.",
                "Полноэкранные эффекты идут на оба глаза в высоком разрешении; уберите или упростите.");
            Add("VR_CANVAS_OVERLAY", "Canvas in Screen Space Overlay: not visible in the headset.", "Canvas в Screen Space Overlay: не виден в шлеме.");
            Add("VR_CANVAS_OVERLAY.fix", "Switch to World Space, scale about 0.001, set the event camera and use a VR pointer.",
                "Переведите в World Space, масштаб около 0.001, назначьте камеру событий и VR-указатель.");
            Add("VR_CANVAS_CAMERA", "Canvas in Screen Space Camera: sticks to the head.", "Canvas в Screen Space Camera: прилипает к голове.");
            Add("VR_CANVAS_CAMERA.fix", "Use World Space for comfort.", "Для комфорта используйте World Space.");
            Add("VR_PP_LAYER", "Post Process Layer on a camera.", "Post Process Layer на камере.");
            Add("VR_PP_LAYER.fix", "Post-processing runs for both eyes at a high resolution; keep only cheap effects.",
                "Постобработка идёт на оба глаза в высоком разрешении; оставьте только дешёвые эффекты.");
            Add("VR_PP_EFFECTS", "Post Process Volume with effects: {0}. Expensive in VR: {1}.", "Post Process Volume с эффектами: {0}. Дорогие в VR: {1}.");
            Add("VR_PP_EFFECTS.fix", "Turn off Motion Blur, Depth of Field, Chromatic Aberration, SSR and Ambient Occlusion.",
                "Выключите Motion Blur, Depth of Field, Chromatic Aberration, SSR и Ambient Occlusion.");
            Add("VR_STEAMVR_MISSING", "SteamVR Plugin not found in the project.", "SteamVR Plugin не найден в проекте.");
            Add("VR_STEAMVR_MISSING.fix", "Fine if the project uses another rig (for example XR Interaction Toolkit).", "Нормально, если в проекте другой rig (например, XR Interaction Toolkit).");
            Add("VR_STEAMVR_NOT_IN_SCENE", "SteamVR Plugin is in the project, but the scenes have no SteamVR rig components.", "SteamVR Plugin есть в проекте, но в сценах нет компонентов его rig.");
            Add("VR_STEAMVR_NOT_IN_SCENE.fix", "Fine if the rig is created at runtime or lives in another scene.", "Нормально, если rig создаётся в рантайме или лежит в другой сцене.");
            Add("VR_PROBE_EVERY_FRAME", "Realtime Reflection Probe refreshed every frame.", "Realtime Reflection Probe обновляется каждый кадр.");
            Add("VR_PROBE_EVERY_FRAME.fix", "Bake it, or refresh it on awake or via scripting.", "Запеките или обновляйте при старте или из скрипта.");
            Add("VR_PROBE_REALTIME", "Realtime Reflection Probe.", "Realtime Reflection Probe.");
            Add("VR_PROBE_REALTIME.fix", "Baked probes are preferred in VR.", "В VR лучше запечённые пробы.");
            Add("VR_NO_OCCLUSION", "{0} static renderers, but no occlusion culling data is baked.", "{0} статичных рендереров, но occlusion culling не запечён.");
            Add("VR_NO_OCCLUSION.fix", "Bake occlusion culling (Window > Rendering > Occlusion Culling).", "Запеките occlusion culling (Window > Rendering > Occlusion Culling).");
        }

        static void AddAssets()
        {
            Add("MESH_UNUSED_CHANNELS", "Mesh {0} carries vertex data its materials do not use: {1}.", "Меш {0} несёт данные вершин, которые его материалы не используют: {1}.");
            Add("MESH_UNUSED_CHANNELS.fix", "Turn off Import Tangents without normal maps, remove vertex colors and extra UV sets in the 3D editor — less memory and bandwidth.", "Без карт нормалей выключите Import Tangents, уберите цвета вершин и лишние UV-наборы в 3D-редакторе — меньше памяти и нагрузки.");
            Add("MESH_SPLIT_VERTICES", "Mesh {0}: {1} vertices for {2} triangles — the mesh is split.", "Меш {0}: {1} вершин на {2} треугольников — сетка рваная.");
            Add("MESH_SPLIT_VERTICES.fix", "Usually split normals or UV seams from the export: smooth normals, weld vertices, fewer UV islands.", "Обычно это разбитые нормали или швы UV после экспорта: сгладьте нормали, сварите вершины, уменьшите число островов UV.");
            Add("MESH_INDEX_32", "Mesh {0} uses 32-bit indices with only {1} vertices.", "Меш {0} с 32-битными индексами при {1} вершинах.");
            Add("MESH_INDEX_32.fix", "16-bit indices are enough below 65 535 vertices and take half the memory.", "До 65 535 вершин хватает 16-битных индексов — вдвое меньше памяти.");
            Add("MESH_INDEX_32.do", "Set Index Format to Auto", "Поставить Index Format = Auto");
            Add("MESH_COMPRESSION_OFF", "Mesh Compression is off for a model of {0} triangles.", "У модели на {0} треугольников выключен Mesh Compression.");
            Add("MESH_COMPRESSION_OFF.fix", "Compression only shrinks the build, not runtime memory; Low rarely shows artifacts.", "Сжатие уменьшает только размер сборки, не память в игре; Low редко даёт артефакты.");
            Add("MESH_COMPRESSION_OFF.do", "Set Mesh Compression to Low", "Поставить Mesh Compression = Low");
            Add("MESH_IMPORT_SCALE", "Model imported with Scale Factor {0}.", "Модель импортирована со Scale Factor {0}.");
            Add("MESH_IMPORT_SCALE.fix", "Export in meters with scale 1: a wrong unit breaks lighting, physics and lightmap density.", "Экспортируйте в метрах с масштабом 1: неверная единица ломает свет, физику и плотность lightmap.");
            Add("MESH_EXTREME_SCALE", "Extreme object scale {0}.", "Крайний масштаб объекта {0}.");
            Add("MESH_EXTREME_SCALE.fix", "The model was probably exported in the wrong units: fix the export instead of scaling in the scene.", "Скорее всего, модель выгружена в неверных единицах: исправьте экспорт, а не масштаб в сцене.");
            Add("MESH_DENSE_SMALL", "{0} triangles on an object of {1} cm.", "{0} треугольников на объекте {1} см.");
            Add("MESH_DENSE_SMALL.fix", "Small objects need few triangles in VR: simplify the mesh or add LODs.", "Мелким объектам в VR хватает немногих треугольников: упростите меш или добавьте LOD.");
            Add("MESH_LOD_WEAK", "LOD1 keeps {0}% of LOD0 triangles.", "LOD1 сохраняет {0}% треугольников LOD0.");
            Add("MESH_LOD_WEAK.fix", "A useful LOD1 has about half of LOD0 or less.", "Полезный LOD1 — примерно половина LOD0 и меньше.");
            Add("MESH_LOD_NO_CULL", "The last LOD level is never culled.", "Последний уровень LOD никогда не отсекается.");
            Add("MESH_LOD_NO_CULL.fix", "Give the last level a transition above 0% so distant objects disappear.", "Дайте последнему уровню переход больше 0 %, чтобы дальние объекты пропадали.");
            Add("MESH_LOD_LATE", "LOD0 is used down to {0}% of the screen.", "LOD0 держится до {0}% экрана.");
            Add("MESH_LOD_LATE.fix", "Switch to LOD1 earlier (around 30–50%): the full mesh is wasted on small screen sizes.", "Переключайте на LOD1 раньше (около 30–50 %): полный меш на маленьком размере на экране не нужен.");
            Add("MESH_LOD_MATERIALS", "LOD level {0} uses other materials than LOD0.", "Уровень LOD {0} использует другие материалы, чем LOD0.");
            Add("MESH_LOD_MATERIALS.fix", "Different materials between levels add SetPass calls and popping; share one material.", "Разные материалы на уровнях добавляют SetPass и скачки вида; используйте общий материал.");
            Add("MESH_IMPORT_ANIMATION", "Model without animation imports it (Animation Type {0}).", "Модель без анимации импортирует её (Animation Type {0}).");
            Add("MESH_IMPORT_ANIMATION.fix", "Static models need Animation Type None: otherwise each instance gets an empty Animator.", "Статичным моделям нужен Animation Type = None: иначе каждый экземпляр получает пустой Animator.");
            Add("MESH_IMPORT_ANIMATION.do", "Set Animation Type to None and turn off Import Animation", "Поставить Animation Type = None и выключить Import Animation");
            Add("MESH_IMPORT_CAMERAS_LIGHTS", "Model contains {0} from the 3D editor.", "В модели есть {0} из 3D-редактора.");
            Add("MESH_IMPORT_CAMERAS_LIGHTS.fix", "Cameras and lights from the model end up in the scene; turn off Import Cameras and Import Lights.", "Камеры и свет из модели попадают в сцену; выключите Import Cameras и Import Lights.");
            Add("MESH_IMPORT_CAMERAS_LIGHTS.do", "Turn off Import Cameras and Import Lights", "Выключить Import Cameras и Import Lights");
            Add("MESH_EMBEDDED_MATERIALS", "Model keeps {0} materials inside the file.", "Модель держит {0} материалов внутри файла.");
            Add("MESH_EMBEDDED_MATERIALS.fix", "Embedded materials cannot be edited or shared; extract them (Materials tab → Extract Materials) or use your own.", "Вложенные материалы нельзя править и переиспользовать; извлеките их (вкладка Materials → Extract Materials) или используйте свои.");
            Add("MESH_DEGENERATE", "Mesh {0} has {1} zero-area triangles.", "У меша {0} {1} треугольников нулевой площади.");
            Add("MESH_DEGENERATE.fix", "Degenerate triangles cost time and break normals and lightmap UVs; clean the mesh in the 3D editor.", "Вырожденные треугольники тратят время и портят нормали и развёртку под свет; почистите меш в 3D-редакторе.");
            Add("MESH_DUPLICATE", "{0} identical meshes stored separately: {1}.", "{0} одинаковых меша хранятся отдельно: {1}.");
            Add("MESH_DUPLICATE.fix", "Use one mesh asset: duplicates take memory and break batching.", "Используйте один меш: дубликаты занимают память и мешают батчингу.");
            Add("MESH_PIVOT_FAR", "Pivot of mesh {0} is {1} m away from the mesh.", "Pivot меша {0} в {1} м от самого меша.");
            Add("MESH_PIVOT_FAR.fix", "A far pivot breaks culling, LOD and transparent sorting; put the pivot at the base of the object in the 3D editor.", "Далёкий pivot ломает отсечение, LOD и сортировку прозрачных; поставьте pivot в основание объекта в 3D-редакторе.");
            Add("TEX_NPOT", "Texture size {0} is not a power of two and NPOT scaling is off.", "Размер текстуры {0} не степень двойки, масштаб NPOT выключен.");
            Add("TEX_NPOT.fix", "Power-of-two sizes compress better and get proper mip levels.", "Размеры степени двойки лучше сжимаются и дают правильные mip-уровни.");
            Add("TEX_NPOT.do", "Set Non-Power of 2 to To Nearest", "Поставить Non-Power of 2 = To Nearest");
            Add("TEX_DUPLICATE", "{0} identical texture files: {1}.", "{0} одинаковых файла текстур: {1}.");
            Add("TEX_DUPLICATE.fix", "Keep one file and point the materials to it.", "Оставьте один файл и переназначьте его в материалах.");
            Add("TEX_DENSITY_HIGH", "About {0} px per meter: {1} px texture on an object of {2} m.", "Около {0} px на метр: текстура {1} px на объекте {2} м.");
            Add("TEX_DENSITY_HIGH.fix", "Lower Max Size: in VR around 512–1024 px per meter is usually enough.", "Уменьшите Max Size: в VR обычно хватает 512–1024 px на метр.");
            Add("TEX_DENSITY_LOW", "About {0} px per meter: {1} px texture on an object of {2} m.", "Около {0} px на метр: текстура {1} px на объекте {2} м.");
            Add("TEX_DENSITY_LOW.fix", "The surface will look blurry up close; tile the texture or use a larger one.", "Вблизи поверхность будет размытой; затайлите текстуру или возьмите крупнее.");
            Add("TEX_ALPHA_UNUSED", "Alpha is fully opaque, but the texture keeps it ({0}).", "Альфа полностью непрозрачная, но текстура её хранит ({0}).");
            Add("TEX_ALPHA_UNUSED.fix", "Without alpha the texture compresses to DXT1: half the memory.", "Без альфы текстура сожмётся в DXT1 — вдвое меньше памяти.");
            Add("TEX_ALPHA_UNUSED.do", "Set Alpha Source to None", "Поставить Alpha Source = None");
            Add("TEX_SRGB_DATA", "Data map in {0} is imported as sRGB colour.", "Карта данных в {0} импортирована как цвет (sRGB).");
            Add("TEX_SRGB_DATA.fix", "Metallic, occlusion, height and masks are data: with sRGB on, lighting is wrong in the Linear color space.", "Metallic, occlusion, высота и маски — это данные: с sRGB освещение в Linear-пространстве неверное.");
            Add("TEX_SRGB_DATA.do", "Turn sRGB off", "Выключить sRGB");
            Add("TEX_ANISO_LOW", "Large floor or wall texture with Aniso Level {0}.", "Текстура большого пола или стены с Aniso Level {0}.");
            Add("TEX_ANISO_LOW.fix", "Surfaces at grazing angles blur and shimmer in VR; Aniso Level 4 is a good balance.", "Поверхности под углом размываются и мерцают в VR; Aniso Level 4 — хороший баланс.");
            Add("TEX_ANISO_LOW.do", "Set Aniso Level to 4", "Поставить Aniso Level = 4");
            Add("TEX_STREAMING_OFF", "Texture of {0} px without Streaming Mip Maps.", "Текстура {0} px без Streaming Mip Maps.");
            Add("TEX_STREAMING_OFF.fix", "Texture streaming is on in the quality settings: large textures should use it to save memory.", "В настройках качества включён стриминг текстур: большим текстурам стоит его использовать.");
            Add("TEX_STREAMING_OFF.do", "Turn on Streaming Mip Maps", "Включить Streaming Mip Maps");
            Add("MAT_DUPLICATE", "{0} identical materials: {1}.", "{0} одинаковых материала: {1}.");
            Add("MAT_DUPLICATE.fix", "One material instead of copies: fewer SetPass calls and better batching.", "Один материал вместо копий: меньше SetPass и лучше батчинг.");
            Add("MAT_DUPLICATE.do", "Use the first material on every scene renderer of the copies", "Поставить первый материал всем рендерерам копий в сцене");
            Add("MAT_TRANSPARENT", "Transparent material; largest object {0} m.", "Прозрачный материал; самый крупный объект {0} м.");
            Add("MAT_TRANSPARENT.fix", "Transparency is drawn without depth and overlaps: on large surfaces it eats fill rate. Use Cutout or Opaque where possible.", "Прозрачное рисуется без глубины и с наложением: на крупных поверхностях это съедает fill rate. Где можно — Cutout или Opaque.");
            Add("MAT_KEYWORD_NO_TEXTURE", "Keyword {0} is on without its texture.", "Ключевое слово {0} включено без своей текстуры.");
            Add("MAT_KEYWORD_NO_TEXTURE.fix", "The shader runs the more expensive variant for nothing; turn the keyword off.", "Шейдер зря работает в более дорогом варианте; выключите ключевое слово.");
            Add("MAT_KEYWORD_NO_TEXTURE.do", "Turn the keyword off", "Выключить ключевое слово");
            Add("MAT_SIMPLE_STANDARD", "Standard with no maps besides albedo: {0} fits; Metallic {1}, Smoothness {2} — gloss and reflections go.",
                "Standard без карт, кроме альбедо: подойдёт {0}; Metallic {1}, Smoothness {2} — блеск и отражения пропадут.");
            Add("MAT_SIMPLE_STANDARD.fix",
                "The full PBR of Standard is not needed here: the shader named in the row costs less per pixel and keeps the texture, tiling and color. Mobile/Diffuse lights only the main directional light per pixel; check the look after the change.",
                "Полный PBR Standard тут не нужен: шейдер из записи дешевле на каждый пиксель, текстура, тайлинг и цвет остаются. Mobile/Diffuse попиксельно освещает только главный направленный свет; после замены проверьте вид.");
            Add("MAT_SIMPLE_STANDARD.do", "Switch the shader to {0}", "Сменить шейдер на {0}");
            Add("MAT_SIMPLE_STANDARD_MODEL", "Material {3} inside a model is Standard with no maps besides albedo: {0} fits; Metallic {1}, Smoothness {2}.",
                "Материал {3} внутри модели — Standard без карт, кроме альбедо: подойдёт {0}; Metallic {1}, Smoothness {2}.");
            Add("MAT_SIMPLE_STANDARD_MODEL.fix",
                "A material inside a model cannot be changed: extract it (model → Materials → Extract Materials) and run the check again to switch the shader.",
                "Материал внутри модели менять нельзя: извлеките его (модель → Materials → Extract Materials) и проверьте снова — появится замена шейдера.");
            Add("MAT_DEFAULT", "Object renders with the built-in Default-Material.", "Объект рисуется встроенным Default-Material.");
            Add("MAT_DEFAULT.fix", "Assign a project material: the built-in one cannot be edited or batched with yours.", "Назначьте материал проекта: встроенный нельзя править и батчить с вашими.");
        }

        static void AddGeneral()
        {
            Add("GEN_EMPTY_LEAF", "Empty object without components or children.", "Пустой объект без компонентов и детей.");
            Add("GEN_EMPTY_LEAF.fix", "Remove it unless it is a marker used by scripts.", "Уберите, если это не метка для скриптов.");
            Add("GEN_NEGATIVE_SCALE", "Negative scale.", "Отрицательный масштаб.");
            Add("GEN_NEGATIVE_SCALE.fix", "It breaks dynamic batching and flips the face winding; mirror the mesh in the model instead.",
                "Ломает dynamic batching и переворачивает обход граней; отзеркальте меш в модели.");
            Add("GEN_DEEP_HIERARCHY", "Hierarchy deeper than {0} levels.", "Иерархия глубже {0} уровней.");
            Add("GEN_DEEP_HIERARCHY.fix", "Deep hierarchies make transform updates more expensive for moving objects.", "Глубокая иерархия удорожает пересчёт Transform у движущихся объектов.");
            Add("GEN_MISSING_SCRIPTS", "{0} missing scripts.", "Пропавших скриптов: {0}.");
            Add("GEN_MISSING_SCRIPTS.fix", "Remove the missing script components.", "Удалите компоненты пропавших скриптов.");
            Add("GEN_INACTIVE_HEAVY_MESH", "Inactive object with a mesh of {0} triangles.", "Неактивный объект с мешем на {0} треугольников.");
            Add("GEN_INACTIVE_HEAVY_MESH.fix", "It is still loaded with the scene; load it on demand if it is rarely used.",
                "Он всё равно грузится со сценой; если нужен редко — загружайте по требованию.");
            Add("GEN_INACTIVE_HEAVY_TEXTURE", "Inactive object with a {0} px texture.", "Неактивный объект с текстурой {0} px.");
            Add("GEN_INACTIVE_HEAVY_TEXTURE.fix", "It is still loaded with the scene; load it on demand if it is rarely used.",
                "Она всё равно грузится со сценой; если нужна редко — загружайте по требованию.");
        }
    }
}
