// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>Whether a <see cref="DirectiveStackOp"/> saves (§7.3.22 PUSH) or restores (§7.3.20 POP).</summary>
public enum DirectiveStackKind { Push, Pop }

/// <summary>
/// ONE <c>&gt;&gt;PUSH</c> or <c>&gt;&gt;POP</c> directive (ISO §7.3.22 / §7.3.20), reduced to what the directive
/// state stack acts on: where it was written, which way it goes, and which directive's state it names — a
/// <c>constructs.json</c> ROW id, or null for <c>ALL</c>.
/// </summary>
/// <param name="Line">1-based line in the frame of the stage that parsed it.</param>
/// <param name="Kind">PUSH or POP.</param>
/// <param name="Row">The named directive's registry row (§7.3.20.2 / §7.3.22.2 <c>directive-name</c>), or null
/// when <c>ALL</c> is specified. A row, never a spelling: <c>&gt;&gt;PUSH FLAG-02</c> names the FLAG-02 row and
/// nothing else.</param>
public readonly record struct DirectiveStackOp(int Line, DirectiveStackKind Kind, string? Row)
{
    /// <summary>Read a parsed directive line as a PUSH/POP op. False when the line is not a PUSH or POP, and also
    /// when its operand is malformed — <c>COBOLNET1911</c> has already said so at the one recognition point
    /// (kb/Work PB794), and a malformed directive changes no state.</summary>
    public static bool TryParse(CompilerDirectiveLine directive, int line, out DirectiveStackOp op)
    {
        op = default;
        DirectiveStackKind kind;
        if (directive.Word.Equals("PUSH", StringComparison.OrdinalIgnoreCase)) kind = DirectiveStackKind.Push;
        else if (directive.Word.Equals("POP", StringComparison.OrdinalIgnoreCase)) kind = DirectiveStackKind.Pop;
        else return false;
        if (!CompilerDirectiveCatalog.TryOperandWord(directive.Word, directive.Operand, out string word)
            || word.Length == 0) return false;
        if (word == "ALL") { op = new DirectiveStackOp(line, kind, null); return true; }
        if (CompilerDirectiveCatalog.PushableRowOf(word) is not { } row) return false;
        op = new DirectiveStackOp(line, kind, row);
        return true;
    }

    /// <summary>Parse <paramref name="rawLine"/> as a PUSH/POP directive line (see the other overload).</summary>
    public static bool TryParse(string rawLine, int line, out DirectiveStackOp op)
    {
        op = default;
        return CompilerDirectiveLine.TryParse(rawLine, out var d) && TryParse(d, line, out op);
    }
}

/// <summary>
/// A holder of ONE directive's state that a PUSH saves and a POP restores — the only thing a stage writes to
/// take part in §7.3.20 / §7.3.22. The pairing of PUSH with POP, the ALL fan-out and the unsuccessful-POP rule
/// live in <see cref="DirectiveStateStack"/>, once; a carrier knows only how to snapshot and reinstate itself.
/// </summary>
public interface IDirectiveStateCarrier
{
    /// <summary>A snapshot of the state in effect now (§7.3.22.4 GR1: "the state of the directive is saved";
    /// GR3: the directive's effects remain active, so saving changes nothing).</summary>
    object? Save();

    /// <summary>Reinstate <paramref name="saved"/> — §7.3.20.4 GR1/GR3, "the state … shall be restored" — from
    /// the POP directive on <paramref name="popLine"/> onward.</summary>
    void Restore(object? saved, int popLine);
}

/// <summary>
/// THE compiler-directive state stack (ISO/IEC 1989:2023 §7.3.20 POP, §7.3.22 PUSH; kb/Work PB941) — one
/// mechanism for every directive whose state the standard lets a PUSH save, never per-directive save code.
///
/// <list type="bullet">
/// <item>§7.3.22.4 GR1 — "If directive-name is specified, the state of the directive is saved."</item>
/// <item>§7.3.22.4 GR2 — "If ALL is specified, the state of all of the directives other than EVALUATE, IF, PAGE,
/// POP, or PUSH are saved." The set is <see cref="CompilerDirectiveCatalog.PushableRows"/>, derived from the
/// registry, so a new directive is pushed by ALL because it exists.</item>
/// <item>§7.3.20.4 GR1 / GR3 — a POP restores the named directive's (or, for ALL, every directive's) state
/// "previously stored by a PUSH directive and … not removed by a POP directive". Each directive therefore has a
/// STACK of saved states, and a POP removes the most recent one.</item>
/// <item>§7.3.20.4 GR2 — a named POP with nothing stored "is unsuccessful"; <see cref="Apply"/> answers that, and
/// <see cref="DirectiveSiteProcessor"/> is the one place that warns (<c>COBOLNET2297</c>).</item>
/// </list>
///
/// <para>The state itself lives where each directive is processed — the reference format in the normalizer, the
/// compilation variables in the conditional-compilation driver, the line-scoped toggles in the post-COPY stages —
/// and each of those stages runs its OWN instance of this class over the same PUSH/POP sequence, registering the
/// carriers it holds (<see cref="Carry"/>). Every instance keeps a stack for EVERY pushable row, carried or not,
/// so the PUSH/POP pairing is identical in every stage whatever subset of the state that stage holds.
/// <see cref="DirectiveStateRegistry"/> says which stage carries which directive, and its drift test says that
/// every pushable directive is accounted for.</para>
/// </summary>
public sealed class DirectiveStateStack
{
    private readonly Dictionary<string, Stack<object?>> _saved = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IDirectiveStateCarrier> _carriers = new(StringComparer.Ordinal);
    private readonly IReadOnlyList<DirectiveStackOp> _program;
    private int _next;

    /// <summary>A stack that is fed ops one at a time through <see cref="Apply"/> — the shape for a stage that
    /// meets the PUSH/POP lines itself, in encounter order.</summary>
    public DirectiveStateStack() : this([]) { }

    /// <summary>A stack that REPLAYS <paramref name="program"/> — the PUSH/POP ops of the final text, recorded by
    /// <see cref="DirectiveSiteProcessor"/> before it blanked their lines — as a line-scanning stage advances
    /// (<see cref="AdvanceTo"/>).</summary>
    public DirectiveStateStack(IReadOnlyList<DirectiveStackOp> program) => _program = program;

    /// <summary>Register the carrier of <paramref name="row"/>'s state in this stage. ⛔ The row must be one
    /// <see cref="DirectiveStateRegistry"/> declares carried — a carrier the registry does not know about is a
    /// directive whose PUSH/POP behaviour no test covers.</summary>
    public DirectiveStateStack Carry(string row, IDirectiveStateCarrier carrier)
    {
        if (!DirectiveStateRegistry.IsCarried(row))
            throw new InvalidOperationException(
                $"directive row '{row}' is not declared carried in DirectiveStateRegistry (kb/Work PB941)");
        _carriers.Add(row, carrier);
        return this;
    }

    /// <summary>Apply one PUSH or POP. Returns false exactly when the op is a POP of a named directive whose state
    /// was never stored, or was already restored — §7.3.20.4 GR2's unsuccessful POP. A POP ALL with nothing
    /// stored restores nothing and returns false too; GR2 speaks of directive-name only, so callers warn only for
    /// a named POP.</summary>
    public bool Apply(DirectiveStackOp op)
    {
        bool any = false;
        foreach (string row in op.Row is { } r ? [r] : CompilerDirectiveCatalog.PushableRows)
        {
            if (op.Kind == DirectiveStackKind.Push)
            {
                if (!_saved.TryGetValue(row, out var stack)) _saved[row] = stack = new Stack<object?>();
                stack.Push(_carriers.TryGetValue(row, out var carrier) ? carrier.Save() : null);
                any = true;
            }
            else if (_saved.TryGetValue(row, out var stack) && stack.Count > 0)
            {
                object? saved = stack.Pop();
                if (_carriers.TryGetValue(row, out var carrier)) carrier.Restore(saved, op.Line);
                any = true;
            }
        }
        return any;
    }

    /// <summary>Apply every op of the replayed program written on a line BEFORE <paramref name="line"/> — call it
    /// before acting on a directive at <paramref name="line"/>, so a PUSH saves the state its predecessors built
    /// and a POP restores before the next directive is read.</summary>
    public void AdvanceTo(int line)
    {
        while (_next < _program.Count && _program[_next].Line < line) Apply(_program[_next++]);
    }

    /// <summary>Apply the rest of the replayed program.</summary>
    public void AdvanceToEnd() => AdvanceTo(int.MaxValue);
}

/// <summary>
/// A carrier over a mutable value the stage snapshots by copy — the shape of a SEQUENTIAL stage's state (the
/// reference format, the compilation-variable table, the running FLAG options).
/// </summary>
public sealed class DirectiveValueCarrier<T>(Func<T> save, Action<T> restore) : IDirectiveStateCarrier
{
    /// <inheritdoc/>
    public object? Save() => save();

    /// <inheritdoc/>
    public void Restore(object? saved, int popLine) => restore((T)saved!);
}

/// <summary>
/// The LINE-SCOPED directive events one stage collects — raw, in line order, each tagged with the directive ROW it
/// belongs to — for every directive whose state is a fold over its events (TURN, REF-MOD-ZERO-LENGTH, FLAG-02,
/// FLAG-14). Collecting is ALL a stage does: the PUSH/POP history is applied by <see cref="ToTimeline"/>, which
/// replays the op program through the ONE <see cref="DirectiveStateStack"/> (<see cref="DirectiveTimeline{T}"/>).
/// </summary>
public sealed class DirectiveEventLog<T>
{
    private readonly List<DirectiveTimeline<T>.Entry> _entries = [];

    /// <summary>Record <paramref name="ev"/>, a directive of <paramref name="row"/> written on resultant line
    /// <paramref name="line"/>, in line order.</summary>
    public void Add(string row, int line, T ev) => _entries.Add(new(row, line, ev));

    /// <summary>The events with the PUSH/POP <paramref name="program"/> of the same text applied (kb/Work PB941).</summary>
    public DirectiveTimeline<T> ToTimeline(IReadOnlyList<DirectiveStackOp> program) =>
        _entries.Count == 0 ? DirectiveTimeline<T>.Empty : DirectiveTimeline<T>.Replay([.. _entries], program);
}

/// <summary>
/// A directive's line-ordered events plus, per event, the line of the POP that revoked it (or
/// <see cref="int.MaxValue"/>) — what the binder's compile-time directive states fold over (kb/Work PB941). It IS
/// the event list (<see cref="IReadOnlyList{T}"/>), so a consumer that only enumerates is unchanged; a fold asks
/// <see cref="InEffectAt"/> in addition to its own line test.
///
/// <para>For such a directive "the state" at a line is the fold of the events in effect there, so SAVING it is
/// remembering how many of the row's events were in effect, and RESTORING it is revoking every one recorded since:
/// an event is REVOKED at the POP line, and from that line on it is as though it had never been written — which is
/// exactly §7.3.20.4 GR1's "the state … shall be restored". The events are never reordered or synthesized.</para>
///
/// <para>⛔ A timeline keeps its EVENTS and its OP PROGRAM, and its revocations are always a REPLAY of the one
/// over the other through <see cref="DirectiveStateStack"/> — never patched. That is what lets a later phase add
/// the PUSH/POP ops only it can see (<see cref="WithStackOps"/>): §14.9.28.4 GR14's implicit PUSH ALL / POP ALL
/// around an exception-checking PERFORM's handlers are placed by the PARSE TREE, after every frontend stage has
/// run, and they pair with the explicit ops on the same stacks (kb/Work PB1004; the conditional-compilation
/// driver, which runs before any parse, receives the same ops by re-running — kb/Work PB1066).</para>
/// </summary>
public sealed class DirectiveTimeline<T> : IReadOnlyList<T>
{
    /// <summary>One collected event: its directive row, its resultant line, and the event.</summary>
    public readonly record struct Entry(string Row, int Line, T Event);

    private readonly IReadOnlyList<T> _events;
    private readonly IReadOnlyList<int>? _revokedAt;
    private readonly IReadOnlyList<Entry>? _entries;             // null for a hand-built list (Of)
    private readonly IReadOnlyList<DirectiveStackOp> _program;

    private DirectiveTimeline(IReadOnlyList<T> events, IReadOnlyList<int>? revokedAt, IReadOnlyList<Entry>? entries,
        IReadOnlyList<DirectiveStackOp> program)
    {
        _events = events;
        _revokedAt = revokedAt;
        _entries = entries;
        _program = program;
    }

    /// <summary>No events.</summary>
    public static readonly DirectiveTimeline<T> Empty = new([], null, [], []);

    /// <summary><paramref name="events"/> as a timeline — itself when it already is one, otherwise with no event
    /// ever revoked (a source with no PUSH/POP, or a hand-built test list).</summary>
    public static DirectiveTimeline<T> Of(IReadOnlyList<T>? events) =>
        events as DirectiveTimeline<T> ?? (events is null || events.Count == 0 ? Empty : new(events, null, null, []));

    /// <summary>Replay <paramref name="program"/> over <paramref name="entries"/> through ONE
    /// <see cref="DirectiveStateStack"/>, each row's events carried by its own carrier: a PUSH saves how many of
    /// the row's events are in effect, a POP revokes those recorded since, at the POP's line (§7.3.22.4 GR1/GR2,
    /// §7.3.20.4 GR1/GR3). Every op written BEFORE an event's line applies before it (the stages' own
    /// <c>AdvanceTo</c> discipline); the ops after the last event apply too.</summary>
    internal static DirectiveTimeline<T> Replay(IReadOnlyList<Entry> entries, IReadOnlyList<DirectiveStackOp> program)
    {
        var revokedAt = new int[entries.Count];
        Array.Fill(revokedAt, int.MaxValue);
        var stack = new DirectiveStateStack(program);
        var live = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (var e in entries)
            if (!live.ContainsKey(e.Row))
            {
                var rowLive = live[e.Row] = [];
                stack.Carry(e.Row, new RowCarrier(rowLive, revokedAt));
            }
        for (int i = 0; i < entries.Count; i++)
        {
            stack.AdvanceTo(entries[i].Line);   // the PUSH/POP written before this event
            live[entries[i].Row].Add(i);
        }
        stack.AdvanceToEnd();
        var events = new T[entries.Count];
        for (int i = 0; i < entries.Count; i++) events[i] = entries[i].Event;
        return new(events, revokedAt, entries, program);
    }

    /// <summary>This timeline with <paramref name="ops"/> — PUSH/POP ops placed by a LATER phase — merged into its
    /// op program (line order; an op on the same line as another keeps the order given, which is source order for
    /// the ops of one phase) and the whole program REPLAYED (kb/Work PB1004). Itself when there is nothing to add
    /// or no event to revoke.</summary>
    public DirectiveTimeline<T> WithStackOps(IReadOnlyList<DirectiveStackOp> ops)
    {
        if (ops.Count == 0 || _events.Count == 0) return this;
        if (_entries is null)
            throw new InvalidOperationException(
                "a hand-built DirectiveTimeline has no directive rows, so no PUSH/POP op can be replayed over it");
        return Replay(_entries, [.. _program.Concat(ops).OrderBy(op => op.Line)]);   // OrderBy is stable
    }

    /// <summary>Is event <paramref name="index"/> still in effect at a construct on <paramref name="siteLine"/>?
    /// False from the line of the POP that restored the state saved before it (§7.3.20.4 GR1/GR3). An explicit
    /// directive occupies its own line, so no construct shares an explicit POP's line; §14.9.28.4 GR14's implicit
    /// POP ALL is placed at the END-PERFORM line, which the statements after END-PERFORM share.</summary>
    public bool InEffectAt(int index, int siteLine) => _revokedAt is null || siteLine < _revokedAt[index];

    /// <summary>The line of the POP that revoked event <paramref name="index"/>, or <see cref="int.MaxValue"/>.</summary>
    public int RevokedAt(int index) => _revokedAt?[index] ?? int.MaxValue;

    /// <summary>THE fold of a single-valued ON/OFF directive at a construct on <paramref name="siteLine"/>: the most
    /// recent event written strictly BEFORE the site (a directive occupies its own line and applies to the text that
    /// follows it — the <c>&gt;&gt;TURN</c> GR5 discipline) and still in effect there (<see cref="InEffectAt"/>, a
    /// POP-revoked event is gone). False when no such event exists — the directive is UNSPECIFIED at the site, and
    /// each caller applies its own default. The REF-MOD-ZERO-LENGTH and PROPAGATE states are this fold (kb/Work
    /// PB1119), so neither re-spells the line test or the revocation test.</summary>
    public bool TryLastInEffectBefore(int siteLine, Func<T, int> lineOf, out T ev)
    {
        ev = default!;
        bool found = false;
        for (int k = 0; k < _events.Count; k++)
        {
            if (lineOf(_events[k]) >= siteLine) break;   // events are in line order
            if (InEffectAt(k, siteLine)) { ev = _events[k]; found = true; }
        }
        return found;
    }

    /// <inheritdoc/>
    public T this[int index] => _events[index];

    /// <inheritdoc/>
    public int Count => _events.Count;

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => _events.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The carrier of one row's events: its state is how many of them are live.</summary>
    private sealed class RowCarrier(List<int> live, int[] revokedAt) : IDirectiveStateCarrier
    {
        public object? Save() => live.Count;

        public void Restore(object? saved, int popLine)
        {
            int keep = (int)saved!;
            for (int k = keep; k < live.Count; k++) revokedAt[live[k]] = popLine;
            live.RemoveRange(keep, live.Count - keep);
        }
    }
}
