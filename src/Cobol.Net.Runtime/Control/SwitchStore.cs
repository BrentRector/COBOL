// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;

namespace CobolNet.Runtime;

/// <summary>
/// The run-unit external-switch store (ISO §12.3.7): a SPECIAL-NAMES switch-name identifies an implementor-defined
/// external switch (GR2) whose on/off status is interrogated through switch-status conditions (§8.8.4.6) and
/// altered by <c>SET mnemonic-name TO ON/OFF</c> (GR3 / §14.9.39 Format 3 GR5). Implementor definition
/// (docs/CONFORMANCE.md DOC-A.1-191; §12.3.7.4 GR4 + NOTE 1 / Annex D.15): the switch-names are the closed set
/// <see cref="Names"/> (<c>SWITCH-0</c> … <c>SWITCH-36</c>, <c>UPSI-0</c> … <c>UPSI-7</c>); every switch is settable;
/// switch scope is the RUN UNIT (one switch shared by all runtime elements — an instance on <see cref="RunUnit"/>
/// since P8); the external facility that supplies the initial status is the process environment — variable
/// <c>COBOL_&lt;SWITCH-NAME&gt;</c> (hyphens become underscores, upper-cased), value <c>ON</c> | <c>1</c> |
/// <c>TRUE</c> (case-insensitive) = on, anything else, empty or absent = off. The status is read ONCE, at the
/// switch's first interrogation, and cached whatever the environment held (an absent variable caches OFF exactly as
/// an empty one does), so a later <c>SET</c> — and nothing else — governs for the remainder of the run unit.
/// </summary>
public sealed class SwitchStore
{
    /// <summary>Per-run-unit switch state keyed by implementor switch-name — the §12.3.7 GR4 NOTE 1 run-unit
    /// scope (one switch referenced by all runtime elements of the run unit).</summary>
    private readonly Dictionary<string, bool> _states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The prefix of the external facility's variables: <c>COBOL_</c> — the one place the family
    /// <c>COBOL_&lt;SWITCH-NAME&gt;</c> is spelled (<see cref="RuntimeConfig"/> registers the family by it).</summary>
    public const string Prefix = "COBOL_";

    /// <summary>The implementor-defined switch-names (docs/CONFORMANCE.md DOC-A.1-191): <c>SWITCH-0</c> …
    /// <c>SWITCH-36</c> (GnuCOBOL's set) and <c>UPSI-0</c> … <c>UPSI-7</c> (IBM's User Program Status Indicators).
    /// The ONE place the set is written: the compiler's SPECIAL-NAMES table (<c>ImplementorNames</c>) builds its
    /// switch rows from it, and <see cref="IsVariableName"/> answers the registry's family test from it.</summary>
    public static IReadOnlyList<string> Names { get; } =
        [.. Enumerable.Range(0, 37).Select(i => $"SWITCH-{i}"), .. Enumerable.Range(0, 8).Select(i => $"UPSI-{i}")];

    private static readonly FrozenSet<string> VariableNames =
        Names.Select(VariableNameFor).ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The environment variable that supplies an implementor switch-name's initial status:
    /// <see cref="Prefix"/> + the name with hyphens as underscores, upper-cased (<c>SWITCH-1</c> → <c>COBOL_SWITCH_1</c>).</summary>
    public static string VariableNameFor(string implementorName) => Prefix + implementorName.Replace("-", "_").ToUpperInvariant();

    /// <summary>True when <paramref name="variableName"/> is a variable this store reads: <see cref="VariableNameFor"/>
    /// of one of <see cref="Names"/> — not every <c>COBOL_</c>-prefixed word (<c>COBOL_TYPO</c> is read by nothing).</summary>
    public static bool IsVariableName(string variableName) => VariableNames.Contains(variableName);

    /// <summary>The current status of the switch (true = ON). The first interrogation of a switch probes the
    /// <c>COBOL_&lt;NAME&gt;</c> environment variable once and caches the result (§12.3.7.4 GR4 leaves the facility
    /// to the implementor; DOC-A.1-191 defines it as read at the first interrogation). An absent or empty variable
    /// means OFF (the all-conditions-default of an uninitialized external switch) and is cached too, so the
    /// environment changing afterwards never changes a switch-status condition.</summary>
    public bool Get(string implementorName)
    {
        if (_states.TryGetValue(implementorName, out bool state)) return state;

        string? envValue = Environment.GetEnvironmentVariable(VariableNameFor(implementorName));
        state = envValue is not null
            && (envValue.Equals("ON", StringComparison.OrdinalIgnoreCase)
                || envValue == "1"
                || envValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase));
        _states[implementorName] = state;
        return state;
    }

    /// <summary>Alter the switch's status (ISO §14.9.39 Format 3 GR5: the switch is modified so that a
    /// condition-name associated with it evaluates per the ON/OFF phrase; §12.3.7 GR3).</summary>
    public void Set(string implementorName, bool isOn) => _states[implementorName] = isOn;
}
