// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;
namespace CobolNet.CodeGen;

/// <summary>
/// ⛔ <b>THE ONE PLACE A RECEIVER'S OBJECT-PROPERTY ACCESSORS ARE PLACED</b> (kb/Work PB2078). A statement whose binder
/// CLAIMED its receivers (<c>DataBinder.OoClaimInterleavedReceiver</c>) places each claimed receiver's
/// <see cref="ReceiverBracket"/> — identifier-3's evaluation into the identified-object temporary and the §8.4.3.9.4 GR1
/// GET (the OPEN steps), the GR2 SET (the CLOSE steps) — where the statement's own rules identify and store that receiver,
/// and nowhere else. Two timings exist, and each statement's emitter states which is its own:
/// <list type="bullet">
///   <item><b>Identified as it is stored</b> — <see cref="Receive"/> opens the bracket just before the receiver's
///     access-and-store and closes it just after: ISO §14.7.7 4) b) ("Item identification for the receiving data items is
///     done as each data item is accessed"), §14.9.25.4 GR1 (MOVE, so every implicit move), §14.9.39.4 GR2 (SET), the
///     PERFORM VARYING induction variable (§14.9.28.4 GR12).</item>
///   <item><b>Identified at the start of the statement</b> — <see cref="Identify"/> opens the brackets of the statement's
///     receivers before its first operation (§14.6.4 7), the default: STRING, UNSTRING, CALL), and the receiver's store is then
///     closed by <see cref="Receive"/> (which does not open it twice) or, for a statement whose receivers are all stored
///     at one moment, by <see cref="Settle"/>. The SET then still runs before the statement's own phrase bodies (ON
///     OVERFLOW, ON EXCEPTION), which may read the property.</item>
/// </list>
/// <para>A size error ends the receiver's participation: §14.7.7 4) b) leaves "only that data item … unchanged", so the
/// SET does not run for a receiver whose store raised one (<paramref name="sizeErrorFlag"/>, the statement's flag).</para>
/// <para>The scope is opened by <see cref="StatementEmitter"/> for a <see cref="BoundReceiverBrackets"/> and CLOSED
/// loudly: a claimed bracket no <see cref="Receive"/> or <see cref="Settle"/> placed means an emitter forgot the call, and
/// a dropped GET or SET would be a silent wrong answer.</para>
/// </summary>
internal sealed class ReceiverBracketEmitter(EmitContext ctx)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the steps are statements).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    private sealed class Scope(IReadOnlyList<ReceiverBracket> brackets)
    {
        public IReadOnlyList<ReceiverBracket> Brackets { get; } = brackets;
        /// <summary>Brackets whose OPEN steps <see cref="Identify"/> emitted at the statement's start.</summary>
        public HashSet<ReceiverBracket> Opened { get; } = [];
        /// <summary>Brackets whose CLOSE steps were placed.</summary>
        public HashSet<ReceiverBracket> Placed { get; } = [];
    }

    private readonly Stack<Scope> _scopes = new();

    /// <summary>Emit <paramref name="node"/>'s inner statement with its brackets standing, and fail the compilation if any
    /// bracket was never placed.</summary>
    public bool Emit(BoundReceiverBrackets node)
    {
        var scope = new Scope(node.Brackets);
        _scopes.Push(scope);
        bool terminated, completed = false;
        try
        {
            terminated = Statements.EmitStatement(node.Inner);
            completed = true;
        }
        finally { _scopes.Pop(); }
        if (completed && node.Brackets.FirstOrDefault(b => !scope.Placed.Contains(b)) is { } unplaced)
            throw new InvalidOperationException(
                $"internal error: the object-property receiver '{unplaced.Temp.CobolName}' was claimed by its statement's binder "
                + "but no receiver store placed its accessors (ReceiverBracketEmitter.Receive / Settle); emitting the statement "
                + "would drop its GET/SET");
        return terminated;
    }

    /// <summary>The statement identifies its receivers as its first operation (ISO §14.6.4 7)): emit the OPEN steps of the
    /// brackets of <paramref name="receivers"/> now, in the order given (the statement's left-to-right order), so a later
    /// change to a subscript of identifier-3 — an earlier receiver of the same statement, a BY REFERENCE argument — does not
    /// move the receiver. A receiver that is no claimed object property (a data item, null for an absent phrase) has
    /// nothing to open. The receivers are named, never "every bracket in scope": a statement nested in another's phrase
    /// body runs inside that statement's scope, whose brackets are not its own.</summary>
    public void Identify(params IEnumerable<Place?> receivers)
    {
        foreach (var receiver in receivers)
            if (receiver is not null && Find(receiver) is ({ } scope, { } bracket) && scope.Opened.Add(bracket))
                foreach (var step in bracket.Open) Statements.EmitStatement(step);
    }

    /// <summary>Emit one receiver's access-and-store (<paramref name="store"/>) with its object-property accessors around
    /// it, when it has any: the OPEN steps first unless <see cref="Identify"/> already emitted them, the CLOSE steps after.
    /// <paramref name="sizeErrorFlag"/> is the statement's size-error flag variable while size errors are being checked,
    /// else null.</summary>
    public void Receive(Place receiver, string? sizeErrorFlag, Action store)
    {
        if (Find(receiver) is not ({ } scope, { } bracket)) { store(); return; }
        if (!scope.Opened.Contains(bracket))
            foreach (var step in bracket.Open) Statements.EmitStatement(step);
        scope.Placed.Add(bracket);
        if (bracket.Close.Count == 0) { store(); return; }
        if (sizeErrorFlag is null)
        {
            store();
            foreach (var step in bracket.Close) Statements.EmitStatement(step);
            return;
        }
        // The flag accumulates over the statement's receivers; this receiver's own outcome needs it clear around its store.
        var w = ctx.Writer;
        string prior = $"__rb{ctx.Names.NextStoreTmp()}";
        w.Line($"bool {prior} = {sizeErrorFlag}; {sizeErrorFlag} = false;");
        store();
        using (w.Block($"if (!{sizeErrorFlag})"))
            foreach (var step in bracket.Close) Statements.EmitStatement(step);
        w.Line($"{sizeErrorFlag} |= {prior};");
    }

    /// <summary><see cref="Receive(Place, string?, Action)"/> for a SET-family receiver: an index-name is no data item and
    /// has no accessors.</summary>
    public void Receive(BoundSetTarget receiver, Action store)
    {
        if (receiver is SetPlaceTarget { Place: var place }) Receive(place, null, store);
        else store();
    }

    /// <summary>Every one of <paramref name="receivers"/> has been stored (a CALL's RETURNING item and BY REFERENCE
    /// arguments, all at the return): emit the CLOSE steps of each bracket <see cref="Identify"/> opened, before the
    /// statement's phrase bodies run.</summary>
    public void Settle(params IEnumerable<Place?> receivers)
    {
        foreach (var receiver in receivers)
            if (Unsettled(receiver) is ({ } scope, { } bracket) && scope.Placed.Add(bracket))
                foreach (var step in bracket.Close) Statements.EmitStatement(step);
    }

    /// <summary>Whether <see cref="Settle"/> on <paramref name="receivers"/> would emit any CLOSE step — so a statement whose
    /// settlement must be guarded (a CALL's: only a successful activation stores, and the SET runs outside the activation's
    /// own exception partition, kb/Work PB2078) emits its guard only when there is something to guard.</summary>
    public bool Settles(IEnumerable<Place?> receivers) =>
        receivers.Any(r => Unsettled(r) is (_, { Close.Count: > 0 }));

    /// <summary>The bracket of <paramref name="receiver"/> that <see cref="Identify"/> opened and no store has placed yet.</summary>
    private (Scope, ReceiverBracket)? Unsettled(Place? receiver) =>
        receiver is not null && Find(receiver) is ({ } scope, { } bracket) && scope.Opened.Contains(bracket)
            && !scope.Placed.Contains(bracket)
            ? (scope, bracket) : null;

    private (Scope, ReceiverBracket)? Find(Place receiver)
    {
        foreach (var scope in _scopes)
            foreach (var bracket in scope.Brackets)
                if (ReferenceEquals(bracket.Temp, receiver.Item))
                    return (scope, bracket);
        return null;
    }
}
