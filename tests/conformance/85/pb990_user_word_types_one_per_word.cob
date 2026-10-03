      *> kb/Work PB990 - ISO 8.3.2.2: "Within a source element, a given user-defined word may be used as only one type
      *> of user-defined word with the following exceptions: ... 3) the same name may be used as any of the following
      *> types of user-defined words: constant-name, data-name, property-name, record-key-name, record-name"
      *> (cite.py --check 8.3.2.2 "the same name may be used as any of the following types of user-defined words").
      *> This is the positive side of the one-type-per-word census (the refusals are the negative corpus, pb990-*):
      *>   KEYFLD - the RECORD KEY clause's record-key-name AND the data-name of the record's key item: exception 3
      *>            lets them be one word, and it is the ordinary shape of every indexed file.
      *>   H, H2  - two DECLARATIVES sections. H has a sentence after its USE sentence (the paragraph-name-OMITTED
      *>            paragraph of 14.4.3) and H2 has none: neither declares a PARAGRAPH named like its section, so the
      *>            section-name is the word's one type (the census used to find H declared as both).
      *>   MAIN   - a section whose paragraph is named differently.
      *> Compile + run printing OK proves none of them draws COBOLNET2692.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB990POS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KF ASSIGN TO "PB990KF.DAT" ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL RECORD KEY IS KEYFLD.
           SELECT KF2 ASSIGN TO "PB990K2.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  KF.
       01  KREC.
           05  KEYFLD PIC X(4).
           05  FILLER PIC X(4).
       FD  KF2.
       01  K2R PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON KF.
           DISPLAY "H-RAN".
       H2 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON KF2.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "OK"
           STOP RUN.
