// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A REFERENCE MODIFICATION THE PROGRAM WROTE AND A WINDOW THE COMPILER CHOSE ARE TWO EMITS, AND THE FIRST LIST
/// IS CLOSED (kb/Work PB1707 part 2, owner decision R60).
/// <para><c>RuntimeApi.StrRefMod</c> / <c>StrSpliceInto</c> render ISO §8.4.3.3's reference modification:
/// <c>CobolString.RefMod</c> / <c>SpliceInto</c>, whose range violation is EC-BOUND-REF-MOD and, with checking off,
/// the end of the run unit. Every OTHER character-image slice the emitter renders — a REDEFINES view, an
/// occurs-depending group's current extent, an INVOKE/CALL boundary prefix, a READ … INTO current record — is a
/// window onto an image that may legitimately be shorter than the extent asked for, and is
/// <c>RuntimeApi.StrWindow</c> / <c>StrWindowInto</c> (<c>CobolString.Window</c> / <c>WindowInto</c>), which pads
/// and clamps and never terminates. Emitting one of those through the reference-modification pair turns a short
/// image into a fatal error in a program that never wrote a reference modification.</para>
/// <para>So the files and the number of call sites that may render the reference-modification pair are LISTED
/// here: the procedure-division reference (<c>PlaceRenderer</c>'s read, write and figurative-write arms of a
/// <c>RefModPlace</c>) and a ref-modified function result (<c>IntrinsicRenderer</c>). A new site fails this test
/// until it is added with the reason it is a program's reference modification.</para>
/// </summary>
public sealed class RuntimeApiWindowDriftTests
{
    /// <summary>File name → the number of <c>RuntimeApi.StrRefMod(</c> / <c>RuntimeApi.StrSpliceInto(</c> call
    /// sites a program's reference modification needs in it.</summary>
    private static readonly Dictionary<string, int> UserReferenceModificationSites = new()
    {
        // RefModPlace: the read (StrRefMod), the write (StrSpliceInto) and the figurative-constant write (StrSpliceInto, repeat).
        ["PlaceRenderer.cs"] = 3,
        // A ref-modified FUNCTION RESULT (§8.4.3.3.3 SR2), read-only.
        ["IntrinsicRenderer.cs"] = 1,
    };

    [Fact]
    public void OnlyAProgramsReferenceModification_IsRenderedThroughTheRangeCheckedPair()
    {
        var found = new Dictionary<string, int>();
        foreach (string f in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler", "CodeGen"), "*.cs",
                     SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(f);
            if (name == "RuntimeApi.cs") continue;   // the emit helpers themselves
            int n = Regex.Matches(File.ReadAllText(f), @"RuntimeApi\.Str(?:RefMod|SpliceInto)\(").Count;
            if (n > 0) found[name] = n;
        }
        Assert.Equal(UserReferenceModificationSites.OrderBy(k => k.Key), found.OrderBy(k => k.Key));
    }
}
