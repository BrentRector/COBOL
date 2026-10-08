// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ <b>THE OBJECT OF AN OBJECT PROPERTY WHEN A WORD CANNOT SPELL IT</b> (kb/Work PB1425, PB1783) — the procedure
/// binder's half of §8.4.3.1.2 Format 7 (<c>property-name-1 OF identifier-3</c>), reached through the resolver's
/// <c>BindPropertyObject</c> edge for the <c>propertyObject</c> suffix: SELF, SUPER and <c>object-class-name-1 OF
/// SUPER</c> (§8.4.3.8.3 SR3 names "the object in an object-property identifier" as one of SUPER's two homes), an
/// object-view, a function-identifier, NULL. The word chains (a qualified or subscripted data name, a chained property)
/// never reach here: the resolver reads them itself.
/// <para>SELF and SUPER select the accessor exactly as an INVOKE through them selects a method — the ONE
/// <see cref="OoPredefinedSearchRoot"/>, so SR1 (a method only), SR4 (the qualifier names the INHERITS class) and GR3 / GR4
/// (where the search starts) are asked once for both — and the accessor is invoked on the current object
/// (<see cref="InvokeForm.Self"/> / <see cref="InvokeForm.Super"/>: <c>this</c> / <c>base</c>), the object "used to invoke
/// the method" (§8.4.3.8.4 GR1). A view or a function is bound by its own binder to the temporary it references, which
/// is then an ordinary instance receiver.</para>
/// </summary>
internal sealed partial class OoBinder
{
    /// <summary>Bind <paramref name="po"/>'s object as a property receiver (see the class summary); null having reported,
    /// or — under a resolver <paramref name="probe"/> — null whenever the answer would need a side effect (a function's
    /// activation) or a diagnostic.</summary>
    internal ReferenceResolver.PropertyReceiver? OoBindPropertyObject(Core.PropertyObjectContext po,
        Core.CobolWordContext? superQualifier, bool probe)
    {
        if (po.SELF() is not null || po.SUPER() is not null)
        {
            bool isSuper = po.SUPER() is not null;
            if (OoPredefinedSearchRoot(isSuper, superQualifier, "the object-property object", report: !probe) is not { } root)
                return null;
            // §14.9.23.3 SR4 f)–i): a factory method's SELF / SUPER resolve over the FACTORY interface.
            return new(isSuper ? InvokeForm.Super : InvokeForm.Self, null, root.SearchRoot, null, host.OoInFactory);
        }
        if (po.predefinedNull() is not null)
        {
            // §8.4.3.9.3 SR2: "Identifier-1 shall be an object reference; neither a universal object reference nor the
            // predefined object reference NULL shall be specified."
            if (!probe)
                ctx.Edition.Error(DiagnosticCatalog.PropertyObject,
                    $"'{DataBinder.WrittenText(po.Parent.Parent as Antlr4.Runtime.ParserRuleContext ?? po)}': its object "
                    + "shall not be the predefined object reference NULL (ISO §8.4.3.9.3 SR2)");
            return null;
        }
        if (probe)
            return po.objectView() is { } pv ? DescribedByView(pv)
                : po.functionCall() is { } fc ? DescribedByFunction(fc) : null;

        BoundExpr bound = po.objectView() is { } ov ? OoBindObjectView(ov) : host.Intrinsic.BindIntrinsic(ReferenceResolver.WithoutResultRefMods(po).functionCall()!);
        return OoObjectReferenceTemporary(bound, po, DiagnosticCatalog.PropertyObject.Code,
                "identifier-1 of an object property shall be an object reference (ISO §8.4.3.9.3 SR2)") is { } temp
            ? new(InvokeForm.Instance, temp, null, null, false)
            : null;

        // A probe's reading of a view: the roster its LAST phrase describes, looked up without a diagnostic.
        ReferenceResolver.PropertyReceiver? DescribedByView(Core.ObjectViewContext view)
        {
            var last = view.objectViewPhrase()[^1];
            if (last.UNIVERSAL() is not null) return null;
            var found = OoNameResolution.Lookup(host.OoClasses, last, last.className().GetText(), OoNameResolution.Want.Either);
            return found.Ok ? new(InvokeForm.Instance, null, found.Class, found.Interface, last.FACTORY() is not null) : null;
        }

        // A probe's reading of a function-identifier (kb/Work PB2073): activating it is a side effect, so the probe takes
        // the description its result temporary WOULD carry — §8.4.3.2.4 GR1: "the description, class, and category of
        // the temporary data item is that specified by the description in the linkage section of the item specified in
        // the RETURNING phrase" of the function (prototype) — and the resolver reads the roster off it, the same fact
        // the committing arm reads off the bound temporary. An intrinsic function, or one that returns no object
        // reference, answers null: it is no property's object.
        ReferenceResolver.PropertyReceiver? DescribedByFunction(Core.FunctionCallContext fc) =>
            host.Intrinsic.ReturningItemOf(fc) is { Pic.Category: PicCategory.ObjectReference } item
                ? new(InvokeForm.Instance, null, null, null, false, item)
                : null;
    }
}
