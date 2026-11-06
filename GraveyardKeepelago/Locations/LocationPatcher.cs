using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Locations.Patches;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using HarmonyLib;
using KaitoKid.Utilities.Interfaces;
using System.Collections.Generic;

namespace GraveyardKeepelago.Locations;

public sealed class LocationPatcher
{
    private readonly Harmony _harmony;
    private readonly GKLocationChecker _locationChecker;
    private readonly HashSet<string> _activeLocations;
    private static bool _patchesInstalled;

    public LocationPatcher(ILogger logger, Harmony harmony, GKArchipelagoClient archipelago,
        GKLocationChecker locationChecker, GKItemRegistry registry)
    {
        _harmony = harmony;
        _locationChecker = locationChecker;
        _activeLocations = new HashSet<string>(archipelago.GetAllLocations().Keys);
    }

    public void ReplaceAllLocationsRewardsWithChecks()
    {
        var registry = new LocationTriggerRegistry(ClientContractLoader.LoadDefault(), _activeLocations);
        LocationTriggerRuntime.Initialize(new LocationTriggerService(_locationChecker, registry));
        if (_patchesInstalled) return;
        _harmony.CreateClassProcessor(typeof(GameSaveOnFinishedCraftLocationPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(TechUnlockDialogOpenLocationPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(GameSaveUnlockTechLocationPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(WorldGameObjectAddToInventoryLocationPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(MultiInventoryMoveItemToLocationPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(GameSaveUnlockCraftPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(TechnologyVisibleRewardsPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(TechnologyPreviewDataPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(TechnologyPreviewTooltipPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(TechnologyNpcIconPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(TechnologyBuffIconPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(TechnologyItemDialogContextPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(ReceivedRecipeLockPatch)).Patch();
        _harmony.CreateClassProcessor(typeof(ReceivedPerkUnlockPatch)).Patch();
        _patchesInstalled = true;
    }

    // Called after the owning Harmony instance has been unpatched.
    public void CleanEvents()
    {
        TechnologyNpcIconPatch.RestoreAll();
        TechnologyBuffIconPatch.RestoreAll();
        GraveyardKeepelago.GameModifications.UI.TrapQualityBadgePatch.RestoreAll();
        _patchesInstalled = false;
        LocationTriggerRuntime.Clear();
    }
}
