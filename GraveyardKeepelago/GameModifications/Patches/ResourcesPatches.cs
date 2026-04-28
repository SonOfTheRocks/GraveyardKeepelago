using System;
using System.Reflection;
using DungeonGenerator;
using Fishing;
using FlowCanvas;
using GraveyardKeepelago.Utilities;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    //namespace UnityEngine (In DLLExplorer go from something like EnvironmentPreset.Load)
    //public static Object Load(string path, Type systemTypeInstance)
    [HarmonyPatch(typeof(Resources), nameof(Resources.Load), typeof(string), typeof(Type))]
    public class ResourcesLoadPatch : BasePatch
    {
        private static bool Prefix(string path, MethodBase __originalMethod)
        {
            // No log to avoid spam
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(string path, Object __result, MethodBase __originalMethod)
        {
            if (__result == null)
            {
                // No log to avoid spam
                return;
            }

            return;
            switch (__result)
            {
                case FlowScript fs:
                    //DumpUtils.DumpFlowScript(Logger, fs);
                    break;
                case MobSpawner ms:
                    Logger.LogInfo($"Loaded MobSpawner {ms.name}[{ms.spawner_id}] from {path}");
                    break;
                
                case WorldSimpleObject wso:
                    Logger.LogInfo($"Loaded {wso.wso_type} WSO from {path}");
                    break;
                
                case GJL gjl:
                    Logger.LogInfo($"Loaded GJL {gjl.name} with id {gjl.id} from path {path}");
                    break;
                
                case TextAsset ta:
                    Logger.LogInfo($"Loaded TextAsset {ta.text} from path {path}");
                    break;
                
                case ProjectileObjectPart pop:
                    Logger.LogInfo($"Loaded ProjectileObjectPart from path {path}");
                    break;
                
                // Standard logging
                case ResourceFileList:
                case SpriteAtlas:
                case Tileset:
                case DungeonRoomInterior:
                case DungeonRoomsContainer:
                case DungeonPreset:
                case EasySpriteCollectionSub:
                case EasySpritesCollection:
                case EasySpriteCollectionAtlases:
                case FishingRodPreset:
                case FishPreset:
                case GameBalance:
                case TrailDefinition:
                    var name = (__result as UnityEngine.Object)?.name ?? "<no name>";
                    Logger.LogInfo($"Loaded {__result.GetType().Name} {name} from path {path}");
                    break;

                // Ignore
                case EnvironmentPreset:
                case UIFont:
                case GameObject:
                case Texture2D:
                case TrailObject:
                case FogObject:
                case MovementCurve:
                case WGOMark:
                case TechPointsDrop:
                case PuffFX:
                case SkinPreset:
                case SmartWeatherSettings:
                case WeatherPreset:
                    break;
                
                default:
                    Logger.LogError($"Unknown type of loaded resource: {__result.GetType().Name}");
                    break;
            }
            
            // No log to avoid spam
            
            /*
            var settings = new JsonSerializerSettings{ ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            var serializedValue = JsonConvert.SerializeObject(__result, settings);
            Logger.LogDebug(serializedValue);
            
            LogAfter(__originalMethod);
            */
        }
    }
}