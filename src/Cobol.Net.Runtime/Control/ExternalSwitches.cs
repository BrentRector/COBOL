// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The static facade over the run unit's <see cref="SwitchStore"/> (the emitted surface — switch-status
/// conditions emit <c>ExternalSwitches.Get</c> and SET Format 3 emits <c>ExternalSwitches.Set</c>; kept
/// name-stable pre-G8). Forwards to <c>RunUnit.Current.Switches</c>.
/// </summary>
public static class ExternalSwitches
{
    /// <inheritdoc cref="SwitchStore.Get"/>
    public static bool Get(string implementorName) => RunUnit.Current.Switches.Get(implementorName);

    /// <inheritdoc cref="SwitchStore.Set"/>
    public static void Set(string implementorName, bool isOn) => RunUnit.Current.Switches.Set(implementorName, isOn);
}
