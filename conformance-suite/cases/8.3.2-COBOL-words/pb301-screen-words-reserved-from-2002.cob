*>--- conformance-case
*> id: pb301-screen-words-reserved-from-2002
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB301 - the REJECTING arm of the screen-word rule, and the reason the fix is a nameSlot row and
      *> not an unconditional admission. ISO 8.9 reserves COL, COLS, COLUMNS, CRT, CURSOR and SCREEN from
      *> COBOL-2002, and 8.3.2.1 rule 1 - "Reserved words shall not be used as user-defined words or
      *> system-names" - therefore bars every one of them from the slot below at 2002/2014/2023. The SAME six
      *> declarations are CONFORMING at COBOL-85, where 8.9 does not reserve them; that acceptance lane is
      *> tests/conformance/85/pb301_screen_words_as_user_words.
      *> WHAT THIS PINS THAT "is rejected" DOES NOT: the DIAGNOSTIC. The generated reservation gate withdraws
      *> each word from `cobolWord` at the editions 8.9 reserves it and `reservedGatedWord` re-admits it in the
      *> DEFINITION slot alone, so the entry still PARSES and the 8.9 funnel answers with COBOLNET0901 naming
      *> the rule. Without that second half the user gets COBOL0001 "no viable alternative", which names
      *> nothing and sends them looking for a syntax error that is not there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB301RESERVED2002.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
      *> 8.3.2.1 rule 1 bars a reserved word from BOTH slots it names - "user-defined words OR
      *> SYSTEM-NAMES" - so the implementor-name of a 12.3.7 SPECIAL-NAMES entry is refused too. CRT
      *> without STATUS is not the declined CRT STATUS clause (12.3.7 spells that CRT STATUS IS), so this
      *> is the ordinary implementor-name entry the COBOL-85 lane compiles - and the same text at 2002+
      *> must draw COBOLNET0901, not the screen module's COBOLNET1560.
       SPECIAL-NAMES.
           CRT IS SCR-MNEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COL     PIC X(3) VALUE "W16".
       01 COLS    PIC X(3) VALUE "W17".
       01 COLUMNS PIC X(3) VALUE "W18".
       01 CRT     PIC X(3) VALUE "W19".
       01 CURSOR  PIC X(3) VALUE "W20".
       01 SCREEN  PIC X(3) VALUE "W21".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY COL COLS COLUMNS CRT CURSOR SCREEN.
           STOP RUN.
