// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Editions;

/// <summary>How the compiler treats the source form a <see cref="NonstandardExtension"/> row names.</summary>
public enum ExtensionSupport
{
    /// <summary>ACCEPTED at every edition: the construct compiles and runs, and the §4.2.10 warning mechanism
    /// (<c>--flag-extensions</c>, COBOLNET2894) names each use. This is a CLAIMED extension — the one
    /// <c>docs/CONFORMANCE.md</c> §3.2 must identify.</summary>
    Accepted,

    /// <summary>RECOGNIZED and REFUSED by name: the words are reserved (§4.2.10 permits added reserved words, and
    /// requires the documentation to specify them) so the vendor construct can be refused with a named diagnostic
    /// rather than mis-parsed as user words, but no support is claimed. The row exists to document the reserved
    /// words, never to flag a use: a refused construct is already an error.</summary>
    RefusedByName,
}

/// <summary>
/// One NONSTANDARD EXTENSION this implementation recognizes — a row of the §4.2.10 register
/// (<see cref="NonstandardExtensionRegister"/>, kb/Work PB1525). ISO/IEC 1989:2023 §4.2.10 asks three things of an
/// implementation that has extensions, and this record is the one place all three are answered:
/// <list type="bullet">
/// <item>"Documentation associated with an implementation shall identify nonstandard extensions for which support is
/// claimed and shall specify any reserved words added for nonstandard extensions" — <c>docs/CONFORMANCE.md</c> §3.2 is
/// rendered by hand from these rows and held equal to them by <c>NonstandardExtensionRegisterDriftTests</c>, and
/// <see cref="ReservedWords"/> is held equal to <c>cobol-words.json</c>'s <c>extensionReserved</c>;</item>
/// <item>"An implementation shall provide a warning mechanism that optionally may be invoked by the user at compile
/// time to indicate use of a nonstandard extension in a compilation group" — <c>--flag-extensions</c>, read by the ONE
/// binder seam <c>EditionContext.Extension</c>;</item>
/// <item>"This warning mechanism shall flag only extensions that are syntactically distinguishable" —
/// <see cref="Distinguishable"/>: an extension that is a property of how the compiler is INVOKED rather than of the
/// source text (<c>--source-format auto</c>) is documented here and never flagged.</item>
/// </list>
/// ⛔ A NEW EXTENSION IS A ROW, never a bespoke decision: a usage spelling joins <see cref="Spellings"/> and the one
/// <c>PictureAnalyzer.ParseUsage</c> funnel flags it with no edit; a statement or phrase gets one
/// <c>EditionContext.Extension(ExtensionIds.X, …)</c> call at the ONE place its binder accepts it, and the drift test
/// refuses a <see cref="ExtensionSupport.Accepted"/> row that nothing in the compiler references.
/// </summary>
/// <param name="Id">The stable kebab-case id (<see cref="ExtensionIds"/> carries each as a constant).</param>
/// <param name="Display">The extension as a user would name it.</param>
/// <param name="Support">Whether the construct is accepted (a claimed extension) or only recognized and refused.</param>
/// <param name="Origin">Where the construct comes from — the dialect whose programs are written to it.</param>
/// <param name="Standard">The standard construct that does the same job, or the reason there is none; what a user
/// writes to leave the extension.</param>
/// <param name="Distinguishable">§4.2.10's "syntactically distinguishable": true when the compilation group's TEXT
/// shows the extension. False means documented, never flagged.</param>
/// <param name="Spellings">The USAGE spellings (upper case) the usage funnel recognizes this extension by; empty for
/// every row that is not a usage.</param>
/// <param name="ReservedWords">The words this implementation reserves at every edition on this row's account
/// (§4.2.10: "reserved words added for nonstandard extensions").</param>
public sealed record NonstandardExtension(
    string Id, string Display, ExtensionSupport Support, string Origin, string Standard, bool Distinguishable,
    IReadOnlyList<string> Spellings, IReadOnlyList<string> ReservedWords);

/// <summary>The id of every <see cref="NonstandardExtension"/> row, as a constant a use site can name — so a call is
/// a compile-checked reference and the drift test can find every one by text.</summary>
public static class ExtensionIds
{
    public const string UsageComp1 = "usage-comp-1";
    public const string UsageComp2 = "usage-comp-2";
    public const string UsageComp3 = "usage-comp-3";
    public const string UsageComp4 = "usage-comp-4";
    public const string UsageComp5 = "usage-comp-5";
    public const string GobackReturning = "goback-returning";
    public const string SetProgramPointerToEntry = "set-program-pointer-to-entry";
    public const string SourceFormatAuto = "source-format-auto";
    public const string VendorStatementWords = "vendor-statement-words";
}

/// <summary>
/// THE §4.2.10 REGISTER (kb/Work PB1525): every nonstandard extension this implementation recognizes, as data. See
/// <see cref="NonstandardExtension"/> for the three obligations it answers. Held equal to its documentation and to
/// the reserved-word table by <c>NonstandardExtensionRegisterDriftTests</c>.
/// <para><b>What is NOT a row, and why.</b> The §4.2.10 class-3 behaviours with a standard default and an
/// implementor-defined selector (<c>--sign-encoding</c>, §13.18.52.4 GR4/GR5 — implementor latitude, not an
/// extension) and the constructs <c>--permissive</c> accepts (each a VIOLATION of a standard rule that the user opts
/// into and that is flagged, unconditionally, by its own named warning through <c>EditionContext.Removed</c> — the
/// §4.2.10 warning mechanism for them already). A row is a construct that is accepted <i>silently</i>, or an option
/// that changes what the source means.</para>
/// </summary>
public static class NonstandardExtensionRegister
{
    private const string MainframeAndMicroFocusAndGnu = "IBM Enterprise COBOL, Micro Focus and GnuCOBOL";

    private static NonstandardExtension Usage(string id, string display, string standard, string n) => new(
        id, display, ExtensionSupport.Accepted, MainframeAndMicroFocusAndGnu, standard, Distinguishable: true,
        Spellings: [$"COMP-{n}", $"COMPUTATIONAL-{n}"], ReservedWords: [$"COMP-{n}", $"COMPUTATIONAL-{n}"]);

    /// <summary>Every row, in the order <c>docs/CONFORMANCE.md</c> §3.2 lists them.</summary>
    public static readonly IReadOnlyList<NonstandardExtension> Entries =
    [
        Usage(ExtensionIds.UsageComp1, "USAGE COMP-1 / COMPUTATIONAL-1 (single-precision floating point)",
            "USAGE FLOAT-SHORT (ISO §13.18.60.4 GR13)", "1"),
        Usage(ExtensionIds.UsageComp2, "USAGE COMP-2 / COMPUTATIONAL-2 (double-precision floating point)",
            "USAGE FLOAT-LONG (ISO §13.18.60.4 GR13)", "2"),
        Usage(ExtensionIds.UsageComp3, "USAGE COMP-3 / COMPUTATIONAL-3 (packed decimal)",
            "USAGE PACKED-DECIMAL (ISO §13.18.60.4)", "3"),
        Usage(ExtensionIds.UsageComp4, "USAGE COMP-4 / COMPUTATIONAL-4 (binary)",
            "USAGE BINARY, or COMP / COMPUTATIONAL, which ISO §13.18.60.3 SR6 defines as the abbreviation", "4"),
        Usage(ExtensionIds.UsageComp5, "USAGE COMP-5 / COMPUTATIONAL-5 (native binary)",
            "the fixed-width USAGE BINARY-CHAR, BINARY-SHORT, BINARY-LONG or BINARY-DOUBLE (ISO §13.18.60.4)", "5"),
        new(ExtensionIds.GobackReturning, "GOBACK RETURNING / GIVING identifier", ExtensionSupport.Accepted,
            "GnuCOBOL and Micro Focus",
            "move the value to the RETURNING item of the PROCEDURE DIVISION header, then GOBACK (ISO §14.9.18.2 has "
            + "no RETURNING phrase)",
            Distinguishable: true, Spellings: [], ReservedWords: []),
        new(ExtensionIds.SetProgramPointerToEntry, "SET program-pointer TO ENTRY literal-or-identifier",
            ExtensionSupport.Accepted, "IBM Enterprise COBOL and Micro Focus",
            "SET program-pointer TO ADDRESS OF PROGRAM program-name (ISO §14.9.39.2 Format 9, §8.4.3.13)",
            Distinguishable: true, Spellings: [], ReservedWords: []),
        new(ExtensionIds.SourceFormatAuto, "--source-format auto (reference-format detection)",
            ExtensionSupport.Accepted, "this implementation",
            "state the format: --source-format fixed or free, or a >>SOURCE FORMAT directive (ISO §7.3.24.3)",
            Distinguishable: false, Spellings: [], ReservedWords: []),
        new(ExtensionIds.VendorStatementWords, "ENTRY, JSON GENERATE/PARSE, XML GENERATE/PARSE (words reserved, constructs refused)",
            ExtensionSupport.RefusedByName, MainframeAndMicroFocusAndGnu,
            "no standard construct: each statement is refused by name and no support is claimed",
            Distinguishable: true, Spellings: [], ReservedWords: ["END-JSON", "END-XML", "ENTRY", "JSON", "XML"]),
    ];

    private static Dictionary<string, NonstandardExtension>? _byId;
    private static Dictionary<string, NonstandardExtension>? _bySpelling;

    /// <summary>The row with this id; an unknown id is a compiler defect (a use site naming a row that does not
    /// exist), never a user error.</summary>
    public static NonstandardExtension Get(string id) =>
        (_byId ??= Entries.ToDictionary(e => e.Id, StringComparer.Ordinal)) is var map && map.TryGetValue(id, out var row)
            ? row
            : throw new ArgumentException($"no nonstandard-extension row '{id}' (NonstandardExtensionRegister.Entries)", nameof(id));

    /// <summary>The ACCEPTED usage row whose <see cref="NonstandardExtension.Spellings"/> include
    /// <paramref name="usageWord"/> (compared by the Annex C fold, <see cref="CobolNames.Comparer"/>), or null — the
    /// lookup the one usage funnel makes, so a new usage spelling is flagged by adding it to its row and nothing else.</summary>
    public static NonstandardExtension? ForUsageSpelling(string usageWord)
    {
        _bySpelling ??= Entries.Where(e => e.Support == ExtensionSupport.Accepted)
            .SelectMany(e => e.Spellings.Select(s => (s, e)))
            .ToDictionary(t => t.s, t => t.e, CobolNames.Comparer);
        return _bySpelling.GetValueOrDefault(usageWord);
    }
}
