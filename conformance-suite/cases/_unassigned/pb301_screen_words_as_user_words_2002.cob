*>--- conformance-case
*> id: pb301_screen_words_as_user_words_2002
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB301 - the COBOL-2002 leg of the screen-word rule. ISO 8.9 adds COL, COLS, COLUMNS,
      *> CRT, CURSOR and SCREEN at this edition (the reservation gate withdraws them here - the negative
      *> lane tests/conformance/negative/pb301-screen-words-reserved-from-2002 is the same six declared
      *> and rejected at 2002/2014/2023), while 8.10's fifteen screen words are reserved NOWHERE at any
      *> edition, so 8.3.2.1 rule 3 keeps every one of them a legal user-defined word here. The three
      *> post-85 editions agree word for word; they get their own lane because the gate is GENERATED per
      *> edition from reserved-words.json and a per-edition generator fault is exactly what one lane at
      *> 2023 could not see.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB301WORDS2002.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CONSOLE IS SCR-MNEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
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
      *> 8.3.2.1 rule 3 says "user-defined words AND SYSTEM-NAMES", but it PERMITS a context-sensitive word
      *> as a system-name - it does not create one. HIGHLIGHT used to be exercised as the SPECIAL-NAMES entry's
      *> system-name, which compiled only while any word was accepted as one; 12.3.7.3 SR8 makes the available
      *> system-names the implementor's to specify (kb/Work PB862, docs/CONFORMANCE.md section 7, A.1 items
      *> 189-191) and no screen word is among them, so the entry above names the device CONSOLE.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "W15" TO UNDERLINE (2).
           DISPLAY AUTO BACKGROUND-COLOR BELL BLINK EOL.
           DISPLAY EOS ERASE FOREGROUND-COLOR FULL HIGHLIGHT.
           DISPLAY LOWLIGHT REQUIRED REVERSE-VIDEO SECURE UNDERLINE (2).
           STOP RUN.
