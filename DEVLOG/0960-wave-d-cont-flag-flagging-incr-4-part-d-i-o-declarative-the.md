## Entry 960 — 2026-07-21 19:30 PDT — Wave D (cont.): FLAG flagging Incr 4 (part) — d I-O-DECLARATIVE (the statement→file→open-mode→declarative join)

**Detector 16 of 19.** FLAG-14 **d I-O-DECLARATIVE** (§7.3.15.4 GR4 d; E.2 item 19) — the most complex FLAG
detector. Two rules: **(1)** an INVALID-KEY-capable I-O statement (WRITE / REWRITE / DELETE / START / random READ on
a **keyed** — RELATIVE or INDEXED — file) written WITHOUT its INVALID KEY phrase, when the unit has ANY open-mode USE
declarative (INPUT/OUTPUT/I-O/EXTEND); **(2)** an AT-END-capable READ (a sequential retrieval) written WITHOUT its AT
END phrase, when the unit has an INPUT or I-O declarative. E.2 item 19 confirms both: at 2023 the declarative "will
now be executed" on the exception.

**Machinery.** Per-unit USE-declarative modes read from the BOUND model (order-independent): `VisitProgramUnit` sets
`_hasAnyModeDecl` / `_hasReadModeDecl` from `unit.Bound.Declaratives` (`BoundDeclarative.ModeIndex` = the
`FileOpenMode` ordinal; null for a file-name-targeted or Format-3/4 declarative, which GR4 d does not consider).
Per-statement file classification from `_currentData.Files` (source names, pre-renaming): `IsKeyed` = Organization
RELATIVE/INDEXED; a READ is sequential-retrieval when it has a NEXT/PREVIOUS direction OR the file's AccessMode is
Sequential (so a dynamic keyed READ with no direction is the random/INVALID-KEY arm). WRITE/REWRITE map their record
name to its file via `FileModel.Records`. New overrides `VisitRewrite/Delete/StartStatement`; the existing
`VisitRead/WriteStatement` extended. A file that does not resolve stays unflagged (no organization ⇒ neither branch).

**CLI-probed all cases**: ✅ WRITE/REWRITE/DELETE/START on a keyed file, no INVALID KEY, USE ON I-O → flagged ·
✅ READ NEXT (no AT END) + USE ON INPUT → flagged (rule 2) · ✅ random READ (no INVALID KEY) → flagged (rule 1) ·
✅ WRITE INVALID KEY / START INVALID KEY / READ … AT END → NOT · ✅ SEQUENTIAL-file WRITE (not keyed) → NOT ·
✅ no declarative → NOT · ✅ READ NEXT with a USE ON OUTPUT-only declarative → NOT (rule 2 needs INPUT/I-O) ·
✅ directive OFF / no directive → NOT. 7 unit tests added (`FlagDirectiveTests` 61→68). Wave-local gate green: fresh
build + FLAG 68/68 + characterization **33/33 byte-exact**. Detectors done **16 of 19**; the LAST FLAG option =
FLAG-02 b EC-PROGRAM-EXCEPTIONS.

