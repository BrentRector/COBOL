      *>--- conformance-case
      *> id: pb2834_ls_rewrite_size_before_charset
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2834LS.
      *> kb/Work PB2834 -- a LINE SEQUENTIAL REWRITE tests ISO/IEC
      *> 1989:2023 §14.9.35.4 GR17 in the order the rule states it:
      *> b) a record of MORE bytes than the record being replaced is
      *> unsuccessful, I-O status '44', and only then d) a record area
      *> holding a character outside the line sequential character set
      *> is unsuccessful, '71'. (GR17 a), the part-record READ, is
      *> first and is pinned by CA18.)
      *>   python scripts/spec/cite.py --check 14.9.35.4 "is greater
      *>   than the number of bytes in the record being replaced, the
      *>   execution of the REWRITE statement is unsuccessful"
      *>                                    -> OK  §14.9.35.4 17) (b)
      *>   python scripts/spec/cite.py --check 14.9.35.4 "not in the
      *>   implementor-defined character set defined for a line
      *>   sequential file the execution of the REWRITE statement is
      *>   unsuccessful"                    -> OK  §14.9.35.4 17) (d)
      *> Every line replaced here is the two bytes AB.
      *>  S1-LONGER  - CODE-SET STANDARD-1 (ISO/IEC 646, 128
      *>               characters): "AéBCD" is 5 bytes whatever image
      *>               the é would be given, more than 2 ==> '44'. The
      *>               defect answered '71'.
      *>  S1-EQUAL   - "Aé" is 2 bytes, not more than 2, so b) does not
      *>               apply and d) answers '71'.
      *>  UTF8-BYTES - no CODE-SET: the file is UTF-8 text, "éé" is 4
      *>               bytes ==> '44' (b) counts BYTES on the medium).
      *>  CTRL-LONGER- "AB" and U+0001 is 3 bytes ==> '44' although
      *>               U+0001 is outside the set (d).
      *>  CTRL-EQUAL - U+0001 after "A" is 2 bytes, so only d): '71'.
      *>  S1-AFTER   - a refused REWRITE changes nothing: the line is
      *>               still AB (§9.1.13.10 item 1).
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET S1 IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2834-f.dat"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS ST.
           SELECT G ASSIGN TO "pb2834-g.dat"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS ST.
           SELECT H ASSIGN TO "pb2834-h.dat"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS ST.
           SELECT J ASSIGN TO "pb2834-j.dat"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS ST.
           SELECT K ASSIGN TO "pb2834-k.dat"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS ST.
       DATA DIVISION.
       FILE SECTION.
       FD F CODE-SET IS S1.
       01 FR PIC X(5).
       FD G CODE-SET IS S1.
       01 GR PIC X(2).
       FD H.
       01 HR PIC X(2).
       FD J.
       01 JR PIC X(3).
       FD K.
       01 KR PIC X(2).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "AB" TO FR.
           WRITE FR.
           CLOSE F.
           OPEN I-O F.
           READ F.
           MOVE "AéBCD" TO FR.
           REWRITE FR.
           DISPLAY "S1-LONGER=" ST.
           CLOSE F.
           OPEN OUTPUT G.
           MOVE "AB" TO GR.
           WRITE GR.
           CLOSE G.
           OPEN I-O G.
           READ G.
           MOVE "Aé" TO GR.
           REWRITE GR.
           DISPLAY "S1-EQUAL=" ST.
           CLOSE G.
           OPEN OUTPUT H.
           MOVE "AB" TO HR.
           WRITE HR.
           CLOSE H.
           OPEN I-O H.
           READ H.
           MOVE "éé" TO HR.
           REWRITE HR.
           DISPLAY "UTF8-BYTES=" ST.
           CLOSE H.
           OPEN OUTPUT J.
           MOVE "AB" TO JR.
           WRITE JR.
           CLOSE J.
           OPEN I-O J.
           READ J.
           MOVE X"414201" TO JR.
           REWRITE JR.
           DISPLAY "CTRL-LONGER=" ST.
           CLOSE J.
           OPEN OUTPUT K.
           MOVE "AB" TO KR.
           WRITE KR.
           CLOSE K.
           OPEN I-O K.
           READ K.
           MOVE X"4101" TO KR.
           REWRITE KR.
           DISPLAY "CTRL-EQUAL=" ST.
           CLOSE K.
           OPEN INPUT F.
           READ F.
           DISPLAY "S1-AFTER=" ST " " FR "|".
           CLOSE F.
           STOP RUN.
