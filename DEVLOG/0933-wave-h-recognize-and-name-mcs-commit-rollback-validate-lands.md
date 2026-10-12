## Entry 933 — 2026-07-20 12:05 PDT — Wave H (recognize-and-name MCS/COMMIT/ROLLBACK/VALIDATE) lands; and the C5 re-derivation from the CORRECTED figure finds a THIRD spec inversion

`main` merged and **CI-verified green** (the Release-config check the local Debug battery cannot give — DEVLOG
774's lesson). Then the P13 grammar batch resumed on `phase-13-grammar-batch`, position 1.

**WAVE H — the §4.2.6 ¶3 recognize-and-name band.** MCS (SEND/RECEIVE), COMMIT/ROLLBACK and VALIDATE are
facilities COBOL.NET does not implement — processor-dependent (§4.2.6, A.3 items 4/6-7) or optional-and-obsolete
(VALIDATE, §4.2.7/§4.2.13). §4.2.6 ¶3 makes the compile-time WARNING MECHANISM mandatory anyway, and
§14.6.13.1.1 licenses raising no EC for them, so the conforming posture is: accept, warn by a NAMED code, run
inert. Before this they were GENERIC parse errors — satisfying neither obligation. Now COBOLNET1578 (MCS) /
1579 (commit-rollback) / 1580 (VALIDATE), all warnings; the program compiles, runs, and the facilities are
no-ops (`START / COUNT=3 / END` prints around them).

Followed the DEVLOG-903 MECHANISM CORRECTION exactly: real lexer tokens (RECEIVE/SEND/VALIDATE/COMMIT/ROLLBACK
/MESSAGE/END-RECEIVE/END-SEND), all admitted to the `cobolWord` nameSlot funnel, and KEYWORD-TOKEN-LED
`statement` arms gated by a new `facilityWord()` predicate — never an IDENTIFIER-led arm (which poisons the
ALL(*) DFA). The operand tails are PARSED, not swallowed: the prior packet's `(~DOT)*` swallow would have eaten
END-IF, and both MCS formats carry their own imperative-statements. Split the packet's dead SEND gate
(`{facilityWord("RECEIVE")}?` guarding a `(RECEIVE|SEND)` rule — SEND never parsed) into two rules.

**The riskiest thing here was the NON-MONOTONIC reservation, and it came out clean.** These words are reserved
at different editions (RECEIVE/SEND/END-RECEIVE: 85, user-word 2002/2014, re-reserved 2023; COMMIT/ROLLBACK/
END-SEND: 2023 only; VALIDATE: user-word at 85; MESSAGE the reverse — reserved 85/2002, user-word 2023). Every
profile verified against §8.9 by CLI probe and pinned by negative fixtures: `01 RECEIVE` compiles at 2002/2014
and rejects (COBOLNET0901) at 2023; `01 VALIDATE` compiles at 85 and rejects at 2002. The hard tokens did not
break user-word use anywhere — the cobolWord funnel absorbed them, exactly the RAISE/RESUME/SCREEN precedent.

**One limitation, documented rather than hidden** (CONFORMANCE.md §1): a bare facility verb whose word is also a
legal user-name (COMMIT/ROLLBACK/VALIDATE) written as the FIRST statement of an `EVALUATE … WHEN` arm is absorbed
by the WHEN selection-object list, so no warning fires. Verified it is (a) LOUD at run time
(`NotImplementedCobolFeatureException`), not a silent wrong answer, and (b) PRE-EXISTING, not a Wave H
regression — identical at `--std 2014` where COMMIT is a user word and the Wave H arm does not fire at all.
RECEIVE/SEND are immune (FROM/TO cannot continue an object list). Registered as a P14 Step-0 GAP row; the fix is
a shared EVALUATE-grammar change, deliberately not bundled here.

Ships as one change set per the same-commit rule: grammar + binder + 3 catalog descriptors + 8 cobol-words rows
(regenerated) + the `facilityWord` helper + a positive inertness golden + two reservation negative fixtures +
manifests + the GreenfieldOnly exclusion (the legacy compiler has no Wave H — it still parse-fails these) +
regenerated DIAGNOSTICS.md. Diag band advanced 1578→**1581 next-free**; §0 corrected (it still said 1578) and
the full 1581-1616 reservation map recorded so the parallel-agent collision that produced the ~40-site scout
drift cannot recur.

**C5 PERFORM Format 3 — re-derived from the CORRECTED figure** (the old figure was one of the page-break
truncations, missing WHEN OTHER/WHEN COMMON/FINALLY/END-PERFORM; the prior packet designed against half a
statement). Four fresh derivations, each adversarially verified: ALL FOUR came back REJECTED or PARTIALLY
REFUTED, 48 defects. The synthesis then adjudicated against primary text and produced three things worth
recording:
- **A THIRD spec inversion** (two research passes, three now): `EXIT PERFORM` in a WHEN arm is LEGAL —
  §14.9.14.4 GR4 verbatim: *"If an EXIT PERFORM statement is specified in an exception-checking PERFORM
  statement, control passes to an implicit CONTINUE … immediately preceding the FINALLY phrase"* — the lowering
  derivation wanted to diagnose it. C5 is genuinely treacherous; the owner's D19 "adjudicate, don't defer" was
  the right call.
- **The alleged WHEN-arm grammar ambiguity DISSOLVES on the spec's own terms**: §14.5.2 defines an unterminated
  EVALUATE/SEARCH as a *conditional* statement, which therefore cannot be a constituent of imperative-statement-1
  — no disambiguation predicate warranted, just a legible diagnostic.
- **§14.9.28.4 GR17 licenses the one-mechanism fix directly**: *"The rules for determining a match are specified
  in General rules 3a to 3g of the USE statement"* — so the WHEN operand grammar REUSES the existing USE rules
  rather than forking a second operand grammar (the packet's worst defect). GO TO banned in a WHEN
  (§14.9.17.3 SR3), RESUME AT NEXT STATEMENT permitted (§14.9.33.3 SR1) — both directions confirmed.
C5 is NOT yet implementable — a 65 KB synthesized spec whose four inputs were all refuted needs a careful read
first. (Later corrected 2026-07-20: the rejected re-derivation JSON was DELETED and replaced by the
adversarially-verified `docs/rearchitecture/evidence/PHASE-13-c5-perform-format3-DESIGN.md`; the figure questions
resolved in `PHASE-13-c5-perform-format3-pdf-resolution.md`.)

**Process note:** the Wave H gate was interrupted by a session boundary (no verdict recorded). Did NOT assume it
passed — rebuilt clean, re-verified the behaviour on the fresh build, and re-ran the full comprehensive gate
before committing. A lost gate is a gate not run.

