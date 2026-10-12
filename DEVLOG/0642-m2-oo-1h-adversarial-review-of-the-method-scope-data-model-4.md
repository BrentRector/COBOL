## Entry 642 — 2026-07-07 01:05 PDT — M2-OO-1h: adversarial review of the method-scope data model — 4 REDEFINES defects FIXED (5th deferred, pre-existing)

Ran a find→verify review workflow (wf_8b9a8453-7f8, 3 reviewers) over the four M2-OO-1h commits (637-640). 7
findings, 6 confirmed; four are genuine M2-OO-1h REDEFINES regressions (rejected→miscompiled/mis-accepted), FIXED
here; the 5th is a pre-existing shared bug deferred to its own change set; the 7th refuted.

- **[HIGH] LINKAGE Tier-B canonical as a USING/RETURNING formal → CS0103.** A method Tier-B REDEFINES canonical's
  storage IS its string backing (`_redef_X`), not the suppressed root struct — but the BY REFERENCE copy-out
  (§14.2.3 GR8) and RETURNING (§14.9.23.4 GR8) emitted `{param} = {root.CsName}` / `return {root.CsName}`, naming
  an undeclared local → the backend compile failed on a legal program (a regression from the old loud reject).
  Fixed: both gate on `MethodRedefinesBackingDecl` and write/return the backing. Golden
  `oo_method_redefines_linkage` (NUM 9(4) redefined by NCHARS X(4) as a USING formal; the MOVE through NCHARS
  copies back through the BY REFERENCE boundary → ARG=0042).
- **[MED] Cross-section REDEFINES accepted.** The top-level method-redefiner scope UNIONed WS+LOCAL+LINKAGE, so a
  LOCAL-STORAGE 01 could redefine a WORKING-STORAGE 01 (illegal — §13.18.44.3 SR same-data-description; and their
  storage classes differ: static WS vs per-activation LOCAL). Fixed: scope to the redefiner's OWN section only →
  COBOLNET1518. Unit `MethodRedefines_CrossSection_Rejected`.
- **[LOW] Tier-A method view root emitted a dead local** — the method emit loops declared a local for every
  LOCAL/LINKAGE root except a Tier-B canonical, but didn't replicate BuildPhysicals' Tier-A-view suppression.
  Fixed: skip a non-canonical Alias view in both loops.
- **[LOW] LINKAGE Tier-B backing seed not width-normalized** — the formal seed assigned the raw param string; a
  redefiner wider than its target left the backing short. Fixed: `CobolString.Store(param, class.Width)`.

Deferred (its own commit): **[MED, PRE-EXISTING] `ImageInitOf` seeds a Tier-B-over-OCCURS backing with ONE
occurrence** (should repeat all, §13.18.63 GR9) — a shared FieldEmitter path that also reproduces in program
scope, so it gets a separate change set with a full-battery check. Battery: 199 (corpus + OoSpine) GREEN; full
battery running.

