      *> kb/Work PB1352 - a compiler directive line is not text for
      *> COPY REPLACING or REPLACE: it is never rewritten, it is a
      *> single space for matching, and it is never scanned for COPY.
      *> RULES (each run through scripts/spec/cite.py --check):
      *>   7.3.4 1) "A compiler directive line is not affected by the
      *>     replacing action of a COPY statement or a REPLACE
      *>     statement"                                    -> OK 1)
      *>   7.2.3.4 9) c) 5. / 7.2.4.4 8) c) 5. "Each occurrence of a
      *>     compiler directive line is treated as a single space"
      *>                                                   -> OK
      *>   7.3.16.4 1) "Any text words in text-1 or text-2 that do
      *>     not form a compiler directive line are subject to the
      *>     matching and replacing rules"                 -> OK 1)
      *>   7.3.19.4 1) "Comment-text-1 shall serve only as
      *>     documentation"                                -> OK 1)
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [VV=1]   pb1352a's own >>DEFINE VV AS 1 is not rewritten by
      *>            REPLACING ==AS 1== BY ==AS 3==, so >>IF VV = 1 is
      *>            true. Replacing the directive prints [ELSE].
      *>   [V-TRUE] REPLACING ==V== BY ==W== leaves pb1352b's >>IF V = 1
      *>            alone; V is 1 (W is 2), so the true branch. A
      *>            rewritten condition prints [V-FALSE].
      *>   [DV]     ==DV-A PIC== matches across pb1352c's >>DEFINE
      *>            line (one space), declaring DV-N; with the line
      *>            read as text-words DV-N is undefined.
      *>   [PG]     the COPY in pb1352d's >>PAGE comment-text is no
      *>            COPY statement, so 7.2.3.4 10) is not violated.
      *>   [W1]     ==SHOW-IT NOW== matches across the >>TURN line,
      *>            and ==CHECKING== BY ==CHEKING== does not touch it
      *>            (a rewritten >>TURN is malformed).
       REPLACE ==SHOW-IT NOW== BY ==DISPLAY "[" W1 "]"==
               ==CHECKING== BY ==CHEKING==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1352DIR02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC X(2) VALUE "W1".
       COPY pb1352c REPLACING ==DV-A PIC== BY ==DV-N PIC==.
       COPY pb1352d REPLACING ==PG-A== BY ==PG-B==.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>DEFINE W AS 2
       MAIN-PARA.
           COPY pb1352a REPLACING ==AS 1== BY ==AS 3==.
           COPY pb1352b REPLACING ==V== BY ==W==.
           DISPLAY "[" DV-N "]"
           DISPLAY "[" PG-B "]"
           SHOW-IT
       >>TURN EC-ALL CHECKING OFF
           NOW
           STOP RUN.
