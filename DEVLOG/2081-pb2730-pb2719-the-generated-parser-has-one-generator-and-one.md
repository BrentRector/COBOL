## Entry 2081 — 2026-10-16 08:56 PDT — PB2730 + PB2719: the generated parser has one generator and one up-to-date decision over every input

Wave 1092 C (PB1754 R3), two R2 findings with one root: the ANTLR generation was decided and run in two places.

**Re-probe (this worktree's own build, before the change).** A fresh `dotnet build Cobol.Net.sln` printed
`Generating C# from: CobolLexer.g4` / `CobolParserCore.g4` twice and `All ANTLR generation completed successfully.`
twice: `GenerateIfNewer.ps1` dot-sourced `Invoke-Antlr4CSharp.ps1`, whose "called directly" guard
(`$MyInvocation.MyCommand.Path -eq $PSCommandPath`) is true when dot-sourced, so the generation ran once with the
script's hard-coded namespace and exited back into the caller, which ran it again with the csproj's (PB2730).
`dotnet msbuild -t:EnsureGeneratedFiles -p:AntlrNamespace=Probe.Changed.Ns` printed `Parser files are up to date.` and
left the parser in the old namespace; touching the jar or the script changed nothing (PB2719). The probe measured a
third defect the notes did not name: the target's `Inputs="…;Grammar/Core/*.g4"` holds a wildcard MSBuild never expands
in a target's Inputs (`Input file "Grammar/Core/*.g4" does not exist.`), so the target ran on every build and the
script's `.g4`-only loop was the only staleness check in force.

**Fix.** `EnsureGeneratedFiles` is the ONE up-to-date decision. Its Inputs are the `AntlrGrammar` item
(`Grammar/**/*.g4`), the `AntlrGenerator` script (which carries the tool flags), the `AntlrJar` (a new csproj property,
the jar's one spelling; the redundant `None` items are gone) and `obj/antlr-generation.stamp`, which a new
`WriteAntlrGenerationStamp` target rewrites with `WriteOnlyWhenDifferent` from the namespace and jar path. It runs the
generator directly. `GenerateIfNewer.ps1` is deleted (its only caller was the target). `Invoke-Antlr4CSharp.ps1` is a
plain script with mandatory `-PackageName`, `-JarPath`, `-OutputDir` and no defaults or entry guard.

**Found and fixed on the way (I9).** ANTLR writes its files and then reports a warning with exit 0, so a run that
failed on `warning(125)` left fresh outputs the next build took for up to date. The generator now empties `Generated/`
before generating and again on any failure, and runs under `$ErrorActionPreference = 'Stop'` with a trap, so a failed
copy or delete fails the build.

**After.** Fresh → one generation; warm → `Skipping target "EnsureGeneratedFiles"`; a namespace change, the jar or the
script touched, or a `.g4` in a new `Grammar/` subfolder → regenerates; an injected grammar warning or a missing jar →
MSB3073 with `Generated/` empty, and the next build recovers. The generated files are byte-identical to the baseline
(`diff -r`), and the §4 oracle reads `=== ARCH-ORACLE: IDENTICAL — 7957 cases (baseline bfb2eb47e9b0, new
855996444353) ===`. Pin: `AntlrGenerationDriftTests` (five facts). Docs: DESIGN-architecture-review §8.4's pin line names the
target, DESIGN-frontend-grammar §3.5 item 1a, COBOLNET_DESIGN / PROJECT_ORG, DESIGN-module-topology,
DESIGN-edition-framework, EVAL-antlr-leverage, kb/Compiler/Build System, PB1957 row 11, PB2413. No inventory row (both
notes `closes_rows: []`, process-only). The csproj's Exec also runs `pwsh -NonInteractive`, so a missing mandatory
parameter fails instead of waiting for input.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — … Conformance 6,062/11,770 · Unit
33,272/33,698 · Characterization 6/36 cases ran …` (run 20261016T133256Z-eb5152); the five `AntlrGenerationDriftTests`
facts passed in leg 1. Linux gate: the lander's (batched gating).

**Finisher (the landing's Linux gate RED on 0f8e83d26).** `HostWitnessTraitDriftTests.TheSweepTrait_…` failed in the
whole-population Linux unit leg: "missing the trait: AntlrGenerationDriftTests". The new class reads
`TestRepo.Src("Cobol.Net.Frontend")`, two segments, so `drift_rules.py --sweeps` classifies it a tree-wide sweep on
both hosts alike (measured: the Windows `--sweeps` lists it). It now carries `[Trait("Sweep", "tree")]`. The Windows
gate did not fail on the same source for a planner reason, not a host one: both of the trait guard's dear facts
(`TheSweepTrait_…`, 5-11 s under load, and `EveryFileSystemRaceWitness_…`, 2-9 s, because they spawn the scans) cost
over `SWEEP_CHEAP_SECONDS`, so tier 0c left them in leg 2 and they were NOT RUN in either Windows run
(`not-run-Unit.txt` of 20261016T133256Z and 20261016T134854Z), while the Linux gate runs every unit case. The same
cause explains the sibling branch `worktree-wf_e3f5e138-3a8-20`'s red in the other direction ("trait on a non-sweep:
PictureSeparatorDriftTests", whose path `src/Cobol.Net.Compiler/Binding` is three segments): its two Windows runs left
the guard NOT RUN too. Fix: `gate_plan.TRAIT_GUARD` puts the guard's class in tier 0h, leg 1 at every gate whatever
its cost, because a tier the gate does not check at every gate is a tier on trust. A planner self-test case plants a
dear guard and proves it lands in leg 1 (red with the fix removed), and `ThePlan_Orders…` pins the name to
`nameof(HostWitnessTraitDriftTests)`. Sweep: 185 other sweep cases are over the threshold and stay leg 2 by the
design's cost rule; the guard is the only one whose rule is the gate's own tier membership.

