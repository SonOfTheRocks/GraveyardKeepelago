using System;
using FlowCanvas;
using FlowCanvas.Nodes;

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

    public static IDisposable EnterArchipelagoItemApplication()
    {
        _archipelagoItemApplicationDepth++;
        return new ActionScope(() => _archipelagoItemApplicationDepth--);
    }

    public static void MarkFlowChoice(FlowOutput output, ref Flow flow)
    {
        if (_service == null || !(output.parent is Flow_MultiAnswer) ||
            !output.ID.StartsWith("out_", StringComparison.Ordinal) ||
            !int.TryParse(output.ID.Substring(4), out var answerIndex))
            return;

        var suppression = _service.HandleFlowChoice(output.parent.graph.name, output.parent.ID, answerIndex);
        if (suppression == FlowRewardSuppression.None)
            return;

        var inherited = flow.ReadParameter<int>(FlowSuppressionParameter);
        flow.WriteParameter(FlowSuppressionParameter, inherited | (int)suppression);
    }

    public static void HandleFlowNode(FlowNode node, FlowHandler original, Flow flow)
    {
        var suppression = (FlowRewardSuppression)flow.ReadParameter<int>(FlowSuppressionParameter);
        if (_service == null ||
            !_service.HandleFlowNode(node.graph.name, node.ID, node.GetType().Name, suppression))
        {
            original(flow);
            return;
        }

        if (node.GetOutputPort("Out") is FlowOutput output)
        {
            output.Call(flow);
            return;
        }

        // Never strand a flow graph if a future reward node has no conventional output.
        original(flow);
    }

    public static void HandleCraft(string craftID)
    {
        if (!IsApplyingArchipelagoItem)
            _service?.HandleCraft(craftID);
    }

    public static bool HandleTechnology(string technologyID)
        => !IsApplyingArchipelagoItem && _service != null && _service.HandleTechnology(technologyID);

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
