      *> kb/Work PB1907, docs/CONFORMANCE.md D-INS2. ISO 14.9.22.4 GR13
      *> with GR19 and Annex A.2 item 21 d): a FORMAT 3 INSPECT whose
      *> TALLYING counter shares storage with identifier-1 or with a
      *> REPLACING operand. The overlap makes the result undefined:
      *>   cite.py --check 14.9.22.4 "If identifier-1, identifier-3, or
      *>   identifier-4 occupies the same storage area as identifier-2,
      *>   the result of the execution of this statement is undefined,
      *>   even if they are defined by the same data description entry."
      *>   -> OK 14.9.22.4 13)
      *>   cite.py --check 4.4 "A COBOL run unit that allows these
      *>   situations to happen is a conforming run unit" -> OK 4.4 2)
      *> This golden pins a DOCUMENTED IMPLEMENTOR CHOICE. A format 3
      *> statement is executed literally as GR19 words it:
      *>   cite.py --check 14.9.22.4 "A format 3 INSPECT statement is
      *>   interpreted and executed as though two successive INSPECT
      *>   statements specifying the same identifier-1 had been written"
      *>   -> OK 14.9.22.4 19)
      *> Item identification is done once, before the tallying half
      *> (GR19, last sentence). The tallying half then completes with
      *> its counters stored, and the replacing half takes identifier-1
      *> and its operand values as they stand when IT starts - so the
      *> counter's new value is part of the image that is replaced and
      *> stored back, and is seen by an operand. GnuCOBOL 3.2 emits the
      *> two statements separately and gives the same lines (measured).
      *> EXPECTED:
      *>   F3A  G5 = T(0)+"AAA", TALLYING T FOR ALL "A" REPLACING ALL
      *>        "A" BY "B": the tally stores T=3 ("3AAA"), the
      *>        replacing half turns "3AAA" into "3BBB"      [3BBB]
      *>   F3B  T = "A0A1", C = 0, TALLYING C FOR ALL "A" REPLACING ALL
      *>        C BY "Z": C becomes 2 and the replacing pattern C is
      *>        then "2", which T does not contain       [A0A1|2]
      *>   F3C  the counter is in a REDEFINES of identifier-1's
      *>        record, over a different part (S2RT is S2T, the INSPECT
      *>        item is S2D = "AAXA"): the compiler cannot prove that,
      *>        so it re-reads the image; nothing changes. T=3 and the
      *>        three "A" become "Z"                       [3ZZXZ]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907I3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G5.
          05 G5T PIC 9 VALUE 0.
          05 G5D PIC X(3) VALUE "AAA".
       01 T    PIC X(4) VALUE "A0A1".
       01 C    PIC 9 VALUE 0.
       01 S2.
          05 S2T PIC 9 VALUE 0.
          05 S2D PIC X(4) VALUE "AAXA".
       01 S2R REDEFINES S2.
          05 S2RT PIC 9.
          05 S2RD PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           INSPECT G5 TALLYING G5T FOR ALL "A"
                      REPLACING ALL "A" BY "B"
           DISPLAY "F3A=[" G5 "]"
           INSPECT T TALLYING C FOR ALL "A" REPLACING ALL C BY "Z"
           DISPLAY "F3B=[" T "|" C "]"
           INSPECT S2D TALLYING S2RT FOR ALL "A"
                      REPLACING ALL "A" BY "Z"
           DISPLAY "F3C=[" S2 "]"
           STOP RUN.
