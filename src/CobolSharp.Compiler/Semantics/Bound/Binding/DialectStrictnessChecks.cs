// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// The legacy binder's obsolete-element flags (CBL3607) — the NIST OBSOLETE flagging modules.
/// <para>⛔ It no longer flags OPTIONAL WORDS (kb/Work PB756). It used to carry four "CCVS leniency" checks —
/// KEY omitted from INVALID KEY (L1), from RELATIVE KEY (L2), from RECORD KEY / ALTERNATE RECORD KEY (L3), and
/// COLLATING omitted from SORT/MERGE (L5) — each on the premise that an unbracketed word is required. The test is
/// UNDERLINING (ISO §5.2.2; §8.3.2.4.3: "uppercase words that are not underlined are called optional words"),
/// and none of those four words is underlined on the printed pages, so every one of them flagged CONFORMING source.
/// §4.2.10's warning mechanism flags extensions only.</para>
/// </summary>
internal static class DialectStrictnessChecks
{
    private static SourceLocation MakeLocation(string sourceName, ParserRuleContext ctx) =>
        new(sourceName, 0, ctx.Start.Line, ctx.Start.Column);

    private static TextSpan MakeSpan(ParserRuleContext ctx) =>
        new(ctx.Start.StartIndex, ctx.Stop?.StopIndex ?? ctx.Start.StopIndex);

    /// <summary>
    /// Obsolete-element flag (CBL3607) — the <c>OPEN … REVERSED</c> tape phrase. REVERSED is obsolete in
    /// COBOL-85 and removed in COBOL-2002. Warned under cobol85/Default (the conforming '85 flagger); under
    /// cobol2002+ the phrase is removed and handled by the removed-feature path (WS-DIALECT). Satisfies the
    /// NIST SQ303M OBSOLETE flagging module.
    /// </summary>
    internal static void CheckObsoleteOpenReversed(BindingContext ctx, CobolParserCore.OpenFileSpecContext spec)
    {
        if (spec.REVERSED() == null || ctx.Options.Config.IsCobol2002OrLater) return;
        ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3607,
            MakeLocation(ctx.SourceName, spec), MakeSpan(spec), "OPEN ... REVERSED");
    }
}
