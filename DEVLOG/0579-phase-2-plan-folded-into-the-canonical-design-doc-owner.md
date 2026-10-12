## Entry 579 — 2026-06-11 13:42 PDT — Phase-2 plan folded INTO the canonical design doc (owner-directed durability fix)

**Owner question after the 578 checkpoint: "How will a new session know to look in E:\tmp\g7? Why isn't
resume-prompt.md used?" — answered (resume-prompt.md IS the pointer chain's head) but the underlying concern is
right: the decision-complete plan lived ONLY off-repo (unversioned, single-machine).** Fix, owner-approved
("Say the word"): the durable decisions from `/e/tmp/g7/RESUME-G7-PHASE2.md` are now a first-class
**"Phase-2 implementation plan" section in `docs/VERSION_TEST_MATRIX_DESIGN.md`** (after its §8): P2.1
EditionContext channels + Permissive axis + the `Removed()` policy seam; P2.2 the visitor hook (no generated
listener — `CobolParserCoreBaseVisitor<object?>`, fail-fast pre-Emit); P2.3 the COBOLNET0900–0903 band (0873/
0810/0811/0882 kept); P2.4 the cobolWord-funnel reserved check + the scripted §8.9 table generation (OCR fixes,
METHOD omission, RECEIVE/SEND re-reservation, conservative-confidence rejection) + the agent-content-filter
operational rule; P2.5 ConstructDialectStatus registry + both drift tests; P2.6 the Wave-1 construct checklist
with grammar refs + VCR rows (incl. the DataBinder SD-0873 migration and the Error→Removed binder migrations);
P2.7 harness changes + the NIST-continuity→permissive consequence + the reserved-word interval-encoding matrix
rows; P2.8 Waves 2–3 (MOVE rows, the two latent binder bugs, XOR/EXCLUSIVE-OR, notInGrammar 85-acceptance,
preprocessor rows). Also fixed the doc's STALE status banner (still said "Phases 1–3 ahead" though Phase 1
landed at DEVLOG 531). `/e/tmp/g7/` artifacts remain as convenience copies; the doc section is SSOT on
divergence. resume-prompt.md STATE banner repointed at the doc section. Process note: this is rule-#4
(keep deep-dives current) applied at PLAN time rather than deferred to implementation time — the right call
whenever the plan is the only artifact a session-loss would destroy.

