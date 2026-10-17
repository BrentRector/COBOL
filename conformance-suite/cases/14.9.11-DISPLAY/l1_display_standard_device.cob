*>--- conformance-case
*> id: l1_display_standard_device
*> rules: [FMT-14.9.11.2, DOC-A.1-59]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.4 GR8 DISPLAY statement — the implementor's STANDARD DISPLAY DEVICE (Annex A.1 item 59,
      *> docs/CONFORMANCE.md#DOC-A.1-59).
      *>   python scripts/spec/cite.py --check 14.9.11.4 "If the UPON phrase is not specified, the implementor's
      *>   standard display device is used."  ->  OK  §14.9.11.4 8)
      *> and the device NAMES the UPON phrase may reach are the implementor's too:
      *>   python scripts/spec/cite.py --check 12.3.7.3 "The implementor shall specify the names that are
      *>   available for switch-name-1, feature-name-1, and device-name-1."  ->  OK  §12.3.7.3 8)
      *>   python scripts/spec/cite.py --check 14.9.11.3 "shall be associated with an implementor-defined
      *>   device-name that is identified in the operating environment as a hardware or software device capable
      *>   of receiving data from the program"  ->  OK  §14.9.11.3 2)
      *>
      *> THE DETERMINATION UNDER TEST (row 59): with UPON omitted the standard display device is the process
      *> STANDARD OUTPUT stream; the output-capable device-names are CONSOLE and SYSOUT (both -> standard output)
      *> and SYSERR (-> standard error). The corpus harness compares STANDARD OUTPUT only, so the three arms are
      *> distinguishable from a COBOL program: two lines appear, one does not.
      *>
      *>   NOUPON=STDOUT   GR8 — no UPON phrase, so the standard display device is used, and this line appears
      *>                   on standard output.
      *>   CONSOLE=STDOUT  the CONSOLE device-name resolves to the SAME stream as the GR8 default.
      *>   SYSOUT=STDOUT   so does SYSOUT — the two spellings are one device, not two.
      *>   (no SYSERR line) ⛔ THE DISCRIMINATOR. `DISPLAY … UPON L1-ERR` is executed and its text is ABSENT
      *>                   from this file: SYSERR is a DIFFERENT device from the standard display device, and an
      *>                   implementation that routed every device-name to standard output would print
      *>                   "SYSERR=NOTSTDOUT" between the SYSOUT and TAIL lines and fail here.
      *>   TAIL=SEEN       the run continued past the SYSERR display — it is a device, not a diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DSPDEV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CONSOLE IS L1-CONS
           SYSOUT IS L1-OUT
           SYSERR IS L1-ERR.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "NOUPON=STDOUT"
           DISPLAY "CONSOLE=STDOUT" UPON L1-CONS
           DISPLAY "SYSOUT=STDOUT" UPON L1-OUT
           DISPLAY "SYSERR=NOTSTDOUT" UPON L1-ERR
           DISPLAY "TAIL=SEEN"
           STOP RUN.
