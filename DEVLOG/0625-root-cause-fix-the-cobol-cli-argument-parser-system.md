## Entry 625 — 2026-07-06 12:11 PDT — Root-cause fix: the `cobol` CLI argument parser (System.CommandLine)

**Fixed a real parser bug at the root instead of working around it** (the owner's standing rule, restated this
session: *never work around an issue — fix the root cause; apply it to all existing workarounds*). While testing
the M2-UDF work I mislabelled two behaviours as CLI "quirks" and coded around them — the honest diagnosis is a
broken hand-rolled arg parser:

**Root cause.** `CliOptions.Parse` was a `switch` where every value option (`-o`, `--nist`, `--std`, `--copy`)
called `Next()`, which **unconditionally consumed the following token even when it was another option flag**. So:
- `--nist --run` bound `"--run"` as the NIST test name — `--run` never took effect, so the program compiled but
  **never ran** (the empty output I wrongly called "the `--nist --run` quirk").
- `--nist -o X` ate `-o` as the NIST name, then `X` fell into the source slot ("source file not found: X").
- `-o=path` / `--std=2002` were rejected as "unknown option" — the `--opt=value` form was unsupported entirely.
- A second positional silently OVERWROTE the source instead of erroring.

**The fix — stop hand-rolling arg parsing.** Adopt **`System.CommandLine`** (Microsoft's first-party parser, the
one the `dotnet` CLI itself uses; owner-chosen over a hand-rolled parser / a third-party lib). It handles
`--opt value` and `--opt=value` uniformly, enforces arity, refuses to let an option swallow a following flag, and
auto-generates `--help`/`--version`. `CliOptions` is now a pure DTO; `Program.BuildParser()` returns the configured
`RootCommand` + a pure `ParseResult → CliOptions` resolver (the `--nist`/`--std` defaulting: `--nist`'s test name
is now OPTIONAL, derived from the source file's base name; `--std` wins when given, else 85 under `--nist`, else
2023). `--std` is validated to {85, 2002, 2014, 2023}.

**Evidence.** New `CliParserTests` (11 facts via the pure resolver — both `-o value`/`-o=value` forms, `--output`
alias, the `--nist --run` regression, optional/derived NIST name, `--std` defaulting + rejection, required/single
source, repeatable `--copy`, the full `version-continuity-sweep.sh` invocation). Verified on the real corpus: the
greenfield NIST compile (`SRC -o OUT --nist NAME --std 85`) still works (X-card substitution intact), `--help` is
clean, `--std 99` / missing / duplicate source all error. Unit battery 212 GREEN (+11). The package is pinned via
Central Package Management (`Directory.Packages.props`, `System.CommandLine 2.0.9`). The LEGACY `CobolSharp.CLI`
parser is UNTOUCHED (the frozen differential oracle until G8). ⚠ Lesson logged (transparency): I coded around the
symptom twice before diagnosing the parser — the standing rule is exactly to invert that.

