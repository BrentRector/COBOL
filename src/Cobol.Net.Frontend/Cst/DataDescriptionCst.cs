// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Cst;

using Core = CobolParserCore;

/// <summary>
/// Typed façade over <see cref="Core.DataDescriptionEntryContext"/> (rearchitecture PHASE 04, Group C) — the
/// surface <c>DataBinder.BindEntry</c> reads instead of raw positional <c>GetText()</c>. Thin, 1:1 with the
/// grammar rule, no semantic state. <see cref="Clauses"/> yields a <see cref="DataDescriptionClauseCst"/> per
/// clause whose TEXT-bearing reads are typed; a caller keeps the presence-only clause predicates (BASED / EXTERNAL
/// / TYPEDEF / JUSTIFIED / SYNC / SIGN / USAGE / OCCURS …) raw via <see cref="DataDescriptionClauseCst.Context"/>
/// so their else-if order and edition-gate side effects stay byte-identical.
/// </summary>
public readonly struct DataDescriptionCst(Core.DataDescriptionEntryContext ctx)
{
    public Core.DataDescriptionEntryContext Context => ctx;

    /// <summary>The level number as an <see cref="int"/>, or <see langword="null"/> when it does not parse
    /// (the caller returns early on null — byte-identical to the <c>int.TryParse</c> guard).</summary>
    public int? Level => int.TryParse(ctx.levelNumber().GetText(), out int n) ? n : null;

    /// <summary>The data-name text, or <see langword="null"/> when the entry is FILLER (no <c>dataName</c>) —
    /// the null vs non-null distinction drives the FILLER test, so this MUST stay nullable.</summary>
    public string? Name => ctx.dataName()?.GetText();

    /// <summary>The clauses of the entry body, in source order (empty when the body has no clause list).</summary>
    public IReadOnlyList<DataDescriptionClauseCst> Clauses
    {
        get
        {
            var list = ctx.dataDescriptionBody().dataDescriptionClauses()?.dataDescriptionClause();
            if (list is null || list.Length == 0) return [];
            var wrapped = new DataDescriptionClauseCst[list.Length];
            for (int i = 0; i < list.Length; i++) wrapped[i] = new DataDescriptionClauseCst(list[i]);
            return wrapped;
        }
    }

    /// <summary>
    /// The set of §13.16.2 Format-1 clause slots this entry actually WRITES — the operand of every
    /// "…shall not be specified in the same data description entry with…" rule of §13.16.3.
    ///
    /// <para>⛔ <b>Why a SET and not another hand-written condition list.</b> §13.16.3 SR12, SR13, SR14, SR17 and
    /// SR18 are each a rule over the WHOLE clause list — SR17 and SR18 literally read "the only other clauses
    /// permitted are …". Every one of them used to be spelled in the binder as an <c>||</c> chain over whichever
    /// local decode flags the author happened to remember, and every one of them was INCOMPLETE the moment a new
    /// clause landed: SR12 could not see GROUP-USAGE, PROPERTY, SELECT WHEN, DYNAMIC LENGTH or the validation
    /// clauses; SR13 could not see DYNAMIC LENGTH or SELECT WHEN; SR17 could not see nine of them. The
    /// permitted-set rules are therefore expressed as SET operations over this one classification, and
    /// <c>DataClauseKindDriftTests</c> asserts that every alternative of the <c>dataDescriptionClause</c> grammar
    /// rule has a <see cref="DataClauseKind"/> — so a clause added to the grammar CANNOT silently fall out of
    /// the rules that govern its composition (kb/Work PB487; CLAUDE.md rule 5).</para>
    ///
    /// <para>This is the WRITTEN set, deliberately: a syntax rule asks what the programmer specified, not what
    /// survived the binder's per-clause recovery (several decode flags are CLEARED on a violation, which would
    /// make a second violation in the same entry invisible).</para>
    /// </summary>
    public DataClauseKind WrittenClauses
    {
        get
        {
            var set = DataClauseKind.None;
            foreach (var c in Clauses) set |= c.Kind;
            return set;
        }
    }

    public SourceSpan Span => SourceSpan.Of(ctx);

    public static implicit operator DataDescriptionCst(Core.DataDescriptionEntryContext c) => new(c);
}

/// <summary>Typed façade over one <see cref="Core.DataDescriptionClauseContext"/>. TEXT-bearing accessors are
/// typed (each replacing one <c>GetText()</c> cluster in <c>BindEntry</c>); the presence-only clause predicates
/// stay raw on <see cref="Context"/>.</summary>
public readonly struct DataDescriptionClauseCst(Core.DataDescriptionClauseContext ctx)
{
    /// <summary>The raw clause context — for the presence-only <c>xxxClause() is [not] null</c> predicates.</summary>
    public Core.DataDescriptionClauseContext Context => ctx;

    /// <summary>Which §13.16.2 Format-1 clause slot this is (<see cref="DataClauseKind.None"/> only when the
    /// grammar grew an alternative nobody classified — which <c>DataClauseKindDriftTests</c> makes impossible).
    /// <para>A <c>dataDescriptionClause</c> has exactly ONE child, the chosen alternative's sub-rule, so the
    /// classification is a single map lookup on that child's context type — no text, no re-parse.</para></summary>
    public DataClauseKind Kind =>
        ctx.ChildCount == 1 && DataClauseKinds.ByContextType.TryGetValue(ctx.GetChild(0).GetType(), out var k)
            ? k : DataClauseKind.None;

    /// <summary>The PICTURE string text, or <see langword="null"/> when this is not a picture clause.</summary>
    public string? PictureText => ctx.pictureClause()?.PIC_STRING()?.GetText();

    /// <summary>The <c>TYPE IS type-name</c> referenced type-name text (§13.18.57; D17), or null.</summary>
    public string? TypeRefName => ctx.typeClause()?.IDENTIFIER()?.GetText();

    /// <summary>The <c>SAME AS data-name-1</c> target data-name text (ISO §13.18.49), or null. The first
    /// <c>cobolWord</c> is the target; any further ones are its OF/IN qualifiers (<see cref="SameAsQualifiers"/>).</summary>
    public string? SameAsTargetName => ctx.sameAsClause()?.cobolWord(0)?.GetText();

    /// <summary>The SAME AS target's OF/IN qualifier names, outermost-last as written (empty when unqualified
    /// or when this is not a sameAsClause).</summary>
    public IReadOnlyList<string> SameAsQualifiers
    {
        get
        {
            var words = ctx.sameAsClause()?.cobolWord();
            if (words is null || words.Length <= 1) return [];
            var quals = new string[words.Length - 1];
            for (int i = 1; i < words.Length; i++) quals[i - 1] = words[i].GetText();
            return quals;
        }
    }

    /// <summary>The REDEFINES target's <c>dataReference</c> text, or null.</summary>
    public string? RedefinesTargetName => ctx.redefinesClause()?.dataReference().GetText();

    // (The canonical USAGE keyword text and the VALUE-operand normalization stay in DataBinder's shared
    // UsageKeyword / ExtractValue helpers — reused by BOTH BindEntry and DataBinder.Reports.cs, so the façade does
    // NOT fork a second copy of that computation. This clause façade exposes only the leaf text reads BindEntry
    // migrated in Group C; the report-writer partial migrates in P7.)

    // (The OCCURS fixed bounds are no longer a pure text read: each `integerOperand` is an integer literal OR an
    // integer constant-name (ISO §13.10.3 SR2), and resolving the latter needs the binder's compile-time
    // constant table — so the max-occurrence read lives in DataBinder.Constants.IntegerOperandValue, not here.)

    /// <summary>The OCCURS INDEXED BY index-name texts, in order (empty when absent). The caller keeps the
    /// <c>INDEXED()</c> presence guard raw.</summary>
    public IReadOnlyList<string> IndexNames
    {
        get
        {
            var idxList = ctx.occursClause()?.dataReferenceList();
            if (idxList is null) return [];
            var refs = idxList.dataReference();
            var names = new string[refs.Length];
            for (int i = 0; i < refs.Length; i++) names[i] = refs[i].GetText();
            return names;
        }
    }

    /// <summary>The first VALUE operand's RAW source text (literal or figurative constant), or null. Numeric-literal
    /// normalization stays binder-side (it is binder logic, not a text read).</summary>
    public string? FirstValueText => ctx.valueClause()?.valueItem().FirstOrDefault()?.GetText();

    public static implicit operator DataDescriptionClauseCst(Core.DataDescriptionClauseContext c) => new(c);
}
