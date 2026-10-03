      *> kb/Work PB301 - THE SCREEN SURFACE MUST NOT COST THE USER A WORD. Annex A.4.2 (screen handling) is a
      *> DECLINED optional module (docs/CONFORMANCE.md 5), and this compiler declines it by tokenizing the
      *> module's words so the refusal can name itself (COBOLNET1560 / COBOLNET1707) instead of drawing a raw
      *> parse error. A lexer token is not a reservation: ISO 8.3.2.1 rule 3 - "Context-sensitive words may be
      *> used as user-defined words and system-names in contexts other than the language construct in which
      *> they are defined" - so the fifteen 8.10 screen words below are legal user-defined words at EVERY
      *> edition, and 8.9 adds COL, COLS, COLUMNS, CRT, CURSOR and SCREEN only in 2002, which leaves those six
      *> legal COBOL-85 user words too. All twenty-one are declared and REFERENCED here; the reference matters
      *> as much as the declaration, because the reservation gate withdraws a word from `cobolWord` while
      *> `reservedGatedWord` still admits it in the DEFINITION slot, so a program that only declared would not
      *> notice a half-applied gate.
      *> COLUMN is deliberately absent: 8.9 reserves it at every edition (the report-writer COLUMN clause), so
      *> it is barred by 8.3.2.1 rule 1 - tests/conformance/negative/pb301-column-reserved-at-every-edition.
      *> UNDERLINE is declared as a TABLE so the subscript-trigger half of the cobol-words.json row is exercised:
      *> `UNDERLINE (2)` must enter the lexer's SUBSCRIPT mode, which only happens for a word in _dataNameTokens.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB301WORDS85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
      *> CRT here is an ordinary COBOL-85 user-defined word in the SPECIAL-NAMES paragraph: the CRT STATUS
      *> clause of the declined module is gated on the word being RESERVED, so below 2002 it does not compete
      *> for this text. It is the MNEMONIC-NAME of the entry. It used to be the entry's SYSTEM-NAME
      *> (`CRT IS SCR-MNEM`), which compiled only while any word was accepted as one; 12.3.7.3 SR8 makes the
      *> available system-names the implementor's to specify (kb/Work PB862, docs/CONFORMANCE.md section 7,
      *> A.1 items 189-191), and no screen word is among them - 8.3.2.1 rule 3 PERMITS a context-sensitive
      *> word as a system-name, it does not CREATE one. That the '85 text still reaches the implementor-name
      *> arm rather than the screen refusal is pinned by ImplementorNamesDriftTests.
       SPECIAL-NAMES.
           CONSOLE IS CRT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> ISO 8.10 - context-sensitive, "screen description entry" (and, for six of them, the SET attribute
      *> statement): free at every edition.
       01 AUTO             PIC X(3) VALUE "W01".
       01 BACKGROUND-COLOR PIC X(3) VALUE "W02".
       01 BELL             PIC X(3) VALUE "W03".
       01 BLINK            PIC X(3) VALUE "W04".
       01 EOL              PIC X(3) VALUE "W05".
       01 EOS              PIC X(3) VALUE "W06".
       01 ERASE            PIC X(3) VALUE "W07".
       01 FOREGROUND-COLOR PIC X(3) VALUE "W08".
       01 FULL             PIC X(3) VALUE "W09".
       01 HIGHLIGHT        PIC X(3) VALUE "W10".
       01 LOWLIGHT         PIC X(3) VALUE "W11".
       01 REQUIRED         PIC X(3) VALUE "W12".
       01 REVERSE-VIDEO    PIC X(3) VALUE "W13".
       01 SECURE           PIC X(3) VALUE "W14".
       01 W-TAB.
           05 UNDERLINE    PIC X(3) OCCURS 3 TIMES.
      *> ISO 8.9 - reserved from 2002 only, so legal COBOL-85 user-defined words. CRT is the sixth: it is declared
      *> as the MNEMONIC-NAME above and REFERENCED by the DISPLAY ... UPON CRT below - never also as a data-name,
      *> because 8.3.2.2 allows a word only ONE type of user-defined word in a source element.
       01 COL              PIC X(3) VALUE "W16".
       01 COLS             PIC X(3) VALUE "W17".
       01 COLUMNS          PIC X(3) VALUE "W18".
       01 CURSOR           PIC X(3) VALUE "W20".
       01 SCREEN           PIC X(3) VALUE "W21".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "W15" TO UNDERLINE (2).
           DISPLAY AUTO BACKGROUND-COLOR BELL BLINK EOL.
           DISPLAY EOS ERASE FOREGROUND-COLOR FULL HIGHLIGHT.
           DISPLAY LOWLIGHT REQUIRED REVERSE-VIDEO SECURE UNDERLINE (2).
           DISPLAY COL COLS COLUMNS CURSOR SCREEN.
           DISPLAY "W19" UPON CRT.
           STOP RUN.
