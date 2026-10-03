      *> reject-at: 2023
      *> 7.3.12.4 GR5 b): compile-time-device-1 is a device the
      *> implementor defines for receiving data; SYSIN is an input-only
      *> device-name, so data cannot be transferred to it (kb/Work PB807).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB807I.
000300 PROCEDURE DIVISION.
000400 >>DISPLAY "X" UPON SYSIN
000500     STOP RUN.
