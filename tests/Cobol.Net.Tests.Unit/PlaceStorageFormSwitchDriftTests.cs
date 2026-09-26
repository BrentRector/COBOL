// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SWITCH OVER A <c>Place</c>'s STORAGE FORM MUST NOT MEET A <c>PlaceDecorator</c> IN ITS DEFAULT ARM
/// (kb/Work PB393).
/// <para>A <c>Place</c> is either a STORAGE form — <c>MemberPlace</c>, <c>RedefViewPlace</c>,
/// <c>DynTablePlace</c>, <c>CapacityRegisterPlace</c>, <c>RenamesPlace</c> — or a <c>PlaceDecorator</c> over one,
/// which answers a question about the OPERAND (its occurs-depending extent, its reference modifier, its as-if
/// elementary image view) and never changes where the storage is. A consumer that asks "which storage form is
/// this?" and writes <c>_ =&gt; null</c> therefore rejects, silently and at run time, every operand that happens
/// to carry a decoration — while the identical undecorated operand works.</para>
/// <para>That is not hypothetical: it was measured three times over. An occurs-depending group is an
/// <c>OdoGroupPlace</c> wrapping a <c>MemberPlace</c>, and it fell into the default arm of INITIALIZE's receiver
/// cursor factory (plain COBOL-85 <c>INITIALIZE</c> of an ODO group aborted the run unit), of MOVE
/// CORRESPONDING's <c>CorrAccess.Create</c> (<c>SUBTRACT CORRESPONDING</c> over two ODO groups aborted — a
/// group kind §14.9.44.3 SR6 admits BY NAME), and of <c>FUNCTION LENGTH</c>'s variable-length sum, which peeled
/// exactly one known decorator kind and staged everything else loud.</para>
/// <para>So the rule is enforced here rather than remembered: a storage-form switch either asks
/// <see cref="CobolNet.Binding.Model.Place.Undecorated"/>, or handles decorators itself (an arm naming
/// <c>PlaceDecorator</c> or any concrete decorator type), or carries an adjudicated exemption below with its
/// reason. A NEW decorator kind — or a new switch — then cannot silently rejoin the hole.</para>
/// </summary>
public sealed class PlaceStorageFormSwitchDriftTests
{
    /// <summary>The concrete STORAGE forms — a switch arm naming one of these is asking "where does this live?".
    /// (Kept as a literal list because it is the QUESTION being detected, not the model: the model itself is
    /// asserted against the assembly by <see cref="StorageFormListIsComplete"/> below.)</summary>
    private static readonly string[] StorageForms =
    [
        "MemberPlace", "RedefViewPlace", "DynTablePlace", "CapacityRegisterPlace", "RenamesPlace",
        // A REPORT SECTION sum counter (ISO §13.18.54.4 GR1/GR5/GR12; kb/Work PB840): engine state rather than
        // program storage, but a LEAF all the same — a switch over storage forms that meets it and falls into a
        // catch-all is the very defect this detector exists for, and GR12 makes it a RECEIVER too.
        "ReportSumCounterPlace",
        "DebugRegisterPlace",
        // The predefined object reference EXCEPTION-OBJECT (ISO §8.4.3.6.3 SR2; kb/Work PB922): a read-only VIEW
        // over the run unit's one ExceptionState rather than program storage, but a LEAF exactly as the CAPACITY
        // and DEBUG-ITEM registers are — a storage-form switch that meets it in a catch-all is this detector's
        // subject.
        "ExceptionObjectPlace",
        // A report's PAGE-COUNTER (ISO §8.4.3.15; kb/Work PB429): engine state like the sum counter beside it,
        // and §8.4.3.15.3 SR1 admits it "may be referenced in any context where an integer data item may
        // appear" — so it is a RECEIVER as well as a source, and a storage-form switch that meets it in a
        // catch-all is exactly the defect this detector exists for.
        "ReportPageCounterPlace",
    ];

    /// <summary>Switches that name a storage form, have a catch-all arm, and are neither
    /// <c>Undecorated</c>-based nor decorator-aware — each with the reason it is nonetheless correct. Keyed
    /// <c>file:subject</c>. Adding a row is an adjudication, not a formality.</summary>
    private static readonly Dictionary<string, string> Exempt = new(StringComparer.Ordinal)
    {
        // Moved with the method from CallBinder.cs (kb/Work PB1418: the proof is shared by CALL and the
        // function-identifier, so it lives in ParameterConformance).
        ["ParameterConformance.cs:core"] =
            "ScreenBitAlignment peels the decorations ITSELF in the enclosing `while (core is PlaceDecorator "
            + "dec)` loop — accumulating each reference modifier's leftmost boolean position as it goes, which "
            + "is why it cannot use the one-shot Undecorated — so `core` is a storage place by construction. Its "
            + "default arm is also a documented ACCEPT (ISO §14.9.4.3 SR6/SR8, §8.4.3.2.3 SR14: a shape the bit "
            + "walk cannot model is never rejected), not a loud.",
        ["ProgramEmitter.cs:p"] =
            "PrefixPlace re-anchors a CONTAINER-resolved FILE STATUS item, which ISO §12.4.5.8.3 SR1 makes a "
            + "two-character alphanumeric elementary item and §12.4.5.8 SR1 forbids an OCCURS on — so no "
            + "decoration can reach it (no reference modification of a status item is written by the resolver, no "
            + "ODO extent, no group image). Its default arm is a documented fall-back to the caller's loud guard, "
            + "never a silent wrong-storage store.",
    };

    /// <summary>Every C# source file of the five product assemblies.</summary>
    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !p.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}"));

    [Fact]
    public void StorageFormSwitchesArePlaceDecoratorAware()
    {
        var offenders = new List<string>();
        foreach (string path in SourceFiles())
        {
            string text = Regex.Replace(File.ReadAllText(path), @"//[^\r\n]*", "");
            foreach (var (subject, body) in Switches(text))
            {
                if (!StorageForms.Any(f => Regex.IsMatch(body, $@"\b{f}\b\s*(\w+\s*)?(=>|:|\{{|\bwhen\b)")))
                    continue;                                            // not a storage-form switch
                if (!Regex.IsMatch(body, @"(^|\W)_\s*=>") && !Regex.IsMatch(body, @"\bdefault\s*:"))
                    continue;                                            // exhaustive by kind — fine
                if (AsksUndecorated(subject, text)) continue;
                if (Regex.IsMatch(body, @"\b(PlaceDecorator|RefModPlace|OdoGroupPlace|GroupImagePlace"
                                      + @"|NumericImagePlace|BitImagePlace|NatImagePlace|TableAllPlace)\b"))
                    continue;                                            // decorator-aware in its own arms
                string key = $"{Path.GetFileName(path)}:{subject}";
                if (Exempt.ContainsKey(key)) continue;
                offenders.Add(key);
            }
        }
        Assert.True(offenders.Count == 0,
            "A switch over a Place's STORAGE form meets a PlaceDecorator in its catch-all arm and rejects the "
            + "decorated operand (kb/Work PB393). Switch on `place.Undecorated`, handle the decorators in the "
            + "switch itself, or add an adjudicated exemption:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>The exemption list is a liability, so it may not rot: every key must still name a switch this
    /// scan finds. A row left behind after its switch was fixed would silently excuse the NEXT one written
    /// there.</summary>
    [Fact]
    public void EveryExemptionStillNamesALiveSwitch()
    {
        var live = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in SourceFiles())
        {
            string text = Regex.Replace(File.ReadAllText(path), @"//[^\r\n]*", "");
            foreach (var (subject, body) in Switches(text))
                if (StorageForms.Any(f => Regex.IsMatch(body, $@"\b{f}\b\s*(\w+\s*)?(=>|:|\{{|\bwhen\b)")))
                    live.Add($"{Path.GetFileName(path)}:{subject}");
        }
        string[] stale = [.. Exempt.Keys.Where(k => !live.Contains(k))];
        Assert.True(stale.Length == 0,
            "These PlaceDecorator exemptions no longer name a storage-form switch — delete them:\n  "
            + string.Join("\n  ", stale));
    }

    /// <summary>⛔ The <see cref="StorageForms"/> list above is the detector's own input, so a NEW storage form
    /// added to the model would leave the detector half-blind. This asserts it against the assembly: every
    /// concrete <c>Place</c> that is NOT a <c>PlaceDecorator</c> is named in the list.</summary>
    [Fact]
    public void StorageFormListIsComplete()
    {
        var placeType = typeof(CobolNet.Binding.Model.Place);
        var decorator = typeof(CobolNet.Binding.Model.PlaceDecorator);
        string[] missing =
        [
            .. placeType.Assembly.GetTypes()
                .Where(t => !t.IsAbstract && placeType.IsAssignableFrom(t) && !decorator.IsAssignableFrom(t))
                .Select(t => t.Name)
                .Where(n => !StorageForms.Contains(n, StringComparer.Ordinal))
                .OrderBy(n => n, StringComparer.Ordinal)
        ];
        Assert.True(missing.Length == 0,
            "New Place STORAGE forms are not in this test's detector list, so a switch over them would not be "
            + "checked for decorator-awareness (kb/Work PB393):\n  " + string.Join("\n  ", missing));
    }

    /// <summary>True when the switch subject is the UNDECORATED place — written inline, or held in a local the
    /// same file assigns from <c>.Undecorated</c>. The indirection is admitted because the storage place is
    /// routinely needed twice (once for its item, once for the switch), and forcing it to be re-derived at the
    /// switch would trade a real defect for a cosmetic one.</summary>
    private static bool AsksUndecorated(string subject, string text) =>
        subject.Contains("Undecorated", StringComparison.Ordinal)
        || (Regex.IsMatch(subject, @"\A[A-Za-z_]\w*\z")
            && Regex.IsMatch(text, $@"\b{Regex.Escape(subject)}\s*=\s*[^;]*\.Undecorated\b"));

    /// <summary>Every <c>X switch { … }</c> expression and <c>switch (X) { … }</c> statement in the text, as
    /// (subject, body) — the body found by brace matching so nested switches and object initializers inside an
    /// arm stay with their own switch.</summary>
    private static IEnumerable<(string Subject, string Body)> Switches(string text)
    {
        foreach (Match m in Regex.Matches(text,
            @"(?:(?<expr>[A-Za-z_][\w\.\?\!]*(?:\([^()\r\n]*\))?)\s+switch\s*\{)"
            + @"|(?:\bswitch\s*\(\s*(?<stmt>[^()\r\n]*(?:\([^()\r\n]*\))?[^()\r\n]*)\s*\)\s*\{)"))
        {
            int open = text.IndexOf('{', m.Index + m.Length - 1);
            if (open < 0) continue;
            int depth = 0, i = open;
            for (; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0) break;
            }
            if (i >= text.Length) continue;
            string subject = (m.Groups["expr"].Success ? m.Groups["expr"].Value : m.Groups["stmt"].Value).Trim();
            yield return (subject, text[(open + 1)..i]);
        }
    }
}
