// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1425's sweep: ISO §8.8.1.1 admits only a numeric identifier in an arithmetic expression, and the
/// <c>--permissive</c> digit-decoding extension reads an alphanumeric item's digit CHARACTERS. An object reference holds
/// a reference, not characters (§8.5.2.1 Table 2's class object), so under --permissive it is still refused — it used to
/// be "accepted" with a warning and the generated program failed to build (Roslyn CS1503, object → Int128). SELF, which
/// the expression tier now carries as an identifier (§8.4.3.1.2 Format 6), takes the same verdict.
/// </summary>
public sealed class ReferenceOperandArithmeticTests
{
    private const string Source =
        "       IDENTIFICATION DIVISION.\n"
        + "       PROGRAM-ID. PB1425RM.\n"
        + "       PROCEDURE DIVISION.\n"
        + "       MAIN.\n"
        + "           STOP RUN.\n"
        + "       END PROGRAM PB1425RM.\n"
        + "       IDENTIFICATION DIVISION.\n"
        + "       CLASS-ID. PB1425RA INHERITS FROM BASE.\n"
        + "       ENVIRONMENT DIVISION.\n"
        + "       CONFIGURATION SECTION.\n"
        + "       REPOSITORY.\n"
        + "           CLASS BASE.\n"
        + "       IDENTIFICATION DIVISION.\n"
        + "       OBJECT.\n"
        + "       PROCEDURE DIVISION.\n"
        + "       METHOD-ID. M.\n"
        + "       DATA DIVISION.\n"
        + "       LOCAL-STORAGE SECTION.\n"
        + "       01 O USAGE OBJECT REFERENCE.\n"
        + "       01 N PIC 9.\n"
        + "       PROCEDURE DIVISION.\n"
        + "           COMPUTE N = O\n"
        + "           COMPUTE N = SELF.\n"
        + "       END METHOD M.\n"
        + "       END OBJECT.\n"
        + "       END CLASS PB1425RA.\n";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObjectReferenceInArithmetic_IsRefusedUnderEveryDialect(bool permissive)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_RefArith_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "pb1425r.cob");
            File.WriteAllText(src, Source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, "pb1425r.dll"),
                DialectLevel: 2002, Permissive: permissive, SourceFormat: InitialReferenceFormat.Auto));
            Assert.False(r.Success);
            // Both operands are refused by §8.8.1.1 itself, as errors — never handed to the back end.
            Assert.Equal(2, r.Errors.Count(e => e.Contains("error COBOLNET0844:", StringComparison.Ordinal)));
            Assert.DoesNotContain(r.Errors, e => e.Contains("backend compilation failed", StringComparison.Ordinal));
            Assert.DoesNotContain(r.Errors, e => e.Contains("digit-decoding", StringComparison.Ordinal));
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
