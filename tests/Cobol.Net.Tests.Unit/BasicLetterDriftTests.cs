// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A RULE THAT SAYS "BASIC LETTER" ASKS ONE PREDICATE, <see cref="CobolCharacterRepertoire.IsBasicLetter"/>
/// (kb/Work PB533; ISO §8.1.3.1 Table 1, §13.18.40.3 SR8). The basic letters are A-Z and a-z; an extended letter
/// (Annex B, §8.1.3.2 GR4) is a distinct row of the same table. <see cref="char.IsLetter(char)"/> admits every Unicode
/// letter, so a syntax rule that asked it let the extended letters through where the standard names only the basic
/// ones — the PICTURE EDITING phrase's character-1 compiled with an E-acute for exactly that reason. The compiler
/// assembly therefore asks neither <c>char.IsLetter</c> nor <c>char.IsLetterOrDigit</c> of a character it holds as
/// COBOL source: the word-character question has its own answer (<see cref="CobolCharacterRepertoire.IsLetter"/>),
/// and a test of some other string's characters carries the marker <c>// not a COBOL character:</c>.
/// </summary>
public sealed class BasicLetterDriftTests
{
    private const string Marker = "// not a COBOL character:";

    [Theory]
    [InlineData('A', true)]
    [InlineData('Z', true)]
    [InlineData('a', true)]
    [InlineData('z', true)]
    [InlineData('T', true)]
    [InlineData('0', false)]
    [InlineData('-', false)]
    [InlineData('_', false)]
    [InlineData('É', false)]    // LATIN CAPITAL LETTER E WITH ACUTE: an extended letter
    [InlineData('é', false)]
    [InlineData('ß', false)]
    [InlineData('α', false)]    // GREEK SMALL LETTER ALPHA
    [InlineData('Я', false)]    // CYRILLIC CAPITAL LETTER YA
    [InlineData('ア', false)]   // KATAKANA LETTER A
    public void IsBasicLetter_IsTable1sLatinRowAndNothingElse(char c, bool basic)
    {
        Assert.Equal(basic, CobolCharacterRepertoire.IsBasicLetter(c));
        // The word-character question is the SUPERSET: an extended letter is a letter of a word, a digit is not.
        Assert.Equal(basic || c >= 0x80, CobolCharacterRepertoire.IsLetter(c));
    }

    [Fact]
    public void TheCompiler_AsksNoUnicodeLetterQuestion_OfASourceCharacter()
    {
        var hits = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Root, file);
            if (rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p is "obj" or "bin" or "Generated"))
                continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;   // prose, not a call
                if ((lines[i].Contains("char.IsLetter(", StringComparison.Ordinal)
                        || lines[i].Contains("char.IsLetterOrDigit(", StringComparison.Ordinal))
                    && !lines[i].Contains(Marker, StringComparison.Ordinal))
                    hits.Add($"{rel}:{i + 1}: {code}");
            }
        }
        Assert.True(hits.Count == 0,
            "ask a COBOL character with CobolCharacterRepertoire.IsBasicLetter (a rule that says 'basic letter') or "
            + $"IsLetter (a letter of a word); a string that is not COBOL source carries '{Marker} <what>':\n"
            + string.Join("\n", hits));
    }
}
