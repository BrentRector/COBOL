// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// ⛔ THE REFERENCE FORMAT'S SHARE OF ISO §14.9.28.4 GR14's IMPLICIT PUSH ALL / POP ALL (kb/Work PB1066): "An implicit
/// PUSH ALL followed by TURN OFF ALL is assumed at the end of imperative-statement-1. Immediately preceding the END
/// PERFORM phrase, there is an implicit POP ALL". ALL is every pushable directive (§7.3.22.4 GR2), and the SOURCE FORMAT
/// directive is one — so a <c>&gt;&gt;SOURCE FORMAT</c> written in a WHEN phrase's handler is undone before END-PERFORM,
/// exactly as a <c>&gt;&gt;DEFINE</c> there is.
/// <para>THE DIRECTIVE STATE LIVES WHERE THE DIRECTIVE IS PROCESSED, and the reference format is the one state that is
/// settled BEFORE any parse: the §6.5 logical conversion reads the physical lines in order with the format in effect as
/// state (<see cref="ReferenceFormatProcessor"/>), so the ops that restore it cannot be keyed to the conditional-
/// compilation driver's directive encounters (<see cref="ConditionalCompilationResult.KeyImplicitOps"/> — the driver
/// runs on the already-normalized text) nor to the post-COPY resultant lines the line-scoped timelines use. They are
/// keyed to PHYSICAL lines of the text they were written in: the parse places them in the resultant-line frame, and
/// <see cref="Place"/> maps each through the source-line map to its file and physical line, because the format is read
/// per physical line and each file — the compilation group and every library text — is converted by its own walker
/// with its own format state (§7.3.24.3 5: a library text's format reverts when it ends).</para>
/// <para><b>Where each op falls, in the line frame</b> (a reference format is a per-line property, so each op is a
/// BOUNDARY between physical lines; <see cref="ReferenceFormatProcessor"/> applies them): the PUSH takes effect after the
/// physical line that ends imperative-statement-1 ("at the end of"); the POP takes effect BEFORE the physical line that
/// holds END-PERFORM ("immediately preceding the END PERFORM phrase") — so the END-PERFORM line itself is read in the
/// RESTORED format, because the phrase is text that follows the POP (§7.3.24.3 1: a format governs "the source text …
/// following the directive").</para>
/// <para>Only a bracket that encloses a written format directive of its own file can change anything — it restores
/// exactly the format it saved otherwise — so only those are kept, which is what lets an ordinary source converge on the
/// first pass with no re-normalization (<c>Frontend.Parse</c>'s fixed point). A PUSH and POP in DIFFERENT files (an
/// END-PERFORM written in a copybook) are kept by neither: each file's walker owns its own format state.</para>
/// </summary>
public sealed class ImplicitFormatOps : IEquatable<ImplicitFormatOps>
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DirectiveStackOp>> _byFile;

    private ImplicitFormatOps(IReadOnlyDictionary<string, IReadOnlyList<DirectiveStackOp>> byFile) => _byFile = byFile;

    /// <summary>No implicit op — the program every ordinary source converges on.</summary>
    public static readonly ImplicitFormatOps None = new(new Dictionary<string, IReadOnlyList<DirectiveStackOp>>());

    /// <summary>The ops written in <paramref name="file"/>, in token order, each at its PHYSICAL line of that file.</summary>
    public IReadOnlyList<DirectiveStackOp> For(string file) => _byFile.TryGetValue(file, out var ops) ? ops : [];

    /// <summary>
    /// Place <paramref name="ops"/> — the parse's PUSH/POP ops in token order and the RESULTANT-line frame
    /// (<see cref="ExceptionPerformDirectiveScope.ImplicitOps"/>) — on the files they were written in, keeping the
    /// brackets that enclose a format directive of their own file (<see cref="ReferenceFormatMap.DirectiveLines"/>).
    /// </summary>
    /// <param name="origins">The source origin of each resultant line (<see cref="MappedText.Lines"/>).</param>
    /// <param name="formats">Each text's reference-format map, by file — the compilation group's and every library
    /// text's (<see cref="CopyProcessor.ReferenceFormats"/>).</param>
    public static ImplicitFormatOps Place(IReadOnlyList<DirectiveStackOp> ops, IReadOnlyList<SourceOrigin> origins,
        IReadOnlyDictionary<string, ReferenceFormatMap> formats)
    {
        if (ops.Count == 0 || origins.Count == 0) return None;
        var at = new SourceOrigin[ops.Count];
        for (int i = 0; i < ops.Count; i++) at[i] = origins[Math.Clamp(ops[i].Line, 1, origins.Count) - 1];

        // The ops nest (every exception-checking PERFORM contributes a PUSH then a POP, in token order): pair each POP
        // with its PUSH and keep the pair only when a written format directive of their one file lies between them.
        var keep = new bool[ops.Count];
        var open = new Stack<int>();
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i].Kind == DirectiveStackKind.Push) { open.Push(i); continue; }
            if (open.Count == 0)
                throw new InvalidOperationException(
                    "§14.9.28.4 GR14 implicit POP ALL without its PUSH ALL — ExceptionPerformDirectiveScope emits them in pairs");
            int push = open.Pop();
            if (at[push].File == at[i].File && formats.TryGetValue(at[i].File, out var map)
                && map.DirectiveLines.Any(d => d > at[push].Line && d < at[i].Line))
                keep[push] = keep[i] = true;
        }
        if (open.Count > 0)
            throw new InvalidOperationException(
                "§14.9.28.4 GR14 implicit PUSH ALL without its POP ALL — ExceptionPerformDirectiveScope emits them in pairs");

        var byFile = new Dictionary<string, List<DirectiveStackOp>>(StringComparer.Ordinal);
        for (int i = 0; i < ops.Count; i++)
        {
            if (!keep[i]) continue;
            if (!byFile.TryGetValue(at[i].File, out var list)) byFile[at[i].File] = list = [];
            list.Add(new DirectiveStackOp(at[i].Line, ops[i].Kind, ops[i].Row));
        }
        return byFile.Count == 0
            ? None
            : new(byFile.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<DirectiveStackOp>)kv.Value, StringComparer.Ordinal));
    }

    /// <summary>Equal programs mean equal normalizer behavior — <c>Frontend.Parse</c>'s fixed-point test.</summary>
    public bool Equals(ImplicitFormatOps? other) =>
        other is not null && _byFile.Count == other._byFile.Count
        && _byFile.All(kv => other._byFile.TryGetValue(kv.Key, out var theirs) && kv.Value.SequenceEqual(theirs));

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as ImplicitFormatOps);

    /// <inheritdoc/>
    public override int GetHashCode() => _byFile.Count;
}
