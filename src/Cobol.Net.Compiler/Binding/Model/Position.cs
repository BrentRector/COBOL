// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE TYPED POSITION CARRIER (D10's second half, kb/Work PB2151; docs/rearchitecture/DESIGN-binder-bound-tree.md
/// §3.9): an integer-valued run-time address term — a subscript (ISO §8.4.2.3.2 <c>arithmetic-expression-1</c>), a
/// reference-modifier leftmost position or length (§8.4.3.3.3 SR4), and the offsets and ordinals the place model
/// computes from them. The binder builds STRUCTURE and the code generator renders it
/// (<c>CodeGen.PositionRenderer</c>); no position travels between the two as C# text.
/// <para>The node set is the set of shapes that render DIRECTLY as an ordinal. Every other arithmetic expression (a
/// function-identifier, <c>/</c>, <c>**</c>, a decimal literal, a scaled operand inside a compound, the figurative
/// ZERO) is bound by the ONE expression binder into the §15.4 position temporary and is then a
/// <see cref="PositionItemRead"/> of that temporary, so §8.4.2.3.4 GR1 b)'s integrality rule is asked once, of the
/// result.</para>
/// </summary>
public abstract record Position
{
    /// <summary>The value when this position is a single integer constant within the host's <c>int</c> — the
    /// structural "is it a literal" question a static reader asks (a slice's extent, a conformance proof); null for
    /// an expression, even one built only from constants.</summary>
    public int? Int32Literal => this is PositionConstant { Value: >= int.MinValue and <= int.MaxValue } c ? (int)c.Value : null;

    /// <summary>The compile-time value of a position built only from constants (<c>+ - *</c>, a unary minus,
    /// parentheses); null when any term is read at run time.</summary>
    public long? ConstantValue => this switch
    {
        PositionConstant c => c.Value,
        PositionGroup g => g.Inner.ConstantValue,
        PositionNegate n => -n.Operand.ConstantValue,
        PositionBinary { Left.ConstantValue: { } l, Right.ConstantValue: { } r } b => b.Op switch
        {
            PositionOperator.Add => l + r,
            PositionOperator.Subtract => l - r,
            _ => l * r,
        },
        PositionOffset o => o.Terms.Aggregate(o.Origin.ConstantValue,
            (sum, t) => sum is { } s && t.Index.ConstantValue is { } i ? s + (i - 1) * t.Stride : null),
        _ => null,
    };

    /// <summary>True when this position evaluates to itself at any time — a non-negative integer constant, or an offset
    /// with no subscript term over one. The structural test the §14.6.4 7) evaluate-once identification asks before it
    /// hoists a position into a statement local (<c>CodeGen.PlaceIdentification</c>); a term over a constant index
    /// is still a term, so the identification keeps the address it would have computed.</summary>
    public bool IsSelfEvaluating => this switch
    {
        PositionConstant { Value: >= 0 } => true,
        PositionOffset { Terms.Count: 0 } o => o.Origin.IsSelfEvaluating,
        _ => false,
    };

    /// <summary>⛔ TRUE WHEN THIS DIRECTLY RENDERED POSITION CANNOT BE EVALUATED IN THE HOST'S <c>long</c> (kb/Work
    /// PB2616). <c>CodeGen.PositionRenderer</c> renders <c>+ - *</c> as C# <c>long</c> arithmetic, but §8.4.2.3.4
    /// 1) b) makes the subscript "the result of the evaluation of arithmetic-expression-1" — the expression's value,
    /// not a 64-bit machine product that wraps — so an expression whose worst case does not fit the host's
    /// <c>long</c> is not a shape this carrier may render. The worst case is a COMPILE-TIME digit bound: a
    /// literal is its own magnitude, an item read is the largest value its PICTURE can hold
    /// (<c>10^digits − 1</c>, widened by trailing P positions), and an operator composes its operands' bounds
    /// (<c>+ -</c> add them, <c>*</c> multiplies them). Narrow operands (the ordinary <c>T(I + 1)</c>) keep the
    /// <c>long</c> fast path; a position that answers true is bound through the ONE expression binder instead
    /// (<c>ReferenceResolver.MaterializePosition</c>), whose intermediate rule is the arithmetic statements'
    /// (§8.8.1.3 native arithmetic, kb/Work PB1900) and whose result then meets the §8.4.2.3.4 bound check.
    /// <para>A bare leaf never answers true: an item read is a single <c>CobolTable.Occ</c> and does no arithmetic. Every
    /// operator node answers for itself, and a leaf that is itself past <c>long</c> (a 19–38 digit item) makes its
    /// parent operator exceed, since a saturated read is not the value.</para></summary>
    public bool ExceedsLongArithmetic => Bound(this, out _);

    /// <summary>The worst-case magnitude of <paramref name="p"/> in an <see cref="Int128"/> that saturates, and
    /// whether an operator node beneath it (or it) exceeds the host's <c>long</c>.</summary>
    private static bool Bound(Position p, out Int128 bound)
    {
        switch (p)
        {
            case PositionConstant c:
                bound = Int128.Abs(c.Value);
                return false;
            case PositionItemRead { Item.Pic: { } pic } read:
                bound = DigitsBound(read.Form == PositionReadForm.Digits ? pic.Length : pic.Digits + pic.TrailingPScaling);
                return false;
            case PositionGroup g:
                return Bound(g.Inner, out bound);
            case PositionNegate n:
                return Bound(n.Operand, out bound);
            case PositionBinary b:
                bool exceeds = Bound(b.Left, out Int128 l) | Bound(b.Right, out Int128 r);
                bound = b.Op == PositionOperator.Multiply
                    ? (l == 0 || r == 0 ? 0 : r > Int128.MaxValue / l ? Int128.MaxValue : l * r)
                    : l > Int128.MaxValue - r ? Int128.MaxValue : l + r;
                return exceeds || bound > long.MaxValue;
            case PositionIndexCell or PositionLocal or PositionLocalElement:
                // An index cell, a statement local and a table(ALL) vector element are occurrence numbers and loop
                // counters: bounded by the host's int (a table's extent), never by a PICTURE.
                bound = int.MaxValue;
                return false;
            default:
                // An address term (an offset, a pointer displacement) has no digit bound the walk can state: it is as
                // wide as the host's long, so any operator over it exceeds — never a silent assumption.
                bound = long.MaxValue;
                return false;
        }
    }

    /// <summary><c>10^digits − 1</c>, saturating at <see cref="Int128.MaxValue"/> (a PICTURE is at most 38 digits).</summary>
    private static Int128 DigitsBound(int digits)
    {
        Int128 v = 1;
        for (int i = 0; i < digits && v < Int128.MaxValue / 10; i++) v *= 10;
        return v - 1;
    }
}

/// <summary>A compile-time integer: an integer literal, an integer constant-name's value, a folded term.</summary>
public sealed record PositionConstant(long Value) : Position;

/// <summary>An index-name used as a position: its occurrence cell (<see cref="IndexDeclaration.Cell"/>, a C# member
/// name — the index-name's storage, never an expression).</summary>
public sealed record PositionIndexCell(string Cell) : Position;

/// <summary>How a data item is read as an ordinal position — the overload of the runtime position read.</summary>
public enum PositionReadForm
{
    /// <summary>A numeric item as an occurrence number, through its own profile (scale, sign and byte form):
    /// <c>CobolTable.Occ(x, profile)</c>.</summary>
    Occurrence,

    /// <summary>A numeric item as a reference-modifier position, through its own profile:
    /// <c>CobolString.RefModPosition(x, profile)</c>.</summary>
    RefModPosition,

    /// <summary>A NON-numeric item the <c>--permissive</c> lane admits (COBOLNET0844): its digit characters decoded
    /// as an unsigned integer, <c>CobolTable.Occ(x)</c>.</summary>
    Digits,
}

/// <summary>A data item read as an ordinal position (kb/Work PB41: the item's VALUE, never its unscaled storage).
/// <paramref name="Path"/> is the item's structural access path; the profile is <paramref name="Item"/>'s.</summary>
public sealed record PositionItemRead(AccessPath Path, DataItem Item, PositionReadForm Form) : Position;

/// <summary>A statement-local integer: a report VARYING counter (rendered as a <c>long</c> when
/// <paramref name="AsLong"/>), a hoisted item identification (ISO §14.6.4 7), a loop variable.</summary>
public sealed record PositionLocal(string CsName, bool AsLong = false) : Position;

/// <summary>A statement-local integer array named <paramref name="CsName"/> — a table(ALL) enumeration's index vector
/// (ISO §15.3), one current occurrence per ALL level. Not itself a position: each element is one
/// (<see cref="PositionLocalElement"/>), and the enumeration that declares it (<see cref="TableAllPlace"/>) and the
/// subscripts that read it name the SAME vector.</summary>
public sealed record LocalVector(string CsName);

/// <summary>One element of a statement-local integer array — a table(ALL) enumeration's current occurrence of one
/// level (ISO §15.3): <c>Vector[Index]</c>.</summary>
public sealed record PositionLocalElement(LocalVector Vector, int Index) : Position;

/// <summary>The arithmetic operators a directly rendered position composes with.</summary>
public enum PositionOperator { Add, Subtract, Multiply }

/// <summary><paramref name="Left"/> <paramref name="Op"/> <paramref name="Right"/>, in written order.</summary>
public sealed record PositionBinary(Position Left, PositionOperator Op, Position Right) : Position;

/// <summary>A unary minus.</summary>
public sealed record PositionNegate(Position Operand) : Position;

/// <summary>A WRITTEN parenthesis, kept so the rendered order of evaluation is the source's.</summary>
public sealed record PositionGroup(Position Inner) : Position;

/// <summary>A BASED item's data-address pointer displacement (ISO §13.18.5): the character offset at which the pointer
/// named by <paramref name="PointerField"/> (a C# member) currently points into its storage cell, read at run time.</summary>
public sealed record PositionPointerOffset(string PointerField) : Position;

/// <summary>The component-ordinal twin of <see cref="PositionPointerOffset"/> (kb/Work PB2094): the ordinal of the first
/// cell component the pointer named by <paramref name="PointerField"/> (a C# member) currently addresses, read at run
/// time (<c>CellPointer.DynBase</c>). A class laid over storage through its data-address pointer — a BASED item, an
/// AREA formal (§14.2.3 GR8) — numbers its components from it, as its character window is displaced by the offset.</summary>
public sealed record PositionPointerDynBase(string PointerField) : Position;

/// <summary>⛔ THE ONE STRUCTURED FORM OF EVERY ADDRESS THE PLACE MODEL COMPUTES FROM SUBSCRIPTS (kb/Work PB2151;
/// DESIGN-binder-bound-tree.md §3.9.1): <paramref name="Origin"/> + Σ (index − 1) × stride over
/// <paramref name="Terms"/>, outermost level first. A Tier-B view's byte offset (the item's in-class offset plus each
/// in-class OCCURS level's per-occurrence storage width, ISO §13.18.44), its bit twin (§8.5.1.6.3's bit positions,
/// strided by <c>BitLayout.StrideBits</c>), a cell component ordinal (<c>DataItem.ClassDynOrdinal</c> plus each level's
/// components per occurrence) and the ADDRESS OF displacement are all this one node, so the law
/// "(index − 1) × stride" is written once, here, and every producer builds the node instead of concatenating text.
/// <para>The origin is any position: the static in-class offset or ordinal (a <see cref="PositionConstant"/>), that
/// constant displaced by a BASED pointer (<see cref="PositionPointerOffset"/>) or by an enclosing cursor's own offset —
/// an offset is a valid origin, so a member window built from a group window is the group's offset displaced
/// again.</para></summary>
public sealed record PositionOffset(Position Origin, IReadOnlyList<OffsetTerm> Terms) : Position
{
    /// <summary>The offset at the compile-time position <paramref name="origin"/>, displaced by no subscript yet.</summary>
    public static PositionOffset At(long origin) => new(new PositionConstant(origin), []);

    /// <summary>This offset displaced by one more subscript term (an inner OCCURS level).</summary>
    public PositionOffset Plus(Position oneBasedIndex, long stride) => this with { Terms = [.. Terms, new(oneBasedIndex, stride)] };
}

/// <summary>One subscript term of a <see cref="PositionOffset"/>: <c>(Index − 1) × Stride</c>, the displacement of the
/// <paramref name="Index"/>-th occurrence of a level whose occurrences are <paramref name="Stride"/> units apart.</summary>
public readonly record struct OffsetTerm(Position Index, long Stride);
