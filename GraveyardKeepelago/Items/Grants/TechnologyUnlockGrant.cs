using System;
using System.Collections.Generic;
using System.IO;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class TechnologyUnlockGrant : IItemGrant, IDisposable
{
    private Queue<string> _scripts;
    private GrantFlow _flow;
    public IReadOnlyList<string> TechnologyIDs { get; }

    public TechnologyUnlockGrant(IReadOnlyList<string> technologyIDs) => TechnologyIDs = technologyIDs;

    public GrantResult Apply(GrantContext context)
    {
        var save = context.Save;
        if (_scripts == null)
        {
            _scripts = new Queue<string>();
            foreach (var id in TechnologyIDs)
            {
                if (save.unlocked_techs.Contains(id))
                    continue;

                var tech = GameBalance.me.GetDataOrNull<TechDefinition>(id);
                if (tech == null)
                    throw new InvalidDataException($"Unknown game technology '{id}'.");

                // The technology patch applies the unlock and leaves its flow here to await.
                save.UnlockTech(id);
                if (!save.unlocked_techs.Contains(id))
                    throw new InvalidDataException($"Could not unlock AP technology '{id}'.");
                if (!string.IsNullOrEmpty(tech.flowscript))
                    _scripts.Enqueue(tech.flowscript);
            }
        }

        while (_scripts.Count > 0)
        {
            _flow ??= new GrantFlow();
            if (!_flow.TryComplete(_scripts.Peek()))
                return GrantResult.Pending;
            _flow.Dispose();
            _flow = null;
            _scripts.Dequeue();
        }

        return GrantResult.Applied;
    }

    public void Dispose() => _flow?.Dispose();
}
