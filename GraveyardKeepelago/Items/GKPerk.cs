using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;
using JetBrains.Annotations;

namespace GraveyardKeepelago.Items
{
    public class GKPerk: GKItem
    {
        private readonly List<string> _recipes;
        private readonly List<string> _works;

        public GKPerk(string id, [CanBeNull] string recipe, [CanBeNull] string work, IPlayerActions playerActions = null) : base(id, playerActions)
        {
            _recipes = recipe is not null ? [recipe] : [];
            _works = work is not null ? [work] : [];
        }
        
        public GKPerk(string id, [CanBeNull] List<string> recipes = null, [CanBeNull] List<string> works = null, IPlayerActions playerActions = null) : base(new List<string>{id}, playerActions)
        {
            _recipes = recipes is not null ? [..recipes] : [];
            _works = works is not null ? [..works] : [];
        }

        public override void Apply()
        {
            // Recipe IDs bundled with perks are invisible variants in the game data.
            // Work IDs are regular canonical IDs, such as t_wood_big and t_mushroom2.
            System.Diagnostics.Debug.Assert(_recipes.All(r => r.StartsWith("@")));
            
            var unlocks = IDs
                .Select(id => ((PlayerUtilities.UnlockType, string))(PlayerUtilities.UnlockType.Perk, id))
                .Concat(_recipes.Select(id => ((PlayerUtilities.UnlockType, string))(PlayerUtilities.UnlockType.Craft, id)))
                .Concat(_works.Select(id => ((PlayerUtilities.UnlockType, string))(PlayerUtilities.UnlockType.Work, id)))
                .ToList();
            
            PlayerActions.ApplyUnlocks(unlocks);
        }
    }
}
