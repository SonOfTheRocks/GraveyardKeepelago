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

        public GKPerk(string id, [CanBeNull] string recipe, [CanBeNull] string work) : base(id)
        {
            _recipes = recipe is not null ? [recipe] : [];
            _works = work is not null ? [work] : [];
        }
        
        public GKPerk(string id, [CanBeNull] List<string> recipes = null, [CanBeNull] List<string> works = null) : base(new List<string>{id})
        {
            _recipes = recipes is not null ? [..recipes] : [];
            _works = works is not null ? [..works] : [];
        }

        public override void Apply()
        {
            // The recipes and works that come along with the perk should all be invisible
            System.Diagnostics.Debug.Assert(_recipes.All(r => r.StartsWith("@")));
            System.Diagnostics.Debug.Assert(_works.All(w => w.StartsWith("@")));
            
            var unlocks = IDs
                .Select(id => (PlayerUtilities.UnlockType.Perk, id))
                .Concat(_recipes.Select(id => (PlayerUtilities.UnlockType.Craft, id)))
                .Concat(_works.Select(id => (PlayerUtilities.UnlockType.Work, id)))
                .ToList();
            
            PlayerUtilities.ApplyUnlocks(unlocks);
        }
    }
}