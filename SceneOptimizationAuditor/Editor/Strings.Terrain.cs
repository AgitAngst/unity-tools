namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        // Terrain checks: titles, messages, how to fix.
        static void AddTerrain()
        {
            Add("cat.Terrain", "Terrain", "Террейн");
            Add("TERRAIN_DETAIL_INSTANCES.title", "Much grass around the player", "Много травы вокруг игрока");
            Add("TERRAIN_DETAIL_INSTANCES", "≈ {0} grass and detail instances around the player ({1} triangles) within {2} m.",
                "≈ {0} травинок и деталей вокруг игрока ({1} треугольников) в радиусе {2} м.");
            Add("TERRAIN_DETAIL_INSTANCES.fix", "Lower Detail Density and Detail Distance of the terrain or paint less grass; 30–60 m of grass is usually enough in the headset.",
                "Уменьшите Detail Density и Detail Distance террейна или нарисуйте меньше травы; в шлеме обычно хватает 30–60 м травы.");
            Add("TERRAIN_DETAIL_DISTANCE.title", "Long Detail Distance", "Большая Detail Distance");
            Add("TERRAIN_DETAIL_DISTANCE", "Detail Distance is {0} m (more than {1} m).", "Detail Distance {0} м (больше {1} м).");
            Add("TERRAIN_DETAIL_DISTANCE.fix", "Grass farther away is hardly seen in the headset but is drawn for both eyes; set Detail Distance to 30–60 m.",
                "Траву дальше почти не видно в шлеме, а рисуется она для обоих глаз; поставьте Detail Distance 30–60 м.");
            Add("TERRAIN_DETAIL_DISTANCE.do", "Set Detail Distance to 60 m", "Поставить Detail Distance = 60 м");
            Add("TERRAIN_GRASS_BILLBOARD.title", "Billboard grass", "Трава-билборд");
            Add("TERRAIN_GRASS_BILLBOARD", "Detail \"{0}\" is Grass Billboard.", "Деталь «{0}» — Grass Billboard.");
            Add("TERRAIN_GRASS_BILLBOARD.fix", "Billboards turn with the head and each eye sees them turned its own way: use Grass (fixed quads) or a detail mesh.",
                "Билборды поворачиваются вслед за головой, и каждый глаз видит их по-своему: поставьте Grass (неподвижные квады) или сетку детали.");
            Add("TERRAIN_GRASS_BILLBOARD.do", "Set Render Mode to Grass in the terrain data (for every scene)", "Поставить Render Mode = Grass в данных террейна (для всех сцен)");
            Add("TERRAIN_DRAW_INSTANCED.title", "Terrain without Draw Instanced", "Террейн без Draw Instanced");
            Add("TERRAIN_DRAW_INSTANCED", "Draw Instanced is off.", "Draw Instanced выключен.");
            Add("TERRAIN_DRAW_INSTANCED.fix", "Instanced terrain rendering takes less CPU: turn Draw Instanced on in the terrain settings.",
                "Инстансинг террейна меньше грузит CPU: включите Draw Instanced в настройках террейна.");
            Add("TERRAIN_DRAW_INSTANCED.do", "Turn Draw Instanced on", "Включить Draw Instanced");
            Add("TERRAIN_PIXEL_ERROR.title", "Low Pixel Error", "Малый Pixel Error");
            Add("TERRAIN_PIXEL_ERROR", "Pixel Error is {0} (less than {1}).", "Pixel Error {0} (меньше {1}).");
            Add("TERRAIN_PIXEL_ERROR.fix", "A lower Pixel Error draws more terrain triangles; 5 is the usual value.",
                "Меньший Pixel Error рисует больше треугольников террейна; обычное значение — 5.");
        }
    }
}
