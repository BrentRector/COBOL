// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ START's GR17 a) TEMPORARY KEY AREA IS CUT OUT OF THE RECORD AREA, NEVER BUILT OUT OF data-name-1
/// (kb/Work PB355).
/// </summary>
/// <remarks>
/// <para>
/// ISO §14.9.41.4 GR17 a) — "The specified key is set up by moving the relevant parts of the record area into a
/// temporary data area" — names TWO things and neither is data-name-1: the SOURCE is the record area, and the
/// key is GR16's key of reference ("The key specified in the KEY phrase, or that shares a leftmost character
/// with the data item specified in the KEY phrase, becomes the key of reference"). data-name-1's whole role is
/// to name that key and, through GR17 b), to supply a default LENGTH — "the length specified in the LENGTH
/// clause, if specified, or else the length of record-key-name-1, if specified, or else the length of
/// data-name-1".
/// </para>
/// <para>
/// The two readings agree for every temporary area no longer than data-name-1, which is why the defect survived
/// the whole generic-key corpus: a generic key's own content IS the record area's content at those positions
/// (§13.18.33.4 GR3 makes the FD's level-1 entries "implicit redefinitions of the same area"). They part company
/// the moment <c>WITH LENGTH</c> counts PAST the operand into the rest of the key — where the shipped emitter
/// sent data-name-1's own storage image and the connector then PADDED it with spaces, searching for characters
/// nobody wrote instead of the ones the record area holds.
/// </para>
/// <para>
/// The fix is the CHANNEL, not the padding expression: <c>KeyedIoEmitter.EmitStart</c> sends the record-area
/// image and <c>IndexedConnector.Start</c> slices the key of reference out of it with the SAME <c>KeyOf</c> the
/// random READ (§14.9.30.4 GR32) and DELETE (§14.9.10.4 GR3) use. That is what this file pins — one extraction
/// for three verbs, so a fourth key-valued verb cannot grow a private one, and no arm of START can reintroduce
/// a manufactured character. The BEHAVIOUR is pinned by the corpus goldens
/// <c>tests/conformance/2002/pb355_start_length_past_operand.cob</c> and
/// <c>tests/conformance/2002/pb355_start_length_national_past_operand.cob</c>.
/// </para>
/// <para>
/// ⛔ <see cref="TheTemporaryAreaChecks_ActuallyFail_OnThePb355Shape"/> drives every predicate with the defect's
/// own source AND with the current shape (<c>feedback_green_gates_arent_evidence</c>): a structural gate that
/// never looked at anything is indistinguishable from one that works.
/// </para>
/// </remarks>
public sealed class StartTemporaryKeyAreaDriftTests
{
    private static string IndexedConnectorSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", "IndexedConnector.cs"));

    private static string KeyedIoEmitterSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "KeyedIoEmitter.cs"));

    // ── the pure predicates (driven by the real sources below, and by fabricated ones in the failure proof) ──

    /// <summary>True when the connector's KEY-phrase START builds its first temporary area by extracting the key
    /// of reference from the passed RECORD-AREA image, and invents no character of its own. <c>PadRight</c> /
    /// <c>PadLeft</c> anywhere in the body is the defect's signature: GR17 a) moves what the area HOLDS, and the
    /// only padding the model admits is <see cref="string"/>-level fill of the AREA itself, which
    /// <c>Fit</c>/<c>KeyOf</c> already own — reached through <c>AreaKey</c>, the ONE record-area key reader the
    /// random READ and DELETE share (it fits the area, except where a key's position varies with the record —
    /// kb/Work PB1025).</summary>
    internal static bool StartCutsTheTemporaryAreaOutOfTheRecordArea(string source)
    {
        string body = StartKeyPhraseBody(source);
        return body.Length > 0
            && body.Contains("AreaKey(keyedRecordImage, areaExtents, keyIndex)[..compareLength]", StringComparison.Ordinal)
            && !body.Contains("PadRight", StringComparison.Ordinal)
            && !body.Contains("PadLeft", StringComparison.Ordinal);
    }

    /// <summary>True when the KEY-phrase START's comparand PARAMETER is the record-area image and not an operand
    /// rendering — the name is the contract the three key-valued verbs share (<c>ReadKeyed</c> and the keyed
    /// <c>Delete</c> spell it the same way).</summary>
    internal static bool StartTakesTheRecordAreaImage(string source) =>
        // Matched up to the comparand only: the LENGTH parameter's type is PB357's (StartKeyLength), not this file's.
        source.Contains("public string Start(int keyIndex, string op, string keyedRecordImage, ",
            StringComparison.Ordinal);

    /// <summary>True when the emitter hands <c>FileStartIndexed</c> the RECORD AREA (through
    /// <c>ReferenceResolver.RecordArea</c> and the ONE <c>OperandText.RecordSendImage</c> channel) rather than a
    /// rendering of the START operand.</summary>
    internal static bool EmitStartSendsTheRecordArea(string source)
    {
        int call = source.IndexOf("RuntimeApi.FileStartIndexed(", StringComparison.Ordinal);
        if (call < 0) return false;
        int end = source.IndexOf(";\");", call, StringComparison.Ordinal);
        if (end < 0) end = Math.Min(source.Length, call + 400);
        string site = source[call..end];
        return !site.Contains("OperandText.AsStorageImage", StringComparison.Ordinal)
            && !site.Contains("OperandText.AsString", StringComparison.Ordinal)
            && source.Contains("refs.RecordArea(file) is { } ar", StringComparison.Ordinal)
            && source.Contains("OperandText.RecordSendImage(ar)", StringComparison.Ordinal);
    }

    /// <summary>The body of the KEY-phrase START, matched by signature PREFIX so a later parameter rename cannot
    /// silently empty it.</summary>
    private static string StartKeyPhraseBody(string source) =>
        StartKeyOfReferenceDriftTests.MemberBody(source, "public string Start(int keyIndex, string op, string ");

    // ── the gates ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>§14.9.41.4 GR17 a) — the moved characters come from the record area.</summary>
    [Fact]
    public void IndexedStart_BuildsTheTemporaryArea_FromTheRecordArea()
    {
        Assert.True(StartCutsTheTemporaryAreaOutOfTheRecordArea(IndexedConnectorSource()),
            "IndexedConnector.Start must build its search key as AreaKey(keyedRecordImage, areaExtents, keyIndex) "
            + "truncated to compareLength, and must pad nothing. ISO §14.9.41.4 GR17 a) — \"The specified key is "
            + "set up by moving the relevant parts of the record area into a temporary data area\" — sources it "
            + "from the RECORD AREA at GR16's key of reference; padding data-name-1's own content to the GR17 b) "
            + "length searches for invented spaces (kb/Work PB355).");
    }

    /// <summary>The parameter says what it carries — the same word the other two key-valued verbs use.</summary>
    [Fact]
    public void IndexedStart_TakesTheRecordAreaImage_NotAnOperandRendering()
    {
        Assert.True(StartTakesTheRecordAreaImage(IndexedConnectorSource()),
            "IndexedConnector.Start's comparand parameter must be `string keyedRecordImage` — the record area, "
            + "the same image ReadKeyed and Delete take, because §14.9.41.4 GR17 a), §14.9.30.4 GR32 and "
            + "§14.9.10.4 GR3 all take their key value out of it (kb/Work PB355).");
    }

    /// <summary>The emit boundary is where the defect lived: the connector cannot slice an area it never got.</summary>
    [Fact]
    public void EmitStart_SendsTheRecordArea_NotTheOperand()
    {
        Assert.True(EmitStartSendsTheRecordArea(KeyedIoEmitterSource()),
            "KeyedIoEmitter.EmitStart must pass OperandText.RecordSendImage of ReferenceResolver.RecordArea(file) "
            + "to RuntimeApi.FileStartIndexed. Sending OperandText.AsStorageImage(sta.Operand) makes the record-area "
            + "slice §14.9.41.4 GR17 a) names unavailable to the connector at all (kb/Work PB355).");
    }

    /// <summary>⛔ The failure proof: every predicate must DISCRIMINATE, not merely return a constant.</summary>
    [Fact]
    public void TheTemporaryAreaChecks_ActuallyFail_OnThePb355Shape()
    {
        // The PB355 defect, verbatim in shape: the operand's own content, space-padded to the count.
        const string defectiveConnector = """
                public string Start(int keyIndex, string op, string operand, int compareLength)
                {
                    int keyLength = keyIndex < 0 ? _primeLen : _alts[keyIndex].Len;
                    if (compareLength < 1 || compareLength > keyLength) return StartFail();
                    string value = operand.Length >= compareLength
                        ? operand[..compareLength] : operand.PadRight(compareLength, ' ');
                    return StartFail();
                }
            """;
        Assert.False(StartCutsTheTemporaryAreaOutOfTheRecordArea(defectiveConnector));
        Assert.False(StartTakesTheRecordAreaImage(defectiveConnector));

        // The half-fix that keeps the invention: the area arrives, and the body still pads a short slice.
        const string halfFixedConnector = """
                public string Start(int keyIndex, string op, string keyedRecordImage, int compareLength)
                {
                    string key = KeyOf(Fit(keyedRecordImage), keyIndex);
                    string value = key.Length >= compareLength
                        ? key[..compareLength] : key.PadRight(compareLength, ' ');
                    return StartFail();
                }
            """;
        Assert.True(StartTakesTheRecordAreaImage(halfFixedConnector));
        Assert.False(StartCutsTheTemporaryAreaOutOfTheRecordArea(halfFixedConnector));

        // The emitter half of the defect: the area resolved for something else while the call still sends the
        // operand — the two-arm shape this repository reproduces most often.
        const string defectiveEmitter = """
                Place? area = refs.RecordArea(file) is { } ar ? ar : null;
                w.Line($"var {st} = {RuntimeApi.FileStartIndexed(name, sta.KeyIndex, CsLiteral(sta.Op), OperandText.AsStorageImage(sta.Operand!, "START key operand"), len)};");
            """;
        Assert.False(EmitStartSendsTheRecordArea(defectiveEmitter));

        // …and the predicates must ACCEPT the shipped sources, or they are measuring nothing.
        string connector = IndexedConnectorSource(), emitter = KeyedIoEmitterSource();
        Assert.True(StartCutsTheTemporaryAreaOutOfTheRecordArea(connector));
        Assert.True(StartTakesTheRecordAreaImage(connector));
        Assert.True(EmitStartSendsTheRecordArea(emitter));
        Assert.False(StartCutsTheTemporaryAreaOutOfTheRecordArea("nothing resembling a connector"));
        Assert.False(StartTakesTheRecordAreaImage("nothing resembling a connector"));
        Assert.False(EmitStartSendsTheRecordArea("nothing resembling an emitter"));
    }
}
