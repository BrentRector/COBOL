// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>One operand of an ISO §14.9.39.3 SET Format 5 pair, as SR10 / SR12 / SR14 read it: the description's
/// §13.18.60.2 phrases plus the two CLASS FACTS those rules relate across operands — which classes the description's
/// objects belong to and which interfaces they implement.
/// <para><paramref name="Classes"/> is the class the description names (for an ACTIVE-CLASS description, "the class
/// containing the data item", SR10 c) / SR12 b) 2.) followed by every class it inherits from, so "the same class or a
/// subclass of" a class is "the class is in the list". <paramref name="Interfaces"/> is, for an interface-name
/// description, that interface followed by every interface it inherits from (SR10 a)); for an object-class-name or
/// ACTIVE-CLASS description, every interface the class's objects — its factory object when the FACTORY phrase is
/// written — implement, through IMPLEMENTS, inheritance of interfaces and inheritance of classes (SR10 b) / c); the
/// §11.8.4 GR2 closure). Both are null when the name was not resolved, and for a universal description, which names
/// neither a class nor an interface.</para>
/// <para>The compiler builds operands from its class table (<c>OoClassTable.SetOperand</c>) for every SET, BY CONTENT
/// argument and RETURNING item it binds; the run time builds them from the <see cref="ActivationDescription"/>s that
/// cross a universal invocation or a CALL located at run time, which carry the same facts
/// (<see cref="ActivationDescription.ObjectClasses"/>, <see cref="ActivationDescription.ObjectInterfaces"/>; kb/Work
/// PB2463).</para></summary>
/// <param name="Kind">The §13.18.60.2 alternative written.</param>
/// <param name="Name">The interface-name or object-class-name written; for ACTIVE-CLASS the containing class, null in
/// an interface method prototype (which has none); null for a universal description.</param>
/// <param name="Factory">The FACTORY phrase.</param>
/// <param name="Only">The ONLY phrase.</param>
/// <param name="Classes">The class and its ancestors (see above), or null.</param>
/// <param name="Interfaces">The interfaces the description's objects implement (see above), or null.</param>
public readonly record struct ObjectReferenceOperand(ObjectReferenceKind Kind, string? Name, bool Factory, bool Only,
    IReadOnlyList<string>? Classes, IReadOnlyList<string>? Interfaces)
{
    /// <summary>How the description reads back in a diagnostic — the phrases as written, so a message never has to say
    /// "universal" about a typed reference (kb/Work PB389).</summary>
    public string Spelled => Kind switch
    {
        ObjectReferenceKind.Universal => "a UNIVERSAL object reference",
        ObjectReferenceKind.Interface => $"an object reference described with interface-name '{Name}'",
        ObjectReferenceKind.ActiveClass => $"an object reference described {(Factory ? "FACTORY OF " : "")}ACTIVE-CLASS "
            + (Name is null ? "(in an interface method prototype)" : $"(containing class '{Name}')"),
        _ => "an object reference described "
             + (Factory ? "FACTORY OF " : "") + $"'{Name}'" + (Only ? " ONLY" : ""),
    };
}

/// <summary>
/// ⛔ THE ONE SENDER-INTO-RECEIVER TABLE FOR OBJECT REFERENCES — ISO §14.9.39.3 SR8 / SR10 / SR12 / SR14 (SET Format 5),
/// in ONE place for both phases (kb/Work PB2463, the twin of <see cref="MoveValidity"/> for §14.9.25.3). The receiver's
/// description selects the rule; the sender's description answers it.
/// <list type="bullet">
///   <item><b>universal receiver</b> — SR8: "Identifier-3 shall be any item of class object that is permitted as a
///     receiving item"; §13.18.60.4 GR22 b) makes its content "a reference to any object". Nothing to check.</item>
///   <item><b>interface-name receiver</b> — SR10: a) an interface identifying int-1 or inheriting from it; b) an
///     object-class-name whose b)1. FACTORY object (FACTORY written) or b)2. instance objects implement int-1; c) an
///     ACTIVE-CLASS reference, the same two legs over the CONTAINING class.</item>
///   <item><b>object-class-name receiver</b> — SR12: a) an object-class-name sender, a)1. ONLY ⇒ the sender is ONLY and
///     names the SAME class, a)2. no ONLY ⇒ the same class or a subclass, a)3. FACTORY presence equal; b) an
///     ACTIVE-CLASS sender, b)1. the receiver is not ONLY, b)2. the sender's containing class is the receiver's class or
///     a subclass, b)3. FACTORY presence equal.</item>
///   <item><b>ACTIVE-CLASS receiver</b> — SR14: a) an ACTIVE-CLASS sender "where the presence or absence of the FACTORY
///     phrase is the same as in the data item referenced by identifier-3". The rule as printed constrains only the
///     FACTORY axis.</item>
/// </list>
/// <para>Who asks it. The compiler, for every pair it can see (<c>OoConformance.ObjectRefAssignmentMismatch</c>: a SET, a
/// BY CONTENT object-reference argument §14.8.2.3.3, a RETURNING delivery §14.8.3.3 rule 1, the covariant RETURNING of
/// §9.3.8.2.3 rule 5); and the run time, for the pairs no single compilation sees — §9.3.6 match rule 6, "there shall be
/// a corresponding specification in the invoked method that may be a sending item in a SET statement with the returning
/// item as the receiving item", between a method's returning item and a universal invocation's, and §14.8.2.3.3 /
/// §14.8.3.3 at a CALL whose activated program is located by name (<see cref="ActivationRelations"/>).</para>
/// <para>The SELF and NULL senders of SR10 d)/e), SR12 c)/d) and SR14 b)/c) are not DESCRIPTIONS and are adjudicated at
/// the SET site (<c>OoBinder.OoBindSetObjectRef</c>), which is also where SR11/SR13's class-NAME sender lives.</para>
/// <para><paramref name="activeClassSenderAdmitted"/> is the ONE place two rule sets differ. §14.9.39.3 SR10 c) and SR12
/// b) admit an ACTIVE-CLASS sender into an interface-typed or class-typed receiver; §9.3.8.2.3 rule 5 b)/c), the
/// interface-conformance twin, enumerates no ACTIVE-CLASS leg — its 5 d) pairs ACTIVE-CLASS only with ACTIVE-CLASS. The
/// interface-conformance callers therefore pass false and get the printed rule 5, rather than a shared function quietly
/// widening one of the two.</para>
/// </summary>
public static class ObjectReferenceSet
{
    /// <summary>Null when a SET of <paramref name="send"/> into <paramref name="recv"/> is valid by §14.9.39.3 SR8 / SR10
    /// / SR12 / SR14, else the clause it violates. An operand whose class or interface was not resolved
    /// (<see cref="ObjectReferenceOperand.Classes"/> / <see cref="ObjectReferenceOperand.Interfaces"/> null where the rule
    /// reads them) is refused with that reason.</summary>
    public static string? Refusal(ObjectReferenceOperand send, ObjectReferenceOperand recv, bool activeClassSenderAdmitted = true)
    {
        switch (recv.Kind)
        {
            // ── SR8 / GR22 b): a universal receiver constrains nothing. ──────────────────────────────────────
            case ObjectReferenceKind.Universal:
                return null;

            // ── SR10: the receiver is described with an interface-name that identifies int-1. ────────────────
            case ObjectReferenceKind.Interface:
            {
                if (recv.Interfaces is not [var int1, ..])
                    return $"unresolvable interface '{recv.Name}' in the receiver's description";
                switch (send.Kind)
                {
                    // a) an object reference described with an interface-name identifying int-1 or an interface
                    //    inheriting from int-1.
                    case ObjectReferenceKind.Interface:
                        if (send.Interfaces is null)
                            return $"unresolvable interface '{send.Name}' in the sending description";
                        return Holds(send.Interfaces, int1) ? null
                            : $"interface {send.Name} neither identifies nor inherits from interface "
                              + $"{recv.Name} (ISO §14.9.39.3 SR10 a))";
                    // b) an object reference described with an object-class-name: b)1. FACTORY written ⇒ the FACTORY
                    //    object of that class implements int-1; b)2. otherwise ⇒ its instance objects do.
                    case ObjectReferenceKind.ObjectClass:
                        if (send.Interfaces is null)
                            return $"unresolvable class '{send.Name}' in the sending description";
                        return Holds(send.Interfaces, int1) ? null
                            : $"the {(send.Factory ? "factory object" : "objects")} of class {send.Name} "
                              + $"do{(send.Factory ? "es" : "")} not implement interface {recv.Name} "
                              + $"(ISO §14.9.39.3 SR10 b){(send.Factory ? "1" : "2")}.)";
                    // c) an object reference described with an ACTIVE-CLASS phrase — the same two legs, asked of the
                    //    class CONTAINING the sending data item.
                    case ObjectReferenceKind.ActiveClass:
                        if (!activeClassSenderAdmitted)
                            return $"an ACTIVE-CLASS object reference does not conform to a receiver described "
                                   + $"with interface-name '{recv.Name}' (ISO §9.3.8.2.3 rule 5 b) — its "
                                   + "alternatives are an interface-name and an object-class-name)";
                        if (send.Name is null)
                            return OwnerlessActiveClassSender;
                        if (send.Interfaces is null)
                            return $"unresolvable containing class '{send.Name}' of the ACTIVE-CLASS sender";
                        return Holds(send.Interfaces, int1) ? null
                            : $"the {(send.Factory ? "factory object" : "objects")} of the class containing the "
                              + $"ACTIVE-CLASS sender ({send.Name}) do{(send.Factory ? "es" : "")} not implement "
                              + $"interface {recv.Name} (ISO §14.9.39.3 SR10 c){(send.Factory ? "1" : "2")}.)";
                    default:
                        return "a UNIVERSAL object reference does not conform to a receiver described with "
                               + $"interface-name '{recv.Name}' (ISO §14.9.39.3 SR10 — its closed list of "
                               + "senders does not include a universal reference)";
                }
            }

            // ── SR12: the receiver is described with an object-class-name. ───────────────────────────────────
            case ObjectReferenceKind.ObjectClass:
                switch (send.Kind)
                {
                    // a) an object-class-name sender.
                    case ObjectReferenceKind.ObjectClass:
                        // a)3. — the FACTORY axis is INVARIANT, checked first because it is independent of the class
                        // relation and its violation is the one the name comparison would hide.
                        if (send.Factory != recv.Factory)
                            return $"the FACTORY phrase is {FactoryPresence(recv.Factory)} in the receiver's "
                                   + $"description but {FactoryPresence(send.Factory)} in the sender's "
                                   + "— its presence or absence shall be the same (ISO §14.9.39.3 SR12 a)3.)";
                        // a)1. — an ONLY receiver takes an ONLY sender naming the SAME class, exactly.
                        if (recv.Only)
                            return send.Only && CobolNames.Same(send.Name, recv.Name)
                                ? null
                                : $"the receiver is described with the ONLY phrase, so the sender shall also be "
                                  + $"described ONLY and with the same object-class-name '{recv.Name}' (the "
                                  + $"sender is {send.Spelled}) (ISO §14.9.39.3 SR12 a)1.)";
                        // a)2. — otherwise the same class or a subclass.
                        if (send.Classes is null || recv.Classes is not [var receiverClass, ..])
                            return $"unresolvable class in the pair (sender {send.Name} to receiver {recv.Name})";
                        return Holds(send.Classes, receiverClass) ? null
                            : $"class {send.Name} is not {recv.Name} or one of its subclasses "
                              + "(ISO §14.9.39.3 SR12 a)2.)";
                    // b) an ACTIVE-CLASS sender.
                    case ObjectReferenceKind.ActiveClass:
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
                        if (send.Classes is null || recv.Classes is not [var receiverClass2, ..])
                            return $"unresolvable class in the pair (ACTIVE-CLASS sender in {send.Name} to "
                                   + $"receiver {recv.Name})";
                        return Holds(send.Classes, receiverClass2) ? null
                            : $"the class containing the ACTIVE-CLASS sender ({send.Name}) is not {recv.Name} or "
                              + "one of its subclasses (ISO §14.9.39.3 SR12 b)2.)";
                    default:
                        return $"{send.Spelled} does not conform to a receiver described with object-class-name "
                               + $"'{recv.Name}' (ISO §14.9.39.3 SR12 — its closed list of senders is an "
                               + "object-class-name reference, an ACTIVE-CLASS reference, SELF and NULL)";
                }

            // ── SR14: the receiver is described with an ACTIVE-CLASS phrase. ─────────────────────────────────
            default:
                if (send.Kind is not ObjectReferenceKind.ActiveClass)
                    return $"{send.Spelled} does not conform to a receiver described with the ACTIVE-CLASS "
                           + "phrase (ISO §14.9.39.3 SR14 — its closed list of senders is an ACTIVE-CLASS "
                           + "reference, SELF and NULL)";
                return send.Factory == recv.Factory ? null
                    : $"the FACTORY phrase is {FactoryPresence(recv.Factory)} in the receiver's ACTIVE-CLASS "
                      + $"description but {FactoryPresence(send.Factory)} in the sender's — its presence or absence "
                      + "shall be the same (ISO §14.9.39.3 SR14 a))";
        }
    }

    /// <summary>Whether <paramref name="names"/> holds <paramref name="name"/> — COBOL words compare without regard to
    /// case (§8.3.2.2), by the ONE Annex C fold.</summary>
    private static bool Holds(IReadOnlyList<string> names, string name)
    {
        foreach (string n in names)
            if (CobolNames.Same(n, name)) return true;
        return false;
    }

    /// <summary>The refusal for an ACTIVE-CLASS sender with no containing class — the owner-less description of an
    /// interface method prototype's formal (kb/Work PB1497). The SET rules that relate a sender's class to the receiver's
    /// (SR10 c), SR12 b)) have no class to relate; the pair rules never ask them of such a sender
    /// (<c>activeClassSenderAdmitted: false</c> refuses it first), so this is the loud floor under a future caller.</summary>
    private const string OwnerlessActiveClassSender =
        "an ACTIVE-CLASS sender written in an interface method prototype has no containing class to relate to the receiver's";

    /// <summary>How a FACTORY-axis refusal names one side's phrase (kb/Work PB1498, §9.3.8.2.3 rule 5 c) 3., probe
    /// O5B: the three FACTORY arms once spliced "not " before "specified" for the receiver only).</summary>
    private static string FactoryPresence(bool factory) => factory ? "specified" : "not specified";
}
