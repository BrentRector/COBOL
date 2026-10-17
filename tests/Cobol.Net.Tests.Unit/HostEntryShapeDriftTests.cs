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
               IDENTIFICATION DIVISION.
               PROGRAM-ID. H2GRP.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 TS TYPEDEF STRONG.
                  05 TS-A PIC X.
                  05 TS-P USAGE POINTER OCCURS 2.
               LINKAGE SECTION.
               01 L-G.
                  05 L-N PIC 9(3).
                  05 FILLER PIC X.
                  05 L-S OCCURS 2.
                     10 L-T PIC X(2).
                     10 L-B PIC 1 USAGE BIT.
                  05 L-D PIC X OCCURS 0 TO 3 DEPENDING ON L-N.
               01 L-P TYPE TS.
               01 L-R.
                  05 L-R1 PIC S9(3)V9 COMP-3.
               PROCEDURE DIVISION USING L-G L-P RETURNING L-R.
                   GOBACK.
               END PROGRAM H2GRP.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. H2VAR.
               DATA DIVISION.
               LINKAGE SECTION.
               01 L-V.
                  05 L-VN PIC 9(2).
                  05 L-VD PIC X DYNAMIC LENGTH LIMIT IS 8.
                  05 L-VT OCCURS DYNAMIC CAPACITY IN L-VC FROM 1.
                     10 L-VK PIC 9.
                     10 L-VE PIC X DYNAMIC LENGTH.
                  05 L-VF PIC X(2) OCCURS DYNAMIC CAPACITY IN L-VFC.
               01 L-W.
                  05 L-WD PIC X DYNAMIC LENGTH.
                  05 L-WT PIC X(2) OCCURS DYNAMIC CAPACITY IN L-WC FROM 1.
               PROCEDURE DIVISION USING L-V RETURNING L-W.
                   GOBACK.
               END PROGRAM H2VAR.
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
        Assert.Equal(["H2GRP", "H2MAIN", "H2ROWS", "H2VAR"], entries.Select(e => e.Identifier.Text).Order().ToArray());

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
                    || text.StartsWith("HostLanding.RequireDistinct(", StringComparison.Ordinal)
                    || text.StartsWith("global::System.FormatException? __hbad = null;", StringComparison.Ordinal)
                    || text.StartsWith("ProgramRegistry.CallProgram(", StringComparison.Ordinal)
                    || (s is TryStatementSyntax t && t.Catches.Count == 0 && t.Finally is not null
                        && t.Block.Statements.Count == 1 && t.Block.Statements[0].ToString().StartsWith("ProgramRegistry.CallProgram(", StringComparison.Ordinal))
                    || text.StartsWith("{ if (HostLanding.TryDecode(", StringComparison.Ordinal)
                    || System.Text.RegularExpressions.Regex.IsMatch(text, @"^\{ if \(\w+_Record\.__TryGet\(")
                    || System.Text.RegularExpressions.Regex.IsMatch(text, @"^\S+ __hres = default;$")
                    || text == "if (__hbad is not null) throw __hbad;"
                    || text == "return __hres;";
                Assert.True(part, $"{entry.Identifier.Text}.Call has a statement that is not one of §4.3's parts: {text}");
            }
            // A finally holds only decodes: the validity decode or the storage's own value.
            foreach (var fin in call.Body.DescendantNodes().OfType<FinallyClauseSyntax>())
                Assert.All(fin.Block.Statements, s => Assert.Matches(
                    @"^(\{ if \(HostLanding\.TryDecode\(|\{ if \(\w+_Record\.__TryGet\(|if \(__hc\d+ is not null\) \{ |\w+ = __hc\d+\.Value;)", s.ToString()));
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

        // A group formal or RETURNING item is its published record struct (§4.3's group rows), whose image is composed and
        // decoded by the same door rules: every member landing in __Put is a HostLanding call (or a nested record's
        // __Put), every numeric decode in __TryGet is HostLanding.TryDecode, and no record writes an inline range test
        // (its only comparisons are the loop bounds of a table's occurrences).
        var grp = entries.Single(e => e.Identifier.Text == "H2GRP");
        var grpCall = grp.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Call");
        Assert.Equal(["ref L_G_Record", "ref L_P_Record"], grpCall.ParameterList.Parameters.Select(p => $"{p.Modifiers} {p.Type}".Trim()).ToArray());
        Assert.Equal("L_R_Record", grpCall.ReturnType.ToString());
        var records = grp.DescendantNodes().OfType<RecordDeclarationSyntax>().ToList();
        Assert.Equal(["L_G_Record", "L_P_Record", "L_R_Record", "L_S_Record"], records.Select(r => r.Identifier.Text).Order().ToArray());
        // A record holding references crosses as its cell area (§4.3's last host-type row): its storage is the record's
        // __Area, its argument states that Area, and only such a record has one.
        Assert.Equal(["L_P_Record"], records.Where(r => r.Members.OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.Text == "__Area"))
            .Select(r => r.Identifier.Text).ToArray());
        Assert.Contains("Area: new CellPointer(__hc1, 0)", grpCall.Body!.ToString());
        foreach (var record in records)
        {
            var put = record.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "__Put");
            Assert.All(put.Body!.DescendantNodes().OfType<ExpressionStatementSyntax>().Where(s => s.Parent is not ForStatementSyntax),
                s => Assert.Matches(@"HostLanding\.|^\w+(\[__i\])?\.__Put\(", s.ToString()));
            Assert.All(put.Body.DescendantNodes().OfType<ForStatementSyntax>(),
                f => Assert.Matches(@"HostLanding\.|\.__Put\(", f.Statement.ToString()));
            Assert.DoesNotContain(record.DescendantNodes().OfType<BinaryExpressionSyntax>(),
                b => b.Kind() is SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression
                     && b.Parent is not ForStatementSyntax);
            var get = record.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "__TryGet");
            Assert.All(get.Body!.DescendantNodes().OfType<IfStatementSyntax>(),
                i => Assert.Matches(@"^(HostLanding\.TryDecode\(|HostLanding\.TryOccurrences\(|\w+_Record\.__TryGet\()", i.Condition.ToString()));
        }

        // A VARIABLE-LENGTH group (ISO §8.5.1.12) crosses as the CALL lane's variable-length carrier of its description: its
        // storage is the record's __Var, a ManagedPointer<CobolVarGroup> cell; each SCOPE (the record, and the element of a
        // dynamic-capacity table whose element holds components) composes and decodes a carrier, every component lands
        // through HostLanding (LandVariable for a dynamic-length item, RequireCapacity for a table's array), and a
        // variable-length record publishes no image API.
        var vary = entries.Single(e => e.Identifier.Text == "H2VAR");
        var varCall = vary.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Call");
        Assert.Equal(["ref L_V_Record"], varCall.ParameterList.Parameters.Select(p => $"{p.Modifiers} {p.Type}".Trim()).ToArray());
        Assert.Contains("var __hc0 = ManagedPointer<CobolVarGroup>.Cell(L_V.__Var(", varCall.Body!.ToString());
        Assert.Contains("var __hc1 = ManagedPointer<CobolVarGroup>.Cell(new L_W_Record().__Var(", varCall.Body.ToString());
        var varRecords = vary.DescendantNodes().OfType<RecordDeclarationSyntax>().ToList();
        Assert.Equal(["L_V_Record", "L_VT_Record", "L_W_Record"], varRecords.Select(r => r.Identifier.Text).Order().ToArray());
        foreach (var record in varRecords)
        {
            string[] methods = [.. record.Members.OfType<MethodDeclarationSyntax>().Select(m => m.Identifier.Text)];
            Assert.Contains("__Var", methods);
            Assert.Contains("__PutDyn", methods);
            Assert.Contains("__TryGetDyn", methods);
            Assert.DoesNotContain("AsImage", methods);
            Assert.DoesNotContain("FromImage", methods);
            string putDyn = record.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "__PutDyn").Body!.ToString();
            Assert.Contains("HostLanding.LandVariable(", putDyn);
            Assert.DoesNotContain(record.DescendantNodes().OfType<BinaryExpressionSyntax>(),
                b => b.Kind() is SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression
                     && b.Parent is not ForStatementSyntax);
        }
        Assert.All(varRecords.Where(r => r.Identifier.Text is "L_V_Record" or "L_W_Record"), r => Assert.Contains(
            "HostLanding.RequireCapacity(", r.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "__PutDyn").Body!.ToString()));
    }

    /// <summary>A strongly-typed group holding BOTH a dynamic-length item and a pointer, beside two units that publish.</summary>
    private const string RestrictionFixture = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. H3MAIN.
               PROCEDURE DIVISION.
                   DISPLAY "MAIN"
                   GOBACK.
               END PROGRAM H3MAIN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. H3OK.
               DATA DIVISION.
               LINKAGE SECTION.
               01 L-A PIC X(3).
               PROCEDURE DIVISION USING L-A.
                   GOBACK.
               END PROGRAM H3OK.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. H3BOTH.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 TB TYPEDEF STRONG.
                  05 TB-N PIC 9(2).
                  05 TB-D PIC X DYNAMIC LENGTH LIMIT IS 8.
                  05 TB-P USAGE POINTER.
               LINKAGE SECTION.
               01 L-B TYPE TB.
               PROCEDURE DIVISION USING L-B.
                   GOBACK.
               END PROGRAM H3BOTH.
               """;

    /// <summary>⛔ THE §14.2.3 13) RESTRICTION IS THE BOUNDARY'S (DESIGN-external-repository §4.3, DOC-A.1-141; kb/Work
    /// PB3273). A unit one of whose group formals holds both a variable-length component (§8.5.1.12.1) and a pointer or
    /// object-reference member publishes NO entry — the other units of the group still do — because no crossing form of the
    /// boundary carries that group: the activated program's own formal of that description stages the Tier-C loud at its
    /// copy-in, so an entry for it could only reach a loud. The second half is the pin: the day the CALL lane gains a form
    /// for the shape this test is red, and the restriction (<c>PublishedRecordEmitter.Renders</c>) is to be lifted
    /// together with the record's composition of that form's storage, never left standing and never lifted alone.</summary>
    [Fact]
    public void AGroupHoldingAComponentAndAReference_PublishesNoEntry_WhileTheBoundaryHasNoFormForIt()
    {
        var (ok, stdout, detail) = CompileAndRun(RestrictionFixture, dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal("MAIN", stdout);
        string generated = File.ReadAllText(Path.Combine(TempDir, "prog.g.cs"));
        var root = CSharpSyntaxTree.ParseText(generated).GetRoot();
        var entries = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => c.Members.OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.Text == "Call")
                        && c.Modifiers.Any(SyntaxKind.StaticKeyword))
            .Select(c => c.Identifier.Text).Order().ToArray();
        Assert.Equal(["H3MAIN", "H3OK"], entries);
        Assert.Contains("variable-length group image of 'L-B' with a pointer/object-class leaf", generated);
    }

    /// <summary>⛔ ONE RENDERER (DESIGN-external-repository §17.3): what a host entry's argument states for §14.9.4.4 3) d) —
    /// its storage length and its description — is rendered by the CALL lane's <c>CallEmitter.Described</c>, the renderer a
    /// CALL statement's argument is stated by, from the builders a CALL argument's facts come from
    /// (<c>CallEmitter.ArgumentLength</c>, <c>ActivationDescriptions.Of</c>). The entry composes neither fact itself, and the
    /// emitted fixture shows every described argument in that renderer's form.</summary>
    [Fact]
    public void AHostEntryArgument_IsStatedByTheCallLanesRenderer()
    {
        // The emitter's code without its comments: every token's text, string literals included.
        string src = string.Concat(CSharpSyntaxTree.ParseText(File.ReadAllText(
                CobolNet.Tests.Shared.TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "HostEntryEmitter.cs"))).GetRoot()
            .DescendantTokens().Select(t => t.Text));
        Assert.Contains("CallEmitter.Described(", src);
        Assert.Contains("CallEmitter.ArgumentLength(", src);
        Assert.Contains("ActivationDescriptions.Of(", src);
        Assert.DoesNotContain("Length:", src);
        Assert.DoesNotContain("Length =", src);
        Assert.DoesNotContain("Description:", src);
        Assert.DoesNotContain("Description =", src);

        var (ok, _, detail) = CompileAndRun(Fixture, dialectLevel: 2023);
        Assert.True(ok, detail);
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(TempDir, "prog.g.cs"))).GetRoot();
        var rows = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "H2ROWS");
        var args = rows.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            .Where(o => o.Type.ToString() == "CobolArg" && !o.ToString().Contains("ManagedPointer.Null", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(12, args.Count);   // eleven formals and the RETURNING item
        Assert.All(args, a => Assert.True(a.Parent is WithExpressionSyntax w && w.Initializer.ToString().Contains("Description =", StringComparison.Ordinal),
            $"a host-entry argument not stated by CallEmitter.Described: {a}"));
    }

    /// <summary>The §17.3 collision fixture: words a published name could collide with — a word twice in one record
    /// (F in S1 and S2, beside the record's own F), F beside F-2, words spelling the record's own members
    /// (<c>AsImage</c>, <c>FromImage</c>, <c>Equals</c>, <c>ToString</c>, <c>GetHashCode</c>), a member spelling its
    /// enclosing record type (S1-Record in S1), a program whose host class spells a formal's record type
    /// (L-G-Record beside L-G), and a formal spelling another formal's record type (L-G-2-Record beside L-G-2: the
    /// <c>Call</c> body names that record type, so the parameter may not shadow it; review w1093b R1-1).</summary>
    private const string NameFixture = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. NMMAIN.
               PROCEDURE DIVISION.
                   DISPLAY "MAIN"
                   GOBACK.
               END PROGRAM NMMAIN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. L-G-Record.
               DATA DIVISION.
               LINKAGE SECTION.
               01 L-G.
                  05 F PIC X.
                  05 F-2 PIC X.
                  05 AsImage PIC X.
                  05 FromImage PIC X.
                  05 Equals PIC X.
                  05 ToString PIC X.
                  05 S1.
                     10 F PIC X.
                     10 S1-Record PIC X.
                  05 S2.
                     10 F PIC X.
               01 L-G-2.
                  05 X PIC X.
               01 GetHashCode PIC X.
               01 L-G-2-Record PIC X.
               PROCEDURE DIVISION USING L-G L-G-2 GetHashCode L-G-2-Record.
                   GOBACK.
               END PROGRAM L-G-Record.
               """;

    /// <summary>⛔ THE PUBLISHED NAMES ARE ALLOCATED (DESIGN-external-repository §17.3 "Names"). Every name of a host class,
    /// its record structs and their members comes from <c>CsNames.Allocate</c> over its scope, so the collision fixture
    /// compiles — Roslyn would refuse a CS0102, CS0542 or CS0111 — and two compiles give the same names. Every LINKAGE root
    /// of the corpus is compiled by its own golden, whose entry classes fail that golden on the same errors.</summary>
    [Fact]
    public void ThePublishedNames_AreAllocated_SoACollisionFixtureCompiles_AndTwoCompilesAgree()
    {
        string[] Names()
        {
            var (ok, stdout, detail) = CompileAndRun(NameFixture, dialectLevel: 2023);
            Assert.True(ok, detail);
            Assert.Equal("MAIN", stdout);
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(TempDir, "prog.g.cs"))).GetRoot();
            var host = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "L_G_Record");
            return host.DescendantNodes().OfType<RecordDeclarationSyntax>().Select(r => "type " + r.Identifier.Text)
                .Concat(host.DescendantNodes().OfType<PropertyDeclarationSyntax>().Select(p => "member " + p.Identifier.Text))
                .Concat(host.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Call")
                    .ParameterList.Parameters.Select(p => "parameter " + p.Identifier.Text))
                .ToArray();
        }
        string[] first = Names();
        Assert.Equal(first, Names());
        // The allocator, not the fixture's spelling, decided each colliding name: none equals its enclosing type or a
        // record member, and no record declares one name twice.
        Assert.Contains("type L_G_Record_2", first);
        Assert.Contains("member S1_Record_2", first);
        Assert.Contains("type L_G_2_Record", first);
        Assert.Contains("parameter L_G_2_Record_2", first);
        Assert.DoesNotContain("parameter L_G_2_Record", first);
        Assert.Contains("member AsImage_2", first);
        Assert.Contains("member F_2", first);
        Assert.DoesNotContain("member AsImage", first);
        Assert.DoesNotContain("member ToString", first);
    }
}
