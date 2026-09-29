      *> ISO §14.2.2 SR4 — RETURNING in a program definition, a program
      *> prototype and a method definition, each delivering its value.
      *> RULE (14.2.2 SR4): "The RETURNING phrase may be specified in a
      *> method definition, a program definition, or a program
      *> prototype."
      *> cite.py --check 14.2.2 "The RETURNING phrase may be specified
      *>   in a method definition, a program definition, or a program
      *>   prototype" -> OK  §14.2.2 4)  (Syntax rules)
      *> cite.py --check 14.2.3 "Data-name-2 is the name used in the
      *>   function, method, or program for the result that is returned
      *>   to the activating element" -> OK  §14.2.3 6)
      *> One arm per permitted source element; each value is computed by
      *> the activated element into its RETURNING item and delivered to
      *> the activating element (14.2.3 GR6), so a rejected or ignored
      *> RETURNING phrase leaves the receiver at its VALUE 0.
      *> DERIVATION of every output line:
      *>  R1: program DEFINITION L1C22B, PROCEDURE DIVISION USING L-N
      *>      RETURNING L-R, computes L-R = L-N * 2 with W-N = 7:
      *>      R1=000014.
      *>  R2: program PROTOTYPE L1C22P (IS PROTOTYPE) declares USING L-N
      *>      RETURNING L-R; CALL L1C22P through the REPOSITORY PROGRAM
      *>      specifier activates the in-group definition with the same
      *>      externalized name, L1C22C AS "L1C22P", which computes
      *>      L-R = L-N + 100 with W-N = 7: R2=000107.
      *>  R3: factory METHOD TWICE of class L1C22K, PROCEDURE DIVISION
      *>      USING L-N RETURNING L-R, L-R = L-N * 2; INVOKE ... USING
      *>      W-M (21) RETURNING W-R: R3=000042.
      *>  R4: the same method through inline invocation L1C22K ::
      *>      "TWICE" with W-K = 50; its value is the returning item:
      *>      R4=000100.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22P IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-N RETURNING L-R.
       END PROGRAM L1C22P.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM L1C22P
           CLASS L1C22K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9(4) VALUE 7.
       01 W-M PIC 9(4) VALUE 21.
       01 W-K PIC 9(4) VALUE 50.
       01 W-R PIC 9(6) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1C22B" USING W-N RETURNING W-R
           DISPLAY "R1=" W-R
           MOVE 0 TO W-R
           CALL L1C22P USING W-N RETURNING W-R
           DISPLAY "R2=" W-R
           MOVE 0 TO W-R
           INVOKE L1C22K "TWICE" USING W-M RETURNING W-R
           DISPLAY "R3=" W-R
           MOVE 0 TO W-R
           MOVE L1C22K :: "TWICE" (W-K) TO W-R
           DISPLAY "R4=" W-R
           STOP RUN.
       END PROGRAM L1C22A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-N RETURNING L-R.
       P-MAIN.
           COMPUTE L-R = L-N * 2
           GOBACK.
       END PROGRAM L1C22B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22C AS "L1C22P".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-N RETURNING L-R.
       P-MAIN.
           COMPUTE L-R = L-N + 100
           GOBACK.
       END PROGRAM L1C22C.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1C22K.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TWICE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-N RETURNING L-R.
       P-MAIN.
           COMPUTE L-R = L-N * 2.
       END METHOD TWICE.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS L1C22K.
