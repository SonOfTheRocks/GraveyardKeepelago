using KaitoKid.ArchipelagoUtilities.Net.Client.ConnectionResults;

namespace GraveyardKeepelago.Archipelago.ConnectionResults
{
    public class IncompatibleDataVersionConnectionResult : FailedConnectionResult
    {
        public IncompatibleDataVersionConnectionResult(int multiworldDataVersion, int installedDataVersion)
            : base(
                $"This multiworld requires Graveyard Keeper data version {multiworldDataVersion}, " +
                $"but this client contains data version {installedDataVersion}. " +
                "Update GraveyardKeepelago to the latest version that supports the required data version."
            )
        {
        }
    }
}
