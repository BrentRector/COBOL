*>--- conformance-case
*> id: pb124_alphabetic_class
*> rules: [AR-15.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB124 wave 5 (AR-15.3-1) — class ALPHABETIC is Table 2's own first row, distinct from
      *> alphanumeric. The CLASS-worded rules admit it: UPPER-CASE (15.97.3 r1 "class alphabetic,
      *> alphanumeric, or national"), TRIM (15.96.3 r1; r2's cross rule pairs class-alphabetic argument-1
      *> with an ALPHANUMERIC argument-2 by its own two-block wording), MAX (15.59.3 r2: "mixing of
      *> arguments of alphabetic and alphanumeric classes is allowed" — the standard's own exception), ORD
      *> (15.70.3 r1; 8.4.3.3.4 GR6 keeps the ref-mod's class). The CATEGORY-worded rules reject it — the
      *> negatives pb124-numval-alphabetic / pb124-formatted-alphabetic pin that side. Values hand-derived:
      *> UPPER-CASE("abc") = "ABC"; TRIM strips nothing ('c' guards no edge of "abc  "); MAX picks "zz" over
      *> "abc" in the native collating sequence; ORD("a") = 98 (ORD("A") = 66 convention, 'a' = 97 + 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124AC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AL PIC A(5) VALUE "abc".
       01 R PIC 9(4).
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION UPPER-CASE(AL) TO RS
           IF RS = "ABC     " DISPLAY "UC OK" ELSE DISPLAY "UC BAD [" RS "]"
           END-IF
           MOVE FUNCTION TRIM(AL "c") TO RS
           IF RS = "abc     " DISPLAY "TR OK" ELSE DISPLAY "TR BAD [" RS "]"
           END-IF
           MOVE FUNCTION MAX(AL "zz") TO RS
           IF RS(1:2) = "zz" DISPLAY "MX OK" ELSE DISPLAY "MX BAD [" RS "]"
           END-IF
           COMPUTE R = FUNCTION ORD(AL(1:1))
           IF R = 98 DISPLAY "OR OK" ELSE DISPLAY "OR BAD " R END-IF
           STOP RUN.
