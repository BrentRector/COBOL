## Entry 1927 — 2026-10-07 19:49 PDT — R1 approved: §8 Draft 10 of the architecture review is the target architecture (PB2118); nine refuter rounds; leads PB2425–PB2431

At 17:42 PDT the owner was asked "Is §8 Draft 10 approved as the target architecture (R1)?" and answered "Yes,
approve and land". This landing records that answer and lands the branch `r1-pb2118` (wave 1031) as one commit. With
it, `docs/rearchitecture/DESIGN-architecture-review.md` §8 stops being a draft and becomes the design that every R2–R5
wave executes against. PB2118 is `landed`.

**What §8 decides.** It covers the layers and their allowed edges, written as data that a drift test (PB2417, the
program's first wave) will enforce, with every non-allowed pair seeded and given a remover. It sets the layout and
naming rules, including the four namespace-root flips the owner allowed (question 1). It decomposes each of the 52
census god classes into designed extraction steps (Extract notes PB2296–PB2347; steps, removers and flips PB2352–PB2421).
It also covers the grammar regroup behind the PB2350 differential, the closed hierarchies that become C# 15 unions,
and the shape of the wave plan. The operating rules come from §8.7 and the owner's fifth question:
- **The file-set partition is the only gate** between restructuring waves and fix-lane trains. A wave declares its
  file set, the dispatcher checks that set against in-flight work, and a collision defers the wave, never the train.
  Every other gate is named: PB2417 first, PB2350 before grammar waves, open external-repository slices, PB2151 and
  PB2129 where a note's sites need them, and v1.0 for Cut 3.
- **File sets are computed, not hand-listed.** `scripts/arch/member_index.py` runs over the census's Roslyn walk of
  the current tree and computes a note's sites, the folders those sites sit in, and the callers and emitters of what
  the note moves or changes (Drafts 8–9).
- **The planner admits and the landing check guarantees.** `scripts/orchestrator/landing_check.py` runs inside
  `push-main.sh` and stops a landing (exit 3) when the landing's actual diff leaves its declared set or meets an
  earlier in-flight branch. Draft 10 reads every diff with `--no-renames`, so a moved file counts at both paths. It
  identifies an in-flight branch's work by its dispatch, not by its commit wording, and a landing's own members by
  the train manifest and by content. Its self-test runs 20/20 and `test_plan_wave.py` runs 99/99.
- **The planner's numbers.** On approval, `plan_wave.py --cluster PB2119 --dry-run` plans 68 groups in 14 trains over
  two planner waves between trains, with PB2417 first and 9 groups holding Extract notes. The binding and
  code-generation waves wait on the open external-repository slices. With every gate clear, the whole campaign takes
  six planner waves. R2's findings will set R3's real size, so the estimate leaves them out by design.

**How it got here.** Mythos 5.1 wrote Draft 1, its single dispatch under the owner's per-dispatch approval (R69 §7).
Opus then refuted the draft nine times and revised it nine times. Blocking + non-blocking findings per round:
13 + 4, 10 + 3, 6 + 3, 4 + 0, 3 + 3, 3 + 1, 1 + 2, 1 + 1, then 3 + 5. The findings moved from the design's substance
(layers, decomposition, the census claims) to the mechanics of the partition and finally to the landing check alone.
The ninth round's eight findings (renames, identity by commit wording, a landing stopped by its own implementer;
N1–N5) were all answered in Draft 10. The owner answered five questions during the drafts: (1) the namespace roots
become the project names; (2)–(4) the runtime I-O, report-writer, preprocessor and parser-superclass extracts, the CLI
root flip, and the runtime control and I-O folder moves and extracts may run between trains; (5) "Drop it": R69 §2's
GAP-near-zero hold on binding and code generation, and R64's zero-GAP hold on `tests/`, are gone. No tenth refuter
ran.

**Recorded on the branch.** PB2118 has an "Approved" section and is `landed`. R69 §7 gained the dated approval. The
design doc's top status, §2's paragraph, §3's R1 (Status: DONE), §8's status line and §8.7's gate list now state the
approval. Plan §0's R1 line and the `architecture-review` skill's status were updated the same way. PB2350, PB2351 and
PB2417 had been blocked only by PB2118 and are now `blocked: false` (`work.py check` caught the stale flags). The note
generator in `scripts/arch/file_census_notes.py` no longer writes "§8 binds nothing until the owner approves it". The
ledger's lanes read R1 from the register, and `ledger_plan.py --self-test` passes 11 cases.
`docs/rearchitecture/evidence/ledger-trend.json` gained four points that existed only as an uncommitted render in
main's checkout: GAP points e68c3365, 24a007fe and 42a68e20, and program point 37f2b131.

**Leads filed (PB2425–PB2430).** PB2425: `compare_oracle.py` records no result, so the ledger shows PB2152's status
instead of the latest comparison. PB2426: the ledger render dirties the tracked trend file in main's checkout. PB2427:
wave numbers have no allocator (`alloc.py wave`). PB2428–PB2430: the three landing-check residuals Draft 10 named for
a tenth refuter: identity by declared-set overlap, a landing's own notes read from its subjects, and containment that
sees only committed blobs. Two leads were checked and found already fixed, so they were not refiled: `gen_ledger.py`'s
default `--out` and the `claude-mythos-5-1` literal.

**The first push went red, and the local gates should have caught it.** On the first push, CI's `audits` job failed
in `work.py check`. PB2197, a Delete note the census filed, names
`src/Cobol.Net.Frontend/Generated/CobolParserCore.cs`, and the branch's R3 rule ("every path an open note names
exists") tested that path with `Path.exists()`. The file is the ANTLR build output. It exists in every built checkout,
including the Linux gate's clone, and in no fresh CI checkout. The lander gate and the Linux gate were both GREEN,
which breaks the CI invariant. The fix is at the root. `work.py` now reads existence from the git index
(`tracked_paths`, read once per check), so a built tree and CI's decide alike. A path under `Generated/` counts as a
build output: it is never "missing", and the note must also name the `.g4` it is generated from
(`build_output_problems`). PB2197 now names its grammar files (`Core/CobolLexer.g4`, `CobolParserCore.g4`,
`Core/CobolControlFlow.g4`), states that the lexer's channel and skip tokens are not dead, and waits on PB2350 like
every other grammar wave. `test_plan_wave.py` gained four witnesses (109/109), and with the old filesystem test the
two that plant a generated or untracked path fail. `file_census_notes.py --self-test` now stages its scratch tree in
git. §8.7's register paragraph in the design doc states the rule. A sweep of every script in CI's `audits` job and `scripts/arch` for the same working-tree read found two more. `audit_evidence_supersession.py`'s `path` arm had the identical defect. `work.py`'s `slice_file_set` counted untracked `.cs` files and any file a glob matched on disk. Both now read the index (`test_plan_wave.py` 111/111). The record is in PB1957 (dated 2026-10-07 18:36 PDT), and so is the second cause: CI's `audits` job has no local leg at all. That gap is filed as PB2431, an `audits` leg for `linux-gate.sh` on a fresh unbuilt clone.

Rebased a second time onto train 1036 (DEVLOG 1926): trains 1034b and 1036 landed while the first rebase was gated, so this entry, numbered 1925 on the branch, is 1927. The conflicts kept both sides: plan §0 keeps main's live-state line and the branch's R1 NEXT line, the external-repository design keeps the branch's `ActivationDescriptions.Of` row and main's PB2464 row, and the ledger trend keeps main's points. After the rebase `work.py check` stopped on PB2173, an R3 delete wave on `OoBinder.cs`: train 1036's PB2464 row in §12 names `OoBinder` among the callers slice 7 changes when it deletes `PrototypeSignatureClasses`, so PB2173 now waits on PB2103 (slice 7) like PB2160.

