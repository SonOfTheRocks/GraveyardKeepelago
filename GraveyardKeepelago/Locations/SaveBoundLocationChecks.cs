using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.Progression;

namespace GraveyardKeepelago.Locations;

internal sealed class SaveBoundLocationChecks : ILocationChecker
{
    internal const string RecordPrefix = SaveProgress.RecordPrefix;

    private readonly IReadOnlyDictionary<string, long> _activeLocations;
    private Action<long[]> _send;

    internal SaveProgress Progress { get; }

    private SaveBoundLocationChecks(SaveProgress progress, IReadOnlyDictionary<string, long> activeLocations)
    {
        Progress = progress;
        _activeLocations = activeLocations;
    }

    public static SaveBoundLocationChecks Bind(IList<string> records, (string Seed, int Team, int Slot) identity,
        IReadOnlyDictionary<string, long> activeLocations, bool newSave)
        => new(SaveProgress.Bind(records, identity, newSave), activeLocations);

    public bool Connect((string Seed, int Team, int Slot) identity, Action<long[]> send)
    {
        Disconnect();
        if (Progress.Identity != identity)
            return false;

        _send = send;
        Send(Progress.Data.Checks.Where(_activeLocations.ContainsKey));
        return true;
    }

    public void Disconnect() => _send = null;

    public void AddCheckedLocation(string locationName) => AddCheckedLocations(new[] { locationName });

    public void AddCheckedLocations(string[] locationNames)
    {
        var checks = Progress.Data.Checks;
        var added = locationNames.Where(name => name != null && _activeLocations.ContainsKey(name) &&
            !checks.Contains(name)).Distinct(StringComparer.Ordinal).ToArray();
        if (added.Length == 0)
            return;

        checks.UnionWith(added);
        try
        {
            Progress.Store();
        }
        catch
        {
            checks.ExceptWith(added);
            throw;
        }
        Send(added);
    }

    private void Send(IEnumerable<string> names)
    {
        if (_send == null)
            return;

        var ids = names.Select(name => _activeLocations[name]).OrderBy(id => id).ToArray();
        if (ids.Length > 0)
            _send(ids);
    }
}
