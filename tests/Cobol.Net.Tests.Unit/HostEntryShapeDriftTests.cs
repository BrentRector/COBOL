// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE HOST ENTRY IS A CALLER (DESIGN-external-repository §17.3; kb/Work PB2099). A generated host entry's <c>Call</c>
/// is the .NET counterpart of a CALL statement, never a second activation boundary: its body is exactly the parts §4.3
/// lists — <c>__CobolModule.EnsureRegistered()</c> (a definition's entry), one <c>HostLanding</c> landing per formal
/// whose host value needs one (the door rules written once, no inline range test), one <c>CobolArg</c> per formal,
/// exactly one <c>ProgramRegistry.CallProgram</c>, a <c>finally</c> of <c>HostLanding</c> decodes, and the RETURNING
/// decode — with no catch clause, no program instance, no <c>ICobolProgram</c> or <c>ProgramTable</c> member and no test
/// of an exception-name; and <c>Cancel</c> is one <c>ProgramRegistry.Cancel</c>. This class reads the emitted C# of a
/// fixture covering every elementary landing row and is red when an entry grows any other part.
/// </summary>
public sealed class HostEntryShapeDriftTests : CobolNetTestBase
{
    private const string Fixture = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. H2MAIN.
               PROCEDURE DIVISION.
                   DISPLAY "MAIN"
                   GOBACK.
               END PROGRAM H2MAIN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. H2ROWS.
               DATA DIVISION.
               LINKAGE SECTION.
               01 L-DEC PIC S9(5)V99.
               01 L-INT PIC 9(4).
               01 L-WIDE PIC S9(20).
               01 L-U16 PIC 9(20) COMP-5.
               01 L-TXT PIC X(5).
               01 L-NAT PIC N(3).
               01 L-BOOL PIC 1(4).
               01 L-FLT USAGE FLOAT-LONG.
               01 L-PTR USAGE POINTER.
               01 L-INT2 PIC S9(4) COMP-5.
               01 L-OPT PIC X(2).
               01 L-RET PIC S9(3)V9.
               PROCEDURE DIVISION USING L-DEC L-INT L-WIDE L-U16 L-TXT L-NAT L-BOOL
                   L-FLT L-PTR BY VALUE L-INT2 BY REFERENCE OPTIONAL L-OPT
                   RETURNING L-RET.
                   GOBACK.
               END PROGRAM H2ROWS.
               """;

    [Fact]
    public void EveryHostEntry_IsACaller_OfTheOneBoundary()
    {
        var (ok, stdout, detail) = CompileAndRun(Fixture, dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal("MAIN", stdout);
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(TempDir, "prog.g.cs"))).GetRoot();
        var entries = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => c.Members.OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.Text == "Call")
                        && c.Modifiers.Any(SyntaxKind.StaticKeyword))
            .ToList();
        Assert.Equal(["H2MAIN", "H2ROWS"], entries.Select(e => e.Identifier.Text).Order().ToArray());

        foreach (var entry in entries)
        {
            var call = entry.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Call");
            string body = call.Body!.ToString();
            Assert.Single(call.Body!.DescendantNodes().OfType<InvocationExpressionSyntax>(),
                i => i.Expression.ToString() == "ProgramRegistry.CallProgram");
            Assert.Empty(call.Body.DescendantNodes().OfType<CatchClauseSyntax>());
            Assert.DoesNotContain("ICobolProgram", body);
            Assert.DoesNotContain("ProgramTable", body);
            Assert.DoesNotContain("_PRG_", body);   // no program class, so no instance
            // The only exception-name an entry names is the function arm's not-found condition (DESIGN-external-repository §4.3).
            Assert.All(call.Body.DescendantNodes().OfType<LiteralExpressionSyntax>()
                    .Where(l => l.Token.ValueText.StartsWith("EC-", StringComparison.Ordinal)),
                l => Assert.Equal("EC-FUNCTION-NOT-FOUND", l.Token.ValueText));
            // Every statement is one of §4.3's parts.
            foreach (var s in call.Body.Statements)
            {
                string text = s.ToString();
                bool part = text.StartsWith("__CobolModule.EnsureRegistered();", StringComparison.Ordinal)
                    || text.StartsWith("var __hc", StringComparison.Ordinal)
                    || text.StartsWith("global::System.FormatException? __hbad = null;", StringComparison.Ordinal)
                    || text.StartsWith("ProgramRegistry.CallProgram(", StringComparison.Ordinal)
                    || (s is TryStatementSyntax t && t.Catches.Count == 0 && t.Finally is not null
                        && t.Block.Statements.Count == 1 && t.Block.Statements[0].ToString().StartsWith("ProgramRegistry.CallProgram(", StringComparison.Ordinal))
                    || text.StartsWith("{ if (HostLanding.TryDecode(", StringComparison.Ordinal)
                    || System.Text.RegularExpressions.Regex.IsMatch(text, @"^\S+ __hres = default;$")
                    || text == "if (__hbad is not null) throw __hbad;"
                    || text == "return __hres;";
                Assert.True(part, $"{entry.Identifier.Text}.Call has a statement that is not one of §4.3's parts: {text}");
            }
            // A finally holds only decodes: the validity decode or the storage's own value.
            foreach (var fin in call.Body.DescendantNodes().OfType<FinallyClauseSyntax>())
                Assert.All(fin.Block.Statements, s => Assert.Matches(
                    @"^(\{ if \(HostLanding\.TryDecode\(|if \(__hc\d+ is not null\) \{ |\w+ = __hc\d+\.Value;)", s.ToString()));
            var cancel = entry.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Cancel");
            Assert.StartsWith("ProgramRegistry.Cancel(", cancel.ExpressionBody!.Expression.ToString());
        }

        // The door rules are one function: each landed formal's storage comes from a HostLanding call, never an inline test.
        var rows = entries.Single(e => e.Identifier.Text == "H2ROWS");
        var landings = rows.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
            .Where(d => d.Declaration.Variables[0].Identifier.Text.StartsWith("__hc", StringComparison.Ordinal))
            .ToDictionary(d => d.Declaration.Variables[0].Identifier.Text, d => d.ToString());
        // Formals in order: L-DEC L-INT L-WIDE L-U16 L-TXT L-NAT L-BOOL L-FLT L-PTR L-INT2 L-OPT, then RETURNING.
        foreach (int landed in new[] { 0, 1, 2, 3, 4, 5, 6, 9, 10 })
            Assert.Contains("HostLanding.Land", landings[$"__hc{landed}"]);
        foreach (int unconverted in new[] { 7, 8 })   // a floating-point carrier and a pointer cross as themselves
            Assert.DoesNotContain("HostLanding", landings[$"__hc{unconverted}"]);
        Assert.DoesNotContain(rows.DescendantNodes().OfType<BinaryExpressionSyntax>(),
            b => b.Kind() is SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression);

        // The parameter types are the host-type table's rows (DESIGN-external-repository §4.3, HostSurface.TypeOf).
        var parameters = rows.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Call")
            .ParameterList.Parameters.Select(p => $"{p.Modifiers} {p.Type}".Trim()).ToArray();
        Assert.Equal(new[]
        {
            "ref decimal", "ref long", "ref Int128", "ref UInt128", "ref string", "ref string", "ref string",
            "ref double", "ref ManagedPointer", "long", "ref string?",
        }, parameters);
        Assert.Equal("decimal", rows.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Call").ReturnType.ToString());
    }
}
