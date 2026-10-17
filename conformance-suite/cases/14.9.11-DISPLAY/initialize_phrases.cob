*>--- conformance-case
*> id: initialize_phrases
*> rules: [GR-14.9.11.4-4, GR-14.9.11.4-6, SR-14.9.20.3-8,
*>    GR-14.9.20.4-5, GR-14.9.20.4-6, GR-14.9.20.4-8, FMT-14.9.20.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.20 — COBOL-2002 INITIALIZE phrases: WITH FILLER, {ALL|category-name} TO VALUE,
      *> THEN TO DEFAULT. Per-item precedence: TO VALUE (item's VALUE clause) > REPLACING > default.
      *> ⛔ `ALL TO VALUE` is spelled in full: §14.9.20.2 draws {ALL | category-name} inside a BRACE and
      *> §5.2.6.3 makes one of the two alternatives mandatory, so the bare `TO VALUE` this line used to
      *> carry is NOT conforming source (COBOLNET1981 today; kb/Work PB415). The .out is unchanged —
      *> ALL was always the meaning the omission was silently given.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITPHRASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GRP.
          05 A   PIC X(3) VALUE "AAA".
          05 B   PIC 9(3) VALUE 123.
          05 FILLER PIC X(2) VALUE "FF".
          05 C   PIC X(3).
          05 D   PIC 9(3).
       01 GRPR REDEFINES GRP PIC X(14).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM MESSUP.
           INITIALIZE GRP ALL TO VALUE.
           DISPLAY "TV=[" A "][" B "][" C "][" D "]".
           PERFORM MESSUP.
           INITIALIZE GRP.
           DISPLAY "DF=[" A "][" B "][" C "][" D "]".
           PERFORM MESSUP.
           INITIALIZE GRP ALL TO VALUE THEN TO DEFAULT.
           DISPLAY "VD=[" A "][" B "][" C "][" D "]".
           PERFORM MESSUP.
           INITIALIZE GRP REPLACING NUMERIC DATA BY 7.
           DISPLAY "RP=[" A "][" B "][" C "][" D "]".
           MOVE "XXXXXXXXXXXXXX" TO GRPR.
           INITIALIZE GRP.
           DISPLAY "NOFILL=[" GRPR "]".
           MOVE "XXXXXXXXXXXXXX" TO GRPR.
           INITIALIZE GRP WITH FILLER.
           DISPLAY "FILL=[" GRPR "]".
           STOP RUN.
       MESSUP.
           MOVE "ZZZ" TO A. MOVE 999 TO B.
           MOVE "QQQ" TO C. MOVE 555 TO D.
