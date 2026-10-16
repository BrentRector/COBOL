// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE HOST TYPE OF A DESCRIPTION HOLDS EVERY VALUE ITS STORAGE CAN HOLD (DESIGN-external-repository §4.3 "The host
/// type of a description"; kb/Work PB2099). <see cref="HostSurface.TypeOf"/> is the one function the host entry, its
/// published record structs and the repository record's decoder read, so this pins each row of the table over BOUND
/// descriptions (the binder's own PICTURE and USAGE analysis, never a hand-built <see cref="PicInfo"/>): a fixed-point
/// item by its capacity discipline — a PICTURE's digits for DISPLAY, a binary item's whole container when its
/// truncation is by capacity (ISO §13.18.60.4 12); kb/Work R10) — a floating-point item by its carrier, the character
/// classes as <see cref="string"/>, the pointer and object classes as their runtime carriers, and a group as its
/// published record struct. The expected types are derived from each storage's range: a signed 8-byte container's
/// [-2^63, 2^63) is <c>long</c>'s, an unsigned one's [0, 2^64) needs <c>Int128</c>, an unsigned 16-byte one's [0, 2^128)
/// <c>UInt128</c>; 28 digits is <see cref="decimal"/>'s whole-digit reach.
/// </summary>
public sealed class HostSurfaceTests
{
    private static DataBinder Bind(string linkage)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. HST{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}.
            DATA DIVISION.
            LINKAGE SECTION.
            {linkage}
            PROCEDURE DIVISION.
            MAIN-PARA.
                GOBACK.
            """;
        string path = Path.Combine(Path.GetTempPath(), "cn_hst_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var bound = new BinderDriver().Bind(tree!, new EditionContext(2023));
            return bound.Units[0].Data;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    [Fact]
    public void EachDescription_TakesItsRow()
    {
        var d = Bind("""
            01 AMOUNT        PIC S9(7)V99.
            01 COUNTER       PIC 9(4).
            01 EIGHTEEN      PIC S9(18).
            01 NINETEEN      PIC S9(19).
            01 THIRTY-ONE    PIC S9(31).
            01 THOUSANDS     PIC 9(3)PPP.
            01 WIDE-P        PIC 9(20)P(11).
            01 TINY          PIC VPPP99.
            01 BIG-SCALED    PIC S9(20)V9(10).
            01 DEC28         PIC S9(18)V9(10).
            01 BSHORT        BINARY-SHORT SIGNED.
            01 BDOUBLE       BINARY-DOUBLE SIGNED.
            01 BDOUBLE-U     BINARY-DOUBLE UNSIGNED.
            01 C5-SCALED     PIC S9(9)V99 COMP-5.
            01 C5-WIDE       PIC S9(20) COMP-5.
            01 C5-WIDE-U     PIC 9(20) COMP-5.
            01 C5-WIDE-SCALED PIC S9(18)V99 COMP-5.
            01 C5-WIDE-U-SCALED PIC 9(18)V99 COMP-5.
            01 SINGLE        COMP-1.
            01 DOUBLE        COMP-2.
            01 TEXT          PIC X(10).
            01 NAT           PIC N(4).
            01 BOOL          PIC 1(8).
            01 EDITED        PIC ZZ9.99.
            01 IDX           USAGE INDEX.
            01 PTR           USAGE POINTER.
            01 PPTR          USAGE PROGRAM-POINTER.
            01 FPTR          USAGE FUNCTION-POINTER.
            01 OBJ           USAGE OBJECT REFERENCE.
            01 REC.
               05 REC-ID     PIC 9(4).
            """);
        HostType Of(string name) => HostSurface.TypeOf(d.ByName[name][0]);
        void Row(string name, HostTypeKind kind, string? cs) => Assert.Equal(new HostType(kind, cs), Of(name));

        Row("AMOUNT", HostTypeKind.Decimal, "decimal");
        Row("COUNTER", HostTypeKind.Integer, "long");
        Row("EIGHTEEN", HostTypeKind.Integer, "long");
        Row("NINETEEN", HostTypeKind.Integer, "Int128");
        Row("THIRTY-ONE", HostTypeKind.Integer, "Int128");
        Row("THOUSANDS", HostTypeKind.Integer, "long");        // up to 999000
        Row("WIDE-P", HostTypeKind.Integer, "Int128");         // 31 digit positions, 11 of them P
        Row("TINY", HostTypeKind.Decimal, "decimal");          // scale 5
        Row("BIG-SCALED", HostTypeKind.Dec, "CobolDec");       // 30 digits: past decimal
        Row("DEC28", HostTypeKind.Decimal, "decimal");         // 28 digits: decimal's reach
        Row("BSHORT", HostTypeKind.Integer, "long");
        Row("BDOUBLE", HostTypeKind.Integer, "long");          // [-2^63, 2^63)
        Row("BDOUBLE-U", HostTypeKind.Integer, "Int128");      // [0, 2^64)
        Row("C5-SCALED", HostTypeKind.Decimal, "decimal");     // an 8-byte container: 19 digits
        Row("C5-WIDE", HostTypeKind.Integer, "Int128");        // a 16-byte container: [-2^127, 2^127)
        Row("C5-WIDE-U", HostTypeKind.Integer, "UInt128");     // [0, 2^128)
        Row("C5-WIDE-SCALED", HostTypeKind.Dec, "CobolDec");   // 39 digits at scale 2
        Row("C5-WIDE-U-SCALED", HostTypeKind.Dec, "CobolDec"); // the stated GR13 restriction (HostLanding.TryDec)
        Row("SINGLE", HostTypeKind.Float, "float");
        Row("DOUBLE", HostTypeKind.Float, "double");
        Row("TEXT", HostTypeKind.Text, "string");
        Row("NAT", HostTypeKind.Text, "string");
        Row("BOOL", HostTypeKind.Text, "string");
        Row("EDITED", HostTypeKind.Text, "string");
        Row("IDX", HostTypeKind.Index, "long");
        Row("PTR", HostTypeKind.Pointer, "ManagedPointer");
        Row("PPTR", HostTypeKind.Pointer, "ProgramPointer");
        Row("FPTR", HostTypeKind.Pointer, "FunctionPointer");
        Row("OBJ", HostTypeKind.ObjectReference, "CobolObject?");
        Row("REC", HostTypeKind.Group, null);
        Row("REC-ID", HostTypeKind.Integer, "long");
    }
}
