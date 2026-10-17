// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY START FORMAT DECIDES THE KEY OF REFERENCE, AND THE EPILOGUE MAKES IT SAY SO (kb/Work PB356).
/// </summary>
/// <remarks>
/// <para>
/// ISO §14.9.41.4 states the decision once per format and never as a default: GR16 — "The key specified in the
/// KEY phrase, or that shares a leftmost character with the data item specified in the KEY phrase, becomes the
/// key of reference" — for the KEY phrase, and GR18/GR19 — "the file position indicator is set to the value of
/// the primary key of the first [last] existing logical record in the physical file and the key of reference is
/// set to the primary key" — for FIRST/LAST. A shared epilogue that read the connector's current
/// <c>_refKey</c> therefore has no correct reading: whichever format it is wrong for inherits the key of
/// reference the previous START or random READ left standing.
/// </para>
/// <para>
/// That is exactly what PB356 was. <c>IndexedConnector.StartFirstLast</c> ordered the file with
/// <c>Ordered(_refKey)</c> and never reset it, so a START LAST after a START on an ALTERNATE key answered the
/// alternate ordering's last record — with the prime and alternate orderings inverted, the FIRST record — and
/// left every following sequential READ walking the alternate (§14.9.30.4 GR21 b)'s "Otherwise, the key of
/// reference is set to the last key of reference in the file position indicator"). Where the alternate carried
/// SUPPRESS WHEN it additionally hid records §12.4.5.6.4 GR6 withholds from the ALTERNATE path only, which
/// GR18/GR19's primary-key view must include.
/// </para>
/// <para>
/// The fix is the shape, not the assignment: <c>StartSucceeded(int keyOfReference, KeyedRec found)</c> takes the
/// key of reference as a REQUIRED ARGUMENT, so a fourth START format cannot be written without answering the
/// question. This file is what keeps that true. The BEHAVIOUR is pinned by the corpus goldens
/// <c>conformance-suite/cases/14.9.41-START/pb356_start_first_last_indexed.cob</c> (the inverted orderings and the four
/// following prime-order READs) and <c>conformance-suite/cases/14.9.41-START/pb356_start_first_last_suppressed.cob</c> (the
/// SUPPRESS WHEN records FIRST/LAST must still see); this file pins the STRUCTURE, because an epilogue reading
/// shared mutable state is how the defect could exist at all.
/// </para>
/// <para>
/// ⛔ <see cref="TheEpilogueChecks_ActuallyFail_OnThePb356Shape"/> drives every predicate with the defect's own
/// source AND with the current shape (<c>feedback_green_gates_arent_evidence</c>): a structural gate that never
/// looked at anything is indistinguishable from one that works.
/// </para>
/// </remarks>
public sealed class StartKeyOfReferenceDriftTests
{
    private static string IndexedConnectorSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", "IndexedConnector.cs"));

    // ── the pure predicates (driven by the real source below, and by fabricated ones in the failure proof) ──

    /// <summary>The text of the member starting at <paramref name="signature"/>, up to the next member's
    /// declaration or doc comment at class-member indentation. Good enough to ask what ONE method's body
    /// mentions, which is all these predicates need.</summary>
    internal static string MemberBody(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        if (start < 0) return "";
        int end = source.Length;
        foreach (string boundary in MemberBoundaries)
        {
            int next = source.IndexOf(boundary, start + signature.Length, StringComparison.Ordinal);
            if (next >= 0 && next < end) end = next;
        }
        return source[start..end];
    }

    /// <summary>What ends a member for <see cref="MemberBody"/>: the next declaration or doc comment at
    /// class-member indentation. Nothing inside a method body is written at four spaces.</summary>
    private static readonly string[] MemberBoundaries =
        ["\n    /// ", "\n    public ", "\n    private ", "\n    protected ", "\n    internal "];

    /// <summary>True when the connector has ONE successful-START epilogue and it takes the key of reference as a
    /// parameter. <c>_positioner = 'S'</c> is the marker of that epilogue — §14.9.30.4 GR21 d) keys the whole
    /// following sequential walk on it — so a second occurrence IS a second epilogue.</summary>
    internal static bool OneStartEpilogueTakingTheKeyOfReference(string source)
    {
        const string sig = "private string StartSucceeded(int keyOfReference, KeyedRec found)";
        if (!source.Contains(sig, StringComparison.Ordinal)) return false;
        if (Occurrences(source, "_positioner = 'S'") != 1) return false;
        string body = MemberBody(source, sig);
        return body.Contains("_positioner = 'S'", StringComparison.Ordinal)
            && body.Contains("_refKey = keyOfReference;", StringComparison.Ordinal)
            && body.Contains("KeyOf(found, keyOfReference)", StringComparison.Ordinal);
    }

    /// <summary>True when START FIRST/LAST decides the key of reference itself — GR18/GR19's "set to the primary
    /// key" — rather than reading the one it inherited. The method may not mention <c>_refKey</c> AT ALL: both
    /// the ordering it searches and the key of reference it leaves are the prime key.</summary>
    internal static bool StartFirstLastNamesThePrimeKey(string source)
    {
        string body = MemberBody(source, "public string StartFirstLast(bool last)");
        return body.Length > 0
            && !body.Contains("_refKey", StringComparison.Ordinal)
            && body.Contains("Ordered(PrimeKey)", StringComparison.Ordinal)
            && body.Contains("StartSucceeded(PrimeKey", StringComparison.Ordinal);
    }

    /// <summary>True when the KEY-phrase START routes its success through the same epilogue instead of carrying
    /// a second copy of the file-position-indicator assignment.</summary>
    /// <remarks>The signature is matched by its PREFIX so that renaming a later parameter cannot silently empty
    /// <see cref="MemberBody"/> — the comparand parameter became <c>keyedRecordImage</c> with kb/Work PB355.</remarks>
    internal static bool KeyPhraseStartUsesTheEpilogue(string source) =>
        MemberBody(source, "public string Start(int keyIndex, string op, string ")
            .Contains("return StartSucceeded(keyIndex, found);", StringComparison.Ordinal);

    private static int Occurrences(string haystack, string needle)
    {
        int n = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            n++;
        return n;
    }

    // ── the gates ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One epilogue, and the key of reference is an argument of it.</summary>
    [Fact]
    public void IndexedStart_HasOneEpilogue_TakingTheKeyOfReferenceAsAnArgument()
    {
        Assert.True(OneStartEpilogueTakingTheKeyOfReference(IndexedConnectorSource()),
            "IndexedConnector must complete EVERY successful START through one "
            + "StartSucceeded(int keyOfReference, KeyedRec found). ISO §14.9.41.4 states the key-of-reference "
            + "decision once per format — GR16 for the KEY phrase, GR18/GR19's \"the key of reference is set to "
            + "the primary key\" for FIRST/LAST — so an epilogue that reads _refKey instead inherits whatever "
            + "the last START or random READ established (kb/Work PB356).");
    }

    /// <summary>§14.9.41.4 GR18/GR19 — the prime key both orders the search and becomes the key of reference.</summary>
    [Fact]
    public void StartFirstLast_OrdersAndAnswersOnThePrimeKey_NotTheInheritedOne()
    {
        Assert.True(StartFirstLastNamesThePrimeKey(IndexedConnectorSource()),
            "IndexedConnector.StartFirstLast must use Ordered(PrimeKey) and StartSucceeded(PrimeKey, …) and "
            + "must not mention _refKey. §14.9.41.4 GR18/GR19 name the primary key twice — \"the first [last] "
            + "existing logical record in the physical file\" is the PRIME ordering, and \"the key of reference "
            + "is set to the primary key\" is what the statement leaves for every following sequential READ. "
            + "§14.9.27.4 GR14 is about what OPEN establishes and does not authorize inheriting it here.");
    }

    /// <summary>The KEY-phrase START shares the epilogue — one rule, one place.</summary>
    [Fact]
    public void KeyPhraseStart_SharesTheOneEpilogue()
    {
        Assert.True(KeyPhraseStartUsesTheEpilogue(IndexedConnectorSource()),
            "IndexedConnector.Start(KEY rel-op) must return StartSucceeded(keyIndex, found) — a second copy of "
            + "the GR17 e) 1. file-position-indicator assignment is how the two formats came to disagree.");
    }

    /// <summary>⛔ The failure proof: every predicate must DISCRIMINATE, not merely return a constant.</summary>
    [Fact]
    public void TheEpilogueChecks_ActuallyFail_OnThePb356Shape()
    {
        // The PB356 defect, verbatim in shape: FIRST/LAST ordered by and answered with the INHERITED key of
        // reference, and its own inline epilogue.
        const string defective = """
                private const int PrimeKey = -1;

                public string Start(int keyIndex, string op, string operand, int compareLength)
                {
                    _refKey = keyIndex;
                    _fpiKey = KeyOf(found, keyIndex);
                    _fpiValid = true; _positioner = 'S';
                    return Status = FileStatusCode.Success;
                }

                public string StartFirstLast(bool last)
                {
                    var seq = Ordered(_refKey);
                    var rec = last ? seq[^1] : seq[0];
                    _fpiKey = KeyOf(rec.Image, _refKey);
                    _fpiValid = true; _positioner = 'S';
                    return Status = FileStatusCode.Success;
                }
            """;
        Assert.False(OneStartEpilogueTakingTheKeyOfReference(defective));
        Assert.False(StartFirstLastNamesThePrimeKey(defective));
        Assert.False(KeyPhraseStartUsesTheEpilogue(defective));

        // The half-fix that keeps the two epilogues apart: FIRST/LAST corrected, the KEY phrase left inline —
        // the two-arm dispatch with one arm fixed, this repository's most reproducible defect shape.
        const string halfFixed = """
                private const int PrimeKey = -1;

                public string Start(int keyIndex, string op, string operand, int compareLength)
                {
                    _refKey = keyIndex;
                    _fpiKey = KeyOf(found, keyIndex);
                    _fpiValid = true; _positioner = 'S';
                    return Status = FileStatusCode.Success;
                }

                public string StartFirstLast(bool last)
                {
                    var seq = Ordered(PrimeKey);
                    return StartSucceeded(PrimeKey, last ? seq[^1] : seq[0]);
                }

                private string StartSucceeded(int keyOfReference, KeyedRec found)
                {
                    _refKey = keyOfReference;
                    _fpiKey = KeyOf(found, keyOfReference);
                    _fpiValid = true; _positioner = 'S';
                    return Status = FileStatusCode.Success;
                }
            """;
        Assert.True(StartFirstLastNamesThePrimeKey(halfFixed));
        Assert.False(OneStartEpilogueTakingTheKeyOfReference(halfFixed));   // two 'S' epilogues
        Assert.False(KeyPhraseStartUsesTheEpilogue(halfFixed));

        // An epilogue that reads the field instead of its argument is the defect wearing the fix's shape.
        const string inheritingEpilogue = """
                public string StartFirstLast(bool last)
                {
                    var seq = Ordered(PrimeKey);
                    return StartSucceeded(PrimeKey, last ? seq[^1] : seq[0]);
                }

                private string StartSucceeded(int keyOfReference, KeyedRec found)
                {
                    _fpiKey = KeyOf(found, _refKey);
                    _fpiValid = true; _positioner = 'S';
                    return Status = FileStatusCode.Success;
                }
            """;
        Assert.False(OneStartEpilogueTakingTheKeyOfReference(inheritingEpilogue));

        // …and the predicates must ACCEPT the shipped source, or they are measuring nothing.
        string real = IndexedConnectorSource();
        Assert.True(OneStartEpilogueTakingTheKeyOfReference(real));
        Assert.True(StartFirstLastNamesThePrimeKey(real));
        Assert.True(KeyPhraseStartUsesTheEpilogue(real));
        Assert.False(OneStartEpilogueTakingTheKeyOfReference("nothing resembling a connector"));
        Assert.False(StartFirstLastNamesThePrimeKey("nothing resembling a connector"));
        Assert.False(KeyPhraseStartUsesTheEpilogue("nothing resembling a connector"));
    }
}
