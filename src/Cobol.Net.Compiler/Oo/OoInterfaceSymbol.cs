// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Compiler.Oo;

/// <summary>One INTERFACE-ID of the compilation group (§11.6 — pass-1): the PROTOTYPE roster (reusing
/// <see cref="OoMethodSymbol"/> — headers + LINKAGE formals, no bodies), the INHERITS list (repetition
/// SUPPORTED — C# interface lists are native; the deliberate asymmetry with the class-side single-base
/// restriction, SSOT §18.18), and the emitted C# interface name.</summary>
public sealed class OoInterfaceSymbol(string name, string csName, CobolParserCore.InterfaceDefinitionContext ctx)
{
    public string Name { get; } = name;
    /// <summary>The name externalized to the operating environment (ISO §11.6.4 GR1: "literal-1, if
    /// specified, is the name of the interface that is externalized to the operating environment"), from
    /// the INTERFACE-ID <c>AS literal-1</c>; else <see cref="Name"/>. An interface-name is only ever
    /// referenced by a user-defined WORD (§8.4.6.4), so nothing in COBOL resolves against this — it is
    /// the operating-environment identity this implementation records for the definition (kb/Work
    /// PB303).</summary>
    public string ExternalizedName { get; init; } = name;
    public string CsName { get; } = csName;
    public CobolParserCore.InterfaceDefinitionContext Ctx { get; } = ctx;
    public List<OoInterfaceSymbol> Inherits { get; } = [];

    /// <summary>The emitted C# types a COBOL reference to THIS interface selects when the rule asks whether an
    /// object IMPLEMENTS it — ISO/IEC 1989:2023 §14.9.49.4 GR14 b): "the exception object that was raised is
    /// described with an IMPLEMENTS clause that references interface-name-1". The twin of
    /// <see cref="OoClassSymbol.FactoryOrInstanceCsTypes"/>, and the answer is a SET for the same reason: one
    /// COBOL name is not one C# type by construction, it is one by MEASUREMENT. Today it measures ONE — a
    /// COBOL INTERFACE-ID emits exactly one C# interface (<c>OoEmitter.EmitInterfaceUnit</c>), which BOTH
    /// emitted halves of an implementing class carry (the instance half from the OBJECT paragraph's IMPLEMENTS,
    /// the factory half from the FACTORY paragraph's, §11.8.2), so a single <c>is</c> test covers both object
    /// kinds where a class-name needs two. If the interface ever emits a second half, this is the one line that
    /// changes and <c>Format4UseObjectSelectorDriftTests</c> is what fails first.
    /// <para>The three legs of §11.8.4 GR2 / §11.4.4 GR2 ("a) defined with an IMPLEMENTS clause specifying
    /// intf-1, b) implements an interface that inherits intf-1, c) the class … inherits a class whose instance
    /// object implements intf-1") ride C#'s <c>is</c> rather than this census: the emitted interface carries its
    /// own INHERITS as C# bases and the emitted class carries its base's implementations.</para></summary>
    public IReadOnlyList<string> ImplementedCsTypes => [CsName];

    private readonly Dictionary<string, OoMethodSymbol> _protos = new(CobolNames.Comparer);
    public IReadOnlyList<OoMethodSymbol> Prototypes => _protoList;
    private readonly List<OoMethodSymbol> _protoList = [];

    internal bool TryAddPrototype(OoMethodSymbol m)
    {
        if (!_protos.TryAdd(m.ExternalizedName, m)) return false;   // the roster key (PB303)
        _protoList.Add(m);
        return true;
    }

    /// <summary>This interface's OWN prototype of the given roster key (<see cref="OoMethodSymbol.ExternalizedName"/>),
    /// or null — the question §11.7.3 SR4 b) asks of every interface in the INHERITS closure.</summary>
    internal OoMethodSymbol? FindOwnPrototype(string externalizedName) => _protos.GetValueOrDefault(externalizedName);

    /// <summary>Every interface this one inherits from, directly or indirectly (§9.3.10 — "the inheriting interface has
    /// all the method specifications defined for the inherited interface definition or definitions"), each once and never
    /// this interface itself, in discovery order. Terminates over a cyclic graph (§11.6.3 SR3 reports the cycle).</summary>
    public IEnumerable<OoInterfaceSymbol> InheritedClosure()
    {
        var visited = new HashSet<OoInterfaceSymbol> { this };
        var queue = new Queue<OoInterfaceSymbol>(Inherits);
        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            if (!visited.Add(i)) continue;
            yield return i;
            foreach (var b in i.Inherits) queue.Enqueue(b);
        }
    }

    /// <summary>The prototype the inheriting interface presents for a method-name inherited from SEVERAL interfaces:
    /// §11.6.3 SR5 requires "this interface conforms to all inherited interfaces", so one inherited prototype shall
    /// conform to every other; <c>OoConformance.ValidateInterfaceInheritance</c> finds it and records it here
    /// (<see cref="AllPrototypes"/> reads it). Keyed by method-name, like <see cref="AllPrototypes"/>.</summary>
    private readonly Dictionary<string, OoMethodSymbol> _conformingChoice = new(CobolNames.Comparer);

    internal void ChooseInheritedPrototype(OoMethodSymbol proto) => _conformingChoice[proto.Name] = proto;

    /// <summary>The prototypes this interface PRESENTS for a name it inherits from several interfaces
    /// (<see cref="ChooseInheritedPrototype"/>) — each needs a C# member of its own on the emitted interface, because
    /// C# member lookup over two base interfaces that both declare <c>SPEAK()</c> is ambiguous (CS0121) at every call
    /// through a reference to THIS interface, where §11.6.3 SR5 says the inheriting interface has one method of that
    /// name (<c>OoEmitter.EmitInterfaceUnit</c>).</summary>
    internal IReadOnlyCollection<OoMethodSymbol> PresentedInherited => _conformingChoice.Values;

    /// <summary>The interface's FULL method surface: own prototypes + the INHERITS closure (§9.3.8.2.2), ONE prototype per
    /// name. An inherited name cannot also be an own prototype (§11.7.3 SR4 b), refused at Build), and a name inherited
    /// from several interfaces presents the prototype every other one conforms to
    /// (<see cref="ChooseInheritedPrototype"/>; §11.6.3 SR5 is validated by
    /// <c>OoConformance.ValidateInterfaceInheritance</c> after the prototypes' formals bind) — before that pass, and for
    /// a name with one prototype, discovery order.</summary>
    public IEnumerable<OoMethodSymbol> AllPrototypes()
    {
        var seen = new HashSet<string>(CobolNames.Comparer);
        var stack = new Stack<OoInterfaceSymbol>();
        var visited = new HashSet<OoInterfaceSymbol>();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var i = stack.Pop();
            if (!visited.Add(i)) continue;
            foreach (var m in i._protoList)
                if (seen.Add(m.Name))
                    yield return _conformingChoice.GetValueOrDefault(m.Name) ?? m;
            foreach (var b in i.Inherits) stack.Push(b);
        }
    }
}
