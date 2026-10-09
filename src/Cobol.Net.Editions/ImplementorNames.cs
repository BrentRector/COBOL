// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using CobolNet.Runtime;

namespace CobolNet.Editions;

/// <summary>The three types of system-name a SPECIAL-NAMES implementor-name entry may name (ISO §12.3.7.2:
/// <c>switch-name-1 …</c> | <c>feature-name-1 IS mnemonic-name-2</c> | <c>device-name-1 IS mnemonic-name-3</c>).
/// §8.3.2.3.1: "<i>Within an implementation, a given system-name shall not belong to more than one of the following
/// types of system-names: device-name, feature-name, and switch-name.</i>" — so the KIND of an entry is a property
/// of the NAME, never of how the entry is spelled.</summary>
public enum SystemNameKind
{
    /// <summary>device-name-1 — its mnemonic-name-3 may be specified only in ACCEPT and DISPLAY (§12.3.7.3 SR7).</summary>
    Device,
    /// <summary>feature-name-1 — its mnemonic-name-2 may be specified only in WRITE (§12.3.7.3 SR6).</summary>
    Feature,
    /// <summary>switch-name-1 — an external switch (§12.3.7.4 GR2); its mnemonic-name-1 may be specified only in
    /// SET (§12.3.7.3 SR5), and only the switch arm may carry ON/OFF STATUS condition-names.</summary>
    Switch,
}

/// <summary>What a device-name can do — the §14.9.1.3 SR2 / §14.9.11.3 SR2 capability the ACCEPT FROM and
/// DISPLAY UPON binders screen a mnemonic against.</summary>
[Flags]
public enum DeviceCapability
{
    /// <summary>Not a device (a feature-name or switch-name row).</summary>
    None = 0,
    /// <summary>A device ACCEPT may take input from (§14.9.1.3 SR2) — the process standard input.</summary>
    Input = 1,
    /// <summary>A device DISPLAY may transfer data to (§14.9.11.3 SR2) — the process standard output…</summary>
    Output = 2,
    /// <summary>…or, with this flag, the process standard ERROR stream.</summary>
    StandardError = 4,
}

/// <summary>How a feature-name positions the printed page when its mnemonic is the ADVANCING operand of a WRITE
/// (§14.9.51.4 GR25 d): "<i>the representation of the printed page is advanced according to the rules specified
/// by the implementor for that hardware device.</i>"</summary>
public enum FeatureAdvance
{
    /// <summary>Not a feature-name row.</summary>
    None = 0,
    /// <summary>Suppress spacing — the line is presented with NO vertical advance (a zero-line advance).</summary>
    SuppressSpacing,
    /// <summary>Skip to printer channel 1, the top of the next page — exactly the <c>ADVANCING PAGE</c> advance.</summary>
    TopOfPage,
}

/// <summary>ONE ROW of the implementor system-name table: a name this implementation makes available in the
/// switch-name-1 / feature-name-1 / device-name-1 position of the SPECIAL-NAMES paragraph, and what it means.</summary>
/// <param name="Name">The canonical spelling (upper case); matched case-insensitively (§8.1.3.2 GR3 a — "COBOL basic
/// letters appearing elsewhere within the compilation group are treated in a case-insensitive manner").</param>
/// <param name="Kind">The ONE system-name type the name belongs to (§8.3.2.3.1).</param>
/// <param name="Device">For a device-name, its input/output capability; <see cref="DeviceCapability.None"/> otherwise.</param>
/// <param name="Advance">For a feature-name, its WRITE ADVANCING positioning rule; <see cref="FeatureAdvance.None"/> otherwise.</param>
public sealed record ImplementorName(string Name, SystemNameKind Kind,
    DeviceCapability Device = DeviceCapability.None, FeatureAdvance Advance = FeatureAdvance.None);

/// <summary>
/// ⛔ THE ONE IMPLEMENTOR SYSTEM-NAME TABLE — this implementation's whole answer to ISO §12.3.7.3 SR8, "<i>The
/// implementor shall specify the names that are available for switch-name-1, feature-name-1, and
/// device-name-1.</i>", and to Annex A.1 items 189 (device-name), 190 (feature-name) and 191 (switch-name).
/// Adding the next name is A ROW HERE and nothing else: the SPECIAL-NAMES binder refuses every entry whose name is
/// not a row (COBOLNET2241 — kb/Work PB862) and classifies every mnemonic by its row's <see cref="SystemNameKind"/>,
/// and the ACCEPT / DISPLAY / WRITE / SET binders read the row's capability. There is no second list: the
/// <c>AcceptInputDevices</c> / <c>DisplayOutputDevices</c> sets that used to live in the ACCEPT/DISPLAY binder, and
/// the "any switch-name is accepted" leniency of the switch registry, are this table now.
/// <para><b>device-names</b> (A.1 item 189, documented as DOC-A.1-2 / DOC-A.1-59 before this table existed):
/// <c>CONSOLE</c> (input AND output — standard input / standard output), <c>SYSIN</c> (input — standard input),
/// <c>SYSOUT</c> (output — standard output), <c>SYSERR</c> (output — standard error). §12.3.7.3 SR7's "additional
/// restrictions": an input-only device may not be a DISPLAY target, an output-only one may not be an ACCEPT
/// source (COBOLNET0817).</para>
/// <para><b>feature-names</b> (A.1 items 190 and 222): <c>C01</c> — skip to channel 1, the top of the next page
/// (the <c>ADVANCING PAGE</c> advance); <c>CSP</c> — suppress spacing (a zero-line advance: the record is presented
/// on the current line). ⚖ Surveyed (<c>survey_compilers_on_latitude</c> / <c>follow_gnucobol_on_split_latitude</c>):
/// IBM Enterprise COBOL, Micro Focus and GnuCOBOL all spell these two, with these meanings. Channels 2–12 (C02–C12)
/// are NOT provided: each names a stop on a physical carriage-control tape, and a line-sequential print file has no
/// forms-control model to position against, so a row for them would have to invent one.</para>
/// <para><b>switch-names</b> (A.1 item 191): <c>SWITCH-0</c> … <c>SWITCH-36</c> (GnuCOBOL's set) and
/// <c>UPSI-0</c> … <c>UPSI-7</c> (IBM's User Program Status Indicators). Every switch may be referenced by SET
/// (§12.3.7.4 GR3); the scope of every switch is the RUN UNIT and the external facility that sets its initial status
/// is the process environment variable <c>COBOL_&lt;SWITCH-NAME&gt;</c> (§12.3.7.4 GR4 — <c>SwitchStore</c>).</para>
/// </summary>
public static class ImplementorNames
{
    /// <summary>Every available name, keyed case-insensitively. A name is ONE key, so §8.3.2.3.1's "shall not belong
    /// to more than one" type is true by construction (<c>ImplementorNamesDriftTests</c> pins it anyway).</summary>
    public static FrozenDictionary<string, ImplementorName> All { get; } = Build();

    /// <summary>The row for <paramref name="word"/>, or null when this implementation makes no such name available.</summary>
    public static ImplementorName? Lookup(string word) => All.GetValueOrDefault(word);

    /// <summary>The word a diagnostic uses for <paramref name="kind"/> ("device-name", "feature-name", "switch-name").</summary>
    public static string KindWord(SystemNameKind kind) => kind switch
    {
        SystemNameKind.Device => "device-name",
        SystemNameKind.Feature => "feature-name",
        _ => "switch-name",
    };

    /// <summary>The available names by type, for a diagnostic — GENERATED from the table, so the message can never
    /// drift from it. A numbered run (<c>SWITCH-0</c> … <c>SWITCH-36</c>) prints as its two ends.</summary>
    public static string Describe() => string.Join("; ",
        Enum.GetValues<SystemNameKind>().Select(k => KindWord(k) + "s " + string.Join(", ",
            All.Values.Where(r => r.Kind == k)
                .GroupBy(r => r.Name.TrimEnd(Digits), StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => g.Count() == 1 ? g.Single().Name
                    : $"{g.Key}{g.Min(r => int.Parse(r.Name[g.Key.Length..]))}…"
                      + $"{g.Key}{g.Max(r => int.Parse(r.Name[g.Key.Length..]))}"))));

    private static readonly char[] Digits = "0123456789".ToCharArray();

    /// <summary>The device-names with their capabilities, for a COBOLNET0817 message — generated from the table.</summary>
    public static string DescribeDevices() => string.Join(", ", All.Values
        .Where(r => r.Kind == SystemNameKind.Device)
        .OrderBy(r => r.Name, StringComparer.Ordinal)
        .Select(r => $"{r.Name} ({string.Join(" and ", new[]
            {
                r.Device.HasFlag(DeviceCapability.Input) ? "input" : null,
                r.Device.HasFlag(DeviceCapability.Output)
                    ? r.Device.HasFlag(DeviceCapability.StandardError) ? "output to standard error" : "output" : null,
            }.OfType<string>())})"));

    private static FrozenDictionary<string, ImplementorName> Build()
    {
        var rows = new List<ImplementorName>
        {
            new("CONSOLE", SystemNameKind.Device, DeviceCapability.Input | DeviceCapability.Output),
            new("SYSIN", SystemNameKind.Device, DeviceCapability.Input),
            new("SYSOUT", SystemNameKind.Device, DeviceCapability.Output),
            new("SYSERR", SystemNameKind.Device, DeviceCapability.Output | DeviceCapability.StandardError),
            new("C01", SystemNameKind.Feature, Advance: FeatureAdvance.TopOfPage),
            new("CSP", SystemNameKind.Feature, Advance: FeatureAdvance.SuppressSpacing),
        };
        for (int i = 0; i <= 36; i++) rows.Add(new($"SWITCH-{i}", SystemNameKind.Switch));
        for (int i = 0; i <= 7; i++) rows.Add(new($"UPSI-{i}", SystemNameKind.Switch));
        return rows.ToFrozenDictionary(r => r.Name, CobolNames.Comparer);
    }
}
