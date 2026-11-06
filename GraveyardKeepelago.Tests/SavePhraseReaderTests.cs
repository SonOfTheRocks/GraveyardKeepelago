using System.Text;
using GraveyardKeepelago.Progression;

namespace GraveyardKeepelago.Tests;

public class SavePhraseReaderTests
{
    private const int PhraseField = 144965494; // Native stable hash of unlocked_phrases.

    [Fact]
    public void ReadsPhraseListAndEncryptedIndexedStringsWithoutConstructingWorldObjects()
    {
        var bytes = Fixture(writer =>
        {
            writer.Write(2);
            writer.Write(123); writer.Write((byte)250); writer.Write(3);
            writer.Write(7); writer.Write((byte)102); writer.Write(100_000); writer.Write(new byte[100_000]);
            writer.Write(8); writer.Write((byte)9); Text(writer, "unrelated 世界 🌍");
            writer.Write(9); writer.Write((byte)101); writer.Write(3);
            writer.Write((byte)14); writer.Write(1f); writer.Write(2f); writer.Write(3f);
            writer.Write((byte)8); writer.Write('ö');
            writer.Write((byte)250); writer.Write(-1);
            writer.Write(PhraseField); writer.Write((byte)100); writer.Write(4);
            writer.Write((byte)10); writer.Write(0);
            writer.Write((byte)9); Text(writer, "graveyard_keepelago:progress:v1:long-record");
            writer.Write((byte)11);
            writer.Write((byte)10); writer.Write(1);
        }, "vanilla phrase", "ö m 世界");
        Assert.Equal(new[] { "vanilla phrase", "graveyard_keepelago:progress:v1:long-record", "", "ö m 世界" },
            SavePhraseReader.Read(bytes));
    }

    [Fact]
    public void PreservesGuardDecisionsAndRejectsAnotherSeedWithoutChangingRecords()
    {
        var records = new List<string>();
        new SaveStartupGuard("Seed", 0, 1).ContinueNewSave(records, () => { });
        var bytes = Fixture(writer =>
        {
            writer.Write(1); writer.Write(PhraseField); writer.Write((byte)100); writer.Write(records.Count);
            foreach (var record in records) { writer.Write((byte)9); Text(writer, record); }
        });
        var read = SavePhraseReader.Read(bytes);
        Assert.Equal(records, read);
        Assert.Null(SaveSlotEligibility.Reason(new SaveStartupGuard("Seed", 0, 1), false, () => read));
        Assert.NotNull(SaveSlotEligibility.Reason(new SaveStartupGuard("Other", 0, 1), false, () => read));
        Assert.NotNull(SaveSlotEligibility.Reason(null!, false, () => read));
        Assert.Equal(records, read);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(7)]
    [InlineData(79)] [InlineData(84)]
    public void TruncatedFilesFailClosed(int length)
        => Assert.Throws(length < 8 ? typeof(EndOfStreamException) : typeof(InvalidDataException),
            () => SavePhraseReader.Read(new byte[length]));

    [Fact]
    public void MissingOrWronglyTypedPhraseFieldIsNotMistakenForVanilla()
    {
        Assert.Throws<InvalidDataException>(() => SavePhraseReader.Read(Fixture(writer => writer.Write(0))));
        Assert.Throws<InvalidDataException>(() => SavePhraseReader.Read(Fixture(writer =>
        { writer.Write(1); writer.Write(PhraseField); writer.Write((byte)3); writer.Write(123); })));
    }

    [Theory]
    [InlineData(-1)] [InlineData(1)] [InlineData(int.MaxValue)]
    public void InvalidStringIndicesAreRejected(int index)
        => Assert.Throws<InvalidDataException>(() => SavePhraseReader.Read(Fixture(writer =>
        {
            writer.Write(1); writer.Write(PhraseField); writer.Write((byte)100); writer.Write(1);
            writer.Write((byte)10); writer.Write(index);
        }, "only index zero")));

    [Fact]
    public void NullPhraseIsPreservedSoTheExistingGuardCanRejectCorruption()
    {
        var read = SavePhraseReader.Read(Fixture(writer =>
        {
            writer.Write(1); writer.Write(PhraseField); writer.Write((byte)100); writer.Write(1); writer.Write((byte)0);
        }));
        Assert.Null(Assert.Single(read));
        Assert.Contains("corrupt", SaveSlotEligibility.Reason(null!, false, () => read)!);
    }

    [Fact]
    public void UnknownFormatRequestsNativeFallbackInsteadOfGuessing()
    {
        var version = Fixture(writer => writer.Write(0));
        BitConverter.GetBytes(2).CopyTo(version, 16);
        Assert.Throws<NotSupportedException>(() => SavePhraseReader.Read(version));
        Assert.Throws<NotSupportedException>(() => SavePhraseReader.Read(Fixture(writer =>
        { writer.Write(1); writer.Write(123); writer.Write((byte)200); })));
    }

    private static byte[] Fixture(Action<BinaryWriter> body, params string[] strings)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(0L); writer.Write(0L); writer.Write(1);
        for (var i = 0; i < 15; i++) writer.Write(0);
        body(writer);
        var table = stream.Position;
        writer.Write(strings.Length);
        foreach (var text in strings) Text(writer, text, true);
        writer.Flush(); stream.Position = 0; writer.Write(table); writer.Flush();
        return stream.ToArray();
    }

    private static void Text(BinaryWriter writer, string text, bool encrypted = false)
    {
        writer.Write(text.Length);
        var chars = text.ToCharArray();
        if (encrypted)
            for (var i = 0; i < chars.Length; i++)
                if (chars[i] <= 255 && chars[i] != 0 && chars[i] != 109) chars[i] = (char)(chars[i] ^ 109);
        writer.Write(chars);
    }
}
