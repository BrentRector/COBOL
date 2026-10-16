// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A FRAMED RECORD IS DECODED IN ONE PLACE — RecordFraming.ReadFrame — SO EVERY READER OF A RECORD VARYING SEQUENTIAL
/// FILE OR A KEYED STORE AGREES ON WHICH RECORDS THE FILE HOLDS (kb/Work PB2754).
/// <para><b>The derivation.</b> The frame is the §9.1.7.2 information "the implementor may add to the record on the
/// physical storage medium (such as record length headers)", so what a damaged frame means is the implementor's to
/// decide — but it is decided ONCE. The frame grammar (the length word, the gap tag, bit 31's extent flag, the extent
/// table, the payload) was decoded in four places, two of them through a parallel decoder over Latin-1 characters,
/// and the copies already disagreed: a torn last frame was delivered by the forward READ as one more record while the
/// record count and the backward-READ index (<c>RecordFraming.FrameStarts</c>) ended the data before it; a gap word
/// after a valid frame was a zero-length record to the index and an <see cref="OutOfMemoryException"/> to the READ;
/// a huge extent-table count allocated its table before a single byte was read.</para>
/// <para><b>So the guard is the shape, measured three ways:</b> the frame word's two constants are spelled only by the
/// one decoder and the one encoder; every frame reader reaches the decoder; and on every damaged shape the readers
/// stop at the same frame.</para>
/// </summary>
public sealed class FrameDecoderDriftTests
{
    /// <summary>The members allowed to spell the frame word's constants or decode a little-endian 32-bit word: the
    /// one decoder (and the table it reads), the one encoder, and the gap encoder.</summary>
    private static readonly HashSet<string> FrameWordHomes = new(StringComparer.Ordinal)
    {
        "ReadFrame", "ReadExtentTable", "EncodeFrameHead", "EncodeGap",
    };

    /// <summary>Every member that reads frames, by file — the selector's population, so the reach test measures
    /// something only while it still finds them (feedback_measure_the_selectors_complement).</summary>
    private static readonly (string File, string[] Methods)[] FrameReaders =
    [
        ("RecordFraming.cs", ["DecodeStore", "FrameStarts", "StreamFramingParses"]),
        ("SequentialConnector.cs", ["NextFrame"]),
    ];

    private static IEnumerable<(string File, string Source)> IoSources() =>
        Directory.GetFiles(TestRepo.Src("Cobol.Net.Runtime", "IO"), "*.cs", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(f => (Path.GetFileName(f), File.ReadAllText(f)));

    private static IEnumerable<MethodDeclarationSyntax> Methods(string source) =>
        CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();

    [Fact]
    public void TheFrameWord_IsSpelledOnlyByTheOneDecoderAndTheOneEncoder()
    {
        var offenders = new List<string>();
        int homesSeen = 0;
        foreach (var (file, source) in IoSources())
        {
            var root = CSharpSyntaxTree.ParseText(source).GetRoot();
            foreach (var node in root.DescendantNodes())
            {
                bool spellsWord = node is IdentifierNameSyntax { Identifier.Text: "GapTag" or "ExtentFlag" };
                bool decodesWord = node is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ReadUInt32LittleEndian" },
                };
                if (!spellsWord && !decodesWord) continue;
                string? member = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
                if (member is not null && FrameWordHomes.Contains(member)) { homesSeen++; continue; }
                int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                offenders.Add($"{file}:{line} ({member ?? "outside a method"}): {node}");
            }
        }

        Assert.True(homesSeen >= 4, $"The sweep found the frame word in only {homesSeen} places inside its homes — "
            + "the decoder or the encoder moved, and this test no longer measures anything.");
        Assert.True(offenders.Count == 0,
            "The frame word is decoded or spelled outside RecordFraming.ReadFrame / EncodeFrameHead (kb/Work PB2754). "
            + "A second decoder is a second answer to what a gap and a torn tail are — call ReadFrame through an "
            + "IFrameMedium instead:\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void EveryFrameReader_ReachesTheOneDecoder()
    {
        var sources = IoSources().ToDictionary(s => s.File, s => s.Source, StringComparer.Ordinal);
        var missing = new List<string>();
        foreach (var (file, methods) in FrameReaders)
        {
            Assert.True(sources.ContainsKey(file), $"The frame reader file {file} moved; update {nameof(FrameReaders)}.");
            var declared = Methods(sources[file]).ToList();
            foreach (string name in methods)
            {
                var method = declared.SingleOrDefault(m => m.Identifier.Text == name);
                Assert.True(method is not null, $"{file} no longer declares the frame reader {name}; update {nameof(FrameReaders)}.");
                bool reaches = method!.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => i.Expression switch
                {
                    MemberAccessExpressionSyntax m => m.Name.Identifier.Text is "ReadFrame" or "ReadStreamFrame",
                    IdentifierNameSyntax id => id.Identifier.Text is "ReadFrame" or "ReadStreamFrame",
                    _ => false,
                });
                if (!reaches) missing.Add($"{file}: {name}");
            }
        }
        Assert.True(missing.Count == 0,
            "A frame reader decodes frames without RecordFraming.ReadFrame (kb/Work PB2754):\n  " + string.Join("\n  ", missing));
    }

    // ── every reader stops at the same frame ─────────────────────────────────────────────────────────────────

    /// <summary>The ways a framed file's data can end inside a frame or hold a head this writer cannot produce.</summary>
    public enum Damage
    {
        /// <summary>A length word naming more payload than the file holds.</summary>
        TornPayload,

        /// <summary>A length word cut short.</summary>
        TornWord,

        /// <summary>An extent table whose components the file ends inside.</summary>
        TornTable,

        /// <summary>The gap tag, which no stream-level writer writes, followed by a valid frame.</summary>
        GapWord,

        /// <summary>An extent-table count no file can hold.</summary>
        HugeTableCount,

        /// <summary>A length word of nearly 2^31 bytes (not the gap tag).</summary>
        HugePayload,

        /// <summary>A table component of negative length.</summary>
        NegativeComponent,
    }

    private static byte[] Word(uint w)
    {
        var b = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(b, w);
        return b;
    }

    private static byte[] Frame(string payload) => [.. Word((uint)payload.Length), .. System.Text.Encoding.Latin1.GetBytes(payload)];

    private static byte[] Damaged(Damage damage) => damage switch
    {
        Damage.TornPayload => [.. Word(10), .. "PART"u8],
        Damage.TornWord => [0x05, 0x00],
        Damage.TornTable => [.. Word(0x80000003), .. Word(2), .. Word(0), .. Word(1)],
        Damage.GapWord => [.. Word(0xFFFFFFFF), .. Frame("AFTER")],
        Damage.HugeTableCount => [.. Word(0x80000003), .. Word(0x7FFFFFF0), .. "ABC"u8],
        Damage.HugePayload => [.. Word(0x7FFFFFF0), .. "ABC"u8],
        Damage.NegativeComponent => [.. Word(0x80000003), .. Word(1), .. Word(0), .. Word(0xFFFFFFFF), .. "ABC"u8],
        _ => throw new ArgumentOutOfRangeException(nameof(damage)),
    };

    private static readonly byte[] TwoRecords = [.. Frame("HELLO"), .. Frame("WORLD!")];

    public static TheoryData<Damage> Damages() => new(Enum.GetValues<Damage>());

    [Theory]
    [MemberData(nameof(Damages))]
    public void EveryReader_EndsTheDataAtTheSameFrame(Damage damage)
    {
        string host = Path.Combine(Path.GetTempPath(), $"pb2754-{damage}-{Guid.NewGuid():N}.dat");
        try
        {
            File.WriteAllBytes(host, [.. TwoRecords, .. Damaged(damage)]);

            // The OPEN's framing check reads the first frame, which is whole.
            Assert.True(RecordFraming.StreamFramingParses(host));

            // The record count and the backward-READ index.
            using (var fs = File.OpenRead(host))
                Assert.Equal([0L, 9L], RecordFraming.FrameStarts(fs));

            // The forward READ delivers the same two records and then the end of the data.
            var c = new SequentialConnector(host, 20, lineSequential: false, varyMin: 1, varyMax: 20);
            Assert.Equal("00", c.Open(FileOpenMode.Input));
            Assert.True(c.Read(previous: false, out string first));
            Assert.Equal("HELLO", first.TrimEnd());   // the record area is 20 characters wide
            Assert.True(c.Read(previous: false, out string second));
            Assert.Equal("WORLD!", second.TrimEnd());
            Assert.False(c.Read(previous: false, out _));
            Assert.Equal("10", c.Status);
            Assert.Equal("00", c.Close());
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    [Theory]
    [MemberData(nameof(Damages))]
    public void ADamagedFirstFrame_IsNotAFramedFile(Damage damage)
    {
        string host = Path.Combine(Path.GetTempPath(), $"pb2754-first-{damage}-{Guid.NewGuid():N}.dat");
        try
        {
            File.WriteAllBytes(host, Damaged(damage));
            // A file shorter than one length word states nothing (the torn word); every other damaged first frame is
            // the decoder's End, which the OPEN answers '39' (§9.1.13.6 item 7).
            Assert.Equal(damage == Damage.TornWord, RecordFraming.StreamFramingParses(host));
            using var fs = File.OpenRead(host);
            Assert.Empty(RecordFraming.FrameStarts(fs));
        }
        finally { try { File.Delete(host); } catch (IOException) { } }
    }

    [Fact]
    public void TheStoreShape_KeepsAGapAsASlot_AndEndsAtATornTail()
    {
        var attributes = new FixedFileAttributes(FixedFileAttributes.Relative, true, 1, 20, []);
        using var header = RecordFraming.ComposeStore(attributes, [], generation: 0, releaseMint: 0);
        byte[] image = [.. header.ToArray(), .. TwoRecords, .. Damaged(Damage.GapWord), .. Damaged(Damage.TornPayload)];
        var frames = RecordFraming.DecodeStore(image, image.Length, out _);
        Assert.Equal(new string?[] { "HELLO", "WORLD!", null, "AFTER" }, frames.Select(f => f?.Image));
    }
}
