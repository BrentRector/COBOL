*>--- conformance-case
*> id: pb695_code_set_for_omitted
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> !! THE CODE-SET CLAUSE'S OPTIONAL WORDS, OMITTED (kb/Work PB695 family 2).
      *> ISO 13.18.13.2, read off the printed page (PDF p414 / folio 384). The whole general format
      *> carries exactly THREE underlines - CODE-SET, ALPHANUMERIC and NATIONAL. FOR and IS are printed
      *> plain, so 8.3.2.4.3 ("uppercase words that are not underlined are called optional words and may
      *> be specified at the user's option with no effect on the semantics of the format") makes every
      *> spelling below conforming source. COBOL.NET required the FOR and answered COBOL0001.
      *>   . `CODE-SET FOR ALPHANUMERIC IS AL1`  - fully written
      *>   . `CODE-SET ALPHANUMERIC IS AL1`      - FOR omitted   (this program)
      *>   . `CODE-SET ALPHANUMERIC AL1`         - FOR and IS omitted
      *> The 2002 introduction gate is unaffected: VersionConformancePass.VisitCodeSetClause keys on the
      *> codeSetForPhrase SUBRULE, never on ctx.FOR(), so `--std 85` still answers COBOLNET0900 here.
      *> 13.18.13.4 GR2 makes the on-medium coded character set the one alphabet-name-1 references, and
      *> 12.3.7.4 GR7 c) makes STANDARD-1's correspondence to the native set the IDENTITY over the ISO 646
      *> characters - so the write/read round trip is byte-exact and OUT= is the record written.
      *> The file is RECORD sequential (12.4.5.10, edition-invariant), not LINE sequential: kb/Work
      *> PB688 gated ORGANIZATION IS LINE SEQUENTIAL to COBOL-2023, and this program has to compile at
      *> --std 2002 because that is the edition the CODE-SET FOR phrase was introduced at. The clause
      *> under test is CODE-SET; the file organization is only its carrier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695CSFOR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL1 IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb695csfor.dat"
           ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 CODE-SET ALPHANUMERIC IS AL1.
       01  R1              PIC X(6).
       WORKING-STORAGE SECTION.
       01  DONE-FLAG       PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1
           MOVE "HELLO!" TO R1
           WRITE R1
           CLOSE F1
           MOVE SPACES TO R1
           OPEN INPUT F1
           READ F1 AT END MOVE "Y" TO DONE-FLAG END-READ
           CLOSE F1
           DISPLAY "OUT=" R1
           DISPLAY "EOF=" DONE-FLAG
           DISPLAY "DONE"
           STOP RUN.
