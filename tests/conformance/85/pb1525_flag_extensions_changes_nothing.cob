      *> options: flag-extensions=on
      *> kb/Work PB1525 - ISO 1989:2023 4.2.10: "An implementation shall provide a warning mechanism that optionally may
      *> be invoked by the user at compile time to indicate use of a nonstandard extension in a compilation group."
      *> The mechanism is a WARNING (COBOLNET2894), so it must change nothing about the program: this source uses the
      *> vendor usages COMP-3, COMP-5 and COMP-2, which the compiler accepts at every edition and names under
      *> --flag-extensions, and it compiles and runs exactly as it does without the option. The warnings themselves are
      *> asserted by unit NonstandardExtensionWarningTests and the register by NonstandardExtensionRegisterDriftTests
      *> (the corpus compares output, never the warning channel). Values are plain arithmetic: 12 + 3 = 15, and the
      *> COMP-2 item holds 1.5 exactly (a power of two).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1525FLG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(5) COMP-3 VALUE 12.
       01 B PIC S9(5) COMP-5 VALUE 3.
       01 C COMP-2 VALUE 1.5.
       PROCEDURE DIVISION.
           ADD B TO A.
           DISPLAY A ' ' B ' ' C.
           STOP RUN.
