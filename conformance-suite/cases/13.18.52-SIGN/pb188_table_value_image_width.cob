*>--- conformance-case
*> id: pb188_table_value_image_width
*> rules: [GR-13.18.52.4-6, GR-13.18.63.4-12, GR-13.18.63.4-13]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB188TVW.
      *> kb/Work PB188 — THE WIDTH OF A FORMAT 2 (TABLE) VALUE SEEDED INTO A CHARACTER-IMAGE NUMERIC LEAF.
      *> PB188 landed the fix reasoning that its arm was DEAD: DataBinder.ValidateValueCategory meets a quoted
      *> literal on a numeric subject first (COBOLNET1657) and either errors or rewrites the raw text to the
      *> number, so item.RawValue no longer starts with a quote by the time either seeder sees it.  That was
      *> true of the FORMAT 1 input only.  ValueInitializer.InitializerFor's effRaw is
      *> `item.ValueAt(subs)`, whose Format-2 arm is fed per occurrence by the binder's TableValuePlan from
      *> DataBinder.BuildTableValueSpecs, which at the time called no screen at all - so the arm was LIVE
      *> through the format 2 (table) VALUE, and this program is the shape that measured it.
      *>
      *> MEASURED on 8ca74a3d:  L1=[AA012012CC]  — ten characters where the group area is twelve.  The old
      *> spelling stored `StrStore(chars, pic.Length)`, the PICTURE's DIGIT COUNT (3), into a window whose
      *> width is the item's ImageWidth (4), so every following member of the group image was displaced.
      *> 13.18.52.4 GR6a is why the two numbers differ here: with SEPARATE CHARACTER "the operational sign is
      *> presumed to be the leading (or, respectively, trailing) character position of the data item ...; this
      *> character position is not a digit position", so PIC S9(3) SIGN SEPARATE occupies four positions and
      *> three digits.  Routing through NumFormatImage takes the width from the item's own pinned byte form.
      *>
      *> ⚠ THE LITERALS ARE NUMERIC, and they became numeric when kb/Work PB208 landed.  13.18.63.3 SR2 is an
      *> ALL FORMATS rule - "If the category of the subject of the entry is numeric, all literals in the VALUE
      *> clause shall be numeric" - so the quoted `VALUE "012"` this fixture used to write is a violation in
      *> format 2 exactly as in format 1, and it was accepted only because the format-2 screen did not exist.
      *> It exists now (DataBinder.ScreenValueLiteral, one funnel for every format), so the quoted spelling is
      *> COBOLNET1657 at strict and the numeric store under --permissive - the negative twin is
      *> conformance-suite/cases/13.18.63-VALUE/pb208-table-value-alphanumeric-on-numeric.cob.  THE WIDTH PIN IS
      *> UNCHANGED BY THAT: PB188's whole point was that the CCVS-leniency arm and the numeric arm are ONE
      *> encode (an alphanumeric literal on a numeric item is read AS the numeric literal SR2 asked for), so a
      *> numeric literal reaches the SAME RuntimeApi.NumFormatImage with the SAME item profile, and every
      *> expected value below is byte-for-byte what the quoted spelling produced under --permissive.
      *>
      *> EXPECTED VALUES ARE DERIVED FROM THE SPEC, not from the run:
      *>   13.18.63.4 GR12/GR13 - the FROM (1) TO (2) phrase initializes occurrence 1 to the literal and
      *>     reuses the (single) literal for occurrence 2.  GR11 carries GR5 and GR7 into format 2.
      *>   13.18.52.4 GR6b - the operational signs for positive and negative are '+' and '-'; both values are
      *>     positive, so the sign character is '+'.
      *>   13.18.52.4 GR6a - LEADING SEPARATE puts it in the leading character position and TRAILING SEPARATE
      *>     in the trailing one, over three zero-filled digit positions: LB = "+012", TB = "045+".
      *>   13.18.63.4 GR5/GR7 - the group area is the members' positions in order:
      *>     GL = "AA" + "+012" + "+012" + "CC";  GT = "AA" + "045+" + "045+" + "CC".
      *> The TRAILING twin is here because the LEADING one alone cannot tell a four-wide window from a
      *> three-wide one followed by a stray '+': it puts the extra position at the OTHER end.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GL.
          05 LA PIC X(2) VALUE "AA".
          05 LB PIC S9(3) SIGN IS LEADING SEPARATE OCCURS 2
             VALUE 12 FROM (1) TO (2).
          05 LC PIC X(2) VALUE "CC".
       01 GT.
          05 TA PIC X(2) VALUE "AA".
          05 TB PIC S9(3) SIGN IS TRAILING SEPARATE OCCURS 2
             VALUE 45 FROM (1) TO (2).
          05 TC PIC X(2) VALUE "CC".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "L1=[" GL "]"
           DISPLAY "L2=[" LB(1) "][" LB(2) "][" LC "]"
           DISPLAY "T1=[" GT "]"
           DISPLAY "T2=[" TB(1) "][" TB(2) "][" TC "]"
           STOP RUN.
