// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ <b>THE §8.4.3.1.2 FORMAT 5 IDENTIFIER — THE OBJECT-VIEW</b> (kb/Work PB1425): <c>identifier-1 AS { [FACTORY OF]
/// object-class-name-1 [ONLY] | interface-name-1 | UNIVERSAL }</c>, §8.4.3.5.
///
/// <para><b>What it binds to.</b> §8.4.3.5.4 GR1: "This reference of identifier-1 is treated at compile-time as though
/// it had the description specified by the AS phrase." The view is therefore a reference to identifier-1's object
/// UNDER ANOTHER DESCRIPTION, and the binder gives it exactly that: a compiler temporary described by the AS phrase
/// (<c>DataBinder.OoCreateObjectViewTemp</c>), filled by a statement pre-op (<see cref="BoundObjectView"/>) that copies
/// identifier-1's reference and makes GR2–GR6's run-time conformance check. The view is then the temporary, read by
/// the same <see cref="BoundNumRef"/> an inline invocation's result is, so every object-reference position that admits
/// a computed identifier (the INVOKE receiver, the inline receiver, SET Format 5's sender, RAISE) takes a view with no
/// arm of its own — <c>OoBindComputedObjectReference</c> is their one door.</para>
///
/// <para><b>Why a temporary is the reference itself.</b> An object reference holds a reference, never the object
/// (§13.18.60.4 GR22), so a copy of it designates the same object; and §8.4.3.5.3 SR2 forbids the view as a receiving
/// operand, so nothing can write through it. The pre-op runs where every identifier's activation runs — at its place
/// in the statement, before the statement's own action (the <c>PendingPreOps</c> hoist, §8.4.3.1.4 GR1's order).</para>
/// </summary>
internal sealed partial class OoBinder
{
    /// <summary>Bind one object-view and return the expression that READS it — a <see cref="BoundNumRef"/> over the
    /// view's temporary. Each AS phrase applies to the identifier on its left (§8.4.3.1.4 GR1 c)), so a repeated phrase
    /// re-views the previous view.</summary>
    internal BoundExpr OoBindObjectView(Core.ObjectViewContext ov)
    {
        // The INTRODUCTION gate fires on RECOGNITION in the VersionConformancePass parse arm (VisitObjectView).
        var term = ov.objectReferenceTerm();
        string written = DataBinder.WrittenText(term);

        // §8.4.3.5.3 SR1: "Identifier-1 shall be of class object; the predefined object references SUPER and NULL shall
        // not be specified." Both are admitted by the grammar (the P3 superset parse) so they are refused by THIS rule.
        if (term.predefinedNull() is not null || term.selfAndSuper()?.SUPER() is not null)
            return RefuseViewSubject(written, $"the predefined object reference {(term.predefinedNull() is not null ? "NULL" : "SUPER")} "
                + "shall not be specified as identifier-1");

        Place? source = null;   // null = SELF, the object the method was invoked on (§8.4.3.8.4)
        if (term.selfAndSuper() is not null)
        {
            if (!host.InMethod || host.OoCurrentClass is null)
            {
                // The shared §8.4.3.8.3 SR1 refusal reports; the expression channel carries the refusal on.
                _ = RefusePredefinedObjectOutsideMethod($"the object-view '{DataBinder.WrittenText(ov)}': SELF");
                return BoundExprError.Refused(ctx.Edition, "object-view of SELF outside a method");
            }
        }
        else if (OoBindComputedTerm(term) is { } computed)
        {
            // A function-identifier (§8.4.3.1.2 Format 1) is an identifier-1 of class object exactly when the item it
            // references is an object reference (§8.4.3.2.1) — asked of its temporary, named as written.
            if ((source = OoObjectReferenceTemporary(computed, term, DiagnosticCatalog.ObjectViewSubject.Code,
                    "identifier-1 of an object-view shall be of class object (ISO §8.4.3.5.3 SR1)")) is null)
                return BoundExprError.Refused(ctx.Edition, "object-view of a non-object identifier-1");
        }
        else
        {
            var resolved = host.Expr.ResolveSending(term.dataReference()!);
            if (resolved.PlaceOrReported(ctx.Edition) is not { } place)
                return BoundExprError.Refused(ctx.Edition, "object-view of an unresolved identifier-1");
            if (place.Item.Pic is not { Category: PicCategory.ObjectReference })
                return RefuseViewSubject(written, "identifier-1 is not of class object (it is not an object reference)");
            source = place;
        }

        foreach (var phrase in ov.objectViewPhrase())
        {
            string phraseText = DataBinder.WrittenText(phrase);
            ObjectRefDescriptor view = phrase.UNIVERSAL() is not null
                ? ObjectRefDescriptor.Universal   // GR7
                : ctx.Data.OoDescribeNamedReference(phrase, phrase.className().GetText(),
                    factory: phrase.FACTORY() is not null, only: phrase.ONLY() is not null,
                    $"the object-view '{written} {phraseText}'", "ISO §8.4.3.5.2",
                    DiagnosticCatalog.ObjectViewInterfacePhrase, "ISO §8.4.3.5.2; §8.4.3.5.4 GR6");
            if (ctx.Refs.ResolveItem(ctx.Data.OoCreateObjectViewTemp(view)) is not { } temp)
                return BoundExprError.Unbuilt(ctx.Edition, "the object-view's temporary");
            ctx.Data.PendingPreOps.Add(new BoundObjectView(temp, source, view, $"{written} {phraseText}"));
            source = temp;
            written += " " + phraseText;
        }
        return new BoundNumRef(source!);
    }

    /// <summary>§8.4.3.5.3 SR1's refusal, naming identifier-1 as written.</summary>
    private BoundExpr RefuseViewSubject(string written, string why) =>
        BoundExprError.Report(ctx.Edition, DiagnosticCatalog.ObjectViewSubject,
            $"the object-view of '{written}': {why} — identifier-1 shall be of class object; the predefined object "
            + "references SUPER and NULL shall not be specified (ISO §8.4.3.5.3 SR1)", "object-view identifier-1");

    /// <summary>§8.4.3.5.3 SR2 — "An object-view shall not be specified as a receiving operand" — for the receiving
    /// positions whose grammar admits the view as a superset parse (SET Format 5's receivers, INVOKE RETURNING), so
    /// the program is told the rule it broke rather than that AS is a reserved word.</summary>
    internal BoundStatement RefuseObjectViewReceiver(Core.ObjectViewReceiverContext ov, string position) =>
        BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ObjectViewReceiving,
            $"'{DataBinder.WrittenText(ov)}' is written as {position}: an object-view shall not be specified as a "
            + "receiving operand (ISO §8.4.3.5.3 SR2)");
}
