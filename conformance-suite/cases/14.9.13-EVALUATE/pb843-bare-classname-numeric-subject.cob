*>--- conformance-case
*> id: pb843-bare-classname-numeric-subject
*> rules: [SR-14.9.13.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB843. A BARE class-name selection object is 14.9.13.3 SR5's "class condition without the
      *> identifier", and SR8 splices the selection subject in as that identifier - so the class condition's
      *> own operand rules apply to the SUBJECT. 8.8.4.4.3 SR4: "ALPHABETIC, ALPHABETIC-LOWER,
      *> ALPHABETIC-UPPER, or class-name-1 shall not be specified if the category of the data item referenced
      *> by identifier-1 is boolean, numeric, or numeric-edited." WS-N is numeric. Before PB843 the bare word
      *> bound as identifier-2 and drew "'MY-DIG' is not defined" - the right verdict for the wrong reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB843NEG1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS MY-DIG IS "0" THRU "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-N
               WHEN MY-DIG DISPLAY "DIG"
               WHEN OTHER  DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.
