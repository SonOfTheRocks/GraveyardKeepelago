using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using FlowCanvas;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using ILogger = KaitoKid.Utilities.Interfaces.ILogger;

namespace GraveyardKeepelago.Utilities
{
    public class DumpUtils
    {
        public static void DumpGameData(ILogger logger)
        {

            var gb = GameBalance.me;
            Directory.CreateDirectory("game_data dumps");
            var fieldNames = typeof(GameBalance).GetFields().Select(field => field.Name).ToList();
            foreach (var fieldName in fieldNames.Distinct())
            {
                var filePath = "game_data dumps\\" + fieldName + ".json";
                var path = Path.GetFullPath(filePath);
                logger.LogInfo($"Dumping {fieldName}  to {filePath}");

                var value = typeof(GameBalance).GetField(fieldName).GetValue(gb);
                if (!value.GetType().IsSerializable)
                    logger.LogError($"Field {fieldName} is not serializable");
                else
                {
                    //var serializedValue = JsonUtility.ToJson(value, true);
                    
                    var settings = new JsonSerializerSettings{ ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                    var serializedValue = JsonConvert.SerializeObject(value, settings);
                    
                    logger.LogInfo($"Successfully serialized {serializedValue.Length} characters, writing file");
                    File.WriteAllText(path, serializedValue);
                }
            }

            logger.LogInfo($"Dumping finished");
        }

        public static void DumpPlayerRes(ILogger logger)
        {
            var GRparams = Traverse.Create(MainGame.me.player.data).Field("_params").GetValue<GameRes>();
            var res_types = Traverse.Create(GRparams).Field("_res_type").GetValue<List<string>>();
            var res_values = Traverse.Create(GRparams).Field("_res_v").GetValue<List<float>>();
            
            var dict = res_types
                .Select((key, index) => new {key, value = res_values[index]})
                .ToDictionary(x => x.key, x => x.value);
            
            logger.LogInfo($"Dumping player res");
            foreach (var kvp in dict)
                logger.LogInfo($"\t{kvp.Key}: {kvp.Value}");
        }

        public static void DumpAllFlowScripts(ILogger logger)
        {
            string exportPath = Path.Combine(Paths.BepInExRootPath, "FlowGraphExports");
            Directory.CreateDirectory(exportPath);

            FlowScript[] scripts = Resources.LoadAll<FlowScript>("FlowCanvas");

            logger.LogInfo($"Found {scripts.Length} FlowScripts");

            foreach (var fs in scripts)
                DumpFlowScript(logger, fs);
        }

        public static void DumpFlowScript(ILogger logger, FlowScript fs)
        {
            string exportPath = Path.Combine(Paths.BepInExRootPath, "FlowGraphExports");
            Directory.CreateDirectory(exportPath);

            string json = fs.Serialize(true, null);
            string filePath = Path.Combine(exportPath, fs.name + ".json");
            File.WriteAllText(filePath, json);
            logger.LogInfo($"Exported FlowGraph: {fs.name} → {filePath}");
        }
    }
}