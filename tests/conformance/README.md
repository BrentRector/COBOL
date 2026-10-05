# WiseOwl COBOL conformance corpus

NIST CCVS covers **COBOL-85 only**. This corpus is the spec-derived equivalent for every edition WiseOwl COBOL
compiles: small, focused programs that each exercise a specific ISO rule, compiled at the matching `--std` in
STRICT mode and byte-compared against expected output. It is executed by `CorpusRunnerTests` in
`tests/Cobol.Net.Tests.Conformance`, so it provides **both conformance evidence and regression protection**, and
its programs are the `test-ref` witnesses the P14 traceability inventory cites.

## Layout

```
tests/conformance/85/         compiled --std 85          (the X3.23-1985-only goldens)
tests/conformance/2002/       compiled --std 2002
tests/conformance/2014/       compiled --std 2014
tests/conformance/2023/       compiled --std 2023
tests/conformance/negative/   programs that MUST be REJECTED, at the editions their own header names
```

Each directory carries a `manifest.json` with two flat name lists, `enabled` and `pending`. It is a **discovery
register**: `CorpusRunnerTests.Manifest_CoversEveryProgram_NoOverlap` asserts that every `.cob` on disk is listed
in exactly one of them, so nothing can be silently undiscovered. `pending` catalogues a program whose feature has
not landed yet (the mass-red guard) — the wave that lands the feature moves it to `enabled`.

## Adding a positive test

Drop **two files** in the edition directory and add the name to that directory's `manifest.json` `enabled` list:

- `<name>.cob` — a self-contained COBOL program that exercises the rule and `DISPLAY`s a deterministic result.
  Open it with a comment block naming the ISO clause, quoting the rule, and stating **why each leg can fail** —
  the expected values are DERIVED from the spec (or from a documented implementor determination), never copied
  from a run.
- `<name>.out` — the exact expected stdout. Compared through `CutRunner.Normalize`: LF line endings, per-line
  trailing-space trim, no trailing newline. A `.cob` with no sibling `.out` is a COMPILE-ONLY entry.

Both files are CRLF, UTF-8, no BOM. `PROGRAM-ID`s must be unique across the whole corpus (.NET otherwise serves a
stale same-named assembly).

## Adding a negative test

`<name>.cob` plus `<name>.err` (the expected diagnostic substring, e.g. an edition-band code) in
`tests/conformance/negative/`, listed in its `manifest.json`. **The editions that must reject are named by the
SOURCE**, on its first line:

```cobol
      *> reject-at: 2002 2014 2023
```

The runner throws when that header is absent — a negative case may not leave its editions unstated.

## Per-golden compile options

A positive golden that needs a non-default compile option declares it **in its own leading comment block**, in the
same place and the same spirit as `*> reject-at:`:

```cobol
      *> options: sign-encoding=ascii
```

Read by `ConformanceCorpus.ApplySourceOptions` and applied to the `CompilerDriver.Options` the runner builds. Only
lines BEFORE the first non-comment line are scanned, so a program cannot reconfigure its own compile from a
literal in its PROCEDURE DIVISION, and the line must **begin** with `*> options:` — a comment that merely quotes
the header while explaining itself is prose. **An unrecognized key or value THROWS** rather than falling back to
the default: a silently dropped option compiles a different program, and the only symptom would be an output
mismatch that names the wrong cause.

Recognized keys:

| key | values | meaning |
|---|---|---|
| `sign-encoding` | `ibm` (default) · `ascii` | the DISPLAY over-punch convention (`--sign-encoding`; Annex A.1 items 177/178, kb/Work PB803) |
| `source-format` | `fixed` · `free` · `auto` | the reference format the source starts in (`--source-format`; kb/Work PB1362) |
| `flag-extensions` | `on` · `off` (default) | ISO §4.2.10's warning mechanism (`--flag-extensions`; kb/Work PB1525): the program must still compile and run identically with every nonstandard extension named by a COBOLNET2894 warning |

## Reference format — the harness contract today

The CLI starts every source in FIXED form (`--source-format` default, ISO §7.3.24.3 2)), but the conformance and
characterization harnesses compile with `SourceFormat: InitialReferenceFormat.Auto` unless a golden's
`*> options: source-format=` says otherwise, and the Auto detector (`ReferenceFormatProcessor.IsFixedForm`) reads a
file whose lines run past column 72 as FREE form. Measured 2026-09-30 (kb/Work PB1757): the harness reads 2,905 of the
3,576 conformance sources as free form, and 181 positive goldens at 2002/2014/2023 (55 of them a `>>` directive written
at column 7) plus about a dozen at 85 compile only because of it — the CLI refuses them. Until PB1757's owner decision
lands, a golden that must be read in fixed form says so with `*> options: source-format=fixed`, and a new golden writes
a fixed-form `>>` directive at column 8 and keeps program text within columns 8–72.

## The rule

**Every rule implemented or determination documented MUST ship with at least one witness here, in the same commit
as the feature** (CLAUDE.md rule 3 / the `goldens_ship_with_the_feature` discipline). This corpus is the per-edition
conformance evidence and the regression net as features accrue; `docs/CONFORMANCE.md` is where the
implementor-defined determinations those goldens pin are written down.
