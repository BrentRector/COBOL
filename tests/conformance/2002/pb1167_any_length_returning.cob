      *> kb/Work PB1167 -- an ANY LENGTH RETURNING item of a METHOD and of a CONTAINED program.
      *> ISO 13.18.2.3 SR3 (cite.py --check 13.18.2.3 "If the source element containing the ANY LENGTH clause
      *> is a contained program or is a method" -> OK 13.18.2.3 3)) admits the subject of the clause as the
      *> RETURNING item of the procedure division header, and 13.18.2.4 GR1 b) (cite.py --check 13.18.2.4
      *> "where n is the length of the corresponding argument or returning item of the activating runtime
      *> element" -> OK 13.18.2.4 1) b)) makes the item "n repetitions of the picture symbol" where n is the
      *> length of the ACTIVATOR's receiving item.  14.8.3.3 rule 5 (cite.py --check 14.8.3.3 "If the
      *> sending operand is described with the ANY LENGTH clause, the length of the sending operand is
      *> considered to match the length of the receiving operand" -> OK 14.8.3.3 5)) is the same fact at the
      *> delivery.  The activated element therefore sees n BEFORE its first statement, and a MOVE to the item
      *> fills n positions (14.6.8.5: alphanumeric receivers are "aligned at the leftmost character position
      *> ... with space fill or truncation to the right"):
      *>   GETV into X(3)        n=3  "AB" -> [AB ]            GETS into X(5)   n=5  "ABCDEFGH" -> [ABCDE]
      *>   GETN into N(3)        n=3  N"HELLO" -> [HEL]         GETB into 1(4)   n=4  B"1011" -> [1011]
      *>   GETV into a 6-byte alphanumeric GROUP (14.8.3.2: an alphanumeric group is a conforming receiver)
      *>                         n=6  "AB" -> [AB    ]
      *>   program path: NRET RETURNING the ANY LENGTH formal AL of its caller NFIX (n is AL's own length, 5,
      *>   which NFIX got from R5 -- an ANY LENGTH RECEIVER); NNAT into N(3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1167AL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1167C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1167C.
       01 R5 PIC X(5).
       01 R3 PIC X(3).
       01 N3 PIC N(3).
       01 B4 PIC 1(4).
       01 GR.
          05 GA PIC X(2).
          05 GB PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1167C "NEW" RETURNING O.
           INVOKE O "GETV" RETURNING R3.
           DISPLAY "R3=[" R3 "]".
           INVOKE O "GETS" RETURNING R5.
           DISPLAY "R5=[" R5 "]".
           INVOKE O "GETN" RETURNING N3.
           DISPLAY "N3=[" N3 "]".
           INVOKE O "GETB" RETURNING B4.
           DISPLAY "B4=[" B4 "]".
           INVOKE O "GETV" RETURNING GR.
           DISPLAY "GR=[" GR "]".
           CALL "NFIX" AS NESTED USING R5.
           CALL "NNAT" AS NESTED RETURNING N3.
           DISPLAY "N3=[" N3 "]".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NFIX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 AL PIC X ANY LENGTH.
       PROCEDURE DIVISION USING AL.
           CALL "NRET" AS NESTED RETURNING AL.
           DISPLAY "AL=[" AL "]".
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NRET.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "NRETLEN=" FUNCTION LENGTH(R).
           MOVE "ZYXWVUTS" TO R.
           GOBACK.
       END PROGRAM NRET.
       END PROGRAM NFIX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NNAT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC N ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "NNATLEN=" FUNCTION LENGTH(R).
           MOVE N"HELLO" TO R.
           GOBACK.
       END PROGRAM NNAT.
       END PROGRAM PB1167AL.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1167C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "GETV LEN=" FUNCTION LENGTH(R).
           MOVE "AB" TO R.
       END METHOD GETV.
       METHOD-ID. GETS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "GETS LEN=" FUNCTION LENGTH(R).
           MOVE "ABCDEFGH" TO R.
       END METHOD GETS.
       METHOD-ID. GETN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC N ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "GETN LEN=" FUNCTION LENGTH(R).
           MOVE N"HELLO" TO R.
       END METHOD GETN.
       METHOD-ID. GETB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 1 ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "GETB LEN=" FUNCTION LENGTH(R).
           MOVE B"1011" TO R.
       END METHOD GETB.
       END OBJECT.
       END CLASS PB1167C.
