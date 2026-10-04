      *> kb/Work PB987 - ISO 8.8.4.13 2): "Values are established for
      *> arithmetic expressions and functions if and when the conditions
      *> containing them are evaluated", and 8.8.4.13 1): a hierarchical
      *> level stops as soon as its truth value is determined. An object
      *> property reference (8.4.3.9.4 1)) is the GET property method
      *> invoked "as though" by an INVOKE, so it is fetched at EACH
      *> evaluation of a repeated condition and NOT AT ALL when the
      *> short-circuit never reaches it. GET P counts its own activations
      *> (1, 2, 3 ...); GET Q reads the count without changing it.
      *>
      *> 1 PERFORM UNTIL P > 3: P answers 1,2,3,4 -> the body runs 3 times.
      *> 2 IF K > 50 AND P ...: the left side is false -> P is not fetched.
      *> 3 IF K = 3 OR P ...:  the left side is true  -> P is not fetched.
      *> 4 EVALUATE TRUE: WHEN P = 99 (P=5), WHEN P = 6 (P=6) matches, the
      *>   third WHEN is never processed (14.9.13.4 4)).
      *> 5 SEARCH WHEN E(IX) = P: P answers 7, 8, 9 against 1, 1, 9 -> 3.
      *> 6 VARYING I FROM 1 BY P: P is fetched at each augment (14.9.28.4
      *>   12)): 10, 11, 12, 13 -> I = 1, 11, 22, 34, 47 -> 4 iterations.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB987M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB987C
           PROPERTY P
           PROPERTY Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE PB987C.
       01 K PIC 99 VALUE 0.
       01 I PIC 99 VALUE 0.
       01 N PIC 99 VALUE 0.
       01 X PIC 9 VALUE 0.
       01 TBL.
          05 E PIC 99 OCCURS 5 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB987C "NEW" RETURNING S.
           PERFORM UNTIL P OF S > 3 OR K > 8
               ADD 1 TO K
           END-PERFORM.
           DISPLAY "UNTIL K=" K " CNT=" Q OF S.
           IF K > 50 AND P OF S > 0
               DISPLAY "AND-TRUE"
           END-IF.
           IF K = 3 OR P OF S > 0
               DISPLAY "OR-TRUE"
           END-IF.
           DISPLAY "SHORT CNT=" Q OF S.
           EVALUATE TRUE
               WHEN P OF S = 99 DISPLAY "EV-1"
               WHEN P OF S = 6 DISPLAY "EV-2"
               WHEN P OF S = 7 DISPLAY "EV-3"
           END-EVALUATE.
           DISPLAY "EVALUATE CNT=" Q OF S.
           MOVE 01 TO E(1).
           MOVE 01 TO E(2).
           MOVE 09 TO E(3).
           MOVE 00 TO E(4).
           MOVE 00 TO E(5).
           SET IX TO 1.
           SEARCH E
               AT END DISPLAY "SEARCH-END"
               WHEN E(IX) = P OF S
                   SET X TO IX
                   DISPLAY "SEARCH IX=" X
           END-SEARCH.
           DISPLAY "SEARCH CNT=" Q OF S.
           PERFORM VARYING I FROM 1 BY P OF S UNTIL I > 40
               ADD 1 TO N
           END-PERFORM.
           DISPLAY "VARYING N=" N " I=" I " CNT=" Q OF S.
           STOP RUN.
       END PROGRAM PB987M.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB987C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-P PIC 99.
       PROCEDURE DIVISION RETURNING LK-P.
       P-P.
           ADD 1 TO CNT.
           MOVE CNT TO LK-P.
           GOBACK.
       END METHOD.
       METHOD-ID. GET PROPERTY Q.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-Q PIC 99.
       PROCEDURE DIVISION RETURNING LK-Q.
       Q-P.
           MOVE CNT TO LK-Q.
           GOBACK.
       END METHOD.
       END OBJECT.
       END CLASS PB987C.
