// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// ⛔ <b>§8.4.3.1.2 FORMAT 7 — <c>property-name-1 OF identifier-3</c> — RESOLVED BY THE ORDINARY IDENTIFIER RESOLVER</b>
/// (kb/Work PB1425, PB1783). §8.4.3.1.3 SR1 makes identifier-3 "any of the formats for an identifier", and §8.4.3.1.4
/// GR1 fixes the order the components apply in: a) a qualified data name with its subscripts and the predefined object
/// references, c) an object-view, d) "OF for object properties applies the property-name on the left to the identifier on
/// the right", g) a reference modifier last. A property reference is therefore a qualifier chain whose head word is the
/// property-name and whose remainder is identifier-3 — a qualified, subscripted data name, a chained property, or a
/// chain ending in a non-word object (<c>propertyObject</c>: SELF, SUPER, a view, a function, NULL).
/// <para><b>The split is the symbols'.</b> Textually the reference is a qualified data reference, so it is a property
/// reference exactly when ordinary qualification fails. Identifier-3 is then the written reference less its head word
/// and its reference modifiers (<see cref="PropertyObjectReference"/>), resolved by this same resolver — so every format
/// the resolver knows, and every one it learns later, is an identifier-3 with no arm of its own. It used to be a
/// one-word special case (<c>qualifiers.Count != 1</c> → not a property), which named the compiler's temporary when the
/// object was subscripted and refused every other form.</para>
/// <para><b>The accessor is selected by identifier-3's description</b> (§8.4.3.9.3 SR3/SR4 — "in the object referenced
/// by identifier-1 or in the factory object of the object class object-class-name-1"): a typed reference's class or
/// interface, FACTORY OF's factory half, a class-name's factory, and for SELF / [object-class-name-1 OF] SUPER the same
/// method-search root an INVOKE through them uses (§8.4.3.8.4), so the accessor is invoked on <c>this</c> / <c>base</c>.</para>
/// </summary>
public sealed partial class ReferenceResolver
{
    /// <summary>What the object of an object property selects: the accessors' invocation form, the receiver place for
    /// the instance form, and the roster the accessors are looked up on — a class (its instance or, with
    /// <paramref name="Factory"/>, its factory half) or an interface. A computed object (a view, a function) arrives as an
    /// instance <paramref name="Receiver"/> with no roster, and the roster is read from the receiver's description.</summary>
    internal readonly record struct PropertyReceiver(InvokeForm Form, Place? Receiver, OoClassSymbol? Class,
        OoInterfaceSymbol? Interface, bool Factory);

    /// <summary>The procedure binder's reading of a non-word object (<c>propertyObject</c>) — SELF, SUPER (with its
    /// <c>object-class-name-1</c> qualifier when the chain spelled one), an object-view, a function-identifier, NULL —
    /// as a property's receiver; null having reported, or (a PROBE, the bool) null when it cannot be told without side
    /// effects. The same one-way delegate edge as <see cref="MaterializeSegment"/>: a data-division resolver has none,
    /// and those objects exist only in a method's procedure division.</summary>
    internal Func<Core.PropertyObjectContext, Core.CobolWordContext?, bool, PropertyReceiver?>? BindPropertyObject { get; set; }

    /// <summary>The procedure binder's object-view (§8.4.3.5) over an already resolved identifier-1 — for
    /// <c>A OF G AS C</c>, a view of the qualified data item <c>A OF G</c> (§8.4.3.1.4 GR1 a) before c)) that the grammar
    /// spells as a property chain (<see cref="ViewOfQualifiedItem"/>). Null having reported.</summary>
    internal Func<Place, Core.ObjectViewContext, string, Place?>? ApplyObjectView { get; set; }

    /// <summary>True when <paramref name="dref"/> is an OBJECT-PROPERTY reference (§8.4.3.9.2) rather than an ordinary
    /// data reference. The ONE such test, over the SAME <see cref="ResolveObjectProperty"/> the committing path runs, so
    /// the two can never disagree about what a property reference is.
    /// <para>Its callers are §14.9.4.3 SR20's carve-out (kb/Work PB238): "BY CONTENT shall not be omitted when
    /// identifier-4 is an identifier that is permitted as a receiving operand, EXCEPT that BY CONTENT may be omitted when
    /// identifier-4 is an object property" — and §8.4.3.2.4 GR5 a)'s "other than an object property".</para>
    /// <para>⛔ PURE — it runs the resolution under <c>_probing</c>, so no temp is synthesized, no pending §8.4.3.9.4 op
    /// is registered and no diagnostic is reported (the R30 probe contract). The ordinary-qualification test comes FIRST
    /// for the same reason the committing path orders it that way.</para></summary>
    public bool IsObjectPropertyReference(Core.DataReferenceContext dref)
    {
        if (dref.cobolWord() is not { } head) return false;
        var written = ReadWritten(dref);
        if (written.PropertyObject is null
            && (written.Qualifiers.Count == 0 || ResolveQualified(head.Name(), written.Qualifiers) is not null))
            return false;
        bool saved = _probing;
        _probing = true;
        try { return ViewOfQualifiedItem(dref, report: false) is null && ResolveObjectProperty(dref, head.Name(), written).Temp is not null; }
        finally { _probing = saved; }
    }

    /// <summary>The <c>object-class-name-1</c> of a written <c>object-class-name-1 OF SUPER</c> (§8.4.3.8.2), or null.
    /// The grammar spells it as the chain <c>name OF SUPER</c>, which is equally the property <c>name</c> of SUPER
    /// (§8.4.3.1.2 Format 7); only the declared names tell them apart, and this is the ONE place that does: a name that
    /// is a class-name this source element may reference (§8.4.6.4) is the qualified SUPER.</summary>
    internal Core.CobolWordContext? QualifiedSuperClass(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix() is [var only] && only.propertyObject()?.SUPER() is not null
        && dref.cobolWord() is { } word && IsClassName(dref, word.Name())
            ? word : null;

    private bool IsClassName(RuleContext site, string name) =>
        data.OoClasses is { } table && OoNameResolution.Lookup(table, site, name, OoNameResolution.Want.Class).Class is not null;

    /// <summary>True when <paramref name="dref"/> is the object-view of a qualified data item written as one chain,
    /// <c>A OF G AS C</c> — a view, which §8.4.3.5.3 SR2 forbids as a receiving operand. Pure.</summary>
    internal bool IsObjectViewOfQualifiedItem(Core.DataReferenceContext dref)
    {
        bool saved = _probing;
        _probing = true;
        try { return ViewSubjectChain(dref) is not null; }
        finally { _probing = saved; }
    }

    /// <summary>§8.4.3.1.4 GR1 a) before c): when the chain <c>head OF … OF subject</c> in front of a trailing object-view
    /// (<c>A OF G AS C</c>) is itself a qualified data name, the view applies to THAT item, not to the last word —
    /// the grammar reads the view as the property object (GR1 c) before d), the reading a property-name needs), so the
    /// data reading is decided here, by the symbols. Null when the chain is no qualified data name.</summary>
    private RefResolution? ViewOfQualifiedItem(Core.DataReferenceContext dref, bool report)
    {
        if (ViewSubjectChain(dref) is not { } chain) return null;
        var (subject, view) = chain;
        var resolved = ResolveImpl(subject, report);
        if (resolved.Place is not { } place || !report) return resolved;
        if (ApplyObjectView?.Invoke(place, view, DataBinder.WrittenText(subject)) is { } viewed)
            return RefResolution.Resolved(viewed, "");
        return RefResolution.Refused(DataBinder.WrittenText(dref));
    }

    /// <summary>The qualified data name an object-view written at the end of a chain applies to, and the view — or null.</summary>
    private (Core.DataReferenceContext Subject, Core.ObjectViewContext View)? ViewSubjectChain(Core.DataReferenceContext dref)
    {
        var suffixes = dref.dataReferenceSuffix();
        int at = Array.FindIndex(suffixes, s => s.propertyObject() is not null);
        if (at < 0 || dref.cobolWord() is not { } head
            || suffixes[at].propertyObject() is not { } po || po.objectView() is not { } view
            || view.objectReferenceTerm().dataReference() is not { } term || term.cobolWord() is null
            || ReadWritten(term).PropertyObject is not null)
            return null;

        // head + the suffixes before the object + `OF term-word` + the term's own suffixes, as ONE reference.
        var subject = new Core.DataReferenceContext(dref.Parent as ParserRuleContext, dref.invokingState);
        subject.AddChild(head);
        for (int i = 0; i < at; i++) subject.AddChild(suffixes[i]);
        var qualifier = new Core.QualificationContext(subject, 0);
        qualifier.AddChild(po.OF());
        qualifier.AddChild(term.cobolWord());
        qualifier.Start = po.Start;
        qualifier.Stop = term.cobolWord().Stop;
        subject.AddChild(Suffix(subject, qualifier));
        foreach (var s in term.dataReferenceSuffix()) subject.AddChild(s);
        subject.Start = dref.Start;
        subject.Stop = term.Stop;

        var w = ReadWritten(subject);
        return ResolveQualified(head.Name(), w.Qualifiers) is not null ? (subject, view) : null;
    }

    /// <summary>Resolve <paramref name="dref"/> (head word <paramref name="name"/>, already known NOT to be a data name)
    /// as §8.4.3.1.2 Format 7. Returns <c>(temp, null)</c> for a property reference (the §8.4.3.9.4 temporary; a probe gets
    /// the accessor's MODEL item, with nothing synthesized or registered), <c>(null, answer)</c> when identifier-3 or the
    /// property was refused (reported), and <c>(null, null)</c> when the shape is no property reference at all — the
    /// caller keeps its generic unknown-name diagnosis.</summary>
    private (DataItem? Temp, RefResolution? Final) ResolveObjectProperty(Core.DataReferenceContext dref, string name,
        WrittenReference written)
    {
        if (data.OoClasses is not { } table) return default;
        var terminal = written.PropertyObject;
        var objRef = PropertyObjectReference(dref);
        if (objRef is null && terminal is null) return default;
        string objWritten = objRef is not null ? DataBinder.WrittenText(objRef) : ObjectText(terminal!);
        RefResolution Refusal() => RefResolution.Refused(DataBinder.WrittenText(dref));
        int preOpMark = data.PendingPreOps.Count;

        PropertyReceiver receiver;
        if (objRef is null)
        {
            // A subscript after a non-word object (`P OF U AS C (2)`) subscripts neither part: identifier-3 is no
            // qualified data name and the property's value is the get method's RETURNING item, no table element
            // (§8.4.2.3.2 writes subscripts only after a qualified data name; §8.4.2.3.3 SR2). One BEFORE the object
            // (`P (2) OF SELF`) is ExpressionFormationPass's COBOLNET2776; a word chain keeps its subscripts in
            // identifier-3 (PropertyObjectReference), where the ordinary screen reads them.
            if (written.SubscriptGroup is { } sub && sub.Start.TokenIndex > terminal!.Stop.TokenIndex)
            {
                if (!_probing && _diagnosed.Add(dref))
                    data.Edition.Error(DiagnosticCatalog.SubscriptOnNonTableItem,
                        $"'{DataBinder.WrittenText(dref)}': a subscript is written on the object property '{name}' OF "
                        + $"'{objWritten}', which is no table element, so no subscript may be written on it "
                        + "(ISO §8.4.2.3.3 SR2; §8.4.3.9.3 SR5)");
                return (null, Refusal());
            }
            // `name OF SUPER` with a class-name is the qualified SUPER (§8.4.3.8.2) written as an operand: §8.4.3.8.3 SR3
            // admits it only as the object an invocation selects a method through (OoBinder.OoBindByReceiver).
            if (terminal!.SUPER() is not null && IsClassName(dref, name))
            {
                if (!_probing && _diagnosed.Add(dref))
                    data.Edition.Error(DiagnosticCatalog.SuperPosition,
                        $"'{DataBinder.WrittenText(dref)}' is written as an operand: SUPER may be specified only as the object "
                        + "in an object-property identifier or as the object used to invoke a method (ISO §8.4.3.8.3 SR3)");
                return (null, Refusal());
            }
            if (BindPropertyObject?.Invoke(terminal, null, _probing) is not { } bound)
                return _probing || BindPropertyObject is null ? default : (null, Refusal());
            receiver = bound;
        }
        else if (objRef.dataReferenceSuffix().Length == 0
                 && OoNameResolution.Lookup(table, dref, objRef.cobolWord()!.Name(), OoNameResolution.Want.Class).Class is { } cls)
        {
            // `prop OF class-name` — the FACTORY accessors (SR3/SR4 "or in the factory object of the object class
            // object-class-name-1"). The class-name is a SOURCE reference and takes the §8.4.6.4 scope (kb/Work PB365).
            receiver = new PropertyReceiver(InvokeForm.Factory, null, cls, null, Factory: true);
        }
        else if (QualifiedSuperClass(objRef) is { } superClass)
        {
            if (BindPropertyObject?.Invoke(objRef.dataReferenceSuffix()[0].propertyObject(), superClass, _probing) is not { } bound)
                return _probing || BindPropertyObject is null ? default : (null, Refusal());
            receiver = bound;
        }
        else
        {
            // identifier-3 by the ordinary identifier resolver: whatever it is, it shall be an object reference (SR2).
            if (Probe(objRef) is not { Item.Pic.Category: PicCategory.ObjectReference } sniff) return default;
            // The roster is identifier-3's description AS RESOLVED: for a view of a qualified item (`A OF G AS C`) that
            // is the view's (§8.4.3.5.4 GR1), which the probe's un-viewed item is not — the committed place carries it,
            // and a probe reads it from the view's last phrase, as for a view written alone.
            bool universal = false;
            PropertyReceiver? typed;
            if (!_probing)
            {
                var committed = Resolve(objRef);
                if (committed.Place is not { } place) return (null, committed);
                typed = RosterOf(place.Item, new PropertyReceiver(InvokeForm.Instance, place, null, null, false), name,
                    objWritten, out universal);
            }
            else
                typed = ViewSubjectChain(objRef) is { View.Parent: Core.PropertyObjectContext viewed }
                    ? BindPropertyObject?.Invoke(viewed, null, true)
                    : RosterOf(sniff.Item, new PropertyReceiver(InvokeForm.Instance, null, null, null, false), name,
                        objWritten, out universal);
            if (typed is not { } found) return universal && !_probing ? (null, Refusal()) : default;
            receiver = found;
        }
        if (receiver is { Class: null, Interface: null })
        {
            // A computed object (a view, a function) — its roster is its temporary's description.
            // Its binder reported a non-object identifier; a universal one is SR2's, reported by RosterOf.
            if (receiver.Receiver is not { } computed
                || RosterOf(computed.Item, receiver, name, objWritten, out _) is not { } typed)
                return !_probing ? (null, Refusal()) : default;
            receiver = typed;
        }

        // §12.3.8.2's property-specifier `PROPERTY property-name-1 [ AS literal-4 ]` (kb/Work PB974): the source writes
        // property-name-1, and literal-4 — when written — is the property as the declared classes know it (§12.3.8.3
        // SR16 a)), so the accessors are looked up under THAT name. Without a specifier the reference is still
        // reported below (SR1), under the name as written.
        string known = data.OoRepositoryProperties.TryGetValue(name, out var specified) ? specified : name;
        var get = Accessor(receiver, NamingConvention.GetAccessorName(known));
        var set = Accessor(receiver, NamingConvention.SetAccessorName(known));
        if (get is null && set is null) return default;      // not a property of the roster → generic diagnosis

        var model = get?.Binding!.Returning ?? set!.Binding!.Formals[0].Item;
        // R30 PURITY (kb/Work PB157): a PROBE gets the property's MODEL item — the accessor's own description, carrying
        // the category the sniff asks about — with NO temp, NO pending op and NO diagnostics. The committing resolution
        // that follows does all three exactly once.
        if (_probing) return (model, null);

        // A property reference invokes the get / set accessor METHOD (§8.4.3.9.4 GR1/GR2): the source element
        // "invokes any method" for ISO §7.3.14.4 GR4 b, though the text is a qualified data reference.
        data.ActivationSites.Add(dref);

        if (!data.OoRepositoryProperties.ContainsKey(name))
            data.Edition.Error("COBOLNET0843",
                $"the object-property reference '{name}' OF '{objWritten}' requires a PROPERTY specifier in the "
                + "REPOSITORY paragraph (ISO §8.4.3.9.3 SR1; §12.3.8)");

        // identifier-3's own evaluation — a view's conformance check, a function's activation, a subscript's — travels
        // with the property, ahead of its accessor (DataBinder.OoPendingPropertyOp.Prelude).
        var prelude = data.PendingPreOps.GetRange(preOpMark, data.PendingPreOps.Count - preOpMark);
        data.PendingPreOps.RemoveRange(preOpMark, prelude.Count);

        // ⛔ THE TEMP TAKES THE ACCESSOR'S WHOLE DESCRIPTION, A GROUP INCLUDED (kb/Work PB1448). §8.4.3.9.4 GR1: "The
        // data description of temp-1 is the same as the data description of the item specified in the RETURNING phrase
        // of the get property method", GR2 gives temp-2 the USING parameter's, and §8.4.3.9.3 SR5/SR6 admit the property
        // "wherever a data item with that description would be valid" as a sending / receiving item.
        var temp = data.OoCreatePropertyTemp(model, name);
        data.OoPendingPropertyOps.Add(new DataBinder.OoPendingPropertyOp(
            temp, receiver.Form, receiver.Receiver, receiver.Interface?.CsName ?? receiver.Class!.CsName,
            get, set, name, objWritten, prelude, InterfaceCsName: receiver.Interface?.CsName,
            SelectedByValue: SelectsByValue((IParseTree?)objRef ?? terminal)));
        return (temp, null);
    }

    /// <summary>True when identifier-3 selects its object through a value read at run time: a subscript or argument
    /// written with a data-name (or an index-name), or a function-identifier. Such an object can be a different one at
    /// the property's SET than at its GET, or than at the moment §14.7.7 4) b) and §14.9.25.4 GR1 identify the receiver
    /// (kb/Work PB2078), so <c>OoBinder.OoWrapPropertyOps</c> refuses it as a RECEIVING operand.</summary>
    private static bool SelectsByValue(IParseTree? tree) => tree switch
    {
        null => false,
        ITerminalNode t => t.Symbol.Type == Core.SUB_IDENTIFIER,
        Core.FunctionCallContext => true,
        _ => Enumerable.Range(0, tree.ChildCount).Any(i => SelectsByValue(tree.GetChild(i))),
    };

    /// <summary>The roster an object-reference item of <paramref name="item"/>'s description selects (§8.4.3.9.3 SR3/SR4):
    /// FACTORY OF selects the factory half (kb/Work PB389), an interface-typed reference its prototype closure (kb/Work
    /// PB1449, §11.8.4 GR2). A universal reference is SR2's refusal (reported unless probing); null also for a class this
    /// compilation cannot see, which is no property of any roster — the generic diagnosis.</summary>
    private PropertyReceiver? RosterOf(DataItem item, PropertyReceiver receiver, string name, string objWritten,
        out bool universal)
    {
        universal = false;
        if (item.Pic is not { Category: PicCategory.ObjectReference } pic || data.OoClasses is not { } table) return null;
        var described = pic.ObjectRef ?? ObjectRefDescriptor.Universal;
        if (universal = described.IsUniversal)
        {
            // §8.4.3.9.3 SR2: "Identifier-1 shall be an object reference; neither a universal object reference nor the
            // predefined object reference NULL shall be specified."
            if (!_probing)
                data.Edition.Error(DiagnosticCatalog.PropertyObject,
                    $"the object-property reference '{name}' OF '{objWritten}': its object shall not be a universal "
                    + "object reference (ISO §8.4.3.9.3 SR2)");
            return null;
        }
        if (described.Kind is ObjectRefKind.Interface)
            return table.FindInterface(described.Name!) is { } iface ? receiver with { Interface = iface, Factory = false } : null;
        return table.Find(described.Name!) is { } cls ? receiver with { Class = cls, Factory = described.Factory } : null;
    }

    private static OoMethodSymbol? Accessor(PropertyReceiver receiver, string accessorName) =>
        receiver.Interface is { } iface
            ? iface.AllPrototypes().FirstOrDefault(p => CobolNames.Same(p.ExternalizedName, accessorName))
            : receiver.Factory ? receiver.Class!.FindFactoryMethod(accessorName) : receiver.Class!.FindMethod(accessorName);

    /// <summary>Identifier-3 of a property reference written as a word chain: the written reference less its head word
    /// (the property-name) and its reference modifiers (§8.4.3.1.4 GR1 g) applies them to the property's value, last),
    /// keeping every qualifier, the subscripts and a trailing non-word object — the chain the ordinary resolver then
    /// reads (a qualified, subscripted data name, or a chained property). Null when no qualifier follows the head, so
    /// identifier-3 is the non-word object alone. The copy ADOPTS the original children (the
    /// <see cref="WithoutTrailingSuffix"/> technique) and never mutates the parse tree.</summary>
    private static Core.DataReferenceContext? PropertyObjectReference(Core.DataReferenceContext dref)
    {
        var suffixes = dref.dataReferenceSuffix();
        int first = Array.FindIndex(suffixes, s => s.qualification() is not null);
        if (first < 0) return null;
        var q1 = suffixes[first].qualification();
        var obj = new Core.DataReferenceContext(dref.Parent as ParserRuleContext, dref.invokingState);
        obj.AddChild(q1.cobolWord());
        IToken stop = q1.cobolWord().Stop;
        foreach (var sp in q1.subscriptPart())
            if (!IsRefMod(sp)) stop = Adopt(obj, Suffix(obj, sp));
        for (int i = first + 1; i < suffixes.Length; i++)
        {
            var s = suffixes[i];
            if (s.refModPart() is not null || (s.subscriptPart() is { } part && IsRefMod(part))) continue;
            stop = Adopt(obj, s.qualification() is { } q && (q.refModPart().Length > 0 || q.subscriptPart().Any(IsRefMod))
                ? Suffix(obj, WithoutRefMods(q))
                : s.propertyObject() is { } po && ResultRefModsOf(po).Length > 0 ? Suffix(obj, WithoutResultRefMods(po)) : s);
        }
        obj.Start = q1.cobolWord().Start;
        obj.Stop = stop;
        return obj;

        static bool IsRefMod(Core.SubscriptPartContext sp) => sp.subscriptOrRefMod() is { } g && HasDepth0Colon(g);
        static IToken Adopt(Core.DataReferenceContext into, ParserRuleContext child)
        {
            into.AddChild(child);
            return child.Stop;
        }
        static Core.QualificationContext WithoutRefMods(Core.QualificationContext q)
        {
            var copy = new Core.QualificationContext(q.Parent as ParserRuleContext, q.invokingState);
            copy.AddChild((ITerminalNode)q.GetChild(0));
            copy.AddChild(q.cobolWord());
            IToken last = q.cobolWord().Stop;
            foreach (var sp in q.subscriptPart())
                if (!IsRefMod(sp)) { copy.AddChild(sp); last = sp.Stop; }
            copy.Start = q.Start;
            copy.Stop = last;
            return copy;
        }
    }

    /// <summary>The reference modifiers a function object writes after its argument list (§8.4.3.1.4 GR1 g) applies them
    /// to the whole identifier, last: the function returns an object reference, which §8.4.3.3.3 SR1 never lets one
    /// modify). Without an argument list a '(' after the name is the function's own business (§8.4.3.2.3 SR6).</summary>
    internal static Core.RefModPartContext[] ResultRefModsOf(Core.PropertyObjectContext po) =>
        po.functionCall() is { } fc && fc.FNARG_LPAREN() is not null ? fc.refModPart() : [];

    /// <summary><paramref name="po"/> without <see cref="ResultRefModsOf"/> — a copy ADOPTING the original children (the
    /// <see cref="WithoutTrailingSuffix"/> technique), or <paramref name="po"/> itself when it writes none.</summary>
    internal static Core.PropertyObjectContext WithoutResultRefMods(Core.PropertyObjectContext po)
    {
        if (ResultRefModsOf(po).Length == 0) return po;
        var fc = po.functionCall()!;
        var copy = new Core.PropertyObjectContext(po.Parent as ParserRuleContext, po.invokingState);
        copy.AddChild(po.OF());
        var call = new Core.FunctionCallContext(copy, fc.invokingState);
        foreach (var child in fc.children)
        {
            if (child is Core.RefModPartContext) continue;
            if (child is ITerminalNode t) { call.AddChild(t); call.Stop = t.Symbol; }
            else if (child is ParserRuleContext r) { call.AddChild(r); call.Stop = r.Stop; }
        }
        call.Start = fc.Start;
        copy.AddChild(call);
        copy.Start = po.Start;
        copy.Stop = call.Stop;
        return copy;
    }

    /// <summary>A synthetic <c>dataReferenceSuffix</c> wrapping <paramref name="part"/> (a subscript part or a
    /// qualification) — the node every suffix reader walks.</summary>
    private static Core.DataReferenceSuffixContext Suffix(ParserRuleContext parent, ParserRuleContext part)
    {
        var suffix = new Core.DataReferenceSuffixContext(parent, 0);
        suffix.AddChild(part);
        suffix.Start = part.Start;
        suffix.Stop = part.Stop;
        return suffix;
    }

    /// <summary>The non-word object as written, without its <c>OF</c>.</summary>
    private static string ObjectText(Core.PropertyObjectContext po) =>
        DataBinder.WrittenText(po).AsSpan(po.OF().Symbol.Text.Length).Trim().ToString();
}
