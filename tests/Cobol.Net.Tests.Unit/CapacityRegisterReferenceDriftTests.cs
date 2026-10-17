// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A NAMED OCCURS DYNAMIC CAPACITY REGISTER IS REACHED THROUGH EXACTLY ONE LOOKUP, AND THAT LOOKUP JUDGES THE
/// WRITTEN REFERENCE (kb/Work PB457). The register is not in <c>ByName</c> — it is a view over the table's
/// capacity, never storage — so whatever <c>ReferenceResolver.CapacityRegisterFor</c> refuses is not handed to a
/// slower path: it is the END of resolution, and the general resolver then said COBOLNET1639 "is not defined"
/// about a name ISO §13.18.38.3 SR30 declares. The old hook refused on two PARSE-shape conjuncts —
/// <c>DataReferenceCst.HasNoSuffix</c> (since deleted, kb/Work PB2193; <c>dataReferenceSuffix</c> made it false
/// for QUALIFICATION as well as for subscripts, so the legal <c>SET WS-CAP OF WS-TABLE TO 7</c> failed it) and the
/// zero-index
/// <c>BuildTablePath</c> (null for every table with a table ancestor).
///
/// <para><b>Why a source scan.</b> The properties these tests pin are not observable from behaviour: a second
/// <c>CapacityRegisters</c> lookup added beside the one in <c>SymbolTable.CapacityCandidates</c>, or a fault member
/// added without a diagnostic, both look exactly like the working compiler until the one program that hits them.
/// The behavioural half lives in the conformance corpus —
/// <c>conformance-suite/cases/14.9.39-SET/pb457_capacity_register_qualified.cob</c> and the three negatives
/// <c>pb457-capacity-register-subscripted</c> / <c>-under-table</c> / <c>-bad-qualifier</c>, and for the method
/// scope <c>conformance-suite/cases/_unassigned/pb3165_method_capacity_register_scope.cob</c> (kb/Work PB3165).</para>
/// <para><b>A tree-wide sweep</b> (tier 0c, at every gate): the one-lookup scan reads all of <c>src</c>, so a second
/// lookup added in ANY file breaks it, and the impact map of the file that adds one never names this class.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CapacityRegisterReferenceDriftTests
{
    private static readonly string ReferenceResolverPath =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "ReferenceResolver.cs");
    private static readonly string OdoBinderPath =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Odo.cs");
    private static readonly string SymbolTablePath =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Model", "SymbolTable.cs");

    /// <summary>⛔ ONE lookup path. The register name tables are <c>DataBinder.CapacityRegisters</c> (the unit's) and
    /// <c>OoMethodDataScope.CapacityRegisters</c> (a method's own, kb/Work PB3165). Only
    /// <c>SymbolTable.CapacityCandidates</c> — the one scope-aware name resolver, where §11.7.4 GR5's
    /// method-over-object visibility is applied — reads a name in them
    /// (<see cref="TheOneLookup_HoldsEveryRead_AndHasOneCaller"/>). A reader in any file other than
    /// <c>DataBinder.Odo.cs</c> (the maps' owner: the SR30 duplicate-name screen and the one insert) and
    /// <c>SymbolTable.cs</c> — the codegen profile emitter enumerates the VALUES, never a name — is the two-arm
    /// dispatch this rule already produced once.</summary>
    [Fact]
    public void TheCapacityRegisterNameLookup_IsWrittenInExactlyOnePlace()
    {
        // DataBinder.Odo.cs owns the maps (the SR30 duplicate-name screen + the insert); SymbolTable.cs is the ONE
        // reader that answers a name with them; the SHADOWING predicates only ask whether the word is declared.
        string[] expected = ["DataBinder.Odo.cs", "SymbolTable.cs", .. ShadowingPredicates.Select(static p => p.File)];
        var unexpected = SrcScan.Value.Sites
            .Where(s => !expected.Any(e => s.StartsWith(e + ":", StringComparison.Ordinal))).ToList();
        Assert.True(unexpected.Count == 0,
            "A CAPACITY register name lookup was added outside the one resolver hook:\n  "
            + string.Join("\n  ", unexpected));
    }

    /// <summary>The two SHADOWING predicates (kb/Work PB3154 review R2-1): a named register is a data-name
    /// (§13.18.38.3 SR30), so it hides an inherited constant-name or symbolic-character of its spelling (§8.4.6.1), and
    /// each predicate asks whether a register of the word is DECLARED at its tier — the unit's
    /// (<c>DataBinder.DeclaredNearerThan</c>) and a method's own (<c>OoMethodDataScope.Declares</c>). Neither answers a
    /// reference. Their files may read a register map only inside them.</summary>
    private static readonly (string File, string Method, string Path)[] ShadowingPredicates =
    [
        ("DataBinder.Constants.cs", "DeclaredNearerThan", TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Constants.cs")),
        ("DataBinder.Oo.cs", "Declares", TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Oo.cs")),
    ];

    /// <summary>⛔ A shadowing predicate's file reads a register map ONLY inside the predicate, so a reference lookup
    /// added beside it is still the second lookup <see cref="TheCapacityRegisterNameLookup_IsWrittenInExactlyOnePlace"/>
    /// forbids. (The scan is case-insensitive: the unit's map is the private field <c>_capacityRegisters</c>, which a
    /// case-sensitive scan never saw.)</summary>
    [Fact]
    public void AShadowingPredicate_IsItsFilesOnlyRegisterRead()
    {
        foreach (var (file, method, path) in ShadowingPredicates)
        {
            string body = MethodBody(File.ReadAllText(path), method);
            int inBody = body.Split('\n').Count(l => !l.Trim().StartsWith("//", StringComparison.Ordinal) && IsNameLookup(l.Trim()));
            int inFile = SrcScan.Value.Sites.Count(s => s.StartsWith(file + ":", StringComparison.Ordinal));
            Assert.True(inBody > 0, $"{method} in {file} no longer asks a CAPACITY register map.");
            Assert.True(inFile == inBody, $"{file} reads a CAPACITY register map {inFile - inBody} time(s) outside {method}.");
        }
    }

    /// <summary>⛔ Inside <c>SymbolTable.cs</c> every register-map read lies in <c>CapacityCandidates</c> — ONE method,
    /// however many scopes it consults — and that method has ONE caller, <c>ReferenceResolver.CapacityRegisterFor</c>,
    /// which turns the candidates into a reference. A second caller would be a second reference hook judging the
    /// name by another rule.</summary>
    [Fact]
    public void TheOneLookup_HoldsEveryRead_AndHasOneCaller()
    {
        string hook = MethodBody(File.ReadAllText(SymbolTablePath), "CapacityCandidates");
        int inFile = SrcScan.Value.Sites.Count(s => s.StartsWith("SymbolTable.cs", StringComparison.Ordinal));
        int inHook = hook.Split('\n').Count(l => IsNameLookup(l.Trim()));
        Assert.True(inHook > 0, "SymbolTable.CapacityCandidates reads no CAPACITY register map.");
        Assert.True(inFile == inHook,
            $"SymbolTable.cs reads a CAPACITY register map {inFile - inHook} time(s) outside CapacityCandidates.");

        string caller = Assert.Single(SrcScan.Value.Callers);
        Assert.StartsWith("ReferenceResolver.cs", caller, StringComparison.Ordinal);
        Assert.Contains("Symbols.CapacityCandidates(",
            MethodBody(File.ReadAllText(ReferenceResolverPath), "CapacityRegisterFor"), StringComparison.Ordinal);
    }

    /// <summary>One read of every <c>src</c> source line, shared by the two scans: each register-map read and each
    /// call of <c>CapacityCandidates</c>, as <c>file: line</c>. Comment lines are prose about the rule, not sites.</summary>
    private static readonly Lazy<(List<string> Sites, List<string> Callers)> SrcScan = new(() =>
    {
        var sites = new List<string>();
        var callers = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.Combine("obj", ""), StringComparison.Ordinal)
                || file.Contains(Path.Combine("bin", ""), StringComparison.Ordinal)) continue;
            foreach (string line in File.ReadAllLines(file))
            {
                string t = line.Trim();
                if (t.StartsWith("//", StringComparison.Ordinal)) continue;
                if (IsNameLookup(t)) sites.Add($"{Path.GetFileName(file)}: {t}");
                if (t.Contains("CapacityCandidates(", StringComparison.Ordinal)
                    && !t.Contains("List<DataItem>? CapacityCandidates(", StringComparison.Ordinal))
                    callers.Add($"{Path.GetFileName(file)}: {t}");
            }
        }
        return (sites, callers);
    });

    private static bool IsNameLookup(string line) =>
        line.Contains("CapacityRegisters", StringComparison.OrdinalIgnoreCase)
        && (Regex.IsMatch(line, @"CapacityRegisters\s*\.\s*(TryGetValue|ContainsKey)\b", RegexOptions.IgnoreCase)
            || Regex.IsMatch(line, @"CapacityRegisters\s*\[", RegexOptions.IgnoreCase));

    /// <summary>⛔ The hook judges the WRITTEN reference, never the parse shape. A read of
    /// <c>dataReferenceSuffix()</c> back in <c>CapacityRegisterFor</c> (the old façade <c>HasNoSuffix</c> test,
    /// deleted by kb/Work PB2193, was exactly that read) is the regression that deleted the qualified form: it
    /// cannot tell a subscript (§13.18.38.3 SR31, refused) from a qualification (§8.4.2.2.3 SR2, legal).</summary>
    [Fact]
    public void TheCapacityRegisterHook_ReadsTheWrittenReference_NotTheParseShape()
    {
        string body = MethodBody(File.ReadAllText(ReferenceResolverPath), "CapacityRegisterFor");
        Assert.DoesNotContain("dataReferenceSuffix(", body, StringComparison.Ordinal);
        Assert.DoesNotContain("HasNoSuffix", body, StringComparison.Ordinal);
        Assert.Contains("ReadWritten(", body, StringComparison.Ordinal);
        // The §8.4.2.2 qualifier rule is the binder's ONE matcher, not a copy (kb/Work PB489).
        Assert.Contains("QualifierChainMatches(", body, StringComparison.Ordinal);
    }

    /// <summary>⛔ Every <c>CapacityRefFault</c> has a diagnostic. A new member added to the enum without a case
    /// in <c>CapacityPlaceOf</c> would reach that method's internal-error backstop at bind time.</summary>
    [Fact]
    public void EveryCapacityRefFault_HasADiagnosticArm()
    {
        string text = File.ReadAllText(ReferenceResolverPath);
        var enumBody = Regex.Match(text, @"internal enum CapacityRefFault\s*\{(?<body>.*?)\n    \}",
            RegexOptions.Singleline);
        Assert.True(enumBody.Success, "CapacityRefFault enum not found in ReferenceResolver.cs");
        var members = Regex.Matches(enumBody.Groups["body"].Value, @"^\s{8}(?<name>[A-Z]\w*),", RegexOptions.Multiline)
            .Select(m => m.Groups["name"].Value).ToList();
        Assert.Equal(new[] { "None", "Subscripted", "RefModified", "Qualifiers", "Ambiguous", "UnderATable" }, members);

        string place = MethodBody(text, "CapacityPlaceOf");
        // None never reaches the switch (CapacityPlaceOf returns its Place first). Every other member is a case.
        foreach (string m in members.Where(m => m != "None"))
            Assert.True(place.Contains($"case CapacityRefFault.{m}:", StringComparison.Ordinal),
                $"CapacityRefFault.{m} has no diagnostic arm in CapacityPlaceOf.");
    }

    /// <summary>⛔ The register carries its §13.18.38.3 SR30 position. "If qualifiers are required for uniqueness,
    /// it shall be treated as though implicitly defined at the same level as the entry containing the OCCURS
    /// clause" is ONE assignment where the register is minted — <c>Parent = item.Parent</c> — and it is what makes
    /// both the qualifier match and the nested-table fault (SubscriptArity &gt; 0) derive themselves.</summary>
    [Fact]
    public void TheCapacityRegister_IsMintedWithItsSr30Parent()
    {
        string odo = File.ReadAllText(OdoBinderPath);
        var mint = Regex.Match(odo, @"var reg = new DataItem\s*\{(?<body>.*?)\};", RegexOptions.Singleline);
        Assert.True(mint.Success, "The CAPACITY register minting site was not found in DataBinder.Odo.cs");
        Assert.Contains("Parent = item.Parent,", mint.Groups["body"].Value, StringComparison.Ordinal);
    }

    /// <summary>The source text of a method, from its signature to the closing brace at its own indentation.</summary>
    private static string MethodBody(string text, string name)
    {
        // ⛔ `\r?`: on a CRLF source `[^\r\n]*$` can never match, because .NET's multiline `$` sits BEFORE the `\n`
        // with the `\r` still unconsumed. The sources are LF (kb/Work PB3029); the `\r?` keeps a CR from turning this
        // walk into a silent no-match, which the line-endings audit then reports by name.
        // The DECLARATION, not the first call: the accessibility modifier is what tells them apart (both hooks
        // are also CALLED from ResolveImplCore, at the same indentation, a few hundred lines above).
        var m = Regex.Match(text,
            @"^(?<indent>[ ]*)(?:internal|private|public|protected)[^\r\n]*\b" + Regex.Escape(name)
            + @"\s*\([^\r\n]*\r?$", RegexOptions.Multiline);
        Assert.True(m.Success, $"Method {name} not found.");
        string close = "\n" + m.Groups["indent"].Value + "}";
        int end = text.IndexOf(close, m.Index, StringComparison.Ordinal);
        Assert.True(end > 0, $"Closing brace of {name} not found.");
        return text[m.Index..(end + close.Length)];
    }
}
