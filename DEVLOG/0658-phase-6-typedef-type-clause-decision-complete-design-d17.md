## Entry 658 — 2026-07-07 03:50 PDT — Phase 6: TYPEDEF / TYPE clause — decision-complete design D17 into the deep-dive (recon wf_5dd41937-6d8)

The next Phase-6 feature. A background recon (wf_5dd41937-6d8: 4 parallel readers — spec / grammar+gate / data-model /
design-doc — → an xhigh synthesis, 350k tok) produced a decision-complete plan, now committed into
`docs/COBOLNET_DATA_MODEL_DESIGN.md` as **D17** (per `feedback_plans_in_repo` — decision-complete plans go into the
canonical deep-dive at plan time, not just a brief). No code yet — this is the design commit.

**Two recon findings that shaped the design.** (1) The prompt's "data-model D5" was a MISNOMER — D5 in that doc is
REDEFINES; there was NO prior TYPEDEF design at ANY altitude (SSOT silent, only an M3-2 checkbox). So this is net-new,
allocated the next GLOBAL decision number D17 (D16 = floats). (2) TYPE/TYPEDEF is a **FRONT-END + BINDER-ONLY** feature
with **ZERO emitter change**: a weak TYPE reference is pure macro-expansion (the cloned subtree emits as a hand-written
group would, §8.5.3.2 NOTE); a STRONG type is stored identically and adds only compile-time checks (§8.5.3.3).

**The design (full text in D17).** A `TypeDecls` template registry (the TYPEDEF entry allocates no storage, its
subordinate names not globally visible — §13.18.58.4 GR2/GR1); a recursive `CloneSubtree` (generalizes the flat
`CreateCompilerTemp`, fresh `Uid` per node since `StructName`/`ProfileName` ride on it) spliced into the forest by an
`ExpandType` in `BindEntries` BEFORE `BindResolve` (so every post-build pass sees the clone automatically); STRONG
same-type gating at MOVE/compare/class-condition. ONE shared-.g4 change (a `STRONG` token + `typedefClause`, the
TYPE-reference rule already exists at `CobolData.g4:253`) — inc 1 only, FULL legacy guard. **SAME AS is DEFERRED** (a
distinct feature; a hard `AS` keyword is a legacy-compat hazard; `CloneSubtree` is built generically for its later
reuse). Also FIXES a current silent-drop bug (at 2002+ `TYPE IS name` parses and is dropped — no binder branch).
Diagnostics 1529–1535. Four increments: (1) grammar + weak-TYPE spine [the only legacy-guard slice] · (2) STRONG
typing · (3) level-88s in a TYPEDEF · (4) staged-loud residue. NEXT: inc 1.

