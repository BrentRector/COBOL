*>--- conformance-case
*> id: pb154_cancel_initial
*> rules: [GR-8.5.1.11.3-4, GR-14.6.2.3.2-L2.3, GR-14.9.5.4-3,
*>    GR-14.9.5.4-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB154IN.
      *> kb/Work PB154 - 14.9.5 GR3 via 14.6.2.3.2, the SEVEN actions on a
      *> RECURSIVE unit's STATIC working-storage: after CANCEL the next
      *> CALL finds the dynamic-capacity table at its minimum (action 6),
      *> the DYNAMIC LENGTH item at length zero (action 7), the internal
      *> file connector in no open mode (action 3 - reopening answers 00,
      *> not 41), and the BASED item's address NULL (action 5 - the leg
      *> whose static bridge the compiler previously REJECTED as
      *> RecursiveWsPointerBacked on legal source).
      *> RE-PINNED HOST-INDEPENDENTLY (kb/Work PB168): the REOPEN=00 leg
      *> alone was host-locking-dependent - on POSIX a re-registration
      *> hands back a fresh closed connector even if CANCEL's implicit
      *> close never ran; only Windows' sharing violation ('30') would
      *> discriminate. THE FLUSHED LEG PROVES THE FLUSH BY ITS EFFECT ON
      *> EVERY HOST: GROW writes a record it never closes, and reading
      *> it back means the buffered write SURVIVED CANCEL - completed by
      *> the 14.9.5.4 GR9 implicit CLOSE, with Register's displaced-
      *> connector close (the PB168 hygiene, same observable effect) as
      *> the backstop. The leg pins the OUTCOME the standard requires,
      *> not which of the two same-effect mechanisms delivered it.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MODE-W PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "GROW" TO MODE-W
           CALL "R154" USING MODE-W
           CANCEL "R154"
           MOVE "SHOW" TO MODE-W
           CALL "R154" USING MODE-W
           STOP RUN.
       END PROGRAM PB154IN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R154 RECURSIVE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb154in.dat"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 TBL.
          05 ELEM OCCURS DYNAMIC CAPACITY IN CAP FROM 1 PIC X.
       01 DL PIC X DYNAMIC LENGTH.
       01 B PIC X(3) BASED.
       01 P USAGE POINTER.
       LINKAGE SECTION.
       01 MODE-ARG PIC X(8).
       PROCEDURE DIVISION USING MODE-ARG.
       MAIN.
           IF MODE-ARG = "GROW    "
               MOVE "C" TO ELEM(3)
               MOVE "XYZ" TO DL
               OPEN OUTPUT F
               MOVE "FLSH" TO F-REC
               WRITE F-REC
               ALLOCATE B
               DISPLAY "GROW CAP=" CAP " LEN=" FUNCTION LENGTH(DL)
                   " FS=" FS
           ELSE
               DISPLAY "SHOW CAP=" CAP " LEN=" FUNCTION LENGTH(DL)
               OPEN INPUT F
               READ F
               DISPLAY "FLUSHED=" F-REC " " FS
               CLOSE F
               OPEN OUTPUT F
               DISPLAY "REOPEN=" FS
               CLOSE F
               SET P TO ADDRESS OF B
               IF P = NULL
                   DISPLAY "ADDR=NULL"
               ELSE
                   DISPLAY "ADDR=HELD"
               END-IF
           END-IF
           GOBACK.
       END PROGRAM R154.
