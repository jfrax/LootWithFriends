using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LootWithFriends
{

    public static class ReflectionDumper
    {
        private const int MaxDepth = 4;

        public static void DumpObject(object obj, string fileName)
        {
            var sb = new StringBuilder();
            DumpObjectInternal(obj, sb, 0, new HashSet<object>());
            File.WriteAllText(Path.Combine(
                Application.persistentDataPath,
                fileName), sb.ToString());
        }

        private static void DumpObjectInternal(
            object obj,
            StringBuilder sb,
            int depth,
            HashSet<object> visited)
        {
            if (obj == null || depth > MaxDepth)
                return;

            if (visited.Contains(obj))
                return;

            visited.Add(obj);

            Type type = obj.GetType();
            Indent(sb, depth);
            sb.AppendLine($"[{type.FullName}]");

            // Fields
            foreach (var field in type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object value;
                try { value = field.GetValue(obj); }
                catch { continue; }

                Indent(sb, depth + 1);
                sb.AppendLine($"Field {field.Name} = {FormatValue(value)}");

                if (ShouldRecurse(value))
                    DumpObjectInternal(value, sb, depth + 2, visited);
            }

            // Properties
            foreach (var prop in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;

                object value;
                try { value = prop.GetValue(obj, null); }
                catch { continue; }

                Indent(sb, depth + 1);
                sb.AppendLine($"Prop {prop.Name} = {FormatValue(value)}");

                if (ShouldRecurse(value))
                    DumpObjectInternal(value, sb, depth + 2, visited);
            }
        }

        private static bool ShouldRecurse(object value)
        {
            if (value == null)
                return false;

            Type t = value.GetType();
            if (t.IsPrimitive || t == typeof(string))
                return false;

            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                return false;

            return true;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "null";
            return value.ToString();
        }

        private static void Indent(StringBuilder sb, int depth)
        {
            sb.Append(' ', depth * 2);
        }
    }
}
