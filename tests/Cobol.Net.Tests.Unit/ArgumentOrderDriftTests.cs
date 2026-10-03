// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY FUNCTION-IDENTIFIER'S ARGUMENT LIST IS EVALUATED LEFT TO RIGHT THROUGH ONE WINDOW (kb/Work PB1423,
/// CLAUDE.md rule 5).
/// <para>ISO §8.4.3.2.4 GR2: "its arguments are evaluated individually in the order specified in the list of
/// arguments, from left to right". A user-defined function activation is hoisted as a statement pre-op, so an
/// earlier argument that merely READS state was rendered after a later argument's activation had changed it
/// (<c>FUNCTION SUM(A, FUNCTION BUMP(A))</c> summed the changed A). The repair is one mechanism —
/// <c>ArgumentOrder.Window</c> — opened around an argument list by the ONE intrinsic call point
/// (<c>IntrinsicBinder.BindIntrinsicCore</c>) and by the ONE user-defined activation (<c>UdfBinder.UdfActivate</c>),
/// fed by the ONE argument binder (<c>IntrinsicBinder.BindArgOperand</c>). A function kind added tomorrow that binds
/// its arguments through <c>BindArgOperand</c> inside either entry is ordered automatically; one that binds them
/// anywhere else is what this test refuses.</para>
/// </summary>
public sealed class ArgumentOrderDriftTests
{
    private static readonly string CompilerDir = TestRepo.Src("Cobol.Net.Compiler");

    /// <summary>The file's code with every comment removed — prose naming a method is not a call to it.</summary>
    private static string CodeOf(string path)
    {
        string text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(text, @"//[^\r\n]*", "");
    }

    private static string Verb(string file) =>
        CodeOf(Path.Combine(CompilerDir, "Binding", "Procedure", "Verbs", file));

    private static System.Collections.Generic.IEnumerable<string> CompilerSources() =>
        Directory.EnumerateFiles(CompilerDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                        && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));

    [Fact]
    public void TheArgumentBinder_RecordsIntoTheOpenWindow()
    {
        string code = Verb("IntrinsicBinder.cs");
        Assert.True(Regex.IsMatch(code, @"BindArgOperand\([^)]*\)\s*\{[^}]*ArgOrder\.Record\("),
            "IntrinsicBinder.BindArgOperand no longer records the operand it bound into ArgumentOrder — an argument "
            + "would be invisible to the §8.4.3.2.4 GR2 left-to-right settlement.");
    }

    [Fact]
    public void TheIntrinsicCallPoint_OpensAndSettlesTheWindow()
    {
        string code = Verb("IntrinsicBinder.cs");
        Assert.Contains("ArgOrder.Open()", code);
        Assert.Contains(".Settle(", code);
    }

    [Fact]
    public void TheUserFunctionActivation_OpensAndFreezesTheWindow()
    {
        string code = Verb("UdfBinder.cs");
        Assert.Contains("ArgOrder.Open()", code);
        Assert.Contains(".Freeze(", code);
    }

    [Fact]
    public void NoOtherBinder_BindsFunctionArgumentsOrOpensAWindow()
    {
        var openers = CompilerSources()
            .Where(f => Path.GetFileName(f) is not ("IntrinsicBinder.cs" or "UdfBinder.cs" or "ArgumentOrder.cs"))
            .Where(f => Regex.IsMatch(CodeOf(f), @"\.BindArgOperand\(|ArgOrder\.Open\(\)"))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(openers.Count == 0,
            $"{string.Join(", ", openers)} binds function arguments or opens an ArgumentOrder window outside the two "
            + "entries that settle it — its arguments would evaluate in registration order, not left to right "
            + "(ISO §8.4.3.2.4 GR2; kb/Work PB1423).");
    }
}
