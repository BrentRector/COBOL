      *>--- conformance-case
      *> id: pb830_computer_paragraph_clauses_85
      *> rules: [FMT-12.3.5.2, FMT-12.3.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB830 - the X3.23-1985 SOURCE-COMPUTER and OBJECT-COMPUTER paragraphs, written in the
      *> '85 format's own printed order:
      *>   SOURCE-COMPUTER. computer-name [WITH DEBUGGING MODE].
      *>   OBJECT-COMPUTER. computer-name [MEMORY SIZE integer {WORDS|CHARACTERS|MODULES}]
      *>       [PROGRAM COLLATING SEQUENCE IS alphabet-name] [SEGMENT-LIMIT IS segment-number].
      *> The three '85 clauses ISO 2002 deleted (VCR rows 7.7-7.9) used to be a `~DOT` token SINK behind
      *> the computer-name, which ran to the period: it swallowed arbitrary words at every edition AND
      *> left no place for SEGMENT-LIMIT after PROGRAM COLLATING SEQUENCE, so this LEGAL '85 program was
      *> rejected with COBOL0001.  The clauses are now modelled rules, and the collating sequence named
      *> here must still take effect: under the descending alphabet AL-REV, "A" is HIGHER than "Z"
      *> (PROGRAM COLLATING SEQUENCE governs nonnumeric comparison), so the IF prints REVERSED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB830C85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. GENERIC-BOX WITH DEBUGGING MODE.
       OBJECT-COMPUTER. GENERIC-BOX
           MEMORY SIZE 8000 WORDS
           PROGRAM COLLATING SEQUENCE IS AL-REV
           SEGMENT-LIMIT IS 20.
       SPECIAL-NAMES.
           ALPHABET AL-REV IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LO PIC X VALUE "A".
       01 HI PIC X VALUE "Z".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF LO > HI
               DISPLAY "PB830C85 REVERSED"
           ELSE
               DISPLAY "PB830C85 NATIVE"
           END-IF.
           STOP RUN.
