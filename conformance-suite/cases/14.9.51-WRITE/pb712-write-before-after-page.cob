*>--- conformance-case
*> id: pb712-write-before-after-page
*> rules: [SR-14.9.51.3-17]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.51.3 SR17: "The BEFORE and AFTER phrases shall not both be specified if the PAGE phrase is
*> specified." The pair of words is otherwise legal from COBOL-2023 (Annex §E.3.3 item 2) and §14.9.51.4
*> GR25 f) gives it a defined advance; PAGE is the one operand it cannot carry, because GR25 g) and h)
*> position the record "before or after (depending on the phrase used) the device is repositioned to the
*> next logical page" and with both words written there is no phrase to depend on.
*> Rejected at 2023 ONLY — below 2023 the pair itself does not exist, and the introduction gate
*> (COBOLNET0900, construct write-before-and-after-advancing-2023) answers first with a different code.
*> Its own edition arms are the version-matrix construct row; kb/Work PB712.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB712NEG2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb712neg2.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 4 LINES.
       01 P-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           MOVE "AAAA" TO P-REC.
           WRITE P-REC BEFORE AFTER ADVANCING PAGE.
           CLOSE LPF.
           STOP RUN.
