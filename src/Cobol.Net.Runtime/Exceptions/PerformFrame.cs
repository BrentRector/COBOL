// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// One active Format-3 (exception-checking) PERFORM frame (ISO/IEC 1989:2023 §14.9.28.4 GR17–GR22). The emitted
/// PERFORM pushes a frame onto <see cref="ExceptionEngine"/>'s ambient stack around imperative-statement-1 and pops
/// it in a <c>finally</c>; a raise site inside imp-1 consults the top matching frame (via
/// <see cref="ExceptionEngine.RunTopFrame"/>) BEFORE the USE declaratives (GR17 — "any USE declarative that would
/// normally match … is ignored"). The <see cref="Matcher"/> closure performs the tier-ordered WHEN selection
/// (§14.9.49.4 GR3c–g) and runs the matching imp-2 (+ WHEN COMMON imp-4) as bounded pc-ranges, returning the
/// per-statement dispatch action the raise site consumes.
/// </summary>
public sealed class PerformFrame
{
    /// <summary>The tier-ordered WHEN selector the emitted PERFORM installs. Given the raised exception-name and
    /// (for EC-I-O) the file-connector key, it returns the per-statement dispatch-result action —
    /// <see cref="DispatchResult.Normal"/> handled/continue, <see cref="DispatchResult.ResumeNext"/> RESUME AT NEXT STATEMENT, or a pc <c>&gt;= 0</c> (the last is
    /// unreachable from a WHEN body: RESUME AT procedure-name in a WHEN is bind-rejected, COBOLNET1610) — or
    /// <see cref="NoMatch"/> when neither a WHEN nor WHEN OTHER selects <c>(ec, file)</c>. <c>file</c> is null for a
    /// non-I-O condition. <c>fatal</c> is deliberately NOT a parameter: the fatal-vs-nonfatal split (GR20) is
    /// realized by each raise site's own throw idiom, not by the matcher.</summary>
    public required Func<string /*ec*/, string? /*file*/, int> Matcher { get; init; }

    /// <summary>GR21 transparency: set while this frame is the one whose handler bodies (imp-2..5) are running, so
    /// an exception condition raised inside a handler is NOT re-caught by this same PERFORM (nor by an outer frame
    /// whose imp-1 is suspended while this handler runs) — see <see cref="ExceptionEngine.RunTopFrame"/>.</summary>
    public bool Handling { get; set; }

    /// <summary>The "no WHEN selected" sentinel — distinct from every real <see cref="DispatchResult"/> action (every named value
    /// and any non-negative pc), so a frame that legitimately returns <see cref="DispatchResult.Normal"/> (handled) is never
    /// confused with "no frame matched".</summary>
    public const int NoMatch = int.MinValue;
}
