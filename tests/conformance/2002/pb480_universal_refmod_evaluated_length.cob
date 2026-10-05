      *> kb/Work PB480 (train 1021 review D finding 4) - a REFERENCE-MODIFIED argument crossing a UNIVERSAL
      *> dispatch has its EVALUATED length. 8.4.3.3.4 GR5: "Reference modification creates a unique data
      *> item that is a subset of the data item referenced by identifier-1" ... c) "The evaluation of length
      *> specifies the number of bit positions or character positions of the data item to be used in the
      *> operation. ... If length is not specified, the unique data item extends from and includes the
      *> position identified by leftmost-position up to and including the rightmost position" (cite.py
      *> --check 8.4.3.3.4 -> OK 8.4.3.3.4 5) c)). 14.8.2.2 rule 1: "the formal parameter shall be described
      *> with the same number or a smaller number of bytes as the corresponding argument" (OK 14.8.2.2 1)),
      *> and a smaller formal sees the argument's leading positions (14.2.3 GR8, OK 14.2.3 8)). The match
      *> of a slice with a group formal is the reading conformance:2002/pb480_universal_match_relations
      *> pins for its TP leg (kb/Work for the PIC X(n) question: review D finding 5). Before the fix a
      *> non-literal length or an omitted one described the slice as 0 positions: EC-OO-UNIVERSAL.
      *> DERIVATION (X = "ABCDEFGHIJ" before each leg; TG's formal is a 4-character group):
      *>   X(1:6)  6 >= 4                -> TG:ABCD, x=wxyzEFGHIJ
      *>   X(1:N)  N = 6, 6 >= 4         -> TG:ABCD, x=wxyzEFGHIJ
      *>   X(K:)   K = 3, 8 >= 4         -> TG:CDEF, x=ABwxyzGHIJ
      *>   X(1:K)  K = 3, 3 <  4         -> HANDLED=EC-OO-UNIVERSAL, x=ABCDEFGHIJ
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480RL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C480RL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 X  PIC X(10).
       01 N  PIC 9(2) VALUE 6.
       01 K  PIC 9(2) VALUE 3.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE C480RL "NEW" RETURNING U
           MOVE "ABCDEFGHIJ" TO X
           INVOKE U "TG" USING X(1:6)
           DISPLAY "x=" X
           MOVE "ABCDEFGHIJ" TO X
           INVOKE U "TG" USING X(1:N)
           DISPLAY "x=" X
           MOVE "ABCDEFGHIJ" TO X
           INVOKE U "TG" USING X(K:)
           DISPLAY "x=" X
           MOVE "ABCDEFGHIJ" TO X
           INVOKE U "TG" USING X(1:K)
           DISPLAY "x=" X
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB480RL.

       IDENTIFICATION DIVISION.
       CLASS-ID. C480RL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LG1 PIC X(4).
       PROCEDURE DIVISION USING LG.
           DISPLAY "TG:" LG
           MOVE "wxyz" TO LG.
       END METHOD TG.
       END OBJECT.
       END CLASS C480RL.
