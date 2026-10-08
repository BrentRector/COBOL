// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Binding.Model;

namespace CobolNet.CodeGen;

/// <summary>
/// ⛔ THE ONE RENDERING OF A <see cref="Position"/> (kb/Work PB2151; DESIGN-binder-bound-tree.md §3.9.3) — a typed
/// subscript, reference-modifier position, offset or ordinal to the C# <c>long</c> expression that computes it. Every
/// reader of a position field renders through here, so the text of a position is decided in exactly one place.
/// <para>Only the code generator calls it: no binder code renders a position, and no carrier of the place model
/// holds position text (<c>PositionCarrierDriftTests</c>).</para>
/// </summary>
internal static class PositionRenderer
{
    /// <summary>The C# expression of <paramref name="p"/>.</summary>
    public static string Render(Position p) => p switch
    {
        PositionConstant c => c.Value.ToString(CultureInfo.InvariantCulture),
        PositionIndexCell ix => ix.Cell,
        PositionItemRead r => r.Form switch
        {
            PositionReadForm.Occurrence => RuntimeApi.TableOcc(PlaceRenderer.RenderPath(r.Path, AccessDir.Sending), r.Item.ProfileName),
            PositionReadForm.RefModPosition => RuntimeApi.StrRefModPosition(PlaceRenderer.RenderPath(r.Path, AccessDir.Sending), r.Item.ProfileName),
            PositionReadForm.Digits => RuntimeApi.TableOcc(PlaceRenderer.RenderPath(r.Path, AccessDir.Sending), null),
            _ => throw new System.InvalidOperationException($"PositionRenderer has no arm for read form {r.Form}"),
        },
        PositionLocal l => l.AsLong ? $"(long){l.CsName}" : l.CsName,
        PositionLocalElement e => $"{e.Vector.CsName}[{e.Index.ToString(CultureInfo.InvariantCulture)}]",
        PositionBinary b => $"{Render(b.Left)} {Operator(b.Op)} {Render(b.Right)}",
        PositionNegate n => $"- {Render(n.Operand)}",
        PositionGroup g => $"({Render(g.Inner)})",
        PositionPointerOffset ptr => RuntimeApi.PtrOffsetOf(ptr.PointerField),
        PositionPointerDynBase ptr => RuntimeApi.PtrDynBaseOf(ptr.PointerField),
        PositionOffset o => o.Terms.Aggregate(Render(o.Origin),
            (text, t) => $"{text} + ({Render(t.Index)} - 1) * {t.Stride.ToString(CultureInfo.InvariantCulture)}"),
        _ => throw new System.InvalidOperationException($"PositionRenderer has no arm for {p.GetType().Name}"),
    };

    /// <summary>The one-based occurrence <c>(local + 1)</c> of a zero-based loop local — the code generator's own
    /// enumerations (a table SORT's element loop) address the table through it.</summary>
    public static Position OneBased(string zeroBasedLocal) =>
        new PositionGroup(new PositionBinary(new PositionLocal(zeroBasedLocal), PositionOperator.Add, new PositionConstant(1)));

    private static string Operator(PositionOperator op) => op switch
    {
        PositionOperator.Add => "+",
        PositionOperator.Subtract => "-",
        PositionOperator.Multiply => "*",
        _ => throw new System.InvalidOperationException($"PositionRenderer has no arm for operator {op}"),
    };
}
