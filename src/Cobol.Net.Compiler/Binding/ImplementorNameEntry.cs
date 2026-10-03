// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;

namespace CobolNet.Binding;

/// <summary>ONE switch-name / feature-name / device-name entry of a SPECIAL-NAMES paragraph, read off its parse node
/// and classified against <see cref="ImplementorNames"/> — the ONE reading both walkers of the entry share (the
/// program's switch registry, <c>DataBinder.BindImplementorNameEntry</c>, and the per-unit mnemonic registry
/// <c>Procedure.MnemonicRegistry</c>, which also reads enclosing OO scopes), so the two can never classify a name
/// differently.</summary>
/// <param name="Written">The name as written (the entry's first word).</param>
/// <param name="Row">The table row, or null when no such name is available.</param>
/// <param name="Mnemonic">The mnemonic-name, or null for a switch entry written with status phrases only.</param>
/// <param name="OnCondition">The ON STATUS condition-name, if written.</param>
/// <param name="OffCondition">The OFF STATUS condition-name, if written.</param>
public readonly record struct ImplementorNameEntry(string Written, ImplementorName? Row, string? Mnemonic,
    string? OnCondition, string? OffCondition)
{
    /// <summary>Read and classify <paramref name="e"/>. The grammar guarantees a mnemonic, a status phrase, or both.</summary>
    public static ImplementorNameEntry Read(CobolNet.Frontend.Generated.CobolParserCore.ImplementorSwitchEntryContext e)
    {
        var words = e.cobolWord();
        string written = words[0].GetText();
        var status = e.switchStatusPhrases();
        return new(written, ImplementorNames.Lookup(written), words.Length > 1 ? words[1].GetText() : null,
            status?.switchOnClause()?.cobolWord()?.GetText(), status?.switchOffClause()?.cobolWord()?.GetText());
    }

    /// <summary>True when the entry is written in the switch arm's status form (an ON or OFF STATUS phrase).</summary>
    public bool HasStatus => OnCondition is not null || OffCondition is not null;

    /// <summary>Null when the entry names an available system-name in an arm that name may be written in; otherwise
    /// the COBOLNET2241 message (ISO §12.3.7.3 SR8). An available device-name or feature-name written with ON/OFF
    /// STATUS is refused too: only the switch-name-1 arm prints the status phrases (§12.3.7.2), and a name belongs
    /// to exactly one system-name type (§8.3.2.3.1), so the name — not the spelling — decides the arm.</summary>
    public string? Unavailable => Row is null
        ? $"SPECIAL-NAMES: '{Written}' is not a switch-name, feature-name or device-name this implementation makes "
          + "available — \"The implementor shall specify the names that are available for switch-name-1, "
          + "feature-name-1, and device-name-1\" (ISO §12.3.7.3 SR8). Available: " + ImplementorNames.Describe()
        : HasStatus && Row.Kind != SystemNameKind.Switch
            ? $"SPECIAL-NAMES: '{Written}' is a {ImplementorNames.KindWord(Row.Kind)}, and only a switch-name-1 entry "
              + "may carry ON STATUS / OFF STATUS condition-names (ISO §12.3.7.2; §12.3.7.3 SR8; a system-name belongs "
              + "to one type only, §8.3.2.3.1)"
            : null;
}
