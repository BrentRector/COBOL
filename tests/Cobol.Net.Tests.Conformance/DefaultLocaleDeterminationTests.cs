// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The witnesses for two Annex A.1 locale determinations (kb/Work PB2671), each pinned through an observable that
/// cannot drift with CLDR's date or number formats: the COLLATION ORDER of "ñu" and "nz" under
/// <c>ALPHABET … IS LOCALE</c> with no locale-name, which §12.3.7.4 7) e) (cite.py OK) defines "by the locale that is
/// current". Spanish has sorted ñ as a letter of its own after n in every CLDR release, so "nz" precedes "ñu"; German,
/// and the root collation, treat ñ as n with a secondary (accent) difference, so the primary letters u and z decide and
/// "ñu" precedes "nz".
/// <list type="bullet">
/// <item><b>DOC-A.1-118</b> — "Locale specification (how user and system defaults defined; at least one user and one
/// system default)" (cite.py OK §A.1 118)). §8.2.1 (cite.py OK): "The implementor shall specify the manner in which
/// the user default locale is defined and shall provide at least one user default locale for use in computing
/// environments that do not provide a user default locale", and the same of the system default. DETERMINATION: the user
/// default is <c>COBOL_USER_LOCALE</c> when it is set, else the process culture, else the root; the system default is
/// <c>COBOL_SYSTEM_LOCALE</c>, else the host's installed UI culture, else the root; a variable's value is a locale name
/// in the item-119 notation (trimmed; <c>C</c> names the root). The current locale starts as the user default
/// (§14.6.6 1), cite.py OK), and <c>SET LOCALE LC_ALL TO SYSTEM-DEFAULT</c> / <c>USER-DEFAULT</c> switch it
/// (§14.9.39.4).</item>
/// <item><b>DOC-A.1-120</b> — "Locale switch (whether a switch by a non-COBOL runtime module is recognized by COBOL)"
/// (cite.py OK §A.1 120)). §8.2.1 (cite.py OK): "It is implementor-defined whether, and for which locale categories, a
/// switch of current locale by a non-COBOL runtime module is utilized by COBOL." DETERMINATION: no, for every category:
/// the run unit reads the process culture once, when it starts, and a later change of <c>CultureInfo.CurrentCulture</c>
/// by the .NET host is not utilized, not even by <c>SET LOCALE … TO USER-DEFAULT</c>, whose user default is the one the
/// run unit started with. The next run unit starts from the culture in effect then.</item>
/// </list>
/// </summary>
public sealed class DefaultLocaleDeterminationTests
{
    private const string Program = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. A118LOC.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        OBJECT-COMPUTER. GENERIC-BOX
            PROGRAM COLLATING SEQUENCE IS LOC.
        SPECIAL-NAMES.
            ALPHABET LOC IS LOCALE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 NT PIC X(2) VALUE "ñu".
        01 NZ PIC X(2) VALUE "nz".
        01 TAG PIC X(7).
        PROCEDURE DIVISION.
        MAIN.
            MOVE "CURRENT" TO TAG
            PERFORM SHOW-ORDER
            SET LOCALE LC_ALL TO SYSTEM-DEFAULT
            MOVE "SYSTEM" TO TAG
            PERFORM SHOW-ORDER
            SET LOCALE LC_ALL TO USER-DEFAULT
            MOVE "USER" TO TAG
            PERFORM SHOW-ORDER
            GOBACK.
        SHOW-ORDER.
            IF NT < NZ
                DISPLAY TAG " N-TILDE-FIRST"
            ELSE
                DISPLAY TAG " NZ-FIRST"
            END-IF.
        END PROGRAM A118LOC.
        """;

    private const string SpanishFirst = "CURRENT NZ-FIRST\nSYSTEM  N-TILDE-FIRST\nUSER    NZ-FIRST";
    private const string SpanishSystem = "CURRENT N-TILDE-FIRST\nSYSTEM  NZ-FIRST\nUSER    N-TILDE-FIRST";
    private const string RootThroughout = "CURRENT N-TILDE-FIRST\nSYSTEM  N-TILDE-FIRST\nUSER    N-TILDE-FIRST";

    private static readonly string UserVariable = CobolNet.Runtime.LocaleState.UserDefaultVariable;
    private static readonly string SystemVariable = CobolNet.Runtime.LocaleState.SystemDefaultVariable;

    public static TheoryData<string, string?, string?, string?, string> Environments => new()
    {
        // Each default from its own variable, and the two kept apart: swapping the variables swaps the orders.
        { "user es, system de", "es-ES", "de-DE", null, SpanishFirst },
        { "user de, system es", "de-DE", "es-ES", null, SpanishSystem },
        // The value is trimmed and read in the item-119 notation: C is the root; a POSIX spelling names its locale.
        { "user C, system POSIX spelling", "C", " es_ES.UTF-8 ", null, SpanishSystem },
        // Neither variable set, in an environment with no culture data: both defaults still exist, as the root.
        { "no variables, invariant globalization", null, null, "1", RootThroughout },
    };

    [Theory]
    [MemberData(nameof(Environments))]
    public void TheUserAndSystemDefaults_AreDefinedByTheirEnvironmentVariables(
        string environment, string? user, string? system, string? invariantGlobalization, string expected)
    {
        string dir = CutRunner.NewTempDir("a118loc");
        try
        {
            NonCobolElement.CompileCobol(dir, "A118LOC", Program);
            var env = new Dictionary<string, string?>
            {
                [UserVariable] = user,
                [SystemVariable] = system,
                ["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = invariantGlobalization,
            };
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "A118LOC.dll"), dir, env: env);
            Assert.True(expected == stdout, $"{environment}: expected\n{expected}\nbut got\n{stdout}");
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>The non-COBOL element: a .NET host that sets the process culture, begins a run unit, switches the
    /// culture while the run unit is active, calls the program again, and finally begins a second run unit.</summary>
    private const string Host = """
        using System;
        using System.Globalization;
        using System.IO;
        using CobolNet.Runtime;

        public static class A120Host
        {
            private static void BeginRunUnit()
            {
                ProgramRegistry.Reset();
                var asm = System.Reflection.Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "A118LOC.dll"));
                var registrar = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<CobolNet.Runtime.Repository.CobolRepositoryAttribute>(asm)!.Registrar!;
                asm.GetType(registrar)!.GetMethod("EnsureRegistered", Type.EmptyTypes)!.Invoke(null, null);
            }

            public static int Main()
            {
                CultureInfo.CurrentCulture = new CultureInfo("es-ES");
                BeginRunUnit();
                Console.WriteLine("RUN UNIT 1, CULTURE es-ES");
                ProgramRegistry.CallProgram("A118LOC", "", Array.Empty<CobolArg>(), null);
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Console.WriteLine("RUN UNIT 1, HOST SWITCHED TO de-DE");
                ProgramRegistry.CallProgram("A118LOC", "", Array.Empty<CobolArg>(), null);
                BeginRunUnit();
                Console.WriteLine("RUN UNIT 2, CULTURE de-DE");
                ProgramRegistry.CallProgram("A118LOC", "", Array.Empty<CobolArg>(), null);
                return 0;
            }
        }
        """;

    [Fact]
    public void AHostCultureSwitchDuringARunUnit_IsNotUtilized_ButTheNextRunUnitStartsFromIt()
    {
        string dir = CutRunner.NewTempDir("a120host");
        try
        {
            NonCobolElement.CompileCobol(dir, "A118LOC", Program);
            NonCobolElement.CompileCSharp(dir, "A120HOST", Host, runtimeConfigOf: "A118LOC");
            // The defaults must come from the process culture, so neither variable may be set; the system default is
            // then the host's installed UI culture, which this test does not control, so the expected SYSTEM lines are
            // not asserted — the first and third lines of each block are.
            var env = new Dictionary<string, string?> { [UserVariable] = null, [SystemVariable] = null };
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "A120HOST.dll"), dir, env: env);
            var lines = stdout.Split('\n');
            Assert.True(lines.Length == 12, stdout);
            Assert.Equal(
                "RUN UNIT 1, CULTURE es-ES|CURRENT NZ-FIRST|USER    NZ-FIRST|" +
                "RUN UNIT 1, HOST SWITCHED TO de-DE|CURRENT NZ-FIRST|USER    NZ-FIRST|" +
                "RUN UNIT 2, CULTURE de-DE|CURRENT N-TILDE-FIRST|USER    N-TILDE-FIRST",
                string.Join("|", lines.Where(l => !l.StartsWith("SYSTEM", StringComparison.Ordinal))));
            Assert.Equal(0, exit);
            Assert.Equal("", stderr);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
