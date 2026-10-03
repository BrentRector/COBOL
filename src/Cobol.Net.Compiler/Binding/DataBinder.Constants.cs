// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Preprocessor;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The CONSTANT-entry half of the data binder (ISO/IEC 1989:2023 §13.10 constant entry + §13.18.15 CONSTANT
/// RECORD; P10 Step 15). A constant entry defines a COMPILE-TIME substitution: §13.10.4 GR1/GR3 make every
/// reference to constant-name-1 "as if [the] literal were written where constant-name-1 is written" — so a
/// constant occupies NO storage and produces NO <see cref="DataItem"/>; <see cref="BindConstantEntry"/> folds
/// the entry into the per-unit compile-time constant table (<see cref="_constants"/>), and the reference
/// chokepoints substitute the literal:
/// <list type="bullet">
/// <item><c>ExpressionBinder.FieldOperand</c> / <c>RefExpr</c> — the ONE dataReference→operand/expression
/// mappings every verb consumes (MOVE/DISPLAY sources, relations, PERFORM TIMES, arithmetic);</item>
/// <item><c>ReferenceResolver.ResolveSubscriptName</c> — subscript positions (§13.10.3 SR2: an integer
/// constant-name stands where the format specifies an integer);</item>
/// <item><see cref="OccursBoundValue"/> — the OCCURS integer-1/integer-2 bounds (SR2, via the
/// <c>occursBound</c> grammar alternative);</item>
/// <item><see cref="ExpandPicConstants"/> — PICTURE repetition <c>X(K)</c> (SR2 second sentence);</item>
/// <item><c>ExtractValue</c> — a VALUE-clause constant-name operand substitutes its raw literal text (the
/// text-plumbed data path, the ConcatFolder RawText precedent);</item>
/// <item><c>ExpressionBinder.ResolveReceiving</c> — a constant-name as a receiving operand rejects
/// (<c>COBOLNET1548</c> — a literal cannot be a receiver; SR2 permits a constant only where a LITERAL is
/// permitted).</item>
/// </list>
/// The four AS forms (§13.10.2): <b>AS literal-1</b> (GR1/GR2 — the constant IS the literal, class and
/// category preserved; a single numeric literal in the arithmetic-expression position re-classifies as a
/// literal per SR1, so <c>AS 0.25</c> keeps its non-integer value); <b>AS arithmetic-expression-1</b>
/// (GR4 — evaluated per §7.3.6 compile-time arithmetic: no exponentiation SR1a, fixed-point numeric literal
/// operands SR1b — a constant-name operand substitutes its literal per §13.10.3 SR2/GR1, with SR4/SR5 ruling
/// out circularity — no division by zero SR1c, the final result truncated to its integer part §7.3.6.3 GR3;
/// intermediate results ride the edition's compile-time arithmetic mode, <see cref="CompileTimeArithmetic.For"/> —
/// standard arithmetic at 2002/2014, the documented .NET <see cref="decimal"/> mode from 2023, kb/Work PB1592);
/// <b>AS LENGTH OF data-name-2</b> (GR6 — the value of the §15.50 LENGTH function: <c>ItemLength.Positions</c>,
/// THE §15.50.4 r1/r2/r3 fold the FUNCTION LENGTH binder reads, which is maximum-allocation based so the GR6
/// occurs-depending exception holds); <b>AS BYTE-LENGTH OF data-name-1</b> (GR5 — the value of the §15.14
/// BYTE-LENGTH function: <c>DataItem.ByteWidth</c>, the fold FUNCTION BYTE-LENGTH reads; the same binder and the same
/// SR3/SR10/SR12 screens as LENGTH OF, kb/Work PB1227).
/// The <b>FROM compilation-variable-name-1</b> form (GR1/GR2 — the &gt;&gt;DEFINE tie-in) reads the group's
/// compilation-variable TIMELINE at the entry's own line (<see cref="BindConstantFrom"/>; §7.3.11.4 GR1, §13.10.3
/// SR8 — kb/Work PB1368): the conditional-compilation driver records every DEFINE in the resultant line frame, PUSH/POP
/// revoke as they do for every directive state, and the binder receives it through <c>DirectiveResults</c>.
/// Definition-before-reference is required (the GR1 textual-substitution model — substitution happens as the
/// source is bound, in order); a forward reference fails as an unknown constant-name.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>One folded compile-time constant (ISO §13.10). <paramref name="Text"/> is the substitution
    /// value: for class numeric the LITERAL AS WRITTEN — literal-1 with its own sign and decimal separator
    /// (§13.10.4 GR1, "as if literal-1 … were written where constant-name-1 is written"), or the GR3 integer
    /// literal of an expression / LENGTH OF form; for the string classes the DECODED character value.
    /// <para>⛔ A NUMERIC <paramref name="Text"/> IS SOURCE TEXT, NEVER A NORMALIZED VALUE (kb/Work PB1230). A
    /// consumer that re-binds it takes it through its literal chokepoint in the active DECIMAL-POINT mode, exactly
    /// as the written literal would go — which is why <c>AS 1,5</c> under DECIMAL-POINT IS COMMA substitutes
    /// cleanly and <c>AS +5</c> displays '+5'. It used to hold the evaluator's normalized text ('1.5', '5'), and
    /// every reference re-checked that as a literal: '1.5' was refused under the comma mode.</para>
    /// <paramref name="IntegerText"/> is the constant's value as a canonical integer (no '+', no separator) when the
    /// constant IS an integer (§13.10.3 SR2 — an integer literal-1, or GR3–GR6), else null: the form the integer
    /// positions read (an OCCURS bound, a PICTURE repetition, a subscript). <paramref name="RawText"/> is the
    /// equivalent literal as RAW source text (re-quoted per class) — the currency of the text-plumbed paths (the
    /// DATA-division VALUE capture), mirroring <c>ConcatFolder.Folded.RawText</c>. <paramref name="Specification"/>
    /// is the AS operand as written, which §13.10.3 SR9 compares when the name is duplicated.</summary>
    public sealed record ConstantDef(
        string Name, PicCategory Category, string Text, string? IntegerText, bool IsGlobal, string RawText,
        string Specification);

    /// <summary>The per-unit compile-time constant table (§13.10.4 GR1 substitution source). It also holds the
    /// GLOBAL constants of every containing program (<see cref="InheritGlobalConstants"/>, kb/Work PB1009), named
    /// in <see cref="_inheritedConstants"/> until a local declaration shadows them.</summary>
    private readonly Dictionary<string, ConstantDef> _constants = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The names in <see cref="_constants"/> that came from a container, not from this unit's own entries,
    /// each with the containment distance of the container that declared it (1 = the direct container) — the tier
    /// <see cref="DropShadowedConstants"/> weighs against a same-spelled data-name's (kb/Work PB1047).</summary>
    private readonly Dictionary<string, int> _inheritedConstants = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>ISO §13.18.27.4 GR1–GR2 for constant-names (kb/Work PB1009): "A constant-name, data-name, file-name,
    /// report-name, or screen-name described using a GLOBAL clause is a global name", and "A statement in a program
    /// contained directly or indirectly within a program that describes a global name may reference that name
    /// without describing it again". A constant occupies no storage, so it needs no bridge — the container's
    /// folded definition joins this unit's table BEFORE this unit binds (definition precedes reference, the §13.10.4
    /// GR1 substitution model). The caller passes EVERY container, nearest first, and the first definition of a
    /// name wins — "directly or indirectly", with the nearest global declaration taking precedence. A local declaration of
    /// the same name shadows (§8.4.6): a local constant entry replaces the inherited one
    /// (<see cref="BindConstantEntry"/>), and a data-name declared NEARER removes it (<see cref="DropShadowedConstants"/>).
    /// <paramref name="depth"/> is the container's containment distance (1 = the direct container).</summary>
    internal void InheritGlobalConstants(DataBinder container, int depth)
    {
        foreach (var (name, def) in container._constants)
            if (def.IsGlobal && _constants.TryAdd(name, def))
                _inheritedConstants[name] = depth;
    }

    /// <summary>Once this unit's DATA DIVISION is bound AND every container's global data is inherited
    /// (<see cref="InheritGlobalSubtree"/>): an inherited GLOBAL constant is SHADOWED by a data-name of the same
    /// spelling declared in a NEARER source element — this unit's own, or a nearer container's global one — by ISO
    /// §8.4.6.2.1 3) ("a) If the name is declared in source element B, the item in source element B is the referenced
    /// item. b) … 1. The item in source element A if the name is declared in source element A"), so it leaves the
    /// table before any procedure reference can substitute it (kb/Work PB1009; the nearer-container half kb/Work
    /// PB1047 — an outer program's constant used to beat the middle program's global data-name).</summary>
    internal void DropShadowedConstants()
    {
        foreach (var (name, depth) in _inheritedConstants.ToList())
            if (ByName.TryGetValue(name, out var items) && items.Any(i => DeclaringDepth(i) < depth))
            {
                _constants.Remove(name);
                _inheritedConstants.Remove(name);
            }
    }

    /// <summary>The defined constant named <paramref name="name"/>, or null (§13.10.4 GR1 lookup).</summary>
    internal ConstantDef? FindConstant(string name) => _constants.TryGetValue(name, out var d) ? d : null;

    /// <summary>Is <paramref name="name"/> an INTEGER constant-name — the shape §13.10.3 SR2 admits where a format
    /// specifies an integer literal ("If constant-name-1 is an integer, …"; GR3 for the expression and length
    /// forms)?</summary>
    private bool IsIntegerConstant(string name) =>
        FindConstant(name) is { Category: PicCategory.Numeric, IntegerText: not null };

    /// <summary>The constant a BARE (unqualified, unsubscripted) data reference names, or null. A constant-name
    /// takes no qualifiers, subscripts, or reference-modification — it substitutes a literal (§13.10.4 GR1) —
    /// so any suffixed reference falls through to ordinary resolution.</summary>
    internal ConstantDef? ConstantOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w
        && _constants.TryGetValue(w.GetText(), out var def) ? def : null;

    /// <summary>Whether <paramref name="item"/> is, or is subordinate to, a CONSTANT RECORD (ISO §13.18.15.3
    /// SR2 — "neither the data item described by the subject of the entry nor any data item subordinate to
    /// [it] shall be specified as a receiving data item"). The flag lives on the level-01 root; this walk
    /// covers the subtree.</summary>
    internal bool IsConstantRecordItem(DataItem item)
    {
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (n.IsConstantRecord) return true;
        return false;
    }

    /// <summary>The §13.18.15.3 SR2 receiving-operand prohibition for CONSTANT RECORD content — "Neither the data
    /// item described by the subject of the entry nor any data item subordinate to the subject of the entry shall be
    /// specified as a receiving data item". The <c>COBOLNET1548</c> message when the place is (or is subordinate to)
    /// a structured constant, else null. It REPORTS NOTHING: the receiving chokepoint reports it, and §8.4.3.2.4
    /// GR5's argument-manner test only asks it (kb/Work PB1418).
    /// <para>⛔ IT HAS EXACTLY ONE CALL SITE, AND THAT IS THE DESIGN: <c>ExpressionBinder.ReceivingPlaceBar</c>, the
    /// place half of the ONE receiving chokepoint's prohibition table. A verb reaches this rule by RESOLVING ITS
    /// RECEIVER through <c>ResolveReceiving</c>, never by calling here.
    /// The remark that used to stand here claimed it was "shared by every receiving chokepoint that resolves its
    /// own Place (ACCEPT / INITIALIZE / INSPECT targets / MOVE CORRESPONDING receivers)" — a sentence that
    /// described an intention, not the code: there was no second caller, and INITIALIZE silently destroyed a
    /// structured constant at run time for as long as it resolved identifier-1 with the plain resolver (kb/Work
    /// PB416, which routed it through the chokepoint). A verb whose receiver bypasses <c>ResolveReceiving</c>
    /// bypasses this rule AND the five others that live beside it, so the repair is always the routing.</para></summary>
    internal string? ConstantStoreProhibition(Place place, string what)
    {
        if (!IsConstantRecordItem(place.Item)) return null;
        DataItem root = place.Item;
        while (root.Parent is { } p) root = p;
        return $"{what} names a data item of the CONSTANT RECORD '{root.CobolName ?? root.CsName}' — the content "
            + "of a structured constant cannot be modified (ISO §13.18.15.3 SR2)";
    }

    // ── The constant-entry bind (§13.10) ─────────────────────────────────────────────────────────────────────

    /// <summary>Bind one constant entry (§13.10.2 general format) into the constant table — the
    /// <c>constantEntryBody</c> of a <c>dataDescriptionBody</c> (a record area, WORKING-STORAGE, LOCAL-STORAGE,
    /// LINKAGE) or of a <c>constantEntry</c> standing in the REPORT SECTION (§13.8.2, kb/Work PB1226). Both spell
    /// the same body, so the caller passes the entry's level-number, its optional name and the body, never the host
    /// rule. Produces NO <see cref="DataItem"/> (§13.10.4 GR1/GR3 — a constant is a substitution, not storage). The
    /// COBOL-2002 introduction gate is the VersionConformancePass parse arm (<c>VisitConstantEntryBody</c> →
    /// constant-entry-2002 → COBOLNET0900 below 2002), NOT here (the binder stays edition-agnostic — Step E).</summary>
    private void BindConstantEntry(
        Core.LevelNumberContext level, Core.DataNameContext? nameCtx, Core.ConstantEntryBodyContext body)
    {
        string? name = nameCtx?.GetText();
        string where = $"constant entry '{name ?? "?"}'";
        // §13.10.2: the general format admits level {1 | 01} only, and constant-name-1 is mandatory.
        if (level.GetText() is not ("1" or "01"))
            Edition.Error(DiagnosticCatalog.ConstantEntryRule,
                $"{where}: a constant entry shall have level-number 1 or 01 (ISO §13.10.2)");
        if (name is null || name.Equals("FILLER", StringComparison.OrdinalIgnoreCase))
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule,
                "a constant entry shall be named — constant-name-1 is required (ISO §13.10.2)");
            return;
        }
        DeclareUserWord(name, UserWordKind.ConstantName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        bool isGlobal = body.GLOBAL() is not null;

        ConstantDef? def;
        if (body.cobolWord() is { } variable)   // FROM compilation-variable-name-1 (§13.10.2)
            def = BindConstantFrom(name, isGlobal, variable, level.Start.Line, where);
        else
        {
            var cv = body.constantValue();
            string spec = WrittenSpecification(cv);
            def = cv.LENGTH() is not null ? BindConstantLength(name, isGlobal, spec, cv.dataReference(), where, bytes: false)
                : cv.constantByteLengthWord() is not null ? BindConstantLength(name, isGlobal, spec, cv.dataReference(), where, bytes: true)
                : cv.nonNumericLiteral() is { } nn ? BindConstantStringLiteral(name, isGlobal, spec, nn, where)
                : BindConstantArithmetic(name, isGlobal, spec, cv.arithmeticExpression(), where);
        }
        if (def is null) return;

        // A container's GLOBAL constant of the same name is SHADOWED by this local entry (§8.4.6 scope of names;
        // kb/Work PB1009). ⚠ DETERMINATION: SR9's "duplicates another constant-name" is read within one source
        // element — the contained program's own declaration is the one its references mean, as for any global
        // data-name it redeclares — not as a cross-element obligation to repeat the container's specification.
        if (_inheritedConstants.Remove(name)) _constants.Remove(name);
        // §13.10.3 SR9: "If constant-name-1 duplicates another constant-name, the specification of
        // arithmetic-expression-1, literal-1, data-name-1, data-name-2, or compilation-variable-name-1 shall be the
        // same as specified in the other constant-name" — the SPECIFICATIONS, as written, not the values they fold to
        // (kb/Work PB1230: `AS 5` then `AS 2 + 3`, and `AS LENGTH OF W` then `AS 7`, compared equal by value and
        // compiled). ⚠ DETERMINATION: "the same" is text-word equality under the ONE matcher the standard itself
        // defines for comparing written text (§7.2.3.4 9) c), COPY REPLACING — separators collapse to a space, COBOL
        // words compare without regard to case), so `AS 5` / `AS 05` / `AS +5` are three specifications.
        if (_constants.TryGetValue(name, out var prior))
        {
            if (!CobolNet.Frontend.Preprocessor.TextWordSequence.Matches(prior.Specification, def.Specification))
                Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: duplicates constant-name "
                    + $"'{prior.Name}' with a different specification ('{def.Specification}', where the other entry "
                    + $"specifies '{prior.Specification}') — a duplicated constant-name shall carry the same "
                    + "specification (ISO §13.10.3 SR9)");
            return;
        }
        _constants[name] = def;
    }

    /// <summary>The AS operand's specification as written (§13.10.3 SR9's subject): its tokens' texts, one space
    /// apart. Built from the parse tree's TOKENS, not the source span, so a comment written inside a multi-line
    /// operand (<c>AS 2 *&gt; two</c> / <c>+ 3</c>) is not part of it — joined into one line, the comment would
    /// have swallowed the rest of the operand and made unequal specifications compare equal.</summary>
    private static string WrittenSpecification(Core.ConstantValueContext cv)
    {
        var tokens = new List<Antlr4.Runtime.IToken>();
        ReferenceResolver.CollectLeafTokens(cv, tokens);
        return string.Join(' ', tokens.Select(t => t.Text));
    }

    /// <summary>FROM compilation-variable-name-1 (kb/Work PB1368, PB1228). The name is read in the &gt;&gt;DEFINE
    /// timeline AS OF THIS ENTRY'S LINE — §7.3.11.4 GR1 scopes a definition to "text that follows a DEFINE directive
    /// specifying compilation-variable-name-1 without the OFF phrase", and the one place that may be used outside
    /// conditional compilation includes "a constant entry where the FROM phrase is specified":
    /// <list type="bullet">
    /// <item>§13.10.3 SR8 — "Compilation-variable-name-1 shall be a compilation-variable-name for which the defined
    /// condition is currently true": never defined before the entry, last defined with OFF (§7.3.11.4 GR2), or a
    /// PARAMETER the environment supplied no value for (GR4) — each refused here.</item>
    /// <item>§13.10.4 GR1 — the constant is "as if … the text represented by compilation-variable-name-1 were written
    /// where constant-name-1 is written", and GR2 — its class and category are those of "the literal represented by
    /// compilation-variable-name-1" (<see cref="CtValue.Category"/>).</item>
    /// </list>
    /// The name lives in the compilation-variable namespace, never the data division's: §8.3.2.2 1) lets it be spelled
    /// like any other user-defined word, so <c>FROM CQ</c> beside <c>01 CQ CONSTANT AS …</c> reads the variable.
    /// A numeric variable's text is the directive's literal, written with a period (NOTE 4 of §12.3.7.4 — directives are
    /// processed before DECIMAL-POINT IS COMMA takes effect), so under the comma mode its decimal separator becomes the
    /// comma that writes the same value in this source unit.</summary>
    private ConstantDef? BindConstantFrom(
        string name, bool isGlobal, Core.CobolWordContext variable, int entryLine, string where)
    {
        string word = variable.GetText();
        if (CompilationVariables.DefinitionAt(word, entryLine) is not { Value: { } value })
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: FROM '{word}' — compilation-variable-name-1 "
                + "shall be a compilation-variable-name for which the defined condition is currently true (ISO §13.10.3 "
                + "SR8): no >>DEFINE of it precedes the entry, or the last one specified OFF (ISO §7.3.11.4 GR2) or "
                + "obtained no PARAMETER value (GR4)");
            return null;
        }
        // §13.10.3 SR9 compares the specification as written; FROM and the name are the whole of it.
        string spec = "FROM " + word;
        if (value.Category == CtCategory.Numeric)
        {
            string text = DecimalPointIsComma ? value.Text.Replace('.', ',') : value.Text;
            string? integer = NumericLiteral.IsIntegerLiteralForm(value.Text) ? CtNumeric.ToIntegerText(value.Number) : null;
            return new ConstantDef(name, PicCategory.Numeric, text, integer, isGlobal, text, spec);
        }
        // ⛔ EVERY string category is named: a new CtCategory fails loudly here instead of binding as the last one.
        var folded = value.Category switch
        {
            CtCategory.Alphanumeric => new ConcatFolder.Folded(PicCategory.Alphanumeric, value.Text),
            CtCategory.National => new ConcatFolder.Folded(PicCategory.National, value.Text),
            CtCategory.Boolean => new ConcatFolder.Folded(PicCategory.Boolean, value.Bits!.Bits),
            _ => throw new InvalidOperationException($"{where}: compile-time category {value.Category} has no "
                + "constant-entry class — CtCategory grew a member this binder does not read"),
        };
        return new ConstantDef(name, folded.Category, folded.Value, null, isGlobal, folded.RawText, spec);
    }

    /// <summary>AS literal-1 for the STRING classes (§13.10.4 GR1/GR2 — the constant is the literal; class and
    /// category preserved). A §8.8.3 concatenation expression folds to its equivalent single literal FIRST
    /// (GR3 of §8.8.3.3 — the ConcatFolder chokepoint), so <c>AS "A" &amp; "B"</c> is the literal "AB". A
    /// figurative constant is rejected (§13.10.3 SR6).</summary>
    private ConstantDef? BindConstantStringLiteral(
        string name, bool isGlobal, string spec, Core.NonNumericLiteralContext nn, string where)
    {
        if (nn.concatenationExpression() is { } ce)
        {
            var folded = ConcatFolder.Fold(ce, Edition, LiteralEnv);
            return new ConstantDef(name, folded.Category, folded.Value, null, isGlobal, folded.RawText, spec);
        }
        if (nn.figurativeConstant() is not null)
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: literal-1 shall not be a figurative "
                + $"constant (ISO §13.10.3 SR6; '{nn.GetText()}')");
            return null;
        }
        // The predefined NULL is an identifier, not literal-1, and §13.10.3 SR7 makes every operand of
        // arithmetic-expression-1 a literal — so no alternative of the AS operand admits it (kb/Work PB1427; this
        // decoder used to take it for the boolean arm and threw).
        if (nn.predefinedNull() is not null)
        {
            PredefinedNullRule.Report(Edition, $"the AS operand of {where}, which is literal-1 or an arithmetic "
                + "expression whose operands are all literals (ISO §13.10.2; §13.10.3 SR7)");
            return null;
        }
        // ⛔ EVERY alternative is named: a new nonNumericLiteral arm fails loudly here instead of being decoded as the
        // last one listed.
        var (cat, value) =
            nn.STRINGLIT() is { } s ? (PicCategory.Alphanumeric, CobolLiteral.Decode(s.GetText()))
            : nn.HEXLIT() is { } x ? (PicCategory.Alphanumeric, CobolLiteral.DecodeHex(x.GetText()))
            : nn.NATLIT() is { } nat ? (PicCategory.National, CobolLiteral.Decode(nat.GetText()))
            : nn.BOOLLIT() is { } b ? (PicCategory.Boolean, CobolLiteral.Decode(b.GetText()))
            : throw new InvalidOperationException($"{where}: nonNumericLiteral alternative '{nn.GetText()}' has no "
                + "constant-entry decoding — the grammar rule grew an arm this decoder does not read");
        return new ConstantDef(name, cat, value, null, isGlobal,
            new ConcatFolder.Folded(cat, value).RawText, spec);
    }

    /// <summary>The arithmetic-expression AS form. §13.10.3 SR1 first: an operand that is a SINGLE numeric
    /// literal is a LITERAL, not an arithmetic expression — <c>AS 0.25</c> keeps class/category numeric with
    /// its non-integer value (GR1/GR2), where the expression form would have truncated to an integer (GR4).
    /// Otherwise the expression evaluates per §7.3.6 and the result is an integer (§13.10.4 GR4 + §7.3.6.3 GR3).</summary>
    private ConstantDef? BindConstantArithmetic(
        string name, bool isGlobal, string spec, Core.ArithmeticExpressionContext expr, string where)
    {
        // §7.3.6 evaluation via the ONE shared evaluator — §7.3.11.4 GR5 (single-literal reclassification, so
        // AS 0.25 keeps 0.25) and §7.3.6.3 GR3 (integer truncation of an expression's final result) are applied at
        // its public boundary. The binder supplies numeric-name resolution (a prior numeric constant substitutes
        // its literal, §13.10.3 SR2/GR1), routes the evaluator's diagnostics to its own codes, and names the
        // operand source per §13.10.3.
        var evaluator = new CompileTimeExpressionEvaluator(
            edition: Edition.Edition,
            resolveName: ResolveConstantName,
            diag: new ConstantEvaluatorDiagnostics(this),
            vocab: new CtOperandVocabulary(
                "previously defined numeric constant-names substituting them", "ISO §13.10.3 SR7 / §7.3.6.2 SR1b"),
            decimalPointIsComma: DecimalPointIsComma);
        if (evaluator.EvaluateArithmeticOperand(expr, where) is not { } n) return null;
        // The constant carries the literal AS WRITTEN (§13.10.4 GR1 — kb/Work PB1230), never a normalized form; its
        // value, where an integer position needs one, is the evaluator's — derived once, from the ONE literal parser.
        return new ConstantDef(name, PicCategory.Numeric, n.Literal,
            n.IsInteger ? CtNumeric.ToIntegerText(n.Value) : null, isGlobal, n.Literal, spec);
    }

    /// <summary>The value of a BARE constant-name (§7.3.6.2 SR1b / §13.10.3 SR2 substitution), or null when the
    /// name is not a currently-defined constant — the shared compile-time evaluator's name-resolution callback.
    /// The CONSTANT-entry arithmetic path uses only the NUMERIC case (§7.3.6.2 SR1b), so a non-numeric constant
    /// resolves to null and is rejected there, and so does a floating-point constant (§7.3.6.2 SR1b — its
    /// substituted literal is not fixed-point). A constant's <see cref="ConstantDef.Text"/> is its literal AS WRITTEN
    /// (kb/Work PB1230), so its value is read the way the written literal's is: through the ONE numeric-literal
    /// normalizer in this program's DECIMAL-POINT mode (§12.3.7.4 GR14a — already screened when the constant was
    /// bound, so no issue can arise here) and the ONE literal parser; it enters the expression in the edition's
    /// arithmetic mode at the evaluator.</summary>
    private CtValue? ResolveConstantName(string word) =>
        _constants.TryGetValue(word, out var d) && d.Category == PicCategory.Numeric
        && NumericLiteral.Normalize(d.Text, DecimalPointIsComma, out _) is var canonical
        && !NumericLiteral.IsFloatingPointForm(canonical)
        && CtNumeric.TryParseLiteral(canonical, out var v) ? CtValue.Numeric(v, d.Text) : null;

    /// <summary>Routes the shared compile-time evaluator's diagnostics to the CONSTANT-entry binder's own codes: an
    /// arithmetic rule → the <c>ConstantEntryRule</c> descriptor; a §12.3.7 GR14a separator violation →
    /// COBOLNET0895 — unchanged from the pre-shared-evaluator binder.</summary>
    private sealed class ConstantEvaluatorDiagnostics(DataBinder owner) : CobolNet.Frontend.Expressions.ICtDiagnostics
    {
        public void Report(CobolNet.Frontend.Expressions.CtDiagCode code, string message)
        {
            if (code == CobolNet.Frontend.Expressions.CtDiagCode.NumericSeparator)
                owner.Edition.Error("COBOLNET0895", message);
            else
                owner.Edition.Error(DiagnosticCatalog.ConstantEntryRule, message);
        }
    }

    /// <summary>The two length phrases of the constant entry, ONE binder (kb/Work PB1227): <b>AS LENGTH OF
    /// data-name-2</b> (§13.10.4 GR6 — the value of the §15.50 LENGTH function: <see cref="ItemLength.Positions"/>, THE
    /// §15.50.4 r1/r2/r3 fold the FUNCTION LENGTH binder reads) and <b>AS BYTE-LENGTH OF data-name-1</b> (GR5 — the value
    /// of the §15.14 BYTE-LENGTH function: <see cref="DataItem.ByteWidth"/>, the fold FUNCTION BYTE-LENGTH reads for a
    /// fixed item). Both are maximum-allocation based, so each rule's occurs-depending-group exception — "the maximum
    /// size of the data item is used" — holds by construction. Every syntax rule of §13.10.3 names data-name-1 and
    /// data-name-2 TOGETHER, so the two operands take the same screens: SR3 all subscripts shall be literals; §8.4.2.3.3
    /// SR2/SR3/SR5 the subscript count against the item's OCCURS depth, through
    /// <see cref="ReferenceResolver.ScreenSubscriptArity"/> (kb/Work PB1016); SR10 no ANY LENGTH operand; SR12 no
    /// dynamic-length elementary item or variable-length group operand, through
    /// <see cref="VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup"/>. The operand must already be bound
    /// (definition-before-reference — SR4 rules out the reverse dependence).</summary>
    /// <param name="bytes">True for BYTE-LENGTH OF data-name-1 (GR5), false for LENGTH OF data-name-2 (GR6).</param>
    private ConstantDef? BindConstantLength(
        string name, bool isGlobal, string spec, Core.DataReferenceContext dref, string where, bool bytes)
    {
        string? baseName = dref.cobolWord()?.GetText();
        if (baseName is null) return null;
        // The phrase and operand as §13.10.2 names them, for every diagnostic below.
        string phrase = bytes ? "BYTE-LENGTH OF" : "LENGTH OF";
        string operand = bytes ? "data-name-1" : "data-name-2";
        string written = DataBinder.WrittenText(dref);
        // ⛔ THE ONE DECOMPOSITION (ReferenceResolver.ReadWritten, kb/Work PB443/PB1016). This walked the suffix
        // list itself and looked only at a suffix's OWN subscript part, so a subscript hung off a qualification
        // (`CELL OF ROWX (3 2)`) was neither SR3-checked nor counted, and a separately parsed refModPart was
        // dropped unread.
        var w = ReferenceResolver.ReadWritten(dref);
        if (w.IsReferenceModified)
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: {phrase} '{written}' — the operand is "
                + $"{operand} (ISO §13.10.2), a data-name, and a data-name is not reference-modified");
            return null;
        }
        var resolver = new ReferenceResolver(this);
        int subscripts = 0;   // the subscripts as written — §8.4.2.3.3 SR2/SR3/SR5's operand, screened below
        if (w.SubscriptGroup is { } sub)
        {
            if (ReferenceResolver.IsEmptyGroup(sub))
            {
                Edition.Error(DiagnosticCatalog.EmptyParenthesesOnDataReference,
                    $"{where}: {phrase} '{written}': " + ReferenceResolver.EmptyParenthesesMessage);
                return null;
            }
            // §13.10.3 SR3: all subscripts of data-name-1 and data-name-2 shall be literals. (A subscript never
            // changes the length — every occurrence has the same description — so the tokens are only validated.)
            // A subscript is an integer position, and SR2 lets an INTEGER constant-name stand "anywhere that a format
            // specifies a literal of the class and category of constant-name-1" — GR1/GR3 make it that integer
            // literal — so `LENGTH OF E (KI)` with `01 KI CONSTANT AS 2` is a literal subscript (kb/Work PB1232; the
            // procedure-division twin is ReferenceResolver.ResolveSubscriptName's constant arm).
            var toks = new List<Antlr4.Runtime.IToken>();
            ReferenceResolver.CollectLeafTokens(sub, toks);
            if (toks.Any(t => t.Type is not (Core.SUB_INTEGERLIT or Core.INTEGERLIT or Core.SUB_WS
                    or Core.SUB_LPAREN or Core.SUB_RPAREN or Core.SUB_COMMA)
                && !(t.Type == Core.SUB_IDENTIFIER && IsIntegerConstant(t.Text))))
            {
                Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: all subscripts of the {phrase} "
                    + "operand shall be literals (ISO §13.10.3 SR3)");
                return null;
            }
            subscripts = resolver.SubscriptSegments(dref)?.Count ?? 0;
        }
        DataItem? item = resolver.FindItem(baseName, w.Qualifiers);
        if (item is null)
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: {phrase} '{written}' — the "
                + "data-name is not defined at this point (a constant entry reads only PRECEDING declarations; "
                + "ISO §13.10.3 SR4 rules out the reverse dependence)");
            return null;
        }
        // ⛔ §8.4.2.3.3 SR2/SR3/SR5 — THE SUBSCRIPTS AGAINST THE ITEM'S DIMENSIONS, through the ONE screen every
        // procedure-division reference takes (kb/Work PB1016). §13.10.3 SR3 above constrains only the FORM of a
        // subscript ("All subscripts of data-name-1 and data-name-2 shall be literals"); whether one may be
        // written at all, and how many, is §8.4.2.3.3's, and a constant entry is not among SR5's seven
        // exemptions. `CONSTANT AS LENGTH OF PLAIN (1)` over a non-table item compiled and yielded 7.
        if (resolver.ScreenSubscriptArity(dref, item, subscripts)) return null;
        if (item.IsAnyLength)
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the {phrase} operand shall not be "
                + "described with the ANY LENGTH clause (ISO §13.10.3 SR10)");
            return null;
        }
        // §13.10.3 SR12 — "Data-name-1 and data-name-2 shall not be dynamic-length elementary items or
        // variable-length groups", through the ONE screen of that pair (kb/Work PB1213). This asked a private
        // dynamic-capacity-TABLE walk, so a group made variable-length by a DYNAMIC LENGTH leaf passed and LENGTH
        // OF yielded its fixed part, while a DYNAMIC LENGTH operand itself fell to the GR6 "not computable" arm.
        if (VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup(item) is { } variableShape)
        {
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: {phrase} '{written}' "
                + $"— the operand is {variableShape}; {operand} shall not be a dynamic-length elementary item or "
                + "a variable-length group (ISO §13.10.3 SR12)");
            return null;
        }
        // §13.10.4 GR6 — "determined as specified in the LENGTH intrinsic function": THE §15.50.4 r1/r2/r3 fold, the
        // one FUNCTION LENGTH takes (kb/Work PB1213; it used to read DataItem.ImageWidth, a drifted second copy).
        // §13.10.4 GR5 — "determined as specified in the BYTE-LENGTH intrinsic function": DataItem.ByteWidth, the one
        // FUNCTION BYTE-LENGTH folds a fixed item to (IntrinsicBinder.BindByteLengthFold).
        int width = bytes ? item.ByteWidth : ItemLength.Positions(item);
        if (width <= 0)
        {
            // A TYPE-clause reference not yet expanded / a pending PICTURE-less usage — loud, never a wrong 0.
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the length of '{written}' is "
                + "not computable at this point in the data division (ISO §13.10.4 "
                + (bytes ? "GR5 — the §15.14 BYTE-LENGTH value" : "GR6 — the §15.50 LENGTH value")
                + "; a TYPE-expanded or usage-pending operand is a recorded residue)");
            return null;
        }
        string text = width.ToString(CultureInfo.InvariantCulture);
        return new ConstantDef(name, PicCategory.Numeric, text, text, isGlobal, text, spec);
    }

    // ── The data-division substitution chokepoints (§13.10.3 SR2) ────────────────────────────────────────────

    /// <summary>The value of one OCCURS fixed bound (the <c>occursBound</c> grammar alternative): integer-1/
    /// integer-2 as written, or an INTEGER constant-name substituting one (ISO §13.10.3 SR2 — "if
    /// constant-name-1 is an integer, it may also be used to specify … repetition"; §13.10.4 GR1/GR3). Null
    /// (reported) for a non-integer or unknown constant-name.</summary>
    private int? OccursBoundValue(Core.OccursBoundContext bound, string where)
    {
        if (bound.integerLiteral() is { } il)
            return CobolNet.Validation.IntegerOperandRules.HostValue(il);
        string word = bound.cobolWord().GetText();
        if (_constants.TryGetValue(word, out var k))
        {
            // THE ONE integer-literal reader (kb/Work PB1579): an integer constant beyond the host range is still an
            // INTEGER constant-name — a TryParse here refused it as "not an INTEGER constant-name". Substituted for
            // integer-n (§13.10.4 GR1), it meets the limit the written literal meets: IntegerOperandPass screens a
            // LITERAL pre-bind (COBOLNET2427), and a constant is known only here, so the same verdict is raised here
            // — never a saturated bound handed to the layout, which then tried to allocate it.
            if (k is { Category: PicCategory.Numeric, IntegerText: { } it }
                && CobolNet.Validation.IntegerOperandRules.TryHostValue(it, out int kv, out bool beyondLimit))
            {
                if (!beyondLimit) return kv;
                Edition.Error(DiagnosticCatalog.IntegerOperandBeyondLimit,
                    CobolNet.Validation.IntegerOperandRules.BeyondLimitMessage(where,
                        $"the OCCURS bound '{word}', the integer {it},"));
                return null;
            }
            Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the OCCURS bound '{word}' shall be "
                + "an INTEGER constant-name (ISO §13.10.3 SR2 — only an integer constant may specify an OCCURS "
                + "integer position)");
            return null;
        }
        Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: the OCCURS bound '{word}' is not a "
            + "defined constant-name — the OCCURS integer positions admit an integer literal or an integer "
            + "constant-name (ISO §13.18.38 / §13.10.3 SR2)");
        return null;
    }

    /// <summary>Expand integer constant-name repetition counts inside a PICTURE character-string (ISO
    /// §13.10.3 SR2 second sentence — "if constant-name-1 is an integer, it may also be used to specify
    /// repetition in a picture character-string, as specified in 13.18.40"): each parenthesized WORD (a
    /// repetition position is otherwise an unsigned integer, §13.18.40.2) is replaced by its constant's
    /// value. Called only when the unit defines constants, so a constant-free program's PICTURE pipeline is
    /// byte-identical to before.</summary>
    /// <param name="pictureText">The PICTURE character-string as written.</param>
    /// <param name="where">The entry named in a diagnostic.</param>
    /// <param name="diagnose">False for a MEASURING caller that reads the expanded string only for its width and
    /// leaves the diagnosis to the binder of the same entry, so one bad repetition word is reported once.</param>
    private string ExpandPicConstants(string pictureText, string where, bool diagnose = true)
    {
        return System.Text.RegularExpressions.Regex.Replace(pictureText,
            @"\(\s*([A-Za-z][A-Za-z0-9-]*)\s*\)",
            m =>
            {
                string word = m.Groups[1].Value;
                if (_constants.TryGetValue(word, out var k))
                {
                    if (k is { Category: PicCategory.Numeric, IntegerText: { } it }) return "(" + it + ")";
                    if (diagnose)
                        Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: '{word}' in the PICTURE "
                            + "repetition position shall be an INTEGER constant-name (ISO §13.10.3 SR2)");
                    return "(1)";   // recovery shape — the compile has already failed
                }
                if (diagnose)
                    Edition.Error(DiagnosticCatalog.ConstantEntryRule, $"{where}: '{word}' in the PICTURE "
                        + "repetition position is not a defined constant-name (ISO §13.18.40.2 — a repetition "
                        + "count is an unsigned integer or an integer constant-name, §13.10.3 SR2)");
                return "(1)";
            });
    }

    /// <summary>The RAW literal text a VALUE-clause constant-name operand substitutes (§13.10.3 SR2 — a VALUE
    /// operand is a literal position; §13.10.4 GR1 — "as if [the] literal were written"), or null when the
    /// operand is not a bare constant-name reference. The raw-text form feeds the existing VALUE data path
    /// (ValidateValueCategory → FieldEmitter → ValueInitializer), which stores raw literal text and decodes at
    /// emit time — the ConcatFolder RawText precedent.</summary>
    private string? ConstantValueRawText(Core.ValueClauseOperandContext op) =>
        // A word operand parses through the unaryExpression spine to a bare dataReference —
        // DataBinder.BareValueOperandWord is the ONE walk (any operator or suffix makes the operand a
        // non-constant shape, and returns null); it is the SAME accessor IsLiteralValueOperand's word arm
        // consults, so the screen and this substitution cannot disagree (kb/Work PB732).
        BareValueOperandWord(op) is { } dref ? ConstantOf(dref)?.RawText : null;
}
