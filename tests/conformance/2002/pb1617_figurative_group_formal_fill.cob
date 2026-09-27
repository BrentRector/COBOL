      *> kb/Work PB1617 - a figurative constant or ALL literal passed BY
      *> CONTENT fills the formal parameter's allocated record, for a
      *> GROUP formal as for an elementary one, in all three activations
      *> that know their formal: CALL ... AS NESTED, a user-defined
      *> function, and INVOKE.
      *> RULE (14.2.3 GR9): for "a program and the NESTED phrase is
      *> specified on the CALL statement", a method or a function, the
      *> allocated record is "a data item with the same description and
      *> the same number of bytes as the formal parameter, where the
      *> maximum length is used if the formal parameter is described as
      *> a variable-occurrence data item", and the argument is moved to
      *> it by "a MOVE statement" (a COMPUTE for a numeric formal).
      *> RULE (8.3.3.6.4 GR2): a figurative constant "in association with
      *> a fixed-length data item" has "the string of characters ...
      *> repeated character by character" to the item's character
      *> positions, then truncated from the right.
      *> RULE (14.8.2.1 NOTE): "A bit group or national group is treated
      *> as an elementary item" - a national group is filled in national
      *> character positions.
      *> RULE (8.3.3.6.3 SR1): "A figurative constant may be used whenever
      *> 'literal' appears in a format" - so INVOKE's literal-2 admits it.
      *> cite.py --check 14.2.3 "where the maximum length is used if the
      *>   formal parameter is described as a variable-occurrence data
      *>   item" -> OK  14.2.3 9)
      *> cite.py --check 8.3.3.6.4 "the string of characters is repeated
      *>   character by character" -> OK  8.3.3.6.4 2)
      *> cite.py --check 14.8.2.1 "A bit group or national group is
      *>   treated as an elementary item" -> OK  14.8.2.1
      *> cite.py --check 8.3.3.6.3 "A figurative constant may be used
      *>   whenever 'literal' appears in a format" -> OK  8.3.3.6.3 1)
      *> DERIVATION (each display brackets the formal):
      *>   G = 01 G. 05 PIC X(2). 05 PIC X(2).  (4 positions)
      *>     ALL "*" -> "****"   ALL "AB" -> "ABAB"   QUOTE -> 4 quotes
      *>   H = 01 H. 05 PIC 9(3). 05 PIC X(3).  (6 positions; a group move
      *>     does not convert, so the numeric leaf takes the fill too)
      *>     ALL "*" -> "******"
      *>   T = 01 T. 05 PIC X(2) OCCURS 1 TO 3 DEPENDING ON CNT, CNT = 2
      *>     outside the group: the record is allocated at the MAXIMUM
      *>     (3 occurrences), so ALL "XY" fills "XYXYXY"; T displays its
      *>     current extent "XYXY", and after MOVE 3 TO CNT "XYXYXY".
      *>   NG = GROUP-USAGE NATIONAL, N(2) + N(1) (3 national positions)
      *>     ALL "*" -> "***"    ZERO -> "000"
      *>   function PB1617F(G-formal) returns the formal as PIC X(4):
      *>     ALL "*" -> "****"   ALL "AB" -> "ABAB"   ZERO -> "0000"
      *>   INVOKE TAKEG (group X(2)+X(2)): ALL "*" -> "****",
      *>     SPACE -> 4 spaces; TAKEX (PIC X(4)): ALL "AB" -> "ABAB";
      *>     TAKEN (PIC 9(3)): ZERO is a COMPUTE of zero -> "000".
      *> (Before the fix a group formal received ONE occurrence, space
      *> padded - "*   " - and INVOKE refused every figurative.)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1617F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-G.
          05 L-GA PIC X(2).
          05 L-GB PIC X(2).
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING L-G RETURNING L-R.
           MOVE L-G TO L-R
           GOBACK.
       END FUNCTION PB1617F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1617F
           CLASS PB1617K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS PIC X(4).
       01 O USAGE OBJECT REFERENCE PB1617K.
       PROCEDURE DIVISION.
           CALL "PB1617G" AS NESTED USING BY CONTENT ALL "*"
           CALL "PB1617G" AS NESTED USING BY CONTENT ALL "AB"
           CALL "PB1617G" AS NESTED USING BY CONTENT QUOTE
           CALL "PB1617H" AS NESTED USING BY CONTENT ALL "*"
           CALL "PB1617T" AS NESTED USING BY CONTENT ALL "XY"
           CALL "PB1617N" AS NESTED USING BY CONTENT ALL "*"
           CALL "PB1617N" AS NESTED USING BY CONTENT ZERO
           MOVE FUNCTION PB1617F(ALL "*") TO WS
           DISPLAY "F1=[" WS "]"
           MOVE FUNCTION PB1617F(ALL "AB") TO WS
           DISPLAY "F2=[" WS "]"
           MOVE FUNCTION PB1617F(ZERO) TO WS
           DISPLAY "F3=[" WS "]"
           INVOKE PB1617K "NEW" RETURNING O
           INVOKE O "TAKEG" USING BY CONTENT ALL "*"
           INVOKE O "TAKEG" USING BY CONTENT SPACE
           INVOKE O "TAKEX" USING BY CONTENT ALL "AB"
           INVOKE O "TAKEN" USING BY CONTENT ZERO
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617G.
       DATA DIVISION.
       LINKAGE SECTION.
       01 G.
          05 GA PIC X(2).
          05 GB PIC X(2).
       PROCEDURE DIVISION USING G.
           DISPLAY "G=[" G "] A=[" GA "] B=[" GB "]"
           GOBACK.
       END PROGRAM PB1617G.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617H.
       DATA DIVISION.
       LINKAGE SECTION.
       01 H.
          05 HN PIC 9(3).
          05 HX PIC X(3).
       PROCEDURE DIVISION USING H.
           DISPLAY "H=[" H "]"
           GOBACK.
       END PROGRAM PB1617H.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 2.
       LINKAGE SECTION.
       01 T.
          05 TE PIC X(2) OCCURS 1 TO 3 DEPENDING ON CNT.
       PROCEDURE DIVISION USING T.
           DISPLAY "T=[" T "]"
           MOVE 3 TO CNT
           DISPLAY "T3=[" T "]"
           GOBACK.
       END PROGRAM PB1617T.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 NG GROUP-USAGE NATIONAL.
          05 NA PIC N(2).
          05 NB PIC N(1).
       PROCEDURE DIVISION USING NG.
           DISPLAY "NG=[" NG "]"
           GOBACK.
       END PROGRAM PB1617N.
       END PROGRAM PB1617M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1617K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LGA PIC X(2).
          05 LGB PIC X(2).
       PROCEDURE DIVISION USING LG.
           DISPLAY "IG=[" LG "]"
           GOBACK.
       END METHOD TAKEG.
       METHOD-ID. TAKEX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(4).
       PROCEDURE DIVISION USING LX.
           DISPLAY "IX=[" LX "]"
           GOBACK.
       END METHOD TAKEX.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9(3).
       PROCEDURE DIVISION USING LN.
           DISPLAY "IN=[" LN "]"
           GOBACK.
       END METHOD TAKEN.
       END OBJECT.
       END CLASS PB1617K.
