# Contributing to CobolSharp

Thank you for your interest in CobolSharp (COBOL.NET) — a COBOL compiler that implements ISO/IEC 1989:2023, with
correct support for the 1985, 2002 and 2014 editions, and compiles COBOL to idiomatic typed-native C# built by
Roslyn.

This guide covers how to report a problem, how to propose a change, and what a pull request needs before it can be
merged. By participating you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Ways to contribute

- **Report a wrong answer.** A program that compiles and runs but produces output the standard does not allow is the
  most valuable report you can file. Use the *Wrong output* issue form.
- **Report a compiler crash.** Any unhandled exception, stack trace or hang in the `cobol` command is a bug, whatever
  the input. Use the *Compiler crash* form.
- **Report a conformance finding.** The compiler rejects source the standard allows, accepts source it forbids, or
  gates a construct to the wrong edition. Use the *Conformance finding* form and cite the clause.
- **Improve the documentation**, including `docs/CONFORMANCE.md`, the implementor-defined determinations.
- **Send a pull request** — see below.

Security vulnerabilities are **not** filed as issues; see [SECURITY.md](SECURITY.md).

## The standard is the authority

Every question of syntax, semantics or output is settled by the text of **ISO/IEC 1989:2023**. The Markdown
transcription lives at `specs/ISO_COBOL.md`. When you report a defect or change behavior, cite the exact clause,
syntax rule (SR) or general rule (GR) — for example `§14.9.25.3 SR1` — and check the citation mechanically:

```bash
python scripts/spec/cite.py --check 14.9.25.3 "<the text you are quoting>"
```

Other compilers (GnuCOBOL, IBM Enterprise COBOL, Micro Focus) and the NIST CCVS suite are useful evidence, but they
are not authority. Where the standard leaves a choice to the implementor, the order of precedence is: the standard if
it decides → GnuCOBOL → IBM Enterprise COBOL or Micro Focus. That choice is recorded in `docs/CONFORMANCE.md`.

## Contributor License Agreement

CobolSharp is distributed under the [Business Source License 1.1](LICENSE), and the licensor also offers commercial
licenses. So that every contribution can be distributed under all of those terms, **each contributor signs the
[Contributor License Agreement](CLA.md) once**, before their first pull request can be merged. You keep the
copyright in your contribution; the agreement grants the project the licenses it needs to ship it.

A bot comments on your first pull request with a link to sign. It takes a minute, and it covers every future pull
request you make.

## Building and testing

Prerequisites: the **.NET 10 SDK**, **PowerShell 7+** (`pwsh`, used for ANTLR parser generation and the gate
scripts), **Python 3.14+** and **Java 21+** (for ANTLR).

```bash
git clone https://github.com/BrentRector/CobolSharp.git
cd CobolSharp
dotnet build CobolSharp.sln
```

Always build the **solution**, not a single project, before running tests with `--no-build`; otherwise you test a
stale compiler.

The per-change gate builds the solution and runs the conformance tests for the area you touched, plus the Unit and
Characterization suites:

```bash
pwsh scripts/build-local.ps1 -Filter "~Inspect|~Unstring"     # choose terms that match what you changed
```

The filter is required. Read the verdict line it prints; a leg with no verdict line is a failure, not a pass. CI runs
the full matrix on Windows and Linux for every pull request.

## What a pull request needs

1. **An issue first** for anything larger than a small fix, so the approach can be agreed before you write it.
2. **A spec citation** for every behavior change, in the pull request description and in a comment at the code that
   implements it.
3. **Tests that ship with the change.** A behavior change adds a conformance program under `tests/conformance/`:
   one passing ("golden") program at the edition that introduced the construct, with its expected output, and — for
   anything an earlier edition forbids — one negative program showing it is rejected there. Register each in its
   `manifest.json`.
4. **A root-cause fix.** Do not work around a compiler bug in the test program, and do not special-case the test.
   Every bug is usually a pattern: look for the same mistake in its siblings.
5. **Typed-native code.** A COBOL record is a .NET `record struct` and an elementary item is a native field; do not
   introduce byte-array storage outside the file boundary.
6. **Documentation kept current** in the same pull request, when you change behavior a document describes.
7. **A green CI run.** `main` is protected: nothing merges without a passing `ci-gate` check.

Keep pull requests focused — one defect or one feature per pull request.

## Commit messages

Write the subject as a short imperative summary (≤ 72 characters). In the body, explain *what was wrong*, *which
clause says so*, and *how you verified the fix* — enough that someone reading `git log` a decade from now understands
the change without the pull request.

## Review and merge

The maintainer reviews every pull request against the standard, the design documents under `docs/`, and the four
review dimensions this project holds every change to: architecture, correctness, performance, and duplication. Merged
changes land on `main` through the protected CI gate.

## Questions

Start from the documentation map in `docs/DOC_INDEX.md`.
