// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE METHOD ABI IS A PAIR PER FORMAL, AND ONLY TWO PLACES MAY SPELL IT (kb/Work PB757; COBOLNET_OO_DESIGN D6).
/// <para>Every method formal crosses as <c>ref T value, bool omitted</c> — the omitted state §14.9.23.4 GR9
/// requires, which a C# <c>ref T</c> does not have. <c>OoEmitter.OoSignatureOf</c> DECLARES the pair and
/// <c>OoEmitter.OoArgPair</c> RENDERS it at every caller (the typed INVOKE, the covariant adapter, the universal
/// switch, the PROPERTY setter's signature). Before PB757 the adapter, the universal switch and the property setter
/// each spelled <c>ref …</c> for themselves — four copies of one ABI — so adding the presence half meant finding
/// all four; the next ABI change would have meant finding them again.</para>
/// <para>This test scans <c>OoEmitter.cs</c> (where every method-ABI caller lives) for an interpolated <c>"ref {</c> / <c>$"ref {</c> argument or
/// parameter spelling and admits it only inside those two members.</para>
/// <para>⛔ THIS TEST HAS BEEN RED: run against the pre-PB757 tree it names the adapter's
/// <c>$"ref {f.ParamName}"</c>, the universal switch's <c>$"ref __p{i}"</c> and the typed INVOKE's
/// <c>$"ref {tmp}"</c> / <c>$"ref {PlaceRenderer.Read(mp)}"</c>.</para>
/// </summary>
public sealed class MethodAbiPairDriftTests
{
    /// <summary>A member declaration line: modifiers, a simple or TUPLE return type, the member name, '('.</summary>
    private const string MemberDecl =
        @"^\s*(?:private|public|internal)(?:\s+(?:static|override|virtual|sealed))*\s+(?:\([^)]*\)|[\w<>\[\]?,.]+)\s+(\w+)\(";

    private static readonly Regex RefSpelling = new(@"\$""ref (\{|__)|CellPointer\? \{", RegexOptions.Compiled);

    [Fact]
    public void MethodArgumentRefPair_IsSpelledOnlyByTheAbiBuilders()
    {
        // The method ABI's callers all live in OoEmitter (D5/D6/D10). Other CodeGen files spell C# `ref` for their
        // own runtime helpers (e.g. RuntimeApi's UNSTRING POINTER), which is not this ABI.
        var offenders = new List<string>();
        foreach (string f in new[] { TestRepo.At("src", "Cobol.Net.Compiler", "CodeGen", "Verbs", "OoEmitter.cs") })
        {
            string[] lines = File.ReadAllLines(f);
            string? member = null;
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.Match(lines[i], MemberDecl) is { Success: true } m)
                    member = m.Groups[1].Value;
                if (!RefSpelling.IsMatch(lines[i])) continue;
                if (Path.GetFileName(f) == "OoEmitter.cs" && member is "OoSignatureOf" or "OoArgPair") continue;
                offenders.Add($"{Path.GetFileName(f)}:{i + 1} (in {member ?? "?"}): {lines[i].Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "a method argument/parameter `ref` is spelled outside OoEmitter.OoSignatureOf / OoArgPair — route it "
            + "through OoArgPair so the (carrier, area, omitted) shape cannot drift (kb/Work PB757, PB2087):\n"
            + string.Join("\n", offenders));
    }
}
