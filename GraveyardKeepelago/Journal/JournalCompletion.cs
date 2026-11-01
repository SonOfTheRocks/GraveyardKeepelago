using System;
using System.Collections.Generic;
using System.Linq;

namespace GraveyardKeepelago.Journal;

public sealed class JournalCompletionSuggestion
{
    public string Value { get; }
    public string Description { get; }
    public JournalCompletionSuggestion(string value, string description)
    { Value = value; Description = description; }
}

// Server commands verified against MultiServer.ClientMessageProcessor. No command is executed here.
public sealed class JournalCompletion
{
    private static readonly Dictionary<string, string> Commands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["!help"] = "List server commands",
        ["!hint"] = "<item name> — Find an item; may cost hint points",
        ["!hint_location"] = "<location name> — Hint for a location",
        ["!missing"] = "[location filter] — List missing checks",
        ["!checked"] = "[location filter] — List completed checks",
        ["!players"] = "List connected and missing players",
        ["!status"] = "[tag] — Team status",
        ["!remaining"] = "List remaining items; server rules apply",
        ["!countdown"] = "[seconds] — Start a countdown",
        ["!alias"] = "[name] — Set or clear your display name",
        ["!release"] = "Send remaining items in your world; server rules apply",
        ["!collect"] = "Collect your remaining items; server rules apply",
        ["!getitem"] = "<item name> — Cheat an item; server permission required",
        ["!options"] = "Show server options",
        ["!license"] = "Show license information",
    };
    private readonly string[] _items, _locations;

    public JournalCompletion(IEnumerable<string> items, IEnumerable<string> locations)
    { _items = Names(items); _locations = Names(locations); }

    private static string[] Names(IEnumerable<string> names) => (names ?? Array.Empty<string>())
        .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

    public bool HasParameterSuggestions(string acceptedValue) => acceptedValue != null &&
        acceptedValue.EndsWith(" ", StringComparison.Ordinal) &&
        Suggest(acceptedValue, acceptedValue.Length, acceptedValue.Length, acceptedValue.Length).Length > 0;

    public JournalCompletionSuggestion[] Suggest(string text, int cursor, int selectionStart, int selectionEnd)
    {
        text ??= string.Empty;
        if (cursor != text.Length || selectionStart != selectionEnd || !text.StartsWith("!"))
            return Array.Empty<JournalCompletionSuggestion>();
        var split = text.IndexOf(' ');
        if (split < 0)
            return Commands.Select(command => new { command, rank = Rank(command.Key, text) })
                .Where(match => match.rank < 3).OrderBy(match => match.rank)
                .ThenBy(match => match.command.Key, StringComparer.OrdinalIgnoreCase)
                .Select(match => new JournalCompletionSuggestion(match.command.Key + " ", match.command.Value)).ToArray();
        var commandName = text.Substring(0, split).ToLowerInvariant();
        var query = text.Substring(split + 1).Trim();
        var terms = query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        var names = commandName is "!hint" or "!getitem" ? _items :
            commandName is "!hint_location" or "!missing" or "!checked" ? _locations : Array.Empty<string>();
        return names.Select(name => new { name, rank = ArgumentRank(name, query, terms) }).Where(match => match.rank < 3)
            .OrderBy(match => match.rank).ThenBy(match => match.name, StringComparer.OrdinalIgnoreCase)
            .Select(match => new JournalCompletionSuggestion(commandName + " " + match.name, match.name)).ToArray();
    }

    private static int ArgumentRank(string candidate, string query, string[] terms)
    {
        var rank = Rank(candidate, query);
        if (rank < 2 || terms.Length < 2) return rank;
        var start = 0;
        foreach (var term in terms)
        {
            var match = candidate.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (match < 0) return rank;
            start = match + term.Length;
        }
        // Let "Create: bow" find "Create: A bowl of ..." without changing its canonical name.
        // Contiguous prefix/substring matches still come first; command matching is unchanged.
        return 2;
    }

    private static int Rank(string candidate, string query)
    {
        if (candidate.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 0;
        if (candidate.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return 1;
        // One mistyped character is enough for useful recovery without turning unrelated chat into commands.
        if (query.Length >= 4 && Distance(candidate.ToLowerInvariant(), query.ToLowerInvariant()) <= 1) return 2;
        return 3;
    }

    private static int Distance(string a, string b)
    {
        if (Math.Abs(a.Length - b.Length) > 1) return 2;
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++) current[j] = Math.Min(Math.Min(current[j - 1] + 1,
                previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            previous = current;
        }
        return previous[b.Length];
    }
}
