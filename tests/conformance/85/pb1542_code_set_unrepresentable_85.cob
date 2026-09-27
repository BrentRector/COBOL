       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1542CS.
      *> ISO/IEC 1989:2023 §13.18.13.4 GR6 b) — "On output, each
      *> native coded character in the record is replaced for the
      *> storage medium with its associated coded character as defined
      *> in the alphabet being used" — for EVERY organization, at the
      *> output STATEMENT (kb/Work PB1150, PB1542).
      *>
      *> A character with NO associated coded character in the
      *> CODE-SET alphabet cannot be replaced, so the WRITE or REWRITE
      *> is unsuccessful with the implementor-defined I-O status '91'
      *> (§9.1.13.11 item 1; docs/CONFORMANCE.md DOC-A.1-110 — the
      *> same condition, and status, as a character with no byte image
      *> in a file with no CODE-SET), nothing reaches the medium and
      *> the record area is unaffected (§14.9.51.4 GR15, §14.9.35.4
      *> GR14). STANDARD-1 / STANDARD-2 are ISO/IEC 646 IRV, 128
      *> characters (§12.3.7.4 GR7 c; DOC-A.1-187); EBCDIC is CCSID 37,
      *> 256 characters, all of U+0000-U+00FF.
      *>
      *> Why each leg can fail:
      *>  SQ-E / SQ-EURO - U+00E9 / U+20AC are not ISO 646 characters:
      *>        '91' and the area still holds them (ORD 234 / 8365).
      *>        The defect wrote X'E9' with '00' (PB1542).
      *>  SQ-RAW - the medium holds exactly "AB~" (the refused records
      *>        released nothing), read through a description with no
      *>        CODE-SET: ORD 66 / 127, then '10'.
      *>  SQ-IN / SQ-REWR - a medium byte OUTSIDE the set (X'E9',
      *>        written with no CODE-SET) READs as the native character
      *>        of the same value, '00' (the determination: no rule
      *>        names a READ condition); REWRITing it unchanged is the
      *>        output rule again - '91'; the next REWRITE, of IRV
      *>        characters, is '43' because the refused REWRITE is now
      *>        the immediately previous statement (§14.9.35.4 GR5), so
      *>        the medium still holds X'E9' (SQ-AFTER).
      *>  RL-*  - a RELATIVE store: '91' for U+00E9, '00' for "~".
      *>  IX-*  - an INDEXED store under EBCDIC: U+20AC is '91' AT THE
      *>        WRITE (the defect answered '00' and the implicit CLOSE
      *>        died of an unhandled exception, PB1150); the CLOSE is
      *>        '00'; the refused key is '23'; the accepted record is
      *>        on the medium in CCSID 37 - K = X'D2' (ORD 211), é =
      *>        X'51' (ORD 82) - read back through the raw description.
      *>  IA-*  - an INDEXED store under STANDARD-1 that already holds
      *>        a record with X'E9' (written with no CODE-SET): the READ
      *>        is '00', a REWRITE of it is '91', a new IRV record is
      *>        '00', and the CLOSE that re-persists the whole store is
      *>        '00' and writes the untouched X'E9' back byte-exactly
      *>        (IA-RAW ORD 234).
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET S1 IS STANDARD-1
           ALPHABET S2 IS STANDARD-2
           ALPHABET EB IS EBCDIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "pb1542cs-s.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS FS.
           SELECT SQRAW ASSIGN TO "pb1542cs-s.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS FW.
           SELECT RL ASSIGN TO "pb1542cs-r.dat"
               ORGANIZATION RELATIVE ACCESS DYNAMIC
               RELATIVE KEY RK FILE STATUS FR.
           SELECT IX ASSIGN TO "pb1542cs-i.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY IX-K FILE STATUS FI.
           SELECT IXRAW ASSIGN TO "pb1542cs-i.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY IW-K FILE STATUS FJ.
           SELECT IA ASSIGN TO "pb1542cs-a.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY IA-K FILE STATUS FA.
           SELECT IARAW ASSIGN TO "pb1542cs-a.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY IR-K FILE STATUS FB.
       DATA DIVISION.
       FILE SECTION.
       FD SQ CODE-SET IS S2.
       01 SR PIC X(3).
       FD SQRAW.
       01 SW PIC X(3).
       FD RL CODE-SET IS S1.
       01 RR PIC X(3).
       FD IX CODE-SET IS EB.
       01 IX-R.
          05 IX-K PIC X(4).
          05 IX-D PIC X(4).
       FD IXRAW.
       01 IW-R.
          05 IW-K PIC X(4).
          05 IW-D PIC X(4).
       FD IA CODE-SET IS S1.
       01 IA-R.
          05 IA-K PIC X(4).
          05 IA-D PIC X(4).
       FD IARAW.
       01 IR-R.
          05 IR-K PIC X(4).
          05 IR-D PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 FW PIC XX.
       01 FR PIC XX.
       01 FI PIC XX.
       01 FJ PIC XX.
       01 FA PIC XX.
       01 FB PIC XX.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQ.
           MOVE "AB~" TO SR.
           WRITE SR.
           DISPLAY "SQ-TILDE=" FS.
           MOVE "AéB" TO SR.
           WRITE SR.
           DISPLAY "SQ-E=" FS " " FUNCTION ORD(SR(2:1)).
           MOVE "A€B" TO SR.
           WRITE SR.
           DISPLAY "SQ-EURO=" FS " " FUNCTION ORD(SR(2:1)).
           CLOSE SQ.
           OPEN INPUT SQRAW.
           READ SQRAW.
           DISPLAY "SQ-RAW=" FW " " FUNCTION ORD(SW(1:1))
               " " FUNCTION ORD(SW(3:1)).
           READ SQRAW.
           DISPLAY "SQ-RAW2=" FW.
           CLOSE SQRAW.
           OPEN OUTPUT SQRAW.
           MOVE "XéY" TO SW.
           WRITE SW.
           CLOSE SQRAW.
           OPEN I-O SQ.
           READ SQ.
           DISPLAY "SQ-IN=" FS " " FUNCTION ORD(SR(2:1)).
           REWRITE SR.
           DISPLAY "SQ-REWR=" FS.
           MOVE "XYZ" TO SR.
           REWRITE SR.
           DISPLAY "SQ-REWR2=" FS.
           CLOSE SQ.
           OPEN INPUT SQRAW.
           READ SQRAW.
           DISPLAY "SQ-AFTER=" FW " " SW.
           CLOSE SQRAW.
           OPEN OUTPUT RL.
           MOVE 1 TO RK.
           MOVE "Ré1" TO RR.
           WRITE RR INVALID KEY DISPLAY "RL-E INVALID KEY".
           DISPLAY "RL-E=" FR.
           MOVE "R~1" TO RR.
           WRITE RR INVALID KEY DISPLAY "RL-TILDE INVALID KEY".
           DISPLAY "RL-TILDE=" FR.
           CLOSE RL.
           OPEN INPUT RL.
           READ RL NEXT RECORD.
           DISPLAY "RL-READ=" FR " " RK " " RR.
           CLOSE RL.
           OPEN OUTPUT IX.
           MOVE "K€01DATA" TO IX-R.
           WRITE IX-R INVALID KEY DISPLAY "IX-EURO INVALID KEY".
           DISPLAY "IX-EURO=" FI.
           MOVE "Ké01DATA" TO IX-R.
           WRITE IX-R INVALID KEY DISPLAY "IX-E INVALID KEY".
           DISPLAY "IX-E=" FI.
           CLOSE IX.
           DISPLAY "IX-CLOSE=" FI.
           OPEN INPUT IX.
           MOVE "K€01" TO IX-K.
           READ IX INVALID KEY DISPLAY "IX-READ-EURO INVALID KEY".
           DISPLAY "IX-READ-EURO=" FI.
           MOVE "Ké01" TO IX-K.
           READ IX.
           DISPLAY "IX-READ-E=" FI " " IX-D.
           CLOSE IX.
           OPEN INPUT IXRAW.
           READ IXRAW NEXT RECORD.
           DISPLAY "IX-RAW=" FJ " " FUNCTION ORD(IW-K(1:1))
               " " FUNCTION ORD(IW-K(2:1)).
           CLOSE IXRAW.
           OPEN OUTPUT IARAW.
           MOVE "A001DéTA" TO IR-R.
           WRITE IR-R INVALID KEY DISPLAY "IA-SEED INVALID KEY".
           DISPLAY "IA-SEED=" FB.
           CLOSE IARAW.
           OPEN I-O IA.
           MOVE "A001" TO IA-K.
           READ IA.
           DISPLAY "IA-IN=" FA " " FUNCTION ORD(IA-D(2:1)).
           REWRITE IA-R INVALID KEY DISPLAY "IA-REWR INVALID KEY".
           DISPLAY "IA-REWR=" FA.
           MOVE "B001DATA" TO IA-R.
           WRITE IA-R INVALID KEY DISPLAY "IA-NEW INVALID KEY".
           DISPLAY "IA-NEW=" FA.
           CLOSE IA.
           DISPLAY "IA-CLOSE=" FA.
           OPEN INPUT IARAW.
           MOVE "A001" TO IR-K.
           READ IARAW.
           DISPLAY "IA-RAW=" FB " " FUNCTION ORD(IR-D(2:1)).
           MOVE "B001" TO IR-K.
           READ IARAW.
           DISPLAY "IA-RAW-B=" FB " " IR-D.
           CLOSE IARAW.
           STOP RUN.
