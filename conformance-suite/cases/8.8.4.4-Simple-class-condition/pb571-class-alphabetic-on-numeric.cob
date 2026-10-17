*>--- conformance-case
*> id: pb571-class-alphabetic-on-numeric
*> rules: [SR-8.8.4.4.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.4.3 SR4 names THREE categories: "ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, or
      *> class-name-1 shall not be specified if the category of the data item referenced by identifier-1 is
      *> boolean, numeric, or numeric-edited." Only the BOOLEAN one was screened (its sibling negative is
      *> class-alphabetic-on-boolean), so `IF <numeric item> IS ALPHABETIC` compiled and ran; the rule is
      *> version-invariant, so every edition rejects.
      *>
      *> ⛔ THE ALPHABET-NAME ALTERNATIVE IS NOT IN SR4 and must keep compiling over the same operand -
      *> `IF N9 IS <alphabet-name>` is admitted by 8.8.4.4.3 SR3 alone. 85/pb571_class_condition_one_table's
      *> F line is that complement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB571NG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N9 PIC 9(4) VALUE 12.
       PROCEDURE DIVISION.
       MAIN.
           IF N9 IS ALPHABETIC
               DISPLAY "ALPHA"
           END-IF
           STOP RUN.
