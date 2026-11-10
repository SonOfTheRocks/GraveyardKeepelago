using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Progression;
using HarmonyLib;
using ILogger = KaitoKid.Utilities.Interfaces.ILogger;
using UnityEngine;

namespace GraveyardKeepelago.Technologies;

public sealed class TechnologyRuntime : IDisposable, IUnlockTarget
{
    public static TechnologyRuntime Current { get; private set; }
    private readonly GameSave _save;
    private readonly GKArchipelagoClient _client;
    private readonly ILocationChecker _checker;
    private readonly ILogger _logger;
    private readonly TechnologyCatalog _catalog;
    private readonly HashSet<string> _locations;
    private readonly Dictionary<string, ClientContractItem> _items;
    private readonly ConditionalWeakTable<TechUnlock, PreviewLocation> _previewUnlocks = new();
    private readonly Dictionary<CraftDefinition, bool> _originalRecipeLocks = new();
    private readonly Dictionary<ObjectDefinition, bool> _originalWorkLocks = new();
    private readonly RewardPreviewCache _previews;
    private readonly TechnologyResearch _research;
    private Task<IReadOnlyList<RewardPreview>> _previewRequest;
    private bool _previewsLoaded;
    private float _requestStartedAt;
    private float _retryAt;
    private int _grantDepth;
    private TechDefinition _dialogTechnology;
    public SaveProgress Progress { get; }
    public UnlockPolicy Unlocks { get; }
    public bool IsActive => ReferenceEquals(MainGame.me?.save, _save);

    public TechnologyRuntime(GameSave save, GKArchipelagoClient client, ILocationChecker checker,
        ClientContract contract, TechnologyCatalog catalog, SaveProgress progress, ILogger logger)
    {
        _save = save; _client = client; _checker = checker; _logger = logger; _catalog = catalog; Progress = progress;
        _locations = new HashSet<string>(client.GetAllLocations().Keys, StringComparer.Ordinal);
        _items = contract.Items.Values.ToDictionary(item => item.Name);
        Unlocks = new UnlockPolicy(contract.Items.Values.Where(item => client.SlotData.DLCs.IsContentEnabled(item.DLC)), progress);
        _previews = new RewardPreviewCache(new ServerRewardPreviewSource(client));
        _research = new TechnologyResearch(catalog, _locations, save.unlocked_techs, ApplyWorldEffects, checker.AddCheckedLocations);
        foreach (var craft in GameBalance.me.craft_data.Cast<CraftDefinition>().Concat(GameBalance.me.craft_obj_data))
            if (Unlocks.ControlledCrafts.Contains(craft.id))
            {
                _originalRecipeLocks.Add(craft, craft.needs_unlock);
                craft.needs_unlock = true;
            }
        foreach (var definition in GameBalance.me.objs_data)
        {
            var group = definition.object_groups.FirstOrDefault()?.id;
            if (group == null || !Unlocks.ControlledWorks.Contains(group)) continue;
            _originalWorkLocks.Add(definition, definition.need_unlock_work);
            definition.need_unlock_work = true;
        }
        _save.unlocked_crafts.RemoveAll(Unlocks.CraftLocked);
        _save.unlocked_works.RemoveAll(Unlocks.WorkLocked);
        foreach (var id in progress.Crafts) if (!_save.unlocked_crafts.Contains(id)) _save.unlocked_crafts.Add(id);
        foreach (var id in progress.Works) if (!_save.unlocked_works.Contains(id)) _save.unlocked_works.Add(id);
        Current = this;
        Store();
    }

    public void Store() => Progress.Store(_save.unlocked_phrases);
    public bool CanUnlockCraft(string id) => _grantDepth > 0 || !Unlocks.CraftLocked(id);
    public bool CanUnlockPerk(string id) => _grantDepth > 0 || !Unlocks.PerkLocked(id);

    public void ApplyUnlocks(List<(GameModifications.PlayerUtilities.UnlockType, string)> unlocks)
    {
        _grantDepth++;
        try { Unlocks.Apply(unlocks, this); Store(); }
        finally { _grantDepth--; }
    }

    public void UnlockCraft(string id) => _save.UnlockCraft(id);
    public void UnlockWork(string id) { if (!_save.unlocked_works.Contains(id)) _save.unlocked_works.Add(id); }
    public void UnlockPerk(string id) => _save.UnlockPerk(id);
    public void UnlockPhrase(string id) => _save.UnlockPhrase(id);

    public bool Research(string id)
    {
        var handled = _research.Research(id);
        if (handled) Store();
        return handled;
    }

    public void CompleteQuestTechnology(string id)
    {
        if (_research.Complete(id)) Store();
    }

    public void RevealTechnology(string id)
    {
        var tech = GameBalance.me.GetData<TechDefinition>(id);
        if (tech.hidden) _save.RevealHiddenTech(id);
        if (tech.invisible) _save.MakeVisibleInvisibleTech(id);
    }

    private void ApplyWorldEffects(string id)
    {
        var tech = GameBalance.me.GetData<TechDefinition>(id);
        if (tech.hidden) _save.RevealHiddenTech(id);
        if (tech.invisible) _save.MakeVisibleInvisibleTech(id);
        foreach (var craft in tech.crafts)
            if (!Unlocks.ControlledCrafts.Contains(UnlockPolicy.Canonical(craft))) _save.UnlockCraft(UnlockPolicy.Canonical(craft));
        foreach (var work in tech.works)
            if (!Unlocks.ControlledWorks.Contains(UnlockPolicy.Canonical(work))) UnlockWork(UnlockPolicy.Canonical(work));
        foreach (var phrase in tech.phrases) _save.UnlockPhrase(UnlockPolicy.Canonical(phrase));
        foreach (var perk in tech.perks)
            if (!Unlocks.ControlledPerks.Contains(UnlockPolicy.Canonical(perk))) _save.UnlockPerk(UnlockPolicy.Canonical(perk));
        // Preserve world/quest effects; AP-controlled unlock calls inside these scripts are gated separately.
        if (!string.IsNullOrEmpty(tech.flowscript)) GS.RunFlowScript(tech.flowscript);
    }

    public bool ShouldSuppressCraftReveal(IEnumerable<string> craftIDs)
        => craftIDs != null && craftIDs.Any(id => Unlocks.ControlledCrafts.Contains(UnlockPolicy.Canonical(id)));

    public void TrackTechnologyDialog(TechDefinition technology, bool pseudotech)
        => _dialogTechnology = !pseudotech && technology != null && _catalog.Definitions.ContainsKey(technology.id)
            ? technology : null;

    public void ClearTechnologyDialog() => _dialogTechnology = null;

    public bool IsPreviewDialog(TechDefinition technology) => technology != null &&
        ReferenceEquals(_dialogTechnology, technology) && _catalog.GetPreviewLocations(technology.id, _locations).Length > 0;

    public List<TechUnlock> GetPreviewUnlocks(string technology)
    {
        if (!_catalog.Definitions.ContainsKey(technology)) return null;
        var locations = _catalog.GetPreviewLocations(technology, _locations);
        if (locations.Length == 0) return null;
        return locations.Select(location =>
        {
            // These are presentation objects only, never game craft IDs or save entries.
            var unlock = new TechUnlock { id = location, visible = true };
            _previewUnlocks.Add(unlock, new PreviewLocation { Name = location });
            return unlock;
        }).ToList();
    }

    public bool TryGetPreview(TechUnlock unlock, out TechUnlock.TechUnlockData data)
    {
        data = null;
        if (!_previewUnlocks.TryGetValue(unlock, out var reference)) return false;
        var item = _previews.Get(reference.Name);
        var icon = APSpriteLoader.GetSprite("archipelago_32x32");
        var definition = RewardPreviewPresentation.LocalDefinition(item, _client.GameName, _items);
        if (definition != null)
        {
            try
            {
                var receipt = ItemNotification.Create(item.Item, definition.Grant, null);
                var entry = receipt.Entries.FirstOrDefault();
                if (entry.ID == null && definition.Grant.Stages?.Count > 0)
                    entry = (definition.Grant.Type == "progressive_craft_unlock" ? "craft" : "work",
                        UnlockPolicy.Canonical(definition.Grant.Stages[0][0]));
                if (entry.ID != null) icon = ItemNotificationWindow.GetData(entry.Type, entry.ID).sprite ?? icon;
            }
            catch (Exception ex) { _logger.LogWarning($"Cannot draw preview icon for '{item.Item}': {ex.Message}"); }
        }
        data = new TechUnlock.TechUnlockData
        {
            name = RewardPreviewPresentation.Name(item),
            description = RewardPreviewPresentation.Description(item, reference.Name),
            sprite = icon,
            quality_icon = ResolveCategorySprite(item),
        };
        return true;
    }

    private static Sprite ResolveCategorySprite(RewardPreview preview)
    {
        var spriteName = RewardPreviewPresentation.CategorySpriteName(preview?.Flags);
        if (spriteName == "i_skull") return GameModifications.UI.TrapBadgeSprite.Get();
        return spriteName == null ? null : EasySpritesCollection.GetSprite(spriteName);
    }

    public bool IsRelationPreview(TechUnlock unlock)
    {
        if (unlock == null || !_previewUnlocks.TryGetValue(unlock, out var reference)) return false;
        var reward = _previews.Get(reference.Name);
        return reward != null && reward.Game == _client.GameName &&
               _items.TryGetValue(reward.Item, out var item) && item.Grant.Type == "relation";
    }

    public bool IsPermanentBuffPreview(TechUnlock unlock)
    {
        if (unlock == null || !_previewUnlocks.TryGetValue(unlock, out var reference)) return false;
        return RewardPreviewPresentation.IsLocalPermanentBuff(_previews.Get(reference.Name), _client.GameName, _items);
    }

    public void Update()
    {
        if (_previewsLoaded || !_client.IsConnected || Time.unscaledTime < _retryAt) return;
        if (_previewRequest == null)
        {
            _previewRequest = _previews.FetchAsync(_catalog.Definitions.Keys.SelectMany(id => _catalog.GetPreviewLocations(id, _locations)));
            _requestStartedAt = Time.unscaledTime;
            return;
        }
        if (!_previewRequest.IsCompleted)
        {
            if (Time.unscaledTime - _requestStartedAt > 20)
            {
                // A disconnected socket may leave its old scout task pending indefinitely.
                _previewRequest = null;
                _retryAt = Time.unscaledTime + 5;
            }
            return;
        }
        try
        {
            _previews.Accept(_previewRequest.GetAwaiter().GetResult());
            if (GUIElements.me.tech_tree.is_shown) Traverse.Create(GUIElements.me.tech_tree).Method("RedrawTechs").GetValue();
            var dialog = GUIElements.me.tech_dialog;
            if (dialog.is_shown && _dialogTechnology != null)
            {
                var previews = GetPreviewUnlocks(_dialogTechnology.id);
                if (previews != null)
                {
                    var rows = Traverse.Create(dialog).Field("_unlocks").GetValue<List<TechTreeGUIUnlockItem>>();
                    for (var index = 0; index < rows.Count; index++)
                        rows[index].Draw(index < previews.Count ? previews[index] : null, false);
                    dialog.table.Reposition();
                    dialog.UpdateAllAnchors();
                }
            }
            _logger.LogDebug("AP technology reward previews loaded without creating hints.");
            _previewsLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"AP technology previews will retry: {ex.Message}");
            _retryAt = Time.unscaledTime + 5;
        }
        _previewRequest = null;
    }

    public void Dispose()
    {
        TechnologyNpcIconPatch.RestoreAll();
        TechnologyBuffIconPatch.RestoreAll();
        GameModifications.UI.TrapQualityBadgePatch.RestoreAll();
        foreach (var recipe in _originalRecipeLocks) recipe.Key.needs_unlock = recipe.Value;
        foreach (var work in _originalWorkLocks) work.Key.need_unlock_work = work.Value;
        if (ReferenceEquals(Current, this)) Current = null;
    }
    private sealed class PreviewLocation { public string Name { get; set; } }
}
