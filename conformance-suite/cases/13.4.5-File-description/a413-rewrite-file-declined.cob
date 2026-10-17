*>--- conformance-case
*> id: a413-rewrite-file-declined
*> rules: [SR-13.4.5.3-3, GR-13.18.13.4-4, SR-14.9.35.3-7,
*>    SR-14.9.35.3-8, SR-14.9.35.3-10, SR-14.9.35.3-11, SR-14.9.35.3-12,
*>    GR-14.9.35.4-9, FMT-14.9.35.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 Annex A.4.13 item 1) "REWRITE FILE file-name-1 (14.9.35)" - the REWRITE twin, refused by
*> name with COBOLNET1706 from the SAME binder site as its WRITE sibling. Before 2026-09-02 the two arms
*> diverged: WRITE FILE compiled and ran, REWRITE FILE compiled clean and deferred to a RUN-TIME
*> NotImplemented throw whose interpolated record name was EMPTY. One construct, two arms, two postures,
*> neither of them the documented one - which is why these two fixtures land as a PAIR.
*> The spelling also carries `RECORD`, which 14.9.35.2's printed general format (RENDERED, PDF p710) shows
*> as an OPTIONAL WORD (not underlined) and which this grammar did not accept at all until the same change:
*> `REWRITE record-name-1 RECORD FROM ...` on the MANDATORY arm was being rejected as legal source, an
*> under-accept independent of A.4.13 that the positive control now pins.
IDENTIFICATION DIVISION.
PROGRAM-ID. A413RWF9AL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IO ASSIGN TO "a413rwf9al.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-IO.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-REC PIC X(20) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN.
    OPEN I-O F-IO.
    REWRITE FILE F-IO RECORD FROM WS-REC.
    CLOSE F-IO.
    STOP RUN.
