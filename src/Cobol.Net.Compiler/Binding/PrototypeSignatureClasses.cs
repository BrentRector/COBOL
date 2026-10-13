// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE TYPE OF A RESTRICTED PROGRAM- OR FUNCTION-POINTER, as ONE identity per signature (kb/Work PB2464).
/// ISO §13.18.60.4 GR25 — "A restricted program-pointer shall contain only the predefined address NULL or the
/// address of a program with the same signature as that identified by the specified program-prototype-name" — and GR26,
/// its function-pointer twin, type a restricted pointer by the SIGNATURE of the prototype it names, never by the
/// prototype's name: two pointers restricted to two differently named prototypes of one signature hold the same set of
/// values. §14.9.39.3 SR20 / SR22 therefore admit a SET between them
/// (<see cref="PrototypeSignatures.Same"/>), and §14.8.2.3.2's "If either is a restricted pointer, both shall be
/// restricted and of the same type" is the same question asked at an argument. Before this type every relation that
/// had no host to resolve a prototype name through (the §9.3.6 match of a UNIVERSAL INVOKE, whose two descriptions
/// travel to run time; the typed conformance comparator) compared the NAMES.
/// <para><b>The class.</b> A group's prototypes are partitioned into classes by the ONE same-signature test
/// (<see cref="PrototypeSignatures.Same"/>) — the first prototype presented to <see cref="Identity"/> founds a class and
/// every later prototype of the same signature joins it — and a class's identity is its founder's externalized name
/// (§8.3.2.2: "all instances of a given name that is externalized to the operating environment shall identify the same
/// kind of entity"), spelled <c>TO name</c> as a pointer's USAGE clause
/// spells it. A prototype that took ISO §12.3.8.4 GR10 c) / GR11 c) — the details come from the external repository, so
/// there is no compile-time signature to compare — is a class of its own: <c>TO name</c> of its own name, exactly the
/// identity every relation compared before. A prototype that is its own founder therefore keeps the identity it always
/// had; only an ALIAS of a founder changes.</para>
/// <para><b>Scope.</b> One instance per compilation group (<see cref="BindSession"/>): the identity is the founder's
/// name, so it is comparable between the two descriptions of one run unit only while both were compiled in one group —
/// which is every universal dispatch this implementation can make until the external repository of
/// <c>docs/rearchitecture/DESIGN-external-repository.md</c> (slices 4–8) replaces a group-local founder by a repository
/// record's identity. The class is the type model the relations read, so that replacement changes this type and no
/// relation.</para>
/// </summary>
internal sealed class PrototypeSignatureClasses
{
    private readonly Namespace _programs = new();
    private readonly Namespace _functions = new();

    /// <summary>The identity of a prototype's signature class (<c>TO name</c>). A formal that is itself a restricted
    /// pointer is compared by the signature it names (<see cref="PrototypeBisimulation"/>, kb/Work PB2495), never by the
    /// identity this is computing, so the classification does not depend on the order prototypes are presented in.</summary>
    public string Identity(RestrictedPrototype prototype) =>
        (prototype.Function ? _functions : _programs).Identity(prototype.ExternalizedName, prototype.Signature);

    /// <summary>One namespace's classes (a program prototype and a function prototype of one name are two prototypes,
    /// §9.4): the founders in the order presented, and every name already classified.</summary>
    private sealed class Namespace
    {
        private readonly List<(string Identity, CalleeSignature Signature)> _founders = [];
        private readonly Dictionary<string, string> _classified = new(CobolNames.Comparer);

        public string Identity(string externalizedName, CalleeSignature? signature)
        {
            string own = PrototypeSignatures.OwnIdentity(externalizedName);
            if (signature is null) return own;                   // GR10 c) / GR11 c): nothing to compare, a class of its own
            // An externalized name is ONE program in a run unit, so its classification is stable — and a self entry
            // (BinderDriver.ProgramPrototypesOf) rebuilds its signature object on every call.
            if (_classified.TryGetValue(externalizedName, out var known)) return known;
            foreach (var (identity, founder) in _founders)
                if (PrototypeSignatures.Same(founder, signature))
                    return _classified[externalizedName] = identity;
            _founders.Add((own, signature));
            return _classified[externalizedName] = own;
        }
    }
}

/// <summary>The prototype a restricted program- or function-pointer's <c>TO prototype-name-1</c> phrase resolves to
/// through its unit's prototype tables (<c>BinderDriver.ProgramPrototypesOf</c> / <c>UserFunctionsOf</c>, ISO §8.4.6.8 /
/// §8.4.6.6 and §12.3.8.4 GR10 / GR11): its namespace, its externalized name, and its signature — null when GR10 c) /
/// GR11 c) take the details from the external repository, so there is nothing to compare at compile time. Attached to
/// the pointer as <see cref="Model.DataItem.RestrictedPrototype"/> (kb/Work PB2495) so that a signature comparison
/// can follow a formal's restriction to the signature it names (<see cref="PrototypeSignatures.SameType"/>).</summary>
internal sealed record RestrictedPrototype(bool Function, string ExternalizedName, CalleeSignature? Signature)
{
    public static RestrictedPrototype Of(ProgramPrototype program) =>
        new(Function: false, program.ExternalizedName, program.Signature);

    public static RestrictedPrototype Of(UserFunctionSignature function) =>
        new(Function: true, function.Externalized, new CalleeSignature(function.Formals, function.Returning));

    /// <summary>The prototype's identity in a run unit: its namespace and its folded externalized name (§8.3.2.2
    /// rule 2), the key <see cref="PrototypeBisimulation"/> assumes pairs under.</summary>
    public (bool Function, string Name) Key => (Function, CobolNames.Fold(ExternalizedName));
}
