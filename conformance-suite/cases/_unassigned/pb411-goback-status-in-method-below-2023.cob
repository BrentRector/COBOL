*>--- conformance-case
*> id: pb411-goback-status-in-method-below-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB411 — THE METHOD ARM OF THE EDITION GATE, which had none.
      *> GOBACK ... WITH {NORMAL|ERROR} STATUS is a COBOL-2023 addition to the ONE
      *> general format 14.9.18.2, and Annex E.3.3 item 32 records it: GOBACK "now
      *> allows the same status phrase as the STOP statement". A method's GOBACK is
      *> written in that same general format, so the syntax is unavailable below 2023
      *> there exactly as it is in a program.
      *> Measured before the fix: this program compiled CLEAN at --std 2002 and
      *> --std 2014, while the byte-identical statement in a program drew
      *> COBOLNET0900 - the version pass excluded a method context from the status
      *> gate, which is the RETURNING gate's exclusion (14.9.18.4 GR4 makes a
      *> method's GOBACK a different statement MEANING) applied to a question about
      *> SYNTAX AVAILABILITY, where it does not belong.
      *> Not pinned at 85: the CLASS-ID / METHOD-ID construct is itself a COBOL-2002
      *> introduction and answers first there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB411MG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CB411M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CB411M.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CB411M "NEW" RETURNING O.
           INVOKE O "PING".
           STOP RUN.
       END PROGRAM PB411MG.

       IDENTIFICATION DIVISION.
       CLASS-ID. CB411M INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN-P.
           GOBACK WITH ERROR STATUS 5.
       END METHOD PING.
       END OBJECT.
       END CLASS CB411M.
