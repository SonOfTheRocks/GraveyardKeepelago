using System;
using System.Collections.Generic;
using System.Linq;
using KaitoKid.ArchipelagoUtilities.Net.Client.ConnectionResults;

namespace GraveyardKeepelago.Archipelago.ConnectionResults
{
    public class MissingDLCsConnectionResult: FailedConnectionResult
    {
        public MissingDLCsConnectionResult(List<string> missingDLCs) : base(GetErrorMessage(missingDLCs))
        {
        }

        private static string GetErrorMessage(IEnumerable<string> missingDLCs)
        {
            const string msg = "The slot you are connecting to has been created expecting some DLCs,\nbut no all expected DLCs are installed and active.";
            return missingDLCs.Aggregate(msg, (current, missingDLC) => current + $"{Environment.NewLine}\t{missingDLC}");
        }
    }
}