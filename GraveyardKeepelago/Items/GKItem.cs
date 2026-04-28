using System.Collections.Generic;
using JetBrains.Annotations;

namespace GraveyardKeepelago.Items
{
    public abstract class GKItem: IAPItem
    {
        protected List<string> IDs { get; private set; }

        protected GKItem([CanBeNull] string id)
        {
            IDs = id is not null ? [id] : [];
        }

        protected GKItem([CanBeNull] List<string> ids)
        {
            IDs = ids is not null ? [..ids] : [];
        }

        public abstract void Apply();
    }
}