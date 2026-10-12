## Entry 644 — 2026-07-06 20:59 PDT — M2-OO-1i opens: recon RE-FRAMES the ticket (a method canNOT own ENV/FILE/WS), + inc 1 — method-placement diagnostics (COBOLNET1519)

Opened M2-OO-1i ("a method's own ENVIRONMENT DIVISION + FILE SECTION") with a recon workflow (wf_5d22beb6-140, 4
parallel readers → xhigh synthesis). It produced a **spec-grounded re-framing that inverts the ticket**, which I
then verified myself — every one of the 7 load-bearing citations read verbatim in the spec:

- **A method definition canNOT own an ENVIRONMENT DIVISION, a FILE SECTION, or a WORKING-STORAGE / REPORT / SCREEN
  SECTION.** §12.4.3 SR1 (:14920), §13.4.3 SR1 (:16184), §13.5.3 SR1 (:16461) each say those sections may appear
  *only in a factory or instance definition, not in a method definition*. A method may own **only** LOCAL-STORAGE
  (§13.6.3 SR1 :16513) + LINKAGE (§13.7.3 SR1). So the real M2-OO-1i construct is **the OBJECT/FACTORY paragraph's
  INPUT-OUTPUT + FILE division, referenceable from method bodies** (§11.7.4 GR5 :13281).
- **That binding is already wired.** `OoReparentClassData`/`OoReparentFactoryData` (CSharpEmitter.Oo.cs:144-164)
  reparent the object/factory env+data divisions into the synthetic unit `BindDeclarations` binds — that is *why*
  `data.Files.Count > 0` at the gates on :87-90 (OBJECT) / :108-111 (FACTORY). Method bodies bind through `cls.Data`,
  whose `FilesByName` already holds those files → OPEN/READ/WRITE resolve with zero new scoping. The recon's original
  "method-scoped file map + scope-aware resolver" idea was for a construct that cannot exist — discarded.
- **The only real gaps:** (a) the loud gates → proper diagnostics/emission; (b) connector REGISTRATION + connector-
  KEY namespacing for object/factory/EXTERNAL files (the class emit path never runs `__Activate`/`EmitFileRegistration`;
  the qualification loop at Call.cs:138-142 iterates *program units only*). GLOBAL on a factory/instance/method FD is
  a hard error (§13.18.27.3 SR4 :19038). Instance file-connector lifetime is per-object (§9.1.4 :11216 — implicit
  CLOSE at object deletion, GC-deferral NOTE :11218). The full decision-complete design (5 increments, per-branch
  golden plan, COBOLNET1519/1520) is in `docs/PHASE4_RECONCILIATION.md` §M2-OO-1i.

**Increment 1 (this commit) — method-placement diagnostics.** The two `DataBinder.OoBindMethodData` gates that
raised the catch-all "recognized but not yet implemented" `COBOLNET0899` for a method's ENVIRONMENT DIVISION and its
FILE/REPORT/SCREEN SECTION now raise a HARD **COBOLNET1519** ("a method definition shall not contain an X SECTION"),
split so the message names the offending section + its §. The construct is spec-forbidden, not unimplemented — the
old 0899 wording was wrong. Method WORKING-STORAGE keeps its existing edition-removal-window path (§13.5.3 SR1 via the
`method-working-storage-window` EditionValidator row); method-WS EXTERNAL/GLOBAL keeps its 0899 (a separate data-item
follow-up). Test: `OoSpineTests.MethodMayNotOwnEnvOrFileSection_1519` (4 theory cases — ENV/INPUT-OUTPUT, FILE, REPORT,
SCREEN, each → 1519). No emission change; object/factory files still gated (inc 3-5). Battery: **1963 conformance
(+4) · 216 unit GREEN**, zero regressions. 1519 is free (1518 last-used).

