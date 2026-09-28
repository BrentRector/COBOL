// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The run unit's ONE current FUNCTION RANDOM pseudo-random sequence (ISO §15.75; kb/Work PB307). An instance lives
/// on <see cref="RunUnit"/> beside the other run-unit-lifetime stores, because §15.75.3 r4 scopes the implementor
/// seed to "the first reference to this function in the run unit" — so run unit #2 in one process begins the
/// implementor's sequence rather than CONTINUING run unit #1's (the defect when this state was a process-global
/// <c>static</c> on <see cref="CobolIntrinsics"/>). r3 (a seeded reference starts a new sequence) and r5 (an
/// argument-less reference returns the next number of the current one) both act INSIDE that run unit.
/// </summary>
/// <remarks>The implementor seed of r4 is <c>docs/CONFORMANCE.md#DOC-A.1-144</c>: OS entropy — the parameterless
/// <see cref="System.Random"/> is .NET's xoshiro256** seeded from OS entropy, so there is no fixed seed and a
/// sequence is reproducible only from an explicit <c>FUNCTION RANDOM(seed)</c>. The generator is created lazily at
/// the run unit's first argument-less reference, which is exactly r4's "first reference".</remarks>
public sealed class RandomSequence
{
    private Random? _current;

    /// <summary>§15.75.3 r5 (r4 at the run unit's first reference): the next number of the current sequence.</summary>
    public double Next() => (_current ??= new Random()).NextDouble();

    /// <summary>§15.75.3 r3: a seeded reference starts a NEW sequence and returns its first number. The seed is the
    /// already-reduced generator state (<c>CobolIntrinsics.Random(Int128)</c> owns the DOC-A.1-145 reduction).</summary>
    public double Restart(int seed)
    {
        _current = new Random(seed);
        return _current.NextDouble();
    }
}
