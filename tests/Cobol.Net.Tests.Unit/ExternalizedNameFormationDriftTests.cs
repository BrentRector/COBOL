// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN EXTERNALIZED NAME IS FORMED AND MAPPED BY ONE FUNCTION, <see cref="ExternalizedNames"/> (kb/Work PB1539,
/// DOC-A.1-68). The defect this pins was two copies of one rule disagreeing: the AS literal was registered as
/// written while every CALL / CANCEL / program-address target was <c>Trim()</c>'d — in the emitted C# AND again in
/// <c>ProgramTable</c> — so <c>PROGRAM-ID. S AS "trail  "</c> could not be reached even by <c>CALL "trail  "</c>;
/// and the mapping (case ignored) was written three times (<c>ProgramTable.NameEquals</c>,
/// <c>BinderDriver.NameEq</c>, the pointer <c>SameTarget</c>s). So: (1) the rule itself is pinned by value, and (2)
/// no private name-equality helper and no emitted <c>.Trim()</c> of a program-name expression may reappear in the
/// files that used to carry the copies.
/// </summary>
public sealed class ExternalizedNameFormationDriftTests
{
    [Theory]
    [InlineData("  trail  ", "trail")]
    [InlineData(" Shared Two ", "Shared Two")]   // an inner space is part of the name
    [InlineData("\tTAB\t", "\tTAB\t")]            // only SPACES are padding; a tab is a character of the name
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Form_RemovesLeadingAndTrailingSpacesOnly(string? written, string formed) =>
        Assert.Equal(formed, ExternalizedNames.Form(written));

    [Fact]
    public void Same_IgnoresCase_AndTwoNullAddressesAreTheSame()
    {
        Assert.True(ExternalizedNames.Same("sub-p", "SUB-P"));
        Assert.True(ExternalizedNames.Same(null, null));
        Assert.False(ExternalizedNames.Same(null, "SUB-P"));
        Assert.False(ExternalizedNames.Same("SUB-P", "SUB-Q"));
    }

    /// <summary>A private helper that restates the mapping rule — the shape <c>NameEquals</c> / <c>NameEq</c> had.</summary>
    private static readonly Regex PrivateNameEquality = new(@"\bbool\s+Name(Eq|Equals)\s*\(", RegexOptions.Compiled);

    /// <summary>An emitted <c>(…).Trim()</c> inside a C#-text string — the second, white-space-wide formation the
    /// CALL / CANCEL / program-address emitters carried.</summary>
    private static readonly Regex EmittedTrim = new(@"\)\.Trim\(\)""", RegexOptions.Compiled);

    [Fact]
    public void NoSecondCopyOfTheRule_InTheFilesThatCarriedOne()
    {
        string[] mappingFiles =
        [
            TestRepo.Src("Cobol.Net.Runtime", "Control", "ProgramTable.cs"),
            TestRepo.Src("Cobol.Net.Compiler", "Binding", "BinderDriver.cs"),
        ];
        string[] emitterFiles =
        [
            TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "CallEmitter.cs"),
            TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "PtrEmitter.cs"),
        ];
        var hits = new List<string>();
        foreach (string f in mappingFiles)
        {
            Assert.True(File.Exists(f), $"{f} moved — this drift test scans a path that no longer exists");
            int n = 0;
            foreach (string line in File.ReadLines(f))
            {
                n++;
                if (PrivateNameEquality.IsMatch(line)) hits.Add($"{Path.GetFileName(f)}:{n}: {line.Trim()}");
            }
        }
        foreach (string f in emitterFiles)
        {
            Assert.True(File.Exists(f), $"{f} moved — this drift test scans a path that no longer exists");
            int n = 0;
            foreach (string line in File.ReadLines(f))
            {
                n++;
                if (EmittedTrim.IsMatch(line)) hits.Add($"{Path.GetFileName(f)}:{n}: {line.Trim()}");
            }
        }
        Assert.True(hits.Count == 0,
            "A second copy of the externalized-name formation or mapping rule reappeared; route it through "
            + "CobolNet.Runtime.ExternalizedNames (kb/Work PB1539, DOC-A.1-68):" + Environment.NewLine
            + string.Join(Environment.NewLine, hits));
    }
}
