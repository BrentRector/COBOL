       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB690FCS.
      *> ISO/IEC 1989:2023 §8.1.2 (the computer's coded character
      *> set, Annex A.1 item 31) and §9.1.13.11 item 1 (I-O status 9x)
      *> — a record character with no byte image in the FILE coded
      *> character set is REFUSED, never written as '?' (kb/Work PB690).
      *>
      *> The determination (owner decision kb/Work R47; docs/
      *> CONFORMANCE.md DOC-A.1-31, -110, -159): a file with no
      *> CODE-SET clause is ISO/IEC 8859-1, one byte per character
      *> position; U+0000-U+00FF are the bytes of the same value; the
      *> WRITE or REWRITE of a record holding a character above U+00FF
      *> is unsuccessful with the implementor-defined I-O status '91'
      *> (§9.1.13.11: "An implementor-defined condition exists. This
      *> condition shall not duplicate any other condition specified
      *> by another I-O status value."), nothing reaches the medium and
      *> the record area is unchanged (§14.9.51.4 GR15: "the write
      *> operation does not take place, the content of the record area
      *> is unaffected").
      *>
      *> Why each leg can fail:
      *>  SQ-EURO  - U+20AC has no Latin-1 byte: '91', and the record
      *>             area still holds it (ORD 8365 = U+20AC + 1). The
      *>             defect wrote '?' with '00'.
      *>  SQ-E     - U+00E9 has one: '00'.
      *>  SQ-READ  - the file holds exactly the one record, its second
      *>             byte 0xE9 (ORD 234), then at end '10'.
      *>  SQ-REWR  - REWRITE of a record holding U+20AC is '91' and the
      *>             record in the file is unchanged (SQ-AGAIN).
      *>  RL-*     - a RELATIVE store's record is the same one-byte
      *>             image: '91', then '00', and the file holds RRN 1
      *>             only (the refused WRITE released nothing, so the
      *>             accepted one is RRN 1).
      *>  IX-*     - an INDEXED store's likewise: '91' and '00', and a
      *>             READ of the refused key is '23'.
      *>  PR-*     - a print-control WRITE (the report writer's line)
      *>             is the same file coded character set: '91' for
      *>             U+20AC; U+00E9 is written as its OWN byte 0xE9 —
      *>             never '?' — and the file holds that line alone.
      *>             The file is six bytes: the second WRITE is AFTER
      *>             ADVANCING 1 LINE (§14.9.51.4 GR25 — kb/Work
      *>             PB1027), so a line end (LF: the line end of a
      *>             record sequential print stream with no LINAGE
      *>             clause, DOC-A.1-146 (c'), kb/Work PB1664) comes
      *>             first, then P 0xE9 0 1 at bytes 2-5, and CLOSE
      *>             ends the open line (byte 6). The refused write
      *>             travelled nowhere (§14.9.51.4 GR15).
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0D SYM-X0A
               ARE 14 11.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "pb690fcs-s.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS FS.
           SELECT RL ASSIGN TO "pb690fcs-r.dat"
               ORGANIZATION RELATIVE ACCESS DYNAMIC
               RELATIVE KEY RK FILE STATUS FR.
           SELECT IX ASSIGN TO "pb690fcs-i.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY IX-K FILE STATUS FI.
           SELECT PRTF ASSIGN TO "pb690fcs-p.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS FP.
           SELECT CK ASSIGN TO "pb690fcs-p.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS FC.
       DATA DIVISION.
       FILE SECTION.
       FD SQ.
       01 SR PIC X(4).
       FD RL.
       01 RR PIC X(4).
       FD IX.
       01 IX-R.
          05 IX-K PIC X(4).
          05 IX-D PIC X(4).
       FD PRTF.
       01 PR PIC X(4).
       FD CK.
       01 CR PIC X.
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 FR PIC XX.
       01 FI PIC XX.
       01 FP PIC XX.
       01 FC PIC XX.
       01 RK PIC 9(4).
       01 N  PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQ.
           MOVE "A€BC" TO SR.
           WRITE SR.
           DISPLAY "SQ-EURO=" FS " " FUNCTION ORD(SR(2:1)).
           MOVE "AéBC" TO SR.
           WRITE SR.
           DISPLAY "SQ-E=" FS.
           CLOSE SQ.
           OPEN INPUT SQ.
           READ SQ.
           DISPLAY "SQ-READ=" FS " " FUNCTION ORD(SR(2:1)).
           READ SQ.
           DISPLAY "SQ-READ2=" FS.
           CLOSE SQ.
           OPEN I-O SQ.
           READ SQ.
           MOVE "Z€ZZ" TO SR.
           REWRITE SR.
           DISPLAY "SQ-REWR=" FS.
           CLOSE SQ.
           OPEN INPUT SQ.
           READ SQ.
           DISPLAY "SQ-AGAIN=" FS " " FUNCTION ORD(SR(2:1)).
           CLOSE SQ.
           OPEN OUTPUT RL.
           MOVE 1 TO RK.
           MOVE "R€01" TO RR.
           WRITE RR INVALID KEY DISPLAY "RL-EURO INVALID KEY".
           DISPLAY "RL-EURO=" FR.
           MOVE "Ré01" TO RR.
           WRITE RR INVALID KEY DISPLAY "RL-E INVALID KEY".
           DISPLAY "RL-E=" FR.
           CLOSE RL.
           OPEN INPUT RL.
           READ RL NEXT RECORD.
           DISPLAY "RL-READ=" FR " " RK " " FUNCTION ORD(RR(2:1)).
           READ RL NEXT RECORD AT END DISPLAY "RL-READ2 AT END".
           CLOSE RL.
           OPEN OUTPUT IX.
           MOVE "K€01DATA" TO IX-R.
           WRITE IX-R INVALID KEY DISPLAY "IX-EURO INVALID KEY".
           DISPLAY "IX-EURO=" FI.
           MOVE "Ké01DATA" TO IX-R.
           WRITE IX-R INVALID KEY DISPLAY "IX-E INVALID KEY".
           DISPLAY "IX-E=" FI.
           CLOSE IX.
           OPEN INPUT IX.
           MOVE "K€01" TO IX-K.
           READ IX INVALID KEY DISPLAY "IX-READ-EURO INVALID KEY".
           DISPLAY "IX-READ-EURO=" FI.
           MOVE "Ké01" TO IX-K.
           READ IX.
           DISPLAY "IX-READ-E=" FI " " IX-D.
           CLOSE IX.
           OPEN OUTPUT PRTF.
           MOVE "P€01" TO PR.
           WRITE PR AFTER ADVANCING 1 LINE.
           DISPLAY "PR-EURO=" FP.
           MOVE "Pé01" TO PR.
           WRITE PR AFTER ADVANCING 1 LINE.
           DISPLAY "PR-E=" FP.
           CLOSE PRTF.
           OPEN INPUT CK.
           MOVE 0 TO N.
           PERFORM UNTIL FC NOT = "00"
               READ CK
               IF FC = "00"
                   ADD 1 TO N
                   IF CR NOT = SYM-X0D AND CR NOT = SYM-X0A
                       DISPLAY "PR-BYTE " N "=" FUNCTION ORD(CR)
                   END-IF
               END-IF
           END-PERFORM.
           DISPLAY "PR-END=" FC " " N.
           CLOSE CK.
           STOP RUN.
