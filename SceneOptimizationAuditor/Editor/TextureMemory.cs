using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // Rough GPU size of a texture from its format, size, mip levels and faces.
    public static class TextureMemory
    {
        // Name of the texture's format as the table below knows it.
        public static string FormatOf(Texture texture)
        {
            var t2d = texture as Texture2D;
            if (t2d != null) return t2d.format.ToString();
            var cube = texture as Cubemap;
            if (cube != null) return cube.format.ToString();
            var array = texture as Texture2DArray;
            if (array != null) return array.format.ToString();
            var t3d = texture as Texture3D;
            return t3d != null ? t3d.format.ToString() : "";
        }

        public static double EstimateBytes(Texture texture, bool keepsCpuCopy)
        {
            if (texture == null) return 0;
            int faces = 1;
            int depth = 1;
            bool volume = false;
            string format;

            var t2d = texture as Texture2D;
            var cube = texture as Cubemap;
            var array = texture as Texture2DArray;
            var t3d = texture as Texture3D;
            if (t2d != null) format = t2d.format.ToString();
            else if (cube != null)
            {
                format = cube.format.ToString();
                faces = 6;
            }
            else if (array != null)
            {
                format = array.format.ToString();
                depth = array.depth;
            }
            else if (t3d != null)
            {
                format = t3d.format.ToString();
                depth = t3d.depth;
                volume = true;
            }
            else format = "";

            int mips = Mathf.Max(1, texture.mipmapCount);
            double pixels = 0;
            for (int i = 0; i < mips; i++)
            {
                double level = (double)Mathf.Max(1, texture.width >> i) * Mathf.Max(1, texture.height >> i);
                pixels += level * (volume ? Mathf.Max(1, depth >> i) : depth);
            }
            double bytes = pixels * BitsPerPixel(format) / 8.0 * faces;
            return keepsCpuCopy ? bytes * 2 : bytes;
        }

        // Names, not enum members, so the table compiles on every editor version.
        public static double BitsPerPixel(string format)
        {
            switch (format)
            {
                case "DXT1":
                case "DXT1Crunched":
                case "BC4":
                    return 4;
                case "DXT5":
                case "DXT5Crunched":
                case "BC5":
                case "BC6H":
                case "BC7":
                case "Alpha8":
                case "R8":
                    return 8;
                case "RGB565":
                case "RGBA4444":
                case "ARGB4444":
                case "R16":
                case "RHalf":
                case "RG16":
                    return 16;
                case "RGB24":
                    return 24;
                case "RGBAHalf":
                case "RGFloat":
                    return 64;
                case "RGBAFloat":
                    return 128;
                default:
                    return 32;
            }
        }
    }
}
