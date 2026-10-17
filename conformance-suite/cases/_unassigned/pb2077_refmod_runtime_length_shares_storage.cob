*>--- conformance-case
*> id: pb2077_refmod_runtime_length_shares_storage
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2077 review round 3 - a REFERENCE-MODIFIED argument whose length is a run-time value,
      *> passed BY REFERENCE, still OCCUPIES the slice's storage. 14.2.3 GR8: "If the argument is passed by
      *> reference, the activated runtime element operates as if the formal parameter occupies the same
      *> storage area as the argument" (OK 14.2.3 8)); 8.4.3.3.4 GR5: "Reference modification creates a
      *> unique data item that is a subset of the data item referenced by identifier-1" (OK 8.4.3.3.4 5)),
      *> and GR5 c): "The evaluation of length specifies the number of bit positions or character
      *> positions of the data item to be used in the operation" (OK 8.4.3.3.4 5) c)). So when G and a
      *> slice of G are both passed, a store through the whole-G formal IS visible through the slice
      *> formal, for a static slice and a run-time one alike, on the CALL lane and on the typed and the
      *> universal INVOKE lanes. A run-time slice SHORTER than its group formal (D-RM1 a) in
      *> docs/CONFORMANCE.md: rule 1 was proved on its maximum extent) cannot hold the formal: that formal
      *> holds the slice padded to its width, written back through the slice, truncated to it.
      *> DERIVATION (G = "AAAAAAAAAA" before each leg; F2/LW is a 10-character group, F1/LS a 5-character
      *> group; the callee moves ALL "Z" to F2/LW, then displays F1/LS):
      *>   CALL G G(1:5)            static, 5 >= 5      -> SS:ZZZZZ|  g=ZZZZZZZZZZ
      *>   CALL G G(1:N) N=5        run time, 5 >= 5    -> SS:ZZZZZ|  g=ZZZZZZZZZZ
      *>   CALL G G(2:N) N=7        run time, 7 >= 5    -> SS:ZZZZZ|  g=ZZZZZZZZZZ
      *>   CALL G G(N:)  N=6        to the end, 5 >= 5  -> SS:ZZZZZ|  g=ZZZZZZZZZZ
      *>   CALL G G(1:N) N=3        3 < 5: a copy       -> SS:AAA  |  g=AAAZZZZZZZ (write-back G(1:3))
      *>   CALL G G(N:)  N=7        4 < 5: a copy       -> SS:AAAA |  g=ZZZZZZAAAA (write-back G(7:4))
      *>   INVOKE T (typed)  G G(1:N) N=5               -> TW:ZZZZZ|  g=ZZZZZZZZZZ
      *>   INVOKE U (universal) G G(1:N) N=5            -> TW:ZZZZZ|  g=ZZZZZZZZZZ
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2077SH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2077SH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T  USAGE OBJECT REFERENCE C2077SH.
       01 U  USAGE OBJECT REFERENCE.
       01 N  PIC 9(2).
       01 G.
          05 G1 PIC X(10).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "-- call"
           MOVE ALL "A" TO G
           CALL "PB2077SS" USING G G(1:5)
           DISPLAY "g=" G
           MOVE ALL "A" TO G
           MOVE 5 TO N
           CALL "PB2077SS" USING G G(1:N)
           DISPLAY "g=" G
           MOVE ALL "A" TO G
           MOVE 7 TO N
           CALL "PB2077SS" USING G G(2:N)
           DISPLAY "g=" G
           MOVE ALL "A" TO G
           MOVE 6 TO N
           CALL "PB2077SS" USING G G(N:)
           DISPLAY "g=" G
           MOVE ALL "A" TO G
           MOVE 3 TO N
           CALL "PB2077SS" USING G G(1:N)
           DISPLAY "g=" G
           MOVE ALL "A" TO G
           MOVE 7 TO N
           CALL "PB2077SS" USING G G(N:)
           DISPLAY "g=" G
           DISPLAY "-- invoke"
           INVOKE C2077SH "NEW" RETURNING T
           SET U TO T
           MOVE ALL "A" TO G
           MOVE 5 TO N
           INVOKE T "TW" USING G G(1:N)
           DISPLAY "g=" G
           MOVE ALL "A" TO G
           INVOKE U "TW" USING G G(1:N)
           DISPLAY "g=" G
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB2077SH.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2077SS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 F2.
          05 F2A PIC X(10).
       01 F1.
          05 F1A PIC X(5).
       PROCEDURE DIVISION USING F2 F1.
           MOVE ALL "Z" TO F2A
           DISPLAY "SS:" F1A "|"
           GOBACK.
       END PROGRAM PB2077SS.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2077SH INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C2077SH.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW.
          05 LW1 PIC X(10).
       01 LS.
          05 LS1 PIC X(5).
       PROCEDURE DIVISION USING LW LS.
           MOVE ALL "Z" TO LW1
           DISPLAY "TW:" LS1 "|".
       END METHOD TW.
       END OBJECT.
       END CLASS C2077SH.
