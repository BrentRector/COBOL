      *>--- conformance-case
      *> id: pb919_index_name_qualified
      *> rules: [SR-8.4.2.2.1-1, SR-8.4.2.2.3-1, SR-8.4.2.2.3-3, SR-8.4.2.2.3-6, FMT-8.4.2.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 §8.4.2.2.3 SR1 and SR6 for INDEX-NAMES (kb/Work PB919).
      *>   SR1: "For each non unique user-defined name that is explicitly referenced, uniqueness shall be
      *>   established through a sequence of qualifiers that precludes any ambiguity of reference."
      *>   SR6: "The qualification of an index-name may include the name of the table with which the
      *>   index-name is associated, as well as any name by which that table may be qualified."
      *>   SR3: "The words IN and OF are equivalent."
      *> TA and TB each declare INDEXED BY IX, so every IX below is qualified - through its table (IX OF EA),
      *> through the table's record (IX OF TA), or with IN. Each declaration owns its OWN index (§14.9.39.4
      *> GR1 associates an index-name with the table whose INDEXED BY phrase names it), so setting one never
      *> moves the other. Before PB919 the two shared ONE cell, the qualified spellings were "not defined",
      *> and the bare IX compiled clean (the negative companion pb919-index-name-ambiguous).
      *> WHY EACH LINE CAN FAIL:
      *>   EA=      the first PERFORM stores each occurrence number into EA: 01 02 03.
      *>   EB=      the second stores W * 10 into EB: 10 20 30 40 - a shared cell would leave IX at 4
      *>            (one past TA's bound) before the loop and write nothing.
      *>   PICK=    SET IX OF EA TO 2, IX OF EB TO 4, JX TO 1 (JX is unique - no qualifier needed):
      *>            EA(2)=02, EB(4)=40, EB(1)=10. One cell would read EA(4) or EB(2).
      *>   DISTINCT the relation IX OF EA = 2 AND IX IN TB = 4 is true only with two cells.
      *>   FOUND=   SEARCH EB VARYING IX OF EB from 1 stops at EB(3)=30.
      *>   UP=      SET IX OF TA UP BY 1 moves EA's index 2 -> 3: EA(3)=03; EB's index stays 3 (FOUND).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W57PB919.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA.
          05 EA PIC 9(2) OCCURS 3 INDEXED BY IX.
       01 TB.
          05 EB PIC 9(2) OCCURS 4 INDEXED BY IX JX.
       01 W  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING IX OF TA FROM 1 BY 1 UNTIL IX OF TA > 3
               SET EA (IX OF EA) TO IX OF EA
           END-PERFORM
           PERFORM VARYING IX IN TB FROM 1 BY 1 UNTIL IX IN TB > 4
               SET W TO IX OF EB
               COMPUTE EB (IX OF EB) = W * 10
           END-PERFORM
           DISPLAY "EA=" EA (1) " " EA (2) " " EA (3)
           DISPLAY "EB=" EB (1) " " EB (2) " " EB (3) " " EB (4)
           SET IX OF EA TO 2
           SET IX OF EB TO 4
           SET JX TO 1
           DISPLAY "PICK=" EA (IX OF EA) " " EB (IX OF EB) " " EB (JX)
           IF IX OF EA = 2 AND IX IN TB = 4
               DISPLAY "DISTINCT"
           END-IF
           SET IX OF EB TO 1
           SEARCH EB VARYING IX OF EB
               AT END DISPLAY "NONE"
               WHEN EB (IX OF EB) = 30 DISPLAY "FOUND=" EB (IX OF EB)
           END-SEARCH
           SET IX OF TA UP BY 1
           DISPLAY "UP=" EA (IX OF EA)
           STOP RUN.
