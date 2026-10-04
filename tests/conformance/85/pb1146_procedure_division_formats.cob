      *> kb/Work PB1146. ISO 14.2.1 at --std 85: Format 1 (with-sections) is the procedure division header,
      *> an optional DECLARATIVES portion made of sections, then sections only; each section is "[ sentence ]
      *> ... [ paragraph-name-1. [ sentence ] ... ] ..." (rendered from PDF page 557), so a section MAY open
      *> with sentences before its first paragraph (14.4.3: the paragraph-name-omitted paragraph follows "the
      *> procedure division header or a section header"). Format 2 (without-sections), the nested program
      *> below, is sentences then paragraphs and no section at all. 14.4.1: "If one paragraph is in a
      *> section, all paragraphs shall be in sections" - every paragraph of PB1146P is in one. The reject
      *> side is negative/pb1146-*.
      *>
      *> DERIVATION - control falls through in source order (14.6.2), nothing from the compiler.
      *>  1. The DECLARATIVES section is not executed by fall-through (14.9.49.4 GR3 runs it only for its
      *>     USE condition, which never arises: no file is opened), so execution starts at MAIN-S.
      *>  2. MAIN-S's leading sentence runs first: S-LEAD; then its paragraph MAIN-P: S-PARA.
      *>  3. CALL "PB1146Q": Format 2 - the leading sentence Q-LEAD, then paragraph Q-PARA, then EXIT
      *>     PROGRAM returns.
      *>  4. Fall-through into section TAIL-S: T-PARA, then STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1146P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB1146P.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-S SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-P.
           DISPLAY "IN-DECLARATIVES".
       END DECLARATIVES.
       MAIN-S SECTION.
           DISPLAY "S-LEAD".
       MAIN-P.
           DISPLAY "S-PARA".
           CALL "PB1146Q".
       TAIL-S SECTION.
       TAIL-P.
           DISPLAY "T-PARA".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1146Q.
       PROCEDURE DIVISION.
           DISPLAY "Q-LEAD".
       Q-PARA.
           DISPLAY "Q-PARA".
           EXIT PROGRAM.
       END PROGRAM PB1146Q.
       END PROGRAM PB1146P.
