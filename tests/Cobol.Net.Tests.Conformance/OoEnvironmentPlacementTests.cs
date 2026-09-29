// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1076 + PB813 — the ONE OO environment-division placement table (<c>OoEnvironmentRules</c>), one row
/// per (definition kind × element). Every rule quotes its clause; each citation was run through
/// <c>cite.py --check</c> (§12.3.3 SR2, SR3; §12.4.3 SR1; §12.3.7.3 SR2, SR3). The legal rows are controls: the
/// table must not refuse what the standard admits (a bare method ENVIRONMENT DIVISION; CURSOR in a factory;
/// ALPHABET in an interface).
/// </summary>
public sealed class OoEnvironmentPlacementTests
{
    private const string Io = "INPUT-OUTPUT SECTION. FILE-CONTROL. SELECT F ASSIGN TO \"x.dat\".";

    private static string Source(string kind, string env) => kind switch
    {
        "interface" => $"""
            IDENTIFICATION DIVISION.
            INTERFACE-ID. PLI.
            {env}
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            PROCEDURE DIVISION.
            END METHOD M1.
            END INTERFACE PLI.
            """,
        _ => $"""
            IDENTIFICATION DIVISION.
            CLASS-ID. PLC INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            {(kind == "class" ? env : "")}
            IDENTIFICATION DIVISION.
            FACTORY.
            {(kind == "factory" ? env : "")}
            PROCEDURE DIVISION.
            END FACTORY.
            IDENTIFICATION DIVISION.
            OBJECT.
            {(kind == "object" ? env : "")}
            PROCEDURE DIVISION.
            METHOD-ID. M.
            {(kind == "method" ? env : "")}
            PROCEDURE DIVISION.
            PARA-A.
                CONTINUE.
            END METHOD M.
            END OBJECT.
            END CLASS PLC.
            """,
    };

    [Theory]
    // §12.3.3 SR2 — no CONFIGURATION SECTION in a method; a bare header (§10.6.1, §12.2.1) is legal.
    [InlineData("method", "ENVIRONMENT DIVISION.", false)]
    [InlineData("method", "ENVIRONMENT DIVISION. CONFIGURATION SECTION.", true)]
    // §12.4.3 SR1 — the input-output section: factory and instance only.
    [InlineData("method", "ENVIRONMENT DIVISION. " + Io, true)]
    [InlineData("class", Io, true)]
    [InlineData("interface", "ENVIRONMENT DIVISION. " + Io, true)]
    [InlineData("factory", "ENVIRONMENT DIVISION. " + Io, false)]
    [InlineData("object", "ENVIRONMENT DIVISION. " + Io, false)]
    // §12.3.3 SR3 — no SOURCE-COMPUTER / OBJECT-COMPUTER / REPOSITORY in a factory or instance (both arms).
    [InlineData("factory", "ENVIRONMENT DIVISION. CONFIGURATION SECTION. REPOSITORY. CLASS BASE.", true)]
    [InlineData("object", "ENVIRONMENT DIVISION. CONFIGURATION SECTION. REPOSITORY. CLASS BASE.", true)]
    [InlineData("factory", "ENVIRONMENT DIVISION. CONFIGURATION SECTION. SOURCE-COMPUTER. X.", true)]
    [InlineData("object", "ENVIRONMENT DIVISION. CONFIGURATION SECTION. OBJECT-COMPUTER. X.", true)]
    // §12.3.7.3 SR2 — factory / instance SPECIAL-NAMES: CURSOR and CRT STATUS only.
    [InlineData("factory", "ENVIRONMENT DIVISION. CONFIGURATION SECTION. SPECIAL-NAMES. DECIMAL-POINT IS COMMA.", true)]
    [InlineData("object", "ENVIRONMENT DIVISION. CONFIGURATION SECTION. SPECIAL-NAMES. CURRENCY SIGN IS \"E\".", true)]
    [InlineData("factory", "ENVIRONMENT DIVISION. CONFIGURATION SECTION. SPECIAL-NAMES. CURSOR IS WS-CUR.", false)]
    // §12.3.7.3 SR3 — interface SPECIAL-NAMES: ALPHABET, CURRENCY, DECIMAL-POINT, LOCALE only.
    [InlineData("interface", "ENVIRONMENT DIVISION. CONFIGURATION SECTION. SPECIAL-NAMES. CLASS DIG IS \"0\" THRU \"9\".", true)]
    [InlineData("interface", "ENVIRONMENT DIVISION. CONFIGURATION SECTION. SPECIAL-NAMES. DECIMAL-POINT IS COMMA.", false)]
    public void EnvironmentPlacement_FollowsTheOneTable(string kind, string env, bool refused)
    {
        var (_, errors, _) = EditionHarness.CompileFull(Source(kind, env).Replace("\r\n", "\n"), 2023);
        if (refused)
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2644");
        else
            EditionHarness.AssertNoDiagnostic(errors, "COBOLNET2644");
    }
}
