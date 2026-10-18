using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SCP096TheShyGuy
{
    internal static class Reflect
    {
        internal const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        internal static Type GameType(string name) => Type.GetType(name + ", Assembly-CSharp");

        internal static object Member(object obj, string name)
        {
            if (obj == null) return null;
            Type type = obj is Type ? (Type)obj : obj.GetType();
            object receiver = obj is Type ? null : obj;
            for (Type t = type; t != null; t = t.BaseType)
            {
                var f = t.GetField(name, Any);
                if (f != null) return f.GetValue(receiver);
                var p = t.GetProperty(name, Any);
                if (p != null && p.GetIndexParameters().Length == 0)
                {
                    try { return p.GetValue(receiver, null); }
                    catch { /* getter not safe, ignore */ }
                }
            }
            return null;
        }

        internal static bool Set(object obj, string name, object value)
        {
            if (obj == null) return false;
            Type type = obj is Type ? (Type)obj : obj.GetType();
            object receiver = obj is Type ? null : obj;
            for (Type t = type; t != null; t = t.BaseType)
            {
                var f = t.GetField(name, Any);
                if (f != null && !f.IsInitOnly && Compatible(f.FieldType, value))
                { f.SetValue(receiver, value); return true; }
                var p = t.GetProperty(name, Any);
                if (p != null && p.CanWrite && Compatible(p.PropertyType, value))
                { p.SetValue(receiver, value, null); return true; }
            }
            return false;
        }

        private static bool Compatible(Type type, object val) => val == null
            ? !type.IsValueType || Nullable.GetUnderlyingType(type) != null
            : type.IsInstanceOfType(val);

        internal static MethodInfo Find(object obj, string name, Func<ParameterInfo[], bool> args)
        {
            if (obj == null) return null;
            Type type = obj is Type ? (Type)obj : obj.GetType();
            return type.GetMethods(Any).FirstOrDefault(m => m.Name == name && args(m.GetParameters()));
        }

        internal static object Call(object obj, MethodInfo method, params object[] args)
        {
            if (method == null) return null;
            return method.Invoke(obj is Type ? null : obj, args);
        }

        internal static IEnumerable Enumerate(object obj) => obj as IEnumerable;

        internal static string Signature(MethodInfo m)
        {
            return m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name).ToArray()) + ")";
        }
    }
}
