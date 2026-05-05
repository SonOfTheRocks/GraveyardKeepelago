using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;
using JetBrains.Annotations;

namespace GraveyardKeepelago.Items
{
    public abstract class GKItem: IAPItem
    {
        protected List<string> IDs { get; private set; }
        protected IPlayerActions PlayerActions { get; }

        protected GKItem([CanBeNull] string id, IPlayerActions playerActions)
        {
            IDs = id is not null ? [id] : [];
            PlayerActions = playerActions;
        }

        protected GKItem([CanBeNull] List<string> ids, IPlayerActions playerActions)
        {
            IDs = ids is not null ? [..ids] : [];
            PlayerActions = playerActions;
        }

        public abstract void Apply();
    }
}