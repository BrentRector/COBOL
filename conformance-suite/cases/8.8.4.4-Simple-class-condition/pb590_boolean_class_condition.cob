*>--- conformance-case
*> id: pb590_boolean_class_condition
*> rules: [SR-8.8.4.4.3-3, SR-8.8.4.4.3-5, GR-8.8.4.4.4-2,
*>    FMT-8.8.4.4.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.4.2's class-condition general format prints FOURTEEN alternatives and BOOLEAN is one of
      *> them (verified on the PRINTED page - PDF 224 / printed 194: every keyword alternative underlined,
      *> alphabet-name-1 and class-name-1 not, no choice indicator). 8.8.4.4.4 GR3 e) gives it its truth
      *> value: "If BOOLEAN is specified, the condition is true if the content of the data item referenced
      *> by identifier-1 consists entirely of the boolean values '0' and '1'."
      *>
      *> It was UNIMPLEMENTED (kb/Work PB590): the class-condition grammar offered no BOOLEAN alternative,
      *> so the word fell into the relation condition's identifier slot and drew COBOLNET0901 ("'BOOLEAN' is
      *> a reserved word ... cannot be used as a user-defined word") on conforming source.
      *>
      *> EXPECTED VALUES, FROM THE RULE AND NOT FROM A RUN:
      *>   A - BB holds B"1010", four boolean positions, every one '0' or '1'  => TRUE.
      *>   B - XA is PIC X(4) VALUE "0101". GR3 e) asks about the CONTENT, not the category; 8.8.4.4.3 SR5
      *>       bars only a numeric or numeric-edited operand and SR3 asks only for usage display or
      *>       national, so an alphanumeric item whose four characters are '0'/'1' answers TRUE.
      *>   C - XB is "01X1": position 3 is neither '0' nor '1'                 => FALSE.
      *>   D - the same test with NOT (8.8.4.4.4 GR2, "the truth value is reversed") => TRUE.
      *>   E - the EVALUATE selection subject's own class test (14.9.13.4 GR3 e) names condition-1) is the
      *>       SAME 8.8.4.4 class condition. It used to be parsed by a private second alternative list that
      *>       had no BOOLEAN arm at all; one list, one binder body, so this line answers as line B does.
      *>   F - a SPECIAL-NAMES class-name as an EVALUATE subject's class test (8.8.4.4.4 GR3 f) - the same
      *>       extraction: that private list had no user-defined-word alternative either, so this spelling
      *>       did not parse while the IF spelling did.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB590CLS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES. CLASS DIGIT IS "0" THROUGH "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BB PIC 1(4) VALUE B"1010".
       01 XA PIC X(4) VALUE "0101".
       01 XB PIC X(4) VALUE "01X1".
       PROCEDURE DIVISION.
       MAIN.
           IF BB IS BOOLEAN DISPLAY "A=T" ELSE DISPLAY "A=F" END-IF
           IF XA IS BOOLEAN DISPLAY "B=T" ELSE DISPLAY "B=F" END-IF
           IF XB IS BOOLEAN DISPLAY "C=T" ELSE DISPLAY "C=F" END-IF
           IF XB IS NOT BOOLEAN DISPLAY "D=T" ELSE DISPLAY "D=F" END-IF
           EVALUATE XA IS BOOLEAN
               WHEN TRUE DISPLAY "E=T"
               WHEN OTHER DISPLAY "E=F"
           END-EVALUATE
           EVALUATE XB IS DIGIT
               WHEN TRUE DISPLAY "F=T"
               WHEN OTHER DISPLAY "F=F"
           END-EVALUATE
           STOP RUN.
