// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The reference format in effect at each physical line of ONE source file or library text (kb/Work PB1067) — what
/// the §6.5 logical conversion decided while it read that text: the initial format, then each change a
/// <c>&gt;&gt;SOURCE FORMAT</c> directive (§7.3.24.3 1)) or a <c>&gt;&gt;POP</c> of the format (§7.3.22) makes.
/// <para>Its asker is the COPY statement: "The default reference format of library text is the reference format that
/// was in effect for the COPY statement that resulted in processing of this library text" (§7.3.24.3 3)). The COPY
/// statement's physical line — its <see cref="Common.SourceOrigin"/> — is looked up in the map of the text it was
/// written in, so a COPY inside library text asks that library text's map
/// (<see cref="LibraryTextDefaultAt"/>).</para>
/// </summary>
public sealed class ReferenceFormatMap
{
    // (first 1-based physical line, fixed form?, was the format DETECTED rather than stated?) in ascending line order;
    // element 0 starts at line 1.
    private readonly (int FromLine, bool Fixed, bool Detected)[] _changes;

    private ReferenceFormatMap((int FromLine, bool Fixed, bool Detected)[] changes, int[] directiveLines,
        string[] physicalText)
    {
        _changes = changes;
        DirectiveLines = directiveLines;
        PhysicalText = physicalText;
    }

    /// <summary>The text of each PHYSICAL line of this file, as the line-entry stage settled it (<see cref="PhysicalLines"/>:
    /// terminators gone, tabs expanded), 0-based. The line-entry stage is the ONLY place a text becomes lines
    /// (<c>PhysicalLinesDriftTests</c>), so a later stage that must look at the physical text of a file — the front end's
    /// search for an END-PERFORM phrase the parse could not read (kb/Work PB1066) — reads it HERE, never splits the file
    /// again.</summary>
    public IReadOnlyList<string> PhysicalText { get; }

    /// <summary>The 1-based physical lines of this text that hold a WRITTEN directive the reference format answers to —
    /// a <c>&gt;&gt;SOURCE FORMAT</c>, <c>&gt;&gt;PUSH</c> or <c>&gt;&gt;POP</c> line — ascending, whether or not it
    /// changed the format. What decides whether a §14.9.28.4 GR14 implicit PUSH ALL / POP ALL bracket can matter to the
    /// normalizer at all: a bracket that encloses no such line restores exactly the format it saved
    /// (<see cref="ImplicitFormatOps.Place"/>; kb/Work PB1066).</summary>
    public IReadOnlyList<int> DirectiveLines { get; }

    /// <summary>A text read in <paramref name="initialFixed"/> from line 1 — <paramref name="detected"/> when no
    /// directive or COPY statement stated it — and then in each format of <paramref name="changes"/> (a directive
    /// states each) from its 1-based physical line on (<paramref name="changes"/> ascending).
    /// <paramref name="directiveLines"/> are the lines of its written format directives (<see cref="DirectiveLines"/>) and
    /// <paramref name="physicalText"/> the text of each of its physical lines (<see cref="PhysicalText"/>).</summary>
    public static ReferenceFormatMap Create(bool initialFixed, bool detected, IEnumerable<(int FromLine, bool Fixed)> changes,
        IEnumerable<int> directiveLines, IEnumerable<string> physicalText)
        => new([(1, initialFixed, detected), .. changes.Select(c => (c.FromLine, c.Fixed, false))], [.. directiveLines],
            [.. physicalText]);

    /// <summary>The initial reference format of library text copied by a COPY statement at 1-based physical line
    /// <paramref name="line"/> of this text — §7.3.24.3 3)'s "reference format that was in effect for the COPY
    /// statement" — or null when this text holds no format to hand on and the library text's own is detected.
    /// <para>A format a directive stated, or one the library text inherited, is handed on as the rule says. A
    /// DETECTED format comes only from the documented <c>--source-format auto</c> extension over §7.3.24.3 2)'s
    /// fixed-form default (text is classified by its structure — kb/Work PB1362): a text detected FIXED showed the fixed
    /// column structure and hands fixed form on; a text detected FREE showed only that it is not column-bound —
    /// seven-space indentation is legal in both formats — which says nothing of the library text, so that library text
    /// is classified by its own structure, exactly as the compilation group was.</para></summary>
    public bool? LibraryTextDefaultAt(int line)
    {
        var (_, isFixed, detected) = At(line);
        return detected && !isFixed ? null : isFixed;
    }

    private (int FromLine, bool Fixed, bool Detected) At(int line)
    {
        int lo = 0, hi = _changes.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (_changes[mid].FromLine <= line) lo = mid; else hi = mid - 1;
        }
        return _changes[lo];
    }
}
