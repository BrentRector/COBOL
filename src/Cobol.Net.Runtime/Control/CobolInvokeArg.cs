// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Runtime;

/// <summary>
/// One argument (or the RETURNING slot) crossing a UNIVERSAL object-reference dispatch
/// (<see cref="CobolObject.__CobolInvoke"/>; OO deep-dive D10/D-U2). The <see cref="Descriptor"/> is the
/// caller-computed conformance descriptor (ONE encoding shared with the compile-time strict-conformance
/// rule — <c>OoConformance.ConformanceDescriptor</c>, the D-U3 no-drift invariant): the generated callee
/// switch compares it for string equality — an argument that differs means the method does not MATCH (ISO §9.3.6
/// match rule 3; resolution continues and ends in EC-OO-METHOD), a RETURNING descriptor that differs is §14.9.23.4
/// GR7c's EC-OO-UNIVERSAL (conformance through a universal receiver is checked AT RUNTIME, §9.3.8.2.1 NOTE). The mutable
/// <see cref="Value"/> is the BY REFERENCE write-back channel — everything crossing a universal dispatch is
/// implicitly BY REFERENCE (§14.9.23.3 SR6): the caller boxes per its own crossing form, the callee unboxes
/// (descriptor equality makes the cast total), runs, and re-boxes; the caller copies out.
/// </summary>
public sealed class CobolInvokeArg(string descriptor, object? value = null, bool omitted = false)
{
    /// <summary>The descriptor a spelled OMITTED argument carries (ISO §14.9.23.2 — the OMITTED operand has no
    /// description; §9.3.6 match rule 3 b) admits it only against an OPTIONAL formal, "and this parameter is
    /// considered to match exactly", so the callee switch checks the formal's OPTIONAL phrase instead of a
    /// descriptor for it).</summary>
    public const string OmittedDescriptor = "OMITTED";

    /// <summary>A spelled OMITTED argument (kb/Work PB757).</summary>
    public static CobolInvokeArg OmittedArgument() => new(OmittedDescriptor, null, omitted: true);

    public string Descriptor { get; } = descriptor;
    public object? Value { get; set; } = value;

    /// <summary>True when the omitted-argument condition for the corresponding formal shall be true in the
    /// invoked method (ISO §14.9.23.4 GR9; §8.8.4.8.4 GR1) — a spelled OMITTED argument, or an identifier that
    /// is itself a formal parameter for which that condition is true (GR1 c), whose descriptor still conforms.</summary>
    public bool Omitted { get; } = omitted;
}
