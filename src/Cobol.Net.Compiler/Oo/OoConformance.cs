// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime;

namespace CobolNet.Compiler.Oo;

/// <summary>One covariant-return adapter pair (§9.3.8.2.3 rules 5a/5c2 — conforming COBOL that C# interface
/// implementation cannot express directly, because interface implementations demand the EXACT return type):
/// the emitter renders an EXPLICIT interface implementation <c>PROTO_RET I_CS.M(…) =&gt; this.M(…);</c> per
/// pair; <paramref name="Factory"/> marks the FACTORY side.</summary>
public readonly record struct AdapterPair(
    OoInterfaceSymbol Iface, OoMethodSymbol Proto, OoMethodSymbol Impl, bool Factory);

/// <summary>
/// The OO conformance SERVICE (P9 — R3: validation moved OFF the pass-1 symbol table, which stays a pure
/// lookup structure): the §9.3.8.2 override-signature check, the §9.3.11 IMPLEMENTS pass (returning the
/// covariant <see cref="AdapterPair"/>s instead of mutating table state), the ONE strict identical-description
/// check (§14.8.2.3.2) and the §14.8.3.3-rule-1 object-reference
/// widening direction. Stateless — every entry takes the <see cref="OoClassTable"/> it validates over.
/// </summary>
public static class OoConformance
{
    /// <summary>Validate every override against the method it overrides — ISO §11.7.3 SR9: "If method-name-1 or
    /// literal-1 is the same as a method-name inherited or implemented by the containing definition, the parameter
    /// declarations, returning item, and exceptions that may be raised on the procedure division header shall obey
    /// the rules of conformance according to 9.3.8.2.3, Conformance between interfaces, such that the interface
    /// described by the factory or instance definition containing this method definition conforms to the
    /// interface described by the factory or instance definition containing the inherited or implemented method
    /// definition." So the OVERRIDE is interface-1's method and the overridden one interface-2's, and the rules
    /// are <see cref="MethodConformanceMismatches"/> — the ONE place they are written (kb/Work PB972: this pass
    /// used to re-spell rules 1–6 and 8 with its own wording, and rule 9 was missing from both copies). Runs AFTER
    /// every class's data has bound (formals resolve at data-bind time, not pass-1). A violation is COBOLNET0829 —
    /// a COBOL-worded bind diagnostic, never a Roslyn CS0508/CS0115 on user source (the G4 rule), positioned at
    /// the overriding method.</summary>
    public static void ValidateOverrideSignatures(OoClassTable table, EditionContext edition)
    {
        foreach (var cls in table.Classes)
            foreach (var m in cls.Methods.Concat(cls.FactoryMethods))
            {
                if (m.OverrideOf is not { } baseM) continue;
                using var atMethod = edition.At(m.Ctx);
                string where = $"class '{cls.Name}', method '{m.Name}' overriding '{baseM.Owner.Name}'.'{baseM.Name}'";
                foreach (var err in MethodConformanceMismatches(table, m, baseM,
                             $"the overridden method of class '{baseM.Owner.Name}'", cls.Name, baseM.Owner.Name))
                    edition.Error(DiagnosticCatalog.OverrideSignatureMismatch, $"{where}: {err}; ISO §11.7.3 SR9");
            }
    }

    /// <summary>⛔ ISO §8.4.3.9.3 SR7 — THE TWO ACCESSORS OF ONE PROPERTY DESCRIBE IT THE SAME (kb/Work PB1450): "The
    /// data description of the item specified in the RETURNING phrase of the get property method shall be the same as
    /// the data description of the item specified as the USING parameter of the set property method." Nothing
    /// compared them, so a GET RETURNING PIC 9(5) with a SET USING PIC X(3) compiled and the §8.4.3.9.4 temp (modelled
    /// on the GET's item) crossed a SET formal of another description. The comparison is the ONE pair-mode identity
    /// (<see cref="SignatureDifference"/> — the relation the override and IMPLEMENTS signatures use, §9.3.8.2.3 rules 2/3
    /// and 6), asked of each (GET, SET) pair AS THE CLASS SEES IT:
    /// the roster lookup walks INHERITS (§9.3.6), so a GET inherited from a superclass and a SET written in the
    /// subclass form a pair at the subclass, and a pair already complete in a superclass is the superclass's own.
    /// Runs AFTER every class's data has bound (the formals resolve at data-bind time). The instance and the
    /// FACTORY roster are separate interfaces (§11.4), so each is its own pairing.</summary>
    public static void ValidatePropertyAccessorPairs(OoClassTable table, EditionContext edition)
    {
        foreach (var cls in table.Classes)
        {
            Check(cls, cls.Methods, cls.FindMethod);
            Check(cls, cls.FactoryMethods, cls.FindFactoryMethod);
        }

        void Check(OoClassSymbol cls, IReadOnlyList<OoMethodSymbol> declared, Func<string, OoMethodSymbol?> find)
        {
            var seen = new HashSet<string>(CobolNames.Comparer);
            foreach (var accessor in declared)
            {
                if (accessor.PropertyName is not { } prop || !seen.Add(prop)) continue;
                if (find(NamingConvention.GetAccessorName(prop)) is not { Binding.Returning: { } returning } get
                    || find(NamingConvention.SetAccessorName(prop)) is not { Binding.Formals: [{ } setFormal] } set)
                    continue;   // a missing accessor is §8.4.3.9.3 SR3/SR4's, decided where the polarity is known
                if (SignatureDifference(returning, setFormal.Item, "RETURNING item of the get property method",
                        "USING parameter of the set property method") is not { } why) continue;
                // The diagnostic stands at the accessor THIS class wrote, the later of the pair when it wrote both.
                using var at = edition.At((set.Owner == cls ? set : get).Ctx ?? accessor.Ctx);
                edition.Error(DiagnosticCatalog.PropertyAccessorDescriptionMismatch,
                    $"class '{cls.Name}', property '{prop}' (get property method of '{get.Owner.Name}', set property "
                    + $"method of '{set.Owner.Name}'): {why} (ISO §8.4.3.9.3 SR7)");
            }
        }
    }

    /// <summary>The §9.3.11 IMPLEMENTS conformance pass (via §9.3.8.2.3 — D-I1: the BINDER is the authority;
    /// Roslyn satisfaction is provably insufficient BOTH directions: the C# projection is lossy for
    /// non-object descriptions [PIC 9(4) and 9(8) both emit `ref long` — Roslyn under-rejects], and C#
    /// interface implementations forbid covariant returns that rules 5a/5c2 PERMIT [Roslyn over-rejects
    /// conforming COBOL — cured by the explicit-implementation adapters]). Runs AFTER all class AND
    /// interface formals resolve. The check runs over the §11.8.4 GR2 transitive CLOSURE (direct IMPLEMENTS
    /// + interface-INHERITed + class-INHERITed) per class; each violation is COBOLNET0841 citing the
    /// numbered rule. RETURNS the covariant-return <see cref="AdapterPair"/>s (P9 R3 — a validation pass
    /// reports its findings; it does not mutate the symbol table).</summary>
    public static IReadOnlyList<AdapterPair> ValidateImplements(OoClassTable table, EditionContext edition)
    {
        var adapters = new List<AdapterPair>();
        foreach (var cls in table.Classes)
        {
            Check(cls, table.ImplementsClosure(cls, factory: false), factory: false);
            Check(cls, table.ImplementsClosure(cls, factory: true), factory: true);
        }
        return adapters;

        void Check(OoClassSymbol cls, IReadOnlyList<OoInterfaceSymbol> closure, bool factory)
        {
            string side = factory ? "factory " : "";
            foreach (var iface in closure)
                foreach (var proto in iface.AllPrototypes())
                {
                    var impl = factory ? cls.FindFactoryMethod(proto.ExternalizedName) : cls.FindMethod(proto.ExternalizedName);   // the roster key (PB303)
                    if (impl is null)
                    {
                        using var atClass = edition.At(cls.Ctx.classIdParagraph().className(0));
                        edition.Error(DiagnosticCatalog.InterfaceNotImplemented,
                            $"class '{cls.Name}': the {side}interface '{iface.Name}' requires a method "
                            + $"'{proto.SourceName}' and none is defined or inherited (ISO §9.3.11 — a class "
                            + "shall implement ALL the method prototypes of its interfaces, including "
                            + "inherited ones)");
                        continue;
                    }
                    bool conforms = true;
                    using (edition.At(impl.Ctx))
                        foreach (var err in MethodConformanceMismatches(table, impl, proto,
                                     $"the '{iface.Name}' prototype", cls.Name, iface.Name))
                        {
                            conforms = false;
                            edition.Error(DiagnosticCatalog.InterfaceNotImplemented, $"class '{cls.Name}', method '{impl.SourceName}': {err}");
                        }
                    // Conformant-but-covariant RETURNING: C# needs the explicit-implementation adapter — a question about
                    // the two C# TYPES the returns project to, not about their COBOL descriptions: two ACTIVE-CLASS
                    // returns have the same description whatever classes they were written in (the pair rule is
                    // MethodConformanceMismatches's) yet project to different types (the class's own, the prototype's
                    // owner-less CobolObject).
                    if (conforms
                        && impl.Binding!.Returning?.Pic is { Category: PicCategory.ObjectReference } rp
                        && proto.Binding!.Returning?.Pic is { Category: PicCategory.ObjectReference } prp
                        && rp.ClrType != prp.ClrType)
                        adapters.Add(new AdapterPair(iface, proto, impl, factory));
                }
        }
    }

    /// <summary>§9.3.8.2.3 rule 8: "The presence or absence of the OPTIONAL phrase is the same for corresponding
    /// parameters." (kb/Work PB757 — the phrase is carried on <see cref="OoFormal.Optional"/> since the method arm of
    /// the OPTIONAL formal landed.) Null when the pair agrees.</summary>
    internal static string? OptionalMismatch(OoFormal f1, OoFormal f2) =>
        f1.Optional == f2.Optional ? null
        : $"the OPTIONAL phrase is {(f1.Optional ? "specified" : "absent")} here but "
            + $"{(f2.Optional ? "specified" : "absent")} on the corresponding parameter";

    /// <summary>
    /// ⛔ ISO §9.3.8.2.3 FOR ONE METHOD PAIR — the ONE place the per-method conformance rules are written: does
    /// method <paramref name="m1"/> (of interface-1, the CONFORMING side — a class's implementation, or another
    /// interface's prototype) satisfy the conditions for method <paramref name="m2"/> of interface-2 (named
    /// <paramref name="counterpart"/> in the messages — "the 'I' prototype", "the overridden method of class 'B'")?
    /// Yields one rule-cited message per violation; empty ⇔ the pair conforms. Rules carried: 1) the parameter
    /// count AND the passing mode of each pair of formals ("consistent BY REFERENCE and BY VALUE specifications" —
    /// <see cref="OoFormal.ByValue"/>, kb/Work PB1051); 2)/3) identical formal descriptions
    /// (<see cref="SignatureDifference"/>, the run-time relation's pair-mode identity); 4) RETURNING presence; 5) the
    /// object-reference RETURNING
    /// (covariant — <see cref="ObjectRefAssignmentMismatch(OoClassTable, ObjectRefDescriptor, ObjectRefDescriptor, bool)"/>
    /// with rule 5's closed ACTIVE-CLASS list); 6) identical non-object RETURNING descriptions; 7) strongly-typed
    /// groups of the same type (inside <see cref="SignatureDifference"/>, for every formal and a non-object RETURNING);
    /// 8) the OPTIONAL phrase (<see cref="OptionalMismatch"/>); 9) the RAISING
    /// phrase (<see cref="RaisingMismatches"/>); and the clause's closing sentence, the mutual-reference ban between
    /// the two RETURNING descriptions (<see cref="ReturningCircularity"/>, kb/Work PB1498 — which is why the askers name
    /// <paramref name="interface1"/> and <paramref name="interface2"/>).
    /// <para>Its three askers: §9.3.11 IMPLEMENTS (<see cref="ValidateImplements"/> — the class is interface-1),
    /// §11.7.3 SR9 overrides (<see cref="ValidateOverrideSignatures"/> — the override is interface-1), and the
    /// interface-to-interface relation (<see cref="InterfaceConformsTo"/>).</para>
    /// <para>Extracted from <see cref="ValidateImplements"/> (kb/Work PB814) so that §9.3.11 IMPLEMENTS
    /// conformance and the interface-to-interface conformance GOBACK §14.9.18.3 SR4 b) asks
    /// (<see cref="InterfaceConformsTo"/>) run the SAME comparisons — two copies of one rule set is the shape
    /// under which one arm silently drifts.</para>
    /// </summary>
    /// <param name="interface1">The name of the class or interface whose interface is interface-1 (the conforming side).</param>
    /// <param name="interface2">The name of the class or interface whose interface is interface-2.</param>
    internal static IEnumerable<string> MethodConformanceMismatches(OoClassTable table, OoMethodSymbol m1,
        OoMethodSymbol m2, string counterpart, string interface1, string interface2)
    {
        if (ReturningCircularity(table, m1, interface1, m2, interface2) is { } cerr)
            yield return $"RETURNING: {cerr} vs {counterpart}";
        foreach (var rerr in RaisingMismatches(table, m1, m2, counterpart))
            yield return rerr;
        if (m1.Binding!.Formals.Count != m2.Binding!.Formals.Count)
        {
            yield return $"{m1.Binding!.Formals.Count} formal(s) vs {counterpart}'s "
                + $"{m2.Binding!.Formals.Count} (ISO §9.3.8.2.3 rule 1)";
            yield break;
        }
        for (int i = 0; i < m1.Binding!.Formals.Count; i++)
        {
            if (m1.Binding!.Formals[i].ByValue != m2.Binding!.Formals[i].ByValue)
                yield return $"formal #{i + 1}: {(m1.Binding!.Formals[i].ByValue ? "BY VALUE" : "BY REFERENCE")} here but "
                    + $"{(m2.Binding!.Formals[i].ByValue ? "BY VALUE" : "BY REFERENCE")} on the corresponding parameter of "
                    + $"{counterpart} (ISO §9.3.8.2.3 rule 1 — consistent BY REFERENCE and BY VALUE specifications)";
            // Rules 2 / 3 / 7, which the C# projection cannot check: PIC 9(4) and PIC 9(8) both cross as a long.
            if (SignatureDifference(m1.Binding!.Formals[i].Item, m2.Binding!.Formals[i].Item, "formal parameter",
                    $"corresponding parameter of {counterpart}") is { } err)
                yield return $"formal #{i + 1}: {err}";
            if (OptionalMismatch(m1.Binding!.Formals[i], m2.Binding!.Formals[i]) is { } oerr)
                yield return $"formal #{i + 1}: {oerr} vs {counterpart} (ISO §9.3.8.2.3 rule 8)";
        }
        if ((m1.Binding!.Returning is null) != (m2.Binding!.Returning is null))
            yield return $"RETURNING presence differs from {counterpart} (ISO §9.3.8.2.3 rule 4)";
        else if (m1.Binding!.Returning is { } r && m2.Binding!.Returning is { } pr)
        {
            if (r.Pic is { Category: PicCategory.ObjectReference } rp
                && pr.Pic is { Category: PicCategory.ObjectReference } prp)
            {
                if (ObjectRefAssignmentMismatch(table, rp, prp, activeClassSenderAdmitted: false) is { } werr)
                    // Rule 5 d) governs interface-2's ACTIVE-CLASS returning item; 5 a)–c) every other description.
                    yield return $"RETURNING: {werr} (ISO §9.3.8.2.3 "
                        + $"{(prp.ObjectRef is { Kind: ObjectRefKind.ActiveClass } ? "rule 5 d)" : "rules 5a/5c2")})";
            }
            else if (SignatureDifference(r, pr, "returning item", $"returning item of {counterpart}") is { } rerr)
                yield return $"RETURNING: {rerr}";
        }
    }

    /// <summary>
    /// ISO §9.3.8.2.3 rule 9: "If the RAISING phrase is specified in the procedure division header of the method in
    /// interface-1, the corresponding method in interface-2 specifies the RAISING phrase following these rules" —
    /// EVERY element of <paramref name="m1"/>'s phrase needs a covering element in <paramref name="m2"/>'s:
    /// <list type="bullet">
    ///   <item>a) an exception-name — "the same exception-name";</item>
    ///   <item>b) an object-class-name — "the same object-class-name or the name of a superclass of the class
    ///     identified by that object-class-name, including the FACTORY phrase if and only if the RAISING phrase in
    ///     interface-1 specifies the FACTORY phrase", or "the name of an interface implemented by the factory
    ///     object of that class" (FACTORY) / "by the instance object of that class" (no FACTORY) — implemented in
    ///     the §11.8.4 GR2 sense, <see cref="OoClassTable.ImplementsClosure"/>;</item>
    ///   <item>c) an interface-name — "the same interface-name or the name of an interface inherited by that
    ///     interface" (the INHERITS closure, §9.3.10).</item>
    /// </list>
    /// The direction matters: an interface-2 RAISING phrase with nothing in interface-1 conforms (interface-1 may
    /// raise LESS). Every name here was scope-checked where it was written (<c>RaisingPhrase.Partition</c>, the
    /// §8.4.6.4 funnel), so the table lookups below are symbol lookups on validated names.
    /// </summary>
    internal static IEnumerable<string> RaisingMismatches(OoClassTable table, OoMethodSymbol m1, OoMethodSymbol m2,
        string counterpart)
    {
        foreach (var t in m1.Raising)
        {
            string? arm = t.Kind switch
            {
                RaisingTargetKind.ExceptionName => m2.Raising.Any(u => u.Kind is RaisingTargetKind.ExceptionName
                    && CobolNames.Same(u.Name, t.Name)) ? null : "a",
                RaisingTargetKind.ObjectClass => ClassRaisingCovered(table, t, m2.Raising) ? null : "b",
                _ => InterfaceRaisingCovered(table, t, m2.Raising) ? null : "c",
            };
            if (arm is null) continue;
            string need = arm switch
            {
                "a" => $"the exception-name {t.Name}",
                "b" => $"'{t.Spelled}' or a superclass of {t.Name} with the FACTORY phrase "
                    + $"{(t.Factory ? "" : "not ")}specified, or an interface implemented by the "
                    + $"{(t.Factory ? "factory" : "instance")} object of {t.Name}",
                _ => $"the interface {t.Name} or an interface it inherits",
            };
            yield return m2.Raising.Count == 0
                ? $"RAISING {t.Spelled}: {counterpart} specifies no RAISING phrase, and it shall specify {need} "
                    + $"(ISO §9.3.8.2.3 rule 9 {arm}))"
                : $"RAISING {t.Spelled}: the RAISING phrase of {counterpart} "
                    + $"({string.Join(", ", m2.Raising.Select(u => u.Spelled))}) does not specify {need} "
                    + $"(ISO §9.3.8.2.3 rule 9 {arm}))";
        }
    }

    /// <summary>Rule 9 b) for one object-class-name element <paramref name="t"/> of interface-1's phrase.</summary>
    private static bool ClassRaisingCovered(OoClassTable table, RaisingTarget t, IReadOnlyList<RaisingTarget> other)
    {
        var cls = table.Find(t.Name);
        for (var c = cls; c is not null; c = c.Base)
            if (other.Any(u => u.Kind is RaisingTargetKind.ObjectClass && u.Factory == t.Factory
                    && CobolNames.Same(u.Name, c.Name)))
                return true;
        if (cls is null) return false;
        var implemented = table.ImplementsClosure(cls, factory: t.Factory);
        return other.Any(u => u.Kind is RaisingTargetKind.Interface
            && implemented.Any(i => CobolNames.Same(i.Name, u.Name)));
    }

    /// <summary>Rule 9 c) for one interface-name element <paramref name="t"/> of interface-1's phrase.</summary>
    private static bool InterfaceRaisingCovered(OoClassTable table, RaisingTarget t, IReadOnlyList<RaisingTarget> other)
    {
        var listed = other.Where(u => u.Kind is RaisingTargetKind.Interface).ToList();
        if (listed.Count == 0) return false;
        if (listed.Any(u => CobolNames.Same(u.Name, t.Name))) return true;
        if (table.FindInterface(t.Name) is not { } iface) return false;
        var seen = new HashSet<OoInterfaceSymbol>();
        var stack = new Stack<OoInterfaceSymbol>(iface.Inherits);
        while (stack.Count > 0)
        {
            var b = stack.Pop();
            if (!seen.Add(b)) continue;
            if (listed.Any(u => CobolNames.Same(u.Name, b.Name))) return true;
            foreach (var bb in b.Inherits) stack.Push(bb);
        }
        return false;
    }

    /// <summary>
    /// ⛔ ISO §9.3.8.2.3's CLOSING SENTENCE (kb/Work PB1498): "If the description of the returning item of a method in
    /// interface-1 directly or indirectly references interface-2, the description of the returning item of the
    /// corresponding method in interface-2 shall not directly or indirectly reference interface-1." Null when the pair
    /// satisfies it. Nothing examined it before: a pair whose RETURNING descriptions referenced each other's interface
    /// conformed.
    /// <para>"Indirectly" is the TRANSITIVE CLOSURE (owner decision kb/Work R63 — the ordinary meaning of the text, which
    /// defines it no further) over a reference graph whose nodes are classes and interfaces (<see cref="Reaches"/>): a
    /// RETURNING item described as an object reference to Y is an edge to Y, and a class reaches the interfaces it
    /// IMPLEMENTS and the class it INHERITS. Only DISTINCT interfaces are compared ("If two interfaces are of the same
    /// interface, they conform to each other"), so a self-referencing returning type — <c>NEXT</c> returning its own
    /// class — stays legal; the walk keeps a visited set, so a cycle in the graph terminates.</para>
    /// <para>A class or an interface is a node by its NAME, the key the class table resolves both by; its instance and
    /// factory sides are walked together, the same object being reachable from either.</para>
    /// </summary>
    internal static string? ReturningCircularity(OoClassTable table, OoMethodSymbol m1, string interface1,
        OoMethodSymbol m2, string interface2)
    {
        if (CobolNames.Same(interface1, interface2)) return null;
        if (ReturningReference(m1) is not { } r1 || ReturningReference(m2) is not { } r2) return null;
        return Reaches(table, r1, interface2) && Reaches(table, r2, interface1)
            ? $"the returning item of this method references '{interface2}' (through '{r1}') and the returning item of "
              + $"the corresponding method references '{interface1}' (through '{r2}'), directly or indirectly (ISO "
              + "§9.3.8.2.3: the description of the returning item of the corresponding method in interface-2 \"shall "
              + "not directly or indirectly reference interface-1\")"
            : null;
    }

    /// <summary>The class or interface a method's RETURNING item references — the name an object reference described
    /// with an object-class-name, an interface-name or ACTIVE-CLASS (whose descriptor names the containing class)
    /// carries; null for a universal reference or a returning item that is not an object reference.</summary>
    private static string? ReturningReference(OoMethodSymbol m) =>
        m.Binding?.Returning?.Pic is { Category: PicCategory.ObjectReference, ObjectRef: { Kind: not ObjectRefKind.Universal, Name: { } n } }
            ? n : null;

    /// <summary>Does the reference graph lead from <paramref name="start"/> to <paramref name="target"/> (the start
    /// itself counting — a DIRECT reference)? Edges: every RETURNING item of a class's methods (instance and factory)
    /// or an interface's prototypes (inherited ones included), a class's INHERITS and both IMPLEMENTS lists, an
    /// interface's INHERITS. Breadth-first with a visited set (kb/Work R63 3.).</summary>
    private static bool Reaches(OoClassTable table, string start, string target)
    {
        var seen = new HashSet<string>(CobolNames.Comparer);
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            string node = queue.Dequeue();
            if (!seen.Add(node)) continue;
            if (CobolNames.Same(node, target)) return true;
            if (table.Find(node) is { } cls)
            {
                foreach (var m in cls.Methods.Concat(cls.FactoryMethods))
                    if (ReturningReference(m) is { } r) queue.Enqueue(r);
                if (cls.Base is { } b) queue.Enqueue(b.Name);
                foreach (var i in cls.Implements.Concat(cls.FactoryImplements)) queue.Enqueue(i.Name);
            }
            else if (table.FindInterface(node) is { } iface)
            {
                foreach (var p in iface.AllPrototypes())
                    if (ReturningReference(p) is { } r) queue.Enqueue(r);
                foreach (var i in iface.Inherits) queue.Enqueue(i.Name);
            }
        }
        return false;
    }

    /// <summary>
    /// ISO §9.3.8.2.3, the interface relation itself: "If two interfaces are of the same interface, they conform
    /// to each other. If interface-1 and interface-2 are different interfaces, interface-1 conforms to interface-2
    /// if and only if the entry conventions for interface-1 and interface-2 are the same and for every method in
    /// interface-2 there is a method in interface-1 with the same name that satisfies the following
    /// conditions" — the conditions being <see cref="MethodConformanceMismatches"/>. "Every method in" an
    /// interface includes the inherited ones (§9.3.10: "The inheriting interface has all the method
    /// specifications defined for the inherited interface definition or definitions"), hence
    /// <see cref="OoInterfaceSymbol.AllPrototypes"/> on both sides. One implementation, one entry convention
    /// (every method of the group is emitted to the same .NET calling convention), so that clause holds. Asked by
    /// GOBACK §14.9.18.3 SR4 b) / EXIT §14.9.14.3 SR5 b) (kb/Work PB814); runs after every interface's prototype
    /// formals have bound (<c>BinderDriver</c> binds interface data before any procedure body).
    /// </summary>
    public static bool InterfaceConformsTo(OoClassTable table, OoInterfaceSymbol interface1, OoInterfaceSymbol interface2)
    {
        if (ReferenceEquals(interface1, interface2)) return true;
        var mine = new Dictionary<string, OoMethodSymbol>(CobolNames.Comparer);
        foreach (var p in interface1.AllPrototypes()) mine.TryAdd(p.ExternalizedName, p);   // first declaration wins
        foreach (var m2 in interface2.AllPrototypes())
            if (!mine.TryGetValue(m2.ExternalizedName, out var m1)
                || MethodConformanceMismatches(table, m1, m2, $"the '{interface2.Name}' prototype",
                       interface1.Name, interface2.Name).Any())
                return false;
        return true;
    }

    /// <summary>⛔ ISO §11.6.3 SR5 — AN INTERFACE THAT INHERITS ONE METHOD-NAME FROM SEVERAL INTERFACES CONFORMS TO ALL OF
    /// THEM (kb/Work PB1502): "If a given method-name is inherited from more than one interface, the method prototype in
    /// each inherited interface shall be such that this interface conforms to all inherited interfaces", with §9.3.10's
    /// "The inheriting interface shall always conform to each of the inherited interfaces". The inheriting interface
    /// presents ONE method of that name (it may declare none of its own, §11.7.3 SR4 b)), so the rule is satisfied exactly
    /// when ONE inherited prototype conforms (<see cref="MethodConformanceMismatches"/>, the §9.3.8.2.3 rule set every
    /// asker shares) to every other — identical prototypes inherited twice trivially, and a covariant RETURNING pair
    /// (rule 5) through the more specific one. That prototype is recorded as the interface's own presentation
    /// (<see cref="OoInterfaceSymbol.ChooseInheritedPrototype"/>), so <see cref="OoInterfaceSymbol.AllPrototypes"/>, the
    /// IMPLEMENTS pass and <see cref="InterfaceConformsTo"/> all read the conforming one. A violation is COBOLNET2762 on
    /// the INHERITING interface — before this pass it surfaced only when some class implemented the interface (COBOLNET0841
    /// at the class), and an unimplemented interface was accepted. Runs after every interface's prototype formals bound.</summary>
    public static void ValidateInterfaceInheritance(OoClassTable table, EditionContext edition)
    {
        foreach (var isym in table.Interfaces)
        {
            var byName = new Dictionary<string, List<(OoInterfaceSymbol Iface, OoMethodSymbol Proto)>>(CobolNames.Comparer);
            foreach (var inherited in isym.InheritedClosure())
                foreach (var proto in inherited.Prototypes)
                {
                    if (!byName.TryGetValue(proto.ExternalizedName, out var list))
                        byName[proto.ExternalizedName] = list = [];
                    list.Add((inherited, proto));
                }
            foreach (var (_, protos) in byName)
            {
                if (protos.Count < 2) continue;
                IEnumerable<string> Mismatches((OoInterfaceSymbol Iface, OoMethodSymbol Proto) one,
                    (OoInterfaceSymbol Iface, OoMethodSymbol Proto) other) =>
                    MethodConformanceMismatches(table, one.Proto, other.Proto, $"the '{other.Iface.Name}' prototype",
                        isym.Name, other.Iface.Name);
                var chosen = protos.FirstOrDefault(c => protos.All(o => ReferenceEquals(o.Proto, c.Proto) || !Mismatches(c, o).Any()));
                if (chosen.Proto is not null)
                {
                    isym.ChooseInheritedPrototype(chosen.Proto);
                    continue;
                }
                var (first, second) = (protos[0], protos.Skip(1).First(o => Mismatches(protos[0], o).Any()));
                using var atInterface = edition.At(isym.Ctx.interfaceName(0));
                edition.Error(DiagnosticCatalog.InterfaceInheritedPrototypesConflict,
                    $"interface '{isym.Name}': method '{first.Proto.Name}' is inherited from interfaces "
                    + $"{string.Join(", ", protos.Select(p => $"'{p.Iface.Name}'"))} whose prototypes do not all conform "
                    + $"to one another, so '{isym.Name}' cannot conform to all inherited interfaces — "
                    + $"{string.Join("; ", Mismatches(first, second))} (ISO §11.6.3 SR5; §9.3.10)");
            }
        }
    }

    /// <summary>⛔ THE PAIR-MODE ENTRY (kb/Work PB3101; COBOLNET_OO_DESIGN D6 item 7): are <paramref name="first"/> and
    /// <paramref name="second"/> described the same, as ISO §9.3.8.2.3 rules 2, 3, 6 and 7 ask of two interfaces'
    /// corresponding items? It describes both through the ONE builder (<see cref="ActivationDescriptions.Of"/>) and asks
    /// the ONE relation over the descriptions (<see cref="ActivationRelations.SignatureDifference"/>), the description
    /// identity the §9.3.6 match and the activation conformance read too, so a pair-mode rule cannot mean one thing here
    /// and another there. An item with no description (a group with no crossing form, a PICTURE-less item) is the same
    /// description as nothing (<see cref="ActivationDescriptions.NoDescriptionReason"/>). Asked by every pair-mode asker:
    /// <see cref="MethodConformanceMismatches"/> (override, IMPLEMENTS, interface to interface),
    /// <see cref="ValidatePropertyAccessorPairs"/> (§8.4.3.9.3 SR7), the prototype signature comparison
    /// (<see cref="PrototypeSignatures.Same"/>: §13.7.3 SR2, §10.6.2 SR2 / SR3, the SET pointer pairs and the signature
    /// classes of restricted prototype pointers) and the identity conversion of a variable-length MOVE's corresponding
    /// elementary tables (<c>MoveBinder.ElementMovesOf</c>). There is no <c>DataItem</c> comparator beside it (kb/Work
    /// PB3101). Null when the two
    /// are the same description, else the difference with the two sides named by <paramref name="firstRole"/> and
    /// <paramref name="secondRole"/>.</summary>
    public static string? SignatureDifference(DataItem first, DataItem second, string firstRole, string secondRole)
    {
        if (ActivationDescriptions.Of(first, classes: null) is not { } a)
            return ActivationDescriptions.NoDescriptionReason(first, firstRole);
        if (ActivationDescriptions.Of(second, classes: null) is not { } b)
            return ActivationDescriptions.NoDescriptionReason(second, secondRole);
        return ActivationRelations.SignatureDifference(a, b, firstRole, secondRole);
    }

    // ══ ACTIVE-CLASS — CONFORMANCE THAT DEPENDS ON HOW THE METHOD IS INVOKED (kb/Work PB1112) ═══════════════════════
    // An ACTIVE-CLASS formal or returning item means "the class of the object the method runs on", so the standard
    // states its conformance in terms of the INVOCATION: §14.8.2.3.2 rule 4 and §14.8.2.3.3's ACTIVE-CLASS paragraph
    // for a formal, §14.8.3.3 rule 2 for a returning item. The INVOKE binder describes that invocation ONCE as an
    // ObjectRefDescriptor — "invoked with" — the four cases §14.8.3.3 rule 2 b) enumerates: a class-name is that
    // object-class-name with ONLY (1.), SELF and SUPER are ACTIVE-CLASS (2.), an object reference is its own description
    // (3./4.). Before kb/Work PB1112 the formal was screened as a plain description (BY REFERENCE: identity; BY CONTENT:
    // SET SR14 into the formal) with the invocation never consulted, so rule 4 b)'s ONLY argument was refused and rule
    // 4 a)'s invocation condition never checked; the returning item was sent as ACTIVE-CLASS whatever the invocation.

    /// <summary>§14.8.2.3.2 rule 4 a) / §14.8.2.3.3 1): the method "shall be invoked with the predefined object
    /// references SELF or SUPER, or with an object reference described with the ACTIVE-CLASS phrase".</summary>
    public static bool InvokedThroughActiveClass(ObjectRefDescriptor invokedWith) =>
        invokedWith.Kind is ObjectRefKind.ActiveClass;

    /// <summary>§14.8.2.3.2 rule 4 b) / §14.8.2.3.3 2): the method "shall be invoked with an object-class-name or with
    /// an object reference described with an object-class-name and the ONLY phrase" — which class is
    /// <paramref name="invokedWith"/>'s name.</summary>
    public static bool InvokedThroughOnlyClass(ObjectRefDescriptor invokedWith) =>
        invokedWith is { Kind: ObjectRefKind.ObjectClass, Only: true };

    /// <summary>ISO §14.8.2.3.2 rule 4 (BY REFERENCE) and §14.8.2.3.3's ACTIVE-CLASS paragraph (BY CONTENT / BY
    /// VALUE) for an argument DESCRIPTION <paramref name="arg"/> meeting an ACTIVE-CLASS <paramref name="formal"/> of a
    /// method invoked with <paramref name="invokedWith"/>. Null when one of the two alternatives holds.
    /// <list type="bullet">
    ///   <item>BY REFERENCE a) — "an object reference described with the ACTIVE-CLASS phrase, where the presence or
    ///     absence of the FACTORY phrase is the same as in the formal parameter", invoked with SELF, SUPER or an
    ///     ACTIVE-CLASS reference; b) — "an object reference described with an object-class-name and the ONLY phrase"
    ///     with the formal's FACTORY presence, "and the method to be activated shall be invoked with that
    ///     object-class-name or with an object reference described with that object-class-name and the ONLY
    ///     phrase".</item>
    ///   <item>BY CONTENT 1) — invoked through ACTIVE-CLASS, and a SET of the argument into an ACTIVE-CLASS receiver
    ///     with the formal's FACTORY presence is valid (§14.9.39.3 SR14); 2) — invoked through an object-class-name
    ///     (ONLY), and a SET of the argument into "an object reference described with that object-class-name and the
    ///     ONLY phrase" with the formal's FACTORY presence is valid (SR12 a)1.).</item>
    /// </list>
    /// <paramref name="table"/> null means no class table in the group, where no SET rule is checked (the
    /// <see cref="ObjectRefAssignmentMismatch(OoClassTable?, PicInfo, PicInfo, bool)"/> convention).</summary>
    public static string? ActiveClassFormalMismatch(OoClassTable? table, ObjectRefDescriptor formal,
        ObjectRefDescriptor arg, ObjectRefDescriptor invokedWith, bool byReference)
    {
        string factory = formal.Factory ? "with" : "without";
        if (byReference)
        {
            if (arg.Kind is ObjectRefKind.ActiveClass && arg.Factory == formal.Factory
                && InvokedThroughActiveClass(invokedWith))
                return null;
            if (arg is { Kind: ObjectRefKind.ObjectClass, Only: true } && arg.Factory == formal.Factory
                && InvokedThroughOnlyClass(invokedWith)
                // A parameter-name may be substituted by that very class (§9.3.8.2.4; kb/Work PB2937).
                && (arg.Formal || CobolNames.Same(arg.Name, invokedWith.Name)))
                return null;
            return $"the formal parameter is described ACTIVE-CLASS ({factory} FACTORY) and the argument is "
                + $"{arg.Spelled}, invoked with {invokedWith.Spelled} — BY REFERENCE the argument shall be a) an "
                + $"ACTIVE-CLASS reference {factory} FACTORY, with the method invoked with SELF, SUPER or an "
                + $"ACTIVE-CLASS reference, or b) described with an object-class-name and ONLY, {factory} FACTORY, with "
                + "the method invoked with that object-class-name or a reference described with it and ONLY "
                + "(ISO §14.8.2.3.2 rule 4)";
        }
        string? why1 = !InvokedThroughActiveClass(invokedWith)
            ? "the method is not invoked with SELF, SUPER or an ACTIVE-CLASS reference"
            : table is null ? null
            : ObjectRefAssignmentMismatch(table, arg, ObjectRefDescriptor.ActiveClass(formal.Name, formal.Factory));
        if (why1 is null) return null;
        string? why2 = !InvokedThroughOnlyClass(invokedWith)
            ? "the method is not invoked with an object-class-name or a reference described with one and ONLY"
            : table is null ? null
            : ObjectRefAssignmentMismatch(table, arg,
                ObjectRefDescriptor.ObjectClass(invokedWith.Name!, formal.Factory, only: true));
        if (why2 is null) return null;
        return $"the formal parameter is described ACTIVE-CLASS ({factory} FACTORY) and the argument is "
            + $"{arg.Spelled}, invoked with {invokedWith.Spelled} — neither alternative holds: 1) {why1}; "
            + $"2) {why2} (ISO §14.8.2.3.3)";
    }

    /// <summary>ISO §14.8.3.3 rule 2 — the SENDING operand of a returning item's delivery. A returning item not
    /// described ACTIVE-CLASS sends itself (rule 1). One described ACTIVE-CLASS sends "an object reference described
    /// as follows" (rule 2 b)): 1. invoked with an object-class-name — "that same object-class-name and an ONLY
    /// phrase"; 2. with SELF or SUPER — "an ACTIVE-CLASS phrase"; 3. with a reference described with an
    /// interface-name — "a universal object reference"; 4. with any other object reference — "the same description as
    /// that object reference"; and "the presence or absence of the FACTORY phrase is the same as in the returning item
    /// of the activated element". <paramref name="invokedWith"/> already maps 1. and 2. (<see cref="InvokedThroughActiveClass"/>).</summary>
    public static ObjectRefDescriptor ReturningSender(ObjectRefDescriptor returning, ObjectRefDescriptor invokedWith) =>
        returning.Kind is not ObjectRefKind.ActiveClass ? returning
        : invokedWith.Kind is ObjectRefKind.ObjectClass or ObjectRefKind.ActiveClass
            ? invokedWith with { Factory = returning.Factory }
            : ObjectRefDescriptor.Universal;

    /// <summary>ISO §14.8.2.3.3 for an OBJECT-REFERENCE argument BY CONTENT / BY VALUE into an object-reference formal —
    /// the one pair the activation entry (<c>ParameterConformance.ContentConformanceReason</c>) does not ask the run-time
    /// relation of, because its answer reads the class table AS A PAIR (<see cref="OoClassTable.SetOperands"/>) and, for an
    /// ACTIVE-CLASS formal, how the method is invoked. "If the formal parameter is of class pointer or an object reference
    /// described without the ACTIVE-CLASS phrase, the conformance rules shall be the same as if a SET statement were
    /// performed in the activating runtime element with the argument as the sending operand and the corresponding formal
    /// parameter as the receiving operand" (<see cref="ObjectRefAssignmentMismatch(OoClassTable?, PicInfo, PicInfo, bool)"/>);
    /// an ACTIVE-CLASS formal takes one of the two alternatives, each a condition on the invocation AND a SET
    /// (<see cref="ActiveClassFormalMismatch"/>; <paramref name="invokedWith"/> is null for a CALL, whose formals are never
    /// ACTIVE-CLASS). Null when conformant.</summary>
    public static string? ObjectReferenceContentMismatch(OoClassTable? classes, PicInfo formal, PicInfo argument,
        ObjectRefDescriptor? invokedWith) =>
        formal.ObjectRef is { Kind: ObjectRefKind.ActiveClass } activeClass && invokedWith is { } invocation
            ? ActiveClassFormalMismatch(classes, activeClass, argument.ObjectRef ?? ObjectRefDescriptor.Universal, invocation,
                byReference: false)
            : ObjectRefAssignmentMismatch(classes, argument, formal);


    /// <summary>
    /// ⛔ ISO §14.9.39.3 SR8 / SR10 / SR12 / SR14 (SET Format 5) for a pair of object-reference descriptions this
    /// compilation can see — a SET, §14.8.3.3 rule 1 (RETURNING delivery follows the SET rules), §14.8.2.3.3 (BY CONTENT
    /// object-reference arguments) and §9.3.8.2.3 rule 5 (covariant override / implements RETURNING). The table itself
    /// is the runtime's <see cref="ObjectReferenceSet.Refusal"/>, the ONE copy the run time asks too for the pairs no
    /// single compilation sees (§9.3.6 match rule 6 across a universal invocation, a CALL located at run time; kb/Work
    /// PB2463); this resolves each description's class facts through the group's class table
    /// (<see cref="OoClassTable.SetOperand"/>) and asks it.
    /// <para><paramref name="activeClassSenderAdmitted"/> false is §9.3.8.2.3 rule 5's reading, which enumerates no
    /// ACTIVE-CLASS sender into an interface-typed or class-typed receiver (see <see cref="ObjectReferenceSet"/>).</para>
    /// <para>Null-tolerant on <paramref name="table"/> (no class table in the group ⇒ no OO checking — preserving the
    /// former <c>OoClasses?.</c> call shape). Returns null when the pair conforms, else the clause it violated.</para>
    /// <para>kb/Work PB389 renamed this from <c>ObjectRefWideningMismatch</c>: "widening" described only SR12 a)2.,
    /// which was the single rule the scalar descriptor could express.</para>
    /// </summary>
    public static string? ObjectRefAssignmentMismatch(OoClassTable? table, PicInfo sender, PicInfo receiver,
        bool activeClassSenderAdmitted = true)
        => table is null ? null
            : ObjectRefAssignmentMismatch(table, sender.ObjectRef ?? ObjectRefDescriptor.Universal,
                receiver.ObjectRef ?? ObjectRefDescriptor.Universal, activeClassSenderAdmitted);

    /// <summary>The descriptor-level form of <see cref="ObjectRefAssignmentMismatch(OoClassTable?, PicInfo,
    /// PicInfo, bool)"/>.</summary>
    public static string? ObjectRefAssignmentMismatch(OoClassTable table, ObjectRefDescriptor send,
        ObjectRefDescriptor recv, bool activeClassSenderAdmitted = true)
    {
        // A formal side stands for the actual most favourable to the pair (OoClassTable.SetOperands; kb/Work PB2937).
        var (s, r) = table.SetOperands(send, recv);
        return ObjectReferenceSet.Refusal(s, r, activeClassSenderAdmitted);
    }

    /// <summary>
    /// Which §14.9.39.3 syntax rule governs a SET format-5 statement whose sender is <b>object-class-name-1</b>
    /// (a class NAME, not identifier-4), given the RECEIVER's §13.18.60.2 kind. Every one of those rules names
    /// the receiver's description in its own precondition, so the answer is read off the receiver and never
    /// hard-coded:
    /// <list type="bullet">
    ///   <item><b>interface-name receiver</b> — SR11: "If object-class-name-1 is specified and the data item
    ///     referenced by identifier-3 is described with an interface-name that identifies the interface int-1,
    ///     the factory object of object-class-name-1 shall be described with an IMPLEMENTS clause that
    ///     references int-1."</item>
    ///   <item><b>object-class-name receiver</b> — SR13: "If object-class-name-1 is specified and the data item
    ///     referenced by identifier-3 is described with an object-class-name, the data item shall be described
    ///     with the FACTORY phrase…".</item>
    ///   <item><b>ACTIVE-CLASS receiver</b> — SR14, whose closed list of senders ("the data item referenced by
    ///     identifier-4 shall be one of the following") is an ACTIVE-CLASS reference, SELF and NULL: a class
    ///     name is in none of them.</item>
    ///   <item><b>universal receiver</b> — SR8 only, which constrains nothing; the table returns null and this
    ///     label is never rendered.</item>
    /// </list>
    /// <para>⚠ kb/Work PB451: the SET site used to print "(ISO §14.9.39.3 SR13)" for ALL FOUR, so a program
    /// refused under SR11 or SR14 was told it had broken a rule whose own precondition was false for it — and
    /// the sender's identity, which is §14.9.39.4 GR10, was attributed to SR13 as well.</para>
    /// </summary>
    public static string ClassNameSenderRule(ObjectRefKind receiverKind) => receiverKind switch
    {
        ObjectRefKind.Interface => "ISO §14.9.39.3 SR11",
        ObjectRefKind.ObjectClass => "ISO §14.9.39.3 SR13",
        ObjectRefKind.ActiveClass => "ISO §14.9.39.3 SR14",
        _ => "ISO §14.9.39.3 SR8",
    };
}
