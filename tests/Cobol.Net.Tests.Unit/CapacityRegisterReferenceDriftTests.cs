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
/// <c>CapacityRegisters</c> lookup added beside the first, or a fault member added without a diagnostic, both
/// look exactly like the working compiler until the one program that hits them. The behavioural half lives in
/// the conformance corpus — <c>tests/conformance/2014/pb457_capacity_register_qualified.cob</c> and the three
/// negatives <c>pb457-capacity-register-subscripted</c> / <c>-under-table</c> / <c>-bad-qualifier</c>.</para>
/// </summary>
public sealed class CapacityRegisterReferenceDriftTests
{
    private static string Src(params string[] parts) => Path.Combine([TestRepo.Root, "src", .. parts]);

    private static readonly string ReferenceResolverPath =
        Src("Cobol.Net.Compiler", "Binding", "ReferenceResolver.cs");
    private static readonly string OdoBinderPath =
        Src("Cobol.Net.Compiler", "Binding", "DataBinder.Odo.cs");

    /// <summary>⛔ ONE lookup path. <c>DataBinder.CapacityRegisters</c> is the register name table, and only
    /// <c>ReferenceResolver.CapacityRegisterFor</c> may turn a name in it into a reference — a second reader
    /// outside <c>DataBinder</c> (the map's owner) and the codegen profile emitter (which enumerates the VALUES,
    /// not a name) is the two-arm dispatch this rule already produced once.</summary>
    [Fact]
    public void TheCapacityRegisterNameLookup_IsWrittenInExactlyOnePlace()
    {
        var sites = new List<string>();
        foreach (string file in Directory.EnumerateFiles(
                     Path.Combine(TestRepo.Root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.Combine("obj", ""), StringComparison.Ordinal)
                || file.Contains(Path.Combine("bin", ""), StringComparison.Ordinal)) continue;
            foreach (string line in File.ReadAllLines(file))
            {
                string t = line.Trim();
                if (t.StartsWith("//", StringComparison.Ordinal)) continue;   // prose about the rule is not a site
                if (Regex.IsMatch(t, @"CapacityRegisters\s*\.\s*(TryGetValue|ContainsKey)\b")
                    || Regex.IsMatch(t, @"CapacityRegisters\s*\["))
                    sites.Add($"{Path.GetFileName(file)}: {t}");
            }
        }

        // DataBinder.Odo.cs owns the map (the SR30 duplicate-name screen + the insert); ReferenceResolver.cs is
        // the ONE reader that answers a written reference with it.
        string[] expected = ["DataBinder.Odo.cs", "ReferenceResolver.cs"];
        var unexpected = sites.Where(s => !expected.Any(e => s.StartsWith(e, StringComparison.Ordinal))).ToList();
        Assert.True(unexpected.Count == 0,
            "A CAPACITY register name lookup was added outside the one resolver hook:\n  "
            + string.Join("\n  ", unexpected));
        Assert.Single(sites, s => s.StartsWith("ReferenceResolver.cs", StringComparison.Ordinal));
    }

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
        Assert.Equal(new[] { "None", "Subscripted", "RefModified", "Qualifiers", "UnderATable" }, members);

        string place = MethodBody(text, "CapacityPlaceOf");
        // None never reaches the switch — its Place is non-null by construction. Every other member is a case.
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
        // ⛔ The sources are CRLF: `[^\r\n]*$` can never match, because .NET's multiline `$` sits BEFORE the `\n`
        // with the `\r` still unconsumed. The optional `\r?` is what makes this walk work on this repo at all.
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
