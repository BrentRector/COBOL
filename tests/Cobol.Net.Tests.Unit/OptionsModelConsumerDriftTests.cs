// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY FIELD OF THE OPTIONS MODEL HAS A CONSUMER (kb/Work PB152, PB298).
/// <see cref="OptionsModel"/> is the bound OPTIONS paragraph (ISO §11.9), and a field of it is a clause the
/// program WROTE. A field that <c>OptionsBinder</c> sets and nothing reads is a clause accepted without diagnostic
/// that changes nothing, which PB152 measured as a silent wrong answer (the INITIALIZE clause's SECTIONS were
/// bound and ignored). PB298 found the same shape four more times: two FLOAT-DECIMAL fields (deleted when the
/// clause was declined with COBOLNET2424, kb/Work R43) and the ENTRY-CONVENTION name, which the screen in
/// <c>OptionsBinder.ScreenEntryConvention</c> leaves with exactly one admissible value, COBOL, the implied
/// default. A clause that is screened or declined, and so cannot change a program, has no field.
/// <para>The rule pinned here: each public property of <see cref="OptionsModel"/> is read as
/// <c>…Options.&lt;Field&gt;</c> (or <c>(… Options).&lt;Field&gt;</c>) somewhere in <c>src/</c> outside its producer
/// (<c>OptionsBinder.cs</c>) and its declaration (<c>OptionsModel.cs</c>). A new field fails the test until a
/// consumer reads it, so a producer-only field cannot be added in silence.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class OptionsModelConsumerDriftTests
{
    private static readonly string[] ProducerFiles = ["OptionsBinder.cs", "OptionsModel.cs"];

    private static IEnumerable<string> ConsumerSources() =>
        Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !ProducerFiles.Contains(Path.GetFileName(p)));

    /// <summary>The source with every <c>//</c> line comment (and so every <c>///</c> doc comment) removed, so a
    /// field NAMED in prose (a <c>&lt;see cref&gt;</c> to it) is not counted as a read.</summary>
    private static string Code(string path) =>
        string.Join('\n', File.ReadAllText(path).Replace("\r\n", "\n").Split('\n')
            .Select(l => l.IndexOf("//", StringComparison.Ordinal) is var i and >= 0 ? l[..i] : l));

    [Fact]
    public void EveryOptionsModelField_IsReadOutsideItsProducer()
    {
        var fields = typeof(OptionsModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).ToList();
        Assert.True(fields.Count >= 5, $"OptionsModel exposes {fields.Count} public properties; the scan no longer "
            + "sees the model (Arithmetic, DefaultRounding, FloatBinaryEndianness, IntermediateRounding and Initialize "
            + "are all expected) — fix the reflection, not the assertion");

        var sources = ConsumerSources().Select(Code).ToList();
        Assert.NotEmpty(sources);
        var unread = fields
            .Where(f => !sources.Any(s => Regex.IsMatch(s, $@"Options\)?\s*\.\s*{f}\b")))
            .ToList();
        Assert.True(unread.Count == 0, "OptionsModel field(s) set by OptionsBinder and read by nothing in src/: "
            + string.Join(", ", unread) + ". A producer-only OPTIONS field is a clause the program wrote that changes "
            + "nothing (kb/Work PB152, PB298): wire its consumer, or — when the clause is screened or declined and "
            + "so can change no program — delete the field and keep only the screen.");
    }
}
