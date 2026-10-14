// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB3102 — <b>EC-OO-RESOURCE is declared by the statement that raises it.</b> ISO §16.2.1.2 GR2: "If resources
/// needed to create a new object are not available, the returned object reference is set to NULL, and the
/// EC-OO-RESOURCE exception condition is set to exist and is propagated back to the runtime element that invoked the
/// New method." The runtime raise site (<c>BASE__FACTORY.__New</c> → <c>ExceptionState.OoResourceError</c>) fires only
/// while <c>ExceptionState.OoResourceChecking</c> is true, and the one thing that sets that flag is the INVOKE statement's
/// checking guard — which the binder wrapped only for the EC-OO-NULL / -METHOD / -UNIVERSAL names, so
/// <c>&gt;&gt;TURN EC-OO-RESOURCE CHECKING ON</c> reached nothing (the PB452 / PB1453 shape; PB1524 landed the raise site and
/// the unit test that injects the failure, but no statement ever turned the flag on). A golden cannot exhaust memory
/// (<see cref="StandardClassBaseNewResourceTests"/> injects the failure at the creation seam), so this reads the emitted
/// C#: the guard around an INVOKE … "NEW" sets the flag exactly when the name is enabled at the statement.
/// </summary>
public sealed class OoResourceStatementGuardTests
{
    private static string EmitCSharp(string turn)
    {
        string dir = Directory.CreateTempSubdirectory("pb3102oo").FullName;
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, $$"""
                {{turn}}
                       IDENTIFICATION DIVISION.
                       PROGRAM-ID. PB3102OO.
                       ENVIRONMENT DIVISION.
                       CONFIGURATION SECTION.
                       REPOSITORY.
                           CLASS CNEW.
                       DATA DIVISION.
                       WORKING-STORAGE SECTION.
                       01 C USAGE OBJECT REFERENCE CNEW.
                       PROCEDURE DIVISION.
                       MAIN-P.
                           INVOKE CNEW "NEW" RETURNING C.
                           STOP RUN.
                       END PROGRAM PB3102OO.

                       IDENTIFICATION DIVISION.
                       CLASS-ID. CNEW INHERITS FROM BASE.
                       ENVIRONMENT DIVISION.
                       CONFIGURATION SECTION.
                       REPOSITORY.
                           CLASS BASE.
                       IDENTIFICATION DIVISION.
                       OBJECT.
                       PROCEDURE DIVISION.
                       END OBJECT.
                       END CLASS CNEW.
                """);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "p.dll"), DialectLevel: 2023, CheckOnly: false, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "the program must compile: " + string.Join("; ", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { } }
    }

    [Fact]
    public void InvokeNew_WithTheNameEnabled_SetsTheCheckingFlagAroundTheStatement()
    {
        string cs = EmitCSharp("       >>TURN EC-OO-RESOURCE CHECKING ON");
        Assert.Contains("ExceptionState.OoResourceChecking = true;", cs, StringComparison.Ordinal);
    }

    [Fact]
    public void InvokeNew_WithCheckingOff_EmitsNoGuard()
    {
        string cs = EmitCSharp("");
        Assert.DoesNotContain("OoResourceChecking", cs, StringComparison.Ordinal);
    }
}
