      *>--- conformance-case
      *> id: l1c11_program_text_all_spaces
      *> rules: [SR-6.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §6.3.4 3) — a program-text area of all spaces
      *> Rule: "The program-text area may contain: ... 3) All spaces."
      *>   cite.py --check 6.3.4 "All spaces"
      *>   -> OK  §6.3.4 3)  (Program-text area)
      *>   cite.py --check 6.3.6 "A blank line is one that contains only
      *>   space characters between margin C and margin R. A blank line
      *>   may be written as any line of a compilation group."
      *>   -> OK  §6.3.6   (Blank lines)
      *> Lines whose program-text area (columns 8-72) is all spaces are
      *> written in every shape the reference format admits: zero
      *> characters; six spaces (sequence area only); seven spaces
      *> (indicator area blank); a sequence number followed by spaces
      *> to column 72; and a sequence number, spaces to column 72 and
      *> text in the identification area (columns 73-80). They fall
      *> between divisions, between the clauses of one data entry and
      *> between the words of one statement, so each must contribute
      *> nothing (a separator at most) to the program text.
      *> Derivation:
      *>   A=[ABC]     W-A is PIC X(3) VALUE "ABC" (its clauses are
      *>               split by all-space lines).
      *>   B=[12]      W-B is PIC 99 VALUE 12.
      *>   SUM=[15]    ADD 3 TO W-B written across all-space lines.
      *>   BLANKS-OK   the sentence after the last all-space line runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11P.

       ENVIRONMENT DIVISION.
      
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A
       
           PIC X(3)
000500                                                                  
           VALUE "ABC".
       01 W-B PIC 99
000600                                                                  L1C11PIX
           VALUE 12.

       PROCEDURE DIVISION.
      
       MAIN-P.
           DISPLAY "A=[" W-A "]".
           DISPLAY
       
               "B=[" W-B "]".
           ADD
000500                                                                  
               3
000600                                                                  L1C11PIX
               TO W-B.
           DISPLAY "SUM=[" W-B "]".

      
       
000500                                                                  
000600                                                                  L1C11PIX
           DISPLAY "BLANKS-OK".
           STOP RUN.
