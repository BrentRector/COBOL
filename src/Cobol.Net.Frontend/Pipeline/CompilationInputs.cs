// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend;

/// <summary>
/// ⛔ THE ONE GATEWAY for every AMBIENT input a compilation reads — the files it opens, the paths it probes, the
/// environment variables it consults and the wall clock (kb/Work PB985; <c>DESIGN-test-build-ci.md</c> §3.12). Each
/// read goes through a method here, which performs it AND records it, so after a compile the record names every
/// fact outside the source text and the options that the compiler's output depended on — the dependency list a
/// build tool needs (the <c>gcc -MD</c> depfile shape) and the key a compiled-program cache needs.
/// <para>A read that bypasses this class is an input no cache and no incremental build can see.
/// <c>CompilationInputsDriftTests</c> (Conformance) makes that a compile-time-of-the-tests failure: an ambient-read
/// API anywhere in <c>Cobol.Net.Compiler</c> / <c>Cobol.Net.Frontend</c> / <c>Cobol.Net.Editions</c> outside this
/// file is red unless its line says why it is not a compilation input (<c>// not a compilation input: …</c>).</para>
/// <para>One instance per compilation; not thread-safe (a compilation is single-threaded).</para>
/// </summary>
public sealed class CompilationInputs
{
    private readonly List<string> _filesRead = [];
    private readonly List<InputProbe> _probes = [];
    private readonly List<EnvironmentRead> _environment = [];

    /// <summary>Every file whose CONTENT the compilation read, as a full path, in read order (the source first,
    /// then each copybook as COPY incorporates it).</summary>
    public IReadOnlyList<string> FilesRead => _filesRead;

    /// <summary>Every existence probe and its answer — including the ones that found NOTHING. A copybook search
    /// that missed <c>dir1/X.cpy</c> and found <c>dir2/X.cpy</c> resolves differently the moment the first file
    /// appears, so an absent answer is as much an input as a present one.</summary>
    public IReadOnlyList<InputProbe> Probes => _probes;

    /// <summary>Every environment variable consulted and the value it had, <c>null</c> when it was unset — ISO
    /// §7.3.11.4 GR4: a <c>&gt;&gt;DEFINE … PARAMETER</c> value "is obtained from the operating environment".</summary>
    public IReadOnlyList<EnvironmentRead> EnvironmentReads => _environment;

    /// <summary>True once the compilation read the wall clock — the WHEN-COMPILED stamp (§15.99.3 r2) is baked
    /// into the program, so its output is a function of WHEN it was compiled, not only of its inputs.</summary>
    public bool ReadsCompilationTime { get; private set; }

    /// <summary>Read a file's text (source or copybook), recording its full path.</summary>
    public string ReadAllText(string path)
    {
        string text = File.ReadAllText(path);
        _filesRead.Add(Path.GetFullPath(path));
        return text;
    }

    /// <summary>Probe whether a FILE exists, recording the answer.</summary>
    public bool FileExists(string path)
    {
        bool exists = File.Exists(path);
        _probes.Add(new InputProbe(Path.GetFullPath(path), IsDirectory: false, exists));
        return exists;
    }

    /// <summary>Probe whether a DIRECTORY exists, recording the answer.</summary>
    public bool DirectoryExists(string path)
    {
        bool exists = Directory.Exists(path);
        _probes.Add(new InputProbe(Path.GetFullPath(path), IsDirectory: true, exists));
        return exists;
    }

    /// <summary>The compiler process's working directory, read once per compilation (null until read) — the first
    /// place of the default COBOL library (DOC-A.1-40, kb/Work PB1355), so the output depends on it. Each probe made
    /// there is recorded by <see cref="FileExists"/> / <see cref="DirectoryExists"/> as a full path.</summary>
    public string? WorkingDirectory { get; private set; }

    /// <summary>Read the working directory (<see cref="WorkingDirectory"/>), recording it.</summary>
    public string GetWorkingDirectory() => WorkingDirectory ??= Directory.GetCurrentDirectory();

    /// <summary>Read an environment variable, recording its name and value (null = unset).</summary>
    public string? GetEnvironmentVariable(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        _environment.Add(new EnvironmentRead(name, value));
        return value;
    }

    /// <summary>Read the compilation time through <paramref name="clock"/> (the injectable compile-clock seam),
    /// recording that the output now depends on it.</summary>
    public DateTimeOffset ReadCompilationTime(Func<DateTimeOffset> clock)
    {
        ReadsCompilationTime = true;
        return clock();
    }
}

/// <summary>One existence probe of a compilation (<see cref="CompilationInputs.Probes"/>).</summary>
/// <param name="Path">The full path probed.</param>
/// <param name="IsDirectory">A directory probe (a COPY library, the NIST copy library) rather than a file probe.</param>
/// <param name="Exists">The answer the compilation acted on.</param>
public readonly record struct InputProbe(string Path, bool IsDirectory, bool Exists);

/// <summary>One environment-variable read of a compilation (<see cref="CompilationInputs.EnvironmentReads"/>).</summary>
/// <param name="Name">The variable's name.</param>
/// <param name="Value">Its value at the read; <c>null</c> when unset.</param>
public readonly record struct EnvironmentRead(string Name, string? Value);
