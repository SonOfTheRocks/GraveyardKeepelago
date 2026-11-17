using System.IO;
using System.Linq;
using GraveyardKeepelago.GameModifications.Patches;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class PermanentBuffGrant : IItemGrant
{
    public string BuffID { get; }

    public PermanentBuffGrant(string buffID) => BuffID = buffID;

    public GrantResult Apply(GrantContext context)
    {
        var save = context.Save;
        if (GameBalance.me.GetDataOrNull<BuffDefinition>(BuffID) == null)
            throw new InvalidDataException($"Unknown game buff '{BuffID}'.");

        if (save.buffs.Any(buff => buff.buff_id == BuffID && PermanentBuffUpdatePatch.IsPermanent(buff.end_time)))
            return GrantResult.Applied;

        BuffsLogics.AddBuff(BuffID);
        var applied = save.buffs.FirstOrDefault(buff => buff.buff_id == BuffID);
        if (applied == null)
            throw new InvalidDataException($"Could not apply AP buff '{BuffID}'.");
        applied.end_time = float.MaxValue;
        return GrantResult.Applied;
    }
}
