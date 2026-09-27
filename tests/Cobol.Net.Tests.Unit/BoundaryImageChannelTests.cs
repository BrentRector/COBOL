// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ONE-READER LAW FOR GROUP IMAGES, HELD STRUCTURALLY (kb/Work PB177 + PB178).
/// <para>The rule, stated once in <c>PlaceRenderer.GroupImage</c>'s own doc-comment — "THE ONE reader — a
/// consumer that spells <c>.AsImage()</c> itself is wrong for the window shape" — is: the character image of a
/// group operand is the struct's generated <c>AsImage()</c>, EXCEPT that a Tier-B / BASED string-canonical
/// view's <c>Read</c> ALREADY IS the image (§13.18.44.4 GR1 — one storage area), an <c>OdoGroupPlace</c> must
/// be unwrapped first (and in a SENDING context sliced to §13.18.38.4 GR8's current extent), and an imageless
/// group stages the Tier-C loud. <c>PlaceRenderer</c>'s <c>GroupImage</c> / <c>SendingGroupImage</c> /
/// <c>WriteGroupImage</c> / <c>WriteFullGroupImage</c> state all four arms; every consumer routes through
/// them.</para>
/// <para><b>Why a SOURCE-LEVEL test.</b> The law was already written down and FOUR consumers spelled the call
/// themselves anyway, each missing a different arm: <c>OperandText.AsStorageImage</c> had no RedefViewPlace arm
/// (CS1061 on <c>CobolStr.RefMod(...).AsImage()</c> — PB178), <c>NumericRenderer.FieldNumCore</c> had no ODO
/// unwrap AND returned the maximum image instead of GR8's current extent (PB178's sibling),
/// <c>OoEmitter.EmitMethod</c> had no arm and no capability guard at all (CS1061 on a method formal group with
/// a POINTER leaf — PB177 arm A), and <c>OperandText.FieldAsString</c> was complete only by luck of three
/// separate early returns. A doc-comment cannot stop the fifth copy; this can. Rule 5: prefer the shape that
/// makes the NEXT boundary automatic, and pair it with a drift test so "automatic" stays true.</para>
/// <para>⛔ THIS TEST HAS BEEN RED. Run against the tree before PB177 arm A landed it reported the three
/// <c>OoEmitter</c> sites verbatim; a drift test that has never failed is not evidence
/// (<c>feedback_green_gates_arent_evidence</c>).</para>
/// </summary>
public sealed class BoundaryImageChannelTests
{
    // The generated codec's DEFINITIONS live here (CodeGen/DataDivision) and are correct by definition — this is
    // the emitter that WRITES the AsImage/FromImage/AsBits/FromBits members. PlaceRenderer (CodeGen/Roslyn) is
    // THE ONE READER/WRITER itself. Neither directory is scanned; only the CONSUMER directories are.
    private static readonly string[] ConsumerDirs = ["CodeGen/Emit", "CodeGen/Verbs"];

    /// <summary>The image-channel member names a consumer must never spell for itself. <c>AsImage</c>/
    /// <c>FromImage</c> are the character-image pair; <c>AsBits</c>/<c>FromBits</c> are the bit-group
    /// (§13.18.29.4 GR1b) pair, which has the identical window/ODO/capability problem; <c>AsNat</c>/
    /// <c>FromNat</c> are the NATIONAL-group (§13.18.29.4 GR2b) pair, which has it too.
    /// <para>⛔ THE NATIONAL PAIR WAS MISSING FROM THIS LIST until kb/Work PB678, and that is the two-arm
    /// shape this file exists to catch, one level up: kb/Work PB327 created <c>AsNat()</c>/<c>FromNat()</c> as
    /// the national twin of <c>AsImage()</c>/<c>FromImage()</c> — same window problem, same ODO problem, same
    /// <c>PlaceRenderer.SendingNat</c>/<c>WriteNat</c> one-reader pair — and the law's own enforcement covered
    /// only the alphanumeric and bit halves. A consumer could have spelled <c>.AsNat()</c> for itself and this
    /// test would have stayed green.</para></summary>
    private static readonly string[] Members =
        [".AsImage()", ".FromImage(", ".AsBits()", ".FromBits(", ".AsNat()", ".FromNat("];

    /// <summary>⛔ THE ALLOW-LIST IS THE HAND-MAINTAINED PART, so it is keyed on the TYPE plus a written
    /// justification and it must stay tiny. An addition without a justification is a review failure, not a
    /// mechanical pass (rule 5's warning about hand-maintained lists applies to this list itself).</summary>
    private static readonly Dictionary<string, (int Count, string Why)> AllowList = new()
    {
        // SortEmitter.TableCompare renders `a.{MemberPath}` STRUCT RVALUES (the sort comparer's two element
        // parameters), never a Place — so no window, ODO or Tier-C shape can reach it and there is no Place to
        // hand PlaceRenderer. Its own IsImageCapable guard sits one line above. Self-spelled by NECESSITY.
        // The count is SIX because §14.9.40.4 GR5 gives a key of each CLASS its own operand face and the
        // comparer must read all three: AsImage() for an ordinary group (§8.8.4.2.1 — "an alphanumeric group item shall be treated as an elementary alphanumeric data item"; NOT §8.8.4.2.3 SR2, the identifier-CLASS syntax rule — kb/Work PB741),
        // AsNat() for a GROUP-USAGE NATIONAL group (§13.18.29.4 GR2b) and AsBits() for a GROUP-USAGE BIT one
        // (GR1b), each spelled twice (the two element parameters). kb/Work PB678.
        ["CodeGen/Verbs/SortEmitter.cs"] = (6, "TableCompare compares struct rvalues, not Places"),
        // ⛔ OperandText.FieldAsString's bit-group row IS GONE — routed, not re-justified. It read
        // `Read(p).AsBits()` for itself and was exempted on the promise "kb/Work PB173 gives the bit channel its
        // own Place subtype; when that lands this row is the next one to route". PB173 landed and the row was
        // NOT routed, which is exactly the shape this list exists to catch: the self-spelled call sat BELOW the
        // OdoGroupPlace early return, so a bit group carrying an occurs-depending table took the CHARACTER
        // reader — two alphabets on one operand, at a character-unit extent that came out negative. It now calls
        // PlaceRenderer.SendingBits, THE ONE bit reader. A justification that promises a future routing is a
        // debt; this row is the evidence the debt gets paid rather than renewed.
    };

    /// <summary>Occurrences in CODE only — these members are named constantly in the doc-comments that explain
    /// the law (that is the comments' job), so a raw text count is a false positive. Same stripper as
    /// <see cref="V59ImagePredicateDriftTests"/>.</summary>
    private static int CodeOccurrences(string src, string needle)
    {
        var code = new List<string>();
        foreach (string raw in src.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            if (t.StartsWith("///") || t.StartsWith("//")) continue;
            int i = line.IndexOf("//", StringComparison.Ordinal);
            code.Add(i >= 0 ? line[..i] : line);
        }
        return Regex.Matches(string.Join("\n", code), Regex.Escape(needle)).Count;
    }

    [Fact]
    public void NoConsumerSpellsTheImageChannelItself()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        var actual = new Dictionary<string, int>();
        foreach (string dir in ConsumerDirs)
        {
            string full = Path.Combine(root, dir.Replace('/', Path.DirectorySeparatorChar));
            foreach (string f in Directory.EnumerateFiles(full, "*.cs", SearchOption.AllDirectories))
            {
                string src = File.ReadAllText(f);
                int n = Members.Sum(m => CodeOccurrences(src, m));
                if (n > 0) actual[Path.GetRelativePath(root, f).Replace('\\', '/')] = n;
            }
        }

        var expected = AllowList.ToDictionary(k => k.Key, k => k.Value.Count);
        Assert.Equal(
            expected.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}").ToArray(),
            actual.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}").ToArray());
    }

    /// <summary>The ONE reader and the ONE writer both carry the capability guard and both arms of the window /
    /// ODO dispatch. If a future edit deletes an arm from one and not the other, that is the two-arm-dispatch
    /// shape this repo reproduces more than any other — assert the pairing directly.</summary>
    [Fact]
    public void PlaceRenderer_ReadAndWriteGroupImage_BothCarryEveryArm()
    {
        string src = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "CodeGen", "Roslyn", "PlaceRenderer.cs"));
        foreach (string reader in new[] { "GroupImage", "WriteGroupImage" })
        {
            // The arm list is spelled inside each switch; both must name the window shape, the ODO wrapper and
            // the capability predicate.
            int at = src.IndexOf($"string {reader}(", StringComparison.Ordinal);
            Assert.True(at > 0, $"PlaceRenderer.{reader} not found — the ONE {(reader.StartsWith("Write") ? "writer" : "reader")} was renamed or removed.");
            string body = src[at..(src.IndexOf("};", at, StringComparison.Ordinal) + 2)];
            Assert.Contains("RedefViewPlace", body);
            Assert.Contains("OdoGroupPlace", body);
            // ⛔ The capability is asked of the OPERAND (Place.ImageCapable), never of the declaring entry
            // (kb/Work PB189): `group.Item.IsImageCapable` refused a subscripted dynamic-capacity-table ELEMENT —
            // an ordinary fixed-length group, §8.5.1.12.1 — although its struct carries AsImage().
            Assert.Contains("group.ImageCapable", body);
            Assert.DoesNotContain("Item.IsImageCapable", body);
        }
    }

    /// <summary>⛔ THE §13.18.38.4 GR8 DIRECTION LAW IS WRITTEN ONCE (kb/Work PB202). GR8a and GR8b agree that a
    /// SENDING occurs-depending group uses only its current-count part; only a depending-INSIDE group as a
    /// RECEIVING operand takes the maximum. The ref-mod READ arm used to spell its own
    /// <c>OdoGroupPlace { DependingInside: false }</c> test and read the MAXIMUM image of a depending-inside group,
    /// while the plain MOVE path (through <c>SendingGroupImage</c>) was right — one rule, two spellings, one wrong.
    /// So: no <c>DependingInside</c> pattern anywhere in the renderer but <c>UsesCurrentExtent</c>; the ref-mod read
    /// arm is the SENDING view; and the ref-mod RECEIVER's splice base is the RECEIVING view on every channel (its
    /// sending-direction read truncated a depending-inside national group under the maximum-length store).</summary>
    [Fact]
    public void PlaceRenderer_Gr8DirectionLaw_IsOnePredicate_AndTheRefModArmsAskIt()
    {
        string src = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "CodeGen", "Roslyn", "PlaceRenderer.cs"));
        Assert.DoesNotContain("DependingInside: false", src);
        Assert.Contains("dir == AccessDir.Sending || !o.DependingInside", src);
        Assert.Contains("GroupImagePlace g => GroupImageAs(g.Inner, AccessDir.Sending,", src);
        Assert.Contains("RefModPlace r => Write(r.Inner, RuntimeApi.StrSpliceInto(SpliceBase(r.Inner)", src);
        Assert.Contains("Write(p.Inner, RuntimeApi.StrSpliceInto(SpliceBase(p.Inner)", src);
        foreach (string arm in new[] { "GroupImagePlace g => GroupImageAs(g.Inner, AccessDir.Receiving",
                                       "BitImagePlace b => BitsAs(b.Inner, AccessDir.Receiving)",
                                       "NatImagePlace n => NatAs(n.Inner, AccessDir.Receiving)" })
            Assert.Contains(arm, src);
    }

    /// <summary>The BIT channel is a two-arm dispatch too (kb/Work PB173): a <c>BitImagePlace</c> without BOTH
    /// a <c>Read</c> arm and a <c>Write</c> arm is a runtime <c>Unhandled</c> throw, not a compile error — the
    /// <c>_ =&gt; throw Unhandled(p)</c> default swallows it. Assert the pair directly. (The units this place
    /// carries — BIT positions, §8.4.3.3.4 GR5a — are why it is a separate type from
    /// <c>GroupImagePlace</c>, and a NATIONAL group deliberately keeps the latter, §13.18.29.4 GR2b.)
    /// <para>⛔ Both arms must DELEGATE to <c>SendingBits</c>/<c>WriteBits</c> rather than spell the member: the
    /// bit channel has the same §13.18.38.4 GR8 current-extent split the image channel has, and the first cut of
    /// these arms spelled <c>Read(b.Inner).AsBits()</c> / <c>.FromBits(rhs)</c> with NO ODO arm on either side —
    /// so a bit group holding an occurs-depending table read and wrote its MAXIMUM bit positions. The ONE
    /// reader/writer pair is asserted the same way <see cref="PlaceRenderer_ReadAndWriteGroupImage_BothCarryEveryArm"/>
    /// asserts the image pair's arms.</para></summary>
    [Fact]
    public void PlaceRenderer_BitImagePlace_HasBothARead_AndAWriteArm()
    {
        string src = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "CodeGen", "Roslyn", "PlaceRenderer.cs"));
        Assert.Contains("BitImagePlace b => SendingBits(b.Inner)", src);
        Assert.Contains("BitImagePlace b => WriteBits(b.Inner, rhs)", src);
        // SendingBits delegates to the direction-parameterized BitsAs (kb/Work PB202 — the GR8 direction law), and
        // WriteBits to its direction-parameterized twin BitsWrite (kb/Work PB1128 — WriteGroupValue stores INSPECT's
        // identifier-1 over the SENDING extent), so the arm-carrying bodies are BitsAs and BitsWrite.
        Assert.Contains("string SendingBits(Place group) => BitsAs(group, AccessDir.Sending);", src);
        Assert.Contains("string WriteBits(Place group, string bits) => BitsWrite(group, bits, AccessDir.Receiving);", src);
        foreach (string one in new[] { "BitsAs", "BitsWrite" })
        {
            int at = src.IndexOf($"string {one}(", StringComparison.Ordinal);
            Assert.True(at > 0, $"PlaceRenderer.{one} not found — the ONE bit {(one.EndsWith("Write") ? "writer" : "reader")} was renamed or removed.");
            // Each must name the ODO wrapper (GR8's current extent) and the generated member it owns. The body
            // ends at the blank line before the next member (a `;` scan would stop inside an interpolated
            // string — the emitted `FromBits(…);` carries one).
            int end = src.IndexOf("\r\n\r\n", at, StringComparison.Ordinal);
            string body = end > at ? src[at..end] : src[at..];
            Assert.Contains("OdoGroupPlace", body);
            Assert.Contains("LengthExpr(o)", body);
        }
        // The ref-mod pad and the figurative fill are the other two-arm pair PB173 fixed; both must read
        // OperandPic (null-safe for a GROUP), never the raw Pic that made a bit-group slice space-fill. The
        // ACCEPT omitted-length width is the THIRD arm of that family.
        Assert.DoesNotContain("r.Inner.Item.Pic is { Category: PicCategory.Boolean }", src);
        string move = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "CodeGen", "Verbs", "MoveEmitter.cs"));
        Assert.Contains("rmp.Inner.Item.OperandPic?.Category", move);
        Assert.DoesNotContain("rmp.Inner.Item.Pic?.Category", move);
        string acc = File.ReadAllText(TestRepo.At("src", "Cobol.Net.Compiler", "CodeGen", "Verbs", "AcceptDisplayEmitter.cs"));
        Assert.Contains("rm.Inner.Item.OperandPic?.Length ?? rm.Inner.Item.ImageWidth", acc);
        Assert.DoesNotContain("rm.Inner.Item.Pic?.Length ?? rm.Inner.Item.ImageWidth", acc);
    }
}
