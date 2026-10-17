*>--- conformance-case
*> id: pb2690_initialized_created_occurrences_call
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2690 - THE OCCURRENCES A BY REFERENCE FORMAL'S FIXED TABLE
      *> CREATES IN AN INITIALIZED DYNAMIC-CAPACITY TABLE OF THE ARGUMENT, ACROSS
      *> A FORMAT-2 CALL, AND (GP, GQ) THE CAPACITY A STORE OF THE ELEMENT'S VALUE
      *> RAISES IN A TABLE WITHOUT THE PHRASE.
      *>
      *> 14.2.3 GR8: "the activated runtime element operates as if the formal
      *> parameter occupies the same storage area as the argument", so a store to
      *> the formal's occurrence 3 is a receiving reference to the argument's
      *> dynamic-capacity table, and 8.5.1.9.3 creates "a new element" and raises
      *> the capacity "to the value given by the subscript". 8.5.1.9.5: with
      *> INITIALIZED, "any elementary items not referenced as receiving operands
      *> in a statement that creates new elements in that table are first
      *> initialized as though they had been the subject of a statement of the
      *> form INITIALIZE ... WITH FILLER ALL TO VALUE THEN TO DEFAULT" - the
      *> intermediate occurrences and the unstored items of the created one hold
      *> the element's VALUE, exactly as the same store made in the caller does.
      *> Its NOTE ("functionally identical to one where the intermediate
      *> occurrences are physically allocated and, if specified, initialized")
      *> is why the callee READS an occurrence the argument lacks as the
      *> initialized element (determination D-VGB). BY CONTENT (14.2.3 GR9) the
      *> formal receives a MOVE, whose fixed receiving table is "space filled"
      *> past the sending capacity (14.6.9.2 2).
      *>
      *> EXPECTED VALUES, DERIVED (each table starts with the one element "ab"):
      *>   GA  fixed group formal; the callee stores FE (3) := "r"
      *>                                                    = ab, ab, rb, 3
      *>   DA  the same store made in the caller            = ab, ab, rb, 3
      *>   GB  the callee only reads TF (2): it sees ab     = ab, 1
      *>   GC  the callee stores SPACES into TF (2)         = ab, (2 spaces), 2
      *>   GD  a variable-length formal (TF OCCURS 3 and a dynamic-length
      *>       item); the callee stores FF (3) := "z"       = ab, ab, az, 3
      *>   GE  BY CONTENT: the callee reads TF (2) as spaces; nothing returns
      *>                                                    = ab, 1
      *>   GN  elements holding an INITIALIZED inner dynamic table (ND "d", NI
      *>       "i" FROM 1); the callee stores LI (3, 2) := "X": occurrences 2
      *>       and 3 are created initialized, then NI (3, 2)  = di, diX, 3
      *>   GP  WITHOUT INITIALIZED (PIC 9 VALUE 0 elements, starting "0"),
      *>       the callee stores TFP (3) := 0, the element's own VALUE: still a
      *>       receiving reference past the capacity, so 8.5.1.9.3 raises it to
      *>       3. 8.5.1.9.5: "Otherwise, the content of any unreferenced
      *>       locations in the new table element and the content of the other
      *>       new occurrences are undefined", so occurrence 2 is not shown
      *>                                                    = 0, 0, 3
      *>   GQ  the same one level down: elements of a variable-length group
      *>       (QD PIC 9 VALUE 0, QI an inner dynamic table), no INITIALIZED;
      *>       the callee stores LQD (3) := 0                 = 0, 0, 3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690ICC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 GA.
          05 AX PIC X VALUE "x".
          05 ATB OCCURS DYNAMIC CAPACITY IN CA FROM 1 INITIALIZED.
             10 AE PIC X VALUE "a".
             10 AF PIC X VALUE "b".
       01 DA.
          05 DX PIC X VALUE "x".
          05 DTB OCCURS DYNAMIC CAPACITY IN CDA FROM 1 INITIALIZED.
             10 DQ PIC X VALUE "a".
             10 DR PIC X VALUE "b".
       01 GB.
          05 BX PIC X VALUE "x".
          05 BTB OCCURS DYNAMIC CAPACITY IN CB FROM 1 INITIALIZED.
             10 BE PIC X VALUE "a".
             10 BF PIC X VALUE "b".
       01 GC.
          05 CX PIC X VALUE "x".
          05 CTB OCCURS DYNAMIC CAPACITY IN CC FROM 1 INITIALIZED.
             10 CQ PIC X VALUE "a".
             10 CR PIC X VALUE "b".
       01 GD.
          05 GX PIC X VALUE "x".
          05 GTB OCCURS DYNAMIC CAPACITY IN CGD FROM 1 INITIALIZED.
             10 GE1 PIC X VALUE "a".
             10 GF1 PIC X VALUE "b".
          05 GDL PIC X DYNAMIC LENGTH LIMIT 5.
       01 GE.
          05 EX PIC X VALUE "x".
          05 ETB OCCURS DYNAMIC CAPACITY IN CE1 FROM 1 INITIALIZED.
             10 EE PIC X VALUE "a".
             10 EF PIC X VALUE "b".
       01 GN.
          05 NH PIC X VALUE "h".
          05 NT OCCURS DYNAMIC CAPACITY IN CN FROM 1 INITIALIZED.
             10 ND PIC X VALUE "d".
             10 NI PIC X OCCURS DYNAMIC CAPACITY IN CNI FROM 1
                INITIALIZED VALUE "i".
       01 GP.
          05 PX PIC X VALUE "x".
          05 PTB PIC 9 OCCURS DYNAMIC CAPACITY IN CP FROM 1 VALUE 0.
       01 GQ.
          05 QH PIC X VALUE "h".
          05 QT OCCURS DYNAMIC CAPACITY IN CPQ FROM 1.
             10 QD PIC 9 VALUE 0.
             10 QI PIC X OCCURS DYNAMIC CAPACITY IN CPQI FROM 1
                VALUE "i".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB2690IA" AS NESTED USING GA
           MOVE CA TO WS-C
           DISPLAY "GA=[" AE (1) AF (1) "|" AE (2) AF (2) "|"
               AE (3) AF (3) "] " WS-C
           MOVE "r" TO DQ (3)
           MOVE CDA TO WS-C
           DISPLAY "DA=[" DQ (1) DR (1) "|" DQ (2) DR (2) "|"
               DQ (3) DR (3) "] " WS-C
           CALL "PB2690IB" AS NESTED USING GB
           MOVE CB TO WS-C
           DISPLAY "GB=[" BE (1) BF (1) "] " WS-C
           CALL "PB2690IC" AS NESTED USING GC
           MOVE CC TO WS-C
           DISPLAY "GC=[" CQ (1) CR (1) "|" CQ (2) CR (2) "] " WS-C
           MOVE "dd" TO GDL
           CALL "PB2690ID" AS NESTED USING GD
           MOVE CGD TO WS-C
           DISPLAY "GD=[" GE1 (1) GF1 (1) "|" GE1 (2) GF1 (2) "|"
               GE1 (3) GF1 (3) "] " WS-C
           CALL "PB2690IE" AS NESTED USING BY CONTENT GE
           MOVE CE1 TO WS-C
           DISPLAY "GE=[" EE (1) EF (1) "] " WS-C
           CALL "PB2690IN" AS NESTED USING GN
           MOVE CN TO WS-C
           DISPLAY "GN=[" ND (2) NI (2, 1) "|" ND (3) NI (3, 1)
               NI (3, 2) "] " WS-C
           CALL "PB2690IP" AS NESTED USING GP
           MOVE CP TO WS-C
           DISPLAY "GP=[" PTB (1) "|" PTB (3) "] " WS-C
           CALL "PB2690IQ" AS NESTED USING GQ
           MOVE CPQ TO WS-C
           DISPLAY "GQ=[" QD (1) "|" QD (3) "] " WS-C
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690IA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA.
          05 LAX PIC X.
          05 TF OCCURS 3.
             10 FE PIC X.
             10 FF PIC X.
       PROCEDURE DIVISION USING LA.
       MAIN.
           MOVE "r" TO FE (3)
           GOBACK.
       END PROGRAM PB2690IA.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690IB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB.
          05 LBX PIC X.
          05 TFB OCCURS 3.
             10 FEB PIC X.
             10 FFB PIC X.
       PROCEDURE DIVISION USING LB.
       MAIN.
           DISPLAY "GB sees [" FEB (2) FFB (2) "]"
           GOBACK.
       END PROGRAM PB2690IB.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690IC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC.
          05 LCX PIC X.
          05 TFC OCCURS 3.
             10 FEC PIC X.
             10 FFC PIC X.
       PROCEDURE DIVISION USING LC.
       MAIN.
           MOVE SPACES TO TFC (2)
           GOBACK.
       END PROGRAM PB2690IC.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690ID.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LD.
          05 LDX PIC X.
          05 TFD OCCURS 3.
             10 FED PIC X.
             10 FFD PIC X.
          05 LDL PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION USING LD.
       MAIN.
           MOVE "z" TO FFD (3)
           GOBACK.
       END PROGRAM PB2690ID.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690IE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LE.
          05 LEX PIC X.
          05 TFE OCCURS 3.
             10 FEE PIC X.
             10 FFE PIC X.
       PROCEDURE DIVISION USING LE.
       MAIN.
           DISPLAY "GE sees [" FEE (2) FFE (2) "]"
           MOVE "q" TO FEE (2)
           GOBACK.
       END PROGRAM PB2690IE.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690IN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN.
          05 LNH PIC X.
          05 LTF OCCURS 3.
             10 LD1 PIC X.
             10 LI PIC X OCCURS 2.
       PROCEDURE DIVISION USING LN.
       MAIN.
           MOVE "X" TO LI (3, 2)
           GOBACK.
       END PROGRAM PB2690IN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690IP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP.
          05 LPX PIC X.
          05 TFP PIC 9 OCCURS 3.
       PROCEDURE DIVISION USING LP.
       MAIN.
           MOVE 0 TO TFP (3)
           GOBACK.
       END PROGRAM PB2690IP.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690IQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LQ.
          05 LQH PIC X.
          05 LQT OCCURS 3.
             10 LQD PIC 9.
             10 LQI PIC X OCCURS 2.
       PROCEDURE DIVISION USING LQ.
       MAIN.
           MOVE 0 TO LQD (3)
           GOBACK.
       END PROGRAM PB2690IQ.
       END PROGRAM PB2690ICC.
