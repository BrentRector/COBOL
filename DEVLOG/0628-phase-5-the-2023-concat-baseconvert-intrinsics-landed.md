## Entry 628 — 2026-07-06 14:47 PDT — Phase 5: the 2023 CONCAT + BASECONVERT intrinsics — LANDED

Two of the seven catalogued-but-Deferred 2023 intrinsics (`IntrinsicCatalog.cs`) are now live — the clean
positional-argument ones. (After scoping the RESUME-NEXT M2-OO-1h residue this turn, I reprioritized: unstaging
that residue means extending the whole data-model machinery — REDEFINES classification / ODO / index fields /
RENAMES places — into method scope, an intricate/niche Phase-3-port piece; the staged intrinsics are cleaner,
spec-defined, golden-bearing units that deliver real ISO-2023 functionality — the north star.)

- **CONCAT (§15.18)** — concatenates all argument IMAGES in order (rule 1: "all of the characters", so a
  fixed-width field's trailing padding is included; rule 4: variadic, left to right). `CobolIntrinsics.Concat(
  params string[])`; catalog `MinArgs` tightened 1→2 (§15.18.2 requires argument-1 AND argument-2).
- **BASECONVERT (§15.12)** — an unsigned integer's digits re-expressed from base arg2 into base arg3 (both
  2..16; digits 0-9/A-F). `CobolIntrinsics.BaseConvert(string, long, long)` over `BigInteger` (no overflow);
  an out-of-range base or an invalid source-base digit sets EC-ARGUMENT-FUNCTION and returns the §15.3 default.

Each landed the clean way: flip the catalog row `Deferred`→`Runtime` with a `RuntimeMethod`, add one
`IntrinsicRenderer.RenderString` case (the existing variadic-string-arg + `IntStatic` machinery carried it), and
one `CobolIntrinsics.Text.cs` body. The D8 edition gate (COBOLNET1502 below 2023) was already wired from the
catalog `IntroducedIn`. New golden `tests/conformance/2023/intrinsics_string_2023.cob` (CONCAT image/variadic +
BASECONVERT 16→10/10→16/2→16) — **GreenfieldOnly**: the frozen legacy has CONCAT but CRASHES on BASECONVERT
(InvalidCastException). +6 `IntrinsicFunctionDifferentialTests` (CONCAT images, the four BASECONVERT round-trips,
the 2023 gate). Battery: **1875 conformance (+7) · 216 unit · legacy integration 60 GREEN**; greenfield-only, so
the NIST differential (in the conformance suite) confirms the 85 surface is untouched — no shared/grammar change.

**Staged residue (the remaining five 2023 intrinsics):** FIND-STRING (§15.37), SUBSTITUTE (§15.87), and TRIM
(§15.96) carry KEYWORD modifiers in their syntax (`LAST` / `START AFTER` / `ANYCASE`; `FIRST`/`LAST`;
`LEADING`/`TRAILING`) — they need a shared intrinsic-keyword-argument grammar/bind mechanism, not a clean
positional un-defer; CONVERT (§15.19) likewise; MODULE-NAME (§15.65) needs the runtime module-context + a type
keyword; SMALLEST-ALGEBRAIC (§15.83) needs the argument's PICTURE metadata. Each stays catalogued + 2023-gated +
loud (COBOLNET_DESIGN §1.4) until that wave.

