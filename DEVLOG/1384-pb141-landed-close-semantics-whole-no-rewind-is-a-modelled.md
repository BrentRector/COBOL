## Entry 1384 — 2026-08-29 08:56 PDT — PB141 LANDED: CLOSE semantics whole — NO REWIND is a modelled kind with its '07', the REEL≡UNIT equivalence holds in every consumer, EC-REPORT-NOT-TERMINATED is raised at last, RESUME transfers on the plain I-O path, and DELETE FILE's ON EXCEPTION no longer swallows the EC

Six mechanisms (kb/Work PB141). CLOSE MODEL: BoundCloseKind.NoRewind + FileRegistry.CloseNoRewind — Table
14's Non-unit × NO-REWIND cell c,g performed exactly (the file closes AND a successful close reports '07',
§9.1.13.2 item 6); FOR REMOVAL stays folded into ReelUnit ON A DERIVATION (both UNIT rows share symbol e
on non-unit media — the binder comment carries it). The FLAG-02 predicate gains REEL (§14.9.6.3 SR2 makes
REEL and UNIT one phrase — the old comment elected to break the equivalence in a second consumer of the
parse node). §14.9.6.4 GR5: EmitClose guards a report file's close on ReportWriter.IsActive under the
statement's >>TURN (BoundClose.ReportNotTerminatedCheck) — the EC was catalogued and raised NOWHERE. THE
DISPATCH HALves: the plain __IoCheck returns the declarative's RESUME action under the EC model and every
verb site consumes it (the void call discarded it — RESUME AT procedure-name never transferred on the
non-EC-masked path of ANY I-O verb; pb141_resume_io pins HANDLER=35/RECOVERED); and EmitDeleteFile always
runs the USE hook with the new onExceptionHandled suppression (the third flag beside atEnd/invKey inside
__IoCheckEc), so GR20 b)'s EC sets while the phrase handles — plus the DEEPER half, DELETE FILE was the
ONE I-O statement missing from EcBinder's QueryFor enumeration, so no (EC, file) mask ever reached
__IoCheckEc for it. THE STANDARD DEFECT: DELETE FILE's GR20/GR21 BOTH open 'is successful' in the printed
standard (PDF page 638 text layer, not a transcription artifact) yet prescribe contradictory ON EXCEPTION
outcomes — adjudicated in docs/CONFORMANCE.md: GR20 is read as the unsuccessful-execution rule (its d)
invokes §9.1.12).

Verdicts: GR-14.9.6.4-3/-L2.2 and SR-14.9.6.3-2 and GR-14.9.10.4-20 DIVERGES → CONFORMS, GR-14.9.6.4-4
PARTIAL → CONFORMS (all three wrong CLOSE values now closed across PB140+PB141), GR-14.9.6.4-5
NOT-IMPLEMENTED → CONFORMS, FMT-14.9.6.2 PARTIAL → CONFORMS. Goldens pb141_close_forms /
pb141_delete_file_ec / pb141_resume_io / pb141_report_not_terminated; the FLAG-02 theory covers all three
phrase spellings.

