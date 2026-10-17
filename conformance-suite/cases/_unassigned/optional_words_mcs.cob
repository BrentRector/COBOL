*>--- conformance-case
*> id: optional_words_mcs
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 5.2.3 — an uppercase word printed WITHOUT an underline is an OPTIONAL WORD: it may be written or
      *> omitted with no change of meaning. Measured off the printed pages rather than assumed, by reading the
      *> underline rectangles per word (scripts/spec/figure_extract.py):
      *>   p732 RECEIVE — RECEIVE, GIVING, CONTINUE, MESSAGE, RECEIVED carry underline rules; FROM carries NONE.
      *>   p756 SEND    — SEND, FROM, RETURNING, RAISING carry rules; TO carries none (both send formats).
      *> The grammar required both tokens, so each line below was a parse error. Sibling of the ON fix
      *> (DEVLOG 1041): same notation rule, different statements, found by the same measurement.
      *> MCS is a recognize-and-name facility here (§4.2.6 ¶3 — see wave_h_facilities_inert), so the observable
      *> behaviour is that the statements COMPILE and are INERT; this golden pins the SYNTAX being accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPTWORDSMCS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG-TAG   PIC X(16) VALUE "TAG".
       01 MSG-BODY  PIC X(32) VALUE "BODY".
       01 MSG-LEN   PIC 9(4)  VALUE 0.
       01 COUNTER   PIC 9(4)  VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
      *> FROM omitted on RECEIVE
           RECEIVE MSG-TAG GIVING MSG-BODY MSG-LEN
           END-RECEIVE.
      *> FROM omitted with the CONTINUE phrase present. AFTER and SECONDS are NOT exercised as omissible:
      *> page 732 prints them without underlines but page 634 - 14.9.9.2, the CONTINUE statement's own defining
      *> format - underlines both, so the standard contradicts itself and the defining occurrence wins.
           RECEIVE MSG-TAG GIVING MSG-BODY MSG-LEN
               CONTINUE AFTER 5 SECONDS
           END-RECEIVE.
      *> TO omitted on SEND, both spellings of the operand
           SEND MSG-TAG FROM MSG-BODY
           END-SEND.
           SEND "LITERAL-TAG" FROM MSG-BODY
           END-SEND.
      *> the optional words remain ACCEPTED when written - omission is a spelling, not a replacement
           RECEIVE FROM MSG-TAG GIVING MSG-BODY MSG-LEN
               CONTINUE AFTER 5 SECONDS
           END-RECEIVE.
           SEND TO MSG-TAG FROM MSG-BODY
           END-SEND.
      *> ordinary control flow around the inert facilities is unaffected
           PERFORM 3 TIMES
               ADD 1 TO COUNTER
           END-PERFORM.
           DISPLAY "COUNT=" COUNTER.
           DISPLAY "END".
           STOP RUN.
