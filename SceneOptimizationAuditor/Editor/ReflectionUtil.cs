using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityTools.SceneOptimization
{
    // Access to package and plugin types by name, so the tool has no package dependencies.
    internal static class ReflectionUtil
    {
        const BindingFlags InstanceAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        public static Type FindType(string fullName)
        {
            Type cached;
            if (typeCache.TryGetValue(fullName, out cached)) return cached;
            Type found = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    found = assembly.GetType(fullName, false);
                }
                catch (Exception)
                {
                    found = null;
                }
                if (found != null) break;
            }
            typeCache[fullName] = found;
            return found;
        }

        // Reads a property or field (public or not) up the type hierarchy; null when absent or failing.
        public static object GetMember(object obj, string name)
        {
            if (obj == null) return null;
            for (var t = obj.GetType(); t != null; t = t.BaseType)
            {
                var property = t.GetProperty(name, InstanceAll | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        return property.GetValue(obj, null);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                var field = t.GetField(name, InstanceAll | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(obj);
            }
            return null;
        }

        // True when the type or one of its bases below UnityEngine.MonoBehaviour declares the method.
        public static bool DeclaresMethod(Type type, string name)
        {
            for (var t = type; t != null && t != typeof(UnityEngine.MonoBehaviour); t = t.BaseType)
            {
                if (t.GetMethod(name, InstanceAll | BindingFlags.DeclaredOnly) != null) return true;
            }
            return false;
        }

        public static MethodInfo FindStaticMethod(Type type, string name, Type[] parameters)
        {
            if (type == null) return null;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                return type.GetMethod(name, flags, null, parameters, null);
            }
            catch (AmbiguousMatchException)
            {
                return null;
            }
        }
    }
}
