*>--- conformance-case
*> id: pb64t1_save_restore_locale
*> rules: [GR-14.9.39.4-21, GR-14.9.39.4-22, GR-14.9.39.4-23, GR-14.9.39.4-26, GR-14.9.39.4-27]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39 Format 12 (save-locale) + Format 11 through the pointer — `SET identifier-11 TO LOCALE {LC_ALL |
      *> USER-DEFAULT}` (14.9.39.4 GR26/GR27) and `SET LOCALE category… TO identifier-10` (GR23a, GR21) — increment T1 of
      *> docs/rearchitecture/DESIGN-locale-facility.md (kb/Work PB64; Annex A.4.9 item 9). DETERMINATION L4: the saved
      *> locale is a MANAGED HANDLE in the data-pointer, never an address.
      *>
      *> What each line proves:
      *>   SAVED  — GR26: LC_ALL saves the CURRENT locale (root in every category) into P1; switching LC_COLLATE to ES
      *>            afterwards does not touch the snapshot ("nz" < "ñu" now).
      *>   BACK   — GR23a with identifier-10: LC_COLLATE TO P1 restores the saved root collation ("nz" > "ñu").
      *>   PART   — a saved locale is PER CATEGORY (a snapshot of a state): with LC_COLLATE=ES and LC_TIME=root saved
      *>            into P2, then everything reset to SYSTEM-DEFAULT, LC_COLLATE TO P2 brings back SPANISH collation
      *>            (the snapshot's LC_COLLATE), while LC_TIME TO P2 would bring back root — 14.6.6 r3 at restore time.
      *>   UDSAVE — GR27: USER-DEFAULT saves the USER DEFAULT (root under the harness pin) even while the current
      *>            LC_COLLATE is Spanish; restoring LC_COLLATE from it gives the root order.
      *>   GR22   — `SET LOCALE USER-DEFAULT TO P2` sets the user default FROM a saved locale (SR25 allows identifier-10):
      *>            a later LC_COLLATE TO USER-DEFAULT gives the snapshot's Spanish collation.
      *>   HANDLED/NULL/ADDR — GR21: "The content of the pointer data item referenced by identifier-10 shall reference
      *>            saved locale information; otherwise, the EC-LOCALE-INVALID-PTR exception condition is set to exist
      *>            and the SET statement is unsuccessful." Table 13 (14.6.13.1.6) makes it FATAL; with checking on
      *>            (>>TURN) the USE declarative observes it and RESUME AT NEXT STATEMENT continues. Two invalid
      *>            pointers are tried: NULL, and an ADDRESS OF pointer (a data address is not saved locale
      *>            information). Each time the state is UNCHANGED (Spanish collation survives), as GR21 requires.
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       >>TURN EC-LOCALE-INVALID-PTR CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1SAVE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS CUR.
       SPECIAL-NAMES.
           ALPHABET CUR IS LOCALE
           LOCALE ES IS "es-ES".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC X(2) VALUE "nz".
       01  B               PIC X(2) VALUE "ñu".
       01  VERDICT         PIC X.
       01  P1              USAGE POINTER.
       01  P2              USAGE POINTER.
       01  P3              USAGE POINTER.
       01  PNULL           USAGE POINTER.
       01  PADDR           USAGE POINTER.
       01  SOME-ITEM       PIC X(4) VALUE "data".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-INVALID-PTR.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET P1 TO LOCALE LC_ALL
           SET LOCALE LC_COLLATE TO ES
           PERFORM CMP
           DISPLAY "SAVED=" VERDICT
           SET LOCALE LC_COLLATE TO P1
           PERFORM CMP
           DISPLAY "BACK=" VERDICT
           SET LOCALE LC_COLLATE TO ES
           SET P2 TO LOCALE LC_ALL
           SET LOCALE LC_ALL TO SYSTEM-DEFAULT
           SET LOCALE LC_COLLATE TO P2
           PERFORM CMP
           DISPLAY "PART=" VERDICT
           SET P3 TO LOCALE USER-DEFAULT
           SET LOCALE LC_COLLATE TO P3
           PERFORM CMP
           DISPLAY "UDSAVE=" VERDICT
           SET LOCALE USER-DEFAULT TO P2
           SET LOCALE LC_COLLATE TO USER-DEFAULT
           PERFORM CMP
           DISPLAY "GR22=" VERDICT
           SET PNULL TO NULL
           SET LOCALE LC_COLLATE TO PNULL
           PERFORM CMP
           DISPLAY "NULL=" VERDICT
           SET PADDR TO ADDRESS OF SOME-ITEM
           SET LOCALE LC_ALL TO PADDR
           PERFORM CMP
           DISPLAY "ADDR=" VERDICT
           STOP RUN.
       CMP.
           IF A < B MOVE "<" TO VERDICT ELSE MOVE ">" TO VERDICT END-IF.
