// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE §13.18.60.2 DESCRIPTOR DRIFT PIN (kb/Work PB389). The USAGE OBJECT REFERENCE general format prints
/// THREE stacked alternatives over four independent axes —
/// <code>
/// OBJECT REFERENCE ⎡ interface-name-1                            ⎤
///                  ⎢ [ FACTORY OF ] ACTIVE-CLASS                 ⎥
///                  ⎣ [ FACTORY OF ] object-class-name-1 [ ONLY ] ⎦
/// </code>
/// — plus the bare form §13.18.60.4 GR22 b) calls a universal object reference: EIGHT writable shapes. The
/// compiler modelled ONE axis (a nullable class name), so ACTIVE-CLASS and ONLY were rejected outright as legal
/// COBOL-2002+ source, FACTORY OF was loud-staged, and every §14.9.39.3 SET format-5 rule that discriminates on
/// an unmodelled axis had no expressible subject.
///
/// <para><b>What this pins, and why it is a ROUND TRIP rather than a shape check.</b> Each shape is DECLARED in
/// real source, BOUND, and then asked a question whose answer depends on the descriptor surviving intact —
/// declare → bind → descriptor → diagnostic. A shape whose axes were dropped at any stage answers the wrong
/// way at the end, so no stage can silently normalize one shape into another.</para>
///
/// <para><b>Both directions, deliberately</b> (feedback_green_gates_arent_evidence). The IDENTITY theory proves
/// every shape is ACCEPTED assigning to itself — which is what fails first if a new
/// <c>ObjectRefKind</c> is added without an arm in <c>OoConformance.ObjectRefAssignmentMismatch</c>, since the
/// unhandled kind falls to a refusing arm. The UNIVERSAL-SENDER theory proves the same shapes REFUSE a sender
/// their rule's closed list excludes: §14.9.39.3 SR10 (an interface-name receiver), SR12 (an object-class-name
/// receiver) and SR14 (an ACTIVE-CLASS receiver) each enumerate their permitted senders, and a universal
/// reference is in none of the three lists. Only the bare receiver accepts it — SR8 with GR22 b).</para>
/// </summary>
public sealed class ObjectRefDescriptorDriftTests
{
    /// <summary>The EIGHT writable shapes of the §13.18.60.2 general format, as the USAGE phrase text that
    /// follows <c>USAGE OBJECT REFERENCE</c>. <c>DRIFTI</c> is an interface, <c>DRIFTC</c> a class implementing
    /// it on both halves; ACTIVE-CLASS resolves to the containing class (§13.18.60.3 SR16 admits it in a
    /// method's LOCAL-STORAGE, which is where every item below is declared).</summary>
    public static TheoryData<string> Shapes =>
    [
        "",                                 // GR22 b) — the universal object reference
        " DRIFTI",                          // GR22 c) — interface-name-1
        " DRIFTC",                          // GR22 d)1.b.
        " DRIFTC ONLY",                     // GR22 d)2.b.
        " FACTORY OF DRIFTC",               // GR22 d)1.a.
        " FACTORY OF DRIFTC ONLY",          // GR22 d)2.a.
        " ACTIVE-CLASS",                    // GR22 e)2.
        " FACTORY OF ACTIVE-CLASS",         // GR22 e)1.
    ];

    /// <summary>Every shape assigning to ITSELF conforms — the identical-description case, which every one of
    /// SR10 / SR12 / SR14 admits through its own first alternative (SR10 a), SR12 a)1./a)2. with a)3., SR14 a)).
    /// A shape whose axes were lost between the grammar and the descriptor answers otherwise.</summary>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void EveryGeneralFormatShape_ConformsToItself(string shape)
    {
        var (ok, detail) = Compile(Source(shape, "SET X TO Y."));
        Assert.True(ok, $"USAGE OBJECT REFERENCE{shape} does not accept a sender of its OWN description: {detail}");
    }

    /// <summary>The failing direction: a UNIVERSAL sender is in NO shape's permitted-sender list except the
    /// universal receiver's (which has no list — SR8 leaves it unconstrained, GR22 b) "its content may be a
    /// reference to any object"). Without this, an identity-only pin would pass over a matrix that checks
    /// nothing at all.</summary>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void EveryTypedShape_RefusesAUniversalSender(string shape)
    {
        var (ok, detail) = Compile(Source(shape, "SET X TO U."));
        if (shape.Length == 0)
        {
            Assert.True(ok, "a UNIVERSAL receiver accepts any object reference (ISO §14.9.39.3 SR8; "
                            + "§13.18.60.4 GR22 b)): " + detail);
            return;
        }
        Assert.False(ok, $"USAGE OBJECT REFERENCE{shape} accepted a UNIVERSAL sender — SR10 / SR12 / SR14 each "
                         + "enumerate a CLOSED list of senders and a universal reference is in none of them");
        Assert.Contains("COBOLNET0867", detail);
    }

    /// <summary>⛔ The class-NAME sender's governing rule is READ OFF THE RECEIVER (kb/Work PB451). Every
    /// <c>ObjectRefKind</c> shall map to its OWN §14.9.39.3 rule — SR11 for an interface-name receiver, SR13
    /// for an object-class-name receiver, SR14 for an ACTIVE-CLASS receiver, SR8 for the universal one —
    /// because each of those rules names the receiver's description in its own precondition. A new kind added
    /// without an arm falls to the discard and collides with SR8, which is exactly the hard-coded-one-rule
    /// defect this replaced: the SET site printed "SR13" for all four, so a program refused under SR11 or SR14
    /// was told it had broken a rule whose precondition was false for it.</summary>
    [Fact]
    public void EveryObjectRefKind_HasItsOwnClassNameSenderRule()
    {
        var kinds = Enum.GetValues<CobolNet.Binding.Model.ObjectRefKind>();
        var byRule = kinds.ToLookup(CobolNet.Compiler.Oo.OoConformance.ClassNameSenderRule);
        var collided = byRule.Where(g => g.Count() > 1)
                             .Select(g => $"{g.Key} ← {string.Join(", ", g)}").ToList();
        Assert.True(collided.Count == 0,
            "two ObjectRefKind values share one §14.9.39.3 rule label — a kind added without an arm in "
            + "OoConformance.ClassNameSenderRule fell through to the discard: " + string.Join(" | ", collided));
        Assert.Equal("ISO §14.9.39.3 SR11",
            CobolNet.Compiler.Oo.OoConformance.ClassNameSenderRule(CobolNet.Binding.Model.ObjectRefKind.Interface));
        Assert.Equal("ISO §14.9.39.3 SR13",
            CobolNet.Compiler.Oo.OoConformance.ClassNameSenderRule(CobolNet.Binding.Model.ObjectRefKind.ObjectClass));
        Assert.Equal("ISO §14.9.39.3 SR14",
            CobolNet.Compiler.Oo.OoConformance.ClassNameSenderRule(CobolNet.Binding.Model.ObjectRefKind.ActiveClass));
    }

    /// <summary>Two items of the given shape (X the receiver, Y the sender), a universal U, and the statement
    /// under test — all in the LOCAL-STORAGE of an instance method of DRIFTC, the one place §13.18.60.3 SR16
    /// admits ACTIVE-CLASS alongside every other shape.</summary>
    private static string Source(string shape, string stmt) => ($$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DRIFTP.
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        END PROGRAM DRIFTP.

        IDENTIFICATION DIVISION.
        INTERFACE-ID. DRIFTI.
        PROCEDURE DIVISION.
        METHOD-ID. PING.
        PROCEDURE DIVISION.
        END METHOD PING.
        END INTERFACE DRIFTI.

        IDENTIFICATION DIVISION.
        CLASS-ID. DRIFTC.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            INTERFACE DRIFTI.
        IDENTIFICATION DIVISION.
        FACTORY. IMPLEMENTS DRIFTI.
        PROCEDURE DIVISION.
        METHOD-ID. PING.
        PROCEDURE DIVISION.
        MAIN.
            CONTINUE.
        END METHOD PING.
        END FACTORY.
        IDENTIFICATION DIVISION.
        OBJECT. IMPLEMENTS DRIFTI.
        PROCEDURE DIVISION.
        METHOD-ID. PING.
        PROCEDURE DIVISION.
        MAIN.
            CONTINUE.
        END METHOD PING.
        METHOD-ID. MK.
        DATA DIVISION.
        LOCAL-STORAGE SECTION.
        01 X USAGE OBJECT REFERENCE{{shape}}.
        01 Y USAGE OBJECT REFERENCE{{shape}}.
        01 U USAGE OBJECT REFERENCE.
        PROCEDURE DIVISION.
        MAIN.
            {{stmt}}
        END METHOD MK.
        END OBJECT.
        END CLASS DRIFTC.
        """).Replace("\r\n", "\n");

    private static (bool Ok, string Detail) Compile(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_ORD_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, source);
            var r = CompiledProgramCache.Compile(new CobolNet.CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: 2002, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success, string.Join("\n", r.Errors));
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
