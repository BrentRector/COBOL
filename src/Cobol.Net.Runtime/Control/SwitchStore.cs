// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The run-unit external-switch store (ISO §12.3.7): a SPECIAL-NAMES switch-name identifies an implementor-defined
/// external switch (GR2) whose on/off status is interrogated through switch-status conditions (§8.8.4.6) and
/// altered by <c>SET mnemonic-name TO ON/OFF</c> (GR3 / §14.9.39 Format 3 GR5). Implementor definition (ISO
/// implementor-defined item 191; §12.3.7 GR4 + NOTE 1 / Annex D.15): any switch-name is accepted; every switch is
/// settable; switch scope is the RUN UNIT (one switch shared by all runtime elements — an instance on
/// <see cref="RunUnit"/> since P8); the external facility that supplies the initial status is the process
/// environment — variable <c>COBOL_&lt;SWITCH-NAME&gt;</c> (hyphens become underscores, upper-cased), value
/// <c>ON</c> | <c>1</c> | <c>TRUE</c> (case-insensitive) = on, anything else or absent = off. The probed value is
/// cached on first read, so a later <c>SET</c> governs for the remainder of the run unit.
/// </summary>
public sealed class SwitchStore
{
    /// <summary>Per-run-unit switch state keyed by implementor switch-name — the §12.3.7 GR4 NOTE 1 run-unit
    /// scope (one switch referenced by all runtime elements of the run unit).</summary>
    private readonly Dictionary<string, bool> _states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The prefix of the external facility's variables: <c>COBOL_</c> — the one place the family
    /// <c>COBOL_&lt;SWITCH-NAME&gt;</c> is spelled (<see cref="RuntimeConfig"/> registers the family by it).</summary>
    public const string Prefix = "COBOL_";

    /// <summary>The environment variable that supplies an implementor switch-name's initial status:
    /// <see cref="Prefix"/> + the name with hyphens as underscores, upper-cased (<c>SWITCH-1</c> → <c>COBOL_SWITCH_1</c>).</summary>
    public static string VariableNameFor(string implementorName) => Prefix + implementorName.Replace("-", "_").ToUpperInvariant();

    /// <summary>The current status of the switch (true = ON). The first read of an unset switch probes the
    /// <c>COBOL_&lt;NAME&gt;</c> environment variable and caches the result; an absent external setting means
    /// OFF (the all-conditions-default of an uninitialized external switch).</summary>
    public bool Get(string implementorName)
    {
        if (_states.TryGetValue(implementorName, out bool state)) return state;

        string envName = VariableNameFor(implementorName);
        if (Environment.GetEnvironmentVariable(envName) is { } envValue)
        {
            state = envValue.Equals("ON", StringComparison.OrdinalIgnoreCase)
                || envValue == "1"
                || envValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            _states[implementorName] = state;
            return state;
        }
        return false;
    }

    /// <summary>Alter the switch's status (ISO §14.9.39 Format 3 GR5: the switch is modified so that a
    /// condition-name associated with it evaluates per the ON/OFF phrase; §12.3.7 GR3).</summary>
    public void Set(string implementorName, bool isOn) => _states[implementorName] = isOn;
}
