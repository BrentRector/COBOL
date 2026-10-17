*>--- conformance-case
*> id: pb664-string-national-mix
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.43.3 SR1, SECOND SENTENCE: "If any one of literal-1, literal-2, identifier-1, identifier-2,
      *> or identifier-3 is of class national, then all shall be of class national."  It names five operands
      *> and was enforced at none of them (kb/Work PB664), so this program compiled clean and transcoded a
      *> national sender into an alphanumeric receiver without a word.
      *> The class comes from the ONE 8.5.2.1 Table-2 reader, so a FIGURATIVE operand can never be the
      *> mismatch: 8.3.3.6.4 GR1 makes it "a national character value" in a context requiring national
      *> characters and an alphanumeric one otherwise.
      *> Class national (PICTURE N / USAGE NATIONAL) is a COBOL-2002 introduction, so 85 rejects the
      *> DECLARATION instead and is not claimed here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB664NEGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NN PIC N(2) VALUE N"AB".
       01 XX PIC X(8).
       PROCEDURE DIVISION.
           MOVE SPACES TO XX
           STRING NN DELIMITED BY SIZE INTO XX
           DISPLAY "XX=[" XX "]"
           STOP RUN.
