## Entry 1020 — 2026-07-26 14:15 PDT — Session-state hygiene: plan §0 cut from 690 lines to 123 (history → DEVLOG), CLAUDE.md de-duplicated, the memory index rebuilt, dead agent scaffolding deleted

No compiler change this entry — a maintenance pass on the files a session READS before it does anything, which had
quietly become the project's least accurate artifacts. The owner asked for it directly ("update all memories, session
saved state files … clear out over specificity"), and the trigger was a proof that the drift was already biting:
`session-probe.ps1` reports **next-free diagnostic COBOLNET1626**, while plan §0 — the doc that declares itself the
ONLY live-state SSOT — still said **1624**. A hand-maintained ledger of every diagnostic code ever shipped had lost a
race with a script that computes the same number in half a second.

**What was wrong.** §0 had grown to **690 lines**, of which roughly 550 were *narrative*: the P13 grammar batch, the
EC-seam batch, V45/V47/V52/V53, the Wave-D FLAG increments, the CC-in-COPY refactor — all landed, all merged to
`main`, all already written up in DEVLOG 933–1019. Three separate bands inside §0 were explicitly self-labelled
"⚠ HISTORICAL … superseded by the live state above", which is the doc admitting it had become a log. Worse, §0
restated the fix-queue's LANDED roll-call verbatim while simultaneously declaring "its LANDED header is the live
tally" — a direct violation of its own SINGLE-WRITE RULE, and exactly the class of duplication that produced the
stale-banner failures the rule was written to prevent.

**What changed.**
- **`docs/COBOLNET_REARCHITECTURE_PLAN.md` §0 → 123 lines** (3300 → 2733 for the whole file). It now carries only
  live state: where we are · NEXT in order (the EC-infra + OO super-batch, then CA14/V59, then P14 Step-0) · the
  gates (wave-local per commit, comprehensive per batch, the GnuCOBOL command + baseline, the mechanics that have
  burned us) · the standing facts (D13/D14, the effort model, the diagnostic-band RULE, the spec-diagram rule, the
  open GAPs, the two known-unenforced SRs). Every removed narrative line exists in DEVLOG; nothing live was dropped —
  the P14 GAPs (PICTURE-EDITING render, VALUE Format 2 multi-dim, national-key collating, `>>SOURCE FORMAT` at
  cobol85), the §13.18.63.4 non-assertion, and the OCCURS/RENAMES level-rule findings were all carried forward
  explicitly.
- **The diagnostic band is now a RULE, not a list.** §0 no longer enumerates which wave shipped which code — that is
  DEVLOG's job and `session-probe.ps1` computes the ceiling from both scans. What §0 keeps is the part a script
  cannot infer: the released/reserved mid-band holes (1550–1552 · 1589–1590 · 1598 · 1602/1603 · 1609 · 1613) and
  *why* each is a hole, plus the fixed anchors (0899/0900/0901/0903/1560) and the no-holes release rule.
- **`CLAUDE.md`** said "read plan §0 first" four times in four escalating emoji registers, and re-stated the mission,
  the design SSOT and the doc map in overlapping fragments. Rewritten to six numbered non-negotiable rules + a
  3-step session start + where-things-live, with no rule lost.
- **`scripts/session-probe.ps1`** was still asserting `phase-13-grammar-batch` as an expected branch, so it printed
  "⚠ UNEXPECTED BRANCH" on `phase-14` every run — a warning trained to be ignored. Now checks `phase-14`/`main`.
- **`claude/` and `agents/` (26 files) deleted.** A March-2026 pre-Claude-Code scaffold: 10 role agent YAMLs, a
  routing table, bootstrap prompts, and `claude/state/` (a 171 KB `modernization-ledger.json` plus session-state
  snapshots from March/May). It described an architecture we no longer have — one of the ten agents owns
  "ir-lowering", and the current design has NO lowered IR. Dead state that reads as instructions is worse than no
  state. Preserved in git history; the kb vault still documents the generator scripts as audit artifacts.

**Memory.** The index was a 90-line wall in which the single longest entry was a ~250-word paragraph of *live
project status* — branch, campaign, tally, owner directives — i.e. exactly the content that plan §0 owns and that
memory cannot keep current. Rebuilt as a scannable 131-line index: three anchors (the spec decides · §0 is the only
live state · typed-native only), then one line per memory grouped by function. 21 files moved to `memory/_archive/`
— completed legacy milestones (NIST/M2/drive-to-100/report-writer/WS-spec), superseded status snapshots
(`project_greenfield_state` 33 KB, `project_rearchitecture_plan` 62 KB — a stale copy of the repo's own plan), and
three that were actively WRONG or obsolete, notably `feedback_binder_no_ir` ("the Binder produces bound nodes,
Lowering turns them into IR, the CilEmitter turns IR into CIL") which describes the legacy pipeline, not this one.
`feedback_devlog` and `feedback_autonomous` were consolidated (absorbing devlog_per_commit/per_change and
no_agent_prompts) and normalized to the current frontmatter schema.

**The lesson, stated plainly:** a resume banner that also serves as a trophy case stops being a resume banner. §0's
own SINGLE-WRITE RULE was correct and was being violated by §0 itself — because *adding* to the top of a live-state
doc feels like diligence, and pruning feels like deletion. The counter-discipline is the one already written down
([[feedback_docs_current_state_only]]): every doc except DEVLOG describes the CURRENT compiler. If a §0 line explains
how we got somewhere rather than where we are, it belongs here.

