*>--- conformance-case
*> id: pb843_evaluate_symbol_decided_objects
*> rules: [SR-14.9.13.3-3, SR-14.9.13.3-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB843 - two EVALUATE selection-object shapes the GRAMMAR cannot decide, decided by the
      *> RESOLVED SYMBOL. ISO 8.3.2.2: "Within a source element, a given user-defined word may be used as only
      *> one type of user-defined word" - so an alphabet-name or class-name is never a data-name, and only the
      *> symbol table says which one a word is.
      *>
      *> (1) 14.9.13.2's range-expression ends `[ IN alphabet-name-1 ]`, and 14.9.13.3 SR3 admits it over
      *>     identifiers: "Alphabet-name-1 may be specified only when the literals or identifiers specified in
      *>     the THROUGH phrase are of class alphabetic, alphanumeric, or national". IN is also the
      *>     qualification connective, so `WS-HI IN AL` parses as a qualified reference; it was COBOLNET1639.
      *> (2) 14.9.13.3 SR5: "A selection object is a partial-expression if the leftmost portion of the
      *>     selection object is a relational operator, a class condition without the identifier, ...".
      *>     A BARE class-name / alphabet-name is that class condition; it bound as identifier-2 (COBOLNET1639
      *>     "not defined"), and LEADING a combined object it bound as a stand-alone condition.
      *>     SR8: the object "is treated as though it were specified as condition-2, where condition-2 is the
      *>     conditional expression that results from preceding partial-expression-1 by the selection subject".
      *>
      *> EXPECTED VALUES. ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA": a literal phrase assigns ascending
      *> positions in written order (12.3.7.4 GR7 k), so pos(M)=13, pos(C)=23, pos(A)=25. No PROGRAM
      *> COLLATING SEQUENCE is declared, so with no phrase 14.7.8 rule 2's implementor arm is the NATIVE order.
      *> WS-C = "C".
      *>   A  `WS-LO THRU WS-HI IN AL`, WS-LO = "M", WS-HI = "A": 13 <= 23 <= 25            -> A-IN
      *>      (with no phrase, native "M" > "A" is an inverted range, empty by 14.7.8 rule 2 -> A-OUT)
      *>   B  `"A" THRU WS-H2 IN GRP2` - GRP2 is a GROUP, so IN GRP2 stays a qualifier: WS-H2 OF GRP2 = "D",
      *>      native "A" <= "C" <= "D"                                                       -> B-IN
      *>   C  `"M" THRU WS-H2 IN GRP1 IN AL` - the first IN qualifies, the LAST names the alphabet:
      *>      WS-H2 OF GRP1 = "A", 13 <= 23 <= 25 under AL                                   -> C-IN
      *>   D  WS-X = "5", `WHEN MY-DIG` (CLASS MY-DIG IS "0" THRU "9"): 5 is a member        -> D-DIG
      *>   E  WS-Y = "Q", `WHEN MY-DIG` false, WHEN OTHER                                    -> E-OTHER
      *>   F  WS-Y = "Q", `WHEN NOT MY-DIG` - the class condition without its identifier keeps its own
      *>      [ NOT ] (8.8.4.4.2), so SR8 gives WS-Y IS NOT MY-DIG = true                   -> F-NOT-DIG
      *>   G  WS-Y = "Q", `WHEN AS1` - AS1 IS STANDARD-1; 8.8.4.4.4 GR3 a) makes an alphabet-name class
      *>      condition true when every character is in that alphabet's coded character set; "Q" is ISO 646 -> G-ASCII
      *>   H  WS-Y = "Q", WS-F = "N", `WHEN MY-DIG OR WS-F = "N"` -> WS-Y MY-DIG OR WS-F = "N"
      *>      = false OR true                                                                -> H-OR
      *>   I  WS-X = "5", WS-F = "N", `WHEN MY-DIG AND WS-F = "Y"` = true AND false, WHEN OTHER -> I-OTHER
      *>   J  WS-X ALSO WS-Y, `WHEN MY-DIG ALSO NOT MY-DIG` = true ALSO true                  -> J-BOTH
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB843SYMOBJ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET AS1 IS STANDARD-1
           CLASS MY-DIG IS "0" THRU "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC X VALUE "C".
       01 WS-X PIC X VALUE "5".
       01 WS-Y PIC X VALUE "Q".
       01 WS-F PIC X VALUE "N".
       01 WS-LO PIC X VALUE "M".
       01 WS-HI PIC X VALUE "A".
       01 GRP1.
          05 WS-H2 PIC X VALUE "A".
       01 GRP2.
          05 WS-H2 PIC X VALUE "D".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-C
               WHEN WS-LO THRU WS-HI IN AL DISPLAY "A-IN"
               WHEN OTHER                  DISPLAY "A-OUT"
           END-EVALUATE
           EVALUATE WS-C
               WHEN "A" THRU WS-H2 IN GRP2 DISPLAY "B-IN"
               WHEN OTHER                  DISPLAY "B-OUT"
           END-EVALUATE
           EVALUATE WS-C
               WHEN "M" THRU WS-H2 IN GRP1 IN AL DISPLAY "C-IN"
               WHEN OTHER                        DISPLAY "C-OUT"
           END-EVALUATE
           EVALUATE WS-X
               WHEN MY-DIG DISPLAY "D-DIG"
               WHEN OTHER  DISPLAY "D-OTHER"
           END-EVALUATE
           EVALUATE WS-Y
               WHEN MY-DIG DISPLAY "E-DIG"
               WHEN OTHER  DISPLAY "E-OTHER"
           END-EVALUATE
           EVALUATE WS-Y
               WHEN NOT MY-DIG DISPLAY "F-NOT-DIG"
               WHEN OTHER      DISPLAY "F-DIG"
           END-EVALUATE
           EVALUATE WS-Y
               WHEN AS1   DISPLAY "G-ASCII"
               WHEN OTHER DISPLAY "G-OTHER"
           END-EVALUATE
           EVALUATE WS-Y
               WHEN MY-DIG OR WS-F = "N" DISPLAY "H-OR"
               WHEN OTHER                DISPLAY "H-OTHER"
           END-EVALUATE
           EVALUATE WS-X
               WHEN MY-DIG AND WS-F = "Y" DISPLAY "I-AND"
               WHEN OTHER                 DISPLAY "I-OTHER"
           END-EVALUATE
           EVALUATE WS-X ALSO WS-Y
               WHEN MY-DIG ALSO NOT MY-DIG DISPLAY "J-BOTH"
               WHEN OTHER                  DISPLAY "J-OTHER"
           END-EVALUATE
           STOP RUN.
