// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN UNSTRING RE-READS AN OPERAND A STORE OF THE STATEMENT CAN REACH, AND ONLY THAT OPERAND, AND THE QUESTION
/// "CAN A STORE REACH IT?" IS ANSWERED IN ONE PLACE (kb/Work PB1907). ISO §14.9.48.4 GR18 (Annex A.2 item 61) leaves
/// an UNSTRING whose sending or delimiter operand shares storage with a receiving, DELIMITER IN or COUNT IN
/// operand undefined; WiseOwl COBOL runs it in place, as GnuCOBOL does (docs/CONFORMANCE.md D-UNS1/D-UNS2), so the
/// emitter renders a re-read of the sender or of an identifier delimiter before each later receiving area.
/// <para>Two properties keep that true as the compiler grows. (1) The decision is
/// <c>StorageSharing.MayShare</c> and nothing in the UNSTRING emitter re-derives it from a record walk, so a new way
/// of sharing storage is one edit there. (2) The re-read is paid only where the predicate says so: an UNSTRING over
/// disjoint operands, or over a literal delimiter, emits the code it always did — which is also why the
/// Characterization emit snapshots did not move. This class reads the EMITTED C#, over every arm of the predicate
/// (same record, REDEFINES, LINKAGE) and over the operands that are never live (a literal, a function result), so a
/// new arm that forgets the re-read fails here even if no golden crosses it.</para>
/// </summary>
public sealed class UnstringLiveReadDriftTests
{
    private static string EmitCSharp(string source)
    {
        string dir = Directory.CreateTempSubdirectory("pb1907drift").FullName;
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "p.dll"), DialectLevel: 2023, CheckOnly: false, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "the drift program must compile: " + string.Join("; ", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { } }
    }

    private static string Program(string data, string unstring) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB1907DRIFT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {{data}}
        PROCEDURE DIVISION.
        MAIN.
            {{unstring}}
            STOP RUN.
        """;

    /// <summary>A sender re-read is an assignment to the sender local (the declaration has a type in front).</summary>
    private static readonly Regex SenderReread = new(@"^\s*__unsSrc\d+ = ", RegexOptions.Multiline);

    /// <summary>A delimiter re-read is an element assignment to the delimiter array.</summary>
    private static readonly Regex DelimiterReread = new(@"^\s*__unsDel\d+\[\d+\] = ", RegexOptions.Multiline);

    private const string Disjoint = """
        01 SND  PIC X(9) VALUE "a,b,c".
        01 DLM  PIC X    VALUE ",".
        01 R1   PIC X(3).
        01 R2   PIC X(3).
        """;

    [Fact]
    public void DisjointOperands_RenderNoReread()
    {
        string cs = EmitCSharp(Program(Disjoint, "UNSTRING SND DELIMITED BY DLM INTO R1 R2 END-UNSTRING"));
        Assert.DoesNotMatch(SenderReread, cs);
        Assert.DoesNotMatch(DelimiterReread, cs);
    }

    [Fact]
    public void SubordinateReceiverOfTheSender_RereadsTheSenderOnly()
    {
        const string data = """
            01 REC.
               05 F1 PIC X(4) VALUE "AAA,".
               05 F2 PIC X(4) VALUE "BBB,".
            01 R1   PIC X(8).
            """;
        string cs = EmitCSharp(Program(data, """UNSTRING REC DELIMITED BY "," INTO F2 R1 END-UNSTRING"""));
        Assert.Matches(SenderReread, cs);
        Assert.DoesNotMatch(DelimiterReread, cs);
    }

    [Fact]
    public void RedefinesViewOfTheSender_RereadsTheSender()
    {
        const string data = """
            01 SND  PIC X(9) VALUE "a,b,c".
            01 VW REDEFINES SND.
               05 V1 PIC X(3).
               05 V2 PIC X(6).
            01 R1   PIC X(3).
            """;
        string cs = EmitCSharp(Program(data, """UNSTRING SND DELIMITED BY "," INTO V2 R1 END-UNSTRING"""));
        Assert.Matches(SenderReread, cs);
    }

    [Fact]
    public void IdentifierDelimiterInAReceivedRecord_RereadsTheDelimiterOnly()
    {
        const string data = """
            01 SND  PIC X(9) VALUE "a,b,c".
            01 DLM  PIC X    VALUE ",".
            01 R2   PIC X(3).
            """;
        string cs = EmitCSharp(Program(data, "UNSTRING SND DELIMITED BY DLM INTO DLM R2 END-UNSTRING"));
        Assert.DoesNotMatch(SenderReread, cs);
        Assert.Matches(DelimiterReread, cs);
    }

    [Fact]
    public void LiteralDelimiter_IsNeverReread()
    {
        const string data = """
            01 REC.
               05 F1 PIC X(4) VALUE "AAA,".
               05 F2 PIC X(4) VALUE "BBB,".
            01 R1   PIC X(8).
            """;
        string cs = EmitCSharp(Program(data, """UNSTRING REC DELIMITED BY "," INTO F2 R1 END-UNSTRING"""));
        Assert.DoesNotMatch(DelimiterReread, cs);
    }

    [Fact]
    public void LinkageSender_RereadsBecauseItsStorageIsDecidedAtRunTime()
    {
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1907DRIFTL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC X(9).
            LINKAGE SECTION.
            01 LK-S PIC X(9).
            PROCEDURE DIVISION USING LK-S.
            MAIN.
                UNSTRING LK-S DELIMITED BY "," INTO WS-R WS-R END-UNSTRING
                GOBACK.
            """;
        Assert.Matches(SenderReread, EmitCSharp(program));
    }

    [Fact]
    public void BasedSender_RereadsBecauseItsAddressIsSetAtRunTime()
    {
        const string data = """
            01 BS   PIC X(9) BASED.
            01 R1   PIC X(3).
            """;
        string cs = EmitCSharp(Program(data, """UNSTRING BS DELIMITED BY "," INTO R1 R1 END-UNSTRING"""));
        Assert.Matches(SenderReread, cs);
    }

    [Fact]
    public void Level66AliasOfTheReceivedField_RereadsTheSender()
    {
        const string data = """
            01 REC.
               05 FA PIC X(4) VALUE "AAA,".
               05 FB PIC X(5) VALUE "BBB,C".
               66 AL RENAMES FA THRU FB.
            01 R1   PIC X(9).
            """;
        string cs = EmitCSharp(Program(data, """UNSTRING AL DELIMITED BY "," INTO FB R1 END-UNSTRING"""));
        Assert.Matches(SenderReread, cs);
    }

    [Fact]
    public void FunctionSender_IsNeverReread()
    {
        const string data = """
            01 SND  PIC X(9) VALUE "a,b,c".
            01 R1   PIC X(3).
            01 R2   PIC X(3).
            """;
        string cs = EmitCSharp(Program(data, """UNSTRING FUNCTION UPPER-CASE(SND) DELIMITED BY "," INTO SND R2 END-UNSTRING"""));
        Assert.DoesNotMatch(SenderReread, cs);
    }

    [Fact]
    public void TheEmitterAsksTheOnePredicate_AndWalksNoRecords()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "StringEmitter.cs"));
        Assert.Contains("StorageSharing.MayShare(", src);
        Assert.DoesNotContain(".Parent", src);
        Assert.DoesNotContain(".Class", src);
        // The predicate is the only definition: no other emitter spells a second "may these share" walk.
        string root = Path.GetDirectoryName(TestRepo.Src("Cobol.Net.Compiler", "Cobol.Net.Compiler.csproj"))!;
        var definers = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\bMayShare\s*\("))
            .Select(Path.GetFileName).Order().ToList();
        Assert.Equal(["StorageSharing.cs", "StringEmitter.cs"], definers);
    }
}
