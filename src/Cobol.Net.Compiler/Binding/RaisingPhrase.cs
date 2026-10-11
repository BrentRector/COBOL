// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Frontend.Generated;
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE PARTITION OF A PROCEDURE-DIVISION-HEADER RAISING PHRASE (ISO §14.2.1 / §14.2.2 SR7–SR9;
/// kb/Work PB815 + PB814). Every source element whose header can carry the phrase — program, function, program
/// and function prototypes (<c>EcBinder.EcCollectPdRaising</c>), method definitions and method prototypes
/// (<c>DataBinder.OoBindMethodSignature</c>) — calls THIS, and gets back the tuples
/// (<see cref="RaisingTarget"/>) the consuming rules compare against.
/// <para><b>What it replaced.</b> Two verbatim copies — the PD-header arm and the METHOD-ID arm — each
/// partitioning a bare word list into "EC name" and "class name", each carrying the same comment that SR9's
/// interface alternative was "a later refinement", and neither able to see FACTORY because the grammar had
/// dropped it (<c>raisingClause : RAISING cobolWord+</c>). A fix to one would have left the other refusing the
/// same source (feedback_two_arm_dispatch); the extraction is the fix.</para>
/// <para><b>Why the partition is a bind-time question.</b> The three alternatives are each one user-defined
/// word; only the EC catalog (§14.6.13.1) and the REPOSITORY scope of THIS source element (§8.4.6.4 — through
/// <see cref="OoNameResolution.Lookup"/>, the one class/interface funnel) can tell them apart. The grammar keeps
/// the one axis it can see — the FACTORY phrase — on <c>raisingTarget</c>.</para>
/// </summary>
internal static class RaisingPhrase
{
    /// <summary>Partition every element of <paramref name="rc"/>. <paramref name="where"/> is the header's
    /// spelling in a message (<c>"PROCEDURE DIVISION RAISING"</c>, or the method's site). A rejected element is
    /// reported (COBOLNET0858) and left out of the result.</summary>
    public static List<RaisingTarget> Partition(Core.RaisingClauseContext? rc, OoClassTable? table,
        EditionContext edition, string where)
    {
        var targets = new List<RaisingTarget>();
        if (rc is null) return targets;
        foreach (var t in rc.raisingTarget())
        {
            var word = t.cobolWord();
            string up = CobolNames.UpperFold(word.GetText());
            bool factory = t.FACTORY() is not null;
            // Every report below names THIS element, so it is positioned at it — a method prototype's header has no
            // enclosing position of its own, and its refusals printed with none.
            using var atTarget = edition.At(t);

            // exception-name-1 (SR7). Direct TryGet, not the EcNameResolution funnel: an unresolved word here may
            // legally be a class-name or an interface-name (SR8/SR9), so the funnel's unknown-name error does
            // not apply — but an accepted name still gets the §15.33 width advisory (kb/Work R05). A word written
            // after FACTORY OF is object-class-name-1 by the general format, so it never reaches this arm.
            if (!factory && ExceptionCatalog.TryGet(up, out var info))
            {
                if (info.Level is 3 && info.Level2Parent is "EC-USER")
                {
                    EcNameResolution.Advise(edition, info);
                    targets.Add(new RaisingTarget(RaisingTargetKind.ExceptionName, up, false));
                }
                else
                    edition.Error("COBOLNET0858", $"{where} {up}: an exception-name here shall be a level-3 "
                        + "EC-USER name (ISO §14.2.2 SR7)");
                continue;
            }

            // object-class-name-1 (SR8) or interface-name-1 (SR9) — both "specified in the REPOSITORY paragraph",
            // i.e. the §8.4.6.4 scope of THIS source element, asked through the funnel's non-diagnosing half.
            var found = OoNameResolution.Lookup(table, word, up, OoNameResolution.Want.Either);
            // A parameter-name of the parameterized definition bound as itself (§11.3.4 GR6 / §11.6.4 GR4 admit it
            // wherever a class-name or interface-name is; kb/Work PB2051): legal here, and each expansion names the
            // actual. Its kind is its REPOSITORY specifier's, so FACTORY OF an interface formal is still SR8's refusal.
            // An INTERFACE formal under FACTORY OF falls through to the interface arm's refusal below.
            if (found.IsFormal
                && (!factory || OoNameResolution.Lookup(table, word, up, OoNameResolution.Want.Class).IsFormal))
                continue;
            if (found.Class is not null)
            {
                targets.Add(new RaisingTarget(RaisingTargetKind.ObjectClass, up, factory));
                continue;
            }
            if (found.Interface is not null || found.IsFormal)
            {
                // The interface-name-1 alternative of §14.2.1 carries no FACTORY phrase: `[ FACTORY OF ]` is
                // printed on the object-class-name-1 line only.
                if (factory)
                    edition.Error("COBOLNET0858", $"{where} FACTORY OF {up}: '{up}' is an interface, and the "
                        + "FACTORY phrase belongs to the object-class-name-1 alternative only (ISO §14.2.1 general "
                        + "format; §14.2.2 SR8)");
                else
                    targets.Add(new RaisingTarget(RaisingTargetKind.Interface, up, false));
                continue;
            }
            edition.Error("COBOLNET0858", factory
                ? $"{where} FACTORY OF {up}: not a class this source element may reference (ISO §14.2.2 SR8 / "
                  + "§8.4.6.4)"
                : $"{where} {up}: not an exception-name, and not a class or interface this source element may "
                  + "reference (ISO §14.2.2 SR7–SR9 / §8.4.6.4)");
        }
        return targets;
    }
}
