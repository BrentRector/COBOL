// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// A level-88 condition-name (ISO §13.18.4 / §8.8.4.2.7 r2): a named boolean predicate over a conditional variable
/// (its immediately superior data item). It owns no storage — referencing it tests whether the parent's current
/// value is among the condition's VALUE set (singletons and THRU ranges); <c>SET cond TO TRUE</c> moves the first
/// VALUE into the parent and <c>SET cond TO FALSE</c> moves <see cref="FalseValue"/> (COBOLNET_DESIGN §3.5;
/// §14.9.39.4 GR6/GR7). The compiler renders both forms over the parent's <see cref="Place"/>.
/// <para>⛔ ITS STATE IS EXACTLY WHAT §13.18.63.2 FORMAT 3 PRINTS, and that is the invariant to keep: the format
/// has THREE operand-bearing constituents — the <c>literal-2 [THROUGH literal-3]</c> group, <c>[ IN
/// alphabet-name-1 ]</c> and <c>[ WHEN SET TO FALSE IS literal-4 ]</c> — and every one that the model failed to
/// carry took a rule down with it (kb/Work PB398 for the alphabet, PB555 for literal-4). A new field belongs
/// here AND in <see cref="CopyOnto"/>; <c>Condition88CloneDriftTests</c> enforces the second half.</para>
/// </summary>
public sealed class Condition88
{
    /// <summary>The condition-name as written in the source.</summary>
    public required string Name { get; init; }

    /// <summary>The conditional variable (the immediately superior data item the condition tests).</summary>
    public required DataItem Parent { get; init; }

    /// <summary>
    /// The VALUE set: each entry is a single value (<c>High</c> null) or an inclusive THRU range. The raw source
    /// text of each operand is kept (e.g. <c>"\"Y\""</c>, <c>5</c>, <c>-9</c>); the emitter decodes it against the
    /// parent's category. The first entry's <c>Low</c> is what <c>SET … TO TRUE</c> stores.
    /// </summary>
    public List<(string Low, string? High)> Values { get; } = [];

    /// <summary>alphabet-name-1 of the clause's <c>IN</c> phrase as written (ISO §13.18.63.2 formats 3 and 5 —
    /// <c>[ IN alphabet-name-1 ]</c> stands OUTSIDE the repeated literal group, so one alphabet governs the whole
    /// VALUE set), or null when the phrase is absent. §14.7.8 rule 2 makes it the collating sequence every THRU
    /// range in the set is evaluated in, overriding the no-phrase arm's implementor default — which for this
    /// compiler is the PROGRAM COLLATING SEQUENCE.
    /// <para>⛔ IT WAS PARSED AND DROPPED (kb/Work PB398), which is a silent wrong answer rather than a missing
    /// feature: <c>88 X VALUE "M" THRU "A" IN AL</c> under an AL that reverses the alphabet answered NO for a
    /// value the range contains, because the range was weighed natively. The EVALUATE twin could not even be
    /// written. §14.7.8's opening sentence — "This specification applies to THROUGH phrases specified in the VALUE
    /// clause and the EVALUATE statement" — is why both now resolve through the one
    /// <c>DataBinder.TryResolveRangeAlphabet</c>.</para></summary>
    public string? Alphabet { get; set; }

    /// <summary>literal-4 of the clause's <c>[ WHEN SET TO FALSE IS literal-4 ]</c> phrase (ISO §13.18.63.2
    /// format 3 — the bracket printed on the line after the IN phrase) as WRITTEN, in the same raw-operand-text
    /// convention as <see cref="Values"/>, or null when the phrase is absent. §13.18.63.4 GR20 makes it the value
    /// <c>SET condition-name TO FALSE</c> places in the conditional variable — "<i>When a condition-name is
    /// referenced in a 'SET condition-name TO FALSE' statement, the value of literal-4 from the FALSE phrase is
    /// placed in the associated conditional-variable</i>" — and §14.9.39.4 GR7 states the store itself, in the
    /// SAME words §14.9.39.4 GR6 uses for the TRUE phrase, which is why the emitter has one store path and only
    /// this selector.
    /// <para>⛔ IT WAS THE THIRD OF FORMAT 3's THREE OPERAND-BEARING CONSTITUENTS AND THE ONLY ONE THE MODEL DID
    /// NOT CARRY (kb/Work PB555). Being unmodelled, not merely unchecked, is what made both of its rules
    /// unreachable at once: §13.18.63.3 SR27 had no literal-4 to compare against literal-2, so
    /// <c>88 CN VALUE 1 WHEN SET TO FALSE IS 1</c> compiled clean, and <c>SetBinder</c> had no value to store, so
    /// <c>SET … TO FALSE</c> could only stage LOUD. Storing it is therefore ONE change with two rule
    /// consequences, not two guards.</para></summary>
    public string? FalseValue { get; set; }

    /// <summary>⛔ THE ONE COPIER of a condition-name onto a different conditional variable — a TYPEDEF clone
    /// (ISO §13.18.58.4 GR1: "<i>the condition-names are part of the type</i>") and a SAME AS copy
    /// (§13.18.49.4 GR2a) — and it lives HERE, next to the state, because a copier written at the call site is
    /// a list of fields that silently stops being exhaustive.
    /// <para>⛔ IT WAS EXACTLY THAT (kb/Work PB555 sibling sweep). <c>DataBinder.CloneConditionOnto</c> copied
    /// <see cref="Values"/> and nothing else, so a clone lost <see cref="Alphabet"/> — a SILENT WRONG ANSWER,
    /// since §14.7.8 rule 2's sequence reverting to the program default re-answers every THRU range the clone
    /// tests — and would have lost <see cref="FalseValue"/> the moment it existed. Every constituent
    /// §13.18.63.2 Format 3 prints is carried here; <c>Condition88CloneDriftTests</c> fails if a field is added
    /// to this class and not to this method.</para>
    /// <para><paramref name="newParent"/> is the only thing that legitimately differs: the clone tests the
    /// CLONE's storage, not the template's.</para></summary>
    public Condition88 CopyOnto(DataItem newParent)
    {
        var c = new Condition88 { Name = Name, Parent = newParent, Alphabet = Alphabet, FalseValue = FalseValue };
        c.Values.AddRange(Values);
        c._clonedFrom = this;
        return c;
    }

    /// <summary>The condition-name <see cref="CopyOnto"/> reproduced this one from, or null for a written entry —
    /// the copy's provenance, never a constituent of Format 3 (so it is read-only and not "carried"). Followed to
    /// its end it names the WRITTEN definition the copy stands for, which ISO §8.4.2.2.1 rule 4's "any other
    /// definition of the name" is about (kb/Work PB1476; <c>DataBinder.WithLapsedTypeConditions</c>).</summary>
    public Condition88? ClonedFrom => _clonedFrom;
    private Condition88? _clonedFrom;
}
