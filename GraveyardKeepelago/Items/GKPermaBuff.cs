using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKPermaBuff: GKItem
    {
        private readonly List<string> _res_types;
        private readonly string _icon;
        
        public GKPermaBuff(string id, List<string> res_types, string icon): base(new List<string>{id})
        {
            _res_types = res_types;
            _icon = icon;
        }

        public override void Apply()
        {
            PlayerUtilities.ApplyPermanentBuff(IDs[0]);
        }
    }
}