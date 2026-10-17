      *>--- conformance-case
      *> id: pb807-display-directive-upon-input-device
      *> rules: [GR-7.3.12.4-5, FMT-7.3.12.2, DOC-A.1-54]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> 7.3.12.4 GR5 b): compile-time-device-1 is a device the
      *> implementor defines for receiving data; SYSIN is an input-only
      *> device-name, so data cannot be transferred to it (kb/Work PB807).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB807I.
000300 PROCEDURE DIVISION.
000400 >>DISPLAY "X" UPON SYSIN
000500     STOP RUN.
