      *> kb/Work PB1567 - a STRING statement with its DELIMITED phrase omitted, at the provisional introducing
      *> edition COBOL-2002 (VCR row 7.31; constructs.json string-delimited-omitted-2002; below 2002 it is
      *> COBOLNET0900, negative pb1567-string-delimited-omitted-below-2002).
      *>   cite.py --check 14.9.43.3 "The DELIMITED phrase may be omitted only immediately preceding the INTO
      *>     phrase. If it is omitted, DELIMITED BY SIZE is implied" -> OK §14.9.43.3 9)
      *>   cite.py --check 14.9.43.4 "the entire content of literal-1, or the content of the data item
      *>     referenced by identifier-1, is transferred" -> OK §14.9.43.4 3) c)
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES:
      *>   B=[ABCABC]    STRING A A INTO B: no DELIMITED phrase at all, so BY SIZE is implied for the whole
      *>                 sending group and both copies of "ABC" are transferred.
      *>   C=[AABC    ]  STRING A DELIMITED BY "B" A INTO C: the first A stops before its "B" (3 b)), the
      *>                 trailing A has no phrase of its own and moves whole under the implied SIZE; the
      *>                 positions STRING did not write keep their spaces (GR7).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1567S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) VALUE "ABC".
       01 B PIC X(6) VALUE SPACES.
       01 C PIC X(8) VALUE SPACES.
       PROCEDURE DIVISION.
           STRING A A INTO B
           DISPLAY "B=[" B "]"
           STRING A DELIMITED BY "B" A INTO C
           DISPLAY "C=[" C "]"
           STOP RUN.
