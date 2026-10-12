## Entry 952 — 2026-07-21 15:25 PDT — Wave D (cont.): FLAG flagging Incr 1c — the crisp statement options m WRITE-END-OF-PAGE + FLAG-02 f TERMINATE-WITH-VARYING (parse-arm + resolved name lookups)

The two statement FLAG options with unambiguous predicates but a resolved-fact dependency — implemented on D2's
"name-lookup off the parse-anchored construct" basis (NOT a data-arm). `FlagConformancePass.Run` builds two
source-name lookups from `GroupBindContext.Units[].Data` before walking the tree: the WRITE targets (record- and
file-names) whose file has a `LINAGE` clause, and the report-names whose description carries a VARYING clause. The
lookups key on SOURCE names because the flag pass runs BEFORE the file-connector renaming (so `FileModel.CobolName`
is still the source name).

- **m WRITE-END-OF-PAGE (§7.3.15.4 GR4 m):** `VisitWriteStatement` flags a WRITE with no `writeAtEndOfPage` whose
  target is on a LINAGE file (a LINAGE clause is what "allows an END-OF-PAGE phrase," §14.9.51). recordName is a
  dataReference — unqualified in practice; a qualified WRITE record name is a documented rare false-negative.
- **f TERMINATE-WITH-VARYING (§7.3.14.4 GR4 f):** `VisitTerminateStatement` flags a TERMINATE (once) when any
  named report's description contains a VARYING clause (`ReportModel` groups→lines→fields→`Varyings`, §13.18.64).

**Gate.** Build 0W/0E · CLI-probed both (a WRITE-without-EOP on a LINAGE file → 1621, the AT-EOP form → none; a
TERMINATE of a VARYING report → 1620) · 25 FlagDirectiveTests (5 new: m on/EOP-negative/non-LINAGE-negative, f
on/no-VARYING-negative) · characterization 33/33 byte-exact. **Detectors done: 6 of 19** (READ-PREVIOUS, CLOSE
I-O-STATUS-07, NUM-ED-ZERO-FIGCONST, VALUE-ZERO, WRITE-END-OF-PAGE, TERMINATE-WITH-VARYING). NEXT = Incr 1b (j
VALUE-EDITING + k VALUE-FIG-CON-LENGTH — the subtle §13.18.63/§13.18.40 predicates, a dedicated derivation pass).

