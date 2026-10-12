## Entry 1112 - 2026-07-29 21:40 PDT - Making the resume actually resume: the stalest line in §0 was the one about main

Owner asked that a cleared session pick up exactly here. That is a doc-only change, but auditing §0 against reality
turned up one line that would have actively misled a fresh session, plus three that had quietly inverted.

**THE DANGEROUS ONE.** §0 "Where we are" said: "`main` = `c056f1f4` — PHASE-14 IS MERGED (2026-07-28) … `phase-14`
is fully merged and its tree is identical to `main`." True when written; false the moment this wave started. A
cleared session would have believed the tree was merged and clean against main while sitting on **17 unmerged
commits**. Replaced with the measured state: `phase-14` = `ccfc091d`, `main` = `0e534dc7`, 17 commits ahead,
DEVLOG 1095–1111, unmerged, and the merge is an owner decision. The old §284 merge-gate record is kept but
date-stamped as HISTORICAL with a forward pointer, since it is legitimate history and only reads wrong as state.

**THREE THAT HAD INVERTED, all the same class: a doc describing an intention that has since been fulfilled.**
· NEXT item 1 was the V59 execution record — seven steps of a finished wave sitting at the top of a worklist. §0
  states WHERE WE ARE, never how we got here, so it is gone and its reasoning lives in DEVLOG 1095–1102. The list
  now OPENS with what is actually next: the P14 Step-0 traceability inventory, 3,790 rows still at full GAP, which
  is the real road to v1.0 and had been sitting at item 5.
· "THE WORK = the spec-first CONFORMANCE FIX-QUEUE" — the queue now has ZERO open items, so that sentence pointed a
  new session at an empty file. Rewritten to say it is exhausted, to keep it as the register for NEW discoveries,
  and to name the inventory as the actual source of work.
· `DOC_INDEX.md` still advertised "30 landed / 16 remain as of 2026-07-23" beside the correct advice to trust the
  doc's own header. An inline count that contradicts the header it defers to is worse than no count.

**AND A HEADING THAT CONTRADICTED ITS OWN BODY.** DA6's queue heading read "⏳ OPEN" while the bullets beneath it
recorded it as landed — I had updated the body and missed the line above it. In a register the project treats as
live-state SSOT that is a defect, not untidiness: a session reads the heading first. The queue now has zero "OPEN"
strings in it, which is a greppable invariant rather than a claim.

**TWO MEMORIES, because the wave's lessons are durable and not repo-specific.**
· `one_rule_one_place` — when a single construct fails, grep for the RULE, not the construct. DA3 (three copies of
  a literal dispatch), DA5 (two predicates for one question) and DA6 (one rule at a context-free site) were all
  reported as one construct failing. Includes the tell I nearly missed: patching two of three copies and finding it
  STILL broken is the signal, not a puzzle.
· `green_test_can_hold_a_gap_open` — a RED test is a bug report; a GREEN test pinning a loud-stage reads as a
  DECISION and nothing questions it. Carries the two related traps from the same day: a fan-out whose zero result
  could not distinguish "nothing wrong" from "nothing ran", and a hand-built probe set that inherited my framing
  (every DA6 probe was arithmetic, none was a string intrinsic; the corpus found 79).

**Verification, since a resume doc that is merely plausible is worthless.** Grepped for every stale claim by
pattern (`c056f1f4`, "identical to main", "V59 — the LAST item", "implementation is NOT started", "1 REMAIN",
"⏳ OPEN") — all clean or date-stamped; re-read §0 in bootstrap order as a cleared session would; and ran the
registry/VCR/predicate drift guards (10/10) since the doc edits touch guarded ledgers.

**Gates.** Doc-only change; drift guards 10/10. The code battery is unchanged from `ccfc091d`: Conformance
4137/4137, Unit 942/942, characterization 33/33, guard-fast ALL GREEN with NIST 353 MATCH / 0 REGRESSION, GnuCOBOL
differential 0 flips — every figure measured at that commit, none carried.

