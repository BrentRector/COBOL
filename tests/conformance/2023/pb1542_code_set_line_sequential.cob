       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1542LS.
      *> ISO/IEC 1989:2023 §13.18.13.4 GR6 b) on a LINE SEQUENTIAL
      *> file (kb/Work PB1542). The line sequential character set of
      *> Annex A.1 item 115 (docs/CONFORMANCE.md DOC-A.1-115) is every
      *> character from U+0020 that has an image in the FILE'S coded
      *> character set - and a CODE-SET clause makes that the
      *> alphabet's set. So a record-area character the CODE-SET
      *> alphabet cannot represent is outside it:
      *>  - WRITE: unsuccessful, '71' (§14.9.51.4 GR23; §9.1.13.10
      *>    item 1), the organization's own value for what every other
      *>    organization answers '91';
      *>  - READ of a medium byte outside the set: successful, '09'
      *>    (§14.9.30.4 GR16), the byte carried as the native character
      *>    of the same value. The line was written with NO CODE-SET,
      *>    i.e. as UTF-8 text (DOC-A.1-115, kb/Work PB1760), so the
      *>    "é" is the two bytes X'C3A9' and position 2 holds X'C3'
      *>    (ORD 196) - outside ISO/IEC 646's 128 characters.
      *> Why each leg can fail:
      *>  S1-E    - the defect wrote X'E9' with '00' (STANDARD-1 is
      *>            ISO/IEC 646 IRV, 128 characters, §12.3.7.4 GR7 c).
      *>  EB-EURO - the defect died of an unhandled exception; U+20AC
      *>            has no CCSID 37 code unit.
      *>  EB-E    - U+00E9 has one (X'51'): '00'.
      *>  S1-RAW  - the file holds "AB~" alone: the refused WRITE put
      *>            nothing on the medium.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET S1 IS STANDARD-1
           ALPHABET EB IS EBCDIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LS ASSIGN TO "pb1542ls-s.dat"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS FS.
           SELECT LSRAW ASSIGN TO "pb1542ls-s.dat"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS FW.
           SELECT LE ASSIGN TO "pb1542ls-e.dat"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS FE.
       DATA DIVISION.
       FILE SECTION.
       FD LS CODE-SET IS S1.
       01 LR PIC X(3).
       FD LSRAW.
       01 LW PIC X(3).
       FD LE CODE-SET IS EB.
       01 ER PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 FW PIC XX.
       01 FE PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT LS.
           MOVE "AéB" TO LR.
           WRITE LR.
           DISPLAY "S1-E=" FS " " FUNCTION ORD(LR(2:1)).
           MOVE "AB~" TO LR.
           WRITE LR.
           DISPLAY "S1-TILDE=" FS.
           CLOSE LS.
           OPEN INPUT LSRAW.
           READ LSRAW.
           DISPLAY "S1-RAW=" FW " " LW.
           READ LSRAW.
           DISPLAY "S1-RAW2=" FW.
           CLOSE LSRAW.
           OPEN OUTPUT LSRAW.
           MOVE "XéY" TO LW.
           WRITE LW.
           CLOSE LSRAW.
           OPEN INPUT LS.
           READ LS.
           DISPLAY "S1-IN=" FS " " FUNCTION ORD(LR(2:1)).
           CLOSE LS.
           OPEN OUTPUT LE.
           MOVE "A€B" TO ER.
           WRITE ER.
           DISPLAY "EB-EURO=" FE " " FUNCTION ORD(ER(2:1)).
           MOVE "AéB" TO ER.
           WRITE ER.
           DISPLAY "EB-E=" FE.
           CLOSE LE.
           OPEN INPUT LE.
           READ LE.
           DISPLAY "EB-READ=" FE " " FUNCTION ORD(ER(2:1)).
           CLOSE LE.
           STOP RUN.
