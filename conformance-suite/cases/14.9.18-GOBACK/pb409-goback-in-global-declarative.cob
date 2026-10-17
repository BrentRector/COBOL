*>--- conformance-case
*> id: pb409-goback-in-global-declarative
*> rules: [SR-14.9.18.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB409. ISO 14.9.18.3 SR1: "The GOBACK statement shall not be specified in a declarative
      *> procedure for which the GLOBAL phrase is specified in the associated USE statement." Every edition
      *> that has the GOBACK statement at all (a 2002 introduction) states this rule, and 4.2.2 obliges a
      *> conforming implementation to indicate a syntax-rule violation at compile time - a silent accept is
      *> not one. The USE statement here is 1985-legal, so the ONLY rule this program breaks above 85 is SR1.
      *> POSITIVE CONTROL: the same declarative without the GLOBAL phrase is accepted -
      *> ExitPlacementContextDriftTests.ReturnStatementInADeclarative_FollowsTheGlobalPhrase row PBGD01.
      *> NOT THIS RULE: a GOBACK written OUTSIDE the declarative but executed within its RANGE is LEGAL and
      *> is governed at run time by 14.9.18.4 GR6 (EC-FLOW-GLOBAL-GOBACK).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB409.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "negpb409.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
       D-PARA.
           GOBACK.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           STOP RUN.
