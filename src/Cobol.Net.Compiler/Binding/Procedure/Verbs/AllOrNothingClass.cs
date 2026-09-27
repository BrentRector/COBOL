// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ <b>THE ALL-OR-NOTHING OPERAND-CLASS RULE</b> of the three character-manipulation verbs — ONE predicate for
/// one rule shape, stated three times by the standard (kb/Work PB980):
/// <list type="bullet">
/// <item>STRING, ISO §14.9.43.3 SR1: "If any one of literal-1, literal-2, identifier-1, identifier-2, or
///   identifier-3 is of class national, then all shall be of class national."</item>
/// <item>UNSTRING, §14.9.48.3 SR3: "If any of identifier-1, identifier-2, identifier-3, identifier-4, identifier-5,
///   literal-1, or literal-2 are of category national, then all shall be of category national."</item>
/// <item>INSPECT, §14.9.22.3 SR4: "If any of identifier-1, identifier-3, … literal-5 references an elementary
///   data item or literal of class boolean or national, then all shall reference a data item or literal of class
///   boolean or national, respectively."</item>
/// </list>
/// <para>Before this, STRING carried a private tally and UNSTRING and INSPECT enforced nothing:
/// <c>UNSTRING S DELIMITED BY "," INTO NR</c> with an alphanumeric <c>S</c> and a national <c>NR</c> compiled clean
/// and ran, and so did <c>INSPECT XS REPLACING ALL "," BY N";"</c>.</para>
/// <para>The class of each operand is supplied by the caller from <c>IntrinsicArgumentRules.ClassOf</c> (THE
/// §8.5.2.1 Table-2 reader), except where a verb's own rules answer differently (UNSTRING's numeric receiver —
/// see <c>StringUnstringBinder</c>). An operand whose class is not statically decidable passes
/// <see langword="null"/> and contributes NO opinion — which is exactly what a FIGURATIVE constant is by the
/// standard's own wording (§8.3.3.6.4 GR1; §14.9.48.4 GR7; §14.9.22.3 SR3: its class follows identifier-1), so it
/// can never be the mismatch.</para>
/// </summary>
internal static class AllOrNothingClass
{
    /// <summary>True when some TRIGGERING operand is of class <paramref name="governing"/> and some operand of
    /// <paramref name="all"/> is of a decidable OTHER class. <paramref name="triggers"/> defaults to
    /// <paramref name="all"/>; INSPECT passes its elementary items and literals only ("references an ELEMENTARY
    /// data item or literal of class …" — while every operand, a group included, must then conform).
    /// <para>Each operand is compared by its Table-2 CLASS COLUMN (<see cref="IntrinsicArgumentRules.TableTwoClass"/>),
    /// never by the lattice's refined member: a national numeric-edited item IS class national (kb/Work PB1128 —
    /// compared raw, its refined member was "another class" and INSPECT refused the legal N" " beside it). UNSTRING's
    /// SR3 is CATEGORY-worded, but its SR2/SR4 admit no numeric-edited operand, so the projection cannot move it.</para></summary>
    public static bool Violated(CobolClass governing, IReadOnlyCollection<CobolClass?> all,
                                IReadOnlyCollection<CobolClass?>? triggers = null)
    {
        bool triggered = false;
        foreach (var c in triggers ?? all)
            if (c is { } t && IntrinsicArgumentRules.TableTwoClass(t) == governing) { triggered = true; break; }
        if (!triggered) return false;
        foreach (var c in all)
            if (c is { } cls && IntrinsicArgumentRules.TableTwoClass(cls) != governing) return true;
        return false;
    }

    /// <summary>The ONE message suffix for a violation, naming the rule the caller cites.</summary>
    public static string Offence(CobolClass governing, string rule) =>
        $"mixes class {governing.ToString().ToUpperInvariant()} with an operand of another class; {rule} requires "
        + $"that if any of the operands it names is of class {governing.ToString().ToLowerInvariant()}, all of them shall be";
}
