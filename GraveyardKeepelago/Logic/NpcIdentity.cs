namespace GraveyardKeepelago.Logic;

public static class NpcIdentity
{
    // Historical shared grants use an underscore. ObjectDefinition, native
    // relation parameters and NPC portrait sprites use this exact space.
    // Keep old seeds readable without changing AP names or the shared manifest.
    public static string Canonical(string id) => id == "npc_mrs_chain" ? "npc_mrs chain" : id;
}
