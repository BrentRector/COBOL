// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB2839 — the C# identifier derived from a COBOL word is INJECTIVE (<see cref="DataItem.IdentifierCharacters"/>,
/// and through it <see cref="DataItem.Sanitize"/>, <see cref="DataItem.WordIdentifier"/> and
/// <see cref="CsNames.ModuleNamespaceOf"/>): two different words, or two different assembly names, never give one
/// identifier. ISO §8.3.2.1 (cite.py OK): a COBOL word's characters are "basic letters, basic digits, extended letters, and
/// the basic special characters hyphen and underscore", so <c>A-B</c> and <c>A_B</c> are two words; the mapping wrote
/// both <c>A_B</c>, and two methods, properties or formals so named were one C# member (CS0111) — and modules
/// <c>PAY-ROLL</c> and <c>PAY_ROLL</c> one namespace and one registrar, so the second module was skipped.</summary>
public sealed class IdentifierInjectivityTests
{
    /// <summary>Pairs the old mapping sent to one identifier, and pairs a careless escape would.</summary>
    public static TheoryData<string, string> CollidingPairs => new()
    {
        { "A-B", "A_B" },
        { "A--B", "A__B" },
        { "A-_B", "A_-B" },
        { "A-U30FB-B", "A・B" },     // a hyphen beside an escape's spelling, upper-case (WordIdentifier's domain)
        { "a-u30FB-b", "a・b" },     // ... and lower-case (Sanitize's: an assembly name keeps its case)
        { "x-u005F", "x_" },
        { "-1A", "1A" },                 // a leading hyphen (an assembly name) against Sanitize's leading-digit prefix
        { "PAY.V2", "PAY_u002E_V2" },
    };

    [Theory]
    [MemberData(nameof(CollidingPairs))]
    public void TwoDifferentNames_NeverGiveOneIdentifier(string a, string b)
    {
        Assert.NotEqual(DataItem.IdentifierCharacters(a), DataItem.IdentifierCharacters(b));
        Assert.NotEqual(DataItem.Sanitize(a), DataItem.Sanitize(b));
        Assert.NotEqual(CsNames.ModuleNamespaceOf(a).ClrName, CsNames.ModuleNamespaceOf(b).ClrName);
    }

    [Fact]
    public void TheCommonWord_StillReadsLikeItsSource()
    {
        Assert.Equal("CUST_NAME", DataItem.WordIdentifier("cust-name"));
        Assert.Equal("A_u005F_B", DataItem.WordIdentifier("A_B"));
        Assert.Equal("_1ST_ITEM", DataItem.Sanitize("1ST-ITEM"));
    }

    /// <summary>The mapping is read back by its own rule (an underscore not followed by <c>u</c> is a hyphen; <c>_u</c>
    /// opens an upper-case hex escape closed by the next underscore), over every word built from the characters that
    /// make the mapping hard: a hyphen, an underscore, a lower- and an upper-case <c>u</c>, a hex digit, and a character
    /// C# refuses in an identifier. A decoder that recovers each input proves no two inputs share an output.</summary>
    [Fact]
    public void TheMapping_IsReadBackExactly_OverEveryShortWordOfTheHardCharacters()
    {
        const string alphabet = "-_uU5・A";
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string word in Words(alphabet, 4))
        {
            string id = DataItem.IdentifierCharacters(word);
            Assert.Equal(word, Decode(id));
            Assert.True(seen.TryAdd(id, word), $"'{word}' and '{(seen.TryGetValue(id, out var w) ? w : "")}' both give '{id}'");
        }
    }

    private static IEnumerable<string> Words(string alphabet, int maxLength)
    {
        IEnumerable<string> level = [""];
        for (int n = 1; n <= maxLength; n++)
        {
            level = level.SelectMany(p => alphabet.Select(c => p + c)).ToList();
            foreach (string w in level) yield return w;
        }
    }

    private static string Decode(string id)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < id.Length; i++)
        {
            if (id[i] != '_') { sb.Append(id[i]); continue; }
            if (i + 1 < id.Length && id[i + 1] == 'u')
            {
                int end = id.IndexOf('_', i + 2);
                sb.Append(char.ConvertFromUtf32(Convert.ToInt32(id[(i + 2)..end], 16)));
                i = end;
            }
            else sb.Append('-');
        }
        return sb.ToString();
    }
}
