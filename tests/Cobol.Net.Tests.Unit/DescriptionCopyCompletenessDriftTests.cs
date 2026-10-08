// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DATA-DESCRIPTION COPY IS DEFINED BY AN EXCLUSION LIST, SO ITS DEFAULT IS "CARRIED" (kb/Work PB522).
///
/// <para><b>The rule.</b> ISO <b>§13.18.57.4 GR1</b> — the TYPE clause's effect "is as though the data description
/// identified by type-name-1 had been coded in place of the TYPE clause, <i>excluding</i> the level-number, name,
/// alignment, and the GLOBAL, SELECT WHEN, and TYPEDEF clauses" — and <b>§13.18.49.4 GR1</b>, the same sentence for
/// SAME AS excluding only the level-number, name, CONSTANT RECORD, EXTERNAL, GLOBAL, REDEFINES and SELECT WHEN.
/// Both state what is LEFT OUT. Every other data description clause travels, and §13.18.58.4 GR1 reproduces a
/// template's subordinate entries whole.</para>
///
/// <para><b>What went wrong.</b> Both copiers inverted that default by spelling their own field list in an object
/// initializer, where a clause that is simply absent is invisible — no diagnostic, no site at which the omission can
/// be read. <c>DataItem.GroupUsage</c>, the one field that makes an item a bit or national group, was in NEITHER
/// list, so <c>01 R TYPE T</c> over a <c>GROUP-USAGE NATIONAL</c> template bound as an ORDINARY alphanumeric group —
/// wrong class, wrong category, and <c>FUNCTION LENGTH</c> in the wrong unit (§15.50.4 r1/r2) — and, because
/// §13.16.4 GR1/GR2 imply the clause for every SUBORDINATE group, the false antecedent silently re-categorized the
/// whole subtree. SYNCHRONIZED (§13.18.55), ALIGNED (§13.18.1), ANY LENGTH (§13.18.2) and DYNAMIC LENGTH
/// (§13.18.19) were lost the same way, each measured against a byte-identical inline control.
///
/// <para>⛔ <b>What this file guards, and why a golden cannot.</b> A conformance golden proves the clauses that
/// exist TODAY travel. Only a check on the SHAPE makes the NEXT one automatic: every stored
/// <see cref="DataItem"/> property must carry a <see cref="DescriptionCopyAttribute"/> — so adding a field is a
/// CHOICE, not an omission — and that classification must be behaviourally TRUE of
/// <c>DataBinder.CopyEntryDescription</c>, the ONE copy both carriers and the subordinate clone now funnel
/// through. <see cref="TheAudit_ActuallyFails_WhenAClassifiedClauseIsNotCopied"/> drives the same audit over a
/// deliberately lossy copy, because a green gate that never looked at anything is indistinguishable from one that
/// works (<c>feedback_green_gates_arent_evidence</c>).</para>
/// </summary>
public sealed class DescriptionCopyCompletenessDriftTests
{
    /// <summary>A property that HOLDS state — it has a setter (public, internal or <c>init</c>), or it is a
    /// get-only MUTABLE COLLECTION (<c>Children</c>, <c>Own88s</c>, <c>IndexNames</c>, …). Everything else on
    /// <see cref="DataItem"/> is a pure function of these (<c>AsIfPic</c>, <c>IsGroup</c>, <c>ImageWidth</c>, …)
    /// and cannot be copied at all. Deriving the set this way rather than listing it is the point: a field added
    /// tomorrow is in it automatically.</summary>
    private static bool IsStored(PropertyInfo p) =>
        p.SetMethod is not null
        || (typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string));

    private static IReadOnlyList<PropertyInfo> StoredProperties() =>
        [.. typeof(DataItem).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(IsStored)
            .OrderBy(p => p.Name, StringComparer.Ordinal)];

    private static DescriptionCopyAttribute? Classification(PropertyInfo p) =>
        p.GetCustomAttribute<DescriptionCopyAttribute>();

    // ── 1. Completeness: the classification exists at all ────────────────────────────────────────────────────

    /// <summary>Every stored field says what it is to the copy, and why. An unclassified field is exactly the
    /// state PB522 shipped in — present in the model, absent from both copiers, and nowhere visible.</summary>
    [Fact]
    public void EveryStoredDataItemProperty_DeclaresWhatItIsToTheDescriptionCopy()
    {
        var unclassified = StoredProperties().Where(p => Classification(p) is null).Select(p => p.Name).ToList();
        Assert.True(unclassified.Count == 0,
            "DataItem properties with no [DescriptionCopy(...)] classification: " + string.Join(", ", unclassified)
            + ".\nISO §13.18.57.4 GR1 / §13.18.49.4 GR1 state an EXCLUSION list, so a new field is CARRIED by the "
            + "description copy unless a rule leaves it out. Classify it where it is declared — "
            + "DescriptionCopyKind.Clause (it travels), .Alignment (it travels except onto a TYPE subject), "
            + ".MemberOnly (a reproduced subordinate only), .CopyWritten (the copy derives it) or .None (with the "
            + "§/GR that excludes it, or the pass that owns it) — and DataBinder.CopyEntryDescription must agree.");

        var reasonless = StoredProperties()
            .Where(p => Classification(p) is { } a && string.IsNullOrWhiteSpace(a.Reason)).Select(p => p.Name).ToList();
        Assert.True(reasonless.Count == 0,
            "DataItem properties classified with an EMPTY reason: " + string.Join(", ", reasonless)
            + ". The reason is the clause number or owning pass that justifies the class; without it the "
            + "classification is unverifiable (feedback_a_dead_lookup_is_also_unverified).");
    }

    /// <summary>The PB522 anchor, named so a future edit that drops GROUP-USAGE from the copy fails by NAME rather
    /// than as one row of a table. ISO §13.18.29 is in neither GR-1 exclusion list.</summary>
    [Fact]
    public void GroupUsage_IsClassifiedAsAClauseThatTravels()
    {
        var a = Classification(typeof(DataItem).GetProperty(nameof(DataItem.GroupUsage))!);
        Assert.NotNull(a);
        Assert.Equal(DescriptionCopyKind.Clause, a!.Kind);
    }

    // ── 2. The copy honours the classification ───────────────────────────────────────────────────────────────

    /// <summary>Drive <paramref name="copy"/> over a source carrying a DISTINCT value in every stored field and a
    /// wholly default receiver, and report every property whose outcome contradicts its classification. Returns
    /// one line per disagreement; empty means the copy and the classification say the same thing.</summary>
    private static IReadOnlyList<string> Audit(Action<DataItem, DataItem, DescriptionCopyScope> copy)
    {
        var findings = new List<string>();
        foreach (var scope in Enum.GetValues<DescriptionCopyScope>())
        {
            var reference = NewItem();
            var from = NewItem();
            var to = NewItem();
            foreach (var p in StoredProperties())
                MakeDistinct(p, from, reference);

            // ⛔ The audit is only evidence about the fields it actually varied
            // (feedback_measure_the_selectors_complement). A property whose "distinct" value happened to equal a
            // fresh item's would pass every assertion below without measuring anything, so say so loudly instead.
            var unvaried = StoredProperties()
                .Where(p => SameValue(p.GetValue(from), p.GetValue(reference))).Select(p => p.Name).ToList();
            Assert.True(unvaried.Count == 0,
                "the audit could not give these DataItem properties a value distinct from a fresh item's, so it "
                + "measures NOTHING about them: " + string.Join(", ", unvaried)
                + ". Teach DescriptionCopyCompletenessDriftTests.Distinct their type.");

            copy(from, to, scope);

            foreach (var p in StoredProperties())
            {
                var kind = Classification(p)?.Kind ?? DescriptionCopyKind.None;
                if (kind is DescriptionCopyKind.CopyWritten) continue;   // derived provenance — no verbatim expectation

                bool shouldTravel = kind is DescriptionCopyKind.Clause
                    || (kind is DescriptionCopyKind.Alignment && scope is not DescriptionCopyScope.TypeSubject)
                    || (kind is DescriptionCopyKind.EntryOnly && scope is not DescriptionCopyScope.CompilerTemp);
                object? got = p.GetValue(to), want = p.GetValue(shouldTravel ? from : reference);
                if (SameValue(got, want)) continue;

                findings.Add($"{p.Name} [{kind}, scope:{scope}] — expected "
                    + (shouldTravel ? "the SOURCE's value" : "the receiver's own (default) value")
                    + $"; got {Render(got)} where {Render(want)} was required. Reason on the field: "
                    + (Classification(p)?.Reason ?? "(none)"));
            }
        }
        return findings;
    }

    /// <summary>⛔ The whole contract, measured: <c>DataBinder.CopyEntryDescription</c> carries EXACTLY the fields
    /// classified <see cref="DescriptionCopyKind.Clause"/> (both arms) and <see cref="DescriptionCopyKind.Alignment"/>
    /// (the SAME AS / subordinate arm only — ISO §13.18.57.4 GR1 alone excludes "alignment"), and leaves every
    /// <see cref="DescriptionCopyKind.None"/> / <see cref="DescriptionCopyKind.MemberOnly"/> field at the receiver's
    /// own value.</summary>
    [Fact]
    public void TheDescriptionCopy_CarriesExactlyTheClassifiedClauses()
    {
        var findings = Audit(RealCopy);
        Assert.True(findings.Count == 0,
            "DataBinder.CopyEntryDescription disagrees with the [DescriptionCopy] classification on DataItem:\n  "
            + string.Join("\n  ", findings)
            + "\nEither the copy is missing a clause ISO §13.18.57.4 GR1 / §13.18.49.4 GR1 do not exclude (the "
            + "PB522 shape), or the classification names the wrong class. Fix the ONE copy, never the callers.");
    }

    /// <summary>⛔ Prove the audit's failure branch (feedback_green_gates_arent_evidence). The same audit over a
    /// copy that drops GROUP-USAGE — the exact PB522 defect — must name it.</summary>
    [Fact]
    public void TheAudit_ActuallyFails_WhenAClassifiedClauseIsNotCopied()
    {
        var findings = Audit((from, to, scope) =>
        {
            RealCopy(from, to, scope);
            to.GroupUsage = GroupUsage.None;   // re-introduce PB522: the clause that makes it a bit / national group
        });
        Assert.Contains(findings, f => f.StartsWith(nameof(DataItem.GroupUsage), StringComparison.Ordinal));
    }

    // ── 3. One copy, not two ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>The names of every description CLAUSE field — everything the ONE copy carries in some scope.</summary>
    private static HashSet<string> ClauseFieldNames() =>
        StoredProperties()
            .Where(p => Classification(p)?.Kind is DescriptionCopyKind.Clause or DescriptionCopyKind.Alignment
                or DescriptionCopyKind.EntryOnly)
            .Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

    /// <summary>⛔ No COPIER spells a data description clause of its own. The defect was not that one list was
    /// wrong — it was that there were several, so a clause could be present in one and absent from another with
    /// nothing to contradict it: <c>CloneItem</c> lost GROUP-USAGE, SYNCHRONIZED and ALIGNED (kb/Work PB522), and
    /// the compiler-temp pair <c>CreateCompilerTemp</c> / <c>CloneTempNode</c> — the third and fourth hand lists —
    /// lost them again (kb/Work PB888: ISO §8.4.3.2.4 GR1 gives a function result's temporary "the description,
    /// class, and category" of the RETURNING item, and a GROUP-USAGE NATIONAL result measured 8 national-less
    /// positions where the working-storage twin measured 4). Each copier's own initializer may carry identity, the
    /// level and the <see cref="DescriptionCopyKind.MemberOnly"/> fields; every actual clause goes through
    /// <c>CopyEntryDescription</c>. The anchor is a member each method is known to assign, so a signature change
    /// or a brace-matching slip that silently extracted nothing cannot read as a clean pass
    /// (feedback_measure_the_selectors_complement).</summary>
    [Theory]
    [InlineData("DataBinder.cs", "private DataItem CloneItem(", nameof(DataItem.RedefinesTargetName))]
    [InlineData("DataBinder.Oo.cs", "internal DataItem CreateCompilerTemp(", nameof(DataItem.IsCompilerTemp))]
    [InlineData("DataBinder.Oo.cs", "private DataItem CloneTempNode(", nameof(DataItem.OccursSpec))]
    public void TheCopiers_SpellNoDescriptionClauseOfTheirOwn(string file, string signature, string anchor)
    {
        var clauses = ClauseFieldNames();
        string body = MethodBody(File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", file)), signature);
        var assign = new Regex(@"^\s*(?:[a-z][A-Za-z0-9_]*\.)?(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:\?\?|\|)?=(?!=)",
            RegexOptions.Multiline);

        var assigned = assign.Matches(body)
            .Where(m => !IsCommentLine(LineOf(body, m.Index)))
            .Select(m => m.Groups["name"].Value).Distinct().ToList();
        Assert.Contains(anchor, assigned);

        var respelled = assigned.Where(clauses.Contains).ToList();
        Assert.True(respelled.Count == 0,
            $"DataBinder.{signature.Split(' ')[^1].TrimEnd('(')} assigns description CLAUSES directly: "
            + string.Join(", ", respelled)
            + ".\nA second copy of the rule is how GROUP-USAGE, SYNCHRONIZED and ALIGNED came to be present in one "
            + "copier and absent from another (kb/Work PB522, PB888). Route them through CopyEntryDescription with "
            + "the DescriptionCopyScope that names this copy — the initializer carries only identity and the "
            + "MemberOnly fields.");
    }

    /// <summary>⛔ THE NEXT COPIER IS CAUGHT TOO, not only the three named above. A hand copy has one textual
    /// shape — <c>X = src.X</c> (or <c>??=</c> / <c>|=</c>) for a description clause field — and a site that does
    /// it for TWO OR MORE clause fields from the same source expression is a copier, whatever its name. Outside
    /// <c>DataBinder.CopyEntryDescription</c> there must be none. A single borrowed clause is a different rule and
    /// is not flagged: §13.16.4 GR1/GR2's implied GROUP-USAGE of a subordinate group, the §13.18.60.2 WITH NO SIGN
    /// inheritance, and §13.18.45 GR1's RENAMES alias taking data-name-2's PICTURE each read ONE clause.</summary>
    [Fact]
    public void NoCodeOutsideTheOneCopy_HandCopiesTwoDescriptionClauses()
    {
        var root = TestRepo.Src("Cobol.Net.Compiler");
        var files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();
        Assert.True(files.Count > 50, $"the scan found only {files.Count} source files under {root} — it measured nothing");

        var findings = new List<string>();
        foreach (var f in files)
        {
            string text = File.ReadAllText(f);
            if (f.EndsWith("DataBinder.cs", StringComparison.Ordinal))
                text = text.Replace(MethodBody(text, "private static void CopyEntryDescription("), "{}");
            findings.AddRange(HandCopies(text).Select(h => $"{Path.GetFileName(f)}: {h}"));
        }
        Assert.True(findings.Count == 0,
            "hand-written description copies outside DataBinder.CopyEntryDescription:\n  " + string.Join("\n  ", findings)
            + "\nEvery copy of a data description goes through the ONE copy with the DescriptionCopyScope that names "
            + "it (kb/Work PB522, PB888); a hand list is the defect, however complete it looks today.");
    }

    /// <summary>Prove the scan's failure branch (feedback_green_gates_arent_evidence): the exact pre-PB888
    /// <c>CreateCompilerTemp</c> initializer must be reported.</summary>
    [Fact]
    public void TheHandCopyScan_ActuallyFails_OnThePb888Initializer()
    {
        const string pre = """
            var t = new DataItem
            {
                Level = 1,
                Pic = model.Pic,
                OwnSign = model.OwnSign,
                Justified = model.Justified,
                BlankWhenZero = model.BlankWhenZero,
                IsCompilerTemp = true,
            };
            """;
        Assert.Contains(HandCopies(pre), h => h.StartsWith("model:", StringComparison.Ordinal));
    }

    /// <summary>Every source expression from which TWO OR MORE distinct description-clause fields are copied
    /// field-for-field (<c>Name = expr.Name</c>, <c>??=</c>, <c>|=</c>), comment lines excluded.</summary>
    private static IEnumerable<string> HandCopies(string text)
    {
        var clauses = ClauseFieldNames();
        var copy = new Regex(@"(?:\b(?<dst>[A-Za-z_][A-Za-z0-9_]*)\.)?\b(?<name>[A-Z][A-Za-z0-9_]*)\s*(?:\?\?|\|)?=\s*(?<src>[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\.\k<name>\b");
        // A self-assignment (`item.Pic = item.Pic with { … }`) copies nothing; a copier moves clauses from ONE
        // source onto ONE receiver, so the grouping is by that pair.
        return copy.Matches(text)
            .Where(m => clauses.Contains(m.Groups["name"].Value) && !IsCommentLine(LineOf(text, m.Index))
                     && m.Groups["dst"].Value != m.Groups["src"].Value)
            .GroupBy(m => m.Groups["src"].Value + (m.Groups["dst"].Success ? " -> " + m.Groups["dst"].Value : ""),
                StringComparer.Ordinal)
            .Select(g => (Src: g.Key, Names: g.Select(m => m.Groups["name"].Value).Distinct().ToList()))
            .Where(g => g.Names.Count >= 2)
            .Select(g => $"{g.Src}: {string.Join(", ", g.Names)}");
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly MethodInfo CopyEntryDescription =
        typeof(DataBinder).GetMethod("CopyEntryDescription", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("DataBinder.CopyEntryDescription not found — the ONE description "
            + "copy was renamed or removed; this gate and its callers must move with it.");

    private static void RealCopy(DataItem from, DataItem to, DescriptionCopyScope scope) =>
        CopyEntryDescription.Invoke(null, [from, to, scope]);

    private static DataItem NewItem() => new() { Level = 1, CsName = "SUBJECT" };

    /// <summary>Give <paramref name="target"/>'s <paramref name="p"/> a value DIFFERENT from the one a fresh item
    /// carries, so both directions are measurable: a field that should travel is seen to arrive, and a field that
    /// should not is seen to stay behind.</summary>
    private static void MakeDistinct(PropertyInfo p, DataItem target, DataItem reference)
    {
        object? current = p.GetValue(reference);
        if (p.SetMethod is null)
        {
            if (p.GetValue(target) is IList list && list.Count == 0)
                list.Add(Distinct(p.PropertyType.GetGenericArguments()[0], null));
            return;
        }
        p.SetMethod.Invoke(target, [Distinct(p.PropertyType, current)]);
    }

    /// <summary>A value of <paramref name="t"/> that is not <paramref name="current"/>. Structural rather than a
    /// per-property table: a field added tomorrow gets one without anybody editing this file.</summary>
    private static object? Distinct(Type t, object? current)
    {
        if (Nullable.GetUnderlyingType(t) is { } inner) return Distinct(inner, current);
        if (t == typeof(bool)) return current is not true;
        if (t == typeof(string)) return Equals(current, "PB522") ? "PB522-2" : "PB522";
        if (t == typeof(char)) return Equals(current, 'X') ? 'Y' : 'X';   // DataItem.FunctionValueSeparator (kb/Work PB2507)
        if (t.IsEnum)
            return Enum.GetValues(t).Cast<object>().FirstOrDefault(v => !Equals(v, current)) ?? current;
        if (t == typeof(int) || t == typeof(long) || t == typeof(short))
            return Convert.ChangeType(Convert.ToInt64(current ?? 0L) + 7, t);
        if (t == typeof(PicInfo)) return new PicInfo(PicCategory.National, Usage.National, 4, 0, 0, false);
        if (t == typeof(SignSpec)) return new SignSpec(Leading: true, Separate: true);
        if (t == typeof(DataItem)) return NewItem();
        if (t.IsGenericType && t.GetGenericTypeDefinition() is { } g
            && (g == typeof(IReadOnlyList<>) || g == typeof(List<>) || g == typeof(IList<>)))
        {
            var elem = t.GetGenericArguments()[0];
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elem))!;
            list.Add(Distinct(elem, null));
            return list;
        }
        if (t.IsValueType)
            return Activator.CreateInstance(t, [.. t.GetConstructors().OrderByDescending(c => c.GetParameters().Length)
                .First().GetParameters().Select(pp => Distinct(pp.ParameterType, null))]);
        Type concrete = t.IsAbstract
            ? t.Assembly.GetTypes().First(x => !x.IsAbstract && t.IsAssignableFrom(x))
            : t;
        return RuntimeHelpers.GetUninitializedObject(concrete);
    }

    private static bool SameValue(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a is IEnumerable ea and not string && b is IEnumerable eb and not string)
            return ea.Cast<object?>().SequenceEqual(eb.Cast<object?>());
        return a.Equals(b);
    }

    private static string Render(object? v) => v switch
    {
        null => "null",
        string s => $"\"{s}\"",
        IEnumerable e => $"[{e.Cast<object?>().Count()} item(s)]",
        _ => v.ToString() ?? "?",
    };

    /// <summary>The text of the method whose declaration starts with <paramref name="signature"/>, from its
    /// opening brace to the matching close.</summary>
    private static string MethodBody(string source, string signature)
    {
        int at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"'{signature}' not found in DataBinder.cs — the gate's subject moved.");
        int open = source.IndexOf('{', at);
        int depth = 0;
        for (int i = open; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0) return source[open..(i + 1)];
        }
        throw new InvalidOperationException($"unbalanced braces after '{signature}'");
    }

    private static string LineOf(string text, int index)
    {
        int start = text.LastIndexOf('\n', Math.Max(0, index - 1)) + 1;
        int end = text.IndexOf('\n', index);
        return end < 0 ? text[start..] : text[start..end];
    }

    private static bool IsCommentLine(string line) =>
        line.TrimStart().StartsWith("//", StringComparison.Ordinal)
        || line.TrimStart().StartsWith("///", StringComparison.Ordinal);
}
