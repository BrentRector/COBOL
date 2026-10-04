// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Compiler.Oo;

/// <summary>One class of the pass-1 table: identity, base link, and the method roster (name → symbol,
/// case-insensitive per §8.3.2.2). <see cref="CsName"/> is the emitted C# type name — the SAME mapping
/// <c>PicInfo.ClrType</c> applies to a typed object reference's declared class (Sanitize + uppercase).</summary>
public sealed class OoClassSymbol(string name, string csName, CobolParserCore.ClassDefinitionContext? ctx)
{
    public string Name { get; } = name;
    /// <summary>The name externalized to the operating environment (ISO §11.3.4 GR1: "literal-1, if
    /// specified, is the name of the class that is externalized to the operating environment"), from the
    /// CLASS-ID <c>AS literal-1</c>; else <see cref="Name"/>. An object-class-name is only ever referenced
    /// by a user-defined WORD (§8.4.6.4 — the containing definition or a REPOSITORY entry), so nothing
    /// in COBOL resolves against this, and <see cref="CsName"/> deliberately does NOT derive from it: the
    /// emitted type name is a WIRE CONTRACT with <c>PicInfo.ClrType</c>, which maps a declared
    /// object-class-name to a C# type with no access to this table. kb/Work PB303.</summary>
    public string ExternalizedName { get; init; } = name;
    public string CsName { get; } = csName;
    /// <summary>The CLASS-ID definition this symbol was built from. Every class of the compilation group has one;
    /// the STANDARD class (<see cref="IsStandard"/>) does not, and reading this on it is a loud internal error rather
    /// than a null the caller forgot to test — no consumer of a group class's source should ever reach the standard
    /// class, because it is never in <see cref="OoClassTable.Classes"/>.</summary>
    public CobolParserCore.ClassDefinitionContext Ctx => ctx
        ?? throw new InvalidOperationException(
            $"class '{Name}' is the standard class of ISO §16 and has no source definition");

    /// <summary>True for the standard class BASE (ISO/IEC 1989:2023 §16.1: "A standard class BASE shall be provided
    /// by the implementation"), built by <see cref="OoStandardClasses"/> and never for a CLASS-ID of the group.</summary>
    public bool IsStandard => ctx is null;
    /// <summary>The FIRST (and, in v1, only permitted) INHERITS base-class name — null for a root class.</summary>
    public string? BaseName { get; init; }

    /// <summary>The FULL `INHERITS FROM` list as written (ISO §11.3 permits several; SSOT §18 #18 / A.4.10
    /// restricts v1 to SINGLE inheritance and REJECTS 2+ bases LOUDLY — COBOLNET0849 at pass-1 — so the extra
    /// bases are representable here for the diagnostic, never silently dropped).</summary>
    public IReadOnlyList<string> Bases { get; init; } = [];

    public OoClassSymbol? Base { get; set; }

    /// <summary>CLASS-ID … IS FINAL (§11.3 — GR3: a FINAL class shall not be a superclass; 0839). Emits
    /// C# <c>sealed</c>; every fresh method slot in it emits NON-virtual (the CS0549 trap — a virtual
    /// member inside a sealed class is a Roslyn error on emitted code).</summary>
    public bool IsFinal { get; init; }

    /// <summary>The OBJECT paragraph's IMPLEMENTS interfaces (§11.8.2), resolved at pass-1; the CONFORMANCE
    /// closure (§11.8.4 GR2: direct + interface-INHERITed + class-INHERITed) is computed by
    /// <c>ValidateImplements</c> — the C# base-list emission carries only the DIRECT list (the closure
    /// arrives transitively at the C# level).</summary>
    public List<OoInterfaceSymbol> Implements { get; } = [];

    /// <summary>The FACTORY paragraph's IMPLEMENTS interfaces (§11.8.2 :12755). The factory SINGLETON class
    /// (brief D11 — real virtual members, post-FACTORY-wave) emits and satisfies them exactly like the
    /// instance side; the original brief's validate-only posture is SUPERSEDED by D11.</summary>
    public List<OoInterfaceSymbol> FactoryImplements { get; } = [];


    private readonly Dictionary<string, OoMethodSymbol> _methods = new(CobolNames.Comparer);

    /// <summary>The INSTANCE methods in declaration order (the emitter's roster).</summary>
    public IReadOnlyList<OoMethodSymbol> Methods => _methodList;
    private readonly List<OoMethodSymbol> _methodList = [];

    /// <summary>The emitted C# type of this class's FACTORY OBJECT (brief D11 — a REAL sibling singleton
    /// class, `FOO__FACTORY : BASE__FACTORY | CobolObject`, NEVER statics: §8.6.4 gives every class its OWN
    /// copy of inherited factory data; SELF in a factory method is polymorphic §14.9.23.3 SR4f + §8.4.3.8
    /// GR2; factory resolution walks INHERITS §9.3.6. `__` cannot appear in a COBOL-derived name — no
    /// collision).</summary>
    public string FactoryCsName => CsName + NamingConvention.FactorySuffix;

    /// <summary>The emitted C# types a COBOL reference to THIS class selects when the rule names the class
    /// WITHOUT distinguishing the two object kinds — ISO/IEC 1989:2023 §14.9.49.4 GR14 a): "the exception
    /// object that was raised is a factory object or instance object of object-class-name-1 or of a subclass
    /// of object-class-name-1". A COBOL class is emitted as TWO DISJOINT C# hierarchies —
    /// <see cref="CsName"/> rooted at its base's instance half, <see cref="FactoryCsName"/> at its base's
    /// FACTORY half — so ONE COBOL class-name is a SET of C# types, never one, and a single
    /// <c>is CsName</c> test silently answers false for every factory object (kb/Work PB366). Both halves
    /// mirror INHERITS, so the pair of C# type tests covers "or of a subclass" in both hierarchies.
    /// A rule that names only ONE kind takes the single name it means instead — §13.18.60.4 GR22 d) 1. b):
    /// "If the FACTORY phrase is not specified, the object referenced by this data item shall be an instance
    /// object of the specified class or of a subclass of the specified class" (the typed OBJECT REFERENCE
    /// narrow in <c>OoEmitter.EmitSetObjectRef</c> is instance-only for exactly that reason).</summary>
    public IReadOnlyList<string> FactoryOrInstanceCsTypes => [CsName, FactoryCsName];

    private readonly Dictionary<string, OoMethodSymbol> _factoryMethods = new(CobolNames.Comparer);

    /// <summary>The FACTORY methods in declaration order (§11.4 — a SEPARATE interface from the instance
    /// roster; the two may share names, §9.3.6).</summary>
    public IReadOnlyList<OoMethodSymbol> FactoryMethods => _factoryMethodList;
    private readonly List<OoMethodSymbol> _factoryMethodList = [];

    internal bool TryAddMethod(OoMethodSymbol m)
    {
        if (!_methods.TryAdd(m.ExternalizedName, m)) return false;   // the roster key (PB303)
        _methodList.Add(m);
        return true;
    }

    internal bool TryAddFactoryMethod(OoMethodSymbol m)
    {
        if (!_factoryMethods.TryAdd(m.ExternalizedName, m)) return false;   // the roster key (PB303)
        _factoryMethodList.Add(m);
        return true;
    }

    /// <summary>Resolve a FACTORY method by name over THIS class and its base chain (§9.3.6 — factory
    /// resolution walks INHERITS exactly like instance resolution, over the factory interface).</summary>
    public OoMethodSymbol? FindFactoryMethod(string name)
    {
        var seen = new HashSet<OoClassSymbol>();
        for (OoClassSymbol? c = this; c is not null && seen.Add(c); c = c.Base)
            if (c._factoryMethods.TryGetValue(name, out var m))
                return m;
        return null;
    }

    /// <summary>Resolve a method by name over THIS class and its base chain (§9.3.6 method resolution; the
    /// walk is cycle-safe — pass-1 rejects unknown bases, and a cycle cannot outlast the seen-set).</summary>
    public OoMethodSymbol? FindMethod(string name)
    {
        var seen = new HashSet<OoClassSymbol>();
        for (OoClassSymbol? c = this; c is not null && seen.Add(c); c = c.Base)
            if (c._methods.TryGetValue(name, out var m))
                return m;
        return null;
    }

    /// <summary>True when the standard class BASE is this class or one of its superclasses — i.e. the class has
    /// BASE's life-cycle methods, New in its factory interface and FactoryObject in its object interface (ISO §16.2;
    /// §9.3.9). The emitter derives the class's two C# halves from the runtime BASE pair exactly when this holds.</summary>
    public bool InheritsStandardBase
    {
        get
        {
            var seen = new HashSet<OoClassSymbol>();
            for (OoClassSymbol? c = this; c is not null && seen.Add(c); c = c.Base)
                if (c.IsStandard)
                    return true;
            return false;
        }
    }

    /// <summary>True when <paramref name="other"/> is this class or one of its superclasses — the object-view /
    /// RETURNING-receiver conformance direction (§14.8: a subclass object conforms to its superclass).</summary>
    public bool ConformsTo(OoClassSymbol other)
    {
        var seen = new HashSet<OoClassSymbol>();
        for (OoClassSymbol? c = this; c is not null && seen.Add(c); c = c.Base)
            if (ReferenceEquals(c, other))
                return true;
        return false;
    }
}
