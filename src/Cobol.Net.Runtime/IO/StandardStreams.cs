// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>
/// THE process-wide rule for what WiseOwl COBOL's standard streams carry: <b>UTF-8</b> on standard output and standard
/// error, and on a redirected standard input. Only an entry that IS the process applies it: the <c>cobol</c> CLI's
/// <c>Main</c> and <see cref="ProgramTable.RunModule"/>, the generated <c>Main</c>'s one statement. A host's process
/// console belongs to the host, and no run unit the host embeds touches it (kb/Work PB3193).
/// </summary>
/// <remarks>
/// <para>
/// The repertoires are UTF-16 (CONFORMANCE.md §7 item 188) and the alphanumeric↔national correspondence is the
/// total identity (item 33), so the DEVICE boundary must carry the repertoire or every wide character silently
/// becomes mojibake at the OS code page — a data loss no rule authorizes. The standard display device therefore
/// writes UTF-8 (CONFORMANCE.md §7 item 59; the PB59 landing), and <c>ProcessObserver</c> in the test harness
/// decodes every capture the same way. The ACCEPT device is the same boundary in the other direction (item 1: the
/// device delivers characters of the UTF-16 repertoire), so a redirected standard input is DECODED as UTF-8, as it
/// already was on a host whose console default is UTF-8. On Windows the default was the console's OEM code page, and
/// a piped <c>é</c> (the bytes C3 A9) reached the program as the two characters <c>├⌐</c> under cp437 (kb/Work
/// PB3193, measured).
/// </para>
/// <para>
/// ⛔ ONLY A PROCESS ENTRY CALLS IT (kb/Work PB3193; docs/CONFORMANCE.md DOC-A.1-59, DOC-A.1-167). The console
/// (<see cref="Console.Out"/>, <see cref="Console.Error"/>, <see cref="Console.In"/>, <see cref="Console.OutputEncoding"/>)
/// belongs to the PROCESS, and the entries that are the process own it: the CLI, and
/// <see cref="ProgramTable.RunModule"/> ("A standalone <c>cobol</c> executable IS its own host", DOC-A.1-167). A host
/// that embeds a run unit (<see cref="ProgramTable.RunMain"/>, <c>RunUnit.Run</c>, a CALL into a module) owns its
/// console, and the run unit writes to it as the host configured it: under a host, the standard display device IS the
/// host's <see cref="Console.Out"/> (DOC-A.1-59). Before PB3193 every <c>RunMain</c> called this, so a host that had
/// given its console another encoding found it switched to UTF-8 by the first run unit it ran, and a host with no
/// console found its own writers replaced by the fallback's. That is the console twin of the exit-code defect PB3163
/// fixed the same way. ISO §14.6.1 makes a run unit "an independent entity that may be executed without communicating
/// with, or being coordinated with, any other run unit" (except through messages, files and switches), and a run unit
/// that rewrote a process-wide cell would coordinate with its host through it. <c>ProcessStateOwnerDriftTests</c>
/// holds the callers to the two process entries.
/// </para>
/// <para>
/// ⛔ WHY IT IS A SHARED HELPER AND NOT AN <c>if</c> (kb/Work PB899). The rule was written down in exactly one of
/// the two places that needed it: the run-unit entry set the encoding for a RUN program, and the
/// <c>cobol</c> CLI's diagnostic path set nothing at all. On a default Windows console that is cp437, so the
/// section sign in <c>(ISO §11.10.3 SR5–6)</c> left the process as the single byte <c>0x15</c> and the en dash
/// as <c>-</c> — measured, byte-for-byte, through a redirected stream. Every ISO citation the compiler prints
/// was corrupted in every CI log and every redirected capture, and no golden could see it because the corpus
/// runner matches <c>.err</c> files on a pure-ASCII diagnostic CODE substring. That is
/// <c>feedback_two_arm_dispatch</c>, the most reproducible defect shape in this repository: a dispatch with two
/// arms, one of them fixed. There is now ONE arm.
/// </para>
/// <para>
/// Idempotent and defensive: the work happens once per process, and a process with no console at all (where the
/// <see cref="Console.OutputEncoding"/> setter raises <see cref="IOException"/>) still gets UTF-8, through
/// explicit writers over the standard handles. Setting <see cref="Console.OutputEncoding"/> re-creates BOTH
/// <see cref="Console.Out"/> and <see cref="Console.Error"/>, which is why diagnostics — written to stderr —
/// are covered by the same call. Standard input gets an explicit UTF-8 reader only when it is REDIRECTED (a pipe
/// or a file, whose bytes are in an external encoding): the <see cref="Console.InputEncoding"/> setter would also
/// change the code page of a console the parent shell shares, and an interactive console's input is decoded by
/// the console.
/// </para>
/// </remarks>
public static class StandardStreams
{
    private const int Utf8CodePage = 65001;

    /// <summary>0 = not yet applied, 1 = applied. The encoding is a process-wide property, so the work is
    /// done once however many run units, CALLs or compilations pass through.</summary>
    private static int _applied;

    /// <summary>
    /// Make this process's standard output and standard error carry UTF-8, and decode a redirected standard input
    /// as UTF-8. Called by an entry that IS the process (the CLI's <c>Main</c>, <see cref="ProgramTable.RunModule"/>)
    /// and by nothing a host embeds. Safe to call any number of times, on any platform, including in a process with
    /// no console.
    /// </summary>
    public static void EnsureUtf8()
    {
        if (Interlocked.Exchange(ref _applied, 1) != 0) return;

        // BOM-less: a BOM would prefix every capture, and every golden, with three bytes of nothing.
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        // Standard input first, before anything has read from it, so no byte is buffered under another decoding. The
        // reader is shaped like Console's own (no byte-order-mark detection), so a platform whose default is already
        // UTF-8 reads exactly what it read before.
        if (Console.IsInputRedirected)
        {
            try { Console.SetIn(new StreamReader(Console.OpenStandardInput(), utf8, detectEncodingFromByteOrderMarks: false)); }
            catch (IOException)
            {
                // No standard input handle at all: Console.In stays the runtime's own, and an ACCEPT reads end of input.
            }
        }

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
            // A process with no standard handles at all (a Windows service). There is nothing to encode, and
            // refusing to start over it would be worse than the mojibake this prevents.
        }
    }
}
