*>--- conformance-case
*> id: pb969_function_pointer_empty_arguments
*> rules: [SR-8.4.3.2.3-2, FMT-8.4.3.2.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2.3 SR5: "If function-pointer-name-1 is specified, the parentheses shall be specified" -
      *> and SR2 lets FUNCTION be omitted before a function-pointer-name. So for a function-pointer to a
      *> function that takes no arguments, `FZ()` is the ONLY keyword-omitted spelling there is: 8.4.3.2.2
      *> brackets the arguments INSIDE the parentheses (PDF p157 / folio 127). kb/Work PB969: it died
      *> COBOL0001 "cannot parse construct near ')'" while `FUNCTION FZ()` parsed.
      *>
      *> FZ is SET to the address of W51P969P (14.9.39 SET format 8, function-pointer-assignment), and
      *> W51P969P returns 77 in its RETURNING item, so both spellings evaluate it   -> P1=77, P2=77
       IDENTIFICATION DIVISION.
       FUNCTION-ID. W51P969P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 77 TO L-RES
           GOBACK.
       END FUNCTION W51P969P.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W51P969F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION W51P969P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FZ USAGE FUNCTION-POINTER TO W51P969P.
       01 R PIC 9(9).
       PROCEDURE DIVISION.
       MAIN-P.
           SET FZ TO ADDRESS OF FUNCTION W51P969P
           COMPUTE R = FUNCTION FZ()
           DISPLAY "P1=" R
           COMPUTE R = FZ()
           DISPLAY "P2=" R
           STOP RUN.
       END PROGRAM W51P969F.
