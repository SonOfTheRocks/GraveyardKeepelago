using System.Collections.Generic;
using System.IO;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Items;

namespace GraveyardKeepelago.Logic;

public class GKItemFactory
{
    private readonly IPlayerActions _playerActions;
    private readonly ClientContract _contract;

    public GKItemFactory(IPlayerActions playerActions, ClientContract contract = null)
    {
        _playerActions = playerActions;
        _contract = contract ?? ClientContractLoader.LoadDefault();
    }

    public void BuildAll(GKItemRegistry registry)
    {
        foreach (var item in _contract.Items.Values)
            registry.Add(item.Name, CreateItem(item.Name, item.Grant));
    }

    private IAPItem CreateItem(string name, ClientContractGrant grant)
    {
        switch (grant.Type)
        {
            case "trap":
                var trapID = RequiredString(name, "trap_id", grant.TrapID);
                Items.Traps.MonsterTrapDefinition.Get(trapID);
                return new GKTrap(trapID, _playerActions);
            case "game_item":
                return new GKIngameItem(RequiredString(name, "item_id", grant.ItemID), _playerActions);
            case "craft_unlock":
                return new GKRecipe(RequiredStringList(name, "craft_ids", grant.CraftIDs), _playerActions);
            case "progressive_craft_unlock":
                return new GKProgressiveRecipe(RequiredStages(name, grant.Stages), _playerActions);
            case "work_unlock":
                return new GKWork(
                    RequiredStringList(name, "work_ids", grant.WorkIDs),
                    RequiredStringList(name, "perk_ids", grant.PerkIDs),
                    _playerActions
                );
            case "progressive_work_unlock":
                return new GKProgressiveWork(RequiredStages(name, grant.Stages), _playerActions);
            case "perk_unlock":
                return new GKPerk(
                    RequiredString(name, "perk_id", grant.PerkID),
                    RequiredStringList(name, "craft_ids", grant.CraftIDs),
                    RequiredStringList(name, "work_ids", grant.WorkIDs),
                    _playerActions
                );
            case "permanent_buff":
                return new GKPermaBuff(
                    RequiredString(name, "buff_id", grant.BuffID),
                    RequiredStringList(name, "resource_types", grant.ResourceTypes),
                    RequiredString(name, "icon", grant.Icon, allowEmpty: true),
                    _playerActions
                );
            case "relation":
                return new GKRelation(
                    RequiredString(name, "npc_id", grant.NpcID),
                    grant.Amount,
                    _playerActions
                );
            case "research_points":
                return new GKResearchPoints(
                    RequiredString(name, "color", grant.Color),
                    grant.Amount,
                    _playerActions
                );
            case "technology_unlock":
                return new GKTechnologyUnlock(
                    RequiredStringList(name, "technology_ids", grant.TechnologyIDs),
                    _playerActions
                );
            case "flow_script":
                return new GKFlowScript(
                    RequiredString(name, "script_id", grant.ScriptID),
                    _playerActions
                );
            default:
                throw new InvalidDataException($"Item '{name}' has unsupported grant type '{grant.Type}'");
        }
    }

    private static string RequiredString(
        string itemName,
        string field,
        string value,
        bool allowEmpty = false)
    {
        if (value == null || (!allowEmpty && string.IsNullOrWhiteSpace(value)))
            throw new InvalidDataException($"Item '{itemName}' has invalid grant field '{field}'");
        return value;
    }

    private static List<string> RequiredStringList(string itemName, string field, List<string> value)
    {
        if (value == null)
            throw new InvalidDataException($"Item '{itemName}' has invalid grant field '{field}'");
        return value;
    }

    private static List<List<string>> RequiredStages(string itemName, List<List<string>> value)
    {
        if (value == null || value.Count == 0)
            throw new InvalidDataException($"Item '{itemName}' has invalid progressive stages");
        return value;
    }
}
