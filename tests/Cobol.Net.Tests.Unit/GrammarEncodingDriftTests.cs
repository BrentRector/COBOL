// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ANTLR IS TOLD HOW TO READ A GRAMMAR FILE: UTF-8, ALWAYS (kb/Work PB1944). ANTLR reads a <c>.g4</c> in the JVM's
/// default charset: UTF-8 on a Java 18+ developer machine and on Linux, Cp1252 on Windows CI's JVM. A raw non-ASCII
/// character in a lexer set (PB1402's first landing wrote four Unicode noncharacters that way) was one character on
/// the first two and three Latin characters on the third, where the set repeated characters (<c>warning(180)</c>, an
/// error under warnings-as-errors), and no local gate leg could see it. The fix is that the encoding is declared, not
/// that the grammar avoids non-ASCII text (owner 2026-10-04: use UTF-8 everywhere; no escape is needed to dodge the
/// JVM default): (1) the ANTLR invocation passes <c>-encoding UTF-8</c>, so no grammar's meaning depends on the JVM
/// default; (2) every grammar file is valid UTF-8 without a byte-order mark, which is what that flag promises ANTLR.
/// </summary>
public sealed class GrammarEncodingDriftTests
{
    private static IEnumerable<string> GrammarFiles() =>
        Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories);

    [Fact]
    public void TheAntlrInvocation_ReadsTheGrammarAsUtf8()
    {
        string script = File.ReadAllText(TestRepo.Src("Cobol.Net.Frontend", "Invoke-Antlr4CSharp.ps1"));
        int java = script.IndexOf("& java -jar", StringComparison.Ordinal);
        Assert.True(java >= 0, "Invoke-Antlr4CSharp.ps1 no longer runs `& java -jar`; this drift test must follow the step");
        int end = script.IndexOf("$grammarName", java, StringComparison.Ordinal);
        string invocation = script[java..end];
        Assert.True(invocation.Contains("-encoding UTF-8", StringComparison.Ordinal),
            "the ANTLR step must pass `-encoding UTF-8`; the grammar's encoding may not depend on the JVM default "
            + "(UTF-8 locally, Cp1252 on Windows CI — kb/Work PB1944):\n" + invocation);
    }

    [Fact]
    public void EveryGrammarFile_IsUtf8WithoutABom()
    {
        var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        foreach (string file in GrammarFiles())
        {
            byte[] bytes = File.ReadAllBytes(file);
            string rel = Path.GetRelativePath(TestRepo.Root, file);
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, $"{rel} starts with a byte-order mark");
            try { strict.GetString(bytes); }
            catch (DecoderFallbackException e) { Assert.Fail($"{rel} is not valid UTF-8: {e.Message}"); }
        }
    }
}
