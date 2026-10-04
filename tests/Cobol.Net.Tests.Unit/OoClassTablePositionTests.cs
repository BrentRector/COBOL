// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY OO PASS-1 DIAGNOSTIC CARRIES ITS SOURCE POSITION (kb/Work PB975). <c>OoClassTable.Build</c> reported
/// through <c>edition.Error</c> without positioning the diagnostic cursor, so an unresolved INHERITS, a mismatched
/// END CLASS or a duplicate method printed <c>error COBOLNET0821: …</c> with no <c>file(line,col):</c> — in a
/// compilation group of several classes the user could not find the clause. Each source below marks the line a
/// diagnostic must land on with an inline <c>*&gt; @CODE</c> comment; the test asserts that EVERY error carries a
/// position and that each marked (code, line) pair was reported. A new pass-1 report that forgets its scope fails
/// the "every error is positioned" half the first time any source here reaches it.
/// </summary>
public sealed class OoClassTablePositionTests
{
    private static readonly Regex Positioned = new(@"\((\d+),(\d+)\): error (COBOLNET\d{4})", RegexOptions.Compiled);
    private static readonly Regex Marker = new(@"\*> @(\d{4})", RegexOptions.Compiled);

    public static TheoryData<string, string> Sources => new()
    {
        { "INHERITS", """
            IDENTIFICATION DIVISION.
            CLASS-ID. PT1 INHERITS NOBASE. *> @0821
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS NOBASE.
            END CLASS PT1.
            """ },
        { "END-CLASS", """
            IDENTIFICATION DIVISION.
            CLASS-ID. PT2.
            END CLASS PT2X. *> @2793
            """ },
        { "METHODS", """
            IDENTIFICATION DIVISION.
            CLASS-ID. PT3.
            FACTORY.
            PROCEDURE DIVISION.
            METHOD-ID. NEW.
            END METHOD NEW.
            END FACTORY.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M1.
            END METHOD M1.
            METHOD-ID. M1. *> @0822
            END METHOD M1.
            METHOD-ID. M2.
            END METHOD M9. *> @2793
            METHOD-ID. M3 OVERRIDE. *> @0838
            END METHOD M3.
            END OBJECT.
            END CLASS PT3.
            """ },
        { "MULTIPLE-BASES", """
            IDENTIFICATION DIVISION.
            CLASS-ID. PB1.
            END CLASS PB1.
            IDENTIFICATION DIVISION.
            CLASS-ID. PB2.
            END CLASS PB2.
            IDENTIFICATION DIVISION.
            CLASS-ID. PT5 INHERITS PB1 PB2. *> @0849
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS PB1
                CLASS PB2.
            END CLASS PT5.
            """ },
        // kb/Work PB1505 / PB1020: the §11.3.3 INHERITS rules (COBOLNET2791) land on the INHERITS name, the
        // parameterized definition's included; kb/Work PB1503: the §11.7.3 property-method rules (COBOLNET2790) on the
        // METHOD-ID and on the subject entry.
        { "INHERITS-RULES", """
            IDENTIFICATION DIVISION.
            CLASS-ID. PS1 INHERITS PS1. *> @2791
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS PS1.
            END CLASS PS1.
            IDENTIFICATION DIVISION.
            CLASS-ID. PS2 FINAL.
            END CLASS PS2.
            IDENTIFICATION DIVISION.
            CLASS-ID. PS3 INHERITS PS2 PS2. *> @2791
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS PS2.
            END CLASS PS3.
            IDENTIFICATION DIVISION.
            CLASS-ID. PS4 INHERITS PS2 USING T. *> @0839
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS PS2
                CLASS T.
            END CLASS PS4.
            """ },
        { "PROPERTY-METHODS", """
            IDENTIFICATION DIVISION.
            CLASS-ID. PM1.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BAL PIC 9(4) PROPERTY WITH NO GET. *> @2790
            PROCEDURE DIVISION.
            METHOD-ID. GET PROPERTY BAL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-R PIC 9(4).
            PROCEDURE DIVISION RETURNING LK-R.
            END METHOD.
            METHOD-ID. SET PROPERTY BAX. *> @2790
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-A PIC 9(4).
            01 LK-B PIC 9(4).
            PROCEDURE DIVISION USING LK-A LK-B.
            END METHOD.
            END OBJECT.
            END CLASS PM1.
            """ },
        { "INTERFACES", """
            IDENTIFICATION DIVISION.
            INTERFACE-ID. PI1.
            END INTERFACE PI1X. *> @2793
            IDENTIFICATION DIVISION.
            INTERFACE-ID. PI2 INHERITS NOIF. *> @0840
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                INTERFACE NOIF.
            END INTERFACE PI2.
            IDENTIFICATION DIVISION.
            CLASS-ID. PC3.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                INTERFACE NOIF2.
            IDENTIFICATION DIVISION.
            OBJECT.
            IMPLEMENTS NOIF2. *> @0840
            END OBJECT.
            END CLASS PC3.
            """ },
    };

    [Theory]
    [MemberData(nameof(Sources))]
    public void EveryPass1Diagnostic_IsPositionedAtItsClause(string label, string source)
    {
        string dir = Directory.CreateTempSubdirectory("oopos").FullName;
        try
        {
            string path = Path.Combine(dir, "PT" + label + ".cob");
            File.WriteAllText(path, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(path, DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            Assert.False(r.Success, $"{label}: expected a rejection");
            string all = string.Join("\n", r.Errors);
            var unpositioned = r.Errors.Where(e => e.Contains("error COBOLNET", StringComparison.Ordinal)
                && !Positioned.IsMatch(e)).ToList();
            Assert.True(unpositioned.Count == 0, $"{label}: diagnostics with no source position:\n"
                + string.Join("\n", unpositioned));
            var got = r.Errors.Select(e => Positioned.Match(e)).Where(m => m.Success)
                .Select(m => (Code: m.Groups[3].Value, Line: int.Parse(m.Groups[1].Value))).ToHashSet();
            string[] lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                foreach (Match mk in Marker.Matches(lines[i]))
                    Assert.True(got.Contains(("COBOLNET" + mk.Groups[1].Value, i + 1)),
                        $"{label}: expected COBOLNET{mk.Groups[1].Value} at line {i + 1}; got:\n{all}");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
