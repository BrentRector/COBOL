// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Diagnostics;

/// <summary>The compiler's own output streams a compile-time transfer may name — the compiler process's standard output
/// and its standard error (the SYSOUT / CONSOLE and SYSERR device-names of <c>ImplementorNames</c>, DOC-A.1-54).</summary>
public enum CompileOutputStream
{
    /// <summary>The compiler's standard output — where the DISPLAY directive's data goes with no UPON phrase, with UPON
    /// LISTING (no listing is produced, DOC-A.1-117), and with the output devices CONSOLE and SYSOUT.</summary>
    Standard,

    /// <summary>The compiler's standard error — the output device SYSERR.</summary>
    Error,
}

/// <summary>One line the compilation transfers to its operator at compile time (ISO §7.3.12.4 GR1, kb/Work PB1538).</summary>
/// <param name="Stream">The compiler stream the line is written to.</param>
/// <param name="Text">The line, without its terminator.</param>
public readonly record struct CompileOutputLine(CompileOutputStream Stream, string Text);
