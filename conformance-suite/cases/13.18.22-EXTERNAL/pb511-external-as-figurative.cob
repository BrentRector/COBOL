*>--- conformance-case
*> id: pb511-external-as-figurative
*> rules: [SR-13.18.22.3-3, FMT-13.16.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB511 — §13.18.22.3 syntax rule 3: "Literal-1 shall be an alphanumeric or national literal
      *> and shall be neither a figurative constant nor a zero-length literal." That is the SEVENTH
      *> restatement of the one externalized-name sentence (§11.10.3 SR1, §11.5.3 SR1, §11.6.3 SR1,
      *> §11.7.3 SR1, §12.3.8.3 SR2 and §11.3.3 SR1 are the other six), so it is screened through the ONE
      *> shared ExternalizedName.Screen rather than by a seventh copy of the test — COBOLNET2156.
      *> Rejected at every edition that HAS the phrase, i.e. 2002 on; at 85 the phrase itself is the
      *> COBOLNET0900 introduction gate (pb511-external-as-below-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB511FIG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FG IS EXTERNAL AS ZERO PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
