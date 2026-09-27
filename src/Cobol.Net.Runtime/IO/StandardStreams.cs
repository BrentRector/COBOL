// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>
/// THE process-wide rule for what WiseOwl COBOL puts on stdout and stderr: <b>UTF-8, always, everywhere</b> — one
/// rule, one place, called by every entry point this product has.
/// </summary>
/// <remarks>
/// <para>
/// The repertoires are UTF-16 (CONFORMANCE.md §7 item 188) and the alphanumeric↔national correspondence is the
/// total identity (item 33), so the DEVICE boundary must carry the repertoire or every wide character silently
/// becomes mojibake at the OS code page — a data loss no rule authorizes. The standard display device therefore
/// writes UTF-8 (CONFORMANCE.md §7 item 59; the PB59 landing), and <c>ProcessObserver</c> in the test harness
/// decodes every capture the same way.
/// </para>
/// <para>
/// ⛔ WHY IT IS A SHARED HELPER AND NOT AN <c>if</c> (kb/Work PB899). The rule was written down in exactly one of
/// the two places that needed it: <c>ProgramTable.RunMain</c> set the encoding for a RUN program, and the
/// <c>cobol</c> CLI's diagnostic path set nothing at all. On a default Windows console that is cp437, so the
/// section sign in <c>(ISO §11.10.3 SR5–6)</c> left the process as the single byte <c>0x15</c> and the en dash
/// as <c>-</c> — measured, byte-for-byte, through a redirected stream. Every ISO citation the compiler prints
/// was corrupted in every CI log and every redirected capture, and no golden could see it because the corpus
/// runner matches <c>.err</c> files on a pure-ASCII diagnostic CODE substring. That is
/// <c>feedback_two_arm_dispatch</c>, the most reproducible defect shape in this repository: a dispatch with two
/// arms, one of them fixed. There is now ONE arm.
/// </para>
/// <para>
/// Idempotent and defensive: the work happens once per process, and a host with no console at all (where the
/// <see cref="Console.OutputEncoding"/> setter raises <see cref="IOException"/>) still gets UTF-8, through
/// explicit writers over the standard handles. Setting <see cref="Console.OutputEncoding"/> re-creates BOTH
/// <see cref="Console.Out"/> and <see cref="Console.Error"/>, which is why diagnostics — written to stderr —
/// are covered by the same call.
/// </para>
/// </remarks>
public static class StandardStreams
{
    private const int Utf8CodePage = 65001;

    /// <summary>0 = not yet applied, 1 = applied. The encoding is a process-wide property, so the work is
    /// done once however many run units, CALLs or compilations pass through.</summary>
    private static int _applied;

    /// <summary>
    /// Make this process's standard output and standard error carry UTF-8. Safe to call from anywhere, any
    /// number of times, on any host — including one with no console.
    /// </summary>
    public static void EnsureUtf8()
    {
        if (Interlocked.Exchange(ref _applied, 1) != 0) return;

        // BOM-less: a BOM would prefix every capture, and every golden, with three bytes of nothing.
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        try
        {
            if (Console.OutputEncoding.CodePage != Utf8CodePage) Console.OutputEncoding = utf8;
            return;
        }
        catch (IOException)
        {
            // No console to set a code page on. The streams are still ours to encode — fall through.
        }
        catch (System.Security.SecurityException)
        {
            // Same: the handle exists but the policy refuses. The explicit writers below need no privilege.
        }

        // ⚠ The fallback is not a second mechanism: it is the SAME rule applied where the console API cannot
        // reach. AutoFlush matches what Console's own writers do, so nothing a program printed can be lost at
        // an abnormal termination.
        try
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError(), utf8) { AutoFlush = true });
        }
        catch (IOException)
        {
            // A host with no standard handles at all (a Windows service, an embedded host). There is nothing
            // to encode, and refusing to start over it would be worse than the mojibake this prevents.
        }
    }
}
