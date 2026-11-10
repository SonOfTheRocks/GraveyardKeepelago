using FlowCanvas;
using FlowCanvas.Nodes;
using GraveyardKeepelago.Technologies;
using HarmonyLib;

namespace GraveyardKeepelago.GameModifications.Patches;

[HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.AttachFlowScript))]
public static class TutorialBedAccessPatch
{
    private static void Prefix(WorldGameObject __instance, ref string flowscript_name) =>
        flowscript_name = TutorialAccessPolicy.ResolveBedScript(TechnologyRuntime.Current?.IsActive == true,
            __instance.obj_id, flowscript_name);
}

public static class TutorialDoorAccess
{
    public static bool TryHandle(string graphName, FlowNode node, Flow flow)
    {
        if (!(node is Flow_PlayerFlag flag)) return false;
        var active = TechnologyRuntime.Current?.IsActive == true;
        var parameter = (flag.GetInputPort("param") as ValueInput<string>)?.value;
        string output;
        if (TutorialAccessPolicy.OpenCellarDoor(active, graphName, parameter))
            output = ">= 1";
        else
        {
            var game = MainGame.me;
            if (!active || graphName != "Teleport" || parameter != "lock_tp" ||
                game?.save?.quests == null || game.player == null ||
                !TutorialAccessPolicy.BypassHomeExitLock(active, graphName, parameter,
                    game.player.data.GetParam("lock_tp_param"),
                    game.save.quests.IsQuestSucced("get_out_from_house"),
                    game.save.quests.IsQuestSucced("go_to_talk_with_donkey_first_time")))
                return false;
            output = "== 0";
        }

        var allowed = flag.GetOutputPort(output) as FlowOutput;
        if (allowed == null) return false;
        allowed.Call(flow);
        return true;
    }
}
