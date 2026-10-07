      *> ISO 14.9.13.2's range-expression `[ IN alphabet-name-1 ]` + 14.9.13.3 SR3 + 14.7.8 rule 2, over BOTH
      *> clauses the phrase is printed in - EVALUATE's selection object and the VALUE clause's condition-name
      *> list (13.18.63.2 formats 3 and 5) - because 14.7.8 opens by governing them in one sentence: "This
      *> specification applies to THROUGH phrases specified in the VALUE clause and the EVALUATE statement."
      *> 14.7.8 rule 2: with no phrase "the collating sequence is defined by the implementor" (this compiler's
      *> default is the PROGRAM COLLATING SEQUENCE); "when the IN alphabet-name phrase is specified, the
      *> collating sequence used for range evaluation is the collating sequence defined by that alphabet".
      *> kb/Work PB398: the EVALUATE phrase had NO GRAMMAR (a raw parse error) and the VALUE clause's twin was
      *> PARSED AND DROPPED - a silent wrong answer.
      *>
      *> EXPECTED VALUES. Under ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA" the literal phrase assigns
      *> ascending positions in written order (12.3.7.4 GR7 k), so position(Z)=0 ... position(A)=25; hence
      *> position(M)=13 and position(C)=23. WS-C holds "C". AL is also the PROGRAM COLLATING SEQUENCE, which is
      *> what makes each line a DISCRIMINATOR rather than a coincidence:
      *>   A  `"M" THRU "A" IN AL`   -> 13 <= 23 <= 25                      -> true   -> IN
      *>   B  `"A" THRU "M" IN NAT-A` - NAT-A IS STANDARD-1, whose sequence is the native one (12.3.7.4 GR7 c),
      *>      so rule 2 puts this range under NATIVE order even though the PCS is AL:
      *>      'A'(x41) <= 'C'(x43) <= 'M'(x4D)                              -> true   -> IN
      *>   C  `"A" THRU "M"` with NO phrase -> rule 2's implementor arm = the PCS = AL, where "A"(25) collates
      *>      AFTER "M"(13): the range is inverted and 14.7.8 makes it EMPTY -> false  -> OUT
      *>   D  the VALUE-clause twin, `88 ... VALUE "M" THRU "A" IN AL`      -> true   -> IN
      *>   E  the same set with no phrase, `VALUE "A" THRU "M"`, is inverted under the PCS AL - a sequence the
      *>      compiler KNOWS, so 13.18.63.3 SR26 b) refuses it at compile time (COBOLNET2961, kb/Work PB552);
      *>      that leg lives in conformance:negative/pb552-value-through-not-ascending.
      *>   F  a SINGLETON value in a clause that also names an alphabet is NOT governed by it - 14.7.8 is the
      *>      THROUGH phrase's specification and rule 2 names the sequence "used for RANGE evaluation", while a
      *>      singleton is compared by 8.8.4.5.3 GR2's ordinary relation rules (under the PCS). WS-C = "C" is
      *>      listed as a singleton beside the range "Z" THRU "M" IN AL (0..13), which excludes it (23)
      *>                                                                             -> true   -> YES
      *>   G  the NATIONAL half of SR3 ("if literal-3 or identifier-3 is of class national, alphabet-name-1
      *>      shall reference an alphabet that defines a national collating sequence"): under
      *>      ALPHABET NREV FOR NATIONAL IS N"CBA", position(C)=0 < position(B)=1 < position(A)=2, so the range
      *>      N"C" THRU N"A" contains N"B"                                  -> true   -> IN
      *>   H  the same national range with no phrase, under the NATIVE national sequence (no PROGRAM COLLATING
      *>      SEQUENCE FOR NATIONAL is declared, so D-N3's code-unit identity applies): N"C"(x43) > N"A"(x41),
      *>      an inverted range                                             -> false  -> OUT
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB398INALPH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GENERIC-BOX PROGRAM COLLATING SEQUENCE AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET NAT-A IS STANDARD-1
           ALPHABET NREV FOR NATIONAL IS N"CBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC X VALUE "C".
           88 D-IN-AL     VALUE "M" THRU "A" IN AL.
           88 F-SINGLETON VALUE "C", "Z" THRU "M" IN AL.
       01 WS-NB PIC N VALUE N"B".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-C
               WHEN "M" THRU "A" IN AL     DISPLAY "A=IN"
               WHEN OTHER                  DISPLAY "A=OUT"
           END-EVALUATE
           EVALUATE WS-C
               WHEN "A" THRU "M" IN NAT-A  DISPLAY "B=IN"
               WHEN OTHER                  DISPLAY "B=OUT"
           END-EVALUATE
           EVALUATE WS-C
               WHEN "A" THRU "M"           DISPLAY "C=IN"
               WHEN OTHER                  DISPLAY "C=OUT"
           END-EVALUATE
           IF D-IN-AL     DISPLAY "D=IN"  ELSE DISPLAY "D=OUT"  END-IF
           IF F-SINGLETON DISPLAY "F=YES" ELSE DISPLAY "F=NO"   END-IF
           EVALUATE WS-NB
               WHEN N"C" THRU N"A" IN NREV DISPLAY "G=IN"
               WHEN OTHER                  DISPLAY "G=OUT"
           END-EVALUATE
           EVALUATE WS-NB
               WHEN N"C" THRU N"A"         DISPLAY "H=IN"
               WHEN OTHER                  DISPLAY "H=OUT"
           END-EVALUATE
           STOP RUN.
