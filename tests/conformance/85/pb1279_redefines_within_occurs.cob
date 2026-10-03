      *> kb/Work PB1279 - an item REDEFINED inside a table. ISO/IEC 1989:2023 section 13.18.44.3 SR5: "However,
      *> data-name-2 may be subordinate to an item whose data description entry contains an OCCURS clause."
      *>   cite.py: OK  13.18.44.3 5)  (Syntax rules)
      *> Section 13.18.44.4 GR1: "Storage association for the subject of the entry starts at the first bit of the
      *> item referenced by data-name-2" - in each occurrence of the enclosing table - and GR2: "the data-name
      *> associated with any of those data description entries may be used to reference that storage area."
      *>   cite.py: OK  13.18.44.4 1) and 2)  (General rules)
      *>
      *> DERIVATION. T holds six elements E(1..2, 1..3), each A PIC X(4) VALUE "wxyz" with a group view B over the
      *> SAME four characters (B1 the first two, B2 the last two as digits). Each element has its own storage.
      *> L1: every element is seeded "wxyz", so A(1,1) and A(2,3) both read wxyz.
      *> L2: MOVE "ab12" TO A(2,3) is seen through B1(2,3) = ab and B2(2,3) = 12; A(1,3) is another element.
      *> L3: MOVE I TO B2(I,J) rewrites only the last two characters of element (I,J): the rows read wx01 x3 and
      *> wx02 x2 and, for the element changed in L2, ab02.
      *> L4: E(2,2) is the group view of one element: wx02. Moving it to E(1,1) copies those four characters, so
      *> A(1,1) = wx02 and B1(1,1) = wx.
      *> L5: reference modification of a view, B1(1,2)(2:1), is the second character of "XXXX": X.
      *> L6: INITIALIZE R(2) sets each alphanumeric A of that row to spaces (the view B is not initialized
      *> separately), so A(2,1) and B1(2,3) are blank.
      *> L7: U.GB1(2) is a six-character view over the group G(2) whose table GA has three two-character cells:
      *> storing 123456 through GB1(2) fills GA(2,1..3) as 12, 34, 56, while GA(1,1) of the other row stays blank.
      *> L8: a table that lies within the redefined group, GA(1,3), shows through GB1(1) as "    ZZ".
      *> L9: the OCCURS DEPENDING table V has its own backing per element: VA is "ab" in both occurrences, and a
      *> numeric store through VB(2) reads back through VA(2) as the two digits 07.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1279RO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 I PIC 9 VALUE 0.
       77 J PIC 9 VALUE 0.
       77 N PIC 9 VALUE 2.
       01 T.
          05 R OCCURS 2.
             10 E OCCURS 3.
                15 A PIC X(4) VALUE "wxyz".
                15 B REDEFINES A.
                   20 B1 PIC X(2).
                   20 B2 PIC 99.
       01 U.
          05 R2 OCCURS 2.
             10 G.
                15 GA PIC X(2) OCCURS 3.
             10 GB REDEFINES G.
                15 GB1 PIC X(6).
       01 V.
          05 RV OCCURS 1 TO 3 DEPENDING ON N.
             10 VA PIC X(2) VALUE "ab".
             10 VB REDEFINES VA PIC 99.
       PROCEDURE DIVISION.
           DISPLAY "L1=" A(1, 1) "|" A(2, 3).
           MOVE "ab12" TO A(2, 3).
           DISPLAY "L2=" B1(2, 3) " " B2(2, 3) " " A(2, 3) " " A(1, 3).
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
               PERFORM VARYING J FROM 1 BY 1 UNTIL J > 3
                   MOVE I TO B2(I, J)
               END-PERFORM
           END-PERFORM.
           DISPLAY "L3=" A(1, 1) A(1, 2) A(1, 3) A(2, 1) A(2, 2)
               A(2, 3).
           DISPLAY "L4a=" E(2, 2).
           MOVE E(2, 2) TO E(1, 1).
           DISPLAY "L4b=" A(1, 1) " " B1(1, 1).
           MOVE "XXXX" TO A(1, 2).
           DISPLAY "L5=" B1(1, 2)(2:1).
           INITIALIZE R(2).
           DISPLAY "L6=[" A(2, 1) "][" B1(2, 3) "]".
           MOVE "123456" TO GB1(2).
           DISPLAY "L7=" GA(2, 1) GA(2, 2) GA(2, 3) "|" GA(1, 1) "|".
           MOVE "ZZ" TO GA(1, 3).
           DISPLAY "L8=" GB1(1).
           DISPLAY "L9a=" VA(1) VA(2).
           MOVE 7 TO VB(2).
           DISPLAY "L9b=" VA(2) VB(2).
           STOP RUN.
