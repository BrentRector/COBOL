      *> reject-at: 2023
      *> ISO 15.28.3 rule 1: "Argument-1 is optional and when specified shall be the name of a file connector
      *> that is specified in an FD statement." TF is SELECTed but has no FD entry at all - it is not "specified in an
      *> FD statement". (OTHER-FILE is an unrelated FD so the FILE SECTION is well-formed.) Before PB63 the resolver
      *> matched the bare name only, so
      *> this compiled clean and answered rule 2a's two spaces. The file-connector-name argument form is 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB63EFNOFD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TF ASSIGN TO "pb63neg.dat".
       DATA DIVISION.
       FILE SECTION.
       FD OTHER-FILE.
       01 OREC PIC X(5).
       WORKING-STORAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION.
           COMPUTE L = FUNCTION LENGTH(FUNCTION EXCEPTION-FILE(TF))
           STOP RUN.
