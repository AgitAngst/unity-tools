namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        static void AddShaders()
        {
            // Shader checks: titles, messages, how to fix.
            Add("SHADER_ERROR.title", "Shader with errors", "Шейдер с ошибками");
            Add("SHADER_ERROR", "Shader {0} has errors or is not supported: its objects are drawn pink.",
                "Шейдер {0} с ошибками или не поддерживается: его объекты рисуются розовым.");
            Add("SHADER_ERROR.fix", "Fix the errors listed in the shader's Inspector; a shader of another pipeline (URP, HDRP) needs a Built-in one instead.",
                "Исправьте ошибки из Inspector шейдера; шейдер другого конвейера (URP, HDRP) замените шейдером Built-in.");
            Add("SHADER_MISSING", "Material {0} has no shader (deleted or not imported): its objects are drawn pink.",
                "У материала {0} нет шейдера (удалён или не импортирован): его объекты рисуются розовым.");
            Add("SHADER_MISSING.fix", "Pick a shader on the material or bring the shader file back.", "Выберите шейдер в материале или верните файл шейдера.");
            Add("SHADER_GRABPASS.title", "Shader with GrabPass", "Шейдер с GrabPass");
            Add("SHADER_GRABPASS", "Shader {0} uses GrabPass without a texture name: a copy of the screen for every object, for both eyes in VR.",
                "Шейдер {0} использует GrabPass без имени текстуры: копия экрана на каждый объект, в VR — для обоих глаз.");
            Add("SHADER_GRABPASS.fix", "Name the texture (GrabPass { \"_GrabTexture\" }) to copy the screen once per frame, or make the effect without the screen copy.",
                "Дайте текстуре имя (GrabPass { \"_GrabTexture\" }) — экран будет копироваться раз за кадр — или сделайте эффект без копии экрана.");
            Add("SHADER_GRABPASS_NAMED", "Shader {0} uses a named GrabPass: one copy of the screen per frame, for both eyes in VR.",
                "Шейдер {0} использует GrabPass с именем: копия экрана раз за кадр, в VR — для обоих глаз.");
            Add("SHADER_GRABPASS_NAMED.fix", "Keep it to a few effects; refraction faked with a cubemap or a normal map needs no screen copy.",
                "Оставьте его немногим эффектам; преломление через cubemap или normal map копии экрана не требует.");
            Add("SHADER_MULTIPASS.title", "Shader draws in several passes", "Шейдер рисует в несколько проходов");
            Add("SHADER_MULTIPASS", "Shader {0} draws every object in {1} passes.", "Шейдер {0} рисует каждый объект за несколько проходов: {1}.");
            Add("SHADER_MULTIPASS.fix", "Every pass is one more draw call per object, for both eyes. Merge the passes; give an outline only to the objects that need it.",
                "Каждый проход — ещё draw call на объект, для обоих глаз. Объедините проходы; контур — только тем объектам, которым он нужен.");
            Add("SHADER_NO_STEREO.title", "Shader without stereo instancing", "Шейдер без stereo instancing");
            Add("SHADER_NO_STEREO", "Shader {0} has its own vertex function without the stereo macros: with Single Pass Instanced its objects show in one eye only.",
                "У шейдера {0} своя вершинная функция без stereo-макросов: при Single Pass Instanced его объекты видны одним глазом.");
            Add("SHADER_NO_STEREO.fix",
                "Add UNITY_VERTEX_INPUT_INSTANCE_ID to the vertex input, UNITY_VERTEX_OUTPUT_STEREO to the output, and UNITY_SETUP_INSTANCE_ID(v) and UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o) at the start of the vertex function.",
                "Добавьте UNITY_VERTEX_INPUT_INSTANCE_ID во входную структуру, UNITY_VERTEX_OUTPUT_STEREO — в выходную, а UNITY_SETUP_INSTANCE_ID(v) и UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o) — в начало вершинной функции.");
            Add("SHADER_VARIANTS.title", "Many shader variants", "Много вариантов шейдера");
            Add("SHADER_VARIANTS", "Shader {0} has {1} variants (more than {2}).", "У шейдера {0} вариантов: {1} (больше {2}).");
            Add("SHADER_VARIANTS.fix", "Every multi_compile doubles the variants: use shader_feature for keywords set on materials and drop unused ones — the build and the loading get faster.",
                "Каждый multi_compile удваивает варианты: для ключевых слов материалов берите shader_feature, лишние уберите — сборка и загрузка станут быстрее.");
            Add("SHADER_ALPHA_TEST_MSAA.title", "Alpha test without alpha to coverage", "Alpha-test без alpha to coverage");
            Add("SHADER_ALPHA_TEST_MSAA", "Shader {0}: alpha-tested materials with MSAA {1}x but without AlphaToMask — the edges of leaves and fences shimmer.",
                "Шейдер {0}: alpha-test при MSAA {1}x без AlphaToMask — края листвы и сеток мерцают.");
            Add("SHADER_ALPHA_TEST_MSAA.fix", "With AlphaToMask On in the pass, MSAA smooths the cut-out edges. Standard cannot do it: foliage needs a shader of its own.",
                "С AlphaToMask On в проходе MSAA сглаживает вырезанные края. Standard этого не умеет: листве нужен свой шейдер.");
        }
    }
}
