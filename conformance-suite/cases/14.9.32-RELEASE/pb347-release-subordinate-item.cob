*>--- conformance-case
*> id: pb347-release-subordinate-item
*> rules: [SR-14.9.32.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.32.3 syntax rule 1: "Record-name-1 shall be the name of a logical record
*> in a sort-merge file description entry and it may be qualified."
*> A 05 item subordinate to a record is not itself a logical record, so it is not a record-name-1.
*> Accepted, the compiler released SR-DATA's 5-byte image space-extended to the SD's 8 bytes: a record
*> the program never released, injected into the sorted result.
*> §4.2.2 makes the compile-time indication mandatory for "violations of the general formats and the
*> explicit syntax rules of standard COBOL". The rule is written identically in 1985, 2002 and 2014, so
*> there is no edition gate and every edition rejects. COBOLNET1757 (kb/Work PB347).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB347N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "pb347n1.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  SRTF.
       01  SRT-REC.
           05  SR-KEY   PIC X(3).
           05  SR-DATA  PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SRTF ASCENDING KEY SR-KEY
                INPUT PROCEDURE IS IN-PROC
                OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC.
           MOVE "AAA" TO SR-KEY.
           MOVE "aaaaa" TO SR-DATA.
           RELEASE SR-DATA.
       OUT-PROC.
           PERFORM UNTIL WS-EOF = "Y"
               RETURN SRTF
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "R=[" SRT-REC "]"
               END-RETURN
           END-PERFORM.
