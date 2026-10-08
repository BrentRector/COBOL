// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;
namespace CobolNet.CodeGen;

/// <summary>
/// ⛔ <b>THE ONE PLACE A RECEIVER'S OBJECT-PROPERTY ACCESSORS ARE PLACED</b> (kb/Work PB2078). A statement that stores its
/// receivers one at a time (the arithmetic statements and MOVE) calls <see cref="Receive"/> around each receiver's
/// access-and-store; when that receiver is an object property its <see cref="ReceiverBracket"/> — identifier-3's evaluation
/// and the §8.4.3.9.4 GR1 GET before, the GR2 SET after — is emitted there, and nowhere else.
/// <para>ISO §14.7.7 4) b): the intermediate result "is stored in or combined with and then stored in each single
/// resulting data item in the left-to-right order", with "Item identification for the receiving data items … done as
/// each data item is accessed"; §14.9.25.4 GR1: "Item identification for identifier-2 is performed immediately before
/// the data is moved to the respective data item". The property is the data item's stand-in (§8.4.3.9.4), so the GET
/// that fetches it belongs after the previous receiver's SET and the SET that stores it after ITS store.</para>
/// <para>A size error ends the receiver's participation: §14.7.7 4) b) leaves "only that data item … unchanged", so the
/// SET does not run for a receiver whose store raised one (<paramref name="sizeErrorFlag"/>, the statement's flag).</para>
/// <para>The scope is opened by <see cref="StatementEmitter"/> for a <see cref="BoundReceiverBrackets"/> and CLOSED
/// loudly: a claimed bracket no <see cref="Receive"/> placed means an emitter loop forgot the call, and a dropped GET or
/// SET would be a silent wrong answer.</para>
/// </summary>
internal sealed class ReceiverBracketEmitter(EmitContext ctx)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the steps are statements).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    private sealed class Scope(IReadOnlyList<ReceiverBracket> brackets)
    {
        public IReadOnlyList<ReceiverBracket> Brackets { get; } = brackets;
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
                + "but no receiver loop placed its accessors (ReceiverBracketEmitter.Receive); emitting the statement would drop its GET/SET");
        return terminated;
    }

    /// <summary>Emit one receiver's access-and-store (<paramref name="store"/>) with its object-property accessors around
    /// it, when it has any; <paramref name="sizeErrorFlag"/> is the statement's size-error flag variable while size
    /// errors are being checked, else null.</summary>
    public void Receive(Place receiver, string? sizeErrorFlag, Action store)
    {
        if (Find(receiver) is not { } bracket) { store(); return; }
        foreach (var step in bracket.Open) Statements.EmitStatement(step);
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

    private ReceiverBracket? Find(Place receiver)
    {
        foreach (var scope in _scopes)
            foreach (var bracket in scope.Brackets)
                if (ReferenceEquals(bracket.Temp, receiver.Item))
                {
                    scope.Placed.Add(bracket);
                    return bracket;
                }
        return null;
    }
}
