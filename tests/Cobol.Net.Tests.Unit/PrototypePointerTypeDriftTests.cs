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
/// ⛔ THE TYPE OF A RESTRICTED PROGRAM- OR FUNCTION-POINTER IS ITS PROTOTYPE'S SIGNATURE, SO NO RELATION COMPARES THE
/// PROTOTYPE'S NAME (kb/Work PB2464).
/// <para>ISO §13.18.60.4 GR25 / GR26 type such a pointer by the SIGNATURE of the prototype it names ("shall contain
/// only … the address of a program with the same signature as that identified by the specified
/// program-prototype-name"), so §14.9.39.3 SR20 / SR22 admit a SET between pointers restricted to two differently
/// named prototypes of one signature, and §14.8.2.3.2 ("If either is a restricted pointer, both shall be restricted
/// and of the same type") is the same question at an argument. The relations that have a unit's prototype tables in
/// hand ask <c>PrototypeSignatures.Same</c>; the ones that do not — the §9.3.6 match of a UNIVERSAL INVOKE, whose two
/// descriptions travel to run time, and the typed comparator <c>OoConformance.DescriptionMismatch</c> — compared
/// <c>PicInfo.RestrictedPrototypeName</c> instead, which refused two prototypes of one signature in the first and
/// rejected legal source in the second. Both now read the pointer's resolved TYPE,
/// <c>PrototypeSignatures.RestrictionIdentity</c> (the signature class <c>DataBinder.ResolveRestrictedPrototypes</c>
/// attached), and this test keeps the NEXT relation from comparing the name again: no source line may compare
/// <c>RestrictedPrototypeName</c>. Behaviour is pinned by <c>conformance:2002/pb2464_prototype_pointer_signature_type</c>,
/// <c>conformance:2014/pb2464_function_pointer_signature_type</c> and the negative
/// <c>pb2464-typed-pointer-signature-mismatch</c>.</para>
/// <para>Proven to FAIL before it was trusted: <see cref="ThePatternThisTestKeysOn_FiresOnThePB2464Defect"/> feeds the
/// matcher the exact line the tree carried and asserts it is caught, and feeds it the repaired line and the signature
/// readers and asserts they are not.</para>
/// </summary>
public sealed class PrototypePointerTypeDriftTests
{
    /// <summary>A COMPARISON over the prototype name: a name-equality call or an operator with the property on either side.</summary>
    private static readonly Regex NameComparison = new(
        @"(?:CobolNames\.Same|string\.Equals|\.Equals|Comparer\.Equals)\s*\([^;]*RestrictedPrototypeName"
        + @"|RestrictedPrototypeName\s*(?:==|!=)|(?:==|!=)\s*[\w.?!]*RestrictedPrototypeName",
        RegexOptions.Compiled);

    [Fact]
    public void NoSourceLineComparesARestrictedPrototypeName()
    {
        var offenders = new List<string>();
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                                 && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;
                if (NameComparison.IsMatch(code))
                    offenders.Add($"{Path.GetRelativePath(root, file)}:{i + 1}  {code}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A source line compares a restricted program- or function-pointer's prototype NAME. ISO §13.18.60.4 GR25 / GR26 "
            + "type such a pointer by its prototype's SIGNATURE, so two pointers restricted to differently named prototypes of "
            + "one signature are of the same type (§14.8.2.3.2; §14.9.39.3 SR20 / SR22). Ask the pointer's TYPE — "
            + "PrototypeSignatures.RestrictionIdentity — or, with the unit's prototype tables in hand, "
            + "PrototypeSignatures.Same (kb/Work PB2464):\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void ThePatternThisTestKeysOn_FiresOnThePB2464Defect()
    {
        // The exact line OoConformance.CategoryArmMismatch carried before the repair.
        Assert.Matches(NameComparison, "if (!CobolNames.Same(f.RestrictedPrototypeName, a.RestrictedPrototypeName))");
        Assert.Matches(NameComparison, "string.Equals(receiving.Pic.RestrictedPrototypeName, sending.Pic.RestrictedPrototypeName, StringComparison.Ordinal)");
        Assert.Matches(NameComparison, "if (a.Pic.RestrictedPrototypeName == b.Pic.RestrictedPrototypeName)");
        // The repaired line, and the readers that legitimately name the property, are not caught.
        Assert.DoesNotMatch(NameComparison,
            "if (!string.Equals(PrototypeSignatures.RestrictionIdentity(formal), PrototypeSignatures.RestrictionIdentity(arg), StringComparison.Ordinal))");
        Assert.DoesNotMatch(NameComparison, "string? targetProto = receiver.Pic.RestrictedPrototypeName;");
        Assert.DoesNotMatch(NameComparison, "if (item.Pic is { RestrictedPrototypeName: { } name } pic)");
        Assert.DoesNotMatch(NameComparison, "!PrototypeSignatures.Same(host.ProgramSignatureOf(held), named.Signature)");
    }
}
