*>--- conformance-case
*> id: pb983_range_alphabet_in_optional
*> rules: [FMT-13.18.63.2, FMT-14.9.13.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13.2 (the EVALUATE range-expression) and 13.18.63.2 formats 3 and 5 (the condition-name
      *> VALUE clause) all end a THROUGH range with `[ IN alphabet-name-1 ]`, and in NONE of the three printed
      *> figures is IN underlined (PDF p649 / folio 619 and p546 / folio 516). 5.2.3: optional words "are
      *> shown in uppercase and not underlined in general formats. They may be written to add clarity". So
      *> `WHEN "P" THRU "D" AL` and `88 X VALUE "P" THRU "D" AL` are the IN phrase, with the
      *> meaning 14.7.8 rule 2 gives it: "When the IN alphabet-name phrase is specified, the collating
      *> sequence used for range evaluation is the collating sequence defined by that alphabet". kb/Work
      *> PB983: the EVALUATE spelling died COBOLNET2072 and the VALUE spelling COBOLNET1639.
      *>
      *> NO PROGRAM COLLATING SEQUENCE IS DECLARED, so every leg below is the OPPOSITE of what the native
      *> order answers: a phrase that is parsed but dropped flips a line.
      *>
      *> POSITIONS. `ALPHABET AL IS "Z" THRU "A"` - 12.3.7.4 GR7: the THROUGH set is assigned
      *> "successive ascending position[s]" from literal-1 to literal-2, in either direction, so Z=1 ... A=26:
      *> Q=10, P=11, O=12, M=14, D=23. WS-A holds "M" (position 14 under AL).
      *>
      *>   E1 `WHEN "P" THRU "D" AL`           11 <= 14 <= 23                     -> E1=T (native: "P">"D", empty)
      *>   E2 `WHEN WS-LO THRU WS-HI AL`       the same bounds as identifiers     -> E2=T
      *>   E3 `WHEN "P" THROUGH "D" AL ALSO "Q" THRU "M" AL` - both ranges hold 14 (10..14 includes its end
      *>      value, 14.7.8 rule 2 "up to and including")                        -> E3=T (native: both empty)
      *>   E4 `WHEN "D" THRU "P"`, no phrase - the implementor arm (the native sequence here); the CONTROL
      *>                                                                          -> E4=T
      *>   V1 `88 VALUE "P" THRU "D" AL`                                          -> V1=T
      *>   V2 `88 VALUES "Q" THRU "O", "P" THRU "D" AL` - the bracket stands outside the repeated literal
      *>      group, so ONE alphabet orders both ranges: 10..12 misses 14, 11..23 holds it -> V2=T
      *>   V3 `88 VALUE "P" THRU "D" AL WHEN SET TO FALSE IS "Q"` - the printed order: the IN phrase, then the
      *>      FALSE line                                                          -> V3=T
      *>   V4 `88 VALUE "P" THRU K-D IN AL`, K-D a constant-name for "D" (13.10.3 SR2 - a constant-name "may
      *>      be used anywhere that a format specifies a literal"): IN after a WORD, which only the symbol
      *>      tells from a qualifier (8.3.2.2 - one word, one type)               -> V4=T
      *>   V5 `88 VALUE "P" THRU K-D AL` - the same with IN omitted              -> V5=T
      *> Then 13.18.63.4 GR20: "the value of literal-4 from the FALSE phrase is placed in the associated
      *> conditional-variable" by `SET V3 TO FALSE`                              -> WS-A=Q
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB983RNGINOPT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-D CONSTANT AS "D".
       01 WS-LO PIC X VALUE "P".
       01 WS-HI PIC X VALUE "D".
       01 WS-A PIC X VALUE "M".
           88 V1 VALUE "P" THRU "D" AL.
           88 V2 VALUES "Q" THRU "O", "P" THRU "D" AL.
           88 V3 VALUE "P" THRU "D" AL WHEN SET TO FALSE IS "Q".
           88 V4 VALUE "P" THRU K-D IN AL.
           88 V5 VALUE "P" THRU K-D AL.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-A
               WHEN "P" THRU "D" AL DISPLAY "E1=T"
               WHEN OTHER           DISPLAY "E1=F"
           END-EVALUATE
           EVALUATE WS-A
               WHEN WS-LO THRU WS-HI AL DISPLAY "E2=T"
               WHEN OTHER               DISPLAY "E2=F"
           END-EVALUATE
           EVALUATE WS-A ALSO WS-A
               WHEN "P" THROUGH "D" AL ALSO "Q" THRU "M" AL
                   DISPLAY "E3=T"
               WHEN OTHER
                   DISPLAY "E3=F"
           END-EVALUATE
           EVALUATE WS-A
               WHEN "D" THRU "P" DISPLAY "E4=T"
               WHEN OTHER        DISPLAY "E4=F"
           END-EVALUATE
           IF V1 DISPLAY "V1=T" ELSE DISPLAY "V1=F" END-IF
           IF V2 DISPLAY "V2=T" ELSE DISPLAY "V2=F" END-IF
           IF V3 DISPLAY "V3=T" ELSE DISPLAY "V3=F" END-IF
           IF V4 DISPLAY "V4=T" ELSE DISPLAY "V4=F" END-IF
           IF V5 DISPLAY "V5=T" ELSE DISPLAY "V5=F" END-IF
           SET V3 TO FALSE
           DISPLAY "WS-A=" WS-A
           STOP RUN.
