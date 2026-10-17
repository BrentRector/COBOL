*>--- conformance-case
*> id: pb64t6_locale_size_ec
*> rules: [GR-13.18.40.5-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB64 T6 — EC-LOCALE-SIZE, the condition's FIRST raise site (ISO 13.18.40.5 r14 b: "If any truncated
      *> character is neither a zero nor a space caused by a suppressed zero, the EC-LOCALE-SIZE exception
      *> condition is set to exist"; Table 13 FATAL; DESIGN-locale-facility risk R5 - a registered EC needs a
      *> golden that FIRES it). BOTH silent arms are pinned too, or the raise site is unfalsified:
      *>   SILENT #1 - SIZE 8 truncates exactly the four suppressed-zero spaces (the r14 b exemption).
      *>   RAISE  #1 - SIZE 7 also truncates the '1', a nonzero digit: the declarative observes the condition and
      *>               the MOVE is interrupted, so S7 keeps its initial spaces.
      *>   SILENT #2 - SIZE 11 truncates only the literal zero digit '0'.
      *>   RAISE  #2 - SIZE 10 also truncates the grouping separator ',' - r14 b is CHARACTER-based, not Table
      *>               13's narrower "digits were truncated" gloss; C10 keeps its initial spaces.
       >>TURN EC-LOCALE-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T6SZ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S8 PIC ZZZZZZ9.99 LOCALE IS US SIZE IS 8.
       01 S7 PIC ZZZZZZ9.99 LOCALE IS US SIZE IS 7.
       01 C11 PIC 9999999.99 LOCALE IS US SIZE IS 11.
       01 C10 PIC 9999999.99 LOCALE IS US SIZE IS 10.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-SIZE.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 1234.50 TO S8
           DISPLAY "SILENT1=[" S8 "]"
           MOVE 1234.50 TO S7
           DISPLAY "AFTER1=[" S7 "]"
           MOVE 1234.50 TO C11
           DISPLAY "SILENT2=[" C11 "]"
           MOVE 1234.50 TO C10
           DISPLAY "AFTER2=[" C10 "]"
           STOP RUN.
