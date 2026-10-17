// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ARGUMENT-1 VALUE DOMAINS OF THE binary64 FAMILY, HELD TO THE STANDARD AND TO THE EXACT OPERAND (kb/Work
/// PB952). ISO §15.3 rule 14 sets EC-ARGUMENT-FUNCTION for "an incorrect value for that argument", and the seven
/// §15.x.3 rules that state such a value domain for a <c>Float: true</c> catalog row are ONE catalog column
/// (<see cref="IntrinsicSig.Domain"/>) screened by ONE runtime seam on the argument's own carrier BEFORE the
/// binary64 conversion — because a correctly-rounded conversion moves a value across the bound (ASIN(1 + 10^-18)
/// answered π/2 under armed checking).
/// </summary>
/// <remarks>
/// <para>The drift half re-reads every Float row's §15.x.3 argument rules from <c>specs/ISO_COBOL.md</c>: a row
/// whose rules say "The value of argument-1 shall be …" MUST carry the domain that phrase names and cite that
/// rule's number, and a row with no such rule must carry none — so a new binary64 row with a value rule cannot be
/// added without the screen, and a screen cannot outlive its rule.</para>
/// <para>The runtime half pins each carrier AT the bound: the closest argument outside is raised, the bound itself
/// (or the nearest legal value inside, where binary64 rounds it onto an illegal one) is admitted.</para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class IntrinsicArgumentDomainDriftTests
{
    private static readonly Regex Row = new(
        "Add\\(new\\(\"(?<n>[A-Z0-9-]+)\",\\s*IntrinsicType\\.\\w+,\\s*IntrinsicArity\\.\\w+,\\s*[-\\w]+,\\s*[-\\w]+,\\s*\"[^\"]*\",\\s*\"\\w+\",\\s*IntrinsicBind\\.\\w+,\\s*true",
        RegexOptions.Compiled);

    /// <summary>The §15.x.3 phrase → domain the rule states. The minus sign is matched loosely: the transcription
    /// renders it as a bold en-dash (or a replacement character) inside the rule text.</summary>
    private static IntrinsicDomain? DomainOf(string rule) =>
        Regex.IsMatch(rule, @"greater than or equal to \W*1\W* and less than or equal to \+1") ? IntrinsicDomain.ClosedUnit
        : Regex.IsMatch(rule, @"shall be zero or positive|greater than or equal to zero") ? IntrinsicDomain.NonNegative
        : Regex.IsMatch(rule, @"shall be greater than zero") ? IntrinsicDomain.Positive
        : Regex.IsMatch(rule, @"shall be greater than \W*1\W*\.") ? IntrinsicDomain.AboveMinusOne
        : null;

    [Fact]
    public void EveryBinary64RowsArgumentDomain_IsExactlyItsSection15ValueRule()
    {
        string spec = File.ReadAllText(TestRepo.Specs("ISO_COBOL.md"));
        var sections = SpecClauseText.FunctionSections(spec)
            .ToDictionary(f => f.Name, f => f.Section, StringComparer.OrdinalIgnoreCase);
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));
        var rows = Row.Matches(src).Select(m => m.Groups["n"].Value).ToList();
        Assert.True(rows.Count >= 18, $"only {rows.Count} Float catalog rows parsed — the Add(new(...)) shape changed; fix the regex.");
        var problems = new List<string>();
        int withRule = 0;
        foreach (string name in rows)
        {
            Assert.True(IntrinsicCatalog.TryGet(name, out var sig), $"catalog row {name} not resolvable");
            Assert.True(sections.TryGetValue(name, out string? sec), $"no §15 section for {name}");
            var rules = Regex.Match(spec, @"\n#### " + Regex.Escape(sec!) + @"\.3 (.*?)\n#### " + Regex.Escape(sec!) + @"\.4 ", RegexOptions.Singleline);
            // E and PI take no argument, so their §15.x has no .3 subclause at all.
            var valueRule = Regex.Match(rules.Success ? rules.Groups[1].Value : "",@"\n(\d+)\\?\) The value of argument-1 shall be ([^\n]*)");
            if (!valueRule.Success)
            {
                if (sig.Domain is not IntrinsicDomain.None)
                    problems.Add($"{name}: carries Domain {sig.Domain} but §{sec}.3 states no argument-1 value rule");
                continue;
            }
            withRule++;
            var expected = DomainOf(valueRule.Value);
            string cite = $"§{sec}.3 rule {valueRule.Groups[1].Value}";
            if (expected is null)
                problems.Add($"{name}: §{sec}.3 rule {valueRule.Groups[1].Value} states a value domain this test cannot classify — add its IntrinsicDomain: \"{valueRule.Groups[2].Value}\"");
            else if (sig.Domain != expected)
                problems.Add($"{name}: Domain {sig.Domain}, but {cite} states {expected}");
            else if (sig.DomainRule != cite)
                problems.Add($"{name}: DomainRule \"{sig.DomainRule}\", but the rule is {cite}");
        }
        Assert.True(withRule >= 7, $"only {withRule} Float rows carry an argument-1 value rule — the §15.x.3 shape changed; fix the scanner, do not lower the floor.");
        Assert.True(problems.Count == 0, "IntrinsicSig.Domain disagrees with the standard:\n  " + string.Join("\n  ", problems));
    }

    [Fact]
    public void EveryCatalogDomain_NamesARuntimeScreen()
    {
        // RuntimeApi.DomainArg spells the runtime enum member by the catalog member's NAME.
        var runtime = Enum.GetNames<CobolIntrinsics.ArgumentDomain>().ToHashSet();
        foreach (var d in Enum.GetValues<IntrinsicDomain>().Where(d => d is not IntrinsicDomain.None))
            Assert.Contains(d.ToString(), runtime);
    }

    private static void UnderChecking(bool on, Action body)
    {
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = on;
        try { body(); }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    private static readonly Int128 Ten30 = Int128.Parse("1000000000000000000000000000000");   // 10^30

    [Fact]
    public void TheScaledScreen_DecidesOnTheExactValue_NotItsDouble()
    {
        const CobolIntrinsics.ArgumentDomain Unit = CobolIntrinsics.ArgumentDomain.ClosedUnit;
        UnderChecking(true, () =>
        {
            // 1 + 10^-30 and -(1 + 10^-30) at scale 30 — both round to ±1.0 in binary64.
            Assert.Equal(1.0, CobolFloat.ScaledToDouble(Ten30 + 1, 30));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainScaled(Ten30 + 1, 30, Unit, "ASIN", "§15.10.3 rule 2"));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainScaled(-(Ten30 + 1), 30, Unit, "ACOS", "§15.8.3 rule 2"));
            Assert.Equal(1.0, CobolIntrinsics.DomainScaled(Ten30, 30, Unit, "ASIN", "§15.10.3 rule 2"));
            Assert.Equal(-1.0, CobolIntrinsics.DomainScaled(-Ten30, 30, Unit, "ASIN", "§15.10.3 rule 2"));
            // -0.999…9 (30 nines) is legal for PRESENT-VALUE and rounds to the ILLEGAL -1.0.
            Assert.Equal(-1.0, CobolIntrinsics.DomainScaled(-(Ten30 - 1), 30, CobolIntrinsics.ArgumentDomain.AboveMinusOne, "PRESENT-VALUE", "§15.74.3 rule 2"));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainScaled(-Ten30, 30, CobolIntrinsics.ArgumentDomain.AboveMinusOne, "PRESENT-VALUE", "§15.74.3 rule 2"));
            // zero: admitted by NonNegative, refused by Positive; a trailing-P (negative) scale is a multiple of ten.
            Assert.Equal(0.0, CobolIntrinsics.DomainScaled(0, 3, CobolIntrinsics.ArgumentDomain.NonNegative, "SQRT", "§15.84.3 rule 2"));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainScaled(0, 3, CobolIntrinsics.ArgumentDomain.Positive, "LOG", "§15.55.3 rule 2"));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainScaled(1, -2, Unit, "ACOS", "§15.8.3 rule 2"));
            Assert.Equal(0.0, CobolIntrinsics.DomainScaled(0, -2, Unit, "ACOS", "§15.8.3 rule 2"));
        });
    }

    [Fact]
    public void TheDecScreen_SeesATinyNegative_ThatItsDoubleUnderflowsToMinusZero()
    {
        var tinyNegative = new CobolDec(-1, -400);                 // -1E-400: ToDouble underflows to -0.0
        Assert.Equal(0.0, tinyNegative.ToDouble());
        UnderChecking(true, () =>
        {
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainDec(tinyNegative, CobolIntrinsics.ArgumentDomain.NonNegative, "SQRT", "§15.84.3 rule 2"));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainDec(new CobolDec(0, 0), CobolIntrinsics.ArgumentDomain.Positive, "LOG10", "§15.56.3 rule 2"));
            Assert.Equal(0.5, CobolIntrinsics.DomainDec(new CobolDec(5, -1), CobolIntrinsics.ArgumentDomain.ClosedUnit, "ACOS", "§15.8.3 rule 2"));
        });
    }

    [Fact]
    public void TheRealScreen_AdmitsMinusZeroAsZero_AndRefusesNaN()
    {
        UnderChecking(true, () =>
        {
            Assert.True(double.IsNegative(CobolIntrinsics.DomainReal(-0.0, CobolIntrinsics.ArgumentDomain.NonNegative, "SQRT", "§15.84.3 rule 2")));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainReal(-0.0, CobolIntrinsics.ArgumentDomain.Positive, "LOG", "§15.55.3 rule 2"));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainReal(double.NaN, CobolIntrinsics.ArgumentDomain.ClosedUnit, "ASIN", "§15.10.3 rule 2"));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainReal(Math.BitIncrement(1.0), CobolIntrinsics.ArgumentDomain.ClosedUnit, "ASIN", "§15.10.3 rule 2"));
        });
    }

    [Fact]
    public void CheckingOff_TheRejectedArgumentBecomesNaN_SoTheBodyYieldsTheOneDefault()
    {
        UnderChecking(false, () =>
        {
            double arg = CobolIntrinsics.DomainScaled(Ten30 + 1, 30, CobolIntrinsics.ArgumentDomain.ClosedUnit, "ASIN", "§15.10.3 rule 2");
            Assert.True(double.IsNaN(arg));
            Assert.Equal(0.0, CobolIntrinsics.RealResult(CobolIntrinsics.Asin(arg)));
        });
    }
}
