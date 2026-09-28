// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Binding;
using CobolNet.CodeGen.Emit;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// A floating-point argument moves a call to the binary64 lane only when the WHOLE argument run can ride it —
/// asserted over every catalogued function whose §15.x.3 argument rules MIX a numeric position with a
/// non-numeric one, not over the one function that was reported.
/// </summary>
/// <remarks>
/// <para><b>The rule.</b> ISO §15.3 types the argument positions: type 6 is "An arithmetic expression that will
/// always result in an integer value or an integer data item shall be specified" and type 10 "Numeric. An
/// arithmetic expression or a numeric data item shall be specified" — those two have a numeric value. Types
/// 1/2/9 (alphabetic / alphanumeric / national), 3 (boolean), 5 (index), 11 (object) and 13 (pointer) name
/// character, bit and reference operands, which have none. §15.4.1 grants the binary64 lane its licence —
/// "When native arithmetic is in effect and an equivalent arithmetic expression is specified, the value returned
/// is an implementor-defined approximation of the value of that expression" — and a function with a non-numeric
/// argument position has neither that expression nor a binary64 carrier for the position.</para>
///
/// <para><b>Why this test exists (kb/Work PB635).</b> <c>IntrinsicRenderer.RenderNum</c>'s float dispatch asked
/// "is ANY argument floating" and then rendered EVERY argument through one binary64 conversion. For
/// FIND-STRING — §15.37.3 r1 "Argument-1 shall be a data item or literal of class alphabetic, alphanumeric, or
/// national", r3 "argument-3 shall be an integer data item or integer literal" — a float argument-3 therefore
/// moved the whole call: the two string operands became <c>(double)CobolNum.FromAlphanumeric(…)</c>, the
/// LAST/ANYCASE phrases were dropped, and the emitted call named a <c>FindStringReal</c> body that does not
/// exist. Measured before the fix, under <c>--permissive</c> (the lane PB248's §15.3 type-6 screen leaves the
/// float form reachable in): <c>error CS0117: 'CobolIntrinsics' does not contain a definition for
/// 'FindStringReal'</c> — and the same for <c>OrdReal</c>, <c>TestNumvalReal</c> and
/// <c>IntegerOfBooleanReal</c>. A Roslyn error on the generated C# is an internal failure escaping at the wrong
/// stage, which is what makes this a crash class rather than a wrong answer.</para>
///
/// <para><b>What makes it a drift test rather than one more golden.</b> The POPULATION is derived: every
/// <c>IntrinsicArgumentRules.Verified</c> row whose result type reaches the numeric channel and whose argument
/// run is not all-numeric. Add such a function — or change an existing row's schema so a string position appears
/// beside a numeric one — and this fails until a fixture covers it. That is the difference between "the mixed
/// functions we thought of are safe" and "every one is".</para>
///
/// <para><b>The probe is the GENERATED C#</b>, because the property is about which BODY is called: a call that
/// stayed in its own lane names the catalog row's own runtime method, never that name with a <c>Real</c> suffix
/// (<c>IntrinsicRenderer.RealMethod</c>'s transform). A compile that fails in the BACKEND is a failure outright —
/// that is the defect's own signature.</para>
/// </remarks>
public sealed class IntrinsicFloatLaneArgumentRunDriftTests : CobolNetTestBase
{
    /// <summary>The catalog rows, read from the source so the population cannot drift away from the table:
    /// name → (result type, runtime method, max args).</summary>
    private static readonly Regex Row = new(
        "Add\\(new\\(\"(?<n>[A-Z0-9-]+)\",\\s*IntrinsicType\\.(?<t>\\w+),\\s*IntrinsicArity\\.\\w+,\\s*[-\\w]+,"
        + "\\s*(?<max>[-\\w.]+),\\s*\"[^\"]*\",\\s*\"(?<rm>\\w*)\"",
        RegexOptions.Compiled);

    /// <summary>One legal reference per mixed-class row, with a <c>FLOAT-LONG</c> operand at a position the
    /// function's own §15.x.3 rules describe — an integer position where the row has one (FIND-STRING's
    /// argument-3, §15.37.3 r3), else the string position itself, which <c>--permissive</c> reaches through the
    /// DA6 coercion. The KEY is the catalog's function name — that is what ties this table to the sweep.</summary>
    private static readonly Dictionary<string, string> Fixtures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FIND-STRING"] = "FUNCTION FIND-STRING(WS-HAY WS-NDL START AFTER FL)",
        ["ORD"] = "FUNCTION ORD(FL)",
        ["INTEGER-OF-BOOLEAN"] = "FUNCTION INTEGER-OF-BOOLEAN(FL)",
        ["NUMVAL"] = "FUNCTION NUMVAL(FL)",
        ["NUMVAL-C"] = "FUNCTION NUMVAL-C(FL)",
        ["NUMVAL-F"] = "FUNCTION NUMVAL-F(FL)",
        ["TEST-NUMVAL"] = "FUNCTION TEST-NUMVAL(FL)",
        ["TEST-NUMVAL-C"] = "FUNCTION TEST-NUMVAL-C(FL)",
        ["TEST-NUMVAL-F"] = "FUNCTION TEST-NUMVAL-F(FL)",
        ["INTEGER-OF-FORMATTED-DATE"] = "FUNCTION INTEGER-OF-FORMATTED-DATE(\"YYYYMMDD\" FL)",
        ["SECONDS-FROM-FORMATTED-TIME"] = "FUNCTION SECONDS-FROM-FORMATTED-TIME(\"hhmmss\" FL)",
        ["TEST-FORMATTED-DATETIME"] = "FUNCTION TEST-FORMATTED-DATETIME(\"YYYYMMDD\" FL)",
    };

    /// <summary>An argument count past every DECLARED position of every schema, so the variadic TAIL is
    /// exercised too. <c>ArgumentRunIsAllNumeric</c> asks the schema per position, so any count at or beyond the
    /// widest schema gives the function-level answer; the widest today declares four.</summary>
    internal const int PastEveryDeclaredPosition = 8;

    /// <summary>Every catalog row that (a) returns a NUMERIC or INTEGER value, so <c>RenderNum</c>'s float
    /// dispatch is the channel it reaches, and (b) has at least one argument position §15.3 does not type as
    /// numeric — read through the same <c>ArgumentRunIsAllNumeric</c> derivation the renderer dispatches on, so
    /// the test cannot disagree with the compiler about who is in the set.</summary>
    private static List<(string Name, string Rm)> MixedClassNumericRows()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));
        var all = Row.Matches(src).ToList();
        Assert.True(all.Count >= 79,
            $"only {all.Count} catalog rows parsed — the Add(new(...)) shape changed; fix the regex, do not lower the floor.");
        return [.. all
            .Where(m => m.Groups["t"].Value is "Numeric" or "Integer")
            .Where(m => m.Groups["max"].Value != "0")
            .Where(m => m.Groups["rm"].Value.Length > 0)
            // argCount: every DECLARED position plus one tail position, so a variadic tail is exercised too.
            .Where(m => !IntrinsicArgumentRules.ArgumentRunIsAllNumeric(m.Groups["n"].Value, PastEveryDeclaredPosition))
            .Select(m => (m.Groups["n"].Value, m.Groups["rm"].Value))];
    }

    [Fact]
    public void EveryMixedClassNumericRow_HasAFixture()
    {
        var rows = MixedClassNumericRows();
        Assert.True(rows.Count >= 12,
            $"only {rows.Count} mixed-class numeric-result rows found — the §15.3 argument-type sweep shrank "
            + "unexpectedly; check IntrinsicArgumentRules.Verified before lowering this.");
        var missing = rows.Select(r => r.Name).Where(n => !Fixtures.ContainsKey(n)).ToList();
        var stale = Fixtures.Keys.Where(n => !rows.Any(r => string.Equals(r.Name, n, StringComparison.OrdinalIgnoreCase))).ToList();
        Assert.True(missing.Count == 0,
            "a catalogued function now MIXES a non-numeric §15.3 argument position with the numeric channel and "
            + "has no float-argument fixture — add one, or the PB635 shape (a float argument moving the whole "
            + "call to the binary64 lane, string operands and all) can come back for it unseen:\n  "
            + string.Join("\n  ", missing));
        Assert.True(stale.Count == 0,
            "a fixture names a function whose argument run is now all-numeric — remove it or fix the schema:\n  "
            + string.Join("\n  ", stale));
    }

    [Fact]
    public void AFloatArgument_NeverMovesAMixedClassCallToTheBinary64Lane()
    {
        var offenders = new List<string>();
        int i = 0;
        foreach (var (name, rm) in MixedClassNumericRows().OrderBy(r => r.Name, StringComparer.Ordinal))
        {
            string programId = "PB635D" + (++i).ToString("00");
            var result = CompileWithAFloatArgument(programId, Fixtures[name]);

            // ⛔ THE DEFECT'S OWN SIGNATURE. A BackendError is Roslyn refusing the generated C# — an internal
            // failure escaping at the wrong stage, on source the front end accepted. It is never an acceptable
            // outcome, whatever the verdict on the source itself is.
            if (result.Status == CompilerDriver.Outcome.BackendError)
            {
                offenders.Add($"{name}: {Fixtures[name]} reached the BACKEND and failed there — "
                    + string.Join(" | ", result.Errors));
                continue;
            }
            if (result.GeneratedCsPath is not { } csPath || !File.Exists(csPath)) continue;
            string generated = File.ReadAllText(csPath);
            string realName = IntrinsicRenderer.RealMethod(rm);
            if (generated.Contains(realName + "(", StringComparison.Ordinal))
                offenders.Add($"{name}: the emitted C# calls {realName} — a float argument moved the whole call "
                    + "to the binary64 lane, where every argument including the non-numeric ones is converted to "
                    + "double and the function's phrases are dropped");
        }
        Assert.True(offenders.Count == 0,
            "ISO §15.3 types each argument position, and only types 6 and 10 have a numeric value; §15.4.1's "
            + "binary64 licence covers a function's equivalent arithmetic expression, not a call whose operands "
            + "are character strings. These moved to the binary64 lane on the strength of ONE floating-point "
            + "argument (kb/Work PB635):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// ⛔ THE OTHER ARM (feedback_two_arm_dispatch). <c>RenderNum</c> has TWO routes into
    /// <see cref="IntrinsicRenderer.RenderFloat"/>: the ARGUMENT one this file guards, and the FAMILY one —
    /// <c>if (sig.Float) return RenderFloat(ic, sig.RuntimeMethod)</c> — which runs FIRST and consults no
    /// argument at all. A <c>Float: true</c> row with a non-numeric §15.3 position would ride that arm straight
    /// into the same binary64 conversion of every argument, and would NOT show up as a missing <c>…Real</c> body
    /// because the exact name exists: the failure would be a wrong ANSWER, not a crash, which is strictly worse.
    /// Every <c>Float: true</c> row is all-numeric today (the §15.4.1 float family is the trig / log / financial
    /// set), and this says so out loud rather than leaving it true by luck.
    /// </summary>
    [Fact]
    public void NoFloatFamilyRow_MixesANonNumericArgumentPosition()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));
        var floatRow = new Regex(
            "Add\\(new\\(\"(?<n>[A-Z0-9-]+)\",\\s*IntrinsicType\\.\\w+,\\s*IntrinsicArity\\.\\w+,\\s*[-\\w]+,"
            + "\\s*[-\\w.]+,\\s*\"[^\"]*\",\\s*\"\\w*\",\\s*IntrinsicBind\\.\\w+,\\s*(?<float>true|false),");
        var all = floatRow.Matches(src).ToList();
        Assert.True(all.Count >= 79,
            $"only {all.Count} catalog rows parsed — the Add(new(...)) shape changed; fix the regex, do not lower the floor.");

        var mixed = all
            .Where(m => m.Groups["float"].Value == "true")
            .Select(m => m.Groups["n"].Value)
            .Where(n => !IntrinsicArgumentRules.ArgumentRunIsAllNumeric(n, PastEveryDeclaredPosition))
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.True(mixed.Count == 0,
            "a Float: true catalog row now declares a non-numeric §15.3 argument position, and RenderNum's "
            + "`if (sig.Float)` arm reaches RenderFloat WITHOUT asking about the argument run — every argument, "
            + "the character ones included, would be converted to double and the answer would simply be wrong "
            + "(kb/Work PB635). Either the row is not a float-family function or that arm needs the same "
            + $"precondition:\n  {string.Join("\n  ", mixed)}");
    }

    /// <summary>
    /// The <c>Factorial</c> exemption is a CODOMAIN exemption, and it is declared once rather than spelled inline
    /// in the dispatch (kb/Work PB635): every member of <see cref="IntrinsicRenderer.FloatLaneExempt"/> must be a
    /// function whose argument run IS all-numeric — otherwise it is in the set for the wrong reason and
    /// <see cref="IntrinsicArgumentRules.ArgumentRunIsAllNumeric"/> already covered it.
    /// </summary>
    [Fact]
    public void EveryFloatLaneExemption_IsACodomainExemption()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));
        // A runtime method can name more than one catalog row (the LOCALE family shares bodies), so this is a
        // LOOKUP: every row the method serves has to satisfy the exemption's premise, not just the first one.
        var byMethod = Row.Matches(src)
            .Where(m => m.Groups["rm"].Value.Length > 0)
            .ToLookup(m => m.Groups["rm"].Value, m => m.Groups["n"].Value, StringComparer.Ordinal);

        Assert.NotEmpty(IntrinsicRenderer.FloatLaneExempt);
        foreach (string rm in IntrinsicRenderer.FloatLaneExempt)
        {
            var fns = byMethod[rm].ToList();
            Assert.True(fns.Count > 0,
                $"IntrinsicRenderer.FloatLaneExempt names '{rm}', which is not any catalog row's runtime method — "
                + "an exemption nothing can reach is an exemption nothing can contradict.");
            foreach (string fn in fns)
                Assert.True(IntrinsicArgumentRules.ArgumentRunIsAllNumeric(fn, PastEveryDeclaredPosition),
                    $"FUNCTION {fn} is exempted from the float lane by name, but its argument run is not "
                    + "all-numeric — ArgumentRunIsAllNumeric already excludes it, so the by-name entry is a "
                    + "second copy of one rule (kb/Work PB635). Remove it.");
        }
    }

    /// <summary>Compile one <c>--permissive</c> program that references <paramref name="reference"/> in the
    /// numeric channel with a <c>FLOAT-LONG</c> operand. <c>--permissive</c> is deliberate: PB248's §15.3 type-6
    /// screen REJECTS a floating-point item at an integer position under strict conformance (COBOLNET1627), so
    /// strict alone cannot reach the renderer at all and a strict-only probe would be green for the wrong
    /// reason.</summary>
    private CompilerDriver.Result CompileWithAFloatArgument(string programId, string reference)
    {
        string source = $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {programId}.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 WS-HAY PIC X(20) VALUE "ABCABCABC".
                   01 WS-NDL PIC X(3)  VALUE "ABC".
                   01 FL USAGE FLOAT-LONG.
                   01 R PIC S9(9)V9(4).
                   PROCEDURE DIVISION.
                   MAIN.
                       COMPUTE FL = 2.0E0
                       COMPUTE R = {reference}
                       STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, programId + ".cob");
        File.WriteAllText(srcPath, source);
        return CompilerDriver.Compile(new CompilerDriver.Options(srcPath, DialectLevel: 2023, Permissive: true, SourceFormat: InitialReferenceFormat.Auto));
    }
}
