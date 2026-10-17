      *>--- conformance-case
      *> id: pb571_class_condition_one_table
      *> rules: [SR-8.8.4.4.3-4, SR-8.8.4.4.3-8, FMT-8.8.4.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The class condition's ONE alternative table (ISO 8.8.4.4.2) and its 8.8.4.4.3 operand rules, at the
      *> edition where the class condition, the SPECIAL-NAMES CLASS clause and the ALPHABET clause all
      *> already exist. Every line here is legal COBOL-85 and every line must be ADMITTED - the rejections
      *> the same table now enforces are the negative cases (pb571-class-condition-index-operand,
      *> pb590-boolean-class-numeric-operand, pb590-boolean-class-usage-bit).
      *>
      *> The EVALUATE lines are the point (kb/Work PB590). 14.9.13.4 GR3 e) makes an EVALUATE selection
      *> subject's class test the SAME 8.8.4.4 class condition as the IF spelling, and it used to be parsed
      *> by a private second alternative list that offered ALPHANUMERIC - which 8.8.4.4.2 does not print at
      *> all - and omitted class-name-1 and alphabet-name-1. So `EVALUATE X IS DIGIT` did not parse while
      *> `IF X IS DIGIT` did, over the same declaration. One list, one binder body, and the two spellings
      *> can no longer mean different things.
      *>
      *> EXPECTED VALUES, FROM 8.8.4.4.4 GR3:
      *>   A - GR3 n) 1. a: N9 is category numeric, usage display, content "0012", sign absent and the
      *>       description unsigned                                                             => TRUE
      *>   B - GR3 b) 2.: XA is "AB  ", uppercase letters and space, no locale in effect         => TRUE
      *>   C - GR3 f): DIGIT is "0" THROUGH "9"; XA's content is not all digits                  => FALSE
      *>   D - GR3 f) over XD "0012", every character listed in DIGIT                            => TRUE
      *>   E - the EVALUATE spelling of D                                                        => TRUE
      *>   F - GR3 a): ALPHA is the coded character set STANDARD-1; XA's characters are in it     => TRUE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB571TBL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALPHA IS STANDARD-1
           CLASS DIGIT IS "0" THROUGH "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N9 PIC 9(4) VALUE 12.
       01 XA PIC X(4) VALUE "AB".
       01 XD PIC X(4) VALUE "0012".
       PROCEDURE DIVISION.
       MAIN.
           IF N9 IS NUMERIC DISPLAY "A=T" ELSE DISPLAY "A=F" END-IF
           IF XA IS ALPHABETIC DISPLAY "B=T" ELSE DISPLAY "B=F" END-IF
           IF XA IS DIGIT DISPLAY "C=T" ELSE DISPLAY "C=F" END-IF
           IF XD IS DIGIT DISPLAY "D=T" ELSE DISPLAY "D=F" END-IF
           EVALUATE XD IS DIGIT
               WHEN TRUE DISPLAY "E=T"
               WHEN OTHER DISPLAY "E=F"
           END-EVALUATE
           IF XA IS ALPHA DISPLAY "F=T" ELSE DISPLAY "F=F" END-IF
           STOP RUN.
