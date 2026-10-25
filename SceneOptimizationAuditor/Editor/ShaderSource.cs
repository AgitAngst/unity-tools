using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // What the shader checks need from a .shader file under Assets/: the passes of its first SubShader, GrabPass and
    // AlphaToMask there, and the program code with the project #include files.
    public sealed class ShaderSource
    {
        public sealed class Pass
        {
            public string lightMode = "";
            // UsePass "Shader/NAME": the path; "" for a pass of its own.
            public string usePath = "";
        }

        public readonly List<Pass> passes = new List<Pass>();
        // The first SubShader holds a surface shader program (it makes its own forward passes).
        public bool surface;
        public bool grabPass;
        // A GrabPass without a texture name copies the screen for every object; a named one once per frame.
        public bool grabUnnamed;
        public bool alphaToMask;
        // Every program and include block of the file and the text of its #include files under Assets/.
        public string code = "";
        // Functions named by #pragma vertex in the programs.
        public readonly List<string> vertexFunctions = new List<string>();

        static readonly string[] VertexModes = { "VERTEX", "VERTEXLM", "VERTEXLMRGBM" };
        // Parts of a UsePass name of a pass that does not draw the object in forward rendering.
        static readonly string[] NotDrawnNames = { "SHADOW", "META", "DEFERRED", "PREPASS", "FORWARD_DELTA", "FORWARDADD", "MOTION" };

        static readonly Regex ProgramBlock = new Regex(@"\b(CGPROGRAM|CGINCLUDE|HLSLPROGRAM|HLSLINCLUDE)\b(.*?)\b(ENDCG|ENDHLSL)\b", RegexOptions.Singleline);
        static readonly Regex Include = new Regex("#include\\s+\"([^\"]+)\"");
        static readonly Regex VertexPragma = new Regex(@"#pragma\s+vertex\s+(\w+)");
        static readonly Regex SurfacePragma = new Regex(@"#pragma\s+surface\b");
        static readonly Regex LightModeTag = new Regex("\"LightMode\"\\s*=\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        static readonly Regex AlphaToMaskOn = new Regex(@"\bAlphaToMask\s+(On\b|\[)", RegexOptions.IgnoreCase);
        const char Mark = '\u0001';
        const int MaxIncludeDepth = 8;

        // Null for built-in and package shaders, and when the file cannot be read.
        public static ShaderSource Read(Shader shader)
        {
            var path = shader != null ? AssetDatabase.GetAssetPath(shader) : "";
            if (!AuditContext.IsProjectAssetPath(path) || !path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase)) return null;
            try
            {
                return Parse(File.ReadAllText(path), Path.GetDirectoryName(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneAudit] Could not read shader " + path + ": " + e.Message);
                return null;
            }
        }

        // directory: folder of the file, for relative #include paths; null reads no include files.
        public static ShaderSource Parse(string text, string directory)
        {
            var source = new ShaderSource();
            var programs = new List<string>();
            var shaderLab = ProgramBlock.Replace(StripComments(text), m =>
            {
                programs.Add(m.Groups[2].Value);
                return " " + Mark + (programs.Count - 1) + Mark + " ";
            });

            var code = new StringBuilder();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var program in programs)
            {
                code.AppendLine(program);
                foreach (Match m in VertexPragma.Matches(program)) source.vertexFunctions.Add(m.Groups[1].Value);
                if (directory != null) AddIncludes(program, directory, code, visited, 0);
            }
            source.code = code.ToString();

            var body = FirstSubShader(shaderLab);
            if (body != null) source.ReadSubShader(body, programs);
            return source;
        }

        // Drawing passes of the first SubShader in forward rendering: untagged and Always passes, ForwardBase passes (or the
        // vertex-lit ones when there are none), UsePass of such passes and one for a surface shader.
        public int DrawPasses
        {
            get
            {
                int always = 0, forward = 0;
                // Vertex, VertexLM and VertexLMRGBM are alternatives: an object gets the passes of one of them.
                var vertex = new int[VertexModes.Length];
                foreach (var pass in passes)
                {
                    if (pass.usePath.Length > 0)
                    {
                        if (!NotDrawn(pass.usePath)) always++;
                        continue;
                    }
                    var mode = pass.lightMode.ToUpperInvariant();
                    if (mode.Length == 0 || mode == "ALWAYS") always++;
                    else if (mode == "FORWARDBASE") forward++;
                    else if (Array.IndexOf(VertexModes, mode) >= 0) vertex[Array.IndexOf(VertexModes, mode)]++;
                }
                int vertexLit = 0;
                foreach (var n in vertex) vertexLit = Math.Max(vertexLit, n);
                return always + (forward > 0 ? forward : vertexLit) + (surface ? 1 : 0);
            }
        }

        // A vertex function written in the project's source without the stereo instancing macros.
        public bool VertexWithoutStereo
        {
            get
            {
                bool own = false;
                foreach (var name in vertexFunctions)
                    if (Regex.IsMatch(code, @"\w\s+" + Regex.Escape(name) + @"\s*\(")) own = true;
                if (!own) return false;
                return !Regex.IsMatch(code, @"\bUNITY_VERTEX_OUTPUT_STEREO\b") || !Regex.IsMatch(code, @"\bUNITY_INITIALIZE_VERTEX_OUTPUT_STEREO\b");
            }
        }

        // GrabPass { "_Name" }: a quoted texture name besides the Tags and Name of the pass.
        static bool NamedGrab(string block)
        {
            var rest = Regex.Replace(block, @"\bTags\s*\{[^}]*\}", "", RegexOptions.IgnoreCase);
            rest = Regex.Replace(rest, @"\bName\s+""[^""]*""", "", RegexOptions.IgnoreCase);
            return rest.IndexOf('"') >= 0;
        }

        static bool NotDrawn(string usePath)
        {
            var slash = usePath.LastIndexOf('/');
            var name = (slash >= 0 ? usePath.Substring(slash + 1) : usePath).ToUpperInvariant();
            foreach (var part in NotDrawnNames)
                if (name.Contains(part)) return true;
            return false;
        }

        void ReadSubShader(string body, List<string> programs)
        {
            alphaToMask = AlphaToMaskOn.IsMatch(body);
            int i = 0;
            while (i < body.Length)
            {
                char c = body[i];
                if (c == '"')
                {
                    i = SkipString(body, i);
                    continue;
                }
                if (c == '{')
                {
                    i = MatchingBrace(body, i) + 1;
                    continue;
                }
                if (c == Mark)
                {
                    int end = body.IndexOf(Mark, i + 1);
                    int index;
                    if (end > i && int.TryParse(body.Substring(i + 1, end - i - 1), out index) && index < programs.Count &&
                        SurfacePragma.IsMatch(programs[index]))
                        surface = true;
                    i = end > i ? end + 1 : i + 1;
                    continue;
                }
                if (char.IsLetter(c) && (i == 0 || !IsWordChar(body[i - 1])))
                {
                    int start = i;
                    while (i < body.Length && IsWordChar(body[i])) i++;
                    var word = body.Substring(start, i - start);
                    if (Same(word, "Pass") || Same(word, "GrabPass"))
                    {
                        int brace = NextNonSpace(body, i);
                        if (brace < body.Length && body[brace] == '{')
                        {
                            int close = MatchingBrace(body, brace);
                            if (Same(word, "GrabPass"))
                            {
                                grabPass = true;
                                if (!NamedGrab(body.Substring(brace + 1, Math.Max(0, close - brace - 1)))) grabUnnamed = true;
                            }
                            else
                            {
                                var m = LightModeTag.Match(body.Substring(brace, close - brace));
                                passes.Add(new Pass { lightMode = m.Success ? m.Groups[1].Value : "" });
                            }
                            i = close + 1;
                        }
                    }
                    else if (Same(word, "UsePass"))
                    {
                        int quote = NextNonSpace(body, i);
                        if (quote < body.Length && body[quote] == '"')
                        {
                            int end = SkipString(body, quote);
                            passes.Add(new Pass { usePath = body.Substring(quote + 1, Math.Max(0, end - quote - 2)) });
                            i = end;
                        }
                    }
                    continue;
                }
                i++;
            }
        }

        // Body of the first SubShader (between its braces), or null.
        static string FirstSubShader(string text)
        {
            var m = Regex.Match(text, @"\bSubShader\s*\{", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            int open = m.Index + m.Length - 1;
            int close = MatchingBrace(text, open);
            return text.Substring(open + 1, Math.Max(0, close - open - 1));
        }

        static void AddIncludes(string program, string directory, StringBuilder code, HashSet<string> visited, int depth)
        {
            if (depth >= MaxIncludeDepth) return;
            foreach (Match m in Include.Matches(program))
            {
                var name = m.Groups[1].Value.Replace('\\', '/');
                var path = name.StartsWith("Assets/", StringComparison.Ordinal) ? name : Path.Combine(directory, name).Replace('\\', '/');
                // Built-in includes (UnityCG.cginc) are not files of the project.
                if (!File.Exists(path) || !visited.Add(Path.GetFullPath(path))) continue;
                string text;
                try
                {
                    text = StripComments(File.ReadAllText(path));
                }
                catch (Exception)
                {
                    continue;
                }
                code.AppendLine(text);
                AddIncludes(text, Path.GetDirectoryName(path), code, visited, depth + 1);
            }
        }

        // Removes // and /* */ comments outside quoted strings.
        public static string StripComments(string text)
        {
            var sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '"')
                {
                    int end = SkipString(text, i);
                    sb.Append(text, i, end - i);
                    i = end;
                }
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    while (i < text.Length && text[i] != '\n') i++;
                }
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? text.Length : end + 2;
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }
            return sb.ToString();
        }

        // Index after the closing quote of the string that starts at i (strings do not span lines).
        static int SkipString(string text, int i)
        {
            int j = i + 1;
            while (j < text.Length && text[j] != '"' && text[j] != '\n') j++;
            return Math.Min(text.Length, j + 1);
        }

        // Index of the brace that closes the one at open, skipping strings; the end of the text when it is not closed.
        static int MatchingBrace(string text, int open)
        {
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"')
                {
                    i = SkipString(text, i) - 1;
                    continue;
                }
                if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return i;
            }
            return text.Length;
        }

        static int NextNonSpace(string text, int i)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            return i;
        }

        static bool IsWordChar(char c) { return char.IsLetterOrDigit(c) || c == '_'; }

        static bool Same(string a, string b) { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
    }
}
