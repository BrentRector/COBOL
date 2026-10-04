// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE ONE §8.4.3.11.3 OPERAND SCREEN of a data-address-identifier (<c>ADDRESS OF identifier-1</c>, ISO §8.4.3.11) —
/// every surface that takes the identifier (the SET Format-7 sender, the CALL and INVOKE argument, the relation
/// operand) reaches it through <c>PtrBinder.BindDataAddress</c>, so each prohibited operand is refused ONCE, with the
/// rule it breaks, and a new surface cannot forget one (kb/Work PB1407, PB1062). Before it, the binder checked only
/// that the operand's record was cell-backed, and every one of these shapes yielded a live pointer: to an object
/// reference, into a strongly-typed record, into a CONSTANT RECORD (a BASED view could then write the constant), to
/// a dynamic-length string, and into an object's own working storage.
/// <para>One arm per syntax rule, each on the RESOLVED operand — what the item IS, never how it was spelled:
/// <list type="bullet">
///   <item>SR1 (second sentence) — "shall not be defined in the working-storage or file section of an object or a
///         factory object" → <see cref="DiagnosticCatalog.AddressOfObjectData"/>. (Its first sentence, the four
///         admitted sections, is the resolver's: a SCREEN SECTION or REPORT SECTION entry, a constant-name and a
///         special register identify no data item here.)</item>
///   <item>SR2 — an object reference, or an elementary item subordinate to a strongly-typed group item;
///         SR3 — a CONSTANT RECORD item or one subordinate to it; SR6 — a dynamic-length elementary item, an element
///         of or item under a dynamic-capacity table, an item under a group that contains a dynamic-length
///         elementary item → <see cref="DiagnosticCatalog.AddressOfOperandKind"/>.</item>
///   <item>SR4 — a bit item must have literal subscripts and reference-modification positions and sit on a byte
///         boundary → <see cref="DiagnosticCatalog.AddressOfBitAlignment"/>, through the ONE static-start walk
///         CALL, INVOKE and the function-identifier already spend (<see cref="ParameterConformance.BitStartOf"/>).</item>
/// </list>
/// SR5 (the identifier is never a receiving operand) is structural: the grammar offers
/// <c>dataAddressIdentifier</c> only in sending positions.</para>
/// </summary>
internal sealed class AddressOfOperandScreen(BinderContext ctx)
{
    /// <summary>Screen the resolved operand; every violated rule is reported, and false is returned when any was.</summary>
    /// <param name="written">The reference as written, for the message text.</param>
    /// <param name="operand">What <c>ReferenceResolver.ResolveForAddressOf</c> resolved it to.</param>
    public bool Admit(Core.DataReferenceContext written, ReferenceResolver.AddressOfOperand operand)
    {
        var item = operand.Item;
        string text = DataBinder.WrittenText(written);
        bool ok = true;

        // SR1, second sentence.
        if (ctx.Data.OoIsObjectData(item))
        {
            Report(DiagnosticCatalog.AddressOfObjectData, text,
                "is defined in the working-storage or file section of an object or a factory object, so it shall not "
                + "be the identifier-1 of ADDRESS OF (ISO §8.4.3.11.3 SR1)");
            ok = false;
        }

        // SR2, both halves.
        if (item.Pic?.Category is PicCategory.ObjectReference)
        {
            Report(DiagnosticCatalog.AddressOfOperandKind, text,
                "is an object reference, so it shall not be the identifier-1 of ADDRESS OF (ISO §8.4.3.11.3 SR2)");
            ok = false;
        }
        else if (!item.IsGroup && StrongTypeModel.StrongRoot(item) is { } strong && !ReferenceEquals(strong, item))
        {
            Report(DiagnosticCatalog.AddressOfOperandKind, text,
                $"is an elementary item subordinate to the strongly-typed group item '{strong.CobolName}', so it shall "
                + "not be the identifier-1 of ADDRESS OF — the address of the group is the restricted data-pointer "
                + "§8.4.3.11.4 GR2 defines (ISO §8.4.3.11.3 SR2)");
            ok = false;
        }

        // SR3.
        if (ctx.Data.IsConstantRecordItem(item))
        {
            Report(DiagnosticCatalog.AddressOfOperandKind, text,
                "is described with, or is subordinate to an item described with, the CONSTANT RECORD clause, so it "
                + "shall not be the identifier-1 of ADDRESS OF — a pointer could write the constant (ISO §8.4.3.11.3 SR3)");
            ok = false;
        }

        // SR6.
        if (DynamicCapacityKind(item) is { } kind)
        {
            Report(DiagnosticCatalog.AddressOfOperandKind, text,
                $"is {kind}, so it shall not be the identifier-1 of ADDRESS OF (ISO §8.4.3.11.3 SR6)");
            ok = false;
        }

        // SR4 — a bit item. A shape the walk cannot model is accepted: the screen never rejects legal source it
        // cannot prove misaligned.
        if (BitLayout.IsBitItem(item) && operand.SubscriptedPlace is { } place)
        {
            Place proof = operand.RefMod is { } rm ? new RefModPlace(place, rm.Start, rm.Length) : place;
            var start = ParameterConformance.BitStartOf(proof);
            switch (start.Fault)
            {
                case ParameterConformance.BitStartFault.NonLiteralRefMod:
                    Report(DiagnosticCatalog.AddressOfBitAlignment, text,
                        "is a bit item whose reference-modification leftmost position is not a fixed-point numeric "
                        + "literal or an arithmetic expression of such literals without exponentiation (ISO §8.4.3.11.3 SR4 a))");
                    ok = false;
                    break;
                case ParameterConformance.BitStartFault.NonLiteralSubscript:
                    Report(DiagnosticCatalog.AddressOfBitAlignment, text,
                        "is a bit item whose subscripts are not fixed-point numeric literals or arithmetic "
                        + "expressions of such literals without exponentiation (ISO §8.4.3.11.3 SR4 a))");
                    ok = false;
                    break;
                default:
                    if (start.Bits is { } bits && bits % BitLayout.BitsPerCharacter != 0)
                    {
                        Report(DiagnosticCatalog.AddressOfBitAlignment, text,
                            $"is a bit item that starts at bit {bits} of its record, so it is not aligned on a byte "
                            + "boundary (ISO §8.4.3.11.3 SR4 b) and §8.5.1.6.3)");
                        ok = false;
                    }
                    break;
            }
        }
        return ok;
    }

    /// <summary>What §8.4.3.11.3 SR6 calls <paramref name="item"/> when it forbids its address — a dynamic-length
    /// elementary item, an element of a dynamic-capacity table, an item subordinate to one, or an item subordinate
    /// to a group that contains a dynamic-length elementary item — or null. Read literally: the 01 group that
    /// CONTAINS a dynamic-length item is itself none of the four (it is not subordinate to anything), and the rule
    /// names the dynamic-length ELEMENTARY item, so a group holding only a dynamic-capacity table beside other
    /// items does not bar those siblings.</summary>
    private static string? DynamicCapacityKind(DataItem item)
    {
        if (item.IsDynamicLength) return "a dynamic-length elementary item";
        if (item.IsDynamicTable) return "an element of a dynamic-capacity table";
        for (var up = item.Parent; up is not null; up = up.Parent)
        {
            if (up.IsDynamicTable) return "an item subordinate to a dynamic-capacity table";
            if (ContainsDynamicLengthElementary(up))
                return $"an item subordinate to the group '{up.CobolName}', which contains a dynamic-length elementary item";
        }
        return null;
    }

    private static bool ContainsDynamicLengthElementary(DataItem group)
    {
        foreach (var child in group.Children)
            if (child.IsDynamicLength || (child.IsGroup && ContainsDynamicLengthElementary(child))) return true;
        return false;
    }

    private void Report(DiagnosticDescriptor code, string operandText, string what) =>
        ctx.Edition.Error(code, $"ADDRESS OF '{operandText}': the operand {what}");
}
