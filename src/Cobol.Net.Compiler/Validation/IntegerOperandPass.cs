// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Editions;               // IDiagnosticSink / EditionDiagnostic / EditionSeverity
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog
using CobolNet.Frontend.Generated;     // CobolParserCore

namespace CobolNet.Validation;

using Core = CobolParserCore;

/// <summary>What one <c>integer-n</c> operand position of a general format admits.</summary>
internal enum IntegerSlotKind
{
    /// <summary>§5.5 1)'s default — "unsigned and nonzero". The unsigned half is the grammar's own shape
    /// (<c>integerLiteral : INTEGERLIT</c>); the nonzero half is this pass.</summary>
    NonZero,

    /// <summary>An associated rule "otherwise specifies": zero is permitted, and <see cref="IntegerSlot.Basis"/>
    /// names the rule that says so.</summary>
    ZeroPermitted,

    /// <summary>The grammar spells the operand with <c>integerLiteral</c>, but the standard does not print it as
    /// an <c>integer-n</c> of an ISO general format this pass governs — its range is its own rule, owned elsewhere
    /// (<see cref="IntegerSlot.Basis"/> says which and where).</summary>
    NotGoverned,
}

/// <summary>One operand position's classification, and the rule (or reason) that decides it.</summary>
internal readonly record struct IntegerSlot(IntegerSlotKind Kind, string Basis)
{
    internal static readonly IntegerSlot Default = new(IntegerSlotKind.NonZero, "ISO §5.5 1)");
    internal static IntegerSlot Zero(string basis) => new(IntegerSlotKind.ZeroPermitted, basis);
    internal static IntegerSlot Not(string reason) => new(IntegerSlotKind.NotGoverned, reason);
}

/// <summary>
/// ⛔ THE ONE SCREEN for ISO §5.5 1) — "When the term 'integer-n' (n = 1, 2, …) is used in a general format and
/// associated rules, it refers to a fixed-point integer literal that shall be unsigned and nonzero unless otherwise
/// specified in the associated rules." (kb/Work PB859.)
///
/// <para>The rule is GENERAL, so it is enforced generally: every <c>integerLiteral</c> in the parse tree is an
/// <c>integer-n</c> and is NONZERO by default, with no code per clause. What IS per clause is only the exception —
/// the handful of associated rules that expressly permit zero — and those live in ONE table,
/// <see cref="IntegerOperandRules.Slots"/>, keyed by the grammar rule that spells the operand, each entry citing
/// its rule. A new grammar rule that writes <c>integerLiteral</c> is screened automatically, and
/// <c>IntegerOperandSlotDriftTests</c> fails until it is classified, so the exception question is asked of every
/// new site rather than remembered.</para>
///
/// <para>Before this pass there was no site at all: <c>05 E PIC X OCCURS 0 TIMES.</c> was accepted, lowered, and
/// reached the user as two Roslyn CS0029 errors naming generated C#. It runs pre-bind beside
/// <see cref="LevelNumberPass"/> for the same reason: a pure syntax rule over the raw tree, and a zero that later
/// phases would otherwise turn into a cascade or a backend failure.</para>
/// </summary>
internal sealed class IntegerOperandPass(IDiagnosticSink sink) : CursorFollowingVisitor(sink)
{
    /// <summary>Screen every <c>integer-n</c> operand in the group's raw parse tree.</summary>
    internal static void Run(Core.CompilationUnitContext tree, IDiagnosticSink sink) =>
        new IntegerOperandPass(sink).VisitPositioned(tree);

    public override object? VisitIntegerLiteral(Core.IntegerLiteralContext ctx)
    {
        string text = ctx.GetText();
        if (text.Length > 0 && text.AsSpan().Trim('0').IsEmpty
            && IntegerOperandRules.Classify(ctx).Kind == IntegerSlotKind.NonZero)
        {
            string where = IntegerOperandRules.ConstructName(ctx);
            Sink.Report(new EditionDiagnostic(DiagnosticCatalog.IntegerOperandZero.Code, EditionSeverity.Error,
                DiagnosticCatalog.IntegerOperandZero.Id,
                $"{where}: the integer operand {text} shall be nonzero — ISO §5.5 1): an integer-n \"shall be unsigned "
                + "and nonzero unless otherwise specified in the associated rules\", and no rule of this format "
                + "permits zero here", where, DiagnosticCatalog.IntegerOperandZero.IsoSection));
        }
        if (IntegerOperandRules.BeyondHostLimit(ctx))
        {
            string where = IntegerOperandRules.ConstructName(ctx);
            Sink.Report(new EditionDiagnostic(DiagnosticCatalog.IntegerOperandBeyondLimit.Code, EditionSeverity.Error,
                DiagnosticCatalog.IntegerOperandBeyondLimit.Id,
                IntegerOperandRules.BeyondLimitMessage(where, $"the integer operand {text}"), where,
                DiagnosticCatalog.IntegerOperandBeyondLimit.IsoSection));
        }
        return base.VisitChildren(ctx);
    }
}

/// <summary>The per-position exceptions to §5.5 1)'s NONZERO default, and nothing else (see
/// <see cref="IntegerOperandPass"/>).</summary>
internal static class IntegerOperandRules
{
    /// <summary>A classifier for the operands of ONE grammar rule: given the rule's context and the operand,
    /// the operand's slot.</summary>
    internal delegate IntegerSlot Classifier(ParserRuleContext owner, ParserRuleContext operand);

    private static Classifier All(IntegerSlot slot) => (_, _) => slot;
    private static readonly Classifier Nonzero = All(IntegerSlot.Default);
    private static readonly Classifier ScreenDeclined = All(IntegerSlot.Not(
        "the SCREEN SECTION is a declined optional module (Annex A.4.2) refused whole by COBOLNET1560; its "
        + "clauses never bind, and a color integer may be zero"));

    /// <summary>⛔ EVERY grammar rule that writes <c>integerLiteral</c>, classified. Enumerated from the generated
    /// parser by <c>IntegerOperandSlotDriftTests</c> in both directions — a rule missing here, or a row naming a
    /// rule that no longer writes <c>integerLiteral</c>, is red. Only the ZERO-PERMITTED and NOT-GOVERNED rows
    /// carry a decision; a <see cref="Nonzero"/> row records that the rule was read and found silent.</summary>
    internal static readonly IReadOnlyDictionary<Type, Classifier> Slots = new Dictionary<Type, Classifier>
    {
        // ── IDENTIFICATION / ENVIRONMENT ────────────────────────────────────────────────────────────────────
        [typeof(Core.MemorySizeClauseContext)] = Nonzero,      // OBJECT-COMPUTER MEMORY SIZE integer (1985)
        [typeof(Core.SegmentLimitClauseContext)] = Nonzero,    // SEGMENT-LIMIT IS segment-number (1985)
        [typeof(Core.FileReserveClauseContext)] = Nonzero,     // §12.4.5.14 RESERVE integer-1 AREAS
        [typeof(Core.MultipleFileTapeEntryContext)] = Nonzero, // I-O-CONTROL MULTIPLE FILE … POSITION (1985)
        [typeof(Core.RerunEveryContext)] = Nonzero,            // I-O-CONTROL RERUN EVERY integer RECORDS (1985)
        [typeof(Core.SymbolicCharacterEntryContext)] = All(IntegerSlot.Not(
            "SYMBOLIC CHARACTERS integer-1 is an ORDINAL whose range, one through the size of the character set, is "
            + "§12.3.7.3 SR16's own rule, reported by DataBinder.SwitchBindSymbolic")),
        [typeof(Core.ChannelClauseContext)] = All(IntegerSlot.Not(
            "CHANNEL integer IS … is a vendor extension, not an ISO general format; §5.5 governs general formats")),
        [typeof(Core.ReserveClauseContext)] = All(IntegerSlot.Not(
            "SPECIAL-NAMES RESERVE integer CHANNELS is a vendor extension, not an ISO general format")),

        // ── PROCEDURE DIVISION HEADERS ──────────────────────────────────────────────────────────────────────
        [typeof(Core.SectionDefinitionContext)] = All(IntegerSlot.Not(
            "the section header's segment-number is the 1985 Segmentation module's priority-number (0 through 99), "
            + "not an integer-n; its range is that module's rule")),
        [typeof(Core.DeclarativeSectionContext)] = All(IntegerSlot.Not(
            "the declarative section header's segment-number is the 1985 Segmentation module's priority-number")),

        // ── DATA DIVISION ───────────────────────────────────────────────────────────────────────────────────
        [typeof(Core.BlockContainsClauseContext)] = Nonzero,   // §13.18.10 — nothing permits 0; SR1 orders them (DataBinder.BindBlockContainsClause)
        [typeof(Core.RecordClauseContext)] = RecordClause,
        [typeof(Core.LinageClauseContext)] = Nonzero,          // §13.18.34 integer-1
        [typeof(Core.LinageFootingPhraseContext)] = Nonzero,   // §13.18.34 integer-2
        [typeof(Core.LinageLinesAtTopPhraseContext)] = All(IntegerSlot.Zero("ISO §13.18.34.3 SR4")),
        [typeof(Core.LinageLinesAtBottomPhraseContext)] = All(IntegerSlot.Zero("ISO §13.18.34.3 SR4")),
        [typeof(Core.DynamicLengthClauseContext)] = Nonzero,   // §13.18.19 LIMIT IS integer-1
        [typeof(Core.PictureLocalePhraseContext)] = Nonzero,   // §13.18.40 PICTURE … LOCALE … SIZE integer-1
        [typeof(Core.OccursClauseContext)] = OccursBound,      // integer-1 / integer-2: a literal or a constant-name (§13.10.3 SR2)
        [typeof(Core.OccursStepPhraseContext)] = Nonzero,      // §13.18.38 Format 3 STEP integer-3
        [typeof(Core.OccursDynamicPhraseContext)] = OccursDynamic,

        // ── REPORT WRITER ───────────────────────────────────────────────────────────────────────────────────
        [typeof(Core.ReportPageClauseContext)] = Nonzero,      // §13.18.39.3 SR6 — "shall be greater than zero"
        [typeof(Core.ReportPageWidthContext)] = Nonzero,       // integer-2 COLUMNS — §5.5 1) NONZERO default (SR6 lists integer-1 and 3–7)
        [typeof(Core.ReportPageSubclauseContext)] = Nonzero,   // ditto
        [typeof(Core.ReportLineOperandContext)] = ReportLine,
        [typeof(Core.ReportNextGroupClauseContext)] = Nonzero, // §13.18.37 NEXT GROUP integer-1 / PLUS integer-2
        [typeof(Core.ReportColumnOperandContext)] = Nonzero,   // §13.18.14 Format 1 COLUMN integer-1 / PLUS

        // ── SCREEN SECTION (Annex A.4.2 — declined, refused whole by COBOLNET1560) ──────────────────────────
        [typeof(Core.ScreenLineClauseContext)] = ScreenDeclined,
        [typeof(Core.ScreenColumnClauseContext)] = ScreenDeclined,
        [typeof(Core.ScreenForegroundColorClauseContext)] = ScreenDeclined,
        [typeof(Core.ScreenBackgroundColorClauseContext)] = ScreenDeclined,
        [typeof(Core.ScreenPositionLegContext)] = ScreenDeclined,

        // ── STATEMENTS ──────────────────────────────────────────────────────────────────────────────────────
        // §14.9.28 PERFORM integer-1 TIMES: GR9's "equal to zero or is negative" speaks only of identifier-1's
        // run-time VALUE; no syntax rule otherwise-specifies integer-1, so the §5.5 default holds.
        [typeof(Core.PerformTimesContext)] = Nonzero,
        [typeof(Core.WriteBeforeAfterContext)] = All(IntegerSlot.Zero("ISO §14.9.51.3 SR15")),
    };

    /// <summary>§13.18.43.2's three formats: Format 1 <c>RECORD CONTAINS integer-1</c>; Format 2 <c>RECORD IS
    /// VARYING … [FROM integer-2] [TO integer-3]</c>; Format 3 <c>RECORD CONTAINS integer-4 TO integer-5</c>.
    /// Integer-2 (SR7) and integer-4 (SR8) "shall be greater than or equal to zero".</summary>
    private static IntegerSlot RecordClause(ParserRuleContext owner, ParserRuleContext op)
    {
        var rc = (Core.RecordClauseContext)owner;
        if (PrecededBy(owner, OperandOf(op), Core.TO)) return IntegerSlot.Default;   // integer-3 / integer-5
        if (rc.VARYING() is not null) return IntegerSlot.Zero("ISO §13.18.43.3 SR7"); // integer-2
        return rc.TO() is not null ? IntegerSlot.Zero("ISO §13.18.43.3 SR8")         // integer-4
            : IntegerSlot.Default;                                                     // integer-1
    }

    /// <summary>§13.18.38 OCCURS: in <c>integer-1 TO integer-2</c> integer-1 "shall be greater than or equal to
    /// zero" (SR16); a lone bound is integer-2 and keeps the default.</summary>
    private static IntegerSlot OccursBound(ParserRuleContext owner, ParserRuleContext op) =>
        owner is Core.OccursClauseContext oc && oc.TO() is not null && oc.integerOperand(0) == OperandOf(op)
            ? IntegerSlot.Zero("ISO §13.18.38.3 SR16")
            : IntegerSlot.Default;

    /// <summary>§13.18.38 Format 4 (DYNAMIC): FROM integer-4 "shall be nonnegative" (SR28); TO integer-5 keeps the
    /// default.</summary>
    private static IntegerSlot OccursDynamic(ParserRuleContext owner, ParserRuleContext op) =>
        ((Core.OccursDynamicPhraseContext)owner).FROM() is not null
            ? IntegerSlot.Zero("ISO §13.18.38.3 SR28")
            : IntegerSlot.Default;

    /// <summary>§13.18.35 LINE: integer-1 is an absolute line number (default); integer-2, written after PLUS, is a
    /// relative one and "may be zero" (SR3).</summary>
    private static IntegerSlot ReportLine(ParserRuleContext owner, ParserRuleContext op) =>
        ((Core.ReportLineOperandContext)owner).reportRelativeSign() is not null
            ? IntegerSlot.Zero("ISO §13.18.35.3 SR3")
            : IntegerSlot.Default;

    /// <summary>⛔ EVERY grammar rule that may still spell a BARE <c>integerLiteral</c>, each with the REASON a constant-name
    /// does not need the <c>integerOperand</c> rule there (kb/Work PB1948, completing PB1947). ISO §13.10.3 SR2 —
    /// "constant-name-1 may be used anywhere that a format specifies a literal of the class and category of
    /// constant-name-1" — makes every <c>integer-n</c> (§5.5 1)) a position a constant-name stands in, so a clause that
    /// prints an <c>integer-n</c> writes <c>integerOperand</c> and this table is where the exceptions are argued, never
    /// the default. <c>IntegerOperandSlotDriftTests</c> derives the rules that write a bare <c>integerLiteral</c> from the
    /// generated parser and holds them to THIS set in both directions: a new clause that forgets <c>integerOperand</c>
    /// is red until it is either fixed or argued here.</summary>
    internal static readonly IReadOnlyDictionary<Type, string> LiteralOnlySlots = new Dictionary<Type, string>
    {
        [typeof(Core.MemorySizeClauseContext)] = Removed85,
        [typeof(Core.SegmentLimitClauseContext)] = Removed85,
        [typeof(Core.MultipleFileTapeEntryContext)] = Removed85,
        [typeof(Core.RerunEveryContext)] = Removed85,
        [typeof(Core.SymbolicCharacterEntryContext)] =
            "the entry's FIRST alternative, the literal ordinals every program wrote before constant entries existed, listed "
            + "first so that such a program parses exactly as it did; its second and third alternatives write integerOperand "
            + "and bare words (DataBinder.SymbolicEntryGroups), because a constant-name ordinal is the same kind of token as "
            + "the next symbolic-character-1 and only the constant table tells them apart",
        [typeof(Core.ChannelClauseContext)] = "a vendor extension, not an ISO general format (§5.5 governs general formats)",
        [typeof(Core.ReserveClauseContext)] = "SPECIAL-NAMES RESERVE … CHANNELS is a vendor extension, not an ISO general format",
        [typeof(Core.SectionDefinitionContext)] = "the 1985 Segmentation module's priority-number, not an integer-n; deleted at 2002",
        [typeof(Core.DeclarativeSectionContext)] = "the 1985 Segmentation module's priority-number, not an integer-n; deleted at 2002",
        [typeof(Core.ScreenLineClauseContext)] = ScreenDeclinedReason,
        [typeof(Core.ScreenColumnClauseContext)] = ScreenDeclinedReason,
        [typeof(Core.ScreenForegroundColorClauseContext)] = ScreenDeclinedReason,
        [typeof(Core.ScreenBackgroundColorClauseContext)] = ScreenDeclinedReason,
        [typeof(Core.ScreenPositionLegContext)] = ScreenDeclinedReason,
        [typeof(Core.LinageClauseContext)] = DataNameOrInteger,
        [typeof(Core.LinageFootingPhraseContext)] = DataNameOrInteger,
        [typeof(Core.LinageLinesAtTopPhraseContext)] = DataNameOrInteger,
        [typeof(Core.LinageLinesAtBottomPhraseContext)] = DataNameOrInteger,
        [typeof(Core.PerformTimesContext)] = DataNameOrInteger,
        [typeof(Core.WriteBeforeAfterContext)] = DataNameOrInteger,
    };

    private const string Removed85 =
        "an X3.23-1985 element ISO 2002 deleted; CONSTANT entries first exist at 2002 (constant-entry-2002), so no edition "
        + "holds both the clause and a constant-name to stand in it";

    private const string ScreenDeclinedReason =
        "the SCREEN SECTION is a declined optional module (Annex A.4.2) refused whole by COBOLNET1560; its clauses never bind";

    private const string DataNameOrInteger =
        "the format prints 'data-name-1 or integer-1': a constant-name parses as the dataReference arm and the binder reads "
        + "the constant's integer there (LINAGE: DataBinder.ConstantOperandValue; PERFORM TIMES and WRITE ADVANCING: "
        + "ExpressionBinder's constant substitution)";

    /// <summary>The largest <c>integer-n</c> value the compiler binds into its own model — an <see cref="int"/>,
    /// because every such operand sizes, counts or positions something the compiler lays out in a .NET object
    /// (a table, a record, a page, a line, a character set), and none of those can exceed it.</summary>
    internal const int HostLimit = int.MaxValue;

    /// <summary>⛔ The grammar rules whose <c>integer-n</c> is NOT bound into the compiler's model as an
    /// <see cref="int"/>, because its value is meaningful at any magnitude and is carried at full width:
    /// <list type="bullet">
    /// <item>a statement's repeat count (§14.9.28 PERFORM integer-1 TIMES) and line count (§14.9.51 WRITE
    /// ADVANCING integer-1 LINES), carried to run time through the one narrowing
    /// <c>CobolNet.Runtime.HostInteger</c> (kb/Work PB1033);</item>
    /// <item>the DYNAMIC LENGTH clause's LIMIT integer-1, read as an <see cref="Int128"/> and bounded by
    /// §8.5.1.10.1 ("The maximum size of a dynamic-length elementary item is smallest of") against the implementor maximum (kb/Work PB463).</item>
    /// </list>
    /// Every other rule's operand is bound through <see cref="HostValue"/>. A rule added here must read its
    /// operand wide; <c>IntegerOperandSlotDriftTests</c> holds the set.</summary>
    internal static readonly IReadOnlySet<Type> FullValueSlots = new HashSet<Type>
    {
        typeof(Core.PerformTimesContext),
        typeof(Core.WriteBeforeAfterContext),
        typeof(Core.DynamicLengthClauseContext),
    };

    /// <summary>True when <paramref name="operand"/> is bound into the compiler's model (it is not one of
    /// <see cref="FullValueSlots"/>) and its value exceeds <see cref="HostLimit"/> — the one question
    /// <see cref="IntegerOperandPass"/> asks before any binder reads the value (kb/Work PB1058).</summary>
    internal static bool BeyondHostLimit(Core.IntegerLiteralContext operand) =>
        !IsFullValueSlot(operand)
        && TryHostValue(operand.GetText(), out _, out bool saturated) && saturated;

    /// <summary>True when <paramref name="operand"/> sits in one of the <see cref="FullValueSlots"/> — the written
    /// literal and the integer constant-name that stands for it (ISO §13.10.3 SR2) are both exempt from the host limit
    /// there.</summary>
    internal static bool IsFullValueSlot(ParserRuleContext operand) =>
        OwnerOf(operand) is { } owner && FullValueSlots.Contains(owner.GetType());

    /// <summary>The <c>integerOperand</c> node an operand sits in — the operand itself when it is one, its parent
    /// when it is the <c>integerLiteral</c> arm of one, and the operand unchanged when the clause writes a bare
    /// <c>integerLiteral</c> (kb/Work PB1947).</summary>
    internal static ParserRuleContext OperandOf(ParserRuleContext operand) =>
        operand is Core.IntegerLiteralContext && operand.Parent is Core.IntegerOperandContext wrapper ? wrapper : operand;

    /// <summary>The grammar rule that OWNS an <c>integer-n</c> operand — the clause whose general format prints it,
    /// which is what <see cref="Slots"/> classifies. An <c>integerOperand</c> is only the shared carrier of "a literal or
    /// a constant-name" (§13.10.3 SR2) and never a clause of its own, so it is looked through.</summary>
    internal static ParserRuleContext? OwnerOf(ParserRuleContext operand) => OperandOf(operand).Parent as ParserRuleContext;

    /// <summary>⛔ THE ONE READER of an <c>integer-n</c> the binder keeps in its model (kb/Work PB1058). A value
    /// beyond <see cref="HostLimit"/> has already been reported by <see cref="IntegerOperandPass"/> (it runs
    /// pre-bind), so this returns <see cref="HostLimit"/> for it — a value past every range rule the binder then
    /// screens — rather than throwing: an <c>int.Parse</c> here used to take the whole compiler down with an
    /// unhandled <see cref="OverflowException"/> on <c>PAGE LIMIT 77777777777</c>, <c>COLUMN 77777777777</c>
    /// and <c>LINAGE 77777777777</c>.</summary>
    internal static int HostValue(Core.IntegerLiteralContext operand) => HostValue(operand.GetText());

    /// <summary>⛔ THE ONE READER of a SPECIAL-NAMES ORDINAL too (ISO §12.3.7.3 SR14 b1/c1, SR16 e/f, SR17 b2/c2 — kb/Work
    /// PB1091): an unsigned integer literal naming a 1-based position in a character set is read through THIS, whether
    /// it is written as an <c>integerLiteral</c> (SYMBOLIC CHARACTERS integer-1) or as a numeric literal of an ALPHABET
    /// or CLASS literal phrase (whose digits <c>DataBinder.IntegerLiteralDigits</c> extracts). The value SATURATES at
    /// <see cref="HostLimit"/>, which no character set reaches (UCS-4, the largest, has 0x110000 − 0x800), so a
    /// saturated ordinal is out of range exactly when the literal is and the clause's range rule — never a "not an
    /// integer" answer — reports it. <c>DataBinder.Switches.cs</c> had a second, private saturating reader for the
    /// literal phrases and an <c>int.TryParse</c> before it (two decoders for one rule, the two-arm shape);
    /// <c>SpecialNamesOrdinalReaderDriftTests</c> holds it to this one.</summary>
    /// <param name="integerText">The literal's text: decimal digits, optionally signed.</param>
    internal static int HostValue(string integerText) =>
        TryHostValue(integerText, out int v) ? v : HostLimit;

    /// <summary>⛔ THE ONE READER of the TEXT of a COBOL integer literal — an optional sign and decimal digits
    /// (§8.3.3.3.2: "An integer literal is a fixed-point numeric literal that contains no decimal point"; the
    /// SIGNED forms a few operands admit) — reached from a token, a constant's integer text or an expression's
    /// source text rather than an <c>integerLiteral</c> node (kb/Work PB1579). It separates the two questions a
    /// bare <c>int.TryParse</c> conflated: "is this an integer literal?" (the return value) and "what is its value
    /// in the host model?" (<paramref name="value"/>, saturated at <see cref="HostLimit"/> — at int.MinValue for a
    /// negative literal, the Format 2 VALUE subscript floor of kb/Work PB553 — like
    /// <see cref="HostValue(Core.IntegerLiteralContext)"/>, so a range rule downstream still finds it out of range;
    /// a diagnostic quotes the literal's own TEXT, never the saturated number). A <c>TryParse</c> answered "not an
    /// integer" for an 11-digit literal, and each site then took ANOTHER branch: an integer constant-name was
    /// refused as "not an INTEGER constant-name", a reference-modification literal as "not an integer literal", a
    /// 31-digit CURRENCY SIGN literal as not numeric. A site whose integer sizes or positions something the compiler
    /// lays out asks for <c>saturated</c> and raises COBOLNET2427 (<see cref="BeyondLimitMessage"/>) — handing the
    /// saturated bound on would have the layout allocate it.</summary>
    internal static bool TryHostValue(string text, out int value) => TryHostValue(text, out value, out _);

    /// <inheritdoc cref="TryHostValue(string, out int)"/>
    /// <param name="saturated">True when the literal's value lies outside the host range and
    /// <paramref name="value"/> is the saturated bound — the fact a site that substitutes a CONSTANT for an
    /// <c>integer-n</c> needs to raise the <see cref="HostLimit"/> diagnostic the literal itself would get.</param>
    /// <summary>The <c>long</c> twin of <see cref="TryHostValue(string, out int, out bool)"/> for an integer literal
    /// used as a run-time POSITION (a subscript or a reference-modifier position, kb/Work PB2151): the position
    /// intakes take a <c>long</c> (<c>CobolNum.Position</c> saturates there), so the literal is read at that width and
    /// saturates at <see cref="long.MaxValue"/> / <see cref="long.MinValue"/> — still out of every table's and item's
    /// range, so the range rules downstream find it so.</summary>
    internal static bool TryHostPosition(string text, out long value, out bool saturated)
    {
        value = 0;
        saturated = false;
        int digitsFrom = text.Length > 0 && text[0] is '+' or '-' ? 1 : 0;
        if (digitsFrom == text.Length) return false;
        for (int i = digitsFrom; i < text.Length; i++)
            if (!char.IsAsciiDigit(text[i])) return false;
        if (long.TryParse(text, System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture, out long v))
            value = v;
        else
        {
            saturated = true;
            value = text[0] == '-' ? long.MinValue : long.MaxValue;
        }
        return true;
    }

    /// <inheritdoc cref="TryHostValue(string, out int)"/>
    internal static bool TryHostValue(string text, out int value, out bool saturated)
    {
        value = 0;
        saturated = false;
        int digitsFrom = text.Length > 0 && text[0] is '+' or '-' ? 1 : 0;
        if (digitsFrom == text.Length) return false;
        for (int i = digitsFrom; i < text.Length; i++)
            if (!char.IsAsciiDigit(text[i])) return false;
        if (int.TryParse(text, System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture, out int v))
            value = v;
        else
        {
            saturated = true;
            value = text[0] == '-' ? int.MinValue : HostLimit;
        }
        return true;
    }

    /// <summary>⛔ THE ONE SENTENCE of COBOLNET2427 (kb/Work PB1579) — the pre-bind screen of a written
    /// <c>integer-n</c> and every binder site that meets the same over-limit integer by another road (an integer
    /// constant-name substituted for integer-n, a reference-modification literal of a data-division clause
    /// operand) report it in the same words, quoting the integer as the source wrote it.</summary>
    /// <param name="where">The construct, as the site names it.</param>
    /// <param name="subject">What the integer is, e.g. <c>the integer operand 77777777777</c>.</param>
    internal static string BeyondLimitMessage(string where, string subject) =>
        $"{where}: {subject} exceeds this implementation's limit of {HostLimit:N0} for an integer-n that sizes, "
        + "counts or positions something the compiler lays out (docs/CONFORMANCE.md §3 'Integer operands and host "
        + "carriers'). ISO §4.5: \"Translation may be unsuccessful due to factors other than lack of conformance of "
        + "a compilation group\" — the limits of an implementation";

    /// <summary>The slot of <paramref name="operand"/>, from the grammar rule that spells it. A rule the table
    /// does not name takes §5.5 1)'s default — the drift test keeps that from being silent.</summary>
    internal static IntegerSlot Classify(ParserRuleContext operand) =>
        OwnerOf(operand) is { } owner && Slots.TryGetValue(owner.GetType(), out var classify)
            ? classify(owner, operand)
            : IntegerSlot.Default;

    /// <summary>The construct named in the diagnostic: the source text of the clause or phrase that spells the
    /// operand (an OCCURS bound names its whole OCCURS clause).</summary>
    internal static string ConstructName(ParserRuleContext operand)
    {
        var owner = OwnerOf(operand);
        // A report LINE or COLUMN operand is one operand of a larger clause: name the clause.
        if (owner is Core.ReportLineOperandContext or Core.ReportColumnOperandContext)
            owner = owner.Parent as ParserRuleContext;
        if (owner is null) return "integer operand";
        string text = string.Join(' ', Tokens(owner));
        return text.Length <= 60 ? text : text[..57] + "...";
    }

    private static IEnumerable<string> Tokens(IParseTree node)
    {
        if (node is ITerminalNode t) { yield return t.GetText(); yield break; }
        for (int i = 0; i < node.ChildCount; i++)
            foreach (var s in Tokens(node.GetChild(i))) yield return s;
    }

    private static bool PrecededBy(ParserRuleContext owner, IParseTree operand, int tokenType)
    {
        for (int i = 1; i < owner.ChildCount; i++)
            if (owner.GetChild(i) == operand)
                return owner.GetChild(i - 1) is ITerminalNode t && t.Symbol.Type == tokenType;
        return false;
    }
}
