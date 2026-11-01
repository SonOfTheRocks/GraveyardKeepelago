using System;
using FlowCanvas;

namespace GraveyardKeepelago.Locations;

public static class LocationTriggerRuntime
{
    private const string FlowSuppressionParameter = "graveyard_keepelago_reward_suppression";
    private const string ArchipelagoItemParameter = "graveyard_keepelago_archipelago_item";
    private static LocationTriggerService _service;
    private static int _archipelagoItemApplicationDepth;

    public static bool IsApplyingArchipelagoItem => _archipelagoItemApplicationDepth > 0;

    public static void Initialize(LocationTriggerService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public static void Clear() => _service = null;

    public static IDisposable EnterArchipelagoItemApplication()
    {
        _archipelagoItemApplicationDepth++;
        return new ActionScope(() => _archipelagoItemApplicationDepth--);
    }

    public static void MarkFlowChoice(string graphName, int nodeID, int? answerIndex, ref Flow flow)
    {
        if (_service == null || string.IsNullOrEmpty(graphName) || nodeID < 0 || answerIndex == null)
            return;

        var suppression = _service.HandleFlowChoice(graphName, nodeID, answerIndex.Value);
        if (suppression == FlowRewardSuppression.None)
            return;

        var inherited = flow.ReadParameter<int>(FlowSuppressionParameter);
        flow.WriteParameter(FlowSuppressionParameter, inherited | (int)suppression);
    }

    public static void HandleFlowNode(string graphName, int nodeID, string nodeType, FlowOutput continuation,
        FlowHandler original, Flow flow)
    {
        var suppression = (FlowRewardSuppression)flow.ReadParameter<int>(FlowSuppressionParameter);
        if (_service == null || string.IsNullOrEmpty(graphName) || nodeID < 0 || string.IsNullOrEmpty(nodeType) ||
            !_service.HandleFlowNode(graphName, nodeID, nodeType, suppression))
        {
            original(flow);
            return;
        }

        if (continuation != null)
        {
            continuation.Call(flow);
            return;
        }

        // Never strand a flow graph if a future reward node has no conventional output.
        original(flow);
    }

    public static void HandleCraftCompletion(string craftID)
    {
        if (!IsApplyingArchipelagoItem)
            _service?.HandleCraftCompletion(craftID);
    }

    public static bool HandleCraftUnlock(string craftID)
        => !IsApplyingArchipelagoItem && _service != null && _service.HandleCraftUnlock(craftID);

    public static bool ShouldSuppressCraftReveal(System.Collections.Generic.IEnumerable<string> craftIDs)
        => !IsApplyingArchipelagoItem && _service != null && _service.ShouldSuppressCraftReveal(craftIDs);

    public static bool HandleTechnology(string technologyID)
        => !IsApplyingArchipelagoItem && _service != null && _service.HandleTechnology(technologyID);

    public static bool HandleTechnologyDialog(string technologyID, bool forcedUnlock, bool revealTech, bool pseudotech)
        => !IsApplyingArchipelagoItem && _service != null &&
           _service.HandleTechnologyDialog(technologyID, forcedUnlock, revealTech, pseudotech);

    public static bool HandleWorldItem(string itemID)
        => !IsApplyingArchipelagoItem && _service != null && _service.HandleWorldItem(itemID);

    public static void MarkArchipelagoItem(Item item) => item?.SetParam(ArchipelagoItemParameter, 1f);

    public static bool IsArchipelagoItem(Item item)
        => item != null && item.GetParamInt(ArchipelagoItemParameter) != 0;

    private sealed class ActionScope : IDisposable
    {
        private Action _onDispose;

        public ActionScope(Action onDispose) => _onDispose = onDispose;

        public void Dispose()
        {
            var action = _onDispose;
            _onDispose = null;
            action?.Invoke();
        }
    }
}
