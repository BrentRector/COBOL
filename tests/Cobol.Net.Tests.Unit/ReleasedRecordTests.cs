// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1195 — <see cref="CobolFile.ReleasedRecord"/> is the logical record a WRITE, REWRITE or RELEASE releases
/// at its released length (§13.18.43.4 GR13): a DEPENDING ON item's content cuts the record-area image
/// (GR13 a), otherwise the record is its own size (GR13 b)/c)) and the image is the record. A length past the image
/// releases the image — a record cannot be longer than the area it came from — and never invents characters.
/// </summary>
public sealed class ReleasedRecordTests
{
    [Theory]
    [InlineData("ABCDEFGH", 3, "ABC")]      // GR13 a): the DEPENDING ON item says 3
    [InlineData("ABCDEFGH", 8, "ABCDEFGH")] // equal: the record is the image
    [InlineData("ABCDEFGH", 0, "")]         // a zero-length record
    [InlineData("ABCDEFGH", -1, "ABCDEFGH")] // no DEPENDING ON: GR13 b)/c), the record's own size
    [InlineData("ABCDEFGH", 12, "ABCDEFGH")] // past the image: the image, never a pad
    public void ReleasedRecord_IsTheImageCutToGr13sLength(string image, int length, string expected) =>
        Assert.Equal(expected, CobolFile.ReleasedRecord(image, length));
}
