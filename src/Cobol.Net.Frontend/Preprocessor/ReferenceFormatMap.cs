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

    private ReferenceFormatMap((int FromLine, bool Fixed, bool Detected)[] changes) => _changes = changes;

    /// <summary>A text read in <paramref name="initialFixed"/> from line 1 — <paramref name="detected"/> when no
    /// directive or COPY statement stated it — and then in each format of <paramref name="changes"/> (a directive
    /// states each) from its 1-based physical line on (<paramref name="changes"/> ascending).</summary>
    public static ReferenceFormatMap Create(bool initialFixed, bool detected, IEnumerable<(int FromLine, bool Fixed)> changes)
        => new([(1, initialFixed, detected), .. changes.Select(c => (c.FromLine, c.Fixed, false))]);

    /// <summary>The initial reference format of library text copied by a COPY statement at 1-based physical line
    /// <paramref name="line"/> of this text — §7.3.24.3 3)'s "reference format that was in effect for the COPY
    /// statement" — or null when this text holds no format to hand on and the library text's own is detected.
    /// <para>A format a directive stated, or one the library text inherited, is handed on as the rule says. A
    /// DETECTED format is this compiler's documented extension over §7.3.24.3 2)'s fixed-form default (text with no
    /// SOURCE FORMAT directive is classified by its structure — DEVLOG 931): a text detected FIXED showed the fixed
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
