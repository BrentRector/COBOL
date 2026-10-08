// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Keeps the ISO §15.3 argument-class table WIRED IN (fix-queue PB1).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ THE DEFECT THIS GUARDS AGAINST ALREADY HAPPENED ONCE, AND WAS INVISIBLE FOR THE WHOLE LIFE OF THE FEATURE.
/// <c>IntrinsicCatalog</c> declared an <c>ArgKinds</c> class code on all 79 of its rows, and
/// <c>IntrinsicSig.ArgKind(int)</c> existed to read it — with ZERO callers. The table looked complete, every row
/// looked maintained, and no §15 argument rule was enforced from it: <c>FUNCTION REVERSE</c> over a numeric item
/// and <c>FUNCTION ABS</c> over an alphanumeric one both compiled clean and produced garbage. Nothing failed,
/// because a declaration nobody reads cannot fail.
/// </para>
/// <para>
/// A dead lookup is the hardest kind of defect to notice — it presents as thorough, well-maintained data. So the
/// wiring itself is asserted here rather than assumed, which is the half of CLAUDE.md rule 5 that makes the
/// restructuring stick: "pair it with a drift test so 'automatic' stays true".
/// </para>
/// </remarks>
public sealed class IntrinsicArgumentClassDriftTests
{
    private static string CatalogSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));

    private static string BinderSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs"));

    private static string RulesSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicArgumentRules.cs"));

    /// <summary>Every <c>Add(new("NAME", …, "&lt;argkinds&gt;", …))</c> row, as (function, declared kinds).</summary>
    private static List<(string Name, string Kinds)> CatalogRows()
    {
        // ⚠ The arity bounds are NOT always literals — a variadic row writes `inf` for MaxArgs. An earlier
        // version of this pattern required digits and silently skipped every variadic function, which is how a
        // guard against a dead table becomes a dead guard. Caught by
        // EveryVerifiedRule_NamesARealFunction_WithAHandledCode reporting ORD-MAX as absent from the catalog.
        var rx = new Regex(
            "Add\\(new\\(\"(?<n>[A-Z0-9-]+)\",\\s*IntrinsicType\\.\\w+,\\s*IntrinsicArity\\.\\w+,"
            + "\\s*[-\\w]+,\\s*[-\\w]+,\\s*\"(?<k>[a-z ]*)\"",
            RegexOptions.Compiled);
        return [.. rx.Matches(CatalogSource()).Select(m => (m.Groups["n"].Value, m.Groups["k"].Value))];
    }

    [Fact]
    public void TheCatalog_StillDeclaresArgumentKinds()
    {
        var rows = CatalogRows();
        // A floor, not the exact count: adding functions must not fail this, but a parser that silently stops
        // matching (a formatting change to the rows) must.
        Assert.True(rows.Count >= 79,
            $"only {rows.Count} catalog rows parsed — the Add(new(...)) shape changed and this guard has gone "
            + "blind; fix the regex, do not lower the floor.");
    }

    /// <summary>
    /// ⛔ THE CENTRAL ASSERTION: <c>ArgKind</c> is CALLED. This is the exact fact that was false for the whole
    /// life of the catalog, and the one that cannot be inferred from the data looking well-maintained.
    /// </summary>
    [Fact]
    public void TheArgumentClassScreen_IsActuallyWiredIn()
    {
        string binder = BinderSource();
        Assert.True(binder.Contains("CheckArgumentClasses", StringComparison.Ordinal),
            "the ISO §15.3 argument-class screen is gone from IntrinsicBinder — every catalogued function's "
            + "argument rule is unenforced again (fix-queue PB1).");
        Assert.True(binder.Contains("IntrinsicArgumentRules.Verified", StringComparison.Ordinal),
            "IntrinsicBinder no longer consults IntrinsicArgumentRules.Verified. The `ArgKinds == \"p\"` "
            + "polymorphism test does NOT count as enforcement: it reads the whole string for one function "
            + "family and screens nothing — that is precisely the state PB1 found.");
    }

    /// <summary>
    /// ⛔ EVERY SCREENED FUNCTION CITES THE CLAUSE ITS RULE COMES FROM. This is the guard that keeps the table
    /// spec-derived rather than guessed, which is the distinction that cost 12 legal corpus programs to learn:
    /// the catalog's own <c>ArgKinds</c> hint column is UNAUDITED (BYTE-LENGTH declares "s" where §15.14.3 admits
    /// any class), so screening from it rejected valid COBOL.
    /// </summary>
    [Fact]
    public void EveryVerifiedRule_CitesItsClause()
    {
        string rules = RulesSource();
        int at = rules.IndexOf("Verified =", StringComparison.Ordinal);
        Assert.True(at > 0, "IntrinsicArgumentRules.Verified is gone");
        string table = rules[at..rules.IndexOf("};", at, StringComparison.Ordinal)];

        // ⚠ THE ROW SHAPE IS NOW A SCHEMA, NOT A KIND (fix-queue PB12): `Uniform('n', "§…")` for the common
        // one-kind-per-position rule and `Schema("§…", ['s','s','i'], …)` for a mixed-class one. Both carry the
        // clause in the SAME position — the first string literal — so this guard reads either without caring
        // which. A regex that still required the old tuple would have silently matched nothing and passed.
        var entries = Regex.Matches(table,
            """\["(?<f>[A-Z0-9-]+)"\]\s*=\s*(?:Uniform\('(?<k>.)',\s*"(?<c>[^"]*)"|Schema\("(?<c2>[^"]*)")""");
        Assert.True(entries.Count >= 11, $"only {entries.Count} verified rules parsed — the table shape changed "
            + "and this guard has gone blind; fix the regex, do not lower the floor.");

        var uncited = entries.Where(m => !(m.Groups["c"].Value + m.Groups["c2"].Value).Contains('§'))
            .Select(m => m.Groups["f"].Value).ToList();
        Assert.True(uncited.Count == 0,
            $"verified argument rule(s) with no ISO clause: [{string.Join(", ", uncited)}]. An entry here is a "
            + "spec-derived fact and must carry the § it was read from — an uncited one is a guess that rejects "
            + "legal source.");
    }

    /// <summary>A screened function is one the catalog actually has, and its code is one the screen handles.</summary>
    [Fact]
    public void EveryVerifiedRule_NamesARealFunction_WithAHandledCode()
    {
        string rules = RulesSource();
        int at = rules.IndexOf("Verified =", StringComparison.Ordinal);
        string table = rules[at..rules.IndexOf("};", at, StringComparison.Ordinal)];
        var catalogNames = CatalogRows().Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var handled = new HashSet<char>(Regex.Matches(RulesSource(), @"'(?<c>[a-z])' =>")
            .Select(m => m.Groups["c"].Value[0])) { 'p', ' ' };

        foreach (Match m in Regex.Matches(table,
            """\["(?<f>[A-Z0-9-]+)"\]\s*=\s*(?:Uniform\('(?<k>.)'|Schema\("[^"]*",\s*\['(?<k2>.)')"""))
        {
            string fn = m.Groups["f"].Value;
            char code = (m.Groups["k"].Value + m.Groups["k2"].Value)[0];
            Assert.True(catalogNames.Contains(fn),
                $"verified rule names FUNCTION {fn}, which is not in IntrinsicCatalog — the screen can never fire");
            Assert.True(handled.Contains(code),
                $"FUNCTION {fn} is verified with code '{code}', which Admissible does not handle — silently "
                + "unscreened, the dead-table failure in miniature");
        }
    }

    /// <summary>The three PB1 negative fixtures exist and are REGISTERED — an unregistered golden never runs.</summary>
    [Fact]
    public void ThePb1NegativeFixtures_ExistAndAreRegistered()
    {
        string manifest = File.ReadAllText(
            TestRepo.Tests("conformance", "negative", "manifest.json"));
        foreach (string name in new[]
                 {
                     "pb1-numeric-arg-alphanumeric",       // 's'-shaped rule violated by a numeric operand
                     "pb1-string-arg-numeric",             // 'n'-shaped rule violated by an alphanumeric operand
                     "pb1-numeric-arg-numeric-edited",     // the §8.5.2.1 Table-2 row that reads the other way
                 })
        {
            Assert.True(File.Exists(TestRepo.Tests("conformance", "negative", name + ".cob")), $"{name}.cob missing");
            Assert.True(File.Exists(TestRepo.Tests("conformance", "negative", name + ".err")), $"{name}.err missing");
            Assert.True(manifest.Contains(name, StringComparison.Ordinal),
                $"{name} is not in tests/conformance/negative/manifest.json — it would never run");
        }
    }

    /// <summary>
    /// The diagnostic cites the clauses the screen actually implements, and says CLASS — because the whole
    /// defect turns on §8.5.2.1 Table 2 being a CLASS table, not a category one.
    /// </summary>
    [Fact]
    public void TheDiagnostic_CitesTheGoverningClauses()
    {
        string catalog = File.ReadAllText(
            TestRepo.Src("Cobol.Net.Editions", "Diagnostics", "DiagnosticCatalog.cs"));
        int at = catalog.IndexOf("COBOLNET1627", StringComparison.Ordinal);
        Assert.True(at > 0, "COBOLNET1627 (intrinsic-argument-class) is not in the catalog");
        string block = catalog[at..Math.Min(catalog.Length, at + 2400)];
        foreach (string cite in new[] { "§15.3", "§8.5.2.1", "§4.2.2" })
            Assert.True(block.Contains(cite, StringComparison.Ordinal), $"COBOLNET1627 no longer cites {cite}");
    }

    /// <summary>
    /// EVERY bespoke binder that <c>return</c>s before the generic path's screen calls the screen ITSELF.
    /// </summary>
    /// <remarks>
    /// ⛔ THE COMMENT ON <c>CheckArgumentClasses</c> CLAIMED THIS PROPERTY AND IT WAS FALSE (fix-queue PB12).
    /// It read: "It sits HERE — after arity, before every per-function arm — so a new catalog row is screened
    /// the day it is added rather than the day someone remembers to write its arm." Eight functions
    /// (TRIM, FIND-STRING, SUBSTITUTE, CONVERT, MODULE-NAME, LENGTH, NUMVAL-C, EXCEPTION-FILE) bind through a
    /// bespoke arm that RETURNS ABOVE that line, so no <c>Verified</c> row could ever screen them — the review
    /// recorded it as "structurally unreachable" and it stayed that way through three batches.
    /// <para>
    /// ⚠ A claim about control flow is exactly the kind a comment cannot keep true. This asserts it instead: a
    /// binder that grows a new early return, or loses its screen call, fails HERE with the reason.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("BindTrim")]
    [InlineData("BindFindString")]
    [InlineData("BindSubstitute")]
    [InlineData("BindConvert")]
    [InlineData("BindNumvalCFamily")]
    [InlineData("BindLengthFamily")]
    [InlineData("BindStandardCompare")]
    public void EveryBespokeBinder_CallsTheArgumentClassScreenItself(string method)
    {
        string src = BinderSource();
        int at = src.IndexOf($"private BoundExpr {method}(", StringComparison.Ordinal);
        Assert.True(at > 0, $"{method} is gone from IntrinsicBinder — update this guard or restore the binder");
        // The method body runs to the next `private BoundExpr Bind…(` declaration (or end of file).
        int next = src.IndexOf("    private BoundExpr Bind", at + 10, StringComparison.Ordinal);
        string body = next > 0 ? src[at..next] : src[at..];
        Assert.True(body.Contains("CheckArgumentClasses(sig, operands)", StringComparison.Ordinal),
            $"{method} returns before the generic §15.3 screen and does not call it on its own operand list, so "
            + "every IntrinsicArgumentRules.Verified row for that function is DEAD — the 'structurally "
            + "unreachable' state PB12 recorded. Add CheckArgumentClasses(sig, operands) after its arity check.");
    }

    /// <summary>A schema with a cross-argument rule cites the clause that rule came from — the same
    /// spec-derived-not-guessed bar every per-position kind carries (fix-queue PB31).</summary>
    [Fact]
    public void EveryCrossArgumentRule_CitesItsClause()
    {
        string rules = RulesSource();
        int at = rules.IndexOf("Verified =", StringComparison.Ordinal);
        string table = rules[at..rules.IndexOf("};", at, StringComparison.Ordinal)];
        var withCross = Regex.Matches(table, @"CrossArgRule\.(?<r>\w+)");
        Assert.True(withCross.Count >= 4,
            $"only {withCross.Count} cross-argument rules found — §15.59.3 r2, §15.63.3 r2, §15.68.3 r2 and "
            + "§15.37.3 r2 are all modelled, so this guard has gone blind; fix the regex, do not lower the floor.");
        foreach (Match m in Regex.Matches(table, """\["(?<f>[A-Z0-9-]+)"\][^\r\n]*CrossArgRule\.(?<r>\w+)"""))
        {
            if (m.Groups["r"].Value == "None") continue;
            string row = m.Value;
            // The crossClause: argument is the LAST string literal on the row (or its continuation line).
            int rowEnd = table.IndexOf("),", m.Index, StringComparison.Ordinal);
            string full = rowEnd > 0 ? table[m.Index..rowEnd] : row;
            Assert.True(full.Contains('§'),
                $"FUNCTION {m.Groups["f"].Value} declares a cross-argument rule with no ISO clause — an "
                + "uncited cross rule rejects legal source on a guess.");
        }
    }

    /// <summary>kb/Work R27 — §8.5.2.1 Table 2's CLASS question must be answered for every USAGE, and a new
    /// usage member must not silently inherit its storage category's answer. An index data item's PicInfo
    /// carries category NUMERIC for the storage model, so before the usage-keyed arm it passed every
    /// class-numeric screen and <c>FUNCTION INTEGER(IX)</c> computed the occurrence number silently. USAGE
    /// MESSAGE-TAG is Table 2's other unexpressed class, and it LANDED as a <c>Usage</c> member with kb/Work
    /// PB487 — which is this fact doing its job: it went red the moment the member appeared, and stayed red
    /// until the disposition below was decided and recorded. Its answer is "no usage-keyed arm, and none can be
    /// needed", argued at the row.</summary>
    [Fact]
    public void EveryUsageMember_HasATable2ClassDisposition()
    {
        // "category" = the item's PicCategory row answers through ClassOfCategory (the storage category IS the
        // Table-2 class for these usages); anything else names the usage-keyed CobolClass arm in ClassOfPlace.
        var dispositioned = new Dictionary<CobolNet.Binding.Model.Usage, string>
        {
            [CobolNet.Binding.Model.Usage.Display] = "category",
            [CobolNet.Binding.Model.Usage.Binary] = "category",
            [CobolNet.Binding.Model.Usage.Packed] = "category",
            [CobolNet.Binding.Model.Usage.Comp5] = "category",
            [CobolNet.Binding.Model.Usage.Float] = "category",
            [CobolNet.Binding.Model.Usage.Double] = "category",
            [CobolNet.Binding.Model.Usage.Index] = "usage-keyed: CobolClass.Index",
            [CobolNet.Binding.Model.Usage.ObjectReference] = "category",
            [CobolNet.Binding.Model.Usage.National] = "category",
            [CobolNet.Binding.Model.Usage.Bit] = "category",
            [CobolNet.Binding.Model.Usage.Pointer] = "category",
            [CobolNet.Binding.Model.Usage.ProgramPointer] = "category",
            [CobolNet.Binding.Model.Usage.FunctionPointer] = "category",
            // USAGE MESSAGE-TAG (kb/Work PB487) — §13.18.60.4 GR9 makes the class AND category of a message-tag
            // data item message-tag, a class this implementation does not model because it DECLINES the facility
            // (Annex A.3 item 4; docs/CONFORMANCE.md §4 item 1): ParseUsage refuses the usage BY NAME with
            // COBOLNET1943, an Error, at every edition. So no CONFORMING program can present such an item to
            // ClassOfPlace, and a usage-keyed CobolClass arm would be an arm for a class no bound item can
            // legally have. In the errored bind the storage category answers, as it does for the other refused
            // usages above (FLOAT-BINARY-128 and the FLOAT-DECIMAL pair) — hence "category". ⛔ This row is the
            // DECLINE recorded, not a claim that message-tag IS its storage category: if the facility is ever
            // implemented, GR9 requires a class of its own and this row must change with it.
            [CobolNet.Binding.Model.Usage.MessageTag] = "category",
            [CobolNet.Binding.Model.Usage.FloatShort] = "category",
            [CobolNet.Binding.Model.Usage.FloatLong] = "category",
            [CobolNet.Binding.Model.Usage.FloatExtended] = "category",
            [CobolNet.Binding.Model.Usage.FloatBinary32] = "category",
            [CobolNet.Binding.Model.Usage.FloatBinary64] = "category",
            [CobolNet.Binding.Model.Usage.FloatBinary128] = "category",
            [CobolNet.Binding.Model.Usage.FloatDecimal16] = "category",
            [CobolNet.Binding.Model.Usage.FloatDecimal34] = "category",
            [CobolNet.Binding.Model.Usage.BinaryChar] = "category",
            [CobolNet.Binding.Model.Usage.BinaryShort] = "category",
            [CobolNet.Binding.Model.Usage.BinaryLong] = "category",
            [CobolNet.Binding.Model.Usage.BinaryDouble] = "category",
        };
        foreach (var u in Enum.GetValues<CobolNet.Binding.Model.Usage>())
            Assert.True(dispositioned.ContainsKey(u),
                $"Usage.{u} has no §8.5.2.1 Table-2 class disposition (kb/Work R27) — decide its Table-2 "
                + "class, wire the usage-keyed arm in ClassOfPlace if the storage category must not answer, "
                + "then record the decision here.");
        // The usage-keyed arm itself exists (the source-form half, same style as the rest of this suite).
        Assert.Matches(new Regex(@"Usage:\s*Usage\.Index\s*\}\s*\)\s*return\s+CobolClass\.Index"), RulesSource());
    }

    /// <summary>
    /// ⛔ EVERY <c>ArgKinds</c> CODE THE CATALOG USES HAS A DISPOSITION — an <c>Admissible</c> arm (an OPERAND
    /// kind, screened) or a row in <c>IntrinsicArgumentRules.NonOperandArgumentKinds</c> (a §15.3 NAME/keyword
    /// type, resolved by the function's own binder). Without this, a new code is a declaration nothing reads and
    /// nothing can contradict — the PB1 dead-column defect in its general form, and the exact trap the
    /// ordering-name code <c>'o'</c> (§15.3 argument type 12, kb/Work PB101) would have walked into.
    /// </summary>
    /// <remarks>
    /// ⚠ It is the CATALOG's vocabulary that is checked, not <c>Verified</c>'s: the sibling fact above already
    /// holds every screened SCHEMA to a handled code, and that is precisely the half that cannot see a code
    /// introduced on a catalog row whose function binds through a bespoke arm. §15.3 has fourteen argument
    /// types and four of them (Keyword, Locale-name, Ordering-name, Type declaration) are not operands at all,
    /// so more such codes are expected — the locale increments add a locale-name code across five functions —
    /// and each one costs a row rather than a rediscovery.
    /// </remarks>
    [Fact]
    public void EveryArgumentKindCodeInTheCatalog_HasADisposition()
    {
        // The same `'x' =>` arm scan the sibling fact uses, so the two cannot disagree about what is handled.
        var screened = new HashSet<char>(Regex.Matches(RulesSource(), @"'(?<c>[a-z])' =>")
            .Select(m => m.Groups["c"].Value[0])) { 'p', ' ' };
        Assert.True(screened.Count >= 6,
            $"only {screened.Count} Admissible arm(s) parsed — the switch shape changed and this guard has gone "
            + "blind; fix the regex, do not lower the floor.");

        var rows = CatalogRows();
        Assert.True(rows.Count >= 79, $"only {rows.Count} catalog rows parsed — this guard has gone blind");

        var undispositioned = rows
            .SelectMany(r => r.Kinds.Select(k => (r.Name, Code: k)))
            .Where(x => !screened.Contains(x.Code)
                        && !IntrinsicArgumentRules.NonOperandArgumentKinds.ContainsKey(x.Code))
            .Select(x => $"FUNCTION {x.Name} declares ArgKinds code '{x.Code}'")
            .Distinct()
            .Order()
            .ToList();
        Assert.True(undispositioned.Count == 0,
            $"ArgKinds code(s) with neither an Admissible arm nor a NonOperandArgumentKinds row: "
            + $"[{string.Join("; ", undispositioned)}]. A code nothing reads is a dead column — give it an "
            + "Admissible arm if it is an operand class, or a NonOperandArgumentKinds row naming the §15.3 "
            + "argument type and the binder that resolves it.");

        // A non-operand kind's disposition NAMES its resolver, and that resolver exists. A row saying "the
        // binder owns it" while no such binder exists is the same dead lookup one level up.
        string binder = BinderSource();
        foreach (var (code, why) in IntrinsicArgumentRules.NonOperandArgumentKinds)
        {
            Assert.Contains("§15.3", why, StringComparison.Ordinal);
            var named = Regex.Matches(why, @"IntrinsicBinder\.(?<m>\w+)").Select(m => m.Groups["m"].Value).ToList();
            Assert.True(named.Count > 0,
                $"NonOperandArgumentKinds['{code}'] does not name the IntrinsicBinder method that resolves it");
            foreach (string m in named)
                Assert.True(binder.Contains($" {m}(", StringComparison.Ordinal),
                    $"NonOperandArgumentKinds['{code}'] names IntrinsicBinder.{m}, which does not exist");
        }
    }

    /// <summary>kb/Work PB58 — the "absent row" class of gap dies structurally: EVERY catalogued function has a
    /// row in <c>Verified</c> (a screened schema) or in <c>DeliberatelyUnscreened</c> (a read rule with the reason
    /// it is not a class screen). Before this, six functions (the date family, NUMVAL, NUMVAL-F) had neither, and
    /// <c>CheckArgumentClasses</c> returned at its <c>TryGetValue</c> guard for them — no rule enforced, and
    /// nothing to say so.</summary>
    [Fact]
    public void EveryCataloguedFunction_HasARow()
    {
        var catalog = CatalogRows().Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = catalog
            .Where(n => !IntrinsicArgumentRules.Verified.ContainsKey(n) && !IntrinsicArgumentRules.DeliberatelyUnscreened.ContainsKey(n))
            .Order()
            .ToList();
        Assert.True(missing.Count == 0,
            $"catalogued function(s) with NO argument-rule row and NO DeliberatelyUnscreened reason: [{string.Join(", ", missing)}] — "
            + "read the function's §15.x.3 argument rule, cite it, and add its row (or its reason).");
    }

    /// <summary>kb/Work PB2079 — a function with a <c>Verified</c> schema is NOT also listed as deliberately
    /// unscreened. BASECONVERT and CONCAT were in both: the second table kept saying "an axis this ordinal table
    /// cannot express" about positions the first one screened, so argument-2/-3 of BASECONVERT lost their class
    /// screen to a sentence that was only true of argument-1. A position a class cannot carry is DECLARED with
    /// <c>IntrinsicArgumentRules.UnscreenedKind</c> inside the schema instead.</summary>
    [Fact]
    public void NoFunction_IsBothVerified_AndDeliberatelyUnscreened()
    {
        var both = IntrinsicArgumentRules.Verified.Keys
            .Where(IntrinsicArgumentRules.DeliberatelyUnscreened.ContainsKey)
            .Order()
            .ToList();
        Assert.True(both.Count == 0,
            $"function(s) in BOTH Verified and DeliberatelyUnscreened: [{string.Join(", ", both)}] — a function with "
            + "any class-ruled position belongs in Verified (declare its non-class positions with "
            + "IntrinsicArgumentRules.UnscreenedKind); DeliberatelyUnscreened is for a function with NO class rule.");
    }

    /// <summary>kb/Work PB2079 — every declared position kind in <c>Verified</c> is a kind with a disposition:
    /// an <c>Admissible</c> class set, the negative-list kind <c>'p'</c>, the no-argument marker <c>' '</c>, or
    /// <c>UnscreenedKind</c>. A typo'd kind would otherwise screen NOTHING and look declared.</summary>
    [Fact]
    public void EveryVerifiedPositionKind_HasADisposition_AndUnscreenedOnesAreOnlyBaseConvertArgument1()
    {
        var schemas = IntrinsicArgumentRules.Verified;
        var undispositioned = schemas
            .SelectMany(kv => kv.Value.Positions.Select((r, i) => (Fn: kv.Key, Ordinal: i + 1, r.Kind))
                .Concat(kv.Value.Tail is { } t ? [(kv.Key, kv.Value.Positions.Length + 1, t.Kind)] : []))
            .Where(x => x.Kind is not ('p' or ' ' or IntrinsicArgumentRules.UnscreenedKind)
                        && IntrinsicArgumentRules.Admissible(x.Kind) is null)
            .Select(x => $"FUNCTION {x.Fn} argument-{x.Ordinal} kind '{x.Kind}'")
            .Order()
            .ToList();
        Assert.True(undispositioned.Count == 0,
            $"Verified position(s) whose kind screens nothing: [{string.Join("; ", undispositioned)}]");

        // An UnscreenedKind position is owned by a bespoke binder arm; the one that exists is BASECONVERT's
        // argument-1 (usage, CheckBaseConvertArgs). A second one must name its owner here, not appear silently.
        var unscreened = schemas
            .SelectMany(kv => kv.Value.Positions.Select((r, i) => (Fn: kv.Key, Ordinal: i + 1, r.Kind)))
            .Where(x => x.Kind == IntrinsicArgumentRules.UnscreenedKind)
            .Select(x => $"{x.Fn}:{x.Ordinal}")
            .Order()
            .ToList();
        Assert.Equal(["BASECONVERT:1"], unscreened);
        Assert.Contains("CheckBaseConvertArgs(args)", BinderSource(), StringComparison.Ordinal);
    }

    /// <summary>A bare reference to an elementary item of <paramref name="usage"/> — the operand shape the
    /// §15.3 type-6 screen sees for <c>FUNCTION CHAR(WS-F)</c>.</summary>
    private static BoundFieldOperand ItemOperand(Usage usage, PicInfo pic) =>
        new(new MemberPlace(
            new AccessPath([new RootFieldSegment("WS_" + usage)]),
            new DataItem { Level = 1, CobolName = "WS-" + usage, CsName = "WS_" + usage, Pic = pic }));

    /// <summary>Every <see cref="Usage"/> whose synthesized profile is a FLOATING-POINT item (§14.6.8.3 plus the
    /// COMP-1/COMP-2 synonyms), derived from <see cref="PicInfo.IsFloat"/> rather than listed — a new float
    /// usage joins the sweep below the moment it lands.</summary>
    public static TheoryData<Usage> FloatUsages()
    {
        var d = new TheoryData<Usage>();
        foreach (var u in Enum.GetValues<Usage>())
            if (PicInfo.FloatItem(u).IsFloat) d.Add(u);
        return d;
    }

    /// <summary>
    /// ⛔ EVERY 'i' ARGUMENT POSITION REJECTS A FLOATING-POINT ITEM, at every floating-point usage (kb/Work
    /// PB248). This is the drift test the fix is paired with, and it is written over the SCHEMA TABLE rather
    /// than over a list of functions, so a new <c>Uniform('i', …)</c> or <c>Schema(… ['i','i','i'])</c> row is
    /// swept the day it is added and a new float <see cref="Usage"/> member the day it is modelled.
    /// </summary>
    /// <remarks>
    /// ⚠ THE DEFECT IT GUARDS WAS INVISIBLE BY CONSTRUCTION, which is why the assertion is over the whole
    /// cross-product and not over one witness. ISO §5.5 2)b)2. defines an integer operand written as an
    /// identifier as "a <b>fixed-point</b> numeric data item … whose description does not include any digit
    /// positions to the right of the radix point" — TWO conditions. A floating-point item is PICTURE-less, so
    /// <see cref="PicInfo.FloatItem"/> gives it <c>Scale: 0</c> and the SECOND condition is vacuously
    /// satisfied; the screen tested only that one, so <c>FUNCTION TEST-DATE-YYYYMMDD</c> over a COMP-2 holding
    /// 20240229.9 compiled clean and answered "valid date". A test that named one function and one usage would
    /// have passed while the other twenty positions and eight usages stayed open.
    /// </remarks>
    [Theory]
    [MemberData(nameof(FloatUsages))]
    public void EveryIntegerArgumentPosition_RejectsAFloatingPointItem(Usage usage)
    {
        var op = ItemOperand(usage, PicInfo.FloatItem(usage));

        // The sibling classifier §15.2 type 5 asks the SAME question of the SAME operand and must agree —
        // it is what enforces every statement rule that says "shall be an integer" (§14.9.28.3 SR2).
        Assert.False(IntrinsicResultType.IsIntegerOperand(op),
            $"IntrinsicResultType.IsIntegerOperand answers TRUE for a USAGE {usage} item — §5.5 2)b)2. requires "
            + "a FIXED-POINT numeric data item, and a floating-point item is PICTURE-less (Scale 0), so a "
            + "scale-only test admits it. PERFORM … TIMES over it iterates the truncated value silently.");

        var positions = IntrinsicArgumentRules.Verified
            .SelectMany(kv => kv.Value.Positions
                .Select((r, i) => (Fn: kv.Key, Ordinal: i + 1, Rule: r))
                .Concat(kv.Value.Tail is { } t ? [(kv.Key, kv.Value.Positions.Length + 1, t)] : []))
            .Where(x => x.Rule.Kind == 'i')
            .ToList();
        Assert.True(positions.Count >= 8,
            $"only {positions.Count} 'i' argument position(s) found in the Verified schema table — the table "
            + "shape changed and this guard has gone blind; fix the walk, do not lower the floor.");

        var admitted = positions
            .Where(x => IntrinsicArgumentRules.Violation(x.Rule, op) is null)
            .Select(x => $"FUNCTION {x.Fn} argument-{x.Ordinal}")
            .Order()
            .ToList();
        Assert.True(admitted.Count == 0,
            $"USAGE {usage} (a floating-point item, ISO §14.6.8.3) is ADMITTED at 'i' (§15.3 type 6) "
            + $"position(s): [{string.Join("; ", admitted)}]. §15.3 type 6 requires an integer data item or an "
            + "always-integral arithmetic expression, and §5.5 2)b)2. makes an integer data item a FIXED-POINT "
            + "one. Fix PicInfo.IsIntegerDescription — the ONE primitive both integer screens read — never the "
            + "individual arm.");
    }

    /// <summary>
    /// The ADMITTED half, so the arm above cannot be "fixed" into a rejecter of legal source (the PB1 failure
    /// mode from the opposite direction): a fixed-point integer item and a scale-0 P-scaled item stay
    /// admissible at every 'i' position, and <c>IsIntegerOperand</c> still answers true for them.
    /// </summary>
    [Fact]
    public void EveryIntegerArgumentPosition_StillAdmitsAFixedPointIntegerItem()
    {
        // PIC 9(4) — category numeric, scale 0, usage display: §5.5 2)b)2.'s integer data item exactly.
        var pic = new PicInfo(PicCategory.Numeric, Usage.Display, Length: 4, Digits: 4, Scale: 0, Signed: false);
        var op = ItemOperand(Usage.Display, pic);
        Assert.True(IntrinsicResultType.IsIntegerOperand(op));

        var rejected = IntrinsicArgumentRules.Verified
            .SelectMany(kv => kv.Value.Positions
                .Select((r, i) => (Fn: kv.Key, Ordinal: i + 1, Rule: r))
                .Concat(kv.Value.Tail is { } t ? [(kv.Key, kv.Value.Positions.Length + 1, t)] : []))
            .Where(x => x.Rule.Kind == 'i' && IntrinsicArgumentRules.Violation(x.Rule, op) is { } why)
            .Select(x => $"FUNCTION {x.Fn} argument-{x.Ordinal}: {IntrinsicArgumentRules.Violation(x.Rule, op)}")
            .Order()
            .ToList();
        Assert.True(rejected.Count == 0,
            $"a PIC 9(4) integer item is REJECTED at 'i' position(s): [{string.Join("; ", rejected)}] — §15.3 "
            + "type 6 admits an integer data item, and §5.5 2)b)2. says this is one.");
    }

    /// <summary>One data-item witness per <see cref="CobolClass"/> member a PICTURE can present, with the
    /// member each is BUILT to present — the bank the two cross-argument facts below are written over, so a new
    /// lattice member joins them the day it is modelled rather than the day someone remembers it.</summary>
    /// <remarks>⚠ Each witness is checked against <c>ClassOf</c> before it is used: a bank that silently stopped
    /// presenting the class it names would turn both facts green for the wrong reason
    /// (<c>feedback_green_gates_arent_evidence</c>).</remarks>
    private static List<(CobolClass Class, BoundFieldOperand Op)> ClassWitnesses()
    {
        (CobolClass Cls, PicInfo Pic)[] bank =
        [
            (CobolClass.Alphanumeric, new PicInfo(PicCategory.Alphanumeric, Usage.Display, 4, 0, 0, false)),
            (CobolClass.Alphabetic,
                new PicInfo(PicCategory.Alphanumeric, Usage.Display, 4, 0, 0, false) { IsAlphabetic = true }),
            // PIC ZZ9 — category numeric-edited, usage display: §8.5.2.1 Table 2 class ALPHANUMERIC, and the
            // refined member the lattice reports for it. THIS is the witness kb/Work PB305 turns on.
            (CobolClass.NumericEditedDeEditing, new PicInfo(PicCategory.NumericEdited, Usage.Display, 3, 3, 0, false)),
            (CobolClass.National, new PicInfo(PicCategory.National, Usage.National, 4, 0, 0, false)),
            // PIC ZZ9 USAGE NATIONAL — Table 2 splits the numeric-edited row on usage: class NATIONAL (kb/Work PB1128).
            (CobolClass.NumericEditedNational, new PicInfo(PicCategory.NumericEdited, Usage.National, 3, 3, 0, false)),
            (CobolClass.Numeric, new PicInfo(PicCategory.Numeric, Usage.Display, 4, 4, 0, false)),
            (CobolClass.Boolean, new PicInfo(PicCategory.Boolean, Usage.Bit, 8, 0, 0, false)),
            (CobolClass.Object, new PicInfo(PicCategory.ObjectReference, Usage.ObjectReference, 8, 0, 0, false)),
            (CobolClass.Pointer, new PicInfo(PicCategory.Pointer, Usage.Pointer, 8, 0, 0, false)),
            (CobolClass.Index, new PicInfo(PicCategory.Numeric, Usage.Index, 8, 9, 0, false)),
        ];
        var built = new List<(CobolClass, BoundFieldOperand)>();
        foreach (var (cls, pic) in bank)
        {
            var op = ItemOperand(pic.Usage, pic);
            Assert.True(IntrinsicArgumentRules.ClassOf(op) == cls,
                $"the {cls} witness classifies as {IntrinsicArgumentRules.ClassOf(op)?.ToString() ?? "<undecidable>"} "
                + "— the bank has rotted and every fact written over it is green for the wrong reason; fix the "
                + "witness, never the assertion.");
            built.Add((cls, op));
        }
        // Every member of the lattice is represented, so "swept the whole enum" is a fact and not a hope.
        foreach (var m in Enum.GetValues<CobolClass>())
            Assert.Contains(m, built.Select(b => b.Item1));
        return built;
    }

    /// <summary>The rule governing argument position <paramref name="i"/> of a schema (its own row, or the
    /// variadic tail).</summary>
    private static ArgRule? RuleAt(ArgSchema s, int i) =>
        i < s.Positions.Length ? s.Positions[i] : s.Tail;

    /// <summary>
    /// ⛔ EVERY CROSS-ARGUMENT RULE ACCEPTS TWO OPERANDS OF ONE §8.5.2.1 TABLE-2 CLASS (kb/Work PB305). This is
    /// the drift test the fix is paired with, and it is written over the SCHEMA TABLE and the whole
    /// <see cref="CobolClass"/> lattice rather than over a list of functions, so a new cross rule is swept the
    /// day its row is added and a new refined class member the day it is modelled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⛔ EVERY cross clause in the catalogue is CLASS-worded, which is what makes this assertion total rather
    /// than per-function: §15.37.3 r2 makes argument-2 "a data item or literal of either class alphabetic or
    /// alphanumeric", §15.96.3 r2 makes it "a single character that is either class alphabetic or class
    /// alphanumeric", §15.87.3 r2 says it per argument-2/argument-3 pair, §15.59.3 r2 / §15.63.3 r2 / §15.71.3 r3 /
    /// §15.72.3 r3 say "All arguments shall be of the same class", §15.68.3 r2 says "of the same class as
    /// argument-1" — even though its own r1 is category-worded — and §15.48.3 r3 / §15.79.3 r3 / §15.92.3 r2's
    /// "the same type as argument-1" is read as the class. So a screen may MERGE two Table-2 classes (§15.59.3
    /// r2's own alphabetic/alphanumeric exception) but may never SPLIT one.
    /// </para>
    /// <para>
    /// ⚠ THE DEFECT IT GUARDS WAS SELF-REFUTING AND STILL SHIPPED. <c>CrossViolation</c> normalized candidates
    /// with a hand-written one-case fold (Alphabetic → Alphanumeric) instead of the Table-2 class projection, so
    /// <c>CobolClass.NumericEditedDeEditing</c> — whose own doc comment reads "Anywhere a rule says 'shall be of
    /// CLASS alphanumeric', this counts as alphanumeric" — stayed in a block of its own and intersected empty
    /// against an ordinary alphanumeric operand. TEN catalogued functions rejected legal source, and written as
    /// <c>FUNCTION FIND-STRING("ABC" &lt;PIC ZZ9&gt;)</c> the message said it outright: "argument-2 is of class
    /// alphanumeric (numeric-edited), which cannot agree with argument-1". A test naming one function and one
    /// class would have passed while the other nine stayed open.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryCrossArgumentRule_AcceptsTwoOperandsOfOneTable2Class()
    {
        var witnesses = ClassWitnesses();
        var crossSchemas = IntrinsicArgumentRules.Verified.Where(kv => kv.Value.Cross != CrossArgRule.None).ToList();
        Assert.True(crossSchemas.Count >= 10,
            $"only {crossSchemas.Count} schema(s) carry a cross-argument rule — the table shape changed and this "
            + "guard has gone blind; fix the walk, do not lower the floor.");

        var rejected = new List<string>();
        int exercised = 0;
        foreach (var (fn, schema) in crossSchemas.Select(kv => (kv.Key, kv.Value)))
        {
            foreach (var (ca, opA) in witnesses)
            {
                foreach (var (cb, opB) in witnesses)
                {
                    // Only pairs Table 2 puts in ONE class: a cross rule is free to reject anything else.
                    if (IntrinsicArgumentRules.TableTwoClass(ca) != IntrinsicArgumentRules.TableTwoClass(cb)) continue;
                    // …and only pairs the PER-POSITION screen already admits, so this fact measures the cross
                    // screen alone and never re-litigates §15.x.3 r1.
                    if (RuleAt(schema, 0) is not { } r0 || IntrinsicArgumentRules.Violation(r0, opA) is not null) continue;
                    if (RuleAt(schema, 1) is not { } r1 || IntrinsicArgumentRules.Violation(r1, opB) is not null) continue;
                    exercised++;
                    if (IntrinsicArgumentRules.CrossViolation(schema, [opA, opB]) is { } why)
                        rejected.Add($"FUNCTION {fn}({ca} {cb}): {why}");
                }
            }
        }
        Assert.True(exercised >= 20,
            $"only {exercised} admissible same-class pair(s) reached CrossViolation — the witness bank or the "
            + "per-position filter has gone blind; fix the walk, do not lower the floor.");
        Assert.True(rejected.Count == 0,
            "a cross-argument rule REJECTED two operands of one §8.5.2.1 Table-2 class — it is SPLITTING a class "
            + $"the standard does not split (kb/Work PB305):{Environment.NewLine}"
            + string.Join(Environment.NewLine, rejected.Order()));
    }

    /// <summary>
    /// ⛔ EVERY CLASS-WORDED ARGUMENT KIND'S ADMISSIBLE SET IS CLOSED UNDER ITS §8.5.2.1 TABLE-2 CLASS: if it
    /// admits a class, it admits every lattice member of that class. This is what keeps
    /// <c>IntrinsicArgumentRules.ByClass</c> honest — a hand-edit back to a literal member list fails here.
    /// </summary>
    /// <remarks>
    /// ⚠ THERE IS NO CATEGORY-WORDED KIND, AND NO EXEMPTION FROM THIS RULE (kb/Work PB658). The one kind that used to
    /// be exempt ('t', the NUMVAL / FORMATTED-* family) was a hand-listed CATEGORY set that admitted the edited items
    /// its own clauses ("of category alphanumeric or national", §15.68.3 r1) exclude. A category-worded position is
    /// now the derived class set plus an <c>ArgPredicateKind.CharacterCategory</c> predicate, so every kind is a
    /// class set and this closure holds for all of them; a new kind with a literal member list turns this red.
    /// </remarks>
    [Fact]
    public void EveryClassWordedArgumentKind_IsClosedUnderItsTable2Class()
    {
        var kinds = IntrinsicArgumentRules.Verified.Values
            .SelectMany(s => s.Positions.Select(p => p.Kind).Concat(s.Tail is { } t ? [t.Kind] : []))
            .Distinct()
            .Where(k => IntrinsicArgumentRules.Admissible(k) is not null)
            .Order()
            .ToList();
        Assert.True(kinds.Count >= 5,
            $"only {kinds.Count} screened kind code(s) found in the Verified schema table — the table shape "
            + "changed and this guard has gone blind; fix the walk, do not lower the floor.");

        var open = new List<string>();
        foreach (char k in kinds)
        {
            var ok = IntrinsicArgumentRules.Admissible(k)!;
            foreach (var admitted in ok)
                foreach (var m in Enum.GetValues<CobolClass>())
                    if (IntrinsicArgumentRules.TableTwoClass(m) == IntrinsicArgumentRules.TableTwoClass(admitted)
                        && !ok.Contains(m))
                        open.Add($"kind '{k}' admits {admitted} but not {m}, which §8.5.2.1 Table 2 puts in the "
                            + $"same class ({IntrinsicArgumentRules.TableTwoClass(m)})");
        }
        Assert.True(open.Count == 0,
            "a CLASS-worded argument kind admits only part of a §8.5.2.1 Table-2 class — state it with "
            + "IntrinsicArgumentRules.ByClass; a CATEGORY-worded rule is the class set plus the CharacterCategory "
            + $"predicate, never a hand-listed category set:{Environment.NewLine}"
            + string.Join(Environment.NewLine, open.Distinct().Order()));
    }

    /// <summary>
    /// ⛔ A FUNCTION'S STRING-ARGUMENT RULE IS WORDED CATEGORY OR CLASS, AND THE ROW CARRIES THE <c>CharacterCategory</c>
    /// PREDICATE EXACTLY WHEN THE CLAUSE SAYS CATEGORY (kb/Work PB658 — §8.5.2.1's closing sentence: "Use of the name of
    /// a data class or data category in the rules of COBOL refers to the category unless class is specifically
    /// indicated"). The wording table is declared HERE, beside the clause text it was read from, and the clause text
    /// is checked against the specification on every build, so a re-worded or mis-attributed row fails.
    /// </summary>
    /// <remarks>
    /// Two mechanisms used to answer §15.68.3 r1: the generic string set admitted a numeric-edited item and a bespoke
    /// IntrinsicBinder arm rejected it for NUMVAL-C alone, so which one bit was an accident of call order, and NUMVAL
    /// and NUMVAL-F (the same category wording) screened nothing finer than the class. The bespoke arm is deleted; the
    /// predicate is the one owner, and this test is what keeps the wording table and the rows in agreement.
    /// </remarks>
    [Fact]
    public void CategoryWordedStringArguments_CarryTheCategoryPredicate_AndClassWordedOnesDoNot()
    {
        // function → (clause, a sentence of its argument-1 rule as printed, wording).
        var wording = new (string Function, string Clause, string Text, bool Category)[]
        {
            ("NUMVAL", "15.67.3", "Argument-1 shall be an alphanumeric or national literal or an alphanumeric or national data item", true),
            ("NUMVAL-F", "15.69.3", "Argument-1 shall be an alphanumeric or national literal or data item", true),
            ("NUMVAL-C", "15.68.3", "Argument-1 shall be of category alphanumeric or national", true),
            ("TEST-NUMVAL-C", "15.94.3", "The argument rules for the TEST-NUMVAL-C function are the same as those specified in 15.68, NUMVAL-C function, Arguments", true),
            ("ORD", "15.70.3", "Argument-1 shall be one character position in length and shall be of category alphabetic, alphanumeric, or national", true),
            ("TEST-NUMVAL", "15.93.3", "a data item of class alphanumeric or national", false),
            ("TEST-NUMVAL-F", "15.95.3", "a data item of class alphanumeric or national", false),
            ("LOCALE-DATE", "15.52.3", "Argument-1 shall be of class alphanumeric or national and shall be 8 character positions in length", false),
            ("LOCALE-TIME", "15.53.3", "Argument-1 shall be of class alphanumeric or national and shall be 6 character positions in length", false),
        };

        string spec = File.ReadAllText(TestRepo.Specs("ISO_COBOL.md"));
        var problems = new List<string>();
        foreach (var (fn, clause, text, category) in wording)
        {
            // The clause's own text, not any occurrence of the sentence: slice from its heading to the next heading.
            var m = Regex.Match(spec, $"(?m)^#+ {Regex.Escape(clause)} .*$");
            if (!m.Success) { problems.Add($"{fn}: no heading for §{clause} in the specification"); continue; }
            int from = m.Index, to = spec.IndexOf("\n#", from + m.Length, StringComparison.Ordinal);
            string body = spec[from..(to < 0 ? spec.Length : to)];
            if (!body.Contains(text, StringComparison.Ordinal))
                problems.Add($"{fn}: §{clause} no longer prints \"{text}\"");

            var schema = IntrinsicArgumentRules.Verified[fn];
            var rule = schema.At(0) ?? throw new InvalidOperationException($"{fn} declares no argument-1 rule");
            bool hasPredicate = rule.Predicates.Any(p => p.Kind == ArgPredicateKind.CharacterCategory);
            if (hasPredicate != category)
                problems.Add($"{fn}: §{clause} is worded {(category ? "CATEGORY" : "CLASS")} but its row "
                    + (hasPredicate ? "carries" : "lacks") + " the CharacterCategory predicate");
        }
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));

        // The deleted second mechanism stays deleted: no function-specific "EDITED category" rejection in the binder.
        Assert.DoesNotContain("is of an EDITED category", BinderSource(), StringComparison.Ordinal);
    }

    /// <summary>
    /// ⛔ EVERY OPERAND POSITION THAT §15.6 TABLE 21 TYPES <c>Int</c> AND NOTHING ELSE IS KIND <c>'i'</c> IN THE VERIFIED
    /// TABLE (kb/Work PB1420). §8.4.3.2.3 SR11 ("A numeric function shall not be specified where an integer operand is
    /// required") is only as complete as the table of integer positions, and that table was HAND-MAINTAINED: RANDOM's
    /// seed (Table 21: <c>Int1</c>) and COMBINED-DATETIME's argument-1 were typed class-numeric, so a numeric function
    /// stood where an integer is required. The positions are DERIVED from the standard's own table now, so the next
    /// function typed Int that nobody remembered to mark fails here instead of compiling.
    /// </summary>
    /// <remarks>
    /// A position whose alternatives are all <c>Int…</c> is integer-typed; one that also lists <c>Num</c> (ABS, RANGE,
    /// SUM) admits numeric operands and is not asked. A <c>Key</c>, <c>Ord</c> or <c>Loc</c> item is a NAME, not an
    /// operand, and does not occupy a position (the convention the Verified rows already use); <c>Key4 and Int4</c>
    /// (FIND-STRING) is an integer operand with its keyword. BASECONVERT's argument-2 and argument-3 are
    /// <c>Int</c> positions and <c>'i'</c> in its schema (argument-1 is <c>UnscreenedKind</c>, owned by
    /// <c>CheckBaseConvertArgs</c>); only <c>DeliberatelyUnscreened</c> functions are skipped.
    /// </remarks>
    [Fact]
    public void EveryTable21IntegerPosition_IsTypedInteger_InTheVerifiedTable()
    {
        string[] lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int start = Array.FindIndex(lines, l => l.StartsWith("**Table 21", StringComparison.Ordinal));
        Assert.True(start >= 0, "Table 21 is gone from the transcription — this guard has gone blind");

        var problems = new List<string>();
        int integerPositions = 0;
        for (int i = start + 1; i < lines.Length; i++)
        {
            if (lines[i].StartsWith('#')) break;          // the next heading ends the table's section
            if (!lines[i].StartsWith('|')) continue;      // the figure notes the transcription interleaves
            string[] cells = lines[i].Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length < 2 || !Regex.IsMatch(cells[0], "^[A-Z][A-Z0-9-]+$")) continue;
            string function = cells[0];

            // Operand positions: drop the NAME items, keep the operand tokens of the rest.
            var positions = new List<string[]>();
            foreach (string item in cells[1].Split(','))
            {
                var operands = item.Split([" or ", " and "], StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0 && !Regex.IsMatch(t, "^(Key|Ord|Loc)"))
                    .ToArray();
                if (operands.Length > 0) positions.Add(operands);
            }

            for (int p = 0; p < positions.Count; p++)
            {
                if (!positions[p].All(t => t.StartsWith("Int", StringComparison.Ordinal))) continue;
                integerPositions++;
                if (IntrinsicArgumentRules.DeliberatelyUnscreened.ContainsKey(function)) continue;
                if (!IntrinsicArgumentRules.Verified.TryGetValue(function, out var schema))
                {
                    problems.Add($"{function}: Table 21 types argument position {p + 1} Int, and the function has no Verified row");
                    continue;
                }
                char? kind = schema.At(p)?.Kind;
                if (kind != 'i')
                    problems.Add($"{function}: Table 21 types operand position {p + 1} Int (§15.3 type 6), but its Verified "
                        + $"kind is '{(kind?.ToString() ?? "none")}' — SR11 cannot bar a numeric function from it");
            }
        }

        Assert.True(integerPositions >= 30,
            $"only {integerPositions} Int-only Table 21 position(s) found — the table shape changed and this guard "
            + "has gone blind; fix the walk, do not lower the floor.");
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}
