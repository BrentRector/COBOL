*>--- conformance-case
*> id: pb169-goback-status-zero-length-literal
*> rules: [SR-14.9.18.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> THE ARM PIN. GOBACK's status phrase is 14.9.18.3 SR6/SR7/SR8 - the same
      *> three rules as STOP RUN's SR2/SR3/SR4, over the SAME shared grammar rule
      *> and the SAME binder method. This fixture exists so a future edit that
      *> fixes STOP and misses GOBACK fails, because "which arm did I fix" is the
      *> most reproducible defect shape in this repository.
      *> (GOBACK ... WITH STATUS is a COBOL-2023 introduction, so 2023 only - at
      *> 85/2002/2014 the introduction gate COBOLNET0900 would answer first and
      *> the fixture would pin the wrong rejection.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB169N5.
       PROCEDURE DIVISION.
       MAIN.
           GOBACK WITH ERROR STATUS "".
