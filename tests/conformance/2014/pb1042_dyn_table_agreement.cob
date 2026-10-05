      *> kb/Work PB1042 - ISO 14.6.13.2 rule 6: "When the internal format of a dynamic-capacity
      *> table, as defined by the implementor, is not correctly formed or does not agree with the
      *> corresponding OCCURS clause an EC-DATA-INCOMPATIBLE exception condition is set to exist."
      *> A dynamic-capacity table in an EXTERNAL record (8.5.1.9.1 3)) is the one table read through
      *> a description other than the one that built it (13.18.22.4 GR1): PB1042GA builds GT with
      *> 2-character elements and capacity 2.
      *> B: PB1042GB describes GT with 3-character elements -> HANDLED, the MOVE does not complete.
      *> C: PB1042GC describes GT with FROM 3, above the current capacity 2 -> HANDLED.
      *> D: PB1042GD describes GT as PB1042GA does -> no condition, W [CD  ].
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042GA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB1042XG EXTERNAL.
          05 GH PIC X(2).
          05 GT PIC X(2) OCCURS DYNAMIC CAPACITY IN GC.
       PROCEDURE DIVISION.
           MOVE "AB" TO GT(1)
           MOVE "CD" TO GT(2)
           CALL "PB1042GB"
           CALL "PB1042GC"
           CALL "PB1042GD"
           DISPLAY "MAIN-AFTER " GC
           STOP RUN.
       END PROGRAM PB1042GA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042GB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 PB1042XG EXTERNAL.
          05 GH PIC X(2).
          05 GT PIC X(3) OCCURS DYNAMIC CAPACITY IN GC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DI SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       DI-1.
           DISPLAY "B HANDLED " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-B.
           MOVE "...." TO W
           MOVE GT(1) TO W
           DISPLAY "B [" W "]"
           GOBACK.
       END PROGRAM PB1042GB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042GC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 PB1042XG EXTERNAL.
          05 GH PIC X(2).
          05 GT PIC X(2) OCCURS DYNAMIC CAPACITY IN GC FROM 3.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DI SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       DI-1.
           DISPLAY "C HANDLED " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-C.
           MOVE "...." TO W
           MOVE GT(2) TO W
           DISPLAY "C [" W "]"
           GOBACK.
       END PROGRAM PB1042GC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042GD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 PB1042XG EXTERNAL.
          05 GH PIC X(2).
          05 GT PIC X(2) OCCURS DYNAMIC CAPACITY IN GC.
       PROCEDURE DIVISION.
           MOVE "...." TO W
           MOVE GT(2) TO W
           DISPLAY "D [" W "]"
           GOBACK.
       END PROGRAM PB1042GD.
