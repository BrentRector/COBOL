      *>--- conformance-case
      *> id: pb154_cancel_active
      *> rules: [GR-14.9.5.4-5, DOC-A.1-18]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>TURN EC-PROGRAM-CANCEL-ACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB154CA.
      *> kb/Work PB154 - 14.9.5 GR5: CANCEL of an ACTIVE program raises
      *> the fatal EC-PROGRAM-CANCEL-ACTIVE and the program is NOT
      *> canceled - the self-cancel inside A is handled by A's USE
      *> declarative, RESUME continues, and A's state survives (the
      *> second call answers 2).
       PROCEDURE DIVISION.
       MAIN.
           CALL "A154"
           CALL "A154"
           STOP RUN.
       END PROGRAM PB154CA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A154.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-CANCEL-ACTIVE.
       ERR-PARA.
           DISPLAY "EC=" FUNCTION EXCEPTION-STATUS(1:25)
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           ADD 1 TO N
           CANCEL "A154"
           DISPLAY "A=" N
           GOBACK.
       END PROGRAM A154.
