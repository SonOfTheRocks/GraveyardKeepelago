using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using FlowCanvas;
using FlowCanvas.Macros;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodeCanvas.Framework;
using UnityEngine;
using ILogger = KaitoKid.Utilities.Interfaces.ILogger;

namespace GraveyardKeepelago.Utilities
{
    public class DumpUtils
    {
        private sealed class PublicFieldsContractResolver : DefaultContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                return objectType
                    .GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Cast<MemberInfo>()
                    .ToList();
            }
        }

        public static void DumpGameData(ILogger logger)
        {
            DumpGameData(logger, GameBalance.me, "game_data dumps");
        }

        public static void DumpGameData(ILogger logger, GameBalance gameBalance, string exportDirectory)
        {
            Directory.CreateDirectory(exportDirectory);
            var fieldNames = typeof(GameBalance).GetFields().Select(field => field.Name).ToList();
            foreach (var fieldName in fieldNames.Distinct())
            {
                var filePath = Path.Combine(exportDirectory, fieldName + ".json");
                var path = Path.GetFullPath(filePath);
                logger.LogDebug($"Dumping {fieldName}  to {filePath}");

                try
                {
                    var value = typeof(GameBalance).GetField(fieldName).GetValue(gameBalance);
                    if (value == null)
                    {
                        File.WriteAllText(path, "null");
                        continue;
                    }

                    if (!value.GetType().IsSerializable)
                    {
                        logger.LogError($"Field {fieldName} is not serializable");
                        continue;
                    }

                    //var serializedValue = JsonUtility.ToJson(value, true);
                    var settings = new JsonSerializerSettings
                    {
                        ContractResolver = new PublicFieldsContractResolver(),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    };
                    var serializedValue = JsonConvert.SerializeObject(value, settings);

                    logger.LogDebug($"Successfully serialized {serializedValue.Length} characters, writing file");
                    File.WriteAllText(path, serializedValue);
                }
                catch (Exception exception)
                {
                    logger.LogError($"Could not dump GameBalance field {fieldName}: {exception}");
                }
            }

            logger.LogDebug($"Dumping finished");
        }

        public static void DumpPlayerRes(ILogger logger)
        {
            var GRparams = Traverse.Create(MainGame.me.player.data).Field("_params").GetValue<GameRes>();
            var res_types = Traverse.Create(GRparams).Field("_res_type").GetValue<List<string>>();
            var res_values = Traverse.Create(GRparams).Field("_res_v").GetValue<List<float>>();
            
            var dict = res_types
                .Select((key, index) => new {key, value = res_values[index]})
                .ToDictionary(x => x.key, x => x.value);
            
            logger.LogDebug($"Dumping player res");
            foreach (var kvp in dict)
                logger.LogDebug($"\t{kvp.Key}: {kvp.Value}");
        }

        public static void DumpAllFlowScripts(ILogger logger)
        {
            string exportPath = Path.Combine(Paths.BepInExRootPath, "FlowGraphExports");
            Directory.CreateDirectory(exportPath);

            FlowScript[] scripts = Resources.LoadAll<FlowScript>("FlowCanvas");
            Macro[] macros = Resources.LoadAll<Macro>("FlowMacros");

            logger.LogDebug($"Found {scripts.Length} FlowScripts and {macros.Length} FlowMacros");

            foreach (var fs in scripts)
                DumpFlowScript(logger, fs);

            foreach (var macro in macros)
                DumpFlowMacro(logger, macro);
        }

        public static void DumpFlowScript(ILogger logger, FlowScript fs)
        {
            DumpFlowGraph(logger, fs, "FlowScript");
        }

        public static void DumpFlowMacro(ILogger logger, Macro macro)
        {
            DumpFlowGraph(logger, macro, "FlowMacro");
        }

        private static void DumpFlowGraph(ILogger logger, Graph graph, string graphKind)
        {
            string exportPath = Path.Combine(Paths.BepInExRootPath, "FlowGraphExports");
            Directory.CreateDirectory(exportPath);

            // Graph.Serialize() calls UpdateNodeIDs(true), which reorders the graph and
            // assigns traversal-based IDs. Capture the IDs assigned by deserialization
            // first: those are the IDs FlowCanvas uses in a normal, non-dump game run.
            var runtimeNodeIDs = graph.allNodes.ToDictionary(node => node, node => node.ID);
            var objectReferences = new List<UnityEngine.Object>();
            string json = graph.Serialize(true, objectReferences);
            var serializedGraph = JObject.Parse(json);
            var serializedNodes = (JArray)serializedGraph["nodes"];
            for (var index = 0; index < graph.allNodes.Count; index++)
            {
                if (serializedNodes[index] is JObject serializedNode)
                    serializedNode["_runtimeNodeId"] = runtimeNodeIDs[graph.allNodes[index]];
            }
            var serializedReferences = new JArray();

            for (var index = 0; index < objectReferences.Count; index++)
            {
                var objectReference = objectReferences[index];
                serializedReferences.Add(objectReference == null
                    ? JValue.CreateNull()
                    : new JObject
                    {
                        ["index"] = index,
                        ["name"] = objectReference.name,
                        ["type"] = objectReference.GetType().FullName
                    });
            }

            serializedGraph["_unityObjectReferences"] = serializedReferences;
            serializedGraph["_graphKind"] = graphKind;

            var fileName = graphKind == "FlowMacro"
                ? "macro__" + graph.name + ".json"
                : graph.name + ".json";
            string filePath = Path.Combine(exportPath, fileName);
            File.WriteAllText(filePath, serializedGraph.ToString(Formatting.Indented));
            logger.LogDebug($"Exported {graphKind}: {graph.name} → {filePath}");
        }
    }
}
