using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AuditTestsEditor
{
    // One open editor serving test commands, so a test run does not start Unity for every step.
    // The editor is started once with -auditServer <dir>; scripts drop <dir>/<name>.cmd files: the first line
    // is Type.Method (a static method without parameters), the next lines are its arguments, one per line.
    // The answer is <dir>/<name>.result with the code the method would have exited with; <dir>/alive holds
    // the editor's process id, its mode (batch or gui) and the time of its last tick, so scripts can tell
    // the editor is there. Tests run in a batch editor: a visible one adds packages and shows modal dialogs
    // when a variant turns on built-in VR. Window shots need a visible one.
    [InitializeOnLoad]
    public static class AuditTestServer
    {
        const string RunningKey = "AuditTestServer.Running";

        static readonly string dir;
        static string running;
        static string[] commandArgs;
        static double nextPoll;
        static double lastAlive = -1;

        static AuditTestServer()
        {
            dir = CommandLineArg("-auditServer");
            if (dir == null) return;
            Directory.CreateDirectory(dir);
            // A script reload in the middle of a command loses its state: answer it as interrupted.
            var interrupted = SessionState.GetString(RunningKey, "");
            if (interrupted.Length > 0)
            {
                Debug.LogWarning("[AuditTests] Command " + interrupted + " was interrupted by a script reload.");
                Answer(interrupted, 3);
            }
            EditorApplication.update += Tick;
        }

        public static bool Serving { get { return dir != null; } }

        // Arguments of the running command, or the editor's own command line outside the server.
        public static string[] Args { get { return commandArgs ?? Environment.GetCommandLineArgs(); } }

        // Closes the server editor, for a script that needs the other mode.
        public static void Quit()
        {
            EditorApplication.Exit(0);
        }

        // Ends a test entry point: quits a batch editor, answers the command in the server.
        public static void Exit(int code)
        {
            if (running == null) EditorApplication.Exit(code);
            else Answer(running, code);
        }

        static void Tick()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - lastAlive >= 1)
            {
                lastAlive = now;
                try
                {
                    File.WriteAllText(Path.Combine(dir, "alive"), System.Diagnostics.Process.GetCurrentProcess().Id + "\n" +
                        (Application.isBatchMode ? "batch" : "gui") + "\n" + DateTime.UtcNow.ToString("o"));
                }
                catch (IOException)
                {
                    // The script may be reading it right now: the next tick writes it again.
                }
            }
            if (running != null || now < nextPoll) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            nextPoll = now + 0.5;

            var file = Directory.GetFiles(dir, "*.cmd").OrderBy(f => f, StringComparer.Ordinal).FirstOrDefault();
            if (file == null) return;
            // Code edited while the editor stays open is picked up first: the command waits for the reload.
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            var lines = File.ReadAllLines(file);
            File.Delete(file);
            var name = Path.GetFileNameWithoutExtension(file);
            running = name;
            SessionState.SetString(RunningKey, name);
            commandArgs = new[] { "Unity.exe" }.Concat(lines.Skip(1)).ToArray();
            var target = lines.Length > 0 ? lines[0].Trim() : "";
            Debug.Log("[AuditTests] Command " + name + ": " + target + " " + string.Join(" ", lines.Skip(1).ToArray()));
            try
            {
                var method = Find(target);
                if (method == null)
                {
                    Debug.LogError("[AuditTests] No static method " + target);
                    Answer(name, 4);
                    return;
                }
                method.Invoke(null, null);
            }
            catch (Exception e)
            {
                Debug.LogError("[AuditTests] " + target + " crashed: " + (e.InnerException ?? e));
                if (running == name) Answer(name, 2);
            }
        }

        static void Answer(string name, int code)
        {
            File.WriteAllText(Path.Combine(dir, name + ".result"), code.ToString());
            running = null;
            commandArgs = null;
            SessionState.EraseString(RunningKey);
            // A step that changed scripting defines starts a recompile meanwhile; the next poll waits for it.
            nextPoll = EditorApplication.timeSinceStartup + 2;
        }

        static MethodInfo Find(string fullName)
        {
            int dot = fullName.LastIndexOf('.');
            if (dot <= 0) return null;
            var typeName = fullName.Substring(0, dot);
            var methodName = fullName.Substring(dot + 1);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                    return type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }
            return null;
        }

        static string CommandLineArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
