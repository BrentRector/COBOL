// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Keeps <see cref="IntrinsicSig.RepeatsAnArgument"/> — the ISO §15.3 precondition for the ALL subscript in an
/// argument ("When the definition of a function permits an argument to be repeated a variable number of times, a
/// table may be referenced by … ALL"; kb/Work PB62) — DERIVED FROM THE STANDARD rather than from a hand-kept list.
/// </summary>
/// <remarks>
/// <para>The property is <c>MaxArgs == int.MaxValue</c>: a row is unbounded exactly when its §15.x.2 general
/// format carries an ellipsis (<c>{ argument-1 } …</c>, <c>argument-2 …</c>, <c>[ argument-2 ] …</c>). This test
/// re-reads every catalogued function's general format from <c>specs/ISO_COBOL.md</c> and asserts the equivalence
/// BOTH ways: an unbounded row whose format repeats nothing would admit `T(ALL)` where the standard forbids it, and
/// a bounded row whose format repeats an argument would reject legal source (and, separately, is an arity bug).
/// CONVERT and FIND-STRING are the instructive rows — Variadic in the catalog through their PHRASE words, MaxArgs
/// 4 and 3, no ellipsis: not repeatable. TRIM's 2023 format IS (`[ argument-2 ] …` — the figure notes say the
/// ellipsis "denotes repetition of that bracketed portion").</para>
/// <para>Keyed on the ELLIPSIS CHARACTER inside the format's <c>&lt;pre&gt;</c> block, never on prose: the
/// formats are diagrams, and this is the one mark the transcription preserved uniformly.</para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class IntrinsicRepeatedArgumentDriftTests
{
    private static readonly Regex Row = new(
        "Add\\(new\\(\"(?<n>[A-Z0-9-]+)\",\\s*IntrinsicType\\.\\w+,\\s*IntrinsicArity\\.\\w+,\\s*(?<min>[-\\w]+),\\s*(?<max>[-\\w]+),",
        RegexOptions.Compiled);

    private static Dictionary<string, bool> SpecFormatRepeats()
    {
        string spec = File.ReadAllText(TestRepo.Specs("ISO_COBOL.md"));
        // "### 15.60 MEAN function" → section 15.60; its general format is the "#### 15.60.2 General format"
        // block up to "#### 15.60.3".
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var (sec, name) in SpecClauseText.FunctionSections(spec))
        {
            var fmt = Regex.Match(spec, @"\n#### " + Regex.Escape(sec) + @"\.2 General format\s*\n(.*?)\n#### " + Regex.Escape(sec) + @"\.3 ", RegexOptions.Singleline);
            if (!fmt.Success) continue;
            result[name] = fmt.Groups[1].Value.Contains('…');
        }
        Assert.True(result.Count >= 79, $"only {result.Count} §15.x.2 general formats found in specs/ISO_COBOL.md — the heading shape changed; fix the scanner, do not lower the floor.");
        return result;
    }

    [Fact]
    public void RepeatsAnArgument_IsExactly_TheFormatsEllipsis()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));
        var rows = Row.Matches(src).Select(m => (Name: m.Groups["n"].Value, Max: m.Groups["max"].Value)).ToList();
        Assert.True(rows.Count >= 79, $"only {rows.Count} catalog rows parsed — the Add(new(...)) shape changed; fix the regex.");
        var spec = SpecFormatRepeats();
        var mismatches = new List<string>();
        foreach (var (name, max) in rows)
        {
            Assert.True(IntrinsicCatalog.TryGet(name, out var sig), $"catalog row {name} not resolvable");
            Assert.True(spec.TryGetValue(name, out bool repeats), $"no §15.x.2 general format found in the spec for {name}");
            if (sig.RepeatsAnArgument != repeats)
                mismatches.Add($"{name}: catalog MaxArgs {max} says RepeatsAnArgument={sig.RepeatsAnArgument}, the §15 general format {(repeats ? "carries" : "carries no")} ellipsis");
        }
        Assert.True(mismatches.Count == 0,
            "IntrinsicSig.RepeatsAnArgument (MaxArgs unbounded) disagrees with the standard's general formats — fix the row's arity bounds:\n  "
            + string.Join("\n  ", mismatches));
    }
}
