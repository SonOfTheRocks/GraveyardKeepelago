using System;
using System.Linq;
using System.Reflection;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.GameModifications.Patches
{

    public class LogParam
    {
        public readonly string Name;
        public readonly object Value;

        public LogParam(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
    
    public abstract class BasePatch
    {
        protected static ILogger Logger;

        public static void Initialize(ILogger logger)
        {
            Logger = logger;
        }
        
        // Thread-local indentation for nested calls
        [ThreadStatic]
        private static int _depth;

        protected static void LogBefore(MethodBase method, LogParam[] args = null)
        {
            if (Logger == null)
                return;

            var indent = new string(' ', _depth * 2);
            var argText = FormatArgs(args);

            Logger.LogDebug($"{indent}-> {method.DeclaringType?.Name}.{method.Name}({argText})");

            _depth++;
        }

        protected static void LogAfter(MethodBase method, object result = null)
        {
            if (Logger == null)
                return;

            _depth--;
            
            var indent = new string(' ', _depth * 2);

            if (method is MethodInfo mi && mi.ReturnType != typeof(void))
                Logger.LogDebug($"{indent}<- {method.DeclaringType?.Name}.{method.Name} returned {result}");
            else
                Logger.LogDebug($"{indent}<- {method.DeclaringType?.Name}.{method.Name} completed");
        }

        private static string FormatArgs(LogParam[] args)
        {
            if (args == null || args.Length == 0)
                return "";

            return string.Join(", ", args.Select(arg => $"{arg.Name} = {arg.Value ?? "null"}"));
        }

    }

}