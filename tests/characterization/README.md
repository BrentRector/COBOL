# Characterization corpus (rearchitecture Phase 0)

A **curated, one-per-family, tiny** COBOL corpus that anchors the behavior-neutrality gates. It is deliberately small
and legible (NOT the NIST set) so a diff is human-readable and the run is fast. Snapshots seeded ONCE from the
pre-refactor emitter; every later rearchitecture phase asserts against them to *prove* it changed no observable
behavior.

## Layout
- `positive/*.cob` — programs expected to COMPILE. Exercised by both gates:
  - **gate 2** (`DiagnosticSnapshotTests`) — the WiseOwl COBOL diagnostic surface (a CheckOnly compile), snapshotted to
    `../Cobol.Net.Tests.Characterization/Snapshots/<name>.<edition>.diag.txt`.
  - **gate 3** (`EmittedCSharpSnapshotTests`) — the generated C#, snapshotted to `<name>.<edition>.g.cs.txt`.
- `negative/*.cob` — programs expected to FAIL with a diagnostic. Exercised by **gate 2 only** (they emit no C#).
- `<name>.std` sidecar (optional) — the ISO edition to compile at. **Default = 85** (the core edition). Feature
  programs that need a later edition carry a sidecar (e.g. `char_typedef.std` = `2002`).

## Families covered (positive)
MOVE (elementary / edited / figurative), group MOVE, arithmetic + ON SIZE ERROR, IF/EVALUATE + class/sign conditions,
PERFORM (TIMES / VARYING / out-of-line), OCCURS + subscript + INDEXED + SET + SEARCH, REDEFINES (Tier-A + Tier-B),
level-66 RENAMES, INSPECT / STRING / UNSTRING, INITIALIZE, intrinsic FUNCTION, CALL … USING, sequential file I/O, and a
weak TYPEDEF (2002). The NIST/conformance suites cover the deeper 2002+ feature surface; this corpus's job is broad,
tiny coverage of the emitter's shapes.

## Negatives
`char_neg_undef` (undefined data-name), `char_neg_typedef85` (a 2002 construct at `--std 85` → an edition-gate
diagnostic), `char_neg_pic` (`JUSTIFIED` on a numeric item — illegal per §13.18.32.3 SR3).

## Re-seeding / re-baselining (LOCAL ONLY — never in CI)
When a phase INTENTIONALLY changes emission/diagnostics, re-baseline with review:
```
COBOLNET_UPDATE_SNAPSHOTS=1 dotnet test tests/Cobol.Net.Tests.Characterization
```
Then read the snapshot diff in the PR and commit it alongside the source change. CI never sets that variable; it only
compares. Gate 3 (emitted C#) is advisory — a diff is a RED only if gate 1 (goldens) or gate 2 fails too, OR there is
no corresponding source change in the PR.
