// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The drift guard behind <c>DataBinder.UsageWord</c>'s DERIVED default arm (kb/Work PB184's landing): the
/// §13.18.63.3 SR14 diagnostic names the usage the programmer wrote, so every <see cref="Usage"/> member has to
/// render as words a COBOL program can contain. Before this, the default arm was
/// <c>usage.ToString().ToUpperInvariant()</c> and rendered 'PROGRAMPOINTER', 'FLOATBINARY32', 'BINARYCHAR' and
/// 'OBJECTREFERENCE' — none of them COBOL words.
///
/// <para>⛔ The point of the test is that adding a <see cref="Usage"/> member stays AUTOMATIC. The hyphenation
/// derives the keyword for all but five members; this asserts the derivation still lands on reserved words, so a
/// new member whose enum name is not its COBOL spelling fails here instead of shipping in a diagnostic.</para>
/// </summary>
public class UsageWordDriftTests
{
    /// <summary>"A word a COBOL program can contain" is the SAME disjunction VersionConformancePass uses for the
    /// §8.9 user-word gates: a lexer keyword token, or a row in the ReservedWords table. COMPUTATIONAL-5 is the
    /// case that makes the disjunction necessary — it has no §13.18.60.2 spelling at all (it is the dialect word
    /// the programmer wrote, lexed as COMPUTATIONAL_5), so ReservedWords alone would reject it.</summary>
    private static bool IsCobolWord(string w) =>
        CobolNet.Frontend.Parsing.CobolKeywordTokens.TryTokenType(w, out _) || ReservedWords.Find(w) is not null;

    [Fact]
    public void EveryUsage_RendersAsCobolWords()
    {
        var offenders = new List<string>();
        foreach (Usage usage in Enum.GetValues<Usage>())
        {
            string word = UsageFamilies.UsageWord(usage);
            foreach (string token in word.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (!IsCobolWord(token))
                    offenders.Add($"{usage} → \"{word}\" (token '{token}' is not a COBOL word)");
        }
        Assert.True(offenders.Count == 0,
            "DataBinder.UsageWord must render §13.18.60 keywords: " + string.Join("; ", offenders));
    }

    /// <summary>The derivation itself, on the shapes that motivated it — a hyphen before each interior capital
    /// and before a digit run that follows a non-digit.</summary>
    [Theory]
    [InlineData(Usage.ProgramPointer, "PROGRAM-POINTER")]
    [InlineData(Usage.FunctionPointer, "FUNCTION-POINTER")]
    [InlineData(Usage.FloatBinary32, "FLOAT-BINARY-32")]
    [InlineData(Usage.FloatDecimal16, "FLOAT-DECIMAL-16")]
    [InlineData(Usage.BinaryChar, "BINARY-CHAR")]
    [InlineData(Usage.BinaryDouble, "BINARY-DOUBLE")]
    [InlineData(Usage.Display, "DISPLAY")]
    [InlineData(Usage.Index, "INDEX")]
    [InlineData(Usage.ObjectReference, "OBJECT REFERENCE")]
    [InlineData(Usage.Packed, "PACKED-DECIMAL")]
    public void UsageWord_IsTheCobolSpelling(Usage usage, string expected) =>
        Assert.Equal(expected, UsageFamilies.UsageWord(usage));
}
