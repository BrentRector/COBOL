*>--- conformance-case
*> id: pb511_external_as_name
*> rules: [SR-8.3.2.2-L3.2, GR-13.18.22.4-4, GR-13.18.22.4-5, FMT-13.4.5.2, FMT-13.16.2, FMT-13.18.22.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB511AS.
      *> kb/Work PB511 at the INTRODUCING EDITION of the phrase: the EXTERNAL clause's `AS literal-1` is a
      *> COBOL-2002 addition (X3.23-1985 has no AS phrase anywhere and AS is a user-definable word there),
      *> and it had NO GRAMMAR AT ALL -- `externalClause : IS? EXTERNAL` was the whole rule, so a conforming
      *> program that names its external items was `error COBOLNET0901: 'AS' is a reserved word` at 2002,
      *> 2014 and 2023 alike.  The rules this golden pins:
      *>   13.18.22.2 general format (rendered, PDF p430/folio 400):
      *>                "IS EXTERNAL [ AS literal-1 ]"  -- IS bare, EXTERNAL and AS underlined.
      *>   13.4.5.2 Formats 1/2/3 and 13.16.2 Format 1 both print the slot as
      *>                "[ IS EXTERNAL [ AS literal-1 ] ]" -- so the phrase rides the FILE DESCRIPTION entry
      *>                and the DATA DESCRIPTION entry alike; both arms are exercised below.
      *>   13.18.22.4 GR5 "Literal-1, if specified, is the name of the file connector or record that is
      *>                externalized to the operating environment.  If literal-1 is not specified, the
      *>                externalized name of the file connector or record is the name specified in the file
      *>                description entry or the data-name format of the entry-name clause, respectively."
      *>   13.18.22.4 GR1/GR4b -- the data of an external record may be accessed by any runtime element of
      *>                the run unit that describes the same record as external.
      *> WHY TWO PROGRAMS WITH DIFFERENT DATA-NAMES: that is the only way GR5's FIRST sentence is
      *> OBSERVABLE.  MAIN-SHARED and SUB-SHARED are different data-names, and MAIN-FILE and SUB-FILE are
      *> different file-names; without the AS phrase each would key its own cell under GR5's SECOND sentence
      *> and neither pair would share anything.  They share because both name PB511XW / PB511XF.
      *> EXPECTED, derived from those rules and never measured:
      *>   1[HELLO]        the CALLed element reads the caller's value out of the shared WS cell
      *>   2[MAINWROTE1]   and out of the shared FD RECORD AREA (GR4b), though the two FDs are named
      *>                   differently and no OPEN has happened -- the record area is storage, not I/O
      *>   3[WORLD]        the store the CALLed element made is visible back in the caller
      *>   4[SUBWROTE12]   likewise through the record area
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MAIN-FILE ASSIGN TO "pb511-ext.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  MAIN-FILE IS EXTERNAL AS "PB511XF".
       01  MAIN-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  MAIN-SHARED IS EXTERNAL AS "PB511XW" PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "HELLO" TO MAIN-SHARED
           MOVE "MAINWROTE1" TO MAIN-REC
           CALL "PB511SB"
           DISPLAY "3[" MAIN-SHARED "]"
           DISPLAY "4[" MAIN-REC "]"
           STOP RUN.
       END PROGRAM PB511AS.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB511SB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SUB-FILE ASSIGN TO "pb511-ext.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SUB-FILE IS EXTERNAL AS "PB511XF".
       01  SUB-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  SUB-SHARED IS EXTERNAL AS "PB511XW" PIC X(5).
       PROCEDURE DIVISION.
       SUB-MAIN.
           DISPLAY "1[" SUB-SHARED "]"
           DISPLAY "2[" SUB-REC "]"
           MOVE "WORLD" TO SUB-SHARED
           MOVE "SUBWROTE12" TO SUB-REC
           GOBACK.
       END PROGRAM PB511SB.
