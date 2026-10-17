*>--- conformance-case
*> id: pb487-picture-required-elementary
*> rules: [SR-13.16.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB487 - ISO 13.16.3 syntax rule 8, last sentence: "For any other entry describing an
      *> elementary item, a PICTURE clause shall be specified except as indicated in Syntax rule 9."
      *> M has nothing subordinate to it (8.5.1.3.1 makes it elementary - "The most basic subdivisions of a
      *> record, that is, those not further subdivided, are called elementary items"), carries no PICTURE, no usage from SR8's
      *> picture-less list, and no VALUE literal to imply one under SR9 - so it is nonconforming at every
      *> edition.  This rule was enforced ONLY for USAGE NATIONAL and USAGE BIT, the two usages that had a
      *> deferred-adjudication mark; every other picture-less elementary entry escaped the binder with a null
      *> PicInfo and CRASHED the compiler - an unhandled System.NullReferenceException in
      *> MoveEmitter.ConvertSource, with no diagnostic of any kind.  It needs no exotic clause to reach: this
      *> program is the whole repro.  It is now COBOLNET0881 (the usage-clause compatibility band, whose
      *> picture-less-usage arm this generalizes) and a recovery PicInfo, so nothing reaches the emitter
      *> without one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB487NP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M.
       01 N PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCD" TO M
           DISPLAY "SHOULD NOT COMPILE" N
           STOP RUN.
