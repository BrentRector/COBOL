*>--- conformance-case
*> id: pb695_object_computer_optional_words
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> !! THE OBJECT-COMPUTER PARAGRAPH WITH EVERY OPTIONAL WORD OF BOTH ITS CLAUSES OMITTED (PB695).
      *> ISO 12.3.6.2 prints
      *>     OBJECT-COMPUTER. [ computer-name-1 ]
      *>         [ CHARACTER CLASSIFICATION { IS locale-phrase-1 [ locale-phrase-2 ]
      *>                                    | { FOR ALPHANUMERIC IS locale-phrase-1
      *>                                      | FOR NATIONAL     IS locale-phrase-2 } ... } ]
      *>         [ PROGRAM COLLATING SEQUENCE { IS alphabet-name-1 [ alphabet-name-2 ]
      *>                                      | { FOR ALPHANUMERIC IS alphabet-name-1
      *>                                        | FOR NATIONAL     IS alphabet-name-2 } ... } ] .
      *> and printed folio 285 carries underline rectangles under EXACTLY
      *>     ALPHANUMERIC CLASSIFICATION LOCALE NATIONAL OBJECT-COMPUTER. SEQUENCE SYSTEM-DEFAULT
      *>     USER-DEFAULT
      *> - so CHARACTER, PROGRAM, COLLATING, FOR and IS are all optional words (5.2.3; 8.3.2.4.3 makes
      *> the omission semantics-preserving). Each clause here is written with every one of them left
      *> out, which is the shortest conforming spelling of the paragraph:
      *>     CLASSIFICATION SYSTEM-DEFAULT           (CHARACTER and IS omitted)
      *>     SEQUENCE ALPHANUMERIC IS REV            (PROGRAM, COLLATING and FOR omitted)
      *> Before PB695 the grammar demanded CHARACTER, PROGRAM and FOR; `OBJECT-COMPUTER. SEQUENCE IS
      *> REV.` was a parse error at the word SEQUENCE.
      *> DERIVATION of the expected lines:
      *>  . 12.3.6.4 9): "When the PROGRAM COLLATING SEQUENCE clause is specified, the initial
      *>    alphanumeric program collating sequence is the collating sequence associated with
      *>    alphabet-name-1", and 8.8.4.2.7 compares alphanumeric operands under the alphanumeric
      *>    program collating sequence in effect. AL-REV is declared by the
      *>    12.3.7.2 ALPHABET clause as `"Z" THRU "A"`, whose 12.3.7.4 reading assigns the characters
      *>    of the native set the ordinal positions of that reversed range - so within this program
      *>    "A" collates AFTER "Z" and the first comparison is FALSE while the second is TRUE.
      *>  . The comparison is unaffected by the CHARACTER CLASSIFICATION clause, which governs the
      *>    locale used for class conditions and case conversion (Annex A.4.9 item 7), not the
      *>    program collating sequence. It is written here to prove the CHARACTER-omitted spelling
      *>    reaches the binder as the clause it is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695OBJCOMP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER.
           CLASSIFICATION SYSTEM-DEFAULT
           SEQUENCE ALPHANUMERIC IS AL-REV.
       SPECIAL-NAMES.
           ALPHABET AL-REV IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LOW-CH  PIC X VALUE "A".
       01 HIGH-CH PIC X VALUE "Z".
       PROCEDURE DIVISION.
       MAIN.
           IF LOW-CH < HIGH-CH
               DISPLAY "A-LT-Z=yes"
           ELSE
               DISPLAY "A-LT-Z=no"
           END-IF
           IF HIGH-CH < LOW-CH
               DISPLAY "Z-LT-A=yes"
           ELSE
               DISPLAY "Z-LT-A=no"
           END-IF
           DISPLAY "DONE"
           STOP RUN.
