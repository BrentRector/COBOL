// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;

namespace CobolNet.Frontend.Diagnostics;

/// <summary>
/// Collects diagnostics during compilation. Passed through all compiler phases.
/// </summary>
public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = [];

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
    public bool HasErrors => _diagnostics.Exists(d => d.IsError);

    private readonly List<CompileOutputLine> _compileOutput = [];

    /// <summary>What the compilation itself TRANSFERS to its operator, in order: the lines the DISPLAY directive writes at
    /// compile time (ISO §7.3.12.4 GR1, kb/Work PB1538). Not a diagnostic — a diagnostic names a defect at a position,
    /// and a transferred line is the program's own output — so it travels beside them on the sink every phase already
    /// holds, and the driver hands it to the CLI, which writes each line to the stream it names.</summary>
    public IReadOnlyList<CompileOutputLine> CompileOutput => _compileOutput;

    /// <summary>Transfer one line to <paramref name="stream"/> when the compilation's output is presented.</summary>
    public void Transfer(CompileOutputStream stream, string text) => _compileOutput.Add(new CompileOutputLine(stream, text));

    /// <summary>Carry <paramref name="lines"/> over from another bag (a speculative front-end pass whose result is the
    /// compilation's).</summary>
    public void AddCompileOutput(IEnumerable<CompileOutputLine> lines) => _compileOutput.AddRange(lines);

    public void Report(string code, DiagnosticSeverity severity, string message,
        SourceLocation location, TextSpan span)
    {
        _diagnostics.Add(new Diagnostic(code, severity, message, location, span));
    }

    public void ReportError(string code, string message, SourceLocation location, TextSpan span)
    {
        Report(code, DiagnosticSeverity.Error, message, location, span);
    }

    public void ReportWarning(string code, string message, SourceLocation location, TextSpan span)
    {
        Report(code, DiagnosticSeverity.Warning, message, location, span);
    }

    public void Add(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);

    /// <summary>
    /// Report a diagnostic using a descriptor and format arguments.
    /// </summary>
    public void Report(DiagnosticDescriptor descriptor, SourceLocation location, TextSpan span,
        params object[] args)
    {
        string message = args.Length > 0
            ? string.Format(descriptor.MessageTemplate, args)
            : descriptor.MessageTemplate;
        _diagnostics.Add(new Diagnostic(descriptor.Code, descriptor.DefaultSeverity, message, location, span));
    }
}
