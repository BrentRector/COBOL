       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1542CS.
      *> ISO/IEC 1989:2023 §13.18.13.4 GR6 b) — "On output, each
      *> native coded character in the record is replaced for the
      *> storage medium with its associated coded character as defined
      *> in the alphabet being used" — at the output STATEMENT
      *> (kb/Work PB1150, PB1542).
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
      *>  Only SEQUENTIAL files carry the clause: a RELATIVE or
      *>        INDEXED file description entry is Format 2, which
      *>        prints no CODE-SET clause (§13.4.5.3 SR7; kb/Work
      *>        PB1238 refuses it, COBOLNET2604).
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
       DATA DIVISION.
       FILE SECTION.
       FD SQ CODE-SET IS S2.
       01 SR PIC X(3).
       FD SQRAW.
       01 SW PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 FW PIC XX.
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
           STOP RUN.
