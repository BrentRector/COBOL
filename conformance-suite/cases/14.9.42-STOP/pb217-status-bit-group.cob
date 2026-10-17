*>--- conformance-case
*> id: pb217-status-bit-group
*> rules: [SR-14.9.42.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.42.3 SR2: "Identifier-1 shall reference an integer data item or
      *> a data item with usage display or usage national." A BIT group is
      *> "treated as though it were an elementary data item of usage bit and
      *> class and category boolean described with PICTURE 1(m)" (13.18.29.4
      *> GR1 b) - usage BIT is none of SR2's three descriptions.
      *> The companions are the shapes SR2 DOES admit and this screen used to
      *> reject: a GROUP-USAGE NATIONAL group, which 13.18.29.4 GR2 b makes an
      *> item of usage national, and an ALPHANUMERIC group, which 8.5.2.1 makes
      *> an item of usage display - "An alphanumeric group item is treated as
      *> though it had a usage of display" (both in StopGobackExitCodeTests).
      *> Reading the ONE 8.5.2.1 usage reader (ItemCategory.UsageOf) settles
      *> every group kind: national and alphanumeric admitted, BIT rejected
      *> here, and the strongly-typed / variable-length groups 3.11 excludes
      *> from "alphanumeric group item" rejected by
      *> pb411-status-strongly-typed-group.
      *> kb/Work PB217, PB411.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB217N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-BITGRP GROUP-USAGE IS BIT.
          05 WS-B PIC 1(8) USAGE BIT VALUE B"00000111".
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN WITH ERROR STATUS WS-BITGRP.
