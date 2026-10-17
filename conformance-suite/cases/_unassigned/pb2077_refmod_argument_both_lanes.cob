*>--- conformance-case
*> id: pb2077_refmod_argument_both_lanes
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2077 (decision PB3098) - a REFERENCE-MODIFIED argument to a METHOD gets ONE verdict on
      *> the typed lane (an object reference described with a class-name: compile time) and the universal
      *> lane (run time). 14.9.23.4 GR7 b) locates the method for EVERY INVOKE by 9.3.6 (OK 14.9.23.4 7)),
      *> and 14.9.23.3 SR5 c) with 14.8.1 asks a typed receiver's conformance at compile time
      *> (OK 14.9.23.3 5) c); OK 14.8.1). 8.4.3.3.4 GR5/GR6: "Reference modification creates a unique data
      *> item that is a subset of the data item referenced by identifier-1" (OK 8.4.3.3.4 5)); "The unique
      *> data item is considered to be an elementary data item without the JUSTIFIED clause" (OK 8.4.3.3.4
      *> 6)) - class, category and usage, and no PICTURE clause. 9.3.6 match rule 3 e): "the corresponding
      *> parameter has the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE,
      *> SIGN, and USAGE clauses" (OK 9.3.6 3)): so BY REFERENCE a slice matches only a formal with none of
      *> them - an alphanumeric group - and 14.8.2.2 rule 1 bounds the group: "the formal parameter shall be
      *> described with the same number or a smaller number of bytes as the corresponding argument"
      *> (OK 14.8.2.2 1)). A whole PIC X(10) never matches a group formal (3 e)). BY CONTENT the slice is a
      *> MOVE sender (9.3.6 match rule 4 d), OK 9.3.6 4) d)). An ANY LENGTH formal's "length is considered
      *> to match the length of the corresponding argument" (OK 14.8.2.3.2 2) d); D-RM1 b)), its PICTURE
      *> clause still does not; GR7 c) bans an ANY LENGTH formal through a universal receiver
      *> (OK 14.9.23.4 7) c)). 9.3.6 6): "otherwise, the EC-OO-METHOD exception condition is set to exist"
      *> (OK 9.3.6 6)). A slice whose length is a run-time value takes rule 1 on its MAXIMUM extent at
      *> compile time (docs/CONFORMANCE.md D-RM1 a)): the typed lane binds X(3:N) and its formal holds the
      *> slice padded to the formal, written back through the slice; the universal lane measures the
      *> evaluated length. The typed refusals are the negatives pb2077-refmod-into-elementary-by-reference,
      *> pb2077-refmod-shorter-than-group-formal and pb2077-elementary-into-group-formal.
      *> DERIVATION (X = "ABCDEFGHIJ" before each leg; TG's formal LG is a 4-character group, TP's LP is
      *> PIC X(5), TA's LA is PIC X ANY LENGTH; T is typed, U universal):
      *>   T TG X(1:6)             group, 4 <= 6          -> TG:ABCD|  x=wxyzEFGHIJ
      *>   T TG X(3:N) N=6         group, max 8 >= 4      -> TG:CDEF|  x=ABwxyzGHIJ
      *>   T TG X(3:N) N=2         binds; formal = "CD  " -> TG:CD  |  x=ABwxEFGHIJ
      *>   T TG BY CONTENT X(2:6)  MOVE to a group        -> TG:BCDE|  x=ABCDEFGHIJ
      *>   T TP BY CONTENT X(1:5)  MOVE to PIC X(5)       -> TP:ABCDE|
      *>   T TA Y (PIC X(4))       d): length matches     -> TA:MNOP|  y=q
      *>   G is a 10-character GROUP (ABCDEFGHIJ): its slices are measured as X's are (kb/Work PB2077 R1-1)
      *>   T TG G(1:N) N=6         group, max 10 >= 4     -> TG:ABCD|  g=wxyzEFGHIJ
      *>   T TG G(3:)              group, 8 >= 4          -> TG:CDEF|  g=ABwxyzGHIJ
      *>   U TP X(1:5)             3 e): no PICTURE       -> HANDLED=EC-OO-METHOD
      *>   U TP X(1:N) N=6         3 e)                   -> HANDLED=EC-OO-METHOD
      *>   U TA X(1:5)             3 e): no PICTURE       -> HANDLED=EC-OO-METHOD
      *>   U TG X(1:6)             group, 4 <= 6          -> TG:ABCD|  x=wxyzEFGHIJ
      *>   U TG X(1:3)             rule 1: 4 > 3          -> HANDLED=EC-OO-UNIVERSAL  x=ABCDEFGHIJ
      *>   U TG X(3:N) N=2         rule 1 on 2            -> HANDLED=EC-OO-UNIVERSAL  x=ABCDEFGHIJ
      *>   U TG X                  3 e): PICTURE one side -> HANDLED=EC-OO-METHOD
      *>   U TA Y                  matches; GR7 c)        -> HANDLED=EC-OO-UNIVERSAL
      *>   U TG G(1:N) N=6         group, 6 >= 4          -> TG:ABCD|  g=wxyzEFGHIJ
      *>   U TG G(3:)              group, 8 >= 4          -> TG:CDEF|  g=ABwxyzGHIJ
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2077RM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2077RM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T  USAGE OBJECT REFERENCE C2077RM.
       01 U  USAGE OBJECT REFERENCE.
       01 X  PIC X(10).
       01 Y  PIC X(4).
       01 N  PIC 9(2).
       01 G.
          05 G1 PIC X(5).
          05 G2 PIC X(5).
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
           INVOKE C2077RM "NEW" RETURNING T
           SET U TO T
           DISPLAY "-- typed"
           MOVE "ABCDEFGHIJ" TO X
           INVOKE T "TG" USING X(1:6)
           DISPLAY "x=" X
           MOVE "ABCDEFGHIJ" TO X
           MOVE 6 TO N
           INVOKE T "TG" USING X(3:N)
           DISPLAY "x=" X
           MOVE "ABCDEFGHIJ" TO X
           MOVE 2 TO N
           INVOKE T "TG" USING X(3:N)
           DISPLAY "x=" X
           MOVE "ABCDEFGHIJ" TO X
           INVOKE T "TG" USING BY CONTENT X(2:6)
           DISPLAY "x=" X
           INVOKE T "TP" USING BY CONTENT X(1:5)
           MOVE "MNOP" TO Y
           INVOKE T "TA" USING Y
           DISPLAY "y=" Y
           MOVE "ABCDEFGHIJ" TO G
           MOVE 6 TO N
           INVOKE T "TG" USING G(1:N)
           DISPLAY "g=" G
           MOVE "ABCDEFGHIJ" TO G
           INVOKE T "TG" USING G(3:)
           DISPLAY "g=" G
           DISPLAY "-- universal"
           MOVE "ABCDEFGHIJ" TO X
           INVOKE U "TP" USING X(1:5)
           MOVE 6 TO N
           INVOKE U "TP" USING X(1:N)
           INVOKE U "TA" USING X(1:5)
           INVOKE U "TG" USING X(1:6)
           DISPLAY "x=" X
           MOVE "ABCDEFGHIJ" TO X
           INVOKE U "TG" USING X(1:3)
           DISPLAY "x=" X
           MOVE 2 TO N
           INVOKE U "TG" USING X(3:N)
           DISPLAY "x=" X
           INVOKE U "TG" USING X
           MOVE "MNOP" TO Y
           INVOKE U "TA" USING Y
           MOVE "ABCDEFGHIJ" TO G
           MOVE 6 TO N
           INVOKE U "TG" USING G(1:N)
           DISPLAY "g=" G
           MOVE "ABCDEFGHIJ" TO G
           INVOKE U "TG" USING G(3:)
           DISPLAY "g=" G
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB2077RM.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2077RM INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C2077RM.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LG1 PIC X(4).
       PROCEDURE DIVISION USING LG.
           DISPLAY "TG:" LG "|"
           MOVE "wxyz" TO LG.
       END METHOD TG.
       METHOD-ID. TP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP PIC X(5).
       PROCEDURE DIVISION USING LP.
           DISPLAY "TP:" LP "|".
       END METHOD TP.
       METHOD-ID. TA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X ANY LENGTH.
       PROCEDURE DIVISION USING LA.
           DISPLAY "TA:" LA "|"
           MOVE "q" TO LA.
       END METHOD TA.
       END OBJECT.
       END CLASS C2077RM.
