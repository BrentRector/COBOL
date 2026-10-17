*>--- conformance-case
*> id: pb461_all_literal_value_categories
*> rules: [GR-14.9.39.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB461 - the SAME level-88 round trip as tests/conformance/85/pb461_all_literal_value_roundtrip_85,
      *> over the categories COBOL-2002 introduced. ISO 8.3.3.6.3 SR2 admits an alphanumeric, boolean OR national
      *> literal-1 in Format 6, so the ALL-literal rule has to reach all three; 8.3.3.6.4 GR2 sizes each in the
      *> conditional variable's OWN positions (national positions for a PIC N item, boolean positions for a PIC 1
      *> item), and 14.9.39.4 GR6 stores it "according to the rules for the VALUE clause" so that 8.8.4.5.3 GR3
      *> then reads it back true. The expected images are GR2's repeat-then-truncate: ALL N"AB" on N(4) is ABAB,
      *> ALL N"AB" on N(5) is ABABA, ALL B"10" on 1(4) is 1010, ALL B"10" on 1(5) is 10101.
      *>
      *> MEASURED BEFORE the fix: the SET emitter had no Format-6 arm at all, so a national conditional variable
      *> took the glued parse text and printed ALLN, and its own condition was false.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB461-ALL-VALUE-CAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC N(4) VALUE N"ZZZZ".
          88 A-AB VALUE ALL N"AB".
       01 WS-B PIC N(5) VALUE N"ZZZZZ".
          88 B-AB VALUE ALL N"AB".
       01 WS-C PIC 1(4) VALUE B"1111".
          88 C-10 VALUE ALL B"10".
       01 WS-D PIC 1(5) VALUE B"11111".
          88 D-10 VALUE ALL B"10".
       01 WS-E PIC N(4) VALUE N"ZZZZ".
          88 E-SP VALUE ALL SPACES.
       PROCEDURE DIVISION.
           SET A-AB TO TRUE
           DISPLAY "A=[" WS-A "]"
           IF A-AB DISPLAY "A-TRUE" ELSE DISPLAY "A-FALSE" END-IF
           SET B-AB TO TRUE
           DISPLAY "B=[" WS-B "]"
           IF B-AB DISPLAY "B-TRUE" ELSE DISPLAY "B-FALSE" END-IF
           SET C-10 TO TRUE
           DISPLAY "C=[" WS-C "]"
           IF C-10 DISPLAY "C-TRUE" ELSE DISPLAY "C-FALSE" END-IF
           SET D-10 TO TRUE
           DISPLAY "D=[" WS-D "]"
           IF D-10 DISPLAY "D-TRUE" ELSE DISPLAY "D-FALSE" END-IF
           SET E-SP TO TRUE
           DISPLAY "E=[" WS-E "]"
           IF E-SP DISPLAY "E-TRUE" ELSE DISPLAY "E-FALSE" END-IF
           STOP RUN.
