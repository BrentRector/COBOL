// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation;

/// <summary>
/// ⛔ THE ONE FAILURE OF LOCALE-COLLATION RESOLUTION (kb/Work PB2773): the collation DATA a locale resolves through
/// cannot be used — a site CLDR file (<c>COBOL_CLDR_DIR</c>) or <c>.tailor</c> file (<c>COBOL_COLLATION_DIR</c>) that
/// is malformed, unreadable, written for another UCA version, or that asks for more than the builder can number.
/// Every data problem on the resolution path raises THIS type, at the boundary where the data is read or applied:
/// <see cref="Cldr.CldrLocaleLoader"/> (parsing a CLDR file), <see cref="TailoringRules"/> (parsing a <c>.tailor</c>),
/// <see cref="CollationTable.WithTailoring"/> (a stale <c>@version</c>) and <see cref="Cldr.CldrTailoringBuilder"/>
/// (numbering). A problem with ONE rule of a CLDR collation is not a failure of the locale: the builder skips the rule
/// and lists it in <see cref="ResolvedLocaleCollation.Unsupported"/>.
/// <para>The consumers each turn it into their defined outcome, never a crash: <see cref="CollationEngine.ResolveLocale"/>
/// records it as <see cref="ResolvedLocaleCollation.Failure"/> over the root order (the nearest available order);
/// <see cref="CollationEngine.TryGetOrderingTable"/> answers false with its message (the compiler's COBOLNET1662
/// warning, EC-ORDER-NOT-SUPPORTED at run time, §15.85.4 r2); <see cref="CollationEngine.IsKnownLocale"/> answers
/// false (the locale is not available in this operating environment); <see cref="Locale.LocaleManager.GetLocale"/>
/// throws it to its host caller; and <see cref="CollationRuntime.Warmup"/> reports it as its Warning.</para>
/// </summary>
public sealed class LocaleCollationException : Exception
{
    /// <summary>A failure of <paramref name="dataSource"/> (a file path, or the name of an embedded resource), at
    /// <paramref name="line"/> when the problem has one.</summary>
    public LocaleCollationException(string dataSource, int? line, string reason, Exception? inner = null)
        : base(line is { } l ? $"{dataSource}({l}): {reason}" : $"{dataSource}: {reason}", inner)
    {
        DataSource = dataSource;
        Line = line;
        Reason = reason;
    }

    /// <summary>The file (or embedded resource) whose data cannot be used.</summary>
    public string DataSource { get; }

    /// <summary>The 1-based line of the problem in <see cref="DataSource"/>, when it has one.</summary>
    public int? Line { get; }

    /// <summary>What is wrong, without the source.</summary>
    public string Reason { get; }
}
