*>--- conformance-case
*> id: pb347-release-refmod-record
*> rules: [SR-14.9.32.3-1, FMT-14.9.32.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.32.3 syntax rule 1: "Record-name-1 shall be the name of a logical record
*> in a sort-merge file description entry and it may be qualified."
*> A record-name is a user-defined word (§8.3.2.2.25) and §5.2.4 gives that operand type "User-defined
*> word, including qualification and subscripting if needed" - those two decorations and no other.
*> §8.4.3.3.3 SR5 permits reference modification only "anywhere an identifier referencing a data item
*> of class alphanumeric, boolean, or national is permitted", and its NOTE draws the consequence:
*> "where data-name-n is used in a general format or syntax rule, then reference-modification is not
*> permitted". The printed general format writes record-name-1, not identifier-1.
*> Accepted, RELEASE SRT-REC(1:3) put a 3-byte record into an 8-byte sort file.
*> §4.2.2 makes the compile-time indication mandatory for "violations of the general formats and the
*> explicit syntax rules of standard COBOL". The rule is written identically in 1985, 2002 and 2014, so
*> there is no edition gate and every edition rejects. COBOLNET1757 (kb/Work PB347).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB347N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "pb347n2.tmp".
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
           RELEASE SRT-REC(1:3).
       OUT-PROC.
           PERFORM UNTIL WS-EOF = "Y"
               RETURN SRTF
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "R=[" SRT-REC "]"
               END-RETURN
           END-PERFORM.
