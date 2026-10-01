// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1233 — a CONSTANT RECORD's content is that of an INITIALIZE, not the initial state, and the choice is
/// made ONCE, at the root, by <c>ValueInitializer.RecipeFor</c> (ISO §13.18.15.4 GR1: "as though the clause had
/// been omitted and the record had been the subject of an INITIALIZE statement that is specified with the FILLER
/// phrase; the VALUE phrase with the category-name of ALL; and the DEFAULT phrase"). The defect was that both seed
/// lanes composed the ordinary initial state (the OPTIONS INITIALIZE background, a blank numeric-edited item, a
/// group-level VALUE) for a structured constant. Every seed enters through one of two roots — the record-struct
/// lane (<c>ValueInitializer.FieldInit</c>) and the image lane (<c>GroupImageCodec.ImageInitOf</c>) — so this pins
/// that BOTH ask the one chooser first, and that nothing else decides the recipe from <c>IsConstantRecord</c>:
/// a third entry that spells the test itself is a third copy of the rule.
/// </summary>
public sealed class ConstantRecordSeedRecipeDriftTests
{
    private static string Body(string file, string signature)
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "DataDivision", file));
        int start = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"{signature} not found in {file} — the drift test must follow the method");
        int open = src.IndexOf('{', start);
        // The recipe line must be the method's FIRST statement; look only at the text up to the first comment-free
        // statement boundary, i.e. the first 400 characters of the body.
        return src.Substring(open, Math.Min(400, src.Length - open));
    }

    [Fact]
    public void FieldInit_AsksTheChooserFirst()
    {
        string head = Body("ValueInitializer.cs", "public string FieldInit(");
        Assert.Contains("recipe = RecipeFor(item, recipe);", head);
    }

    [Fact]
    public void ImageInitOf_AsksTheChooserFirst()
    {
        string head = Body("GroupImageCodec.cs", "public string ImageInitOf(");
        Assert.Contains("recipe = ValueInitializer.RecipeFor(item, recipe);", head);
    }

    [Fact]
    public void NoOtherSeedCodeDecidesTheRecipeFromTheConstantRecordFlag()
    {
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(
                     TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "DataDivision"), "*.cs"))
        {
            string name = Path.GetFileName(file);
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].TrimStart();
                if (t.StartsWith("//") || t.StartsWith("///")) continue;
                // useValues: IsConstantRecord is the EXTERNAL seed's VALUE switch (§11.9.10.4 GR7), a different
                // question; a recipe ternary on the flag is the copy this test forbids.
                if (lines[i].Contains("IsConstantRecord") && lines[i].Contains("SeedRecipe.")
                    && name != "ValueInitializer.cs")
                    offenders.Add($"{name}:{i + 1}: {lines[i].Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A seed lane chooses its SeedRecipe from IsConstantRecord itself instead of ValueInitializer.RecipeFor "
            + "(ISO §13.18.15.4 GR1; kb/Work PB1233):\n" + string.Join("\n", offenders));
    }
}
