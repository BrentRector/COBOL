      *> kb/Work PB2078, the last statements. A RECEIVING object property is identified ONCE -- identifier-3 is evaluated
      *> into one temporary and the GET and the SET both reach that object (ISO 14.6.4; 8.4.3.9.4 GR3) -- at the
      *> moment its statement's rules identify the receiver, and it is SET with its own store, before the statement's
      *> phrase bodies. The shapes below were refused (COBOLNET0899) when identifier-3 was subscripted by a data-name,
      *> and the ON / NOT ON phrases read the property before it was SET.
      *> EXPECTED, derived line by line (each AR(n) starts BAL 0001, NM "ABCDEF"; X is "AAXAAXYZ"):
      *>   1  INSPECT X TALLYING BAL OF AR(I): 4 "A"s added to AR(1)'s 1 (14.9.22.4 GR11) = 0005. INSPECT NM OF AR(I)
      *>      TALLYING I ... REPLACING: identifier-1 is identified once, first (GR6, GR19), so AR(1) although the
      *>      tally makes I 2: one "A" -> I = 2, AR(1) "ZBCDEF"             "INSPECT I=2 0005 ZBCDEF ABCDEF"
      *>   2  STRING "QQ" INTO X WITH POINTER BAL OF AR(1) = 3: positions 3-4 (14.9.43.4 GR6), pointer 5; nothing
      *>      overflows, and NOT ON OVERFLOW reads the stored pointer          "STRING PTR=0005 AAQQAXYZ"
      *>   3  UNSTRING "AAXAAXYZ" DELIMITED BY "X" INTO Y POINTER BAL OF AR(1) = 2 TALLYING IN BAL OF AR(2): "A" (2..2)
      *>      to Y, pointer 4 past the delimiter (14.9.48.4 GR13), tally 1 + 1 (GR14); "AAXYZ" is unexamined with
      *>      every receiving area acted upon, so ON OVERFLOW (GR15 b))    "UNSTRING OVF PTR=0004 TALLY=0002 [A       ]"
      *>   4  UNSTRING "2,XY" DELIMITED BY "," INTO I NM OF AR(I), I = 1: every identifier is identified first
      *>      (14.6.4 7)), so the second area is AR(1) although the first made I 2
      *>                                                                  "UNSTRING I=2 [XY    ]/[ABCDEF]"
      *>   5  CALL "PB2078SB" USING I RETURNING BAL OF AR(I), I = 1: identifier-3 is identified at the beginning of the
      *>      CALL (14.9.4.4 GR3 a)), so AR(1) receives 9 although the called program set I to 2; NOT ON EXCEPTION
      *>      (GR3 i)) reads it after the store                            "CALL I=2 RET=0009"
      *>   6  INVOKE AR(2) "GETN" RETURNING BAL OF AR(I), I = 1 (14.9.23.4 GR7 a), GR8): AR(1) = 7  "INVOKE 0007 0001"
      *>   7  SET I, BAL OF AR(I) TO IX, IX = 2: each identifier-1 is identified immediately before it is changed
      *>      (14.9.39.4 GR2), so I = 2 first and then AR(2)'s BAL = 2   "SET I=2 0007 0002"
      *>   8  PERFORM VARYING BAL OF AR(I) FROM 1 BY 1 UNTIL BAL OF AR(I) > 3: set and augmented each time (14.9.28.4
      *>      GR12), and the UNTIL condition reads each new value      "V0001" "V0002" "V0003" "PERFORM 0004"
      *>   9  SEARCH TE VARYING BAL OF AR(2), BAL = 1, IX = 1: incremented with the search index (14.9.37.4 GR3 b) 2.),
      *>      "A" and "B" fail, "C" matches at IX 3 with BAL 3           "SEARCH 0003"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2078P3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2078RC
           PROPERTY NM
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT TYPEDEF STRONG.
          05 AR USAGE OBJECT REFERENCE PB2078RC OCCURS 2.
       01 R TYPE RT.
       01 I PIC 9.
       01 X PIC X(8) VALUE "AAXAAXYZ".
       01 Y PIC X(8).
       01 TB VALUE "ABC".
          05 TE PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB2078RC "NEW" RETURNING AR(1)
           INVOKE PB2078RC "NEW" RETURNING AR(2)
           MOVE 1 TO I
           INSPECT X TALLYING BAL OF AR(I) FOR ALL "A"
           INSPECT NM OF AR(I) TALLYING I FOR ALL "A"
               REPLACING ALL "A" BY "Z"
           DISPLAY "INSPECT I=" I " " BAL OF AR(1) " " NM OF AR(1)
               " " NM OF AR(2)
           MOVE 3 TO BAL OF AR(1)
           MOVE 1 TO I
           STRING "QQ" DELIMITED BY SIZE INTO X
               WITH POINTER BAL OF AR(I)
               ON OVERFLOW DISPLAY "STRING OVERFLOW"
               NOT ON OVERFLOW DISPLAY "STRING PTR=" BAL OF AR(1) " " X
           END-STRING
           MOVE "AAXAAXYZ" TO X
           MOVE 2 TO BAL OF AR(1)
           UNSTRING X DELIMITED BY "X" INTO Y
               WITH POINTER BAL OF AR(I) TALLYING IN BAL OF AR(2)
               ON OVERFLOW DISPLAY "UNSTRING OVF PTR=" BAL OF AR(1)
                   " TALLY=" BAL OF AR(2) " [" Y "]"
           END-UNSTRING
           MOVE "2,XY" TO Y
           UNSTRING Y DELIMITED BY "," INTO I NM OF AR(I)
           DISPLAY "UNSTRING I=" I " [" NM OF AR(1) "]/["
               NM OF AR(2) "]"
           MOVE 1 TO I
           CALL "PB2078SB" USING I RETURNING BAL OF AR(I)
               NOT ON EXCEPTION DISPLAY "CALL I=" I " RET=" BAL OF AR(1)
           END-CALL
           MOVE 1 TO I
           MOVE 1 TO BAL OF AR(2)
           INVOKE AR(2) "GETN" RETURNING BAL OF AR(I)
           DISPLAY "INVOKE " BAL OF AR(1) " " BAL OF AR(2)
           SET IX TO 2
           MOVE 1 TO I
           SET I, BAL OF AR(I) TO IX
           DISPLAY "SET I=" I " " BAL OF AR(1) " " BAL OF AR(2)
           MOVE 1 TO I
           PERFORM VARYING BAL OF AR(I) FROM 1 BY 1
                   UNTIL BAL OF AR(I) > 3
               DISPLAY "V" BAL OF AR(1)
           END-PERFORM
           DISPLAY "PERFORM " BAL OF AR(1)
           MOVE 1 TO BAL OF AR(2)
           SET IX TO 1
           SEARCH TE VARYING BAL OF AR(2)
               AT END DISPLAY "SEARCH AT END"
               WHEN TE(IX) = "C" DISPLAY "SEARCH " BAL OF AR(2)
           END-SEARCH
           STOP RUN.
       END PROGRAM PB2078P3.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2078SB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 K PIC 9.
       01 RV PIC 9(4).
       PROCEDURE DIVISION USING K RETURNING RV.
           MOVE 2 TO K
           MOVE 9 TO RV
           GOBACK.
       END PROGRAM PB2078SB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2078RC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB2078RC.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM PIC X(6) VALUE "ABCDEF" PROPERTY.
       01 BAL PIC 9(4) VALUE 1 PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. GETN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RV PIC 9(4).
       PROCEDURE DIVISION RETURNING RV.
           MOVE 7 TO RV.
       END METHOD GETN.
       END OBJECT.
       END CLASS PB2078RC.
