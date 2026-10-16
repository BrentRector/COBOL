// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
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
/// (<see cref="PrototypeSignatures.Same"/>), as the coarsest stable partition (<see cref="Classify"/>: refined until a
/// round splits nothing, so the classes do not depend on the order prototypes are presented in), and a class's identity
/// is its founder's — the first member presented — externalized name
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

    /// <summary>⛔ THE PARTITION REFINEMENT (kb/Work PB3101; COBOLNET_OO_DESIGN D6 item 7, the wave-1080 design decision):
    /// classify every prototype a restricted pointer of <paramref name="pointers"/> resolves to, and stamp each pointer
    /// with its class (<see cref="DataItem.PointerRestrictionIdentity"/>). Every resolved prototype starts in ONE block
    /// of its namespace; each round stamps every pointer with its prototype's CURRENT block and splits each block by
    /// <see cref="PrototypeSignatures.Same"/>, which compares the formals' and returning items' descriptions — and a
    /// formal that is itself a restricted pointer is described by the block its own prototype is in. A round that splits
    /// nothing is the coarsest stable partition: two prototypes share a class unless some finite path through their
    /// formals reaches a difference, the greatest fixed point "the same signature" is for prototypes that name each other
    /// or themselves (ISO §13.18.60.4 GR25: "the address of a program with the same signature as that identified by the
    /// specified program-prototype-name"; kb/Work PB2495). Called once per compilation group, after every forest's
    /// pointers have their <see cref="DataItem.RestrictedPrototype"/> (<c>BinderDriver.ResolvePointerRestrictions</c>).</summary>
    public void Classify(IReadOnlyList<DataItem> pointers)
    {
        foreach (var pointer in pointers)
            if (pointer.RestrictedPrototype is { } prototype)
                SpaceOf(prototype).Present(prototype.ExternalizedName, prototype.Signature);
        while (true)
        {
            foreach (var pointer in pointers)
                if (pointer.RestrictedPrototype is { } prototype)
                    pointer.PointerRestrictionIdentity = Identity(prototype);
            // Both namespaces refine under ONE stamping (a program prototype's formal may be a function-pointer), so
            // neither short-circuits the other.
            bool programsSplit = _programs.Refine(), functionsSplit = _functions.Refine();
            if (!programsSplit && !functionsSplit) return;
        }
    }

    /// <summary>The identity of a prototype's signature class (<c>TO name</c>, its founder's externalized name). A
    /// prototype <see cref="Classify"/> partitioned answers its block; one no restricted pointer names (a
    /// program-address-identifier's prototype, asked by the procedure binder) joins the class whose founder it has the
    /// same signature as, under the identities the partition stamped, or founds one of its own — no stamped pointer is
    /// restricted to it, so the partition stays stable.</summary>
    public string Identity(RestrictedPrototype prototype) =>
        SpaceOf(prototype).Identity(prototype.ExternalizedName, prototype.Signature);

    private Namespace SpaceOf(RestrictedPrototype prototype) => prototype.Function ? _functions : _programs;

    /// <summary>One namespace's classes (a program prototype and a function prototype of one name are two prototypes,
    /// §9.4): its blocks, each led by its founder (the first member presented), and every name already classified.</summary>
    private sealed class Namespace
    {
        private List<Block> _blocks = [];
        // An externalized name is ONE program in a run unit (§8.3.2.2 rule 2), so it is classified once — and a self
        // entry (BinderDriver.ProgramPrototypesOf) rebuilds its signature object on every call.
        private readonly Dictionary<string, Member> _members = new(CobolNames.Comparer);

        /// <summary>Puts a prototype with a compile-time signature into the namespace's first block, before
        /// <see cref="Refine"/> runs; GR10 c) / GR11 c) (no signature) is a class of its own and joins no block.</summary>
        public void Present(string externalizedName, CalleeSignature? signature)
        {
            if (signature is null || _members.ContainsKey(externalizedName)) return;
            if (_blocks.Count == 0) _blocks.Add(new Block());
            _blocks[0].Add(_members[externalizedName] = new Member(externalizedName, signature));
        }

        /// <summary>One round: splits every block by <see cref="PrototypeSignatures.Same"/> against the founder of each
        /// part (the founder stays first, so a block's identity survives the split). True when any block split.</summary>
        public bool Refine()
        {
            bool split = false;
            var next = new List<Block>(_blocks.Count);
            foreach (var block in _blocks)
            {
                int first = next.Count;
                foreach (var member in block.Members)
                {
                    Block? home = null;
                    for (int i = first; i < next.Count && home is null; i++)
                        if (PrototypeSignatures.Same(next[i].Founder.Signature, member.Signature)) home = next[i];
                    if (home is null) next.Add(home = new Block());
                    home.Add(member);
                }
                split |= next.Count - first > 1;
            }
            _blocks = next;
            return split;
        }

        public string Identity(string externalizedName, CalleeSignature? signature)
        {
            if (signature is null) return PrototypeSignatures.OwnIdentity(externalizedName);   // GR10 c) / GR11 c)
            if (_members.TryGetValue(externalizedName, out var known)) return known.Block!.Founder.Identity;
            var member = _members[externalizedName] = new Member(externalizedName, signature);
            var home = _blocks.Find(b => PrototypeSignatures.Same(b.Founder.Signature, signature));
            if (home is null) _blocks.Add(home = new Block());
            home.Add(member);
            return home.Founder.Identity;
        }
    }

    /// <summary>A prototype in the partition: its class identity were it a founder (<c>TO</c> its externalized name),
    /// its signature, and the block it is in now.</summary>
    private sealed class Member(string externalizedName, CalleeSignature signature)
    {
        public string Identity { get; } = PrototypeSignatures.OwnIdentity(externalizedName);
        public CalleeSignature Signature { get; } = signature;
        public Block? Block { get; set; }
    }

    /// <summary>One block of the partition; its first member is its founder, whose identity is the block's.</summary>
    private sealed class Block
    {
        public List<Member> Members { get; } = [];
        public Member Founder => Members[0];

        public void Add(Member member)
        {
            Members.Add(member);
            member.Block = this;
        }
    }
}

/// <summary>The prototype a restricted program- or function-pointer's <c>TO prototype-name-1</c> phrase resolves to
/// through its unit's prototype tables (<c>BinderDriver.ProgramPrototypesOf</c> / <c>UserFunctionsOf</c>, ISO §8.4.6.8 /
/// §8.4.6.6 and §12.3.8.4 GR10 / GR11): its namespace, its externalized name, and its signature — null when GR10 c) /
/// GR11 c) take the details from the external repository, so there is nothing to compare at compile time. Attached to
/// the pointer as <see cref="DataItem.RestrictedPrototype"/> (kb/Work PB2495) so that the signature classes can be
/// computed over every prototype a pointer names (<see cref="PrototypeSignatureClasses.Classify"/>).</summary>
internal sealed record RestrictedPrototype(bool Function, string ExternalizedName, CalleeSignature? Signature)
{
    public static RestrictedPrototype Of(ProgramPrototype program) =>
        new(Function: false, program.ExternalizedName, program.Signature);

    public static RestrictedPrototype Of(UserFunctionSignature function) =>
        new(Function: true, function.Externalized, new CalleeSignature(function.Formals, function.Returning));
}
