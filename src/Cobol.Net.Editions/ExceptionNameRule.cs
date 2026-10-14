// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Editions;

/// <summary>A written exception-name the targeted edition does not have: the catalog descriptor to report under and the
/// message. Both halves of the report ride together so every channel that prints it (the front end's
/// <c>DiagnosticBag</c>, the binder's <c>EditionContext</c>) prints the same text under the same code.</summary>
public readonly record struct ExceptionNameViolation(DiagnosticDescriptor Descriptor, string Message);

/// <summary>
/// ⛔ THE ONE HOME OF "IS THIS WRITTEN WORD AN EXCEPTION-NAME THE TARGETED EDITION HAS" (kb/Work PB2740, R05). ISO
/// §7.3.25.3 SR2 — "Exception-name-1 shall be one of the exception names listed in 14.6.13.1, Exception conditions" —
/// is a SYNTAX rule of the <c>&gt;&gt;TURN</c> directive, and §7.2.1 requires a compiler directive to be "syntactically
/// correct in the initial source text and library text", the false path of a <c>&gt;&gt;IF</c> included. Two channels
/// therefore have to ask the same question and print the same answer: the directive stage
/// (<c>TurnDirectiveProcessor.ParseTurn</c>, which a COMPILED line and an OMITTED-branch line both reach) and the
/// binder's written-name funnel (<c>EcNameResolution.TryResolve</c>: USE, RAISE, RAISING, the PERFORM WHEN list). The
/// rule used to live only in the binder, which reads compiled events alone, so
/// <c>&gt;&gt;IF 1 = 0 / &gt;&gt;TURN EC-NOSUCH CHECKING ON / &gt;&gt;END-IF</c> compiled clean while the same directive
/// outside the <c>&gt;&gt;IF</c> was COBOLNET0711.
/// <para>The rule lives BELOW both channels (the front end cannot reference the binder), in the assembly that owns the
/// diagnostic catalog, so the next written-name site calls it and cannot write a third copy. What stays in the binder
/// is the part that is not syntax: the level test of the RAISE contexts, the declined-module postures and the
/// §15.33 width advisory (<c>EcNameResolution.Advise</c>) — they need the compile's options, and an omitted branch is
/// never compiled.</para>
/// </summary>
public static class ExceptionNameRule
{
    /// <summary>Resolve <paramref name="raw"/> against the §14.6.13.1 catalog (and the open EC-USER- family,
    /// §14.6.13.1.1). False with <see cref="DiagnosticCatalog.EcNameUnknown"/> (COBOLNET0711) when it is neither;
    /// <paramref name="where"/> names the construct that wrote it (<c>&gt;&gt;TURN</c>, <c>RAISE</c>, …).</summary>
    public static bool TryGet(string raw, string where, out EcInfo info, out ExceptionNameViolation violation)
    {
        if (ExceptionCatalog.TryGet(raw, out info))
        {
            violation = default;
            return true;
        }
        violation = new ExceptionNameViolation(DiagnosticCatalog.EcNameUnknown,
            $"{where}: '{raw}' is not an exception-name of ISO/IEC 1989 §14.6.13.1 (and not a valid EC-USER- name; "
            + "this implementation defines no EC-IMP- name, DOC-A.1-99)");
        return false;
    }

    /// <summary>The per-name edition window: the 2023-only families (VERSION_CHANGE_REFERENCE rows 40/61) are not
    /// exception-names at <paramref name="dialectLevel"/> 2002/2014. Null when the name exists there, else
    /// <see cref="DiagnosticCatalog.EcNameIntroducedLater"/> (COBOLNET0878).</summary>
    public static ExceptionNameViolation? Window(EcInfo info, int dialectLevel, string where) =>
        info.IntroducedIn > dialectLevel
            ? new ExceptionNameViolation(DiagnosticCatalog.EcNameIntroducedLater,
                $"{where}: exception-name {info.Name} was introduced by ISO/IEC 1989:{info.IntroducedIn} — it "
                + $"requires --std {info.IntroducedIn} or later (targeting COBOL-{dialectLevel})")
            : null;
}
