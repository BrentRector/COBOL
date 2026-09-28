// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The reference format a compilation group STARTS in, before any <c>&gt;&gt;SOURCE FORMAT</c> directive (kb/Work
/// PB1362) — the CLI's <c>--source-format</c>. ISO §7.3.24.3 2): "The default reference format of a compilation group
/// is fixed form", so <see cref="Fixed"/> is the default at every entry point. <see cref="Free"/> and
/// <see cref="Auto"/> are the implementor-defined mechanism §4.2.10 3) requires for selecting a nonstandard behavior
/// of a required language element ("provided that standard-conforming behavior is also implemented and that an
/// implementor-defined mechanism exists for selection of the nonstandard behavior"); GnuCOBOL, the rule-1 precedent,
/// likewise defaults to fixed and selects free form by a compiler flag. docs/CONFORMANCE.md DOC-A.1-158 documents
/// the three.
/// <para>A <c>&gt;&gt;SOURCE FORMAT</c> directive switches the format whichever selection started it (§7.3.24.3 1));
/// library text starts in the format in effect for its COPY statement (3)).</para>
/// </summary>
public enum InitialReferenceFormat
{
    /// <summary>Fixed form — the standard's default (§7.3.24.3 2)).</summary>
    Fixed,

    /// <summary>Free form from the first line — a stated selection, as if the text began with
    /// <c>&gt;&gt;SOURCE FORMAT FREE</c>, and available at every edition.</summary>
    Free,

    /// <summary>The documented extension: the initial format is DETECTED from the text's structure
    /// (<see cref="ReferenceFormatProcessor.IsFixedForm"/>) — a heuristic that can misjudge conforming fixed-form source,
    /// so it is never the default.</summary>
    Auto,
}

/// <summary>The one reader of the <c>--source-format</c> vocabulary (the CLI and the conformance harness's
/// <c>*&gt; options:</c> header both call it).</summary>
public static class InitialReferenceFormatOption
{
    /// <summary>The accepted spellings, in <see cref="InitialReferenceFormat"/> order.</summary>
    public static readonly IReadOnlyList<string> OptionSpellings = ["fixed", "free", "auto"];

    /// <summary>Parse an option value (case-insensitive). An absent value (null) is the standard default,
    /// <see cref="InitialReferenceFormat.Fixed"/>, and returns false; so does an unrecognized one.</summary>
    public static bool TryParse(string? value, out InitialReferenceFormat format)
    {
        format = InitialReferenceFormat.Fixed;
        if (value is null) return false;
        for (int i = 0; i < OptionSpellings.Count; i++)
            if (OptionSpellings[i].Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                format = (InitialReferenceFormat)i;
                return true;
            }
        return false;
    }

    /// <summary>The initial format as the §6.5 walker takes it: fixed?, or null to detect it.</summary>
    public static bool? InitialFixed(this InitialReferenceFormat format) => format switch
    {
        InitialReferenceFormat.Fixed => true,
        InitialReferenceFormat.Free => false,
        _ => null,
    };
}
