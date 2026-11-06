using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GraveyardKeepelago.Progression;

// Read-only projection of SmartSerializer v1, verified against the game's implementation.
// Skip world data without reflection, constructors, Unity calls or serializer-global caches.
// The normal full-save load guard still validates the actual selected save.
public static class SavePhraseReader
{
    public static IList<string> Read(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, false);
        using var reader = new BinaryReader(stream, Encoding.UTF8);
        return new Projection(reader).Read();
    }

    private sealed class Projection
    {
        private readonly BinaryReader _reader;
        private readonly char[] _chars = new char[4096];
        private long _dataEnd;
        private string[] _strings;
        public Projection(BinaryReader reader) => _reader = reader;

        public IList<string> Read()
        {
            _dataEnd = _reader.ReadInt64();
            if (_dataEnd < 84 || _dataEnd > _reader.BaseStream.Length - 4)
                throw new InvalidDataException("Invalid binary save header.");
            _reader.ReadInt64(); // Reserved header offset, distinct from the table pointer.
            if (_reader.ReadInt32() != 1) throw new NotSupportedException("Unknown binary save version.");
            SkipBytes(60); // Fifteen reserved Int32s.
            var fields = Count();
            for (var field = 0; field < fields; field++)
            {
                var hash = _reader.ReadInt32();
                if (hash == PhraseHash()) return ReadPhrases();
                SkipValue(0);
            }
            throw new InvalidDataException("The save has no phrase list.");
        }

        private IList<string> ReadPhrases()
        {
            var type = _reader.ReadByte();
            if (type == 0) return null;
            if (type != 100) throw new InvalidDataException("Invalid save phrase list.");
            var count = Count();
            var result = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                type = _reader.ReadByte();
                switch (type)
                {
                    case 0: result.Add(null); break;
                    case 9: result.Add(ReadString(false)); break;
                    case 10:
                        var index = _reader.ReadInt32();
                        LoadStrings();
                        if (index < 0 || index >= _strings.Length) throw new InvalidDataException("Invalid save string index.");
                        result.Add(_strings[index]);
                        break;
                    case 11: result.Add(string.Empty); break;
                    default: throw new InvalidDataException("Invalid save phrase value.");
                }
                CheckDataBounds();
            }
            return result;
        }

        private void SkipValue(int depth)
        {
            if (depth > 128) throw new InvalidDataException("Save nesting is too deep.");
            var type = _reader.ReadByte();
            switch (type)
            {
                case 0: case 1: case 2: case 11: case 16: case 17: case 18: case 19:
                case 20: case 21: case 22: case 23: case 24: break;
                case 3: case 5: case 10: SkipBytes(4); break;
                case 4: case 6: case 13: SkipBytes(8); break;
                case 7: SkipBytes(1); break;
                case 8: _reader.ReadChar(); break;
                case 9: SkipString(); break;
                case 14: SkipBytes(12); break;
                case 15: SkipBytes(16); break;
                case 102: SkipBytes(Count()); break;
                case 100: case 101:
                    var count = Count();
                    for (var i = 0; i < count; i++) SkipValue(depth + 1);
                    break;
                case 250:
                    var fields = Count(true);
                    for (var i = 0; i < fields; i++)
                    { SkipBytes(4); SkipValue(depth + 1); }
                    break;
                default: throw new NotSupportedException("Unknown binary save field type: " + type);
            }
            CheckDataBounds();
        }

        private int Count(bool nullable = false)
        {
            var count = _reader.ReadInt32();
            if (nullable && count == -1) return 0;
            if (count < 0 || count > _reader.BaseStream.Length - _reader.BaseStream.Position)
                throw new InvalidDataException("Invalid save collection length.");
            return count;
        }

        private void SkipBytes(long count)
        {
            if (count < 0 || count > _dataEnd - _reader.BaseStream.Position)
                throw new InvalidDataException("Truncated binary save.");
            _reader.BaseStream.Seek(count, SeekOrigin.Current);
        }

        private void CheckDataBounds()
        {
            if (_reader.BaseStream.Position > _dataEnd) throw new InvalidDataException("Truncated binary save.");
        }

        private void SkipString() => ReadChars(false, false);
        private string ReadString(bool encrypted) => ReadChars(true, encrypted);

        private string ReadChars(bool keep, bool encrypted)
        {
            var length = _reader.ReadInt32();
            if (length == -1) return null;
            if (length < 0 || length > _reader.BaseStream.Length - _reader.BaseStream.Position)
                throw new InvalidDataException("Invalid save string length.");
            var text = keep ? new StringBuilder() : null;
            while (length > 0)
            {
                var count = Math.Min(length, _chars.Length);
                if (_reader.Read(_chars, 0, count) != count) throw new EndOfStreamException();
                if (encrypted)
                    for (var i = 0; i < count; i++)
                        if (_chars[i] <= 255 && _chars[i] != 0 && _chars[i] != 109)
                            _chars[i] = (char)(_chars[i] ^ 109);
                text?.Append(_chars, 0, count);
                length -= count;
            }
            return text?.ToString();
        }

        private void LoadStrings()
        {
            if (_strings != null) return;
            var position = _reader.BaseStream.Position;
            _reader.BaseStream.Position = _dataEnd;
            _strings = new string[Count()];
            for (var i = 0; i < _strings.Length; i++) _strings[i] = ReadString(true);
            _reader.BaseStream.Position = position;
        }

        private static int PhraseHash()
        {
            const string name = "unlocked_phrases";
            unchecked
            {
                var first = 5381;
                var second = first;
                for (var i = 0; i < name.Length; i += 2)
                {
                    first = first * 33 ^ name[i];
                    if (i + 1 < name.Length) second = second * 33 ^ name[i + 1];
                }
                return first + second * 1566083941;
            }
        }
    }
}
