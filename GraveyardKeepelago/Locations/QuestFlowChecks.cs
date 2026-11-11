using FlowCanvas;

namespace GraveyardKeepelago.Locations;

internal sealed class QuestFlowChecks
{
    private const string RouteParameter = "graveyard_keepelago_quest_flow_route";
    private volatile Slot _slot;

    public void StartSlot(LocationTriggerService service) => _slot = new Slot(service);

    public void Clear() => _slot = null;

    public FlowHandler Wrap(FlowHandler current, FlowHandler vanilla, FlowHandler continuation, string graph,
        int sourceNodeID, int? answerIndex, int targetNodeID, string targetNodeType)
    {
        // FlowCanvas binds an active output to its input's handler. A wrapped or inactive
        // output no longer has that handler, so graph restarts cannot stack wrappers.
        if (vanilla == null || current != vanilla)
            return current;

        return flow =>
        {
            var slot = _slot;
            if (slot == null)
            {
                vanilla(flow);
                return;
            }

            var route = flow.ReadParameter<Route>(RouteParameter);
            if (route.SlotToken != null && route.SlotToken != slot.Token)
            {
                vanilla(flow);
                return;
            }

            var suppression = route.Suppression;
            if (answerIndex is int answer)
                suppression |= slot.Service.HandleFlowChoice(graph, sourceNodeID, answer);

            if (route.SlotToken == null || suppression != route.Suppression)
                flow.WriteParameter(RouteParameter, new Route(slot.Token, suppression));

            if (continuation != null && slot.Service.HandleFlowNode(graph, targetNodeID, targetNodeType, suppression))
                continuation(flow);
            else
                vanilla(flow);
        };
    }

    private sealed class Slot
    {
        public Slot(LocationTriggerService service) => Service = service;
        public LocationTriggerService Service { get; }
        public object Token { get; } = new object();
    }

    private readonly struct Route
    {
        public Route(object slotToken, FlowRewardSuppression suppression)
        {
            SlotToken = slotToken;
            Suppression = suppression;
        }

        public object SlotToken { get; }
        public FlowRewardSuppression Suppression { get; }
    }
}
