*>--- conformance-case
*> id: pb81_substitute_table_all
*> rules: [RV-15.87.4-1, RV-15.87.4-2, RV-15.87.4-3, RV-15.87.4-4,
*>    RV-15.87.4-5, FMT-15.87.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB81 - FUNCTION SUBSTITUTE with a table(ALL) argument. ISO 15.3: "When the definition of a function
      *> permits an argument to be repeated a variable number of times, a table may be referenced by ... ALL ...
      *> the effect is as if each table element ... were specified" - SUBSTITUTE's 15.87.2 format repeats the
      *> PAIR `[ANYCASE] [FIRST|LAST] argument-2 argument-3`, so the enumerated elements form the pairs by position
      *> at RUN time, and a keyword before a table(ALL) attaches to the pair its FIRST element opens (the keywords
      *> precede argument-2). Before this landing the argument was the staged COBOLNET0899. Every expected value is
      *> the 15.87.4 result of the written-out call the enumeration stands for:
      *>   P2:  SUBSTITUTE("aXbXc" SB2(ALL))          SB2 = "X","Y"          -> ("X" "Y")            = aYbYc
      *>   P3:  SUBSTITUTE("aXbXc" T3(ALL) "!")        T3 = "X","Y","Z"       -> ("X" "Y")("Z" "!")   = aYbYc
      *>   FIR: SUBSTITUTE("aXbXc" FIRST SB2(ALL))    FIRST attaches to (X->Y)                        = aYbXc
      *>   TWO: SUBSTITUTE("abcabc" A2(ALL) B2(ALL))   A2 = "a","b"; B2 = "c","d" -> ("a" "b")("c" "d") = bbdbbd
      *>   MIX: SUBSTITUTE("aXbXc" "X" T3(ALL) "-")    a written "X", the enumeration X Y Z, a written "-":
      *>        five elements -> ODD -> EC-ARGUMENT-FUNCTION and the zero-length default (a blank R)
      *>   ODD: SUBSTITUTE("aXbXc" T3(ALL))            three elements -> odd -> EC-ARGUMENT-FUNCTION and the 15.3
      *>        default zero-length result (checking off)
      *>   AC:  SUBSTITUTE("AxBXc" ANYCASE SB2(ALL))   ANYCASE on (X->Y): both x and X replaced           = AYBYc
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB81SUBALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SB2-T.
          05 SB2 PIC X OCCURS 2.
       01 T3-T.
          05 T3 PIC X OCCURS 3.
       01 A2-T.
          05 A2 PIC X OCCURS 2.
       01 B2-T.
          05 B2 PIC X OCCURS 2.
       01 R PIC X(10).
       PROCEDURE DIVISION.
           MOVE "X" TO SB2(1)  MOVE "Y" TO SB2(2)
           MOVE "X" TO T3(1)   MOVE "Y" TO T3(2)   MOVE "Z" TO T3(3)
           MOVE "a" TO A2(1)   MOVE "b" TO A2(2)
           MOVE "c" TO B2(1)   MOVE "d" TO B2(2)
           MOVE FUNCTION SUBSTITUTE("aXbXc" SB2(ALL)) TO R
           DISPLAY "P2=[" R "]"
           MOVE FUNCTION SUBSTITUTE("aXbXc" T3(ALL) "!") TO R
           DISPLAY "P3=[" R "]"
           MOVE FUNCTION SUBSTITUTE("aXbXc" FIRST SB2(ALL)) TO R
           DISPLAY "FIR=[" R "]"
           MOVE FUNCTION SUBSTITUTE("abcabc" A2(ALL) B2(ALL)) TO R
           DISPLAY "TWO=[" R "]"
           MOVE FUNCTION SUBSTITUTE("aXbXc" "X" T3(ALL) "-") TO R
           DISPLAY "MIX=[" R "]"
           MOVE FUNCTION SUBSTITUTE("aXbXc" T3(ALL)) TO R
           DISPLAY "ODD=[" R "]"
           MOVE FUNCTION SUBSTITUTE("AxBXc" ANYCASE SB2(ALL)) TO R
           DISPLAY "AC=[" R "]"
           STOP RUN.
