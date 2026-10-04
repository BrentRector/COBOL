      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1146. ISO 14.2.1: the DECLARATIVES portion is printed only in Format 1 (with-sections), whose
      *> body after END DECLARATIVES is sections; 14.4.1: "If one paragraph is in a section, all paragraphs
      *> shall be in sections" - the declarative section ERR-S makes paragraph P1 a paragraph outside every
      *> section. Refused COBOLNET2797 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1146NC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB1146NC.TMP".
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
       P1.
           DISPLAY "B".
           STOP RUN.
