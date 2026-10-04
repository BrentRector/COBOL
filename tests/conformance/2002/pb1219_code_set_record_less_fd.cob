      *> kb/Work PB1219 - ISO 13.18.13.3 SR3: "If there are record description entries associated with the file
      *>   and no SELECT WHEN clauses are specified, either alphabet-name-1 or alphabet-name-2 may be specified,
      *>   but not both; and: a) if alphabet-name-1 is specified, all elementary data items of all record
      *>   description entries associated with the file shall be described as usage display ... b) if
      *>   alphabet-name-2 is specified, all elementary data items of all record description entries ...
      *>   shall be described as usage national".
      *>   cite.py: OK  13.18.13.3 3)  (Syntax rules)
      *> The whole rule has the antecedent "if there are record description entries". An FD with NONE (legal
      *> for a sequential file, 13.4.5.3 SR3, with a RECORD clause) has none, so neither the not-both sentence
      *> nor a) / b) applies, and 13.18.13.4 GR4's second sentence expressly contemplates it: "If there are no
      *> record description entries associated with the file, the record description used for conversion is
      *> the description of the identifier or literal specified in the FROM phrase".
      *>   cite.py: OK  13.18.13.4 4)  (General rules)
      *> The implied record 14.9.30.4 GR6 gives such a file ("one record description entry describing an
      *> alphanumeric group item of the maximum size established by the RECORD clause") is NOT a record
      *> description entry, so FN (BOTH alphabets) and FP (the FOR form) compile and READ ... INTO proceeds;
      *> FM names only alphabet-name-2 and is merely declared. The data is written through FW, whose record
      *> is alphanumeric DISPLAY under alphabet-name-1 (EBCDIC): FN and FP convert each EBCDIC byte back, so
      *> both read AB012 (13.18.13.4 GR1, GR2, GR6 a). Before the fix FN, FP and FM each drew COBOLNET1672
      *> "the record item 'FILLER' is not usage NATIONAL" about an item the program never wrote.
      *>   cite.py: OK  14.9.30.4 6)  (General rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1219A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-EBC IS EBCDIC
           ALPHABET N-U FOR NATIONAL IS UTF-16.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FW ASSIGN TO "pb1219.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT FN ASSIGN TO "pb1219.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT FP ASSIGN TO "pb1219.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT FM ASSIGN TO "pb1219m.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD FW CODE-SET IS A-EBC RECORD CONTAINS 5 CHARACTERS.
       01 WR PIC X(5).
       FD FN CODE-SET IS A-EBC N-U RECORD CONTAINS 5 CHARACTERS.
       FD FP CODE-SET FOR ALPHANUMERIC IS A-EBC FOR NATIONAL IS N-U
              RECORD CONTAINS 5 CHARACTERS.
       FD FM CODE-SET FOR NATIONAL IS N-U RECORD CONTAINS 5 CHARACTERS.
       WORKING-STORAGE SECTION.
       01 X PIC X(5).
       PROCEDURE DIVISION.
           OPEN OUTPUT FW
           MOVE "AB012" TO WR
           WRITE WR
           CLOSE FW
           OPEN INPUT FN
           READ FN INTO X
           CLOSE FN
           DISPLAY "FN[" X "]"
           MOVE SPACES TO X
           OPEN INPUT FP
           READ FP INTO X
           CLOSE FP
           DISPLAY "FP[" X "]"
           STOP RUN.
