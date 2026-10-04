       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB154CC.
      *> kb/Work PB154 - the CANCEL cascade on true GR7 premises. 14.9.18
      *> GR2's implicit CANCEL at an INITIAL program's return cascades
      *> (14.9.5 GR4) over its containee, so the second CALL "OUTER154"
      *> repeats W/A exactly (11.10.4 GR3 + 14.6.2.3.2). kb/Work PB1507:
      *> the containee is NOT RECURSIVE - an INITIAL program shall not
      *> contain a RECURSIVE one (11.10.3 SR6: "The RECURSIVE clause
      *> shall not be specified if any program that directly or
      *> indirectly contains this program is an initial program"), and
      *> the earlier text of this golden was nonconforming source. An
      *> explicit CANCEL of a container cascades over a
      *> NEVER-CALLED containee that owns a never-registered file
      *> connector: GR7 is "no action", not a close of nothing (the
      *> sibling shape NIST IC203A CNCL-TEST-04 measured). A repeated
      *> CANCEL is GR7's already-canceled no-op, and the re-CALL finds
      *> the initial state (GR3): C back to 0001. The recursive
      *> containee shapes are pb1133_recursive_container_static_ws.
       PROCEDURE DIVISION.
       MAIN.
           CALL "OUTER154"
           CALL "OUTER154"
           CALL "CONT154"
           CALL "CONT154"
           CANCEL "CONT154"
           CANCEL "CONT154"
           CALL "CONT154"
           STOP RUN.
       END PROGRAM PB154CC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER154 INITIAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO W
           DISPLAY "W=" W
           CALL "INNERA154"
           CALL "INNERA154"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERA154.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO A
           DISPLAY "A=" A
           GOBACK.
       END PROGRAM INNERA154.
       END PROGRAM OUTER154.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONT154.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO C
           DISPLAY "C=" C
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEVER154.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "pb154cc.dat".
       DATA DIVISION.
       FILE SECTION.
       FD NF.
       01 NF-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT NF
           CLOSE NF
           GOBACK.
       END PROGRAM NEVER154.
       END PROGRAM CONT154.
