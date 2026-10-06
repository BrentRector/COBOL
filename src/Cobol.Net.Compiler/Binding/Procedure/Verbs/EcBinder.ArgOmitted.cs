// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ THE *-ARG-OMITTED RULE, BIND HALF (kb/Work PB971). ISO §14.9.4.4 GR12, §8.4.3.2.4 GR8 and §14.9.23.4 GR10 are
/// ONE rule written three times, keyed to the kind of the ACTIVATED element that owns the formal: "If a parameter
/// for which the omitted-argument condition is true is referenced in [a called program | an activated function |
/// an invoked method], except as an argument or in the omitted-argument condition, the [EC-PROGRAM | EC-FUNCTION
/// | EC-OO]-ARG-OMITTED exception condition is set to exist."
/// <para>Two facts realize it, and both key on <see cref="ElementKindOf"/> so they cannot disagree about which of
/// the three names applies:</para>
/// <list type="number">
/// <item>THE REFERENCE: every formal parameter's root item carries an <see cref="OmittedFormalGuard"/>
/// (<see cref="MarkFormals"/>), so every access path rooted at it renders through the runtime guard
/// <see cref="OmittedFormal"/> — the raise site.</item>
/// <item>THE CHECKING SCOPE: every statement of the element queries >>TURN for the kind's name
/// (<see cref="ArgOmittedName"/>), so the statement's §7.3.25.4 scope sets the flag the raise tests. Before PB971
/// the name was queried only for a CALL / CANCEL (as if the ACTIVATOR raised it), so the callee's referencing
/// statement never enabled it and the condition could not be raised at all, in any of the three kinds.</item>
/// </list>
/// </summary>
internal sealed partial class EcBinder
{
    /// <summary>The activated-element kind of a source element — the ONE mapping from the binder's
    /// <see cref="SourceElementKind"/> to the runtime's key. A prototype's procedure division is its kind's
    /// (§14.2.2 SR10's nouns).</summary>
    internal static ActivatedElementKind ElementKindOf(SourceElementKind kind) => kind switch
    {
        SourceElementKind.Program or SourceElementKind.ProgramPrototype => ActivatedElementKind.Program,
        SourceElementKind.FunctionDefinition or SourceElementKind.FunctionPrototype => ActivatedElementKind.Function,
        SourceElementKind.MethodDefinition => ActivatedElementKind.Method,
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>The *-ARG-OMITTED name the CURRENT element's statements enable (§7.3.25.4 GR6 scope).</summary>
    private string ArgOmittedName => OmittedFormal.ConditionName(ElementKindOf(ctx.SourceElement));

    /// <summary>Mark a PROGRAM or FUNCTION unit's formals (called once the unit's data is bound, before any
    /// contained program bridges its GLOBAL roots — the bridge carries the presence member too). A no-op when the
    /// compilation group enables the kind's condition nowhere: the reference then renders unguarded, because the
    /// raise could never fire (the zero-scaffolding invariant).</summary>
    internal static void MarkFormals(IReadOnlyList<LinkageFormal> formals, SourceElementKind element, TurnState turn)
    {
        var kind = ElementKindOf(element);
        if (formals.Count == 0 || !turn.AnyEnabledFor(OmittedFormal.ConditionName(kind))) return;
        foreach (var f in formals)
            f.Item.OmittedGuard = new OmittedFormalGuard(OmittedFormalGuard.PresenceMember(f.Item), kind,
                f.Item.CobolName ?? "", f.CarrierResident ? f.CarrierField : null);
    }

    /// <summary>Mark every method's formals (§14.9.23.4 GR10). The presence is the method's own omitted flag
    /// parameter (<see cref="OoFormal.OmittedFlag"/>), in scope throughout the method body.</summary>
    internal static void MarkFormals(IEnumerable<OoMethodSymbol> methods, TurnState turn)
    {
        if (!turn.AnyEnabledFor(OmittedFormal.ConditionName(ActivatedElementKind.Method))) return;
        foreach (var m in methods)
            if (m.Binding is { } b)
                foreach (var f in b.Formals)
                    // A resident formal's root text is its carrier's `.Value` (kb/Work PB2087), passed through the guard.
                    f.Item.OmittedGuard = new OmittedFormalGuard(f.OmittedFlag, ActivatedElementKind.Method,
                        f.Item.CobolName ?? "", f.CarrierResident ? f.CarrierLocal : null);
    }
}
