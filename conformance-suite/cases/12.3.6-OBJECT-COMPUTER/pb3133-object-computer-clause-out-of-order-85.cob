      *>--- conformance-case
      *> id: pb3133-object-computer-clause-out-of-order-85
      *> rules: [FMT-12.3.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3133 - the COBOL-85 OBJECT-COMPUTER paragraph prints
      *>   MEMORY SIZE, PROGRAM COLLATING SEQUENCE and SEGMENT-LIMIT in
      *>   that sequence, each in its own bracket, and no rule of the
      *>   format frees the order, so ISO 5.2.1 binds it: "The words,
      *>   phrases, clauses, punctuation, and operands in each general
      *>   format shall be written in the compilation group in the
      *>   sequence given in the general format". DERIVED, NOT QUOTED:
      *>   no 1985 text is in the repository (VERSION_CHANGE_REFERENCE
      *>   row 7.7). SEGMENT-LIMIT written before MEMORY SIZE is
      *>   COBOLNET2987; it compiled clean at --std 85 before.
      *> cite.py --check 5.2.1 "shall be written in the compilation
      *>   group in the sequence given in the general format" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3133O.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS 10
           MEMORY SIZE 20 WORDS.
       PROCEDURE DIVISION.
           STOP RUN.
