*>--- conformance-case
*> id: pb2623_method_is_a_user_word_at_2023
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2623 - ISO §4.2.8 "An implementation shall recognize as reserved words all the COBOL reserved
      *> words specified in 8.9, Reserved words" (cite.py --check 4.2.8 -> OK). The printed §8.9 list of COBOL-2023
      *> reads ... MERGE, MESSAGE-TAG, METHOD-ID, MINUS, MODE ... (PDF printed page 206): METHOD is NOT in it, and
      *> §8.10 does not list it either, so at COBOL-2023 METHOD is a user-defined word (§8.3.2.1 rule 1 bars only
      *> reserved words from that use). It was reserved at 2002/2014 (the negative twin
      *> negative/pb2623-method-reserved-at-2002-2014 pins that) and the compiler wrongly kept it reserved at 2023
      *> on an "OCR omission" claim the printed page refutes.
      *> The END METHOD marker (§10.7.2 general format, cite.py --check 10.7.2 "METHOD, and INTERFACE are
      *> underlined" -> OK) still spells the word, as a keyword in that position: this program therefore ALSO
      *> declares a method and closes it with END METHOD, so the data name and the marker are proven to coexist.
      *> Expected output, derived: the data item METHOD holds "HELLO"; the method PING displays "PINGED".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2623M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB2623.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 METHOD PIC X(5) VALUE "HELLO".
       01 O USAGE OBJECT REFERENCE CPB2623.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY METHOD.
           INVOKE CPB2623 "NEW" RETURNING O.
           INVOKE O "PING".
           STOP RUN.
       END PROGRAM PB2623M.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB2623 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "PINGED".
       END METHOD PING.
       END OBJECT.
       END CLASS CPB2623.
