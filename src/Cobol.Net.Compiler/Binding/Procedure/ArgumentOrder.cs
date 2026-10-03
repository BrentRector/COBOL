// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ <b>THE ONE LEFT-TO-RIGHT ARGUMENT ORDER</b> — ISO §8.4.3.2.4 GR2 (kb/Work PB1423): "At the time reference is
/// made to a function, its arguments are evaluated individually in the order specified in the list of arguments,
/// from left to right. An argument being evaluated may itself be a function-identifier or may be an expression
/// containing function-identifiers." It governs every function-identifier — GR1's three name forms are all
/// "a function-identifier" — so the intrinsic call and the user-defined activation share it here.
///
/// <para><b>The defect it removes.</b> A user-defined function activation is a statement-scoped PRE-op
/// (<c>DataBinder.PendingPreOps</c>, hoisted ahead of the statement that consumes its result), and an argument
/// that merely READS state — an identifier, an arithmetic expression, an intrinsic result — is rendered at the
/// consumer, after every hoisted activation. So the later argument's activation ran BEFORE the earlier argument
/// was evaluated: <c>FUNCTION SUM(A, FUNCTION BUMP(A))</c> summed A's value AFTER BUMP had changed it, although
/// the argument to its left is evaluated first.</para>
///
/// <para><b>The mechanism.</b> A <see cref="Window"/> is opened around one function's argument list. Every argument
/// operand bound inside it is recorded with the pending pre-op range it registered. When a LATER argument
/// registered pre-ops (an activation, an inline invocation, a function-bearing subscript store) every earlier
/// argument whose value can still change is MATERIALIZED into its own intermediate result item by the ONE
/// materializer (<see cref="SendingValueTemp.Materialize"/> — the same §14.9.25.4 GR1 temp a MOVE sender gets),
/// and the store is placed on the pending list at the position its argument finished binding — i.e. before the
/// later argument's pre-ops. The list order IS the evaluation order, so no argument can observe a later one.</para>
///
/// <para><b>What is not frozen, and why.</b> A literal or figurative constant cannot change
/// (<see cref="SendingValueTemp.Materialize"/> answers null). A compiler temporary — a user function's result, an
/// earlier snapshot, an object-property or inline-invocation value — is written once, at its own position in the
/// list, and no later argument writes it. A BY REFERENCE identifier argument of a user function designates its
/// storage, and §8.4.3.2.4 GR6a says "the values of argument-1 are made available to the activated function at
/// the time control is transferred" — the caller of <see cref="Window.Freeze"/> never offers it. A BOOLEAN-EXPRESSION
/// argument is stored by COMPUTE Format 2's own store into a boolean item of §8.8.2 rule 10's length
/// (<see cref="SendingValueTemp.MaterializeBoolean"/>); the one shape left alone is a boolean expression whose
/// length is a run-time value, which no intermediate item can describe.</para>
/// </summary>
internal sealed class ArgumentOrder(BinderContext ctx, SendingValueTemp temps)
{
    private readonly Stack<Window> _open = new();

    private List<BoundStatement> Pending => ctx.Data.PendingPreOps;

    /// <summary>The pending pre-op count now — the position a pre-op registered next would take.</summary>
    internal int Mark => Pending.Count;

    /// <summary>Open the window of one function's argument list. Windows nest like the function references do (an
    /// argument may itself be a function-identifier); dispose the window when the argument list is done.</summary>
    internal Window Open()
    {
        var window = new Window(this);
        _open.Push(window);
        return window;
    }

    /// <summary>Record an argument operand just bound in the innermost open window, with the pending count from
    /// before it bound (<paramref name="before"/>) to now. Outside any window — nothing evaluates arguments — it is
    /// ignored.</summary>
    internal void Record(BoundOperand operand, int before)
    {
        if (_open.Count > 0) _open.Peek().Entries.Add(new Entry(operand, before, Mark));
    }

    internal readonly record struct Entry(BoundOperand Operand, int Before, int After);

    /// <summary>One function's argument list. See <see cref="ArgumentOrder"/>.</summary>
    internal sealed class Window(ArgumentOrder owner) : IDisposable
    {
        internal List<Entry> Entries { get; } = [];

        public void Dispose()
        {
            if (owner._open.Count == 0 || !ReferenceEquals(owner._open.Peek(), this))
                throw new InvalidOperationException("ArgumentOrder windows must close in the reverse order they opened");
            owner._open.Pop();
        }

        /// <summary>Settle the window: for every operand in <paramref name="candidates"/> that a later argument's
        /// pre-ops would otherwise overtake, store its value in an intermediate result item at the position its
        /// argument finished binding, and return the replacement operand (keyed by REFERENCE to the operand as
        /// bound). Operands that need no freeze are absent from the result. The caller passes the operands whose
        /// value is read at the activation — in argument order — and swaps the replacements in.</summary>
        internal Dictionary<BoundOperand, BoundOperand> Freeze(IEnumerable<BoundOperand> candidates)
        {
            var frozen = new Dictionary<BoundOperand, BoundOperand>(ReferenceEqualityComparer.Instance);
            var wanted = new HashSet<BoundOperand>(candidates, ReferenceEqualityComparer.Instance);
            if (wanted.Count == 0) return frozen;
            // needs[i]: some LATER argument registered pre-ops, so entry i would be read after them.
            var needs = new bool[Entries.Count];
            bool later = false;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                needs[i] = later;
                later |= Entries[i].After > Entries[i].Before;
            }
            int shift = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                var operand = Entries[i].Operand;
                if (!needs[i] || !wanted.Contains(operand) || IsStable(operand)) continue;
                if (owner.StoreAt(operand, Entries[i].After + shift, out int added) is not { } replacement) continue;
                shift += added;
                frozen[operand] = replacement;
            }
            return frozen;
        }

        /// <summary>The intrinsic form: every argument of the bound call settles, and the call is returned with the
        /// frozen operands swapped in (the argument-class screens already ran on the operands as written).</summary>
        internal BoundExpr Settle(BoundExpr result)
        {
            if (result is not BoundIntrinsicCall call) return result;
            var frozen = Freeze(call.Args);
            if (frozen.Count == 0) return result;
            return call with { Args = [.. call.Args.Select(a => frozen.GetValueOrDefault(a) ?? a)] };
        }

        private static bool IsStable(BoundOperand operand) => operand is BoundFieldOperand { Place: var place }
            && RootOf(place.Item).IsCompilerTemp;

        private static DataItem RootOf(DataItem item)
        {
            var root = item;
            while (root.Parent is { } p) root = p;
            return root;
        }
    }

    /// <summary>Materialize <paramref name="operand"/> and move the registered pre-ops (the extent freeze, when
    /// there is one, and the store itself) from the end of the pending list to <paramref name="index"/>.</summary>
    private BoundFieldOperand? StoreAt(BoundOperand operand, int index, out int added)
    {
        var pending = Pending;
        int start = pending.Count;
        added = 0;
        // A boolean-expression argument has its own store (COMPUTE Format 2's) and its own length (§8.8.2 rule 10); a
        // run-time length is the one shape no intermediate item can describe, and stays where it was.
        var held = operand is BoundBoolOperand { Expr: var boolean }
            ? ConditionBinder.BoolResultLength(boolean) is int positions and > 0
                ? temps.MaterializeBoolean(boolean, positions, "fnarg")
                : null
            : temps.Materialize(operand, "fnarg");
        if (held is not { } place) return null;
        var steps = pending.GetRange(start, pending.Count - start);
        pending.RemoveRange(start, steps.Count);
        pending.InsertRange(Math.Min(index, pending.Count), steps);
        added = steps.Count;
        return new BoundFieldOperand(place);
    }
}
