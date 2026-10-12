## Entry 1744 — 2026-09-27 18:38 PDT — The fix lane is a rolling wave, with same-file successors

**What.** The fix lane is now dispatched through one standard workflow, `.claude/skills/workstream/templates/wf_rolling_wave.js`:
- **Rolling pool.** N workers pull groups from one queue, so a freed implementer slot is refilled the moment its agent returns.
- **Trains as branches finish.** A lander train starts at `train_size` ready branches (trains are serialized; a final remainder of at least `min_final_train` gets a train).
- **Same-file successors.** A group may declare `after: '<letter>'`. It then waits for that predecessor, merges its branch, and orients from the predecessor's report instead of re-surveying the file. The predecessor's branch is held from the trains and lands through the successor.

It replaces the barrier pattern (`wave-and-train`: every implementer, then one train), in which a finished slot sat idle behind the slowest group of its wave.

**Why.** Owner, 2026-09-27: "fix related work together … minimize repetitive context gathering", and "update all appropriate skills such that we use this new rolling functionality as it reduces context and token use".
- Orientation was ~46 % of every implementer's tokens in waves 45–57.
- `fix_clusters.py` caps a cluster at five notes because of the quadratic cost law. So a file with more notes (ReportWriter.cs 9, DataBinder.cs 10) became two groups that each oriented from scratch.
- A successor is a FRESH agent (the cheap part of the curve) that inherits the file's learned context. It is not a longer transcript.

**Tooling fixed on the way.**
- `make_dispatch_specs.py` named spec files by the slug's LAST character, so the successors V2 and U2 both wrote `msg-w68-2.txt`. Spec files are now keyed on the full letter, which is the name the workflow opens.
- `check_practices.py` accepts successor report slugs (`w68v2`).
- `check_practices.py <groups.json>` now REFUSES two groups whose `root` names the same primary `src/` file unless one reaches the other through `after:` (MANDATORY-PRACTICES O2). Tested both ways: RED on a probe with U2's `after` removed ("U+U2 on src/Cobol.Net.Compiler/Binding/DataBinder.cs"), GREEN on wave 68's twelve groups.
- `wf_rolling_wave.js` is itself checked: STOP, role agentTypes, the final-StructuredOutput reminder, successor handling and push-main.

**Docs.** workstream SKILL §2 (the rolling wave, its args, the successor rule and its measured basis) and §3; MANDATORY-PRACTICES O2; docs/DOC_INDEX.md. The public claude-skills agent-fleet skill carries the generic form (v1.6.0).

**First use.** Wave 68 (workflow wf_bf025bee-c67): twelve groups, including successors V2 (after V, ReportWriter.cs), V3 (after V2, ReportWriterEmitter.cs) and U2 (after U, DataBinder.cs). It was relaunched once to add the successors. The first launch's work (R/S/T predecessor merges, U's PB1088) was preserved on its branches and resumed, not redone.

