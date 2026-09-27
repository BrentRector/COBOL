// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1067 — the reference format library text starts in: "The default reference format of library text is the
/// reference format that was in effect for the COPY statement that resulted in processing of this library text"
/// (§7.3.24.3 3)), asked of the <see cref="ReferenceFormatMap"/> the §6.5 walker records for the text holding the COPY.
/// </summary>
public sealed class LibraryTextReferenceFormatTests
{
    private static ReferenceFormatMap MapOf(string text, bool? initialFixed = null)
    {
        ReferenceFormatProcessor.NormalizeToFreeFormMapped(text, 2023, permissive: false, diagnostics: null, "t.cob",
            initialFixed, out var formats);
        return formats;
    }

    [Fact] // 1): a SOURCE FORMAT directive states the format from the next line on — and the COPY there hands it on.
    public void StatedFormat_IsHandedOnFromTheLineAfterTheDirective()
    {
        var map = MapOf("       >>SOURCE FORMAT FIXED\n       COPY A.\n       >>SOURCE FORMAT FREE\nCOPY B.\n");
        Assert.True(map.LibraryTextDefaultAt(2));
        Assert.False(map.LibraryTextDefaultAt(4));
    }

    [Fact] // A DETECTED fixed text hands fixed on; a text detected free (no column structure) hands nothing on.
    public void DetectedFormat_FixedIsHandedOn_FreeIsNot()
    {
        Assert.True(MapOf("000100 IDENTIFICATION DIVISION.\n000200 COPY A.\n").LibraryTextDefaultAt(2));
        Assert.Null(MapOf("IDENTIFICATION DIVISION.\nCOPY A.\n").LibraryTextDefaultAt(2));
    }

    [Fact] // 3): library text that INHERITED its initial format hands that format on to a COPY inside it, free included.
    public void InheritedFormat_IsHandedOn()
    {
        Assert.False(MapOf("COPY A.\n", initialFixed: false).LibraryTextDefaultAt(1));
        Assert.True(MapOf("       COPY A.\n", initialFixed: true).LibraryTextDefaultAt(1));
    }
}
