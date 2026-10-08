// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DECIMAL-POINT MODE OF A PICTURE IS THE DESCRIBING UNIT'S, READ OFF THE ITEM (kb/Work PB2554).
/// ISO §12.3.7.4 GR14 b) makes the comma the decimal separator "in character-strings, and inserted in numeric-edited
/// items" under DECIMAL-POINT IS COMMA — a property of the source element that DESCRIBES the item; §12.3.4 GR1 hands
/// it to a contained program only. A separately compiled class keeps it for its formals no matter which unit INVOKEs
/// them, so every editing, de-editing, receiver-scale and edited-image consumer reads
/// <c>PicInfo.DecimalPointIsComma</c> (carried by the picture's <c>Clause</c>) and none reads the EMITTING or BINDING
/// unit's mode for an item's PICTURE. The unit's own mode stays the right answer for exactly the things the unit
/// itself writes or runs: the closed allowlist below names each, with its reason.
/// </summary>
public sealed class DecimalPointModeDriftTests
{
    private static readonly Regex UnitModeRead =
        new(@"\b(?:ctx\.Data|Data|_currentData)\??\.(?:DecimalPointIsComma|DecimalSeparator)\b", RegexOptions.Compiled);

    /// <summary>The unit-mode readers that are NOT about an item's PICTURE: (file, expected count, why).</summary>
    private static readonly (string File, int Count, string Why)[] Allowed =
    [
        ("CodeGen/Emit/IntrinsicRenderer.cs", 1,
            "CommaFlag: NUMVAL / NUMVAL-C / NUMVAL-F parse a STRING at run time under the mode of the unit that "
            + "references the function (§15 NUMVAL), not an item's picture"),
        ("Binding/Procedure/Verbs/IntrinsicBinder.cs", 2,
            "DateTimeFormatGrammar.Describe/Classify: a date-time format literal the referencing unit wrote"),
        ("Binding/Procedure/ExpressionBinder.cs", 2,
            "the two numeric-literal producers: the literal keeps the separator its writer used (PB1643, "
            + "NumericLiteralImageDriftTests)"),
    ];

    [Fact]
    public void NoPictureConsumer_ReadsTheEmittingOrBindingUnitsMode()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        var dirs = new[] { Path.Combine(root, "CodeGen"), Path.Combine(root, "Binding", "Procedure") };
        var found = new Dictionary<string, int>();
        foreach (string dir in dirs)
            foreach (string f in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                int n = UnitModeRead.Matches(File.ReadAllText(f)).Count;
                if (n > 0) found[Path.GetRelativePath(root, f).Replace('\\', '/')] = n;
            }

        var allowed = Allowed.ToDictionary(a => a.File, a => a.Count);
        var unexpected = found.Where(kv => !allowed.TryGetValue(kv.Key, out int n) || n != kv.Value)
            .Select(kv => $"{kv.Key} x{kv.Value}")
            .Concat(allowed.Where(kv => !found.ContainsKey(kv.Key)).Select(kv => $"{kv.Key}: allowlisted but no longer reads it"))
            .ToList();
        Assert.True(unexpected.Count == 0,
            "A CodeGen / procedure-binder file reads the EMITTING unit's DECIMAL-POINT IS COMMA mode (" + string.Join("; ", unexpected)
            + "). An item's PICTURE is edited, de-edited and scaled under the mode of the unit that DESCRIBES it "
            + "(§12.3.7.4 GR14 b): read PicInfo.DecimalPointIsComma. If the read is not about a picture, add it to the allowlist with its reason (kb/Work PB2554).");
    }

    [Fact]
    public void ThePicturesMode_IsTheClausesOwn_AndFalseWhereItCannotMatter()
    {
        var dpc = new PicInfo(PicCategory.NumericEdited, Usage.Display, 6, 3, 0, false)
        {
            Clause = new PictureClauseIdentity("ZZ9,99", null, true, null),
        };
        var period = dpc with { Clause = new PictureClauseIdentity("ZZ9.99", null, false, null) };
        var noSeparator = dpc with { Clause = new PictureClauseIdentity("ZZ9", null, null, null) };
        Assert.True(dpc.DecimalPointIsComma);
        Assert.False(period.DecimalPointIsComma);
        Assert.False(noSeparator.DecimalPointIsComma);
        Assert.False(new PicInfo(PicCategory.Numeric, Usage.Display, 1, 1, 0, false).DecimalPointIsComma);
    }
}
