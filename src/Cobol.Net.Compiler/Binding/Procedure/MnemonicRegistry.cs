// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;
using CobolNet.Editions;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>The SPECIAL-NAMES {mnemonic-name → implementor system-name} registry (ISO §12.3.7.2: <c>switch-name-1
/// IS mnemonic-name-1</c> | <c>feature-name-1 IS mnemonic-name-2</c> | <c>device-name-1 IS mnemonic-name-3</c>),
/// built once per program unit by walking the parse tree UP from the referencing statement (no bound
/// environment-division model exists yet). Every ENCLOSING program unit contributes too — SPECIAL-NAMES
/// declarations are visible to contained source units (§12.3.7.4 GR1), the nearest declaration winning
/// (<c>TryAdd</c>, innermost first).
/// <para>⛔ The value is the <see cref="ImplementorName"/> ROW, so every consumer asks the row's KIND, never the
/// spelling of the entry (kb/Work PB862). This map used to hold every two-word entry WITHOUT an ON/OFF phrase as a
/// "device", so <c>SWITCH-1 IS SW1</c> made SW1 a legal WRITE ADVANCING operand and <c>WIBBLE WOBBLE</c> a
/// device named WIBBLE. §12.3.7.3 SR5/SR6/SR7 key the use of a mnemonic on the TYPE of the name it is
/// associated with — SET for a switch, WRITE for a feature, ACCEPT/DISPLAY for a device — and §8.3.2.3.1 makes
/// that type a property of the name. An entry naming an unavailable name contributes nothing (the SPECIAL-NAMES
/// binder reported it, COBOLNET2241); a switch entry with no mnemonic declares nothing a statement can
/// reference.</para>
/// (P7 Step 10h — moved off the ACCEPT partial onto <see cref="BinderContext"/>: ACCEPT-FROM, DISPLAY-UPON and
/// the WRITE SR13 / SR16 / ADVANCING legs share the ONE per-unit map; since kb/Work PB454 so do SET Format 3's
/// switch operand (§14.9.39.3 SR5) and the condition binder's "that is a switch mnemonic" diagnostic, which used
/// to read a second, switch-only map built by a second walk of the same entries.)
/// <para>⛔ A LOOKUP IS ONE OF TWO NAMED OPERATIONS (kb/Work PB3153), as for the data binder's other
/// configuration-section names (<see cref="ConfigurationNames{T}"/>): <see cref="TryUse"/> resolves a reference the
/// binder commits to and records it as a §8.3.2.2 use of the word as a mnemonic-name in the referring source
/// element's census (<see cref="DataBinder.RecordConfigurationNameUse"/>) — a contained program or a method uses
/// names its container declares, so no declaration of its own ever announced them; <see cref="TryPeek"/> reads
/// without recording, for a diagnostic that names what the word is. The map itself is private.</para></summary>
internal sealed class MnemonicRegistry(DataBinder data)
{
    private Dictionary<string, ImplementorName>? _map;

    /// <summary>Resolve the mnemonic-name <paramref name="name"/> written at <paramref name="at"/>, recording the use
    /// (§8.3.2.2). False, recording nothing, when no SPECIAL-NAMES entry in scope declares it.</summary>
    public bool TryUse(Antlr4.Runtime.RuleContext at, string name, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out ImplementorName row)
    {
        if (!Of(at).TryGetValue(name, out row)) return false;
        data.RecordConfigurationNameUse(at, name, UserWordKind.MnemonicName);
        return true;
    }

    /// <summary>The row <paramref name="name"/> names, read WITHOUT recording a use: a diagnostic's question.</summary>
    public bool TryPeek(IParseTree at, string name, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out ImplementorName row) =>
        Of(at).TryGetValue(name, out row);

    /// <summary>The per-unit map, computed lazily from any parse node INSIDE the unit.</summary>
    private Dictionary<string, ImplementorName> Of(IParseTree at)
    {
        if (_map is not null) return _map;
        var map = new Dictionary<string, ImplementorName>(CobolNames.Comparer);
        for (IParseTree? n = at; n is not null; n = n.Parent)
        {
            // kb/Work PB135: a METHOD's parse chain never meets a ProgramUnitContext (it tops out at
            // compilationGroup through the class contexts), so the device map was EMPTY inside every method
            // while the SWITCH map — built from the SAME rule through OoDriver's synthetic reparented unit —
            // resolved. The walk now reads each OO ancestor's own configuration (a method itself may not
            // declare one — §12.3.3 SR2, enforced as COBOLNET1519); TryAdd keeps the nearest scope.
            var envs = n switch
            {
                Core.ProgramUnitContext pu => DataBinder.EnvDivisions(pu),
                Core.ObjectParagraphContext op => op.environmentDivision() is { } e ? [e] : System.Array.Empty<Core.EnvironmentDivisionContext>(),
                Core.FactoryParagraphContext fp => fp.environmentDivision() is { } e ? [e] : System.Array.Empty<Core.EnvironmentDivisionContext>(),
                Core.ClassDefinitionContext cd => cd.environmentDivision() is { } e ? [e] : System.Array.Empty<Core.EnvironmentDivisionContext>(),
                Core.InterfaceDefinitionContext idf => idf.environmentDivision() is { } e ? [e] : System.Array.Empty<Core.EnvironmentDivisionContext>(),
                _ => null,
            };
            if (envs is null) continue;
            var paragraphs = envs
                .SelectMany(env => env.configurationSection()?.configurationParagraph() ?? []);
            foreach (var para in paragraphs)
                foreach (var entry in para.specialNamesParagraph()?.specialNameEntry() ?? [])
                    if (entry.implementorSwitchEntry() is { } e
                        && ImplementorNameEntry.Read(e) is { Unavailable: null, Mnemonic: { } alias, Row: { } row })
                        map.TryAdd(alias, row);
        }
        return _map = map;
    }
}
