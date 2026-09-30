      *> PB1125 - ISO 14.9.22.2 writes the tallying phrase as
      *>   `identifier-2 FOR ...` repeated, and 14.9.22.4 GR10 makes ALL
      *>   and LEADING transitive only "across the operands that follow
      *>   them". So an identifier followed by FOR - however it is
      *>   written: subscripted, qualified - is the NEXT counter, never one
      *>   more operand of the previous counter.
      *> cite.py --check 14.9.22.4 "Both the ALL and LEADING phrases are
      *>   transitive across the operands that follow them" -> OK
      *>   14.9.22.4 10)
      *> Derivation (S1 = "AAB   "):
      *>   T1 N FOR ALL "A", C(1) FOR ALL "B": N counts the two A, C(1)
      *>      counts the B: N=2, C(1)=1 (CT = 100).
      *>   T2 the same with the qualified counter M OF G: N=2, M=1.
      *>   T3 X="AAB", I FOR ALL "A" (I=1 -> 3), CNT(J) FOR ALL "B" with
      *>      J=1: CNT(1)=1 (T = 10000).
      *>   T4 control, the transitive LEADING list still parses: over
      *>      "SSTAA" the leading S run gives 2; T is not at the first
      *>      eligible position (GR12 b), so it adds nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1125.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X(6).
       01 N PIC 9 VALUE 0.
       01 CT.
          05 C PIC 9 OCCURS 3 VALUE 0.
       01 G.
          05 M PIC 9 VALUE 0.
       01 X PIC X(3) VALUE "AAB".
       01 I PIC 9 VALUE 1.
       01 J PIC 9 VALUE 1.
       01 T.
          05 CNT PIC 9 OCCURS 5 VALUE 0.
       PROCEDURE DIVISION.
           MOVE "AAB" TO S1
           INSPECT S1 TALLYING N FOR ALL "A" C(1) FOR ALL "B"
           DISPLAY "T1 " N " " CT
           MOVE 0 TO N
           INSPECT S1 TALLYING N FOR ALL "A" M OF G FOR ALL "B"
           DISPLAY "T2 " N " " M
           INSPECT X TALLYING I FOR ALL "A" CNT(J) FOR ALL "B"
           DISPLAY "T3 " I " " T
           MOVE 0 TO N
           MOVE "SSTAA" TO S1
           INSPECT S1 TALLYING N FOR LEADING "S" "S" "T"
           DISPLAY "T4 " N
           STOP RUN.
