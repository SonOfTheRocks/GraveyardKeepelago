using System;
using System.Collections.Generic;
using UnityEngine;
using ILogger = KaitoKid.Utilities.Interfaces.ILogger;

namespace GraveyardKeepelago.GameModifications
{
    public static class KeyDispatcher
    {
        private static ILogger _logger;
        private static readonly Dictionary<KeyCode, Action> _bindings = new();

        public static void Initialize(ILogger logger)
        {
            _logger = logger;
        }

        public static void Register(KeyCode key, Action action)
        {
            if (_bindings.ContainsKey(key))
            {
                var alreadyRegisteredAction = _bindings[key].Method.Name;
                _logger.LogError($"Key '{key}' already has a registered action ({alreadyRegisteredAction})!");
                return;
            }
            
            _bindings[key] = action;
        }

        public static void Unregister(KeyCode key)
        {
            _bindings.Remove(key);
        }

        public static void Update()
        {
            // use a copy, because the action might trigger an unregister and therefore changes the collection
            var bindingsCopy = new List<KeyValuePair<KeyCode, Action>>(_bindings);
            foreach (var binding in bindingsCopy)
            {
                if (!Input.GetKeyDown(binding.Key))
                    continue;
                
                try
                {
                    _logger.LogDebug($"Key '{binding.Key}' was pressed, executing {binding.Value?.Method.Name}");
                    binding.Value?.Invoke();
                }
                catch (Exception e)
                {
                    _logger.LogInfo($"Error while executing action for key '{binding.Key}': {e}");
                }
            }
        }
    }
}