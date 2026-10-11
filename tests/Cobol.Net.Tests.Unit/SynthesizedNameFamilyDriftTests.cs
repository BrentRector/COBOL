// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Compiler.Oo;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SYNTHESIZED C# NAME THAT EMBEDS A USER-DEFINED WORD COMES FROM A TAGGED FAMILY IN
/// <see cref="NamingConvention"/>, AND NO EMITTER-FIXED <c>__X</c> NAME ENTERS A FAMILY (kb/Work PB973).
/// The emitter declares its own members in the <c>__</c> space (<c>__N</c> the paragraph count, <c>__pc</c>,
/// <c>__V</c>, <c>__NEW</c>, …). A method formal's parameter was once <c>"__" + WORD</c> — an EMPTY tag — so a
/// LINKAGE formal named N became <c>__N</c> and shadowed the paragraph-count constant (Roslyn CS1628 on a legal
/// method). A user word never sanitizes to a leading <c>__</c> (§8.3.2.1: a hyphen or underscore is never the
/// first character), so a TAGGED family can only meet another <c>__</c> name that starts with its tag; this class
/// proves no fixed name does, that the families are mutually prefix-free, and that no site outside the naming
/// authority re-derives a <c>__</c> name from a word.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class SynthesizedNameFamilyDriftTests
{
    private static readonly Regex StringLiteral = new(@"\$?@?""(?:[^""\\\r\n]|\\.)*""", RegexOptions.Compiled);
    private static readonly Regex DunderIdent = new(@"(?<![A-Za-z0-9_])__[A-Za-z][A-Za-z0-9_]*", RegexOptions.Compiled);

    /// <summary>The untagged concatenation shape that WAS the bug: <c>"__" + …</c>.</summary>
    private static readonly Regex UntaggedConcat = new(@"""__""\s*\+", RegexOptions.Compiled);

    /// <summary>Interpolated <c>$"__{…}</c> names: adjudicated sites whose hole is an emitter-FIXED stem followed by
    /// a serial, never a user word. file → (count, reason). A new one is an adjudication.</summary>
    private static readonly Dictionary<string, (int Count, string Reason)> InterpolatedStemSites =
        new(StringComparer.Ordinal)
    {
        [Path.Combine("CodeGen", "Emit", "EmitCore.cs")] =
            (1, "SendOnce: the stem is the calling statement's fixed lowercase word, then a store-temp serial"),
        [Path.Combine("CodeGen", "Verbs", "SetEmitter.cs")] =
            (1, "LandAmount: the prefix is a fixed lowercase stem (set / cap / pv), then a SET serial"),
    };

    private static IEnumerable<(string Rel, string Text)> CompilerSources()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains("Generated", StringComparison.Ordinal)
                || f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) continue;
            // Code only: a comment may QUOTE the retired shape (this class's own subject) without deriving it.
            var code = File.ReadLines(f).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal));
            yield return (Path.GetRelativePath(root, f), string.Join(Environment.NewLine, code));
        }
    }

    private static readonly string NamingFile = Path.Combine("Oo", "NamingConvention.cs");

    [Fact]
    public void EveryFamily_CarriesANonEmptyTag_AndTheFamiliesArePrefixFree()
    {
        var fams = NamingConvention.CobolWordDerivedPrefixes;
        Assert.Equal(fams.Count, fams.Distinct(StringComparer.Ordinal).Count());
        foreach (string p in fams)
            Assert.Matches(@"^__[A-Za-z][A-Za-z0-9]*_$", p);
        foreach (string a in fams)
            foreach (string b in fams)
                if (!ReferenceEquals(a, b) && a != b)
                    Assert.False(b.StartsWith(a, StringComparison.Ordinal), $"family '{a}' is a prefix of family '{b}'");
    }

    [Fact]
    public void NoEmitterFixedName_EntersAWordDerivedFamily()
    {
        var offenders = new List<string>();
        foreach (var (rel, text) in CompilerSources())
        {
            if (rel == NamingFile) continue;
            foreach (Match lit in StringLiteral.Matches(text))
                foreach (Match id in DunderIdent.Matches(lit.Value))
                    foreach (string fam in NamingConvention.CobolWordDerivedPrefixes)
                        if (id.Value.StartsWith(fam, StringComparison.Ordinal))
                            offenders.Add($"{rel}: '{id.Value}' is inside the word-derived family '{fam}'");
        }
        Assert.True(offenders.Count == 0, "an emitter-fixed name can collide with a user-word-derived name "
            + "(derive it through NamingConvention, or rename the fixed member):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void NoSite_DerivesAnUntaggedDunderName()
    {
        var offenders = new List<string>();
        var interpolated = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (rel, text) in CompilerSources())
        {
            if (UntaggedConcat.IsMatch(text)) offenders.Add($"{rel}: \"__\" + …");
            int n = Regex.Matches(text, @"\$""__\{").Count;
            if (n > 0) interpolated[rel] = n;
        }
        Assert.True(offenders.Count == 0, "untagged `__` + word derivation (the PB973 shape):\n" + string.Join("\n", offenders));
        foreach (var (rel, n) in interpolated)
            Assert.True(InterpolatedStemSites.TryGetValue(rel, out var adj) && adj.Count == n,
                $"{rel}: {n} interpolated $\"__{{…}}\" name(s) — derive a word-bearing name through NamingConvention, "
                + "or adjudicate a fixed-stem site in InterpolatedStemSites");
        foreach (var (rel, adj) in InterpolatedStemSites)
            Assert.True(interpolated.TryGetValue(rel, out int got) && got == adj.Count,
                $"{rel}: adjudicated {adj.Count} interpolated stem site(s), found {(interpolated.TryGetValue(rel, out int g) ? g : 0)} — update the adjudication");
    }
}
