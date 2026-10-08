// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Diagnostics;

/// <summary>
/// A descriptor for a specific diagnostic: code, default severity, message template.
/// </summary>
public sealed record DiagnosticDescriptor(
    string Code,
    DiagnosticSeverity DefaultSeverity,
    string MessageTemplate);

/// <summary>
/// The frontend's own diagnostic descriptors: the generic parse error (COBOL0001), the parser's syntax hints
/// (COBOL0100–0313, chosen in <c>CobolErrorStrategy</c>), the parse-layer twins of <c>DiagnosticCatalog</c>
/// descriptors (each COBOLNET twin takes its code from the catalogue descriptor, so the two cannot drift) and the
/// COPY preprocessing errors (CBL3620–3622). Every other compiler diagnostic is a <c>DiagnosticCatalog</c> descriptor
/// (<c>CobolNet.Editions.Diagnostics</c>).
/// </summary>
public static class DiagnosticDescriptors
{
    // ══════════════════════════════════════
    // COBOL0001: Generic parse error
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0001 = new("COBOL0001", DiagnosticSeverity.Error,
        "{0}");

    // ══════════════════════════════════════
    // COBOL0100–0109: Parser — unsupported feature warnings
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0100 = new("COBOL0100", DiagnosticSeverity.Warning,
        "ASCENDING/DESCENDING KEY is out of place: in an OCCURS clause the KEY phrases are written before INDEXED BY (ISO §13.18.38.2).");
    public static readonly DiagnosticDescriptor COBOL0101 = new("COBOL0101", DiagnosticSeverity.Warning,
        "BLANK WHEN ZERO may not be recognized. Check that it appears as a single clause on the data item.");
    public static readonly DiagnosticDescriptor COBOL0102 = new("COBOL0102", DiagnosticSeverity.Warning,
        "This SET form may not be supported. Supported forms: SET identifier TO value, SET condition TO TRUE/FALSE, SET index UP/DOWN BY integer.");
    public static readonly DiagnosticDescriptor COBOL0103 = new("COBOL0103", DiagnosticSeverity.Warning,
        "SEARCH statement may not be fully supported.");
    // COBOL0104 (OCCURS DEPENDING ON), 0105 (INSPECT CONVERTING) and 0106 (INITIALIZE REPLACING)
    // were "not yet supported" hints for features that now work; removed (DEVLOG 232).
    public static readonly DiagnosticDescriptor COBOL0107 = new("COBOL0107", DiagnosticSeverity.Warning,
        "EVALUATE with ALSO (multi-subject) may not be fully supported.");

    // ══════════════════════════════════════
    // COBOL0200–0201: Parser — reserved word conflicts
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0200 = new("COBOL0200", DiagnosticSeverity.Warning,
        "STATUS is a reserved word here. For file status, use 'FILE STATUS IS <data-name>'.");
    public static readonly DiagnosticDescriptor COBOL0201 = new("COBOL0201", DiagnosticSeverity.Warning,
        "PROGRAM is a reserved word. If this is a paragraph name, it cannot be named PROGRAM.");

    // ══════════════════════════════════════
    // COBOL0300–0312: Parser — syntax guidance
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOL0300 = new("COBOL0300", DiagnosticSeverity.Warning,
        "THROUGH/THRU is not recognized in this context. Check PERFORM or VALUE THROUGH syntax.");
    public static readonly DiagnosticDescriptor COBOL0303 = new("COBOL0303", DiagnosticSeverity.Warning,
        "In a MOVE statement, did you forget TO before the target?");
    public static readonly DiagnosticDescriptor COBOL0304 = new("COBOL0304", DiagnosticSeverity.Warning,
        "Missing period after paragraph name — the parser is treating it as a qualified reference.");
    public static readonly DiagnosticDescriptor COBOL0305 = new("COBOL0305", DiagnosticSeverity.Warning,
        "Unexpected token in SPECIAL-NAMES. Check implementor-name or mnemonic-name syntax.");
    public static readonly DiagnosticDescriptor COBOL0306 = new("COBOL0306", DiagnosticSeverity.Warning,
        "{0} appears without a matching {1} statement.");
    public static readonly DiagnosticDescriptor COBOL0307 = new("COBOL0307", DiagnosticSeverity.Warning,
        "A period may be missing at the end of the previous sentence.");
    public static readonly DiagnosticDescriptor COBOL0308 = new("COBOL0308", DiagnosticSeverity.Warning,
        "A data-name is expected here, not a literal.");
    public static readonly DiagnosticDescriptor COBOL0309 = new("COBOL0309", DiagnosticSeverity.Warning,
        "A literal value is expected here, not a data-name.");
    public static readonly DiagnosticDescriptor COBOL0310 = new("COBOL0310", DiagnosticSeverity.Warning,
        "Missing BY keyword. INDEXED BY requires 'INDEXED BY <index-name>'.");
    // COBOL0311 ("NOT = / NOT > / NOT < abbreviated condition not yet supported") was a hint for a
    // feature that now works; removed (DEVLOG 232).
    public static readonly DiagnosticDescriptor COBOL0312 = new("COBOL0312", DiagnosticSeverity.Warning,
        "Unexpected token in FILE-CONTROL paragraph. Check SELECT/ASSIGN TO syntax.");
    // W1.5 (VERSION_TEST_MATRIX_DESIGN P2.8): a non-ISO vendor statement (JSON/XML) behind a parse error —
    // NOT the 0900 edition band (no ISO edition has the construct; owner decision 2, DEVLOG 581).
    public static readonly DiagnosticDescriptor COBOL0313 = new("COBOL0313", DiagnosticSeverity.Error,
        "{0}");

    // ══════════════════════════════════════
    // COBOLNET2072/2073: the required imperative-statement operand and the misplaced WHEN OTHER (kb/Work PB396).
    // Parse-layer twins of catalogue descriptors — the CODE is single-sourced from DiagnosticCatalog so the two
    // registries cannot drift. The message is composed at the CobolErrorStrategy site (it names the enclosing
    // statement and the offending token).
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOLNET2072 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.RequiredImperativeMissing.Code,
        DiagnosticSeverity.Error, "{0}");
    public static readonly DiagnosticDescriptor COBOLNET2073 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.WhenOtherOutOfPosition.Code,
        DiagnosticSeverity.Error, "{0}");

    // ══════════════════════════════════════
    // COBOLNET2172/2173: a written shape NO general format of its clause prints (kb/Work PB412, PB421) — the
    // GO TO complement (§14.9.17.2) and a CORRESPONDING phrase after a MOVE sending operand (§14.9.25.2).
    // Same single-sourcing as 2072/2073 above: the CODE comes from the catalogue descriptor, the message is
    // composed at the CobolErrorStrategy site from the format's own cardinalities.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOLNET2172 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.GoToFormatShape.Code,
        DiagnosticSeverity.Error, "{0}");
    public static readonly DiagnosticDescriptor COBOLNET2173 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.MoveCorrespondingPosition.Code,
        DiagnosticSeverity.Error, "{0}");

    // ══════════════════════════════════════
    // COBOLNET2418/2419: the parse-layer twins of the figurative-spelling keyword refusal (kb/Work PB510) and the
    // §13.18.40.3 SR7 PICTURE separator-period rule (kb/Work PB569). Same single-sourcing as 2072/2073.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOLNET2418 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.FigurativeSpellingNotTheKeyword.Code,
        DiagnosticSeverity.Error, "{0}");
    public static readonly DiagnosticDescriptor COBOLNET2419 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.PictureTrailingSymbolNotLast.Code,
        DiagnosticSeverity.Error, "{0}");

    // COBOLNET2994: an inline method invocation's empty parenthesis pair (kb/Work PB1430) — the grammar requires the
    // argument list inside the optional parentheses, and CobolErrorStrategy names the refused shape.
    public static readonly DiagnosticDescriptor COBOLNET2994 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.InlineInvocationEmptyArguments.Code,
        DiagnosticSeverity.Error, "{0}");

    // ══════════════════════════════════════
    // COBOLNET2269: a statement written in a shape none of its general formats prints (kb/Work PB909) — the
    // parse-layer twin, for the shapes the grammar now REFUSES rather than admits and hands to the binder: the
    // SEARCH phrases no SEARCH format prints (NOT AT END, a KEY phrase, a second Format-2 WHEN; kb/Work PB446).
    // Same single-sourcing as 2172/2173: ONE code, the message composed at the CobolErrorStrategy site.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor COBOLNET2269 = new(
        CobolNet.Editions.Diagnostics.DiagnosticCatalog.StatementFormatShape.Code,
        DiagnosticSeverity.Error, "{0}");

    // ══════════════════════════════════════
    // CBL3620–3622: COPY preprocessing (ISO §7.2.3). All three are unconditional errors: CBL3620 is library
    // text that cannot be located (§7.2.3.4 GR1/GR2 — at every edition and on every path since kb/Work PB1355;
    // the search is the DOC-A.1-40 determination); CBL3621 a circular include; CBL3622 over-deep nesting.
    // ══════════════════════════════════════
    public static readonly DiagnosticDescriptor CBL3620 = new("CBL3620", DiagnosticSeverity.Error,
        "COPY copybook '{0}' not found. Searched: {1}");
    public static readonly DiagnosticDescriptor CBL3621 = new("CBL3621", DiagnosticSeverity.Error,
        "Circular COPY: copybook '{0}' is already being included");
    public static readonly DiagnosticDescriptor CBL3622 = new("CBL3622", DiagnosticSeverity.Error,
        "COPY nesting too deep (limit {0}); possible recursive include");
}
