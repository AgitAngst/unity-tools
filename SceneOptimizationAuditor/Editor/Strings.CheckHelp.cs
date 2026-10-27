namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        // "More about the check": the 30 checks met most often on VR levels, by category.
        static void AddCheckHelp()
        {
            // Draw calls.
            Help("DC_MULTI_MATERIAL",
                "An active MeshRenderer has 2 to 4 materials (5 and more is the separate check “Too many materials on a renderer”). Skinned meshes are not counted; the limits are fixed.",
                "У включённого MeshRenderer 2–4 материала (5 и больше — отдельная проверка «Слишком много материалов на рендерере»). SkinnedMeshRenderer не считаются; пределы фиксированы.",
                "Each material is its own draw call, and the CPU prepares every one of them for the frame; at 90 Hz a frame lasts only 11.1 ms.",
                "Каждый материал — отдельный draw call, и процессор готовит каждый к кадру, а на 90 Гц кадр длится всего 11,1 мс.",
                "In the 3D editor put the textures of the materials into one atlas, give the model one material and reimport it. There is no Fix button: the model and its UVs have to change.",
                "В 3D-редакторе соберите текстуры материалов в один атлас, дайте модели один материал и переимпортируйте её. Кнопки «Исправить» нет: нужно менять модель и её развёртку.",
                "Materials − 1 draw calls (≈): 3 materials — 2 draw calls. Batching and the second eye are not counted.",
                "Материалов − 1 draw calls (≈): 3 материала — 2 draw calls. Батчинг и второй глаз не учитываются.",
                "When the materials really differ — glass next to metal, an emissive part, another shader — or when the object is a single one and the draw call budget has room.",
                "Когда материалы действительно разные — стекло рядом с металлом, светящаяся часть, другой шейдер — или когда объект один, а бюджет draw calls с запасом.");

            Help("DC_INSTANCING_OFF",
                "A material with GPU Instancing off draws one mesh more than 3 times on objects that are not Batching Static, and its shader supports instancing.",
                "Материал с выключенным GPU Instancing рисует один меш больше 3 раз на объектах без Batching Static, и его шейдер поддерживает инстансинг.",
                "Every copy is its own draw call, and the CPU prepares each one for the frame. With instancing, the copies of one mesh with one material go in one call.",
                "Каждая копия — отдельный draw call, и процессор готовит каждый к кадру. С инстансингом копии одного меша с одним материалом рисуются одним вызовом.",
                "Select the material and tick Enable GPU Instancing in the Inspector. For a material in Assets/ the Fix button and Auto-Fix Safe Issues do the same; built-in and package materials have no button. The change is in the material, for every scene (Undo does not revert it).",
                "Выделите материал и включите Enable GPU Instancing в Inspector. У материала из Assets/ кнопки «Исправить» и «Исправить безопасное» делают то же; у встроенных материалов и материалов пакетов кнопки нет. Правка — в материале, для всех сцен (Undo её не откатывает).",
                "Copies − 1 draw calls: 5 rocks with one mesh — 4 draw calls. Exact; batching is not counted.",
                "Копий − 1 draw calls: 5 камней с одним мешем — 4 draw calls. Точная оценка; батчинг не учитывается.",
                "When the objects will be Batching Static (static batching takes them instead of instancing), or when the shader's per-object data is not set up for instancing yet.",
                "Когда объекты станут Batching Static (их возьмёт static batching, а не инстансинг) или когда данные шейдера на каждый объект ещё не готовы к инстансингу.");

            Help("DC_STATIC_CANDIDATE",
                "An active MeshRenderer on an object that is not Batching Static, when neither the object nor any of its parents has an Animator, Animation, Rigidbody, NavMeshAgent or script (MonoBehaviour).",
                "Включённый MeshRenderer на объекте без Batching Static, если ни на нём, ни на его родителях нет Animator, Animation, Rigidbody, NavMeshAgent и скриптов (MonoBehaviour).",
                "Static batching combines static objects with one material into fewer draw calls; a non-static object is drawn on its own.",
                "Static batching объединяет статичные объекты с одним материалом в меньшее число draw calls; нестатичный объект рисуется отдельно.",
                "Select the object, open the Static drop-down at the top of the Inspector and tick Batching Static. The Fix button does the same after the list of changes; the change is on the object in the scene, Undo reverts it.",
                "Выделите объект, откройте список Static вверху Inspector и включите Batching Static. Кнопка «Исправить» делает то же после списка изменений; правка — на объекте в сцене, Undo её откатывает.",
                "",
                "",
                "When something moves the object in a way the check cannot see — a script or tween on another object, or a move while the game runs. Static batching also uses extra memory.",
                "Когда объект двигается способом, которого проверка не видит: скриптом или твином на другом объекте, или он перемещается во время игры. Static batching к тому же расходует память.");

            Help("DC_SHADOW_CASTERS",
                "Counts active renderers whose Cast Shadows is not Off, and the shadow passes in which each is drawn again: the cascades of a directional light, 6 for a point light, 1 for a spot light (lights with realtime or mixed shadows only). Without shadow passes (Shadows disabled in Quality, or no such light) there is no row.",
                "Считает включённые рендереры с Cast Shadows не Off и проходы теней, в которых каждый рисуется заново: каскады направленного света, 6 на point-источник, 1 на spot (только источники с realtime- или mixed-тенями). Без проходов теней (Shadows выключены в Quality или таких источников нет) записи нет.",
                "Every shadow caster is drawn again in each shadow pass, and these draw calls are added to the draw call estimate of the scene.",
                "Каждый объект с тенью рисуется заново в каждом проходе теней, и эти draw calls входят в оценку draw calls сцены.",
                "Turn Cast Shadows off in the Mesh Renderer of small and distant objects. The passes also drop with fewer shadowed lights and fewer Shadow Cascades in the Quality settings. There is no Fix button: which shadows matter is your call.",
                "Выключите Cast Shadows в Mesh Renderer у мелких и дальних объектов. Проходов станет меньше и при меньшем числе источников с тенями и каскадов (Shadow Cascades в настройках Quality). Кнопки «Исправить» нет: какие тени нужны, решаете вы.",
                "",
                "",
                "This row is a count, not a defect. Leave the shadows that anchor things in the scene: big objects near the player and moving characters.",
                "Эта запись — счётчик, а не поломка. Оставьте тени, которые «приземляют» предметы: крупные объекты рядом с игроком и движущиеся персонажи.");

            // Meshes.
            Help("MESH_NO_LOD",
                "An active renderer outside any LODGroup whose mesh has more triangles than the Warning threshold of Budgets (object triangles without LOD, 10,000 by default); above the Critical one (25,000) it is critical.",
                "Включённый рендерер вне LODGroup, у меша которого треугольников больше порога «Предупреждение» на «Бюджетах» (треугольники объекта без LOD, по умолчанию 10 000); больше порога «Критично» (25 000) — критично.",
                "Far away the object covers a few pixels, but the GPU still processes all its triangles. A lighter LOD level takes over at a distance and spares them.",
                "Вдали объект занимает несколько пикселей, но видеокарта всё равно обрабатывает все его треугольники. Более лёгкий уровень LOD берёт на себя дальний план и избавляет от них.",
                "Add a LODGroup (Add Component > Rendering > LOD Group), keep the original renderer in LOD 0 and put renderers with a simplified mesh into LOD 1 and further; the last level can be Culled. Or make the mesh lighter in the 3D editor. There is no Fix button: LOD levels are made by hand.",
                "Добавьте LODGroup (Add Component > Rendering > LOD Group), оставьте исходный рендерер в LOD 0, а рендереры с упрощённым мешем поставьте в LOD 1 и дальше; последний уровень может быть Culled. Или облегчите меш в 3D-редакторе. Кнопки «Исправить» нет: уровни LOD делаются вручную.",
                "≈ half of the mesh triangles: LOD 1 is assumed twice as light. It is saved far from the object, so the Triangles (LOD0) bar does not change.",
                "≈ половина треугольников меша: LOD 1 считается вдвое легче. Выигрыш — вдали от объекта, поэтому полоса «Треугольники (LOD0)» не меняется.",
                "Objects that are never seen from afar: what the player holds, a cockpit, an item at arm's length on a table.",
                "Объекты, которые никогда не видны издали: то, что игрок держит в руках, кабина, предмет на столе на расстоянии вытянутой руки.");

            Help("MESH_READ_WRITE",
                "A model file in Assets/ used in the scenes has Read/Write Enabled in its import settings. If a MeshCollider or Particle System uses its mesh, it gets a separate Info row without a Fix button. One row per model file.",
                "У файла модели в Assets/, который используют сцены, в настройках импорта включён Read/Write Enabled. Если его меш использует MeshCollider или Particle System, запись другая — Info, без кнопки «Исправить». Одна запись на файл модели.",
                "With Read/Write the mesh is kept twice in memory: on the GPU and as a copy for the CPU.",
                "С Read/Write меш хранится в памяти дважды: на видеокарте и копией для процессора.",
                "Select the model file, open the Model tab in the Inspector, untick Read/Write Enabled and press Apply. The Fix button does the same after the list of changes; the change is in the import settings of the model, for every scene (Undo does not revert it).",
                "Выделите файл модели, откройте вкладку Model в Inspector, выключите Read/Write Enabled и нажмите Apply. Кнопка «Исправить» делает то же после списка изменений; правка — в настройках импорта модели, для всех сцен (Undo её не откатывает).",
                "Vertex and index buffers of the model's meshes used in the scenes (attribute sizes × vertices, plus indices): the CPU copy that goes away. Exact.",
                "Вершинные и индексные буферы мешей модели, которые используют сцены (размер атрибутов × число вершин плюс индексы): копия в памяти процессора, которая исчезнет. Точная оценка.",
                "When scripts read or build from this mesh while the game runs (mesh.vertices, combining meshes); the check cannot see scripts.",
                "Когда скрипты читают этот меш или собирают из него новый во время игры (mesh.vertices, объединение мешей); проверка скрипты не видит.");

            Help("MESH_DUPLICATE",
                "Two or more mesh assets in Assets/ used in the scenes hold the same data in different assets: vertex layout, positions, UV0 and indices. One row per group of copies.",
                "Два или больше мешей-ассетов в Assets/, которые используют сцены, хранят одни и те же данные в разных ассетах: набор каналов, позиции, UV0 и индексы. Одна запись на группу копий.",
                "Each copy is stored in memory on its own, and objects with different copies of the mesh cannot be instanced together.",
                "Каждая копия хранится в памяти отдельно, а объекты с разными копиями меша нельзя объединить в инстансинг.",
                "Pick one copy: assign it to the Mesh field of the MeshFilter on all the objects (or fix the prefabs), then delete the extra files once nothing uses them. There is no Fix button: the right copy is your choice.",
                "Выберите одну копию: назначьте её в поле Mesh у MeshFilter всех объектов (или поправьте префабы), потом удалите лишние файлы, когда ими ничто не пользуется. Кнопки «Исправить» нет: какую копию оставить, решаете вы.",
                "Memory of the vertex and index buffers of all copies except the first. Exact.",
                "Память вершинных и индексных буферов всех копий, кроме первой. Точная оценка.",
                "When the copies differ in what the check does not compare — the values of normals, tangents, vertex colors or UV1 (lightmap unwrap) — or have their own import settings on purpose.",
                "Когда копии отличаются тем, что проверка не сравнивает, — значениями нормалей, касательных, цветов вершин или UV1 (развёртка под свет), — или нарочно имеют свои настройки импорта.");

            // Textures.
            Help("TEX_SIZE",
                "A texture whose longer side (with Max Size of the Standalone override, else of the default settings) is above the Warning threshold of Budgets — 2048 px by default; above the Critical one (4096) it is critical. Cubemaps and skyboxes are only a warning; lightmaps are not checked here.",
                "Текстура, у которой большая сторона (с учётом Max Size из override Standalone, иначе из настроек по умолчанию) больше порога «Предупреждение» на «Бюджетах» — по умолчанию 2048 px; больше порога «Критично» (4096) — критично. У cubemap и skybox — только предупреждение; lightmaps здесь не проверяются.",
                "GPU memory holds every texture of the level, the lightmaps and the frames of both eyes. A 4096 px texture takes four times the memory of a 2048 px one, and from a metre away the difference is hardly seen in the headset.",
                "Память видеокарты держит все текстуры уровня, lightmaps и кадры обоих глаз. Текстура 4096 px занимает вчетверо больше памяти, чем 2048 px, а уже с метра разницу в шлеме почти не видно.",
                "Select the texture, set Max Size to 2048 in the import settings (or in the PC, Mac & Linux Standalone override) and press Apply. For large surfaces use tiling or a detail texture instead of one huge texture. There is no Fix button: the right size is your call.",
                "Выделите текстуру, поставьте Max Size = 2048 в настройках импорта (или в override PC, Mac & Linux Standalone) и нажмите Apply. Для больших поверхностей — тайлинг или detail-текстура вместо одной огромной. Кнопки «Исправить» нет: какой размер нужен, решаете вы.",
                "Memory × (1 − (Warning threshold / side)²), 2048 by default: three quarters of the memory of a 4096 px texture. Exact.",
                "Память × (1 − (порог «Предупреждение» / сторона)²), по умолчанию 2048: у текстуры 4096 px — три четверти памяти. Точная оценка.",
                "Skies and panoramas, texture atlases that replace dozens of textures, and objects the player holds right in front of the eyes.",
                "Небо и панорамы, атласы, заменяющие десятки текстур, и предметы, которые игрок держит прямо перед глазами.");

            Help("TEX_UNCOMPRESSED",
                "A texture used on 3D objects, in UI or as a skybox (lightmaps and probes are skipped) whose Standalone format is uncompressed: with a Standalone override it is RGBA32, RGB24, half, float or another raw format; without one, Compression is None.",
                "Текстура на 3D-объектах, в UI или в skybox (lightmaps и пробы пропускаются) с несжатым форматом для Standalone: при override Standalone — RGBA32, RGB24, half, float или другой «сырой» формат, без override — Compression = None.",
                "A raw RGBA32 pixel takes 32 bits, DXT1 takes 4 and BC7 or DXT5 take 8, so the texture takes 4 to 8 times more video memory than needed.",
                "Пиксель в «сыром» RGBA32 занимает 32 бита, в DXT1 — 4, в BC7 или DXT5 — 8, так что текстура занимает в 4–8 раз больше видеопамяти, чем нужно.",
                "Select the texture and in the import settings set Compression to Normal Quality (with a Standalone override pick the suggested format: DXT1 without alpha, BC7 with alpha, BC5 for normal maps, BC6H for HDR), then press Apply. The Fix button does the same after the list of changes; the change is in the import settings of the asset, for every scene (Undo does not revert it).",
                "Выделите текстуру, в настройках импорта поставьте Compression = Normal Quality (при override Standalone — предложенный формат: DXT1 без альфы, BC7 с альфой, BC5 для нормалей, BC6H для HDR) и нажмите Apply. Кнопка «Исправить» делает то же после списка изменений; правка — в настройках импорта ассета, для всех сцен (Undo её не откатывает).",
                "Memory × (1 − bits per pixel of the suggested format / of the current one): RGBA32 → BC7 saves three quarters. Exact.",
                "Память × (1 − бит на пиксель предложенного формата / текущего): RGBA32 → BC7 даёт три четверти. Точная оценка.",
                "UI sprites and pixel art with sharp edges, smooth gradients where compression shows banding, data textures that need exact values (lookup tables), and tiny textures where the memory is negligible.",
                "UI-спрайты и пиксель-арт с резкими краями, плавные градиенты, где сжатие даёт полосы, текстуры данных с точными значениями (lookup-таблицы) и крошечные текстуры, где память не важна.");

            Help("TEX_NO_MIPMAPS",
                "A texture used on 3D objects or world-space UI has Generate Mip Maps off. Textures only on a screen-space UI, lightmaps and probes are skipped.",
                "У текстуры на 3D-объектах или world-space UI выключен Generate Mip Maps. Текстуры только экранного UI, lightmaps и пробы пропускаются.",
                "Without mipmaps a distant surface is sampled from the full-size texture, so it aliases and shimmers at every small head movement.",
                "Без mipmaps дальняя поверхность читается из полноразмерной текстуры, поэтому она «лесенкой» мерцает при каждом небольшом движении головы.",
                "Select the texture, tick Generate Mip Maps in the import settings and press Apply. The Fix button and Auto-Fix Safe Issues do the same; the change is in the import settings of the asset, for every scene (Undo does not revert it).",
                "Выделите текстуру, включите Generate Mip Maps в настройках импорта и нажмите Apply. Кнопки «Исправить» и «Исправить безопасное» делают то же; правка — в настройках импорта ассета, для всех сцен (Undo её не откатывает).",
                "",
                "",
                "A texture always shown at one distance, where mip levels are never used (a flat panel fixed in front of the eyes). Mip maps also add about a third to the memory of a texture.",
                "Текстура, которая всегда на одном расстоянии и где mip-уровни не нужны (плоская панель, закреплённая перед глазами). Mipmaps к тому же добавляют около трети к памяти текстуры.");

            Help("TEX_READ_WRITE",
                "A texture used in the scenes (lightmaps and probes too) has Read/Write Enabled in its import settings.",
                "У текстуры, которую используют сцены (lightmaps и пробы тоже), в настройках импорта включён Read/Write Enabled.",
                "With Read/Write the texture is kept twice in memory: on the GPU and as a copy for the CPU.",
                "С Read/Write текстура хранится в памяти дважды: на видеокарте и копией для процессора.",
                "Select the texture, untick Read/Write Enabled in the import settings and press Apply. The Fix button does the same after the list of changes; the change is in the import settings of the asset, for every scene (Undo does not revert it).",
                "Выделите текстуру, выключите Read/Write Enabled в настройках импорта и нажмите Apply. Кнопка «Исправить» делает то же после списка изменений; правка — в настройках импорта ассета, для всех сцен (Undo её не откатывает).",
                "The memory of the texture: the CPU copy that goes away. Exact.",
                "Память текстуры — копия в памяти процессора, которая исчезнет. Точная оценка.",
                "When scripts read or write its pixels while the game runs (GetPixels, SetPixel, ReadPixels); the check cannot see scripts.",
                "Когда скрипты читают или пишут её пиксели во время игры (GetPixels, SetPixel, ReadPixels); проверка скрипты не видит.");

            Help("TEX_ALPHA_UNUSED",
                "A Default-type texture (not a normal map or sprite) from a PNG file with Alpha Source = Input Texture Alpha, where every pixel of alpha is fully opaque, while its format keeps an alpha channel (DXT5, BC7, RGBA32 and the like). Other file types and PNG files over 16 MB are not checked.",
                "Текстура типа Default (не карта нормалей и не спрайт) из PNG-файла с Alpha Source = Input Texture Alpha, у которой альфа полностью непрозрачна в каждом пикселе, а формат хранит альфа-канал (DXT5, BC7, RGBA32 и подобные). Файлы других типов и PNG больше 16 МБ не проверяются.",
                "An alpha channel nobody uses doubles the memory of a compressed texture: DXT5 and BC7 take 8 bits per pixel, DXT1 takes 4.",
                "Альфа-канал, которым никто не пользуется, удваивает память сжатой текстуры: DXT5 и BC7 занимают 8 бит на пиксель, DXT1 — 4.",
                "Select the texture, set Alpha Source to None in the import settings and press Apply; with an automatic format it then compresses to DXT1, with a format set in the Standalone override pick DXT1 yourself. The Fix button sets Alpha Source after the list of changes; the change is in the import settings of the asset, for every scene (Undo does not revert it).",
                "Выделите текстуру, поставьте Alpha Source = None в настройках импорта и нажмите Apply; при автоматическом формате она сожмётся в DXT1, при формате из override Standalone выберите DXT1 сами. Кнопка «Исправить» ставит Alpha Source после списка изменений; правка — в настройках импорта ассета, для всех сцен (Undo её не откатывает).",
                "Memory × (1 − 4 / bits per pixel of the current format), down to DXT1: DXT5 or BC7 → DXT1 saves half. Exact.",
                "Память × (1 − 4 / бит на пиксель текущего формата) — до DXT1: DXT5 или BC7 → DXT1 даёт половину. Точная оценка.",
                "When a shader reads alpha as data (for example smoothness) and the fully opaque value is intended.",
                "Когда шейдер читает альфу как данные (например, гладкость) и полностью непрозрачное значение задумано.");

            Help("TEX_DUPLICATE",
                "Several texture files in Assets/ used in the scenes are byte-for-byte equal (the same file hash) under different names or paths. One row per group of copies.",
                "Несколько файлов текстур в Assets/, которые используют сцены, побайтно одинаковы (один хэш файла) под разными именами или путями. Одна запись на группу копий.",
                "Every copy is loaded into GPU memory on its own, though the picture is the same.",
                "Каждая копия загружается в память видеокарты отдельно, хотя картинка одна и та же.",
                "Keep one file: assign it in the materials that use the other copies (drag it onto the slot), then delete the extra files. There is no Fix button: which file to keep is your choice.",
                "Оставьте один файл: назначьте его в материалах, которые используют остальные копии (перетащите на слот), потом удалите лишние файлы. Кнопки «Исправить» нет: какой файл оставить, решаете вы.",
                "Memory of all copies except the first. Exact.",
                "Память всех копий, кроме первой. Точная оценка.",
                "When the copies are imported differently on purpose (one as sRGB colour, one as linear data, one with Read/Write), or the files belong to a third-party asset you do not want to edit.",
                "Когда копии нарочно импортированы по-разному (одна как цвет sRGB, другая как линейные данные, третья с Read/Write) или файлы принадлежат стороннему ассету, который вы не хотите править.");

            Help("TEX_NPOT",
                "A 2D texture used on 3D objects (not a Sprite; lightmaps and probes are skipped) whose width or height is not a power of two, with Non Power of 2 set to None.",
                "2D-текстура на 3D-объектах (не Sprite; lightmaps и пробы пропускаются), у которой ширина или высота не степень двойки, при Non Power of 2 = None.",
                "Power-of-two textures compress better and get proper mip levels.",
                "Текстуры со сторонами степени двойки лучше сжимаются и дают правильные mip-уровни.",
                "Best: resize the source image to a power of two (512, 1024, 2048…). Or set Non Power of 2 to To Nearest in the import settings and press Apply: the image is then stretched to the nearest power of two at import. The Fix button does that after the list of changes; the change is in the import settings of the asset, for every scene (Undo does not revert it).",
                "Лучше всего: приведите исходную картинку к степени двойки (512, 1024, 2048…). Или поставьте Non Power of 2 = To Nearest в настройках импорта и нажмите Apply: при импорте картинка растянется до ближайшей степени двойки. Кнопка «Исправить» делает это после списка изменений; правка — в настройках импорта ассета, для всех сцен (Undo её не откатывает).",
                "",
                "",
                "When the picture must keep its exact pixels — font atlases, lookup tables, textures a shader reads pixel by pixel: stretching would shift them.",
                "Когда картинка должна сохранить точные пиксели — атласы шрифтов, lookup-таблицы, текстуры, которые шейдер читает попиксельно: растяжение сдвинет их.");

            // Lighting.
            Help("LIGHT_REALTIME",
                "An active Point or Spot light whose Mode is Realtime.",
                "Включённый источник Point или Spot с Mode = Realtime.",
                "A realtime point or spot light costs a lighting pass on every object it reaches, every frame. A baked light costs nothing at runtime.",
                "Realtime-источник point или spot каждый кадр стоит прохода освещения на каждом объекте, до которого дотягивается. Запечённый источник во время игры ничего не стоит.",
                "Select the light and set Mode to Baked (or Mixed if moving objects must still be lit by it), then rebake with Generate Lighting in the Lighting window. There is no Fix button: baking is yours to start.",
                "Выделите источник и поставьте Mode = Baked (или Mixed, если им должны освещаться и движущиеся объекты), затем перезапеките свет кнопкой Generate Lighting в окне Lighting. Кнопки «Исправить» нет: запекание запускаете вы.",
                "≈ the number of renderers in the light's range and culling mask, in draw calls: one pass on each, as if the light became Baked. Pixel Light Count and Deferred are not taken into account.",
                "≈ число рендереров в зоне источника (с учётом его маски слоёв), в draw calls: по проходу на каждый, как если бы источник стал Baked. Pixel Light Count и Deferred не учитываются.",
                "Lights that must change while the game runs — a flashlight, a muzzle flash, a lamp that switches on or flickers — and lights whose range holds few objects.",
                "Источники, которые должны меняться во время игры, — фонарик, вспышка выстрела, лампа, что включается или мерцает, — и источники, в зоне которых мало объектов.");

            Help("LIGHT_SHADOWED",
                "Active lights with shadows whose Mode is Realtime or Mixed (directional ones too) are more than the Warning threshold of Budgets — 1 by default; more than the Critical one (2) is critical. Baked lights are not counted; there is one row for the whole run.",
                "Включённых источников с тенями (Mode — Realtime или Mixed, направленные тоже) больше порога «Предупреждение» на «Бюджетах» — по умолчанию 1; больше порога «Критично» (2) — критично. Baked-источники не считаются; запись одна на весь аудит.",
                "Each such light draws its shadow map every frame: 6 faces for a point light, 1 map for a spot light, up to 4 cascades for a directional one. Shadow maps are expensive at headset resolutions.",
                "Каждый такой источник каждый кадр рисует свою карту теней: 6 граней у point, 1 карту у spot, до 4 каскадов у направленного. Карты теней дороги при разрешении шлема.",
                "Keep shadows on one main light (usually the sun): set Shadow Type to No Shadows on the others, or switch their Mode to Baked. There is no Fix button: which light keeps its shadow is your choice.",
                "Оставьте тени одному главному источнику (обычно солнцу): у остальных поставьте Shadow Type = No Shadows или переключите Mode на Baked. Кнопки «Исправить» нет: какой источник оставить с тенью, решаете вы.",
                "",
                "",
                "By default one shadowed light is within the threshold. More may stay when the look needs them (a flashlight in a dark level) and the frame time holds: check the Profiler tab.",
                "По умолчанию один источник с тенями укладывается в порог. Больше можно оставить, если без них вид не тот (фонарик на тёмном уровне) и время кадра держится: проверьте на вкладке «Профайлер».");

            Help("LIGHT_NO_UV1",
                "An active MeshRenderer with the Contribute GI flag and Receive Global Illumination = Lightmaps whose mesh has no UV1, the second UV channel for lightmaps. One row per mesh.",
                "Включённый MeshRenderer с флагом Contribute GI и Receive Global Illumination = Lightmaps, у меша которого нет UV1 — второго UV-канала под lightmaps. Одна запись на меш.",
                "Baked light is stored in a texture, and every point of the mesh needs its own place there: that is what UV1 gives. Without it the light cannot be laid out on the object correctly.",
                "Запечённый свет хранится в текстуре, и каждой точке меша нужно своё место в ней: его даёт UV1. Без него свет нельзя правильно разложить по объекту.",
                "In the model's import settings (Model tab) tick Generate Lightmap UVs, press Apply and rebake the lighting. The Fix button does this when the box is off; the change is in the import settings of the model, for every scene. For meshes from other sources make a second UV set without overlaps in the 3D editor.",
                "В настройках импорта модели (вкладка Model) включите Generate Lightmap UVs, нажмите Apply и перезапеките свет. Кнопка «Исправить» делает это, когда галочка выключена; правка — в настройках импорта модели, для всех сцен. Для мешей из других источников сделайте второй UV-набор без наложений в 3D-редакторе.",
                "",
                "",
                "Hardly ever: if the object does not need baked light, set its Receive Global Illumination to Light Probes (or turn Contribute GI off) and the row goes away.",
                "Почти никогда: если объекту не нужен запечённый свет, поставьте ему Receive Global Illumination = Light Probes (или выключите Contribute GI) — запись исчезнет.");

            Help("LIGHT_SMALL_IN_LIGHTMAP",
                "A MeshRenderer baked into lightmaps (Contribute GI, Receive Global Illumination = Lightmaps, Baked GI on) whose longest side is under 0.5 m (a fixed limit).",
                "MeshRenderer, запечённый в lightmaps (Contribute GI, Receive Global Illumination = Lightmaps, Baked GI включён), у которого большая сторона меньше 0,5 м (фиксированный предел).",
                "A small object takes a piece of the lightmap atlas and often shows seams. Light from probes is cheaper and has no seams on small objects.",
                "Мелкий объект занимает кусок атласа lightmap и часто даёт швы. Свет из проб для мелких объектов дешевле и без швов.",
                "Select the object, set Receive Global Illumination to Light Probes in the Mesh Renderer and rebake the lighting. The Fix button sets it after the list of changes; the change is on the object in the scene, Undo reverts it, but the rebake is yours.",
                "Выделите объект, поставьте в Mesh Renderer Receive Global Illumination = Light Probes и перезапеките свет. Кнопка «Исправить» ставит это после списка изменений; правка — на объекте в сцене, Undo её откатывает, а перезапекание за вами.",
                "≈ the share of the lightmap atlas the object occupies (its part of the atlas area × the atlas memory), after you rebake the lighting.",
                "≈ доля атласа lightmap, которую занимает объект (его часть площади атласа × память атласа), после перезапекания света.",
                "When probe lighting would look flat on it (a small item in a sharp baked shadow), or when no Light Probe Group covers the place.",
                "Когда свет из проб будет выглядеть плоским (мелкий предмет в резкой запечённой тени) или когда это место не накрывает ни один Light Probe Group.");

            Help("LIGHT_LIGHTMAP_UNCOMPRESSED",
                "The scene has baked lightmaps and Compress Lightmaps is off in the Lighting window. This is a scene setting, not an object.",
                "В сцене есть запечённые lightmaps, а Compress Lightmaps выключено в окне Lighting. Это настройка сцены, а не объект.",
                "Lightmaps are large textures in video memory (their total counts in the texture memory); uncompressed, they take several times more.",
                "Lightmaps — крупные текстуры в видеопамяти (их сумма входит в память текстур); без сжатия они занимают в несколько раз больше.",
                "Open the Lighting window, tick Compress Lightmaps in the lightmapping settings and rebake the lighting. The Settings button opens that window; there is no Fix button.",
                "Откройте окно Lighting, включите Compress Lightmaps в настройках lightmapping и перезапеките свет. Кнопка «Настройки» открывает это окно; кнопки «Исправить» нет.",
                "≈ 75% of the lightmap memory (colour, direction and shadowmask): compressed lightmaps are assumed to take a quarter.",
                "≈ 75% памяти lightmaps (цвет, направление и shadowmask): сжатые lightmaps считаются в четверть.",
                "When compression shows blocks or banding on smooth gradients of baked light, or when the lightmaps are small against the Lightmap memory threshold of Budgets (128 MB by default).",
                "Когда сжатие даёт блоки или полосы на плавных градиентах запечённого света или когда lightmaps малы по сравнению с порогом памяти lightmaps на «Бюджетах» (по умолчанию 128 МБ).");

            // Animators.
            Help("ANIM_ALWAYS_ANIMATE",
                "An active Animator whose Culling Mode is Always Animate.",
                "Включённый Animator с Culling Mode = Always Animate.",
                "Always Animate plays the animation even when the object is out of view, so Animators in other rooms spend CPU time for nothing.",
                "Always Animate проигрывает анимацию, даже когда объекта не видно, так что Animator в других комнатах зря тратят время процессора.",
                "Select the object and set Culling Mode to Cull Update Transforms in the Animator. The Fix button does the same after the list of changes; the change is on the object in the scene, Undo reverts it. Cull Completely would also stop off-screen doors and platforms with colliders, Animation Events and IK.",
                "Выделите объект и поставьте у Animator Culling Mode = Cull Update Transforms. Кнопка «Исправить» делает то же после списка изменений; правка — на объекте в сцене, Undo её откатывает. Cull Completely остановил бы за кадром и двери и платформы с коллайдерами, Animation Events и IK.",
                "",
                "",
                "When the animation must run off-screen: a door, lift or platform with colliders, Animation Events, IK. Check these in Play Mode after the change; if one stopped, return Always Animate for it.",
                "Когда анимация должна идти за кадром: дверь, лифт или платформа с коллайдерами, Animation Events, IK. После правки проверьте их в Play Mode; если что-то встало, верните этому объекту Always Animate.");

            // Audio.
            Help("AUDIO_DECOMPRESS_LONG",
                "An audio clip in Assets/ used in the scenes has Load Type = Decompress On Load (the Standalone override, else the default) and is longer than 10 seconds (a fixed limit).",
                "У аудиоклипа в Assets/, который используют сцены, Load Type = Decompress On Load (override Standalone, иначе по умолчанию), и он длиннее 10 секунд (фиксированный предел).",
                "Such a clip is unpacked into memory as raw 16-bit sound when it loads: a minute of stereo at 44.1 kHz takes about 10 MB, and the unpacking costs CPU time.",
                "Такой клип при загрузке распаковывается в память «сырым» 16-битным звуком: минута стерео на 44,1 кГц занимает около 10 МБ, а распаковка стоит времени процессора.",
                "Select the clip and set Load Type to Streaming for music and ambience, or Compressed In Memory for medium sounds, then press Apply. The Fix button does the same after the list of changes (Streaming from 60 seconds, otherwise Compressed In Memory); the change is in the import settings of the clip, for every scene (Undo does not revert it).",
                "Выделите клип и поставьте Load Type = Streaming для музыки и эмбиента или Compressed In Memory для средних звуков, затем нажмите Apply. Кнопка «Исправить» делает то же после списка изменений (Streaming от 60 секунд, иначе Compressed In Memory); правка — в настройках импорта клипа, для всех сцен (Undo её не откатывает).",
                "≈ 85% of the clip's raw 16-bit size (samples × channels × 2 bytes): kept compressed or streamed, it is assumed to take about 15%.",
                "≈ 85% «сырого» 16-битного размера клипа (сэмплы × каналы × 2 байта): сжатый в памяти или стримингом он считается примерно в 15%.",
                "When a long clip must start with no delay and memory is not tight; Streaming and Compressed In Memory both spend CPU time while playing.",
                "Когда длинный клип должен стартовать без задержки, а памяти хватает; Streaming и Compressed In Memory оба тратят время процессора при проигрывании.");

            Help("AUDIO_2D_SOURCE",
                "An active AudioSource with Spatial Blend = 0, a fully 2D sound.",
                "Включённый AudioSource с Spatial Blend = 0 — полностью 2D-звук.",
                "A 2D sound is the same in both ears wherever you turn your head. A sound of the world in 3D comes from its place, and that adds to the feeling of presence in VR.",
                "2D-звук одинаков в обоих ушах, куда бы вы ни повернули голову. Звук мира в 3D идёт из своего места, и это добавляет ощущения присутствия в VR.",
                "Set Spatial Blend to 1 (3D) in the AudioSource. The Fix button does the same after the list of changes; the change is on the object in the scene, Undo reverts it.",
                "Поставьте у AudioSource Spatial Blend = 1 (3D). Кнопка «Исправить» делает то же после списка изменений; правка — на объекте в сцене, Undo её откатывает.",
                "",
                "",
                "Music, UI sounds, narrator and voice-over, and anything meant to sound the same in both ears.",
                "Музыка, звуки интерфейса, диктор и озвучка и всё, что должно звучать одинаково в обоих ушах.");

            // VR settings.
            Help("VR_STEREO_MODE",
                "VR is on for Standalone and the stereo rendering mode is known, but it is not Single Pass Instanced (it is Multi Pass or Single Pass). This is a project setting.",
                "VR включён для Standalone, режим stereo известен, но это не Single Pass Instanced (а Multi Pass или Single Pass). Это настройка проекта.",
                "Single Pass Instanced draws both eyes with one call per object; in the other modes the audit counts every draw call twice, once per eye.",
                "Single Pass Instanced рисует оба глаза одним вызовом на объект; в остальных режимах аудит считает каждый draw call дважды — по разу на глаз.",
                "Set Stereo Rendering Mode to Single Pass Instanced: in Project Settings > Player > XR Settings, or with XR Plug-in Management in the OpenVR settings. The Settings button opens the page. Your own shaders need the stereo instancing macros first (see “Shader without stereo instancing”).",
                "Поставьте Stereo Rendering Mode = Single Pass Instanced: в Project Settings > Player > XR Settings или, с XR Plug-in Management, в настройках OpenVR. Кнопка «Настройки» открывает страницу. Своим шейдерам сначала нужны stereo instancing макросы (см. «Шейдер без stereo instancing»).",
                "",
                "",
                "While your own shaders cannot do stereo instancing yet: their objects would show in one eye only.",
                "Пока свои шейдеры не умеют stereo instancing: их объекты были бы видны только одним глазом.");

            Help("VR_CANVAS_OVERLAY",
                "An active root Canvas whose Render Mode is Screen Space - Overlay.",
                "Включённый корневой Canvas с Render Mode = Screen Space - Overlay.",
                "Such a canvas is drawn on the flat screen, not into the headset, so the player does not see it at all.",
                "Такой canvas рисуется на плоском экране, а не в шлеме, поэтому игрок его совсем не видит.",
                "Set Render Mode to World Space, scale the Canvas to about 0.001, place it in front of the player, assign the Event Camera and use a VR pointer for input. There is no Fix button: the place and size of the UI are your choice.",
                "Поставьте Render Mode = World Space, масштаб Canvas около 0.001, разместите его перед игроком, назначьте Event Camera и используйте VR-указатель для ввода. Кнопки «Исправить» нет: место и размер интерфейса выбираете вы.",
                "",
                "",
                "Only if the canvas is not meant for the headset (a debug panel for the editor) and is switched off in the build: inactive canvases are not reported.",
                "Только если canvas не предназначен для шлема (отладочная панель для редактора) и в сборке выключен: выключенные canvas не попадают в проверку.");

            Help("VR_MSAA",
                "Anti Aliasing of the quality level Standalone uses is off. 8x is reported apart as Info (MSAA 8x). This is a project setting.",
                "Anti Aliasing уровня качества, который использует Standalone, выключен. 8x — отдельная запись «Сведения» (MSAA 8x). Это настройка проекта.",
                "Without MSAA edges stair-step and shimmer in the headset. 2x already smooths them; 4x costs about twice as much as 2x in memory and bandwidth of the eye textures.",
                "Без MSAA края идут лесенкой и мерцают в шлеме. 2x уже сглаживает их; 4x стоит примерно вдвое больше, чем 2x, по памяти и пропускной способности текстур глаз.",
                "In Project Settings > Quality pick the level Standalone uses and set Anti Aliasing to 2x Multi Sampling; 4x only when the frame time has headroom. MSAA works in Forward only. The Settings button opens the page; there is no Fix button.",
                "В Project Settings > Quality выберите уровень, который использует Standalone, и поставьте Anti Aliasing = 2x Multi Sampling; 4x — только при запасе по времени кадра. MSAA работает только в Forward. Кнопка «Настройки» открывает страницу; кнопки «Исправить» нет.",
                "",
                "",
                "When the render resolution and other costs are already lowered and a measurement shows that even 2x does not fit in the frame (the Profiler and Viewpoints tabs).",
                "Когда разрешение рендера и другие расходы уже снижены, а замер показывает, что даже 2x не укладывается в кадр (вкладки «Профайлер» и «Точки обзора»).");

            Help("VR_NO_OCCLUSION",
                "More than 100 active renderers are marked Occluder Static or Occludee Static (a fixed limit), but the open scene has no baked occlusion culling data.",
                "Больше 100 включённых рендереров отмечены Occluder Static или Occludee Static (фиксированный предел), но в открытой сцене нет запечённых данных occlusion culling.",
                "Occlusion culling skips objects hidden behind walls and other objects, so draw calls and triangles are not spent on them.",
                "Occlusion culling пропускает объекты, спрятанные за стенами и другими объектами, и на них не тратятся draw calls и треугольники.",
                "Mark the static level objects Occluder Static and Occludee Static (the Static drop-down in the Inspector), then bake in Window > Rendering > Occlusion Culling on the Bake tab. There is no Fix button.",
                "Отметьте статичные объекты уровня Occluder Static и Occludee Static (список Static в Inspector), затем запеките в Window > Rendering > Occlusion Culling на вкладке Bake. Кнопки «Исправить» нет.",
                "",
                "",
                "Open levels where almost everything is visible from everywhere: little is hidden there, so the bake brings little.",
                "Открытые уровни, где почти всё видно отовсюду: прятать почти нечего, и запекание мало что даст.");

            // Materials and shaders.
            Help("MAT_TRANSPARENT",
                "A material used by active renderers has Render Queue 2501 or higher (transparent). It is a warning when the largest object with it is over 2 m, otherwise Info (fixed limits).",
                "Материал, который используют включённые рендереры, с Render Queue 2501 и выше (прозрачный). Если самый крупный объект с ним больше 2 м — предупреждение, иначе Info (фиксированные пределы).",
                "Transparent surfaces are sorted and drawn over each other, usually without writing depth, so one pixel is painted several times; on large surfaces this eats the fill rate of the GPU.",
                "Прозрачные поверхности сортируются и рисуются друг поверх друга, обычно без записи глубины, так что один пиксель закрашивается несколько раз; на крупных поверхностях это съедает fill rate видеокарты.",
                "In the Standard shader set Rendering Mode to Opaque, or to Cutout for hard edges (leaves, fences, grids). For your own shader change the queue and blending, or make the transparent surface smaller. There is no Fix button: it depends on the look.",
                "В шейдере Standard поставьте Rendering Mode = Opaque или Cutout для резких краёв (листва, заборы, сетки). Для своего шейдера смените очередь и смешивание или уменьшите прозрачную поверхность. Кнопки «Исправить» нет: всё зависит от вида.",
                "",
                "",
                "Where transparency is the point: glass, water, fog and light cards, fades, holograms. Under 2 m it is only Info.",
                "Там, где прозрачность и нужна: стекло, вода, карточки тумана и света, затухания, голограммы. Меньше 2 м — это только Info.");

            Help("SHADER_NO_STEREO",
                "The stereo mode is Single Pass Instanced, and a shader in Assets/ used by active renderers has its own vertex function (#pragma vertex) whose code, with the #include files from Assets/, lacks a call of at least one of UNITY_SETUP_INSTANCE_ID and UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO. Built-in and package shaders are not read.",
                "Включён stereo-режим Single Pass Instanced, а у шейдера из Assets/, который рисует включённые рендереры, есть своя вершинная функция (#pragma vertex), в коде которой вместе с #include из Assets/ нет вызова хотя бы одного из UNITY_SETUP_INSTANCE_ID и UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO. Встроенные шейдеры и шейдеры пакетов не читаются.",
                "In Single Pass Instanced one instanced call draws both eyes, and the vertex function has to set up the eye with these macros. Without them the objects with this shader show in one eye only.",
                "В Single Pass Instanced один instanced-вызов рисует оба глаза, и вершинная функция должна задать глаз этими макросами. Без них объекты с таким шейдером видны только одним глазом.",
                "In the shader source add UNITY_VERTEX_INPUT_INSTANCE_ID to the vertex input, UNITY_VERTEX_OUTPUT_STEREO to the output, and UNITY_SETUP_INSTANCE_ID(v) and UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o) at the start of the vertex function. There is no Fix button.",
                "В исходнике шейдера добавьте UNITY_VERTEX_INPUT_INSTANCE_ID во входную структуру, UNITY_VERTEX_OUTPUT_STEREO — в выходную, а UNITY_SETUP_INSTANCE_ID(v) и UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o) — в начало вершинной функции. Кнопки «Исправить» нет.",
                "",
                "",
                "When the macros come through an #include outside Assets/ (from a package): the check does not read those files and may be wrong. Or when the objects of the shader are never drawn for the headset.",
                "Когда макросы приходят через #include вне Assets/ (из пакета): проверка такие файлы не читает и может ошибаться. Или когда объекты с этим шейдером никогда не рисуются для шлема.");

            // Particles.
            Help("PART_LARGE_TRANSPARENT",
                "An active Particle System with an enabled renderer in Billboard, Stretched Billboard, Horizontal Billboard or Vertical Billboard mode and a transparent material (render queue 2501 or higher): its largest particle is 1 m or more (Start Size, Size over Lifetime and, by Scaling Mode, the scale of the object), with 20 or more alive at once at the peak. The limits are fixed.",
                "Включённая система частиц с включённым рендерером в режиме Billboard, Stretched Billboard, Horizontal Billboard или Vertical Billboard и прозрачным материалом (render queue 2501 и выше): самая крупная частица — от 1 м (Start Size, Size over Lifetime и, по Scaling Mode, масштаб объекта), в пике одновременно живёт 20 и больше. Пределы фиксированы.",
                "Big transparent sprites lie over each other, so the same screen pixels are painted many times; close to the eyes a few of them can cover the whole view.",
                "Крупные прозрачные спрайты лежат друг на друге, так что одни и те же пиксели экрана закрашиваются много раз; у самых глаз несколько таких частиц могут закрыть весь обзор.",
                "Use fewer and smaller particles; thick smoke looks the same with fewer layers and a denser texture. Max Particle Size in the Particle System Renderer limits how much of the screen one particle may cover near the eyes. There is no Fix button.",
                "Сделайте частиц меньше и размер меньше; густой дым выглядит так же с меньшим числом слоёв и плотной текстурой. Max Particle Size в Particle System Renderer ограничивает, какую часть экрана может закрыть одна частица у глаз. Кнопки «Исправить» нет.",
                "",
                "",
                "Short, rare effects (an explosion, a spell) that are not on screen long, and effects far from the player, where a particle covers few pixels.",
                "Короткие редкие эффекты (взрыв, заклинание), которые недолго на экране, и эффекты вдали от игрока, где частица занимает мало пикселей.");

            // Reflection probes.
            Help("REFL_RESOLUTION",
                "An active Reflection Probe whose Resolution is above 256 is a warning; 1024 and above is critical (fixed limits).",
                "Включённая Reflection Probe с Resolution больше 256 — предупреждение; 1024 и выше — критично (фиксированные пределы).",
                "The cubemap of a probe has six faces and mip levels, so its memory grows with the square of the resolution: 1024 takes 16 times more than 256. The reflection is blurred by roughness anyway.",
                "Кубическая текстура пробы — шесть граней с mip-уровнями, поэтому память растёт как квадрат разрешения: 1024 занимает в 16 раз больше, чем 256. Отражение всё равно размывается шероховатостью.",
                "Select the probe and lower Resolution to 128 or 256 in the Inspector; for a baked probe press Bake again (or Generate Lighting). There is no Fix button.",
                "Выделите пробу и снизьте Resolution до 128 или 256 в Inspector; запечённую пробу перезапеките кнопкой Bake (или Generate Lighting). Кнопки «Исправить» нет.",
                "Memory × (1 − (256 / resolution)²): a 1024 probe loses 15/16 of its memory. A baked probe counts at the size of its cubemap; an unbaked probe's memory is an estimate, and then the gain is marked ≈.",
                "Память × (1 − (256 / разрешение)²): у пробы 1024 уходит 15/16 памяти. Запечённая проба считается по размеру своей кубической текстуры; у незапечённой память — оценка, и тогда выигрыш помечен «≈».",
                "Mirror-like surfaces — chrome, polished floors, water, a showroom — where the player looks at the reflection up close; give that probe the higher resolution and keep the rest at 128–256.",
                "Зеркальные поверхности — хром, полированные полы, вода, шоурум, — где игрок рассматривает отражение вблизи; такой пробе оставьте большее разрешение, а остальным — 128–256.");
        }
    }
}
