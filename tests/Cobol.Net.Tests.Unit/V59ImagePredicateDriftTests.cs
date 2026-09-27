// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE V59 IMAGE-PREDICATE INVENTORY. Two predicates decide "does this group have a whole-group image?":
/// <list type="bullet">
///   <item><c>DataItem.IsImageCapable</c> — V59's. A BINARY/PACKED leaf HAS a pinned byte image, so a group
///         containing one qualifies. Only float / COMP-5 / INDEX leaves are genuinely imageless. This is the
///         predicate <c>RecordStructEmitter</c> uses to decide whether to EMIT <c>AsImage()</c>/<c>FromImage()</c>
///         at all.</item>
///   <item><c>DataItem.IsCharacterImage</c> — the PRE-V59 one. A COMP/binary leaf qualifies only via
///         <c>StoreAsImage</c> promotion, so a plain COMP group does NOT.</item>
/// </list>
/// <para>
/// Why this test exists: V59 migrated SOME emit guards to <c>IsImageCapable</c> and left others on
/// <c>IsCharacterImage</c> — <c>MoveEmitter</c> ended up using BOTH. A guard left on the stricter predicate
/// loud-stages a construct whose codec <c>RecordStructEmitter</c> actually generated, which is how
/// <c>CALL "SUB" USING G</c> came to throw "no whole-group character image" for a group whose
/// <c>FUNCTION BYTE-LENGTH</c> answered 5. That is rejecting conforming source (§14.2.3 GR8 — a BY REFERENCE
/// formal "occupies the same storage area as the argument", which WiseOwl COBOL realizes through that very image
/// round-trip).
/// </para>
/// <para>
/// ⛔ THE REMAINING SITES ARE NOT AUTOMATICALLY BUGS. V59's own governing lesson is BYTES ARE NOT TEXT: a COMP
/// leaf's image is radix-2 bytes, not its digits, so an operation defined over CHARACTER POSITIONS (STRING,
/// UNSTRING, INSPECT) may be CORRECT to refuse it — feeding it those bytes as text would be a silent wrong
/// answer, which is worse than a loud stage. Each remaining site needs its own spec derivation. This test does
/// not prejudge them; it makes the inventory EXPLICIT so none can be forgotten, and forces a deliberate decision
/// when one is added or removed.
/// </para>
/// </summary>
public sealed class V59ImagePredicateDriftTests
{
    private static string Read(params string[] parts) => File.ReadAllText(TestRepo.At(parts));

    /// <summary>Count occurrences in CODE only. These predicates are discussed at length in comments (that is the
    /// point of the comments), so a raw text count is a false positive — the first cut of this test failed on its
    /// own explanatory comment. Strips <c>///</c> doc comments, whole-line <c>//</c> comments, and trailing
    /// <c>//</c> tails.</summary>
    private static int CodeOccurrences(string src, string identifier)
    {
        var code = new List<string>();
        foreach (string raw in src.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            if (t.StartsWith("///") || t.StartsWith("//")) continue;      // doc / whole-line comment
            int i = line.IndexOf("//", StringComparison.Ordinal);
            code.Add(i >= 0 ? line[..i] : line);                          // drop a trailing comment tail
        }
        return Regex.Matches(string.Join("\n", code), $@"\b{Regex.Escape(identifier)}\b").Count;
    }

    /// <summary>CALL's READ and WRITE halves are ONE image round-trip and must agree about which groups have an
    /// image. If they diverged, a group could cross in through <c>FromImage</c> and back out through a raw write,
    /// or be admitted by one half and refused by the other.
    /// <para>⛔ THE INVARIANT CHANGED SHAPE AT kb/Work PB177 ARM B, DELIBERATELY — record the reason rather than
    /// relaxing the count. This fact used to demand TWO spellings of <c>IsImageCapable</c> in
    /// <c>CallEmitter</c>, one per half, and the two were NOT in fact in lockstep: the write half's own guard
    /// let an imageless group FALL THROUGH to a raw <c>PlaceRenderer.Write</c>, rendering
    /// <c>_G = &lt;string&gt;;</c> (backend CS0029) where the read half staged the Tier-C loud. The fix deleted
    /// the write half's copy so every non-<c>RedefViewPlace</c> group routes to
    /// <c>PlaceRenderer.WriteFullGroupImage</c>, whose arm order carries the SAME predicate — the lockstep is
    /// now a STRUCTURAL fact instead of a count this test has to police. So: the read half still guards HERE,
    /// the write half must DELEGATE, and <c>BoundaryImageChannelTests</c> holds the ONE writer's arms.</para></summary>
    [Fact]
    public void CallEmitter_ReadAndWriteHalves_UseTheSamePredicate()
    {
        string src = Read("src", "Cobol.Net.Compiler", "CodeGen", "Verbs", "CallEmitter.cs");
        int capable = CodeOccurrences(src, "BoundaryImageCapable");
        int narrow = CodeOccurrences(src, "IsImageCapable");
        int character = CodeOccurrences(src, "IsCharacterImage");
        Assert.True(capable >= 1,
            $"Expected the CALL argument (read-half) group guard on BoundaryImageCapable; found {capable}.");
        // ⛔ THE PREDICATE WIDENED AT kb/Work PB204, AND THE NARROW ONE IS NOW ITSELF THE DEFECT. V59's lesson
        // was that guarding a CALL on IsCharacterImage rejected conforming source; PB204's is the same lesson
        // one predicate further out. §14.8.2.2 and §14.8.3.2 ADMIT a VARIABLE-LENGTH group across a Format-2
        // CALL / RETURNING / INVOKE boundary "subject to compatibility as described in 8.5.1.12", and
        // IsImageCapable is false for such a group BY DEFINITION — so a guard spelled that way refused every
        // one of them (COBOLNET1688 on legal source). BoundaryImageCapable is "fixed record window OR
        // current-extent carrier"; the ADMISSION itself is decided at bind by VariableLengthCompatibility.
        Assert.True(narrow == 0,
            "CallEmitter must not guard a group crossing on IsImageCapable: a variable-length group is never "
            + "IsImageCapable and §14.8.2.2 admits it subject to §8.5.1.12 compatibility, so that predicate "
            + $"rejects conforming source (kb/Work PB204). Found {narrow} occurrence(s).");
        // The WRITE half delegates instead of testing: it must hand every group to the ONE writer.
        // ⛔ MEASURED WITH THE COMMENT-STRIPPER, NOT RAW TEXT. This was `Assert.Contains("WriteFullGroupImage",
        // src)` against the unstripped file, and the PB177 comment block directly above the fixed arm names the
        // method twice — so deleting the actual delegating CALL would have left this assertion green. A drift
        // test satisfied by the comment explaining the drift is worse than no drift test: it reads as evidence.
        // The same method's capability counts already used CodeOccurrences; this line simply joins them.
        Assert.True(CodeOccurrences(src, "WriteFullGroupImage") >= 1,
            "CallStringWrite must DELEGATE the group arm to PlaceRenderer.WriteFullGroupImage — no occurrence "
            + "found in CODE (comments stripped).");
        Assert.DoesNotContain("p.Item.IsGroup && p is not RedefViewPlace && p.Item.IsImageCapable", src);
        Assert.True(character == 0,
            "CallEmitter must not use the pre-V59 IsCharacterImage: a group whose only non-character leaf is "
            + "BINARY/PACKED has a whole-group image (V59), and RecordStructEmitter emits its codec. Guarding on "
            + "the stricter predicate rejects conforming source — see §14.2.3 GR8. Found "
            + $"{character} occurrence(s).");
    }

    /// <summary>⛔ THE MIGRATION IS COMPLETE — this inventory is now a CLOSED set of ONE, and that one is not a
    /// guard at all. Every genuine Tier-C guard has moved to <c>IsImageCapable</c>: CALL (both halves), the
    /// table-SORT key, STRING/UNSTRING INTO, INSPECT REPLACING/CONVERTING, ACCEPT (device and temporal), and the
    /// numeric-operand arm of <c>NumericRenderer</c> (that last one paired with DA6's §8.8.1.1 rejection, since it
    /// is reachable only under <c>--permissive</c>).
    /// <para>The test's job is now to keep the set closed: an ADDED use of the pre-V59 predicate is a regression
    /// toward the two-mechanism split that caused DA5, and a REMOVED one means the last non-guard use was touched.
    /// Either way a deliberate decision has to be recorded here rather than happening silently.</para></summary>
    [Fact]
    public void RemainingPreV59Guards_AreTheKnownInventory()
    {
        var expected = new Dictionary<string, int>
        {
            // ⛔ MoveEmitter.cs:144 is NOT a Tier-C guard and must NOT be "migrated". It selects a STRATEGY: when
            // the receiver is not a character image it first tries the memberwise leaf-copy fast path (used when
            // the source and receiver leaf LAYOUTS are positionally identical), and only the fall-through reaches
            // the real capability guard at line 168 — which is ALREADY on IsImageCapable. Verified: MOVE into a
            // COMP-containing group works in all three shapes (aligned memberwise, non-aligned image
            // redistribution, and from an alphanumeric source). Counted here so the inventory is complete, with
            // this note so nobody "fixes" a working fast path.
            ["CodeGen/Verbs/MoveEmitter.cs"] = 1,
        };

        var actual = new Dictionary<string, int>();
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            // The predicate's own definition and the StorageForm/pass plumbing that mirrors it are not emit guards.
            string rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            if (rel.StartsWith("Binding/Model/") || rel.StartsWith("Binding/Passes/")) continue;
            int n = CodeOccurrences(File.ReadAllText(f), "IsCharacterImage");
            if (n > 0) actual[rel] = n;
        }

        Assert.Equal(
            expected.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}").ToArray(),
            actual.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}").ToArray());
    }
}
