      *>--- conformance-case
      *> id: a413_declined_phrase_positive_control
      *> rules: [SR-14.9.35.3-7, SR-14.9.35.3-8, SR-14.9.35.3-10, SR-14.9.35.3-11, SR-14.9.35.3-12, GR-14.9.35.4-9, SR-14.9.51.3-7, SR-14.9.51.3-8, SR-14.9.51.3-9, SR-14.9.51.3-10, GR-14.9.51.4-7, GR-14.9.51.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
*> THE POSITIVE CONTROL for the Annex A.4.8 / A.4.13 declines (COBOLNET1705 / COBOLNET1706).
*>
*> A refusal gate is only evidence if it also proves what it did NOT take down. A.4.1 NOTE 1 is the reason
*> there is anything left to prove: "The higher-level constructs or cross-referenced topics are not
*> optional" - only the `FILE file-name-1` ALTERNATIVE of `{ record-name-1 | FILE file-name-1 }` is
*> declined, while the WRITE (14.9.51) and REWRITE (14.9.35) statements themselves are mandatory, fully
*> supported surface. This program exercises the surviving arm end to end - WRITE record-name-1 FROM,
*> READ INTO, REWRITE record-name-1, CLOSE - and RUNS, byte-comparing its output.
*>
*> It also pins the OPTIONAL WORD `RECORD` of 14.9.35.2 (RENDERED, PDF p710: not underlined, therefore
*> optional). The grammar did not accept it at all until 2026-09-02, so `REWRITE F-REC RECORD` - legal at
*> every edition, on the mandatory arm - was rejected outright. It is written here twice, once with the
*> word and once without, because an optional word needs both spellings measured or half the fix is
*> invisible.
*>
*> Placed in the 85 corpus deliberately: every construct here is X3.23-1985 surface, so a green run at
*> --std 85 also proves the two new declines are keyed to the DECLINED spellings and not to the verbs.
IDENTIFICATION DIVISION.
PROGRAM-ID. A413PC9AL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IO ASSIGN TO "a413pc9al.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-IO.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-ONE PIC X(20) VALUE "FIRST".
01 WS-TWO PIC X(20) VALUE "SECOND".
01 WS-IN  PIC X(20).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F-IO.
    WRITE F-REC FROM WS-ONE.
    WRITE F-REC FROM WS-TWO.
    CLOSE F-IO.
    OPEN I-O F-IO.
    READ F-IO INTO WS-IN.
    DISPLAY WS-IN.
    MOVE "REWRITTEN-A" TO F-REC.
    REWRITE F-REC RECORD.
    READ F-IO INTO WS-IN.
    DISPLAY WS-IN.
    MOVE "REWRITTEN-B" TO F-REC.
    REWRITE F-REC.
    CLOSE F-IO.
    OPEN INPUT F-IO.
    READ F-IO INTO WS-IN.
    DISPLAY WS-IN.
    READ F-IO INTO WS-IN.
    DISPLAY WS-IN.
    CLOSE F-IO.
    STOP RUN.
