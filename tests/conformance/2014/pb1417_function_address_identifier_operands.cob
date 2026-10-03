      *> kb/Work PB1417 — ISO/IEC 1989:2023 §8.4.3.12.3 SR1: "Identifier-1 shall be of category alphanumeric or
      *> national" (the function-address-identifier, ADDRESS OF FUNCTION { function-prototype-name-1 | identifier-1 }).
      *> The screen read the item's own PICTURE category, which a GROUP does not have, so an alphanumeric or national
      *> GROUP item — and a reference-modified slice of one — was refused as "category (none)". What the standard says:
      *>   §8.5.2.1          "an alphanumeric group item has class and category alphanumeric" (and a national group
      *>                     item class and category national) — so a group is admissible as identifier-1.
      *>   §8.4.3.3.4 GR6    the unique data item a reference modifier creates has "the same class, category, and usage
      *>                     as that defined for identifier-1" — so GRP(1:7) and NAMES(8:7) are alphanumeric too.
      *>   §8.4.3.1.3 SR1    identifier is defined recursively: identifier-1 "may be any of the formats for an
      *>                     identifier", so a function-identifier of category alphanumeric is identifier-1 as well.
      *>   §8.4.3.12.4 GR1 a) the function is the one named by "the content of the data item referenced by
      *>                     identifier-1" — for a group, its character image.
      *> A reference to the function named by every spelling must equal the address of the function itself (FPREF, the
      *> prototype arm of the same identifier, §8.4.3.12.4 GR1 b)), which is what each IF below tests.
      *> EXPECTED OUTPUT, derived line by line (PB1417F is the function; every name below spells it):
      *>   GROUP-OK    GRP is G1 "PB1" + G2 "417F" = "PB1417F"
      *>   NATGRP-OK   NG is a GROUP-USAGE NATIONAL group, N(3) "PB1" + N(4) "417F" = national "PB1417F" (§13.18.29.4 GR2)
      *>   REFMOD-OK   NAMES is two PIC X(7) occurrences of "PB1417F", so NAMES(8:7) is the second occurrence's seven
      *>               characters (a reference-modified GROUP is an elementary alphanumeric item, §8.4.3.3.4 GR6)
      *>   FUNC-OK     FUNCTION TRIM(PAD) removes PAD's leading and trailing spaces (§15.96): "PB1417F"
      *>   FOLD-OK     FUNCTION UPPER-CASE("pb1417f") is "PB1417F"
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1417F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 7 TO L-RES
           GOBACK.
       END FUNCTION PB1417F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1417M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1417F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER TO PB1417F.
       01 FPREF USAGE FUNCTION-POINTER TO PB1417F.
       01 GRP.
          05 G1 PIC X(3) VALUE "PB1".
          05 G2 PIC X(4) VALUE "417F".
       01 NG GROUP-USAGE NATIONAL.
          05 N1 PIC N(3) VALUE N"PB1".
          05 N2 PIC N(4) VALUE N"417F".
       01 NAMES.
          05 WS-NAME PIC X(7) OCCURS 2 VALUE "PB1417F".
       01 PAD PIC X(10) VALUE "  PB1417F ".
       PROCEDURE DIVISION.
       MAIN.
           SET FPREF TO ADDRESS OF FUNCTION PB1417F
           SET FP TO ADDRESS OF FUNCTION GRP
           IF FP = FPREF
               DISPLAY "GROUP-OK"
           ELSE
               DISPLAY "GROUP-BAD"
           END-IF
           SET FP TO NULL
           SET FP TO ADDRESS OF FUNCTION NG
           IF FP = FPREF
               DISPLAY "NATGRP-OK"
           ELSE
               DISPLAY "NATGRP-BAD"
           END-IF
           SET FP TO NULL
           SET FP TO ADDRESS OF FUNCTION NAMES(8:7)
           IF FP = FPREF
               DISPLAY "REFMOD-OK"
           ELSE
               DISPLAY "REFMOD-BAD"
           END-IF
           SET FP TO NULL
           SET FP TO ADDRESS OF FUNCTION FUNCTION TRIM(PAD)
           IF FP = FPREF
               DISPLAY "FUNC-OK"
           ELSE
               DISPLAY "FUNC-BAD"
           END-IF
           SET FP TO NULL
           SET FP TO ADDRESS OF FUNCTION FUNCTION UPPER-CASE("pb1417f")
           IF FP = FPREF
               DISPLAY "FOLD-OK"
           ELSE
               DISPLAY "FOLD-BAD"
           END-IF
           STOP RUN.
       END PROGRAM PB1417M.
