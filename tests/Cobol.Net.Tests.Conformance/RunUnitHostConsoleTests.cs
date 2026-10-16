// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB3193: A RUN UNIT EMBEDDED IN A HOST PROCESS NEVER RECONFIGURES THE HOST'S CONSOLE. The process console
/// (<c>Console.Out</c>, <c>Console.Error</c> and <c>Console.OutputEncoding</c>) belongs to the process, and only an
/// entry that IS the process configures it: the <c>cobol</c> CLI's <c>Main</c> and the generated <c>Main</c>'s
/// <c>ProgramRegistry.RunModule</c> ("A standalone <c>cobol</c> executable IS its own host", docs/CONFORMANCE.md
/// DOC-A.1-167), which set UTF-8 there (DOC-A.1-59; kb/Work PB59, PB899). Under a host, the standard display device
/// is the host's standard output exactly as the host configured it (DOC-A.1-59), and the run unit only writes to it.
/// This is the console twin of <see cref="RunUnitHostExitCodeTests"/> (kb/Work PB3163), and the rule behind both is
/// §14.6.1's "A run unit is an independent entity that may be executed without communicating with, or being
/// coordinated with, any other run unit": a run unit that rewrote a process-wide cell would coordinate with the host
/// through it.
/// <para>The host is a real C# program compiled with Roslyn and run as its own process. It first gives its console an
/// encoding of its own (ISO 8859-1, code page 28591), then installs its OWN writers for standard output and standard
/// error, then activates a program through its generated host entry (<c>Cobol.HCDISP.HCDISP.Call</c>,
/// DESIGN-external-repository §4.3, the one host surface of §4.4) that DISPLAYs wide characters
/// on the standard display device and UPON SYSERR. Expected, derived: the host's two writers and its encoding are all
/// still in force afterwards, and each DISPLAY reached the host's writer for its device, every character intact
/// (a writer holds UTF-16, so nothing is encoded on the way). Before the fix the run unit's first activation set
/// UTF-8 on the host's console (code page 65001), or, where the console refused, replaced both writers with its
/// own, so the DISPLAYs bypassed the host's.</para>
/// </summary>
public sealed class RunUnitHostConsoleTests
{
    private const string DisplayProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. HCDISP.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            SYSERR IS ERROR-DEVICE.
        PROCEDURE DIVISION.
        P1.
            DISPLAY N"Grüße €".
            DISPLAY N"Fehler ß" UPON ERROR-DEVICE.
            GOBACK.
        """;

    private const string Host = """
        using System;
        using System.IO;
        using System.Text;

        public static class HcHost
        {
            private static string CodePoints(string s)
            {
                var sb = new StringBuilder();
                foreach (char c in s.TrimEnd('\r', '\n')) sb.Append(sb.Length == 0 ? "" : " ").Append(((int)c).ToString("X4"));
                return sb.ToString();
            }

            public static void Main()
            {
                var report = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
                Console.OutputEncoding = Encoding.Latin1;
                var hostOut = new StringWriter();
                var hostErr = new StringWriter();
                Console.SetOut(hostOut);
                Console.SetError(hostErr);
                TextWriter installedOut = Console.Out, installedErr = Console.Error;

                Cobol.HCDISP.HCDISP.Call();

                report.WriteLine("encoding=" + Console.OutputEncoding.CodePage);
                report.WriteLine("out=" + (ReferenceEquals(Console.Out, installedOut) ? "host" : "replaced"));
                report.WriteLine("err=" + (ReferenceEquals(Console.Error, installedErr) ? "host" : "replaced"));
                report.WriteLine("stdout=" + CodePoints(hostOut.ToString()));
                report.WriteLine("stderr=" + CodePoints(hostErr.ToString()));
            }
        }
        """;

    [Fact]
    public void ARunUnitUnderAHost_WritesThroughTheHostsConsoleAndLeavesItAlone()
    {
        string dir = CutRunner.NewTempDir("hcconsole");
        try
        {
            NonCobolElement.CompileCobol(dir, "HCDISP", DisplayProgram);
            NonCobolElement.CompileCSharp(dir, "HCHOST", Host, "HCDISP", "HCDISP");
            var (exit, stdout, stderr) = CutRunner.RunExit(Path.Combine(dir, "HCHOST.dll"), dir);
            Assert.True(exit == 0, $"host exit {exit}: {stderr}");
            Assert.Equal(
                "encoding=28591\n" +                                   // the host's own encoding is still in force
                "out=host\n" +                                         // so are its two writers
                "err=host\n" +
                "stdout=0047 0072 00FC 00DF 0065 0020 20AC\n" +        // "Grüße €" reached the host's stdout writer whole
                "stderr=0046 0065 0068 006C 0065 0072 0020 00DF", stdout); // "Fehler ß" reached its stderr writer whole
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
