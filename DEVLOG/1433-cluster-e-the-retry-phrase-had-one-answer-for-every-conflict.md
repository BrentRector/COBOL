## Entry 1433 — 2026-09-02 18:02 PDT — Cluster E: the RETRY phrase had one answer for every conflict class, the discipline ran on statuses that are not conflicts at all, and three I-O syntax rules were enforced only in the mode nobody gates on

Fix-lane cluster E of the burn-down (PB142 · PB144 · PB196), landed from worktree
`agent-a383892848abd6895` (base `2acbd842`, four WIP commits). Every mechanism below was reproduced by a CLI
probe on the implementer's tree before it was fixed, and re-verified here after the rebase onto the golden
lane's `c11949e3`.

**The RETRY phrase answered `52` for everything, and `52` is the deadlock value.** `FileRegistry.RetryLoop`'s
SECONDS/FOREVER arm never read its amount and manufactured I-O status `52` for every exhausted retry of every
conflict class. §9.1.13.8 item 2 defines `52` as *"The input-output statement is unsuccessful due to a
deadlock"*; §9.1.13.9 defines the FILE SHARING conflicts and gives them `61` (OPEN) and `62` (DELETE FILE) and
**no deadlock value at all**. So `DELETE FILE … RETRY FOREVER` on a file another connector holds open answered
`52` where §14.9.10.4 GR15b is imperative — *"The value … is placed"* — about `62`, and `OPEN … SHARING WITH NO
OTHER` answered `52` where §9.1.13.9 item 1 says `61`. The wrong first digit is not cosmetic: §9.1.13.1 keys the
exception-name on it, so the program also took `EC-I-O-RECORD-OPERATION` where the standard names
`EC-I-O-FILE-SHARING`. The latitude §14.9.10.4 GR6b actually grants ("*A* value … to indicate the record
operation conflict condition", against GR15b's "*The* value") is now exercised in ONE place,
`FileRegistry.ExhaustionStatus`, instead of being improvised at six `RetryLoop` call sites.

**⭐ The widest-reach arm was not in the filed note.** Probing the fix turned up a second root defect: `RetryLoop`
engaged on ANY non-Success status, where §14.7.9.3 GR4 scopes the whole discipline to *"a file sharing conflict
condition or a record operation conflict condition"*. `OPEN INPUT … RETRY FOREVER` of an **absent** file
therefore entered an unterminating retry and eventually answered `52` — a deadlock report for a file-not-found —
instead of `35`. An ordinary missing file, under a legal phrase, became a hang. The fix is a conflict-CLASS gate
on entry, not a status filter at the exits, and the new drift test asserts every *retry-form × conflict-class*
cell **including the not-a-conflict row** — the row whose absence let this live.

**`RETRY 1.5 TIMES` truncated where §14.7.9.3 GR1 rounds up**, and `RETRY 0.5 TIMES` truncated to zero
attempts; both are now ceilings. GR1's round-up and GR2's *un*-ROUNDED truncation of the SECONDS temporary are
two different rules, and `SequentialIoEmitter.RenderRetry` keeps them as two renderings rather than one shared
helper — the contract's proposal to route `ALLOCATE`'s float lane through the new ceiling helper was **refused**
here and recorded as a named exemption: `AllocateReal` fuses the ceiling with GR2's `≤0 ⇒ NULL` and
storage-not-available outcomes, so routing it would re-open the green PB151 landing (`feedback_one_mechanism_per_job`
cuts the other way when the two sites are not the same rule).

**`DELETE FILE … OVERRIDE` parsed and was then dropped.** PB196's filed premise — "the grammar is missing
OVERRIDE" — was **false on today's tree**; the grammar had it and the binder never carried it to the emitter, so
§14.9.10.4 GR18's attribute-check suppression silently did not happen. The carrier now runs end to end. This is
the third time this session a note's premise inverted under a re-probe, and it is why the contract items are
re-measured rather than inherited.

**Three I-O syntax rules were enforced in no mode at all.** `DELETE RECORD … INVALID KEY` under `ACCESS
SEQUENTIAL` (§14.9.10.3 SR2), `REWRITE … INVALID KEY` on sequential organization or relative-sequential
(§14.9.35.3 SR2), and `READ … ADVANCING/AT END/NEXT/NOT AT END/PREVIOUS` under `ACCESS RANDOM` (§14.9.30.3 SR6)
were all bound unconditionally with a *"tolerated in the default (CCVS-lenient) mode"* comment and **no strict
arm** — so at `--std 2023` strict the compiler accepted source the standard forbids and said nothing. ⛔ The
REWRITE rule is the two-arm-dispatch shape again: `SequentialIoBinder.BindRewrite` never called
`rewriteInvalidKeyPhrase()` **at all**, so the sequential-organization arm was not merely ungated, it was
unreachable from the parse tree. One reusable descriptor, **COBOLNET1720** (`io-phrase-forbidden-here`), serves
all three on the COBOLNET1694 precedent — the SHAPE is one rule and each site quotes its own SR — routed through
`EditionContext.Removed`, THE policy seam: ERROR under strict, WARNING with an UNCHANGED bind under
`--permissive`, so the CCVS-85 corpus that motivated the leniency keeps compiling with its existing semantics.
SR1 (DELETE RECORD on sequential *organization*) is deliberately NOT routed through that seam — it is a hard
error at every edition and strictness, because it is not a documented dialect leniency; it had been enforced but
never proved (`COBOLNET0865` occurred exactly once in the repository, at its own emit site), and a negative now
pins it.

**Indexed `DELETE RECORD` had never been executed by the suite**; eight legs (GR1/2/3/5/8/9/11) are green now.

**Edition-gate sweep RUN, not assumed.** All 459 NIST programs check-batched at strict `--std 85` produce the
same 17 pre-existing failures (`CM*` COMMUNICATION-SECTION parse errors and `DB*` COBOLNET1571 debug-module
rejections, each read individually) and zero new ones — after the harness was first proved able to surface
COBOLNET1720 as FAIL, so the zero means something. Two watchdogs were proved to fail before their silence was
trusted: `NumericRoundUpSiteDriftTests` (pointing `RetryTimes` back at `Align` ⇒ red **by name**) and the GR9 leg
(mutating `_fpiValid=false` ⇒ `IXNEXTST` 00→46, reverted byte-identical).

**The Annex A.1 documentation the fix finally made statable.** Three implementor-defined obligations had no
answer to give while the runtime manufactured `52`: **A.1-165** (the interval between RETRY attempts — it is
ZERO, and the ground is structural: every lock this implementation arbitrates is held by a file connector of the
*executing run unit*, which cannot release while another statement of that run unit runs, so no releaser can
execute between two attempts and a nonzero interval could only delay an identical answer), **A.1-166** (the
SECONDS internal representation `9(1)V9(0)`, maximum meaningful value 0, no attempts during the period — which
is why `RETRY FOR 0 SECONDS` and `RETRY FOR 30 SECONDS` agree by one rule rather than by a special case, and
that agreement is what the golden pins), and **A.1-109** (the conditions under which a deadlock is detected —
exactly one, a `RETRY FOREVER` on a record locked by another connector, where §14.7.9.3 GR3's *"until the
input-output operation has been completed"* can never terminate).

⚙ **Landing note — the three DOC rows were closed HERE, by the lander, not by the implementer.** Cluster E was
cut from a main on which an A.1 row could not carry a verdict at all; the DOC-row mechanism (`kb/Work/A11`,
`PB280`) landed two commits later, and §7 was re-keyed from bare item numbers to `DOC-A.1-<n>` with a `Pinned
by` cell. Rather than re-express E's three rows in the new key form and leave them GAP — the smallest diff that
merges — the determinations were pinned to the tests that actually measure them and the inventory rows verdicted,
because the documentation obligation and its evidence are one artifact (`audit_annex_a1.py`'s pin check exists
to stop them drifting apart). That is 3 of the 11 rows this landing closes.

**Registers.** PB142 · PB144 · PB196 → `landed`. **PB144-B was split out and is now [[PB293]]** (the
implementer allocated it as "PB278", which is main's two-lane owner decision — renamed here, with `kb/Work/PB144`
re-pointed): the `RECORD KEY … SOURCE IS` split-key form refuses ANONYMOUSLY, as a raw ANTLR syntax error naming
a token, where §A.3 item 40 makes the phrase processor-dependent and `CONFORMANCE.md` row 40 already records the
non-support. Its draft ended with a bare owner question — *provide the form, or stand pat?* — and that question
is **withdrawn**: under the owner-decision protocol a question is reserved only for a significant consideration,
and here the standard grants the standing-pat position, row 40 already claims it, and nothing in the conformance
claim turns on the answer. The debt is the NAMED refusal, and PB293 is `kind: analysis`, not a blocked defect.

**Docs.** `COBOLNET_FILES_DESIGN` gains **D8 + D9** — the deep-dive had **zero** occurrences of the word "RETRY"
before this; `COBOLNET_NUMERIC_DESIGN` records the ceiling-vs-truncation split and the `AllocateReal` exemption;
`CONFORMANCE.md` §7 gains the three A.1 rows in the new key form; `DIAGNOSTICS.md` regenerated.

**Verdicts.** 13 inventory rows re-verdicted (6 re-adjudicated: `GR-14.9.10.4-6` and `-15` DIVERGES → CONFORMS,
`-18` and `SR-14.9.10.3-2` NOT-IMPLEMENTED → CONFORMS, `-1` and `-3` PARTIAL → CONFORMS) plus the three DOC rows.
⚠ `2023/delete_file_sharing` was a **GREEN golden ratifying the divergence** (`DELFV=52`) and is corrected in the
same change set — a green test pinning a divergence cannot close its row.

**Gate (this worktree, on the rebased tree, full suites rather than the wave-local filter).**
Conformance `Passed! - Failed: 0, Passed: 5398, Skipped: 0, Total: 5398, Duration: 7 m 46 s`;
Unit `Failed! - Failed: 2, Passed: 5211, Skipped: 0, Total: 5213, Duration: 1 m 22 s`;
Characterization `Passed! - Failed: 0, Passed: 33, Skipped: 0, Total: 33, Duration: 1 s`.
Both Unit reds are `ExternalCorpusPopulationDriftTests`, each read: the GPL GnuCOBOL corpus is absent in a fresh
worktree (`POPULATION BAD … EXTERNAL POPULATION ABSENT`, `JSON {"baseline": 1323, "external": 0, "state":
"absent"}`) — the gate refusing to report a missing population as an empty one, which is exactly what PB209 built
it to do. `semgrep/verify.py` measured **identical** to the recorded PB175 baseline (`cobolnet-no-biginteger` 36,
`cobolnet-raw-diagnostic-code-literal` 475) — no increase. `work.py check` 361 items well-formed;
`audit_annex_a1.py --check` zero findings.

**Battery note.** Expect **zero** new GnuCOBOL differential agreements from this landing: `DELETE FILE` is a 2023
statement the oracle does not have. A differential that appeared to confirm the 52→62 change would mean something
else moved.

**GAP 3467 → 3456** (`2acbd842` → this commit). Next free diagnostic: **COBOLNET1721**.

