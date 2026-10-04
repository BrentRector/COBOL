// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Compiler.Oo;

/// <summary>One covariant-return adapter pair (§9.3.8.2.3 rules 5a/5c2 — conforming COBOL that C# interface
/// implementation cannot express directly, because interface implementations demand the EXACT return type):
/// the emitter renders an EXPLICIT interface implementation <c>PROTO_RET I_CS.M(…) =&gt; this.M(…);</c> per
/// pair; <paramref name="Factory"/> marks the FACTORY side.</summary>
public readonly record struct AdapterPair(
    OoInterfaceSymbol Iface, OoMethodSymbol Proto, OoMethodSymbol Impl, bool Factory);

/// <summary>A BY CONTENT / BY VALUE argument that has NO STORAGE, described as the sending operand ISO §14.8.2.3.3
/// rule 2 names — the input of <see cref="OoConformance.ContentValueMismatch"/> (kb/Work PB1113).</summary>
/// <param name="Position">Its §14.9.25.3 Table 16 row.</param>
/// <param name="Operand">The bound operand the whole MOVE question reads (SR6/SR7/SR8 shapes), or null for a value
/// that has none (a boolean expression).</param>
/// <param name="IsExpression">An arithmetic expression — rule 2 a)'s COMPUTE sender. Its VALUE is a numeric sender
/// judged by Table 16 exactly like a numeric literal (<paramref name="Position"/>: noninteger, because an expression's
/// value carries no compile-time integer guarantee), but it has no character image, so a GROUP formal's §14.9.25.4 GR4
/// character copy has nothing to copy.</param>
/// <param name="Spelled">How a refusal names the argument.</param>
public readonly record struct ContentValue(Table16Operand Position, BoundOperand? Operand, bool IsExpression, string Spelled)
{
    /// <summary>A numeric literal: Table 16's numeric row, integer or noninteger by its own digits
    /// (<see cref="MoveTable16.SenderPosition"/> — the reader the written MOVE uses).</summary>
    public static ContentValue Numeric(BoundNumericLiteral literal) =>
        new(MoveTable16.SenderPosition(literal), literal, false, $"the numeric literal {literal.Text}");

    /// <summary>An arithmetic expression (or a numeric intrinsic function's value, §15.4): a numeric COMPUTE sender.</summary>
    public static ContentValue Arithmetic { get; } =
        new(new Table16Operand(PicCategory.Numeric, IsNonInteger: true), null, true, "an arithmetic expression");

    /// <summary>A boolean expression or a boolean literal: Table 16's boolean row (§8.8.2 — a boolean value).</summary>
    public static ContentValue Boolean { get; } =
        new(new Table16Operand(PicCategory.Boolean), null, false, "a boolean value");
}

/// <summary>
/// The OO conformance SERVICE (P9 — R3: validation moved OFF the pass-1 symbol table, which stays a pure
/// lookup structure): the §9.3.8.2 override-signature check, the §9.3.11 IMPLEMENTS pass (returning the
/// covariant <see cref="AdapterPair"/>s instead of mutating table state), the ONE strict identical-description
/// check (§14.8.2.3.2), its runtime descriptor projection (D-U3), and the §14.8.3.3-rule-1 object-reference
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
                    edition.Error("COBOLNET0829", $"{where}: {err}; ISO §11.7.3 SR9");
            }
    }

    /// <summary>⛔ ISO §8.4.3.9.3 SR7 — THE TWO ACCESSORS OF ONE PROPERTY DESCRIBE IT THE SAME (kb/Work PB1450): "The
    /// data description of the item specified in the RETURNING phrase of the get property method shall be the same as
    /// the data description of the item specified as the USING parameter of the set property method." Nothing
    /// compared them, so a GET RETURNING PIC 9(5) with a SET USING PIC X(3) compiled and the §8.4.3.9.4 temp (modelled
    /// on the GET's item) crossed a SET formal of another description. The comparison is the ONE strict
    /// identical-description check (<see cref="DescriptionMismatch"/>, pair mode — the relation the override and
    /// IMPLEMENTS signatures use, §9.3.8.2.3 rules 2/3 and 6), asked of each (GET, SET) pair AS THE CLASS SEES IT:
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
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var accessor in declared)
            {
                if (accessor.PropertyName is not { } prop || !seen.Add(prop)) continue;
                if (find(NamingConvention.GetAccessorName(prop)) is not { Binding.Returning: { } returning } get
                    || find(NamingConvention.SetAccessorName(prop)) is not { Binding.Formals: [{ } setFormal] } set)
                    continue;   // a missing accessor is §8.4.3.9.3 SR3/SR4's, decided where the polarity is known
                if (DescriptionMismatch(setFormal.Item, returning) is not { } why) continue;
                // The diagnostic stands at the accessor THIS class wrote, the later of the pair when it wrote both.
                using var at = edition.At((set.Owner == cls ? set : get).Ctx ?? accessor.Ctx);
                edition.Error(DiagnosticCatalog.PropertyAccessorDescriptionMismatch,
                    $"class '{cls.Name}', property '{prop}': the RETURNING item of the get property method "
                    + $"('{get.Owner.Name}') and the USING parameter of the set property method ('{set.Owner.Name}') are "
                    + $"not described the same — {why} (ISO §8.4.3.9.3 SR7)");
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
                        edition.Error("COBOLNET0841",
                            $"class '{cls.Name}': the {side}interface '{iface.Name}' requires a method "
                            + $"'{proto.Name}' and none is defined or inherited (ISO §9.3.11 — a class "
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
                            edition.Error("COBOLNET0841", $"class '{cls.Name}', method '{impl.Name}': {err}");
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
    /// <see cref="OoFormal.ByValue"/>, kb/Work PB1051); 2)/3) identical formal
    /// descriptions (<see cref="DescriptionMismatch"/>); 4) RETURNING presence; 5) the object-reference RETURNING
    /// (covariant — <see cref="ObjectRefAssignmentMismatch(OoClassTable, ObjectRefDescriptor, ObjectRefDescriptor, bool)"/>
    /// with rule 5's closed ACTIVE-CLASS list); 6) identical non-object RETURNING descriptions; 7) strongly-typed
    /// groups of the same type (inside <see cref="DescriptionMismatch"/>, <c>StrongTypeMismatch</c>, for every
    /// formal and a non-object RETURNING); 8) the OPTIONAL phrase (<see cref="OptionalMismatch"/>); 9) the RAISING
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
            if (DescriptionMismatch(m2.Binding!.Formals[i].Item, m1.Binding!.Formals[i].Item) is { } err)
                yield return $"formal #{i + 1}: {err} (ISO §9.3.8.2.3 rules 2/3 vs {counterpart} — "
                    + "identical descriptions; the C# projection cannot check this)";
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
            else if (DescriptionMismatch(pr, r) is { } rerr)
                yield return $"RETURNING: {rerr} (ISO §9.3.8.2.3 rule 6)";
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
                    && string.Equals(u.Name, t.Name, StringComparison.OrdinalIgnoreCase)) ? null : "a",
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
                    && string.Equals(u.Name, c.Name, StringComparison.OrdinalIgnoreCase)))
                return true;
        if (cls is null) return false;
        var implemented = table.ImplementsClosure(cls, factory: t.Factory);
        return other.Any(u => u.Kind is RaisingTargetKind.Interface
            && implemented.Any(i => string.Equals(i.Name, u.Name, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Rule 9 c) for one interface-name element <paramref name="t"/> of interface-1's phrase.</summary>
    private static bool InterfaceRaisingCovered(OoClassTable table, RaisingTarget t, IReadOnlyList<RaisingTarget> other)
    {
        var listed = other.Where(u => u.Kind is RaisingTargetKind.Interface).ToList();
        if (listed.Count == 0) return false;
        if (listed.Any(u => string.Equals(u.Name, t.Name, StringComparison.OrdinalIgnoreCase))) return true;
        if (table.FindInterface(t.Name) is not { } iface) return false;
        var seen = new HashSet<OoInterfaceSymbol>();
        var stack = new Stack<OoInterfaceSymbol>(iface.Inherits);
        while (stack.Count > 0)
        {
            var b = stack.Pop();
            if (!seen.Add(b)) continue;
            if (listed.Any(u => string.Equals(u.Name, b.Name, StringComparison.OrdinalIgnoreCase))) return true;
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
        if (string.Equals(interface1, interface2, StringComparison.OrdinalIgnoreCase)) return null;
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
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            string node = queue.Dequeue();
            if (!seen.Add(node)) continue;
            if (string.Equals(node, target, StringComparison.OrdinalIgnoreCase)) return true;
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
        var mine = new Dictionary<string, OoMethodSymbol>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in interface1.AllPrototypes()) mine.TryAdd(p.ExternalizedName, p);   // first declaration wins
        foreach (var m2 in interface2.AllPrototypes())
            if (!mine.TryGetValue(m2.ExternalizedName, out var m1)
                || MethodConformanceMismatches(table, m1, m2, $"the '{interface2.Name}' prototype",
                       interface1.Name, interface2.Name).Any())
                return false;
        return true;
    }

    /// <summary>The RUNTIME projection of the strict-conformance rule (D-U3 — the universal-dispatch
    /// wave): ONE descriptor string per description, computed at BIND time on both sides of a universal
    /// crossing; the generated <c>__CobolInvoke</c> switch compares for STRING EQUALITY — an argument mismatch
    /// means the method does not MATCH (§9.3.6 match rule 3, so resolution continues and ends in EC-OO-METHOD), a
    /// RETURNING mismatch within one match class is §14.9.23.4 GR7 c)'s EC-OO-UNIVERSAL (kb/Work PB1500; conformance
    /// through a universal receiver is checked at runtime, §9.3.8.2.1 NOTE). Intended invariant: descriptor equality ⇔
    /// <see cref="DescriptionMismatch"/> == null over every carried category — derived NEXT TO the one mismatch
    /// function so the two projections cannot drift (feedback_one_mechanism_per_job). ⚠ No unit test enumerates
    /// the categories (this comment used to say one did; none has ever existed): each axis is held only by the
    /// goldens that exercise it, and the variable-length-group axis DID drift — the comparator accepted, in pair
    /// mode, a pair whose descriptors ("V:" versus "S:") differ, until kb/Work PB1519
    /// (negative/w59h-vlg-override-fixed-group).
    /// Deliberate strictness deltas, both LOUD-fail directions (AS-BUILT notes): equality cannot express
    /// §14.8.2.2 rule 1's by-ref group-PREFIX leniency (a smaller formal group raises EC-OO-UNIVERSAL
    /// through universal where the TYPED path accepts a prefix), and JUSTIFIED is encoded on alphanumeric
    /// items while the group⇄alphanumeric image pairing carries none. Not-carried categories return the
    /// <c>T:!</c> sentinel — bind rejects them from universal crossings (0866) before any box exists, and
    /// the sentinel deliberately matches nothing the callee ever emits as a checked formal.</summary>
    public static string ConformanceDescriptor(DataItem item)
    {
        // A bit / national group is treated as an ELEMENTARY item at the activation boundary (§14.8.2.1 / §14.8.3.1
        // NOTE), never as the alphanumeric group the "S:" image pairing stands for (kb/Work PB1166): its key is its
        // group usage and its position count, so two such groups of one kind and size match and nothing
        // alphanumeric does. The elementary boolean / national items the comparator also pairs it with are not
        // carried through universal dispatch ("T:!" below) — the same loud strictness delta those items already have.
        if (item.IsAsIfElementary)
            return $"G:{item.GroupUsage}:{item.AsIfPic!.Length}";
        if (item.IsGroup)
            // A VARIABLE-LENGTH group crosses as its §8.5.1.12 component carrier, so its descriptor is that
            // layout's canonical signature (kb/Work PB204) — without it the item fell to the T:! sentinel and
            // bind refused every universal crossing of legal source (COBOLNET0866).
            return item.CurrentExtentImageCapable ? $"V:{VariableLengthCompatibility.Signature(item)}"
                : item.IsImageCapable ? $"S:{item.ImageWidth}:N" : "T:!";
        if (item.Pic is not { } p) return "T:!";
        return p.Category switch
        {
            // The WHOLE §13.18.60.2 description, not just the name (kb/Work PB389): two items described
            // `OBJECT REFERENCE C` and `OBJECT REFERENCE FACTORY OF C ONLY` are different descriptions and
            // §9.3.8.2.3 rule 2 c) makes them non-conformant, so their keys must differ.
            PicCategory.ObjectReference => "O:" + (p.ObjectRef ?? ObjectRefDescriptor.Universal).SignatureKey,
            PicCategory.Numeric =>
                $"N:{p.Usage}:{p.Digits}:{p.Scale}:{(p.Signed ? "S" : "U")}:{p.SignKind}:"
                + (item.BlankWhenZero ? "B" : "-") + ClauseKey(item),
            PicCategory.Alphanumeric =>
                // An ANY LENGTH item's length is runtime-varying (ISO §13.18.2 GR1) — encoded '*' so the pair
                // semantics track DescriptionMismatch (ANY LENGTH must MATCH between the sides; when both carry
                // it the length compare is void). Through UNIVERSAL dispatch a concrete argument never MATCHES an
                // ANY LENGTH formal (§9.3.6 match rule 3 e) names the ANY LENGTH clause), so it resolves no method;
                // an ANY LENGTH argument that does match one is §14.9.23.4 GR7 c)'s ban on the bound method, which
                // OoEmitter.EmitCobolInvokeCase raises as EC-OO-UNIVERSAL (kb/Work PB1500).
                // A picture that is not all X — alphabetic, edited, or mixed A/9/X — carries its PICTURE-clause
                // identity (kb/Work PB1166: PIC A(5) is not PIC X(5)); a plain X(n) item keeps the bare "S:n:J"
                // key the alphanumeric-group image pairs with. The one consequence is a documented LOUD delta: a
                // mixed-symbol category-alphanumeric item (PIC X9X) meeting an alphanumeric group, which the typed
                // path admits by §14.8.2.2 rule 1, raises EC-OO-UNIVERSAL through universal dispatch.
                $"S:{(item.IsAnyLength ? "*" : p.Length.ToString())}:{(item.Justified ? "J" : "N")}"
                + (item.IsDynamicLength ? $":D{item.DynMaxSize}:{item.DynStructure?.Name}" : "")
                + (IsAllX(p) ? "" : ClauseKey(item)),
            // ⛔ CLASS POINTER CROSSES TOO (kb/Work PB1137). §14.8.2.3.2's class-pointer paragraph makes a data-pointer
            // and a program-pointer conform to the same category, and "if either is a restricted pointer, both shall
            // be restricted and of the same type" — so the key is the category plus the restriction, the SAME pair
            // AddressDescriptor builds for an address-identifier argument. A program-pointer's restriction is keyed by
            // its prototype's NAME, where the typed path compares signatures: two same-signature prototypes under
            // different names raise EC-OO-UNIVERSAL here — a LOUD strictness delta, the by-ref group-prefix family.
            PicCategory.Pointer => DataPointerKey(StrongTypeModel.PointerRestriction(item)),
            PicCategory.ProgramPointer => ProgramPointerKey(p.RestrictedPrototypeName),
            _ => "T:!",
        };
    }

    /// <summary>The descriptor prefixes of the reference classes a RETURNING pair must share to MATCH (§9.3.6 match rules
    /// 6 and 7, kb/Work PB1500): an object-reference or pointer returning item is received by a SET, which admits only
    /// its own class and pointer category (§14.8.3.3 rule 1; §14.8.2.3.2's class-pointer paragraph), and every other
    /// description is received by a MOVE, which neither of those can take part in. Object references are one class here
    /// because which object classes a SET admits is a run-time question through a universal receiver
    /// (<c>CobolObject.NarrowUniversal</c>).</summary>
    public static readonly IReadOnlyList<string> ReturningReferenceClasses = ["O:", "P:D:", "P:P:"];

    /// <summary>The §9.3.6 rule 6/7 match class of a RETURNING <paramref name="descriptor"/>: the one
    /// <see cref="ReturningReferenceClasses"/> prefix it carries, or "" for the MOVE class. Two RETURNING items of one
    /// class can meet in a SET or MOVE and so MATCH; whether their descriptions are the SAME is then §14.8.3.3's
    /// conformance question, which §14.9.23.4 GR7 c) asks of the bound method.</summary>
    public static string ReturningMatchClass(string descriptor) =>
        ReturningReferenceClasses.FirstOrDefault(c => descriptor.StartsWith(c, StringComparison.Ordinal)) ?? "";

    /// <summary>The conformance descriptor of an ADDRESS-IDENTIFIER argument crossing a universal dispatch (kb/Work
    /// PB1137): §8.4.3.11.4 GR1 — "Data-address-identifier creates a unique data item of class pointer and category
    /// data-pointer" —
    /// restricted to the type of its operand when that is a strongly-typed group or a restricted data-pointer (GR2,
    /// <see cref="StrongTypeModel.AddressOfRestriction"/>) — and §8.4.3.13.4 GR1 a program-address-identifier one of
    /// category program-pointer, restricted by its prototype (GR3). Keyed exactly as
    /// <see cref="ConformanceDescriptor"/> keys a pointer ITEM, so the callee's formal compares for equality.</summary>
    public static string AddressDescriptor(BoundAddressOperand address) =>
        address.Data is { } data ? DataPointerKey(StrongTypeModel.AddressOfRestriction(data.Item))
        : ProgramPointerKey(address.Program!.Prototype);

    private static string DataPointerKey(StrongTypeModel.TypeRestriction r) =>
        "P:D:" + (r.IsRestricted ? r.Name!.ToUpperInvariant() : "*");

    private static string ProgramPointerKey(string? prototype) =>
        "P:P:" + (prototype is null ? "*" : prototype.ToUpperInvariant());

    /// <summary>The PICTURE-clause identity's descriptor suffix (<see cref="PictureClauseIdentity.Key"/>), or ""
    /// for an item with no PICTURE clause or an ANY LENGTH item (whose one-symbol picture is its length's
    /// placeholder — the comparator skips the identity for it too).</summary>
    private static string ClauseKey(DataItem item) =>
        item.Pic?.Clause is { } c && !item.IsAnyLength ? ":P" + c.Key : "";

    /// <summary>A plain <c>X(n)</c> picture — the only elementary shape whose descriptor is the bare image key.</summary>
    private static bool IsAllX(PicInfo p) => p.Clause is null || p.Clause.CharacterString.All(c => c == 'X');

    /// <summary>§14.8.2.2 rule 1 / §14.8.3.2: the elementary side of an alphanumeric-group pairing "shall be … an
    /// elementary item of category alphanumeric" — the CATEGORY (§8.5.2), so neither alphabetic (<c>PIC A</c>) nor
    /// alphanumeric-edited, both of which share <see cref="PicCategory.Alphanumeric"/> storage (kb/Work PB1166).</summary>
    private static bool IsCategoryAlphanumeric(PicInfo? p) =>
        p is { Category: PicCategory.Alphanumeric, IsAlphabetic: false, EditMask: null };

    /// <summary>ISO §14.8.2.3.2 "Additionally" b)/c) and §14.8.3.3 "Additionally" 2)/3): a BIT GROUP "matches an
    /// elementary bit data item described with the same number of boolean positions", a NATIONAL GROUP "matches an
    /// elementary data item of usage national described with the same number of national character positions" —
    /// because "A bit group or national group is treated as an elementary item" (§14.8.2.1 / §14.8.3.1 NOTE), and
    /// so is never the alphanumeric group §14.8.2.2 / §14.8.3.2 pair with an alphanumeric item (kb/Work PB1166:
    /// both directions were refused as "a group argument requires a group or alphanumeric formal", and a national
    /// group crossed into an alphanumeric group of equal width). Two such groups of one kind match when their
    /// position counts agree. <paramref name="group"/> is the side that IS a bit / national group.</summary>
    private static string? AsIfElementaryGroupMismatch(DataItem group, DataItem other)
    {
        var gp = group.AsIfPic!;
        string kind = group.GroupUsage is GroupUsage.Bit ? "bit group" : "national group";
        string unit = group.GroupUsage is GroupUsage.Bit ? "boolean" : "national character";
        if (other.IsAsIfElementary)
            return other.GroupUsage != group.GroupUsage
                ? $"a {kind} does not match a {(other.GroupUsage is GroupUsage.Bit ? "bit" : "national")} group"
                : other.AsIfPic!.Length != gp.Length
                    ? $"{kind} size mismatch ({gp.Length} vs {other.AsIfPic!.Length} {unit} positions)"
                    : null;
        if (other.IsGroup)
            return $"a {kind} is treated as an elementary item (ISO §14.8.2.1 / §14.8.3.1 NOTE), so it does not "
                + "match an alphanumeric group — that pairing requires an alphanumeric group item or an elementary "
                + "item of category alphanumeric (ISO §14.8.2.2 rule 1 / §14.8.3.2)";
        bool kindOk = group.GroupUsage is GroupUsage.Bit
            ? other.Pic is { Category: PicCategory.Boolean, Usage: Usage.Bit }
            : other.Pic is { Usage: Usage.National };
        if (!kindOk)
            return group.GroupUsage is GroupUsage.Bit
                ? "a bit group matches only an elementary bit data item (ISO §14.8.2.3.2 \"Additionally\" b) / "
                  + "§14.8.3.3 \"Additionally\" 2))"
                : "a national group matches only an elementary data item of usage national (ISO §14.8.2.3.2 "
                  + "\"Additionally\" c) / §14.8.3.3 \"Additionally\" 3))";
        return other.Pic!.Length != gp.Length
            ? $"{kind} size mismatch (the {kind} has {gp.Length} {unit} positions, the elementary item "
              + $"{other.Pic.Length} — they shall be the same number)"
            : null;
    }

    /// <summary>ISO §14.8.2.2 / §14.8.3.2 / §9.3.8.2.3 rule 7 — the activation boundary's strongly-typed
    /// sentence, written ONCE for every mode and every entry point: <i>"If either the formal parameter or the
    /// corresponding argument is a strongly-typed group item, both shall be of the same type."</i> "Same type"
    /// is §8.5.3.1's relation and is asked of the ONE model (<see cref="StrongTypeModel.SameType"/>) — never
    /// re-derived here. Null when the rule is satisfied or does not apply.</summary>
    /// <param name="formal">The formal parameter, or the SENDING returning item (§14.8.3.1 makes the activated
    /// element's item the sender).</param>
    /// <param name="arg">The argument / receiving returning item, or <see langword="null"/> when the operand is
    /// a REFERENCE-MODIFIED view rather than a data item — §8.4.3.3.4 GR6 makes such a view elementary
    /// alphanumeric, so it is of no type and can never be the formal's.</param>
    private static string? StrongTypeMismatch(DataItem formal, DataItem? arg)
    {
        bool formalStrong = StrongTypeModel.IsStrongGroup(formal);
        if (!formalStrong && !(arg is { } a0 && StrongTypeModel.IsStrongGroup(a0))) return null;
        if (arg is { } a && StrongTypeModel.SameType(formal, a)) return null;
        string strongSide = formalStrong ? "formal parameter / returning item" : "argument";
        return $"the {strongSide} is a strongly-typed group item, so both shall be of the SAME type "
            + "(ISO §14.8.2.2 / §14.8.3.2 / §9.3.8.2.3 rule 7; §8.5.3.1 makes two type declarations equivalent "
            + "only when they have the same type-name, the same presence or absence of EXTERNAL and STRONG, and "
            + "corresponding elementary items at the same relative position, of the same length, with the same "
            + "ALIGNED / BLANK WHEN ZERO / DYNAMIC LENGTH / JUSTIFIED / PICTURE / SIGN / SYNCHRONIZED / USAGE "
            + "clauses)";
    }

    /// <summary>The ONE strict IDENTICAL-DESCRIPTION check — §14.8.2.3.2 (BY REFERENCE parameters ONLY; BY
    /// CONTENT follows §14.8.2.3.3 COMPUTE/MOVE/SET rules in the binder mode dispatch) and §9.3.8.2
    /// override-signature validation. Identical = same category, the same ALIGNED / DYNAMIC LENGTH clauses
    /// (<see cref="DescriptionClauses"/>), the per-category clauses (numeric: USAGE + SIGN representation + BLANK
    /// WHEN ZERO + digits + scale + sign; alphanumeric: length + JUSTIFIED; LOCALE phrase; object reference: the
    /// whole descriptor), and the same PICTURE clause by its ONE identity (<see cref="PictureClauseIdentity"/> —
    /// the character-string, currency strings and DECIMAL-POINT IS COMMA; kb/Work PB1166); a bit / national group
    /// is an elementary item (<c>AsIfElementaryGroupMismatch</c>); an alphanumeric group: image crossing with equal
    /// character length (except the §14.8.2.2 rule-1 BY
    /// REFERENCE prefix case — <paramref name="byRefGroupPrefix"/> allows a SMALLER formal). Null when
    /// conformant. This strictness keeps BY REFERENCE marshaling TYPE-PRESERVING (the slice-2 design fact);
    /// CONTENT conversions qualify the owner class internal profiles instead.</summary>
    /// <param name="invokedWith">ACTIVATION through INVOKE only: how the object the method is activated ON is
    /// described (<see cref="InvokedWith"/>). An ACTIVE-CLASS formal's rule — §14.8.2.3.2 rule 4 — is a statement
    /// about the invocation as much as about the argument, so it is decided only when this is supplied; without it
    /// (the override / implements / prototype PAIR, §9.3.8.2.3 rule 2 d)) the two descriptions are compared.</param>
    public static string? DescriptionMismatch(DataItem formal, DataItem arg, bool byRefGroupPrefix = false,
        bool anyLengthActivationRelax = false, ObjectRefDescriptor? invokedWith = null)
    {
        // ⛔ THE STRONGLY-TYPED SENTENCE FIRST, and it governs every crossing this comparator answers for.
        // ONE sentence written three times: §14.8.2.2 "If either the formal parameter or the corresponding
        // argument is a strongly-typed group item, both shall be of the same type", §14.8.3.2 "If either of the
        // operands is a strongly-typed group item, both shall be of the same type" (the RETURNING pair), and
        // §9.3.8.2.3 rule 7 "If either of the corresponding formal parameters or returning items in interface-1
        // or interface-2 is a strongly-typed group item, both are of the same type". It also completes
        // §8.5.1.12.1's fixed-length sentence — "Two fixed-length groups are always compatible, UNLESS they are
        // strongly typed and have different type definitions" — whose strong half VariableLengthCompatibility
        // deliberately leaves to this caller. Measured missing before kb/Work PB427: a plain `01 G. 05 A PIC
        // X(4).` argument crossed BY REFERENCE into a `01 LF TYPE CT-T.` strong formal of the same width and
        // ran, defeating exactly the data integrity §8.5.3.3's restrictions exist to protect.
        if (StrongTypeMismatch(formal, arg) is { } strongWhy) return strongWhy;
        // ANY LENGTH (ISO §13.18.2). PAIR mode (the default — override/implements signatures and the universal
        // descriptor; the §9.3.8.2/§14.8.2 conformance tables :12177/:12247/:12335/:12383 list ANY LENGTH among
        // the clauses that shall be THE SAME between corresponding items): the clause must match between the
        // sides, and when both carry it the length compare is void — both lengths track the same runtime
        // argument (GR1). ACTIVATION mode (<paramref name="anyLengthActivationRelax"/> — INVOKE arguments
        // §14.8.2.3.2 rules d/e (:25375-25377), BY CONTENT (:25414 rule c), and RETURNING delivery §14.8.3.3
        // rules 4/5 (:25503-25505)): parameter 1 (the formal / the sending returning item) being ANY LENGTH
        // makes its length "considered to match" the other side's; the OTHER side being ANY LENGTH alone stays
        // a mismatch (rule e / rule 4 — the pairing must be declared on the formal/receiver too).
        if (!anyLengthActivationRelax && formal.IsAnyLength != arg.IsAnyLength)
            return "ANY LENGTH mismatch (the corresponding items shall have the same ANY LENGTH clause — "
                + "ISO §14.8.2/§9.3.8.2 conformance tables)";
        if (anyLengthActivationRelax && arg.IsAnyLength && !formal.IsAnyLength)
            return "the argument/receiver is described with ANY LENGTH — the corresponding formal/sender shall "
                + "be described with ANY LENGTH too (ISO §14.8.2.3.2 rule e / §14.8.3.3 rule 4)";
        // Relaxes ONLY the length compares below; category and JUSTIFIED checks stay (the §14.8.2 table row).
        bool anyLengthFormal = formal.IsAnyLength;

        // A bit / national group is an ELEMENTARY item here (§14.8.2.1 / §14.8.3.1 NOTE) — its own arm, before the
        // alphanumeric-group arms below can read it as one (kb/Work PB1166).
        if (formal.IsAsIfElementary) return AsIfElementaryGroupMismatch(formal, arg);
        if (arg.IsAsIfElementary) return AsIfElementaryGroupMismatch(arg, formal);

        if (formal.IsGroup)
        {
            // §14.8.2.2 rule 1 / §14.8.3.2: "an alphanumeric group item or an elementary item of category
            // alphanumeric" — the category, so a PIC A or an alphanumeric-edited argument is not one (PB1166).
            if (!(arg.IsGroup || IsCategoryAlphanumeric(arg.Pic)))
                return "a group formal requires a group argument or an elementary argument of category alphanumeric "
                    + "(ISO §14.8.2.2 rule 1 / §14.8.3.2)";
            // ⛔ §14.8.2.2 / §14.8.3.2, THE VARIABLE-LENGTH SENTENCE, BEFORE the capability screens below
            // (kb/Work PB204). "If either the formal parameter or the argument is a variable length group, the
            // formal parameter and the argument shall be compatible, as described in 8.5.1.12" — an ADMISSION
            // subject to a relation, not a prohibition, and §14.9.4.3 SR25 imports it into a Format-2 CALL. The
            // Tier-C arms below used to answer this case, so every such crossing was refused at compile time
            // (COBOLNET1688) however conforming it was; SR12 — "Identifier-2 shall not reference a
            // variable-length group" — is FORMAT 1's rule and reaches neither AS NESTED nor INVOKE.
            if (VariableLengthCompatibility.Mismatch(formal, arg) is { } vlWhy)
                return vlWhy;
            // The ONE Tier-C reason source (kb/Work PB164 — a hand-rolled string here went stale twice as the
            // island shrank; TierCIsland.Reason names the leaf kind the predicate actually tests). The predicate
            // is BoundaryImageCapable, not IsImageCapable: a COMPATIBLE variable-length group crosses through
            // its current-extent codec (kb/Work PB204), so only a group with no boundary image at all — a
            // pointer/object-class leaf, or a variable-length shape outside the current-extent gate — is loud.
            // ⛔ TWO STRONG GROUPS OF THE SAME TYPE ARE DECIDED (kb/Work PB1116): the strongly-typed sentence above is
            // the whole rule for them, and the Tier-C island is a CARRIER question, not a conformance one — a strong
            // group with an object-reference leaf crosses as its leaf vector (OoClassTable.LeafCarried). Before this
            // arm the legal population (§13.18.60.3 confines object references in groups to strong types) was refused.
            if (OoClassTable.LeafCarried(formal) && OoClassTable.LeafCarried(arg)) return null;
            if (arg.IsGroup && !arg.BoundaryImageCapable)
                return TierCIsland.Reason(arg, "argument group");
            if (!formal.BoundaryImageCapable)
                return TierCIsland.Reason(formal, "formal group");
            // ⛔ A VARIABLE-LENGTH PAIR HAS NO LENGTH OF ITS OWN TO COMPARE (kb/Work PB965) — the collapsed
            // ImageWidth below counts a dynamic-capacity table as one element, a convention §8.5.1.12.3 grants only
            // to two MATCHING dynamic-capacity tables. Which size rule applies is the activation mode's:
            //  • RETURNING (§14.8.3.2): the length sentence governs only when "neither of them is strongly typed
            //    or a variable length group" — compatibility, checked above, is the WHOLE rule.
            //  • BY REFERENCE (§14.8.2.2 rule 1): it binds only an ALPHANUMERIC group item, which a variable-length
            //    group is not (§3.11 "alphanumeric group item": "group item except for … a variable-length group
            //    item"). Two variable-length groups: compatibility is the whole rule. A fixed-length group opposite
            //    one: rule 1 binds the fixed side, measured in the lengths §8.5.1.12.3 sentence 3 gives the PAIR
            //    ("the dynamic-capacity table is considered to be the same length as the corresponding table").
            //  • Override/prototype SIGNATURE equality (§9.3.8.2.3 — neither flag): unchanged, strict equality.
            if (VariableLengthCompatibility.IsVariableLength(formal) || VariableLengthCompatibility.IsVariableLength(arg))
            {
                if (byRefGroupPrefix)
                {
                    if (VariableLengthCompatibility.IsVariableLength(formal)
                        && VariableLengthCompatibility.IsVariableLength(arg))
                        return null;
                    var (fw, aw) = VariableLengthCompatibility.PairCharWidths(formal, arg)!.Value;
                    return fw > aw
                        ? $"the formal ({fw} character positions) exceeds the argument ({aw}) (ISO §14.8.2.2 "
                          + "rule 1, in the lengths §8.5.1.12.3 gives the pair — the formal shall not be larger)"
                        : null;
                }
                if (anyLengthActivationRelax) return null;
                // ⛔ PAIR MODE — override / implements / prototype SIGNATURE equality (kb/Work PB1519, wave 59 H).
                // §9.3.5.3 item 7 makes the variable-length-group information PART OF THE METHOD RESOLUTION
                // SIGNATURE ("If the parameter is a variable-length group, sufficient information to determine if
                // this group would match the group is specified in the invoke statement"), and §11.7.3 SR3 demands
                // the SAME signature of an override. A fixed-length group carries no such information, so it is a
                // different signature from a variable-length group even when §8.5.1.12 would call the two
                // COMPATIBLE and their collapsed widths agree. Measured before this arm: a base formal
                // `05 T PIC X(2) OCCURS DYNAMIC …` overridden by `05 T PIC X(2) OCCURS 1.` passed the width compare
                // below and died in Roslyn (CS0115, "no suitable method found to override"), because the C#
                // projection — ConformanceDescriptor's "V:" versus "S:" — already told the two apart: this arm
                // restores the invariant that the descriptor and this comparator agree.
                if (VariableLengthCompatibility.IsVariableLength(formal) != VariableLengthCompatibility.IsVariableLength(arg))
                    return $"one is a variable-length group and the other a fixed-length group (the formal is "
                        + $"{(VariableLengthCompatibility.IsVariableLength(formal) ? "variable" : "fixed")}-length) — "
                        + "ISO §9.3.5.3 item 7 makes a variable-length group's matching information part of the "
                        + "method resolution signature, so the two signatures differ";
            }
            // §14.8.2.2 rule 1 (BY REFERENCE): the formal may be SMALLER than (a prefix of) the argument —
            // the callee sees the leading formal-width character positions; the tail survives write-back.
            // Override signatures and RETURNING pairs keep strict equality.
            return byRefGroupPrefix
                ? (formal.ImageWidth > arg.ImageWidth
                    ? $"the formal ({formal.ImageWidth} character positions) exceeds the argument "
                      + $"({arg.ImageWidth}) (ISO §14.8.2.2 rule 1 — the formal shall not be larger)"
                    : null)
                : arg.ImageWidth != formal.ImageWidth
                    ? $"character length mismatch (formal {formal.ImageWidth}, argument {arg.ImageWidth})"
                    : null;
        }
        if (formal.Pic is not { } f)
            return "the formal parameter has no resolvable description (PICTURE-less item — a later slice)";
        if (arg.IsGroup)
        {
            if (!IsCategoryAlphanumeric(f))
                return "a group argument requires a group formal or an elementary formal of category alphanumeric "
                    + "(ISO §14.8.2.2 rule 1 / §14.8.3.2)";
            // §8.5.1.12.1: a variable-length group is compatible only with a compatible GROUP ("not equivalent
            // to an alphanumeric data item"), so an ELEMENTARY formal — which §14.8.2.2 rule 1 admits for a
            // fixed-length group — is a mismatch, and it is that sentence that says so, not the Tier-C island.
            if (VariableLengthCompatibility.Mismatch(formal, arg) is { } vlWhy) return vlWhy;
            if (!arg.BoundaryImageCapable) return "the argument group has no character image (Tier-C)";
            if (anyLengthFormal) return null;   // §14.8.2.3.2 rule d — the formal's length matches the argument's
            return byRefGroupPrefix
                ? (f.Length > arg.ImageWidth
                    ? $"the formal ({f.Length} character positions) exceeds the argument "
                      + $"({arg.ImageWidth}) (ISO §14.8.2.2 rule 1)"
                    : null)
                : arg.ImageWidth != f.Length
                    ? $"character length mismatch (formal {f.Length}, argument {arg.ImageWidth})"
                    : null;
        }
        if (arg.Pic is not { } a)
            return "the argument has no resolvable description (PICTURE-less item — a later slice)";
        if (f.Category != a.Category)
            return $"category mismatch (formal {f.Category}, argument {a.Category})";
        // ── The clauses every category shares (kb/Work PB1166). ALIGNED and DYNAMIC LENGTH are entry clauses on
        // every identical-description list — §14.8.2.3.2 rule 2 "the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH,
        // JUSTIFIED, PICTURE, SIGN, and USAGE clauses", §9.3.8.2.3 rule 3 "the same ALIGNED, ANY LENGTH, BLANK
        // WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses" — asked through the ONE
        // predicate the §8.5.3.1 profile compare reads too.
        if (DescriptionClauses.AlignedOrDynamicLengthMismatch(formal, arg) is { } clauseWhy) return clauseWhy;
        // The per-category arm speaks first — its USAGE / SIGN / digit messages name the clause more precisely than
        // a character-string difference would — and the PICTURE clause's ONE identity (PictureClauseIdentity)
        // closes every pair the arm accepted: the expanded character-string (so PIC A(5) is not PIC X(5), and an
        // edited picture is not the plain one of equal size), the currency STRING each currency symbol stands for
        // (rule 2 a) — "Currency symbols match if and only if the corresponding currency strings are the same"),
        // and the DECIMAL-POINT IS COMMA state of each side's source element whenever a period or comma symbol is
        // present (rule 2 b)). Before this every category arm compared its own subset — the EditMask text, or
        // nothing — and a DPC class returning Z9,99 into a non-DPC Z9,99 receiver ran, the receiver reading 1234
        // for 12.34. An ANY LENGTH formal's one-symbol picture has no length of its own (§14.8.2.3.2
        // "Additionally" d)), so for it only the SYMBOL must agree.
        if (CategoryArmMismatch(formal, arg, f, a, anyLengthFormal, invokedWith) is { } armWhy) return armWhy;
        return f.Clause is { } fc && a.Clause is { } ac
            ? anyLengthFormal ? PictureClauseIdentity.MismatchAtAnyLength(fc, ac) : PictureClauseIdentity.Mismatch(fc, ac)
            : null;
    }

    /// <summary>The per-category clauses of <see cref="DescriptionMismatch"/>'s elementary pair (the two sides
    /// already share a category): object-reference descriptions, the numeric USAGE / SIGN / BLANK WHEN ZERO /
    /// digit profile, JUSTIFIED, the LOCALE phrase, lengths and the pointer restrictions. The PICTURE clause's
    /// identity is compared by the caller after this, for every category alike.</summary>
    private static string? CategoryArmMismatch(DataItem formal, DataItem arg, PicInfo f, PicInfo a, bool anyLengthFormal,
        ObjectRefDescriptor? invokedWith)
    {
        switch (f.Category)
        {
            case PicCategory.ObjectReference:
                // §9.3.8.2.3 rule 2 — the INVARIANT direction, and it names all four axes: a) universal ⇔
                // universal, b) the SAME interface-name, c) the same object-class-name "and the presence or
                // absence of the FACTORY and ONLY phrases is the same in both interfaces", d) ACTIVE-CLASS with
                // the same FACTORY presence. Before kb/Work PB389 this compared the class NAME alone, so a
                // FACTORY OF or ONLY difference passed as identical.
                var fd = f.ObjectRef ?? ObjectRefDescriptor.Universal;
                var ad = a.ObjectRef ?? ObjectRefDescriptor.Universal;
                // ⛔ An ACTIVE-CLASS formal of an ACTIVATION is §14.8.2.3.2 rule 4's, not rule 2 d)'s (kb/Work PB1112):
                // it admits an ONLY-described argument the pair rule refuses, and constrains the invocation the pair
                // rule cannot see.
                if (fd.Kind is ObjectRefKind.ActiveClass && invokedWith is { } iw)
                    return ActiveClassFormalMismatch(null, fd, ad, iw, byReference: true);
                return fd.SameDescriptionAs(ad) ? null
                    : $"object-reference description mismatch (formal {fd.Spelled}, argument {ad.Spelled} — "
                      + "§9.3.8.2.3 rule 2 requires the same kind, name, FACTORY presence and ONLY presence)";
            case PicCategory.Numeric:
                if (f.Usage != a.Usage)
                    return $"USAGE mismatch (formal {f.Usage}, argument {a.Usage} — §14.8.2.3.2 rule 2 "
                        + "requires the same USAGE clause BY REFERENCE)";
                if (f.SignKind != a.SignKind)
                    return $"SIGN clause mismatch (formal {f.SignKind}, argument {a.SignKind} — "
                        + "§14.8.2.3.2 rule 2: the SIGN clauses shall be the same)";
                if (formal.BlankWhenZero != arg.BlankWhenZero)
                    return "BLANK WHEN ZERO mismatch (§14.8.2.3.2 rule 2)";
                return f.Digits != a.Digits || f.Scale != a.Scale || f.Signed != a.Signed
                    ? $"numeric description mismatch (formal {(f.Signed ? "S" : "")}9({f.Digits}) scale "
                      + $"{f.Scale}, argument {(a.Signed ? "S" : "")}9({a.Digits}) scale {a.Scale})"
                    : null;
            case PicCategory.Alphanumeric:
                if (formal.Justified != arg.Justified)
                    return "JUSTIFIED mismatch (§14.8.2.3.2 rule 2)";
                // The PICTURE clause — alphabetic vs alphanumeric, edited vs plain — was compared above, through
                // its one identity. ANY LENGTH: the length is considered to match (§14.8.2.3.2 rule d / §14.8.3.3 rule 5 in
                // activation mode; both-sides-varying in pair mode — the top-of-function match rule).
                return !anyLengthFormal && f.Length != a.Length
                    ? $"length mismatch (formal X({f.Length}), argument X({a.Length}))"
                    : null;
            // ── The remaining PICTURE categories (ISO §14.8.2.3.2 rule 2, the SAME clause list the arms above
            // spell out per category). ⛔ THESE THREE USED TO FALL INTO A `default:` THAT ANSWERED "formal
            // category {c} is not yet carried across INVOKE", which made a category-boolean, category-national
            // or numeric-edited FORMAL PARAMETER impossible in every passing mode — BY REFERENCE, BY CONTENT
            // and bare alike (fix-queue PB46). Nothing was missing: all three are string-carried
            // (OoClassTable.StringCarried), so the marshaling arms already carried them; only this screen said
            // no. The standard contemplates them explicitly — §14.8.2.3.2's own lettered exceptions b and c
            // pair a BIT GROUP with an elementary bit item and a NATIONAL GROUP with an elementary national
            // item of the same position count.
            case PicCategory.NumericEdited:
            case PicCategory.National:
            case PicCategory.Boolean:
                if (formal.Justified != arg.Justified)
                    return "JUSTIFIED mismatch (§14.8.2.3.2 rule 2)";
                if (formal.BlankWhenZero != arg.BlankWhenZero)
                    return "BLANK WHEN ZERO mismatch (§14.8.2.3.2 rule 2)";
                // USAGE is a rule-2 clause in its own right, and for these categories it is NOT implied by the
                // category: a boolean item is USAGE DISPLAY or USAGE BIT (§13.18.60.3 SR5) and both map to the
                // same D-B1 character storage, so only this compare keeps the declarations identical.
                if (f.Usage != a.Usage)
                    return $"USAGE mismatch (formal {f.Usage}, argument {a.Usage} — §14.8.2.3.2 rule 2 "
                        + "requires the same USAGE clause BY REFERENCE)";
                if (f.SignKind != a.SignKind)
                    return $"SIGN clause mismatch (formal {f.SignKind}, argument {a.SignKind} — "
                        + "§14.8.2.3.2 rule 2: the SIGN clauses shall be the same)";
                // The EDITED character-string was compared above, through the PICTURE clause's one identity. A
                // FORMAT-2 (LOCALE) picture (§14.8.2.3.2 "Additionally" a) — PB64 T6) adds the LOCALE phrase: the
                // same SIZE phrase (the Length compare below carries it — Length = integer-1) and "both specify the
                // LOCALE phrase without a locale-name or both specify the LOCALE phrase with the same external
                // identification, where the external identification is the external-locale-name or literal value
                // associated with a locale-name" — the identification AS WRITTEN (LocaleRef.SameIdentificationAs;
                // kb/Work PB1166: it used to compare the L1-NORMALIZED tags, so "en-US" matched "EN_us.UTF-8").
                if ((f.LocaleEdit is not null) != (a.LocaleEdit is not null))
                    return "PICTURE mismatch (only one of the pair is a format 2 LOCALE picture — "
                        + "§14.8.2.3.2 rule 2 requires the same PICTURE clause)";
                if (f.LocaleEdit is { } fle && a.LocaleEdit is { } ale && !fle.Locale.SameIdentificationAs(ale.Locale))
                    return $"LOCALE mismatch (formal {fle.Locale.Named?.ToString() ?? "LOCALE (current)"}, argument "
                        + $"{ale.Locale.Named?.ToString() ?? "LOCALE (current)"} — both shall specify the "
                        + "LOCALE phrase without a locale-name or with the same external identification, the "
                        + "external-locale-name or literal value as written: ISO §14.8.2.3.2 \"Additionally\" a), "
                        + "§14.8.3.3 \"Additionally\" 1), §9.3.8.2.3 rule 3 \"Additionally\")";
                return !anyLengthFormal && f.Length != a.Length
                    ? $"length mismatch (formal {f.Category} ({f.Length}), argument {a.Category} ({a.Length}))"
                    : null;
            // ── Class pointer (ISO §14.8.2.3.2, the class-pointer paragraph): "If either the argument or the
            // formal parameter is of class pointer, the corresponding formal parameter or argument shall be of
            // class pointer and the corresponding items shall be of the same category" — which the
            // f.Category != a.Category compare above has already proven. A PICTURE-less pointer has no length,
            // USAGE variant or JUSTIFIED clause left to differ in. The SECOND sentence — "If either is a
            // restricted pointer, both shall be restricted and of the same type" — is enforced HERE, over BOTH
            // restriction models, because both are now declarable: `POINTER TO type-name-1` (§13.18.60.4 GR23,
            // kb/Work PB153) carries RestrictedTypeName, and `PROGRAM-POINTER TO program-prototype-name-1` /
            // `FUNCTION-POINTER TO function-prototype-name-1` (GR25/GR26, kb/Work PB452 + PB817) carry
            // RestrictedPrototypeName. Until they were declarable this was dead text under a staged-loud
            // declaration, and the comment that stood here said so; a rule whose subject becomes declarable and
            // whose screen does not follow is a silent under-rejection (feedback_scan_all_similar).
            case PicCategory.Pointer:
            case PicCategory.ProgramPointer:
            case PicCategory.FunctionPointer:
                if (!string.Equals(f.RestrictedTypeName, a.RestrictedTypeName, StringComparison.OrdinalIgnoreCase))
                    return $"restricted data-pointer mismatch (formal {PointerRestrictionText(f.RestrictedTypeName, "type")}, "
                        + $"argument {PointerRestrictionText(a.RestrictedTypeName, "type")} — §14.8.2.3.2: if either is a "
                        + "restricted pointer, both shall be restricted and of the same type)";
                if (!string.Equals(f.RestrictedPrototypeName, a.RestrictedPrototypeName, StringComparison.OrdinalIgnoreCase))
                    return $"restricted pointer mismatch (formal {PointerRestrictionText(f.RestrictedPrototypeName, "prototype")}, "
                        + $"argument {PointerRestrictionText(a.RestrictedPrototypeName, "prototype")} — §14.8.2.3.2: if either "
                        + "is a restricted pointer, both shall be restricted and of the same type)";
                return null;
            default:
                // Unreachable by construction: PicCategory.Group never reaches here (formal.IsGroup returned
                // above, and a group item has no PicInfo), and every other member has an arm. Pinned by
                // OoConformanceCategoryDriftTests — a NEW category must gain an arm, not fall in here, because
                // this arm REJECTS LEGAL SOURCE for whatever lands in it.
                return $"formal category {f.Category} has no §14.8.2.3.2 conformance rule";
        }
    }

    /// <summary>How one side of the §14.8.2.3.2 restricted-pointer compare reads in a diagnostic: the
    /// restriction operand, or "unrestricted" when the side carries none. The rule's failure mode is
    /// restricted-vs-unrestricted as often as it is two different names, so the message has to be able to say
    /// both.</summary>
    private static string PointerRestrictionText(string? restriction, string kind) =>
        restriction is null ? "unrestricted" : $"restricted to {kind} '{restriction}'";

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
                && string.Equals(arg.Name, invokedWith.Name, StringComparison.OrdinalIgnoreCase))
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

    // ══ ISO §14.8.2.3.3 — ELEMENTARY ITEMS PASSED BY CONTENT OR BY VALUE ═════════════════════════════════
    // ⛔ THE ONE HOME FOR THE RULE, for EVERY activation form that imports §14.8.2 (kb/Work PB165). It used
    // to live as `OoBinder.OoContentMismatch`, private to INVOKE — so the Format-2 CALL lane, which
    // §14.9.4.3 SR25 puts under the very same clause, had NO by-content screen at all and
    // `CobolArgAdapt`'s converting views silently adapted a non-conforming pair instead (measured on the
    // pre-fix tree: `CALL "S" AS NESTED USING BY CONTENT A` with `A PIC X(4)` and a `PIC 9(4)` formal
    // printed `LA=0000`). One rule written in one place is what keeps INVOKE and CALL from drifting, and
    // it is the same discipline `DescriptionMismatch` above already carries for §14.8.2.3.2.
    //
    // The clause selects the rule by the FORMAL's shape (rule 2 — the regime for a NESTED call, a program
    // prototype, a method or a function):
    //   a) numeric formal        → "the same as for a COMPUTE statement"
    //   b) index-data-item formal→ "the same as for a SET statement"
    //   c) ANY LENGTH formal     → "its length is considered to match the length of the corresponding argument"
    //   d) otherwise             → "the same as for a MOVE statement"  (⇒ §14.9.25.3 Table 16)
    // and, ahead of those, the class-pointer / object-reference paragraph: "the conformance rules shall be
    // the same as if a SET statement were performed in the activating runtime element with the argument as
    // the sending operand and the corresponding formal parameter as the receiving operand."
    // §14.8.2.2 rule 2 states the GROUP twin in the same words ("the same as for a MOVE statement"), which
    // is why one entry point answers for a group formal too.

    /// <summary>ISO §14.8.2.3.3 — the BY CONTENT / BY VALUE conformance rules for an IDENTIFIER argument,
    /// per formal category: COMPUTE for numeric (any numeric argument, fixed-point or floating-point, into a
    /// fixed-point or floating-point formal — kb/Work PB1114), SET for
    /// object references (widening — the argument's class shall be the receiver's class or a subclass), MOVE
    /// otherwise (§14.9.25.3 Table 16). Null when conformant.</summary>
    /// <param name="pointerAssignment">The ONE §14.9.39.3 verdict for a pointer identifier SET into a pointer receiver of
    /// its category (<c>SetBinder.PointerAssignmentReason</c>, receiver first): the class-pointer arm's SET question
    /// beyond the category — restriction and prototype signature, which need the binder's prototype tables.</param>
    /// <param name="invokedWith">INVOKE only — see <see cref="DescriptionMismatch"/>: an ACTIVE-CLASS formal takes
    /// §14.8.2.3.3's two ACTIVE-CLASS alternatives, each a condition on the invocation AND a SET.</param>
    public static string? ContentMismatch(OoClassTable? classes, Func<DataItem, DataItem, string?> pointerAssignment,
        DataItem formal, Place argPlace, ObjectRefDescriptor? invokedWith = null)
    {
        DataItem arg = argPlace.Item;
        // §14.8.2.2's strongly-typed sentence carries NO passing-mode qualification — it follows rules 1 (BY
        // REFERENCE) and 2 (BY CONTENT) and governs both, and §14.8.2.1 routes a strongly-typed group to
        // §14.8.2.2 because it is a group item. Same rule, same predicate, the other mode's entry point
        // (kb/Work PB427 — the two-arm question asked and answered). A REF-MOD argument is the elementary
        // alphanumeric view §8.4.3.3.4 GR6 creates, never the strong group itself, so it is screened as the
        // non-strong side it is.
        if (StrongTypeMismatch(formal, argPlace is RefModPlace ? null : arg) is { } strongWhy) return strongWhy;
        // §8.4.3.3.4 GR2/GR6 (fix-queue PB72): a REF-MOD argument crosses as the ELEMENTARY plain-alphanumeric
        // (or GR6b/c national) view it creates, never as the inner item — whose numeric category would satisfy
        // the COMPUTE arm below for a slice that is class alphanumeric, and whose finer alphabetic/edited flags
        // refused legal Table-16 crossings. The view's category comes from the ONE GR6 reader
        // (RefModPlace.CategoryOf); a view is elementary by definition, never a group.
        PicCategory? argCat = argPlace is RefModPlace rmp ? rmp.Category : arg.Pic?.Category;
        bool argIsGroup = argPlace.DenotedItem is not null && arg.IsGroup;

        if (formal.IsGroup || formal.Pic?.Category is PicCategory.Alphanumeric)
        {
            if (argIsGroup)
                // ⛔ §14.8.2.2's VARIABLE-LENGTH SENTENCE FIRST, exactly as the BY REFERENCE sibling
                // (DescriptionMismatch above) applies it — "If either the formal parameter or the argument is a
                // variable length group, the formal parameter and the argument shall be compatible, as
                // described in 8.5.1.12" — an ADMISSION subject to a relation, not a prohibition, and
                // §14.8.2.3.3 rule 2d routes a BY CONTENT group crossing through the same MOVE rules.
                // ⚠ This line used to ask `IsImageCapable`, which is false for a variable-length group, so it
                // refused legal source — the residue kb/Work PB818 filed against PB204's rim. PB165 had to
                // repair it here rather than leave it: the extraction made this the rule's ONE home and put the
                // Format-2 CALL lane behind it, which turned PB818's latent INVOKE-only defect into a hard
                // failure of the landed golden conformance:2023/pb204_vlg_boundary. The predicate is PB204's
                // own, unchanged.
                return VariableLengthCompatibility.Mismatch(formal, arg)
                    // Two strong groups of the same type (decided above) cross as their leaf vector (kb/Work PB1116).
                    ?? (arg.BoundaryImageCapable || (OoClassTable.LeafCarried(formal) && OoClassTable.LeafCarried(arg))
                        ? null : TierCIsland.Reason(arg, "argument group"));
            // ⛔ §14.8.2.3.3 rule 2d IS THE WHOLE MOVE QUESTION, ASKED OF THE ONE CHAIN (kb/Work PB878). This
            // arm was a hand list of sender categories — a fourth private copy of Table 16's alphanumeric column
            // that could not see the ALPHABETIC column (numeric-edited → PIC A is "No"), SR8 (a BINARY-LONG
            // argument at a PIC X formal) or a group formal's §14.9.25.4 GR4 conversion-free copy.
            return MoveContentMismatch(formal, argPlace);
        }
        var f = formal.Pic!;
        // ⛔ RULE 2b BEFORE RULE 2a (kb/Work PB1113). "If the formal parameter is an index data item, the conformance
        // rules are the same as for a SET statement with the argument as the sending operand" — and an index data
        // item carries PicCategory.Numeric, so the COMPUTE arm below used to answer for it and admitted a PIC 9(4)
        // argument. SET Format 1 with a class-index receiver takes identifier-2 "of class index" (§14.9.39.3 SR2)
        // and refuses arithmetic-expression-1 (SR3); an identifier argument is identifier-2.
        if (f.Usage is Usage.Index)
            return argCat is PicCategory.Numeric && arg.Pic is { Usage: Usage.Index } && !argIsGroup ? null
                : IndexFormalRefusal("the argument");
        return f.Category switch
        {
            // The numeric/float/object arms key on the VIEW category — a ref-mod view is never numeric or an
            // object reference (GR6c), so their arg.Pic detail reads are only reachable for a whole-item arg.
            // ⛔ RULE 2a IS "the same as for a COMPUTE statement", AND A COMPUTE TAKES ANY NUMERIC SENDER —
            // fixed-point or floating-point, either direction. The float restrictions that used to sit here
            // ("a float formal takes the identical float usage BY CONTENT") were an INVOKE CARRIER limitation,
            // not a conformance rule, and moving them into the shared rule REJECTED LEGAL SOURCE the moment
            // the CALL lane started asking: kb/Work PB238 landed the float crossing for a Format-2 CALL
            // deliberately (§14.2.3 GR10's "COMPUTE statement without the ROUNDED phrase" makes
            // `01 F FLOAT-LONG VALUE 1.5` reach a `PIC S9(3)V99` BY VALUE formal as 001.50), and the landed
            // golden conformance:2023/pb238_call_format2_operands proves it. The INVOKE carrier now lands a
            // fixed-point or other-usage float argument in a float formal through FloatResultant too
            // (OoEmitter.IsFloatLanding; kb/Work PB1114), so no carrier residue is left to screen.
            PicCategory.Numeric =>
                argIsGroup ? "a group argument does not conform to a numeric formal (§14.8.2.3.3)"
                : argCat is PicCategory.Numeric ? null
                : "COMPUTE-rule conformance needs a numeric argument (ISO §14.8.2.3.3 rule 2a)",
            // A CLASS-POINTER formal takes the SET rules, not the MOVE rules (§14.8.2.3.3: "If the formal parameter
            // is of class pointer or an object reference described without the ACTIVE-CLASS phrase, the conformance
            // rules shall be the same as if a SET statement were performed"), and a SET of an identifier into a
            // pointer is category-to-same-category (§14.9.39.3 SR17 data-pointer, SR20 function-pointer, SR21
            // program-pointer). This arm used to fall into the default MOVE arm, where Table 16 happened to admit the pair;
            // once that arm asks §14.9.25.3 SR1 (kb/Work PB970 arm 2) it would refuse every pointer argument.
            // ⛔ AND THE WHOLE SET, NOT ITS CATEGORY SCREEN ALONE (kb/Work PB1063): SR19 refuses a restricted
            // data-pointer stored into an unrestricted one (and the converse), SR22 an unrestricted or differently
            // signed program-pointer into a restricted one, SR20 a function-pointer of another signature — the
            // argument is the SENDER and the formal the RECEIVER, the same direction the SET binder asks.
            PicCategory.Pointer or PicCategory.ProgramPointer or PicCategory.FunctionPointer =>
                argCat == f.Category ? pointerAssignment(formal, arg)
                : $"a {f.Category} formal takes an argument of the same pointer category (SET rules, §14.8.2.3.3)",
            PicCategory.ObjectReference =>
                argCat is not PicCategory.ObjectReference || arg.Pic is not { } ap
                    ? "an object-reference formal takes an object-reference argument (SET rules, §14.8.2.3.3)"
                : f.ObjectRef is { Kind: ObjectRefKind.ActiveClass } afd && invokedWith is { } iw
                    ? ActiveClassFormalMismatch(classes, afd, ap.ObjectRef ?? ObjectRefDescriptor.Universal, iw,
                        byReference: false)
                : ObjectRefAssignmentMismatch(classes, ap, f),
            // ⭐ BOOLEAN / NATIONAL / NUMERIC-EDITED FORMALS ASK TABLE 16, NOT STRICT IDENTITY (fix-queue PB53).
            // This arm used to call DescriptionMismatch — which is §14.8.2.3.2, the BY **REFERENCE** rule —
            // described in its own comment as a "conservative strict gate". It was not conservative, it was the
            // WRONG CLAUSE: §14.8.2.3.3 rule 2d says a BY CONTENT crossing whose formal is not numeric, not an
            // index item and not ANY LENGTH conforms "as for a MOVE statement", i.e. by §14.9.25.3 Table 16.
            // Identity is far narrower, so three pairings the standard admits were refused with a "category
            // mismatch" naming a rule that does not govern the crossing:
            //     boolean → national · alphanumeric → boolean · national → boolean
            // ⛔ ANY LENGTH RELAXES LENGTH, AND ONLY LENGTH (kb/Work PB1113). §14.8.2.3.3 rule 2c makes such a
            // formal's length "considered to match the length of the corresponding argument" — a statement about
            // LENGTH that leaves the category pair to rule 2d's MOVE question, which never asks a length. This arm
            // used to answer an ANY LENGTH formal with "conformant" outright, so a PIC 9V9 argument reached a
            // PIC N ANY LENGTH formal (Table 16: numeric noninteger → national is "No") and printed its digits.
            _ => MoveContentMismatch(formal, argPlace),
        };
    }

    /// <summary>§14.8.2.3.3 rule 2b's refusal for a sender that is not an index data item — the ONE wording the
    /// identifier lane (<see cref="ContentMismatch"/>) and the value lane (<see cref="ContentValueMismatch"/>) share.</summary>
    private static string IndexFormalRefusal(string sender) =>
        $"§14.8.2.3.3 rule 2b transfers a value into an index data item by the SET rules, and {sender} is not a data "
        + "item of class index (ISO §14.9.39.3 SR2: \"Identifier-2 shall reference a data item of class index\"; SR3 "
        + "refuses arithmetic-expression-1, a literal or an expression, into a class-index receiver)";

    /// <summary>ISO §14.8.2.3.3 rule 2d — "Otherwise, the conformance rules are the same as for a MOVE statement
    /// with the argument as the sending operand and the corresponding formal parameter as the receiving operand"
    /// — asked as the WHOLE §14.9.25.3 question (<see cref="MoveTable16.Validity(BoundOperand, Table16Operand, DataItem)"/>:
    /// SR2, SR8, SR9 and Table 16), never as Table 16 alone (kb/Work PB878: a BINARY-LONG argument at a
    /// non-numeric formal (SR8) and a variable-length-group argument at an incompatible formal (SR9) were
    /// admitted where the written MOVE of the same pair is refused). ONE call for both the alphanumeric-formal and
    /// the other-category arms of <see cref="ContentMismatch"/>. Null when conformant.</summary>
    private static string? MoveContentMismatch(DataItem formal, Place argPlace)
    {
        var sender = new BoundFieldOperand(argPlace);
        // ⛔ SR1 FIRST — the MOVE question's class screen, which MoveTable16.Validity leaves to its askers because
        // each frames it differently (kb/Work PB970 arm 2). Rule 2d is "the same as for a MOVE statement", and a
        // MOVE whose sending operand is of class pointer or object is refused by §14.9.25.3 SR1 before Table 16 is
        // consulted. Without it a POINTER argument BY CONTENT to a PIC X(8) formal of a NESTED or prototyped
        // activation compiled — the runtime then delivered the pointer's storage image, which is §14.8.2.3.3
        // rule 1's answer for a rule-1 activation only, on source rule 2 says is in error.
        return MoveTable16.SenderClassRefusal(sender)
            ?? MoveTable16.Validity(sender, Table16Operand.Of(formal), formal)?.Reason;
    }

    /// <summary>
    /// ⛔ THE ONE ISO §14.8.2.3.3 VERDICT FOR A SENDING VALUE WITH NO STORAGE — a numeric literal, an arithmetic
    /// expression, or a boolean expression or literal (kb/Work PB1113). Each shape is DESCRIBED as the sending operand
    /// it is and asked the question rule 2 asks an identifier (<see cref="ContentMismatch"/>), in rule 2's order:
    /// <list type="bullet">
    /// <item>2 b) an index data item formal — the SET rules: "Identifier-2 shall reference a data item of class index"
    /// (§14.9.39.3 SR2) and arithmetic-expression-1 is refused into a class-index receiver (SR3), so no value qualifies;</item>
    /// <item>2 a) a numeric formal — the COMPUTE rules: any numeric sending operand, fixed-point or floating-point
    /// (kb/Work PB1114), and nothing else (§8.8.1.1);</item>
    /// <item>2 c) an ANY LENGTH formal — its length "is considered to match", which is about LENGTH only, so it falls
    /// through to</item>
    /// <item>2 d) the MOVE rules — the whole §14.9.25.3 question for an operand that has one
    /// (<see cref="MoveTable16.Validity(BoundOperand, Table16Operand, DataItem)"/>), Table 16 itself for a boolean
    /// value, which has no bound operand and no data item for SR2/SR8/SR9 to read.</item>
    /// </list>
    /// <para>⛔ IT REPLACED THREE HAND-WRITTEN ADMISSION LISTS, each narrower than the rule: the numeric-literal list
    /// admitted only a numeric or an UNSIGNED-INTEGER-into-alphanumeric formal (so <c>12.5</c> into <c>PIC ZZ9.99</c>,
    /// <c>12</c> into <c>PIC N(4)</c> and <c>-5</c> into <c>PIC X(4)</c> were refused, where Table 16 says Yes to each),
    /// the boolean list refused a national formal "on purpose", and both admitted an index-data-item formal through the
    /// numeric arm. One question asked of a described sender is what keeps the next category automatic.</para>
    /// <para>⛔ AN ARITHMETIC EXPRESSION IS A NUMERIC SENDER, ASKED RULE 2 d)'S QUESTION OF ITS VALUE (kb/Work PB1946,
    /// verdict kb/Work PB1936). Rule 2 d) applies the MOVE rules "with the argument as the sending operand" — the
    /// clause's own words for an ARGUMENT, which §14.9.4.3 SR17 says includes "any identifier specified in
    /// arithmetic-expression-1" — so the expression's value is the sender, the numeric category a numeric literal is.
    /// An expression into a numeric-edited formal is therefore Table 16's "Yes" and edits, and into an alphanumeric or
    /// national formal it takes the NONINTEGER numeric row's "No" (an expression's value carries no compile-time integer
    /// guarantee, the principle §15.2 gives a NUMERIC function, <see cref="MoveTable16.SenderPosition"/>). It used to be
    /// refused for EVERY non-numeric formal on the reading that a MOVE's sending operand is an identifier or a literal
    /// (§14.9.25.2), which is MOVE's own syntax and not the clause's argument. Null when conformant.</para>
    /// </summary>
    public static string? ContentValueMismatch(DataItem formal, ContentValue sender)
    {
        // §14.8.2.3.3's SET paragraph: no SET format sends a numeric, boolean or arithmetic VALUE into a class-pointer or
        // object-reference formal. ONE refusal for every value shape, whichever lane asks.
        if (SlotWindow.CarriedBySlot(formal))
            return "§14.8.2.3.3 transfers a value into a formal parameter of class pointer or object reference by the SET "
                + $"rules, and {sender.Spelled} is not a sending operand of any SET format";
        if (formal is { IsGroup: false, Pic.Usage: Usage.Index }) return IndexFormalRefusal(sender.Spelled);
        if (formal is { IsGroup: false, Pic.Category: PicCategory.Numeric })
            return sender.Position.Category is PicCategory.Numeric ? null
                : $"§14.8.2.3.3 rule 2a transfers a value into a numeric formal parameter by the COMPUTE rules, and "
                  + $"{sender.Spelled} is not a numeric sending operand (ISO §8.8.1.1)";
        // A group formal takes §14.9.25.4 GR4's character copy "with no conversion", and an expression has no description
        // whose characters could be copied (a numeric literal has its own digits, a boolean value its bit string).
        if (formal.IsGroup && sender.IsExpression)
            return $"§14.8.2.2 rule 2 transfers {sender.Spelled} into a group formal parameter by the MOVE rules, whose "
                + "group move copies the sending operand's characters (ISO §14.9.25.4 GR4), and an arithmetic expression "
                + "has no character image";
        var receiver = Table16Operand.Of(formal);
        string? why = sender.Operand is { } op
            ? MoveTable16.Validity(op, receiver, formal)?.Reason
            : MoveTable16.Refusal(sender.Position, receiver);
        return why is null ? null : $"§14.8.2.3.3 rule 2d transfers {sender.Spelled} by the MOVE rules: {why}";
    }

    /// <summary>The three sender categories a bound NONNUMERIC literal can actually be — §8.3.3.2 alphanumeric
    /// (including the hexadecimal format), §8.3.3.5 national and §8.3.3.4 boolean. ⛔ The bound tree renders all
    /// three as <c>BoundStringLiteral</c>, which carries the VALUE and not the CATEGORY, so a consumer holding
    /// only the bound node cannot tell them apart (kb/Work PB165 — measured: screening every such literal as
    /// alphanumeric refused `CALL … USING BY CONTENT N"AB" BY CONTENT B"01"` against `PIC N(2)` / `PIC 1(2)`
    /// formals, which are conforming Table-16 crossings). Until the literal's category rides the bound node,
    /// the conformance rule below refuses only what NO reading of rule 2d can admit.</summary>
    private static readonly Table16Operand[] NonNumericLiteralSenders =
    [
        new(PicCategory.Alphanumeric), new(PicCategory.National), new(PicCategory.Boolean),
    ];

    /// <summary>ISO §14.8.2.3.3 rule 2d for a NONNUMERIC literal argument — "the conformance rules are the same
    /// as for a MOVE statement", i.e. §14.9.25.3 Table 16 with the literal as the sending operand. Null when
    /// conformant. An ELEMENTARY formal only: a group formal is §14.8.2.2 rule 2's WHOLE MOVE question (SR2's strong
    /// type and SR9's variable-length group included), which <c>ParameterConformance.ContentConformanceReason</c> asks
    /// of <see cref="MoveTable16.Validity(BoundOperand, Table16Operand, DataItem?)"/> before it reaches here (kb/Work PB1617).
    /// <para>The verdict is Table 16's, asked once per sender category the literal could be
    /// (<see cref="NonNumericLiteralSenders"/>) — conformant when ANY of them is admitted. That is deliberately
    /// weaker than the rule the standard states for a KNOWN category, and it is the honest strength for a bound
    /// node that has lost the category: it still refuses the shape this screen exists for (a nonnumeric literal
    /// at a numeric or numeric-edited formal, which no sender category rescues) and it never rejects legal
    /// source. Narrowing it to the exact category is what carrying the category on the literal node buys.</para>
    /// </summary>
    public static string? ContentAlphanumericLiteralMismatch(DataItem formal)
    {
        var receiver = Table16Operand.Of(formal);
        return NonNumericLiteralSenders.Any(s => MoveTable16.Refusal(s, receiver) is null)
            ? null
            : "a nonnumeric literal argument has no conforming MOVE into this formal parameter under any "
              + "literal category (ISO §14.8.2.3.3 rule 2d / §14.9.25.3 Table 16)";
    }

    /// <summary>ISO §14.8.2.3.3 rule 2d for a nonnumeric literal argument whose CATEGORY the bound node carries
    /// (<c>BoundStringLiteral.Category</c> — an alphanumeric literal, plain or hexadecimal, a national literal or a
    /// boolean literal): §14.9.25.3 Table 16 asked of THAT sender, which is what carrying the category buys over
    /// <see cref="ContentAlphanumericLiteralMismatch"/>'s any-category reading (kb/Work PB1137 — a national literal at
    /// an alphanumeric formal is Table 16's "No", and the any-category reading admitted it because an ALPHANUMERIC
    /// literal would have moved). An ELEMENTARY formal only — a group formal takes the whole MOVE question first
    /// (<see cref="ContentAlphanumericLiteralMismatch"/>'s remark; kb/Work PB1617). Null when conformant.</summary>
    public static string? ContentNonNumericLiteralMismatch(DataItem formal, PicCategory literalCategory)
    {
        return MoveTable16.Refusal(new Table16Operand(literalCategory), Table16Operand.Of(formal)) is { } why
            ? $"a {literalCategory.ToString().ToLowerInvariant()} literal argument has no conforming MOVE into this "
              + $"formal parameter: {why} (ISO §14.8.2.3.3 rule 2d)"
            : null;
    }


    /// <summary>
    /// ⛔ THE ONE SENDER-INTO-RECEIVER TABLE FOR OBJECT REFERENCES — ISO §14.9.39.3 SR10 / SR12 / SR14 (SET
    /// format 5), reached also by §14.8.3.3 rule 1 (RETURNING delivery follows the SET rules), §14.8.2.3.3 (BY
    /// CONTENT object-reference arguments) and §9.3.8.2.3 rule 5 (covariant override/implements RETURNING).
    /// The receiver's DESCRIPTION selects the rule; the sender's description answers it.
    /// <list type="bullet">
    ///   <item><b>universal receiver</b> — SR8: "identifier-3 shall be any item of class object that is
    ///     permitted as a receiving item"; GR22 b) makes its content "a reference to any object". Nothing to
    ///     check.</item>
    ///   <item><b>interface-name receiver</b> — SR10: a) an interface identifying int-1 or inheriting from it;
    ///     b) an object-class-name whose b)1. FACTORY object (FACTORY written) or b)2. instance objects
    ///     implement int-1; c) an ACTIVE-CLASS reference, same two legs over the CONTAINING class.</item>
    ///   <item><b>object-class-name receiver</b> — SR12: a) an object-class-name sender, a)1. ONLY ⇒ the sender
    ///     is ONLY and names the SAME class, a)2. no ONLY ⇒ the same class or a subclass, a)3. FACTORY presence
    ///     equal; b) an ACTIVE-CLASS sender, b)1. the receiver is not ONLY, b)2. the sender's containing class
    ///     is the receiver's class or a subclass, b)3. FACTORY presence equal.</item>
    ///   <item><b>ACTIVE-CLASS receiver</b> — SR14: a) an ACTIVE-CLASS sender "where the presence or absence of
    ///     the FACTORY phrase is the same as in the data item referenced by identifier-3". (The rule as printed
    ///     constrains only the FACTORY axis — the containing class is necessarily the same one, since both
    ///     operands are written inside the same class definition, §13.18.60.3 SR16.)</item>
    /// </list>
    /// <para>The SELF and NULL senders of SR10 d)/e), SR12 c)/d) and SR14 b)/c) are not DESCRIPTIONS and are
    /// adjudicated at the SET site (<c>OoBinder.OoBindSetObjectRef</c>), which is also where SR11/SR13's
    /// class-NAME sender lives.</para>
    /// <para><paramref name="activeClassSenderAdmitted"/> is the ONE place the two rule sets differ.
    /// §14.9.39.3 SR10 c) and SR12 b) admit an ACTIVE-CLASS sender into an interface-typed or class-typed
    /// receiver; §9.3.8.2.3 rule 5 b)/c), the interface-conformance twin, enumerates no ACTIVE-CLASS leg — its
    /// 5 d) pairs ACTIVE-CLASS only with ACTIVE-CLASS. The interface-conformance callers therefore pass false
    /// and get the printed rule 5, rather than a shared function quietly widening one of the two.</para>
    /// <para>Null-tolerant on <paramref name="table"/> (no class table in the group ⇒ no OO checking —
    /// preserving the former <c>OoClasses?.</c> call shape). Returns null when the pair conforms, else the
    /// clause it violated.</para>
    /// <para>kb/Work PB389 renamed this from <c>ObjectRefWideningMismatch</c>: "widening" described only
    /// SR12 a)2., which was the single rule the scalar descriptor could express.</para>
    /// </summary>
    public static string? ObjectRefAssignmentMismatch(OoClassTable? table, PicInfo sender, PicInfo receiver,
        bool activeClassSenderAdmitted = true)
        => table is null ? null
            : ObjectRefAssignmentMismatch(table, sender.ObjectRef ?? ObjectRefDescriptor.Universal,
                receiver.ObjectRef ?? ObjectRefDescriptor.Universal, activeClassSenderAdmitted);

    /// <summary>The descriptor-level form of <see cref="ObjectRefAssignmentMismatch(OoClassTable?, PicInfo,
    /// PicInfo, bool)"/> — the SET format-5 table itself.</summary>
    public static string? ObjectRefAssignmentMismatch(OoClassTable table, ObjectRefDescriptor send,
        ObjectRefDescriptor recv, bool activeClassSenderAdmitted = true)
    {
        switch (recv.Kind)
        {
            // ── SR8 / GR22 b): a universal receiver constrains nothing. ──────────────────────────────────
            case ObjectRefKind.Universal:
                return null;

            // ── SR10: the receiver is described with an interface-name that identifies int-1. ────────────
            case ObjectRefKind.Interface:
            {
                if (table.FindInterface(recv.Name!) is not { } int1)
                    return $"unresolvable interface '{recv.Name}' in the receiver's description";
                switch (send.Kind)
                {
                    // a) an object reference described with an interface-name identifying int-1 or an
                    //    interface inheriting from int-1.
                    case ObjectRefKind.Interface:
                        if (table.FindInterface(send.Name!) is not { } si)
                            return $"unresolvable interface '{send.Name}' in the sending description";
                        return si == int1 || InheritsClosure(si).Contains(int1) ? null
                            : $"interface {send.Name} neither identifies nor inherits from interface "
                              + $"{recv.Name} (ISO §14.9.39.3 SR10 a))";
                    // b) an object reference described with an object-class-name: b)1. FACTORY written ⇒ the
                    //    FACTORY object of that class implements int-1; b)2. otherwise ⇒ its instance objects do.
                    case ObjectRefKind.ObjectClass:
                    {
                        if (table.Find(send.Name!) is not { } sc)
                            return $"unresolvable class '{send.Name}' in the sending description";
                        return table.ImplementsClosure(sc, send.Factory).Contains(int1) ? null
                            : $"the {(send.Factory ? "factory object" : "objects")} of class {send.Name} "
                              + $"do{(send.Factory ? "es" : "")} not implement interface {recv.Name} "
                              + $"(ISO §14.9.39.3 SR10 b){(send.Factory ? "1" : "2")}.)";
                    }
                    // c) an object reference described with an ACTIVE-CLASS phrase — the same two legs, asked
                    //    of the class CONTAINING the sending data item.
                    case ObjectRefKind.ActiveClass:
                    {
                        if (!activeClassSenderAdmitted)
                            return $"an ACTIVE-CLASS object reference does not conform to a receiver described "
                                   + $"with interface-name '{recv.Name}' (ISO §9.3.8.2.3 rule 5 b) — its "
                                   + "alternatives are an interface-name and an object-class-name)";
                        if (send.Name is null)
                            return OwnerlessActiveClassSender;
                        if (table.Find(send.Name) is not { } ac)
                            return $"unresolvable containing class '{send.Name}' of the ACTIVE-CLASS sender";
                        return table.ImplementsClosure(ac, send.Factory).Contains(int1) ? null
                            : $"the {(send.Factory ? "factory object" : "objects")} of the class containing the "
                              + $"ACTIVE-CLASS sender ({send.Name}) do{(send.Factory ? "es" : "")} not implement "
                              + $"interface {recv.Name} (ISO §14.9.39.3 SR10 c){(send.Factory ? "1" : "2")}.)";
                    }
                    default:
                        return "a UNIVERSAL object reference does not conform to a receiver described with "
                               + $"interface-name '{recv.Name}' (ISO §14.9.39.3 SR10 — its closed list of "
                               + "senders does not include a universal reference)";
                }
            }

            // ── SR12: the receiver is described with an object-class-name. ───────────────────────────────
            case ObjectRefKind.ObjectClass:
                switch (send.Kind)
                {
                    // a) an object-class-name sender.
                    case ObjectRefKind.ObjectClass:
                    {
                        // a)3. — the FACTORY axis is INVARIANT, checked first because it is independent of the
                        // class relation and its violation is the one the name comparison would hide.
                        if (send.Factory != recv.Factory)
                            return $"the FACTORY phrase is {FactoryPresence(recv.Factory)} in the receiver's "
                                   + $"description but {FactoryPresence(send.Factory)} in the sender's "
                                   + "— its presence or absence shall be the same (ISO §14.9.39.3 SR12 a)3.)";
                        // a)1. — an ONLY receiver takes an ONLY sender naming the SAME class, exactly.
                        if (recv.Only)
                            return send.Only && string.Equals(send.Name, recv.Name, StringComparison.OrdinalIgnoreCase)
                                ? null
                                : $"the receiver is described with the ONLY phrase, so the sender shall also be "
                                  + $"described ONLY and with the same object-class-name '{recv.Name}' (the "
                                  + $"sender is {send.Spelled}) (ISO §14.9.39.3 SR12 a)1.)";
                        // a)2. — otherwise the same class or a subclass.
                        var sc2 = table.Find(send.Name!);
                        var rc2 = table.Find(recv.Name!);
                        if (sc2 is null || rc2 is null)
                            return $"unresolvable class in the pair (sender {send.Name} to receiver {recv.Name})";
                        return sc2.ConformsTo(rc2) ? null
                            : $"class {send.Name} is not {recv.Name} or one of its subclasses "
                              + "(ISO §14.9.39.3 SR12 a)2.)";
                    }
                    // b) an ACTIVE-CLASS sender.
                    case ObjectRefKind.ActiveClass:
                    {
                        if (!activeClassSenderAdmitted)
                            return $"an ACTIVE-CLASS object reference does not conform to a receiver described "
                                   + $"with object-class-name '{recv.Name}' (ISO §9.3.8.2.3 rule 5 c) — its "
                                   + "subject is an object-class-name description)";
                        if (send.Factory != recv.Factory)
                            return $"the FACTORY phrase is {FactoryPresence(recv.Factory)} in the receiver's "
                                   + $"description but {FactoryPresence(send.Factory)} in the ACTIVE-CLASS sender's "
                                   + "— its presence or absence shall be the same (ISO §14.9.39.3 SR12 b)3.)";
                        if (recv.Only)
                            return "the receiver is described with the ONLY phrase, so an ACTIVE-CLASS sender is "
                                   + "not permitted (ISO §14.9.39.3 SR12 b)1.)";
                        if (send.Name is null)
                            return OwnerlessActiveClassSender;
                        var ac2 = table.Find(send.Name);
                        var rc3 = table.Find(recv.Name!);
                        if (ac2 is null || rc3 is null)
                            return $"unresolvable class in the pair (ACTIVE-CLASS sender in {send.Name} to "
                                   + $"receiver {recv.Name})";
                        return ac2.ConformsTo(rc3) ? null
                            : $"the class containing the ACTIVE-CLASS sender ({send.Name}) is not {recv.Name} or "
                              + "one of its subclasses (ISO §14.9.39.3 SR12 b)2.)";
                    }
                    default:
                        return $"{send.Spelled} does not conform to a receiver described with object-class-name "
                               + $"'{recv.Name}' (ISO §14.9.39.3 SR12 — its closed list of senders is an "
                               + "object-class-name reference, an ACTIVE-CLASS reference, SELF and NULL)";
                }

            // ── SR14: the receiver is described with an ACTIVE-CLASS phrase. ─────────────────────────────
            default:
                if (send.Kind is not ObjectRefKind.ActiveClass)
                    return $"{send.Spelled} does not conform to a receiver described with the ACTIVE-CLASS "
                           + "phrase (ISO §14.9.39.3 SR14 — its closed list of senders is an ACTIVE-CLASS "
                           + "reference, SELF and NULL)";
                return send.Factory == recv.Factory ? null
                    : $"the FACTORY phrase is {FactoryPresence(recv.Factory)} in the receiver's ACTIVE-CLASS "
                      + $"description but {FactoryPresence(send.Factory)} in the sender's — its presence or absence "
                      + "shall be the same (ISO §14.9.39.3 SR14 a))";
        }
    }

    /// <summary>The refusal for an ACTIVE-CLASS sender with no containing class — the owner-less description of an
    /// interface method prototype's formal (kb/Work PB1497). The SET rules that relate a sender's class to the
    /// receiver's (SR10 c), SR12 b)) have no class to relate; the pair rules never ask them of such a sender
    /// (<c>activeClassSenderAdmitted: false</c> refuses it first), so this is the loud floor under a future caller.</summary>
    private const string OwnerlessActiveClassSender =
        "an ACTIVE-CLASS sender written in an interface method prototype has no containing class to relate to the receiver's";

    /// <summary>How a FACTORY-axis refusal names one side's phrase. The three FACTORY arms above spliced "not " before
    /// "specified" for the receiver only, so a sender WITH the phrase read "… not specified in the receiver's
    /// description and in the sender's" — the reverse of the fact (kb/Work PB1498's row GR-9.3.8.2.3-L2.3, probe O5B).</summary>
    private static string FactoryPresence(bool factory) => factory ? "specified" : "not specified";

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

    /// <summary>The transitive INHERITS closure of one interface (§11.8.4 GR2's interface half), the sender
    /// side of SR10 a) — "an interface-name that identifies int-1 or an interface inheriting from int-1".</summary>
    private static HashSet<OoInterfaceSymbol> InheritsClosure(OoInterfaceSymbol from)
    {
        var seen = new HashSet<OoInterfaceSymbol>();
        var stack = new Stack<OoInterfaceSymbol>([from]);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (!seen.Add(cur)) continue;
            foreach (var b in cur.Inherits) stack.Push(b);
        }
        return seen;
    }
}
