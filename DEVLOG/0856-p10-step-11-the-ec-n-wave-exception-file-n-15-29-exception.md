## Entry 856 — 2026-07-16 03:41 PDT — P10 Step 11 — the EC `-N` wave: EXCEPTION-FILE-N (§15.29) + EXCEPTION-LOCATION-N (§15.31) go Runtime on the ONE NationalOf translator; CHAR-NATIONAL (§15.16) + ORD-over-national (§15.70.4 r2) land with them

**What the spec actually defines.** The 2023 §15 table defines exactly TWO `-N` EC twins — `EXCEPTION-FILE-N`
(§15.29) and `EXCEPTION-LOCATION-N` (§15.31); there is no `-N` for EXCEPTION-STATEMENT/-STATUS (the P11 backlog
note held up against the §15.1/§15.2 function list). Each returns "a national character string" that is the SAME
content as its alphanumeric base "converted at runtime to the runtime national character set" (§15.29.4 r1c /
§15.31.3 r2b) — i.e. the conversion IS the sanctioned alphanumeric→national repertoire translation.

**One mechanism.** `EcFunctions.FileN() = CobolIntrinsics.NationalOf(File())` and
`LocationN() = NationalOf(Location())` — the Step-5 `Repertoire` translator is the single conversion channel
(no second EC formatter). Under D-N4 (Latin-1 ⊂ national, one UTF-16 char per position) the runtime string is
value-identical to the base; the compiler-observable delta is the result CATEGORY = National, carried by the
prerequisite `IntrinsicSig.ResultCategory` channel and driving Table-16 MOVE/compare legality. Catalog rows
flipped `Deferred`→`Runtime` (`EcFileN`/`EcLocationN`); `RenderString` gained the arms; the binder's
`RuntimeMethod.StartsWith("Ec")` EC-gate flag covers both for free.

**Edition window: 2002, kept.** The EC model and national data are both 2002 introductions; the 2023 Annex
E.3.3 items 25/26 list only the ADDED optional file-connector argument as the 2023 delta — proof the functions
predate 2023. So `IntroducedIn 2002` stands (D8 `COBOLNET1502` below), and the r2 argument form renders loud on
base AND twin (recorded at the `EcFile`/`EcFileN` renderer arms; VCR rows 68/69 → PHASE-13 Step 9).

**The other -N legs from the audit's gaps[]:** `CHAR-NATIONAL` (§15.16) landed as
`CobolIntrinsics.CharNational` — the national program collating sequence is native UTF-16 code-point order
(D-N1; no ALPHABET … FOR NATIONAL surface exists — P10 Step 4 — so no weights overload; out-of-range ordinal =
EC-ARGUMENT + the §15.3 one-space default). Reading §15.70.3 for the 0844 guard rewording surfaced that ORD's
argument may be NATIONAL, reading the NATIONAL sequence (§15.70.4 r2) — so the CHAR/ORD-over-national 0844
reject narrowed to CHAR only (cited §15.15.3 r1, pointing at CHAR-NATIONAL), and a national ORD argument now
binds with the alphanumeric `__COLLATE` weights suppressed (their 256-entry domain would alias national chars;
the parameterless runtime `Ord` IS the r2 value under the native national PCS). NATIONAL-OF was already live
(Step 5); the national result-category channel gap was already closed by the same prerequisite.

**Verification (all run before pinning):** goldens `tests/conformance/2002/exception_file_n.cob` (r1a `00`
pre-EC → r1c `10TF` at EC-I-O-AT-END; `FUNCTION LENGTH` pins 4/1 character positions; category proof via a
PIC N MOVE + `N"10TF"` compare; §15.31.3 r1 one national space without WITH LOCATION) and `char_national.cob`
(ordinal 66 → `A`, `N""` equality, LENGTH=1, the wide U+4E16 leg compared in-program, `ORD(N"世")=19991`) —
both ENABLED + legacy `GreenfieldOnly` (no legacy EC model/national category); matrix row
`exception-file-n-2002` (+ registry regen); negative `exception_file_n_below_2002` @85 (1502); inline EC-net
Fact `IoAtEnd_ExceptionFileN_NationalTwin` (ECT018N, 2023). Targeted battery: CorpusRunner 185, ECT suite 51,
matrix+VCR 428, NationalBoolean+intrinsics 205, unit 284, characterization 33/33 — all green. Manually
verified `--std 85` rejects 1502 and `--std 2023` runs byte-identical. One friction note: the first draft of
the inline Fact displayed `FUNCTION LENGTH(...)` directly and hit the pre-existing loud "computed expression
in a string context" DISPLAY channel — not this wave's scope; the Fact MOVEs to PIC 9 receivers instead (the
corpus golden always did). Docs: PHASE-10 Step-11 checkbox/status/verdict/gaps + the closing table row;
PHASE-11 Steps 3–4 + disposition table marked SATISFIED-BY-P10; PHASE4_RECONCILIATION M2-PROC-4/M4-2b/M2-DATA-3
rows + residue #11 + wave table; ISO2023_CONFORMANCE_PLAN M4-2b flipped ☑. No new diagnostic codes minted
(1502/0844 reused; the loud arg-form rides `NotImplemented`).


