// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A UNIVERSAL INVOKE CROSSES EVERY CARRIED SHAPE EXACTLY AS THE TYPED INVOKE DOES (kb/Work PB1781).
///
/// <para>ISO §14.9.23.3 SR6: through a universal object reference "neither the BY CONTENT nor the BY VALUE phrase shall
/// be specified and the BY REFERENCE phrase, if not specified explicitly, is assumed implicitly", and §14.8.2.2 / §14.8.3.2
/// pair the formal with the argument by description, so the same argument that crosses a typed receiver crosses a universal
/// one with the same copy-in and the same copy-out. The universal lane boxes every argument by its own descriptor
/// (<c>OoEmitter.OoUnivCallerRead</c>) and copies the box back (<c>OoUnivCallerWrite</c>); the two are one pair of
/// arms per carried shape, and a shape given one half without the other is not a crossing. A fixed GROUP had the write
/// half (a string) and not the read half (it boxed the record STRUCT), so every group argument was a Roslyn CS0029 on
/// legal source.</para>
///
/// <para>The net is the TYPED-vs-UNIVERSAL pair: the same method, the same arguments, the receiver declared
/// <c>USAGE OBJECT REFERENCE C</c> and <c>USAGE OBJECT REFERENCE</c>, must print the same text — and the text is pinned
/// to the value the rules derive (each row's <c>expected</c>), so two wrong lanes cannot agree. A new carried shape is
/// one row.</para>
/// </summary>
public sealed class UniversalCrossingShapeDriftTests
{
    [Theory]
    // Every row names its edition: OO, strongly-typed and GROUP-USAGE items are COBOL-2002 introductions, DYNAMIC LENGTH
    // is COBOL-2014; the crossing rules are edition-invariant.
    // The note's own repro: an alphanumeric group of a character and a zoned numeric member (§14.8.2.2 rule 1 — same size).
    [InlineData("UCS01", 2002, "01 G. 05 G1 PIC X(2) VALUE \"AB\". 05 G2 PIC 9(2) VALUE 12.", "", "USING G",
        "01 LG. 05 L1 PIC X(2). 05 L2 PIC 9(2).", "USING LG",
        "DISPLAY \"in=\" LG MOVE \"ZY\" TO L1 ADD 1 TO L2", "DISPLAY \"out=\" G", "in=AB12\nout=ZY13")]
    // A NATIONAL group is the elementary national item of its as-if PICTURE N(m) at the boundary (§14.8.2.1 NOTE).
    [InlineData("UCS02", 2002, "01 G GROUP-USAGE NATIONAL. 05 G1 PIC N(3) VALUE N\"ABC\".", "", "USING G",
        "01 LG GROUP-USAGE NATIONAL. 05 L1 PIC N(3).", "USING LG",
        "DISPLAY \"in=\" LG MOVE N\"XYZ\" TO LG", "DISPLAY \"out=\" G", "in=ABC\nout=XYZ")]
    // A BIT group is the elementary boolean item of PICTURE 1(m): m boolean positions.
    [InlineData("UCS03", 2002, "01 G GROUP-USAGE BIT. 05 G1 PIC 1(4) USAGE BIT VALUE B\"1010\".", "", "USING G",
        "01 LG GROUP-USAGE BIT. 05 L1 PIC 1(4) USAGE BIT.", "USING LG",
        "DISPLAY \"in=\" LG MOVE B\"0101\" TO LG", "DISPLAY \"out=\" G", "in=1010\nout=0101")]
    // An elementary alphanumeric item — the shape that always crossed (the control arm).
    [InlineData("UCS04", 2002, "01 G PIC X(4) VALUE \"WXYZ\".", "", "USING G",
        "01 LG PIC X(4).", "USING LG",
        "DISPLAY \"in=\" LG MOVE \"ABCD\" TO LG", "DISPLAY \"out=\" G", "in=WXYZ\nout=ABCD")]
    // A packed member inside a group: the group's IMAGE carries the member's bytes, so the copy-out must restore them.
    [InlineData("UCS05", 2002, "01 G. 05 G1 PIC X(1) VALUE \"P\". 05 G2 PIC S9(3) COMP-3 VALUE 123.", "", "USING G",
        "01 LG. 05 L1 PIC X(1). 05 L2 PIC S9(3) COMP-3.", "USING LG",
        "DISPLAY \"in=\" L1 \"/\" L2 ADD 1 TO L2", "DISPLAY \"out=\" G1 \"/\" G2", "in=P/123\nout=P/124")]
    // A group ENTRY of a table as the argument (a subscripted group place).
    [InlineData("UCS06", 2002, "01 T. 05 E OCCURS 2. 10 E1 PIC X(2).", "MOVE \"AB\" TO E1 (1) MOVE \"CD\" TO E1 (2)", "USING E (2)",
        "01 LG. 05 L1 PIC X(2).", "USING LG",
        "DISPLAY \"in=\" L1 MOVE \"ZZ\" TO L1", "DISPLAY \"out=\" T", "in=CD\nout=ABZZ")]
    // A group delivered through RETURNING (§14.9.23.4 GR8): the callee's returning item lands in the caller's group.
    [InlineData("UCS07", 2002, "01 G. 05 G1 PIC X(2) VALUE \"..\". 05 G2 PIC 9(2) VALUE 0.", "", "RETURNING G",
        "01 LR. 05 R1 PIC X(2). 05 R2 PIC 9(2).", "RETURNING LR",
        "MOVE \"RR\" TO R1 MOVE 42 TO R2", "DISPLAY \"out=\" G", "out=RR42")]
    // A group holding a fixed OCCURS table: the box is the whole image, every occurrence.
    [InlineData("UCS08", 2002, "01 G. 05 T PIC X(2) OCCURS 3.", "MOVE \"AA\" TO T (1) MOVE \"BB\" TO T (2) MOVE \"CC\" TO T (3)", "USING G",
        "01 LG. 05 LT PIC X(2) OCCURS 3.", "USING LG",
        "DISPLAY \"in=\" LG MOVE \"ZZ\" TO LT (2)", "DISPLAY \"out=\" G", "in=AABBCC\nout=AAZZCC")]
    // A VARIABLE-LENGTH group (a dynamic-length member, §8.5.1.12) — the V: carrier, which already had both arms.
    [InlineData("UCS09", 2014, "01 G. 05 G1 PIC X(2) VALUE \"AB\". 05 G2 PIC X DYNAMIC LENGTH.", "MOVE \"cd\" TO G2", "USING G",
        "01 LG. 05 L1 PIC X(2). 05 L2 PIC X DYNAMIC LENGTH.", "USING LG",
        "DISPLAY \"in=\" L1 L2 MOVE \"ZY\" TO L1 MOVE \"efg\" TO L2", "DISPLAY \"out=\" G1 G2", "in=ABcd\nout=ZYefg")]
    public void UniversalInvoke_CrossesTheShapeAsTheTypedInvokeDoes(string pid, int edition, string callerData, string callerPrepare,
        string invokeTail, string calleeData, string procedureHeader, string calleeStatements, string callerShow,
        string expected)
    {
        var compiler = new CobolNetCompiler(edition);
        string typed = Run(compiler, pid + "T", true, callerData, callerPrepare, invokeTail, calleeData, procedureHeader,
            calleeStatements, callerShow);
        string universal = Run(compiler, pid + "U", false, callerData, callerPrepare, invokeTail, calleeData,
            procedureHeader, calleeStatements, callerShow);
        Assert.Equal(Normalize(expected), typed);
        Assert.Equal(typed, universal);
    }

    private static string Run(CobolNetCompiler compiler, string pid, bool typedReceiver, string callerData,
        string callerPrepare, string invokeTail, string calleeData, string procedureHeader, string calleeStatements,
        string callerShow)
    {
        string cls = pid + "C";
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS {cls}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 O USAGE OBJECT REFERENCE{(typedReceiver ? " " + cls : "")}.
            {callerData}
            PROCEDURE DIVISION.
            MAIN.
                INVOKE {cls} "NEW" RETURNING O
                {callerPrepare}
                INVOKE O "M" {invokeTail}
                {callerShow}
                STOP RUN.
            END PROGRAM {pid}.

            IDENTIFICATION DIVISION.
            CLASS-ID. {cls} INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE.
            IDENTIFICATION DIVISION.
            OBJECT.
            PROCEDURE DIVISION.
            METHOD-ID. M.
            DATA DIVISION.
            LINKAGE SECTION.
            {calleeData}
            PROCEDURE DIVISION {procedureHeader}.
                {calleeStatements}.
            END METHOD M.
            END OBJECT.
            END CLASS {cls}.
            """;
        var (ok, output, detail) = compiler.CompileAndRun(source);
        Assert.True(ok, $"{pid}: {detail}");
        return Normalize(output);
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd('\n');
}
