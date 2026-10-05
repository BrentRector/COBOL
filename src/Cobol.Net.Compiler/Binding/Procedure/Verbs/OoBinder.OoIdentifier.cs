// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding.Bound;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ <b>THE OO IDENTIFIER FORMATS AS OPERANDS</b> (kb/Work PB1425, PB1782 step 2) — §8.4.3.1.2 Format 4 (the inline
/// method invocation), Format 5 (the object-view) and Format 6's SELF / [object-class-name-1 OF] SUPER, written where
/// the expression tier writes an identifier: a relation operand (§8.8.4.2.15, "An operand of class object may be
/// compared with another operand of class object"), an argument, a SET sender. §8.4.3.1.3 SR1 makes every such slot
/// "any of the formats for an identifier", so the grammar's `primaryExpression` carries all three and every consumer of
/// a SOLE identifier asks ONE door, <see cref="OoBindOoIdentifier"/>, which binds the format to the expression that
/// reads the temporary item it references.
/// <para>Each format's own position rules are asked HERE, once: §8.4.3.8.3 SR1 (SELF and SUPER only in a method
/// definition), SR3 (SUPER only as the object an invocation or an object property applies to — never an operand of
/// its own), §8.4.3.5.3 SR1 (inside <see cref="OoBindObjectView"/>). The object-reference positions that also admit
/// SUPER (the INVOKE receiver, the inline receiver) bind it through <c>OoBindByReceiver</c>, never through here.</para>
/// </summary>
internal sealed partial class OoBinder
{
    /// <summary>Bind one OO-format identifier operand — an inline invocation, an object-view or SELF / SUPER — to the
    /// expression that reads the temporary it references.</summary>
    internal BoundExpr OoBindOoIdentifier(ParserRuleContext id) => id switch
    {
        Core.InlineMethodInvocationContext imi => OoBindInlineInvocation(imi),
        Core.ObjectViewContext ov => OoBindObjectView(ov),
        Core.SelfAndSuperContext s => OoBindSelfOperand(s),
        _ => throw new InvalidOperationException(
            $"OoBindOoIdentifier serves ISO §8.4.3.1.2 Formats 4, 5 and 6 only, not {id.GetType().Name}"),
    };

    /// <summary><see cref="OoBindOoIdentifier"/> as an operand — the ONE expression→operand mapping
    /// (<see cref="IntrinsicBinder.OperandOf"/>) turns the temporary into a field operand, so a relation's class
    /// dispatch reads the item's class object (§8.8.4.2.15).</summary>
    internal BoundOperand OoIdentifierOperand(ParserRuleContext id) => IntrinsicBinder.OperandOf(OoBindOoIdentifier(id));

    /// <summary>SELF or SUPER written as an operand. SELF binds to the temporary <see cref="BoundSelfReference"/> fills
    /// (§8.4.3.8.4 GR1: "SELF and SUPER both reference the object that was used to invoke the method"); SUPER is
    /// refused by §8.4.3.8.3 SR3, which admits it only where it selects a method search (the INVOKE receiver, the
    /// inline receiver, the object of an object property).</summary>
    private BoundExpr OoBindSelfOperand(Core.SelfAndSuperContext s)
    {
        string written = DataBinder.WrittenText(s);
        if (s.SUPER() is not null)
            return BoundExprError.Report(ctx.Edition, DiagnosticCatalog.SuperPosition,
                $"'{written}' is written as an operand: SUPER may be specified only as the object in an object-property "
                + "identifier or as the object used to invoke a method (ISO §8.4.3.8.3 SR3)", "SUPER as an operand");
        if (!host.InMethod || host.OoCurrentClass is not { } cur)
        {
            _ = RefusePredefinedObjectOutsideMethod(written);
            return BoundExprError.Refused(ctx.Edition, "SELF outside a method");
        }
        if (ctx.Refs.ResolveItem(ctx.Data.OoCreateSelfTemp(cur.Name, host.OoInFactory)) is not { } temp)
            return BoundExprError.Unbuilt(ctx.Edition, "SELF's temporary");
        ctx.Data.PendingPreOps.Add(new BoundSelfReference(temp));
        return new BoundNumRef(temp);
    }
}
