*>--- conformance-case
*> id: pb894_program_prototype_2002
*> rules: [SR-10.6.2-1, SR-10.6.2-2, GR-11.10.4-6, SR-12.3.8.3-14,
*>    GR-12.3.8.4-10, FMT-11.10.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB894 -- ISO 11.10.2 Format 2, the program prototype
      *> definition, at its introducing edition (2002, with the
      *> REPOSITORY PROGRAM specifier that consumes it):
      *>   PROGRAM-ID. program-prototype-name-1 [AS literal-1] IS PROTOTYPE.
      *> 10.6.1 prints the program-prototype source unit with its END
      *> PROGRAM marker unbracketed; 10.6.2 SR1 puts it before every other
      *> source unit; SR4 limits it to a LINKAGE SECTION and a procedure
      *> division HEADER; 11.10.4 GR6 makes literal-1 the externalized name.
      *> DERIVATION of the output. PGPA's REPOSITORY names prototype PGPQ
      *> AS "PGPQX" (12.3.8.4 GR10 NOTE 1: literal-3 is the externalized
      *> name). GR10 a): an in-group program DEFINITION whose externalized
      *> name is "PGPQX" (PGPQDEF AS "PGPQX") supplies the details and is
      *> the program called -- 10.6.2 SR2 requires its signature to equal
      *> the prototype's, and it does. CALL PGPQ USING 12, R therefore runs
      *> PGPQDEF, which stores 12 * 12 into R: R=00000144.
      *> The second CALL names prototype PGPR, whose ONLY in-group source
      *> is the prototype definition PGPR (GR10 b): the details come from it,
      *> and "the program that will be called is the one with the same
      *> externalized name" -- PGPRDEF AS "PGPR" is that program; it adds
      *> 1: N=00013. Both prototypes register no runtime module (10.6.3 GR1
      *> -- compilation generates repository information, not a program),
      *> so neither can be the run unit's main: PGPA is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGPQ AS "PGPQX" IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-N L-R.
       END PROGRAM PGPQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGPR PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(5).
       PROCEDURE DIVISION USING L-N.
       END PROGRAM PGPR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGPA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PGPQ AS "PGPQX"
           PROGRAM PGPR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9(4) VALUE 12.
       01 W-R PIC 9(8) VALUE 0.
       01 W-M PIC 9(5) VALUE 12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL PGPQ USING W-N W-R
           DISPLAY "R=" W-R
           CALL PGPR USING W-M
           DISPLAY "N=" W-M
           STOP RUN.
       END PROGRAM PGPA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGPQDEF AS "PGPQX".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-N L-R.
       P-MAIN.
           COMPUTE L-R = L-N * L-N
           GOBACK.
       END PROGRAM PGPQDEF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGPRDEF AS "PGPR".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(5).
       PROCEDURE DIVISION USING L-N.
       P-MAIN.
           ADD 1 TO L-N
           GOBACK.
       END PROGRAM PGPRDEF.
