// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Raised at run time when a generated program reaches a COBOL construct the compiler bound but could not yet
/// translate. This is the runtime half of WiseOwl COBOL's loud-failure invariant (COBOLNET_DESIGN §1.4): an
/// unsupported construct emits a guarded call that throws here, never a silent <c>// TODO</c> no-op that would let
/// a program run <i>silently wrong</i>.
/// </summary>
public sealed class NotImplementedCobolFeatureException(string feature)
    : Exception("WiseOwl COBOL: a COBOL feature that is not yet implemented was reached at run time: " + feature)
{
    /// <summary>The COBOL construct that was not implemented (for diagnostics).</summary>
    public string Feature { get; } = feature;
}

/// <summary>
/// The emit-side helpers the generated C# calls for an unsupported construct. <see cref="Run"/> is used in
/// statement position (a <c>void</c> call — no unreachable-code warning); <see cref="Value{T}"/> is used in
/// expression position (it returns <typeparamref name="T"/> by signature but always throws), so an unsupported
/// operand can sit anywhere a value is required and still fail loud at run time.
/// </summary>
public static class NotImplemented
{
    /// <summary>Throw for an unsupported construct reached in statement position.</summary>
    public static void Run(string feature) => throw new NotImplementedCobolFeatureException(feature);

    /// <summary>Throw for an unsupported construct reached in expression position (typed for the call site).</summary>
    public static T Value<T>(string feature) => throw new NotImplementedCobolFeatureException(feature);
}
