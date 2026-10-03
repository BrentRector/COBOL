      *> kb/Work PB1452 — ISO/IEC 1989:2023 §8.4.3.13.2's program-address-identifier,
      *>     ADDRESS OF PROGRAM { identifier-1 | literal-1 | program-prototype-name-1 },
      *> and §8.4.3.13.3 SR1 "Identifier-1 shall be of category alphanumeric or national". Two defects, one operand:
      *>  (a) the category screen read the item's own PICTURE category — null for a GROUP — so an alphanumeric or national
      *>      GROUP item, and a reference-modified slice of one, was refused as "category (none)". §8.5.2.1: "an
      *>      alphanumeric group item has class and category alphanumeric" (a national group item: national), and
      *>      §8.4.3.3.4 GR6 gives a reference-modified item "the same class, category, and usage" as identifier-1.
      *>  (b) the grammar wrote identifier-1 as a data reference only. §8.4.3.1.3 SR1: "whenever the format for an
      *>      identifier allows another identifier to be specified, that other identifier may be any of the formats for
      *>      an identifier", so a function-identifier of category alphanumeric (§8.4.3.2.1: it references the data item
      *>      that results from the function) is identifier-1 — a parse error before.
      *> §8.4.3.13.4 GR1 a): the program is the one named by "the content of the data item referenced by identifier-1".
      *> Every spelling below names PB1452SUB, so every activation displays IN-SUB. EXPECTED OUTPUT, line by line:
      *>   1=GROUP   GRP is G1 "PB1" + G2 "452SUB" -> "PB1452SUB"                                           IN-SUB
      *>   2=NATGRP  NG is a GROUP-USAGE NATIONAL group, N(3) "PB1" + N(6) "452SUB" (§13.18.29.4 GR2)        IN-SUB
      *>   3=REFMOD  NAMES holds two PIC X(9) occurrences; NAMES(10:9) is the second, a slice of a GROUP     IN-SUB
      *>   4=UPPER   FUNCTION UPPER-CASE(LOW) over the data item LOW = "pb1452sub" (run time) is "PB1452SUB"                  IN-SUB
      *>   5=FOLD    FUNCTION UPPER-CASE("pb1452sub") is "PB1452SUB"                                          IN-SUB
      *>   6=ARG     the SAME identifier is a CALL argument (§14.9.4.3; identifier Format 9, §8.4.3.1.2): the
      *>             callee's program-pointer formal holds the address, and CALLing it activates PB1452SUB    IN-SUB
      *>   REL-EQ    the identifier as a relation operand (§8.8.4.2.2): the address equals PP's, which holds
      *>             the same program's address                                                                 REL-EQ
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1452M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       01 GRP.
          05 G1 PIC X(3) VALUE "PB1".
          05 G2 PIC X(6) VALUE "452SUB".
       01 NG GROUP-USAGE NATIONAL.
          05 N1 PIC N(3) VALUE N"PB1".
          05 N2 PIC N(6) VALUE N"452SUB".
       01 NAMES.
          05 NM PIC X(9) OCCURS 2 VALUE "PB1452SUB".
       01 LOW PIC X(9) VALUE "pb1452sub".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1=GROUP"
           SET PP TO ADDRESS OF PROGRAM GRP
           CALL PP
           DISPLAY "2=NATGRP"
           SET PP TO ADDRESS OF PROGRAM NG
           CALL PP
           DISPLAY "3=REFMOD"
           SET PP TO ADDRESS OF PROGRAM NAMES(10:9)
           CALL PP
           DISPLAY "4=UPPER"
           SET PP TO ADDRESS OF PROGRAM FUNCTION UPPER-CASE(LOW)
           CALL PP
           DISPLAY "5=FOLD"
           SET PP TO ADDRESS OF PROGRAM FUNCTION UPPER-CASE("pb1452sub")
           CALL PP
           DISPLAY "6=ARG"
           CALL "PB1452TK"
               USING ADDRESS OF PROGRAM FUNCTION UPPER-CASE(LOW)
           SET PP TO ADDRESS OF PROGRAM "PB1452SUB"
           IF PP = ADDRESS OF PROGRAM FUNCTION UPPER-CASE(LOW)
               DISPLAY "REL-EQ"
           ELSE
               DISPLAY "REL-NE"
           END-IF
           STOP RUN.
       END PROGRAM PB1452M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1452TK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING LK-PP.
           CALL LK-PP
           GOBACK.
       END PROGRAM PB1452TK.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1452SUB.
       PROCEDURE DIVISION.
           DISPLAY "IN-SUB"
           GOBACK.
       END PROGRAM PB1452SUB.
