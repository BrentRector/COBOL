*>--- conformance-case
*> id: pb485_value_only_format1
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PENDING -- ISO 13.16.3 SR9: "The PICTURE clause may be omitted for
      *> an elementary item when an alphanumeric, boolean, or national
      *> literal that is not a zero-length literal is specified in the
      *> data-item format of the VALUE clause. A PICTURE clause is implied
      *> as follows: a) if the literal is alphanumeric, 'PICTURE
      *> X(length)'."
      *> COBOL.NET does not implement the implied PICTURE today: this
      *> program compiles no field for W and then emits C# referencing it
      *> (CS0103, "The name 'W' does not exist in the current context") --
      *> an uncompilable-backend failure on LEGAL source, found while
      *> building the COBOLNET1747 goldens. It is NOT the PB485 mechanism
      *> and has its own defect note.
      *> It is registered here rather than dropped because it is also the
      *> only witness available for a design property of the COBOLNET1747
      *> screen: W is a FORMAT-1 entry whose only clause is a VALUE
      *> clause, exactly the shape 13.16.2 formats 3 and 4 have. A screen
      *> that identified "format 3 or 4" from the BODY before selecting on
      *> the level-number would reject W as a condition-name entry written
      *> at level 01. Enable this with the wave that lands SR9.
      *> kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485P4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W VALUE "AB".
       PROCEDURE DIVISION.
           DISPLAY W
           STOP RUN.
