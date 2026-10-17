*>--- conformance-case
*> id: pb178_convert_any_redefines_group
*> rules: [GR-13.18.44.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB178. FUNCTION CONVERT's ANY source format over a Tier-B REDEFINES group VIEW.
      *>
      *> §15.19.3 r7: "When the source-format is ANY, argument-1 shall be of any usage, except index,
      *> message-tag, object reference, pointer, function-pointer or program-pointer. It is not necessary for
      *> the contents to be valid according to the usage" — the exclusion list is keyed on USAGE and names only
      *> the address/index shapes, so a GROUP is legal argument-1 and r7 asks for its RAW STORAGE.
      *> §13.18.44.4 GR1: "Storage association for the subject of the entry starts at the first bit of the data
      *> item referenced by data-name-2 and continues over an area sufficient to contain the number of bits
      *> required by the data item referenced by the subject of the entry" — a REDEFINES view IS the same
      *> storage, so its raw-storage image is the redefined item's bytes, never a second encoding.
      *>
      *> THE DEFECT: OperandText.AsStorageImage's group arm spelled `{Read(p)}.AsImage()` itself and had no
      *> RedefViewPlace arm, while a Tier-B view's Read() is ALREADY the (offset, width) character window — so it
      *> emitted `CobolStr.RefMod(_redef_A, 1, 8).AsImage()`, a backend CS1061 on `string`. The PB176 capability
      *> guard did not help: it is the WRONG AXIS — the shape IS image-capable; the READ is already a window.
      *> The fix routes it through PlaceRenderer.SendingGroupImage, THE ONE reader, whose doc-comment already
      *> stated the law ("a consumer that spells .AsImage() itself is wrong for the window shape").
      *> Held by tests/Cobol.Net.Tests.Unit/BoundaryImageChannelTests.cs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB178CVRD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(8) VALUE "ABCDEFGH".
       01 B REDEFINES A.
          05 B1 PIC X(4).
          05 B2 PIC X(4).
       01 C PIC X(6).
       01 D REDEFINES C.
          05 D1 PIC X(4).
          05 D2 PIC S9(4) COMP.
       01 NN PIC 9 VALUE 2.
       01 BODO BASED.
          05 BF1 PIC X(2).
          05 BT OCCURS 1 TO 5 DEPENDING ON NN PIC X(3).
       01 R PIC X(20).
       PROCEDURE DIVISION.
       MAIN.
      *> LEG 1 - THE DEFECT. B is a Tier-B group VIEW over A's eight characters; r7 + GR1 give the eight
      *> Latin-1 code points of "ABCDEFGH" = 41 42 43 44 45 46 47 48.
           MOVE FUNCTION CONVERT(B ANY ANUM HEX) TO R.
           DISPLAY "L1=" R.
      *> LEG 2 - the arm that was already right and must stay right: an ELEMENTARY Tier-B view reaches
      *> AsStorageImage's Pic switch, not the group arm. "ABCD" = 41 42 43 44.
           MOVE FUNCTION CONVERT(B1 ANY ANUM HEX) TO R.
           DISPLAY "L2=" R.
      *> LEG 3 - FLIP THE AXIS THE FIRST TWO LEGS HOLD FIXED: a Tier-B group view containing a PIC S9(4) COMP
      *> leaf (the PB164-widened shape). Proves the fix routes by PLACE SHAPE, not by leaf category. The window
      *> is D1's four characters "WXYZ" (57 58 59 5A) then D2's radix-2 bytes for 258 (§13.18.60.4 GR4/GR11
      *> leave the representation implementor-defined; COBOL.NET's determination is big-endian two's complement
      *> of the storage width - docs/CONFORMANCE.md items 205-215) = 01 02.
           MOVE "WXYZ" TO D1.
           MOVE 258 TO D2.
           MOVE FUNCTION CONVERT(D ANY ANUM HEX) TO R.
           DISPLAY "L3=" R.
      *> LEG 4 - the OdoGroupPlace-over-RedefViewPlace shape (a BASED record is also string-canonical). §13.18.38.4
      *> GR8a: data-name-1 is OUTSIDE the group, so only the CURRENT-count part is used - 2 + 2x3 = 8 characters,
      *> "AB" + "ccc" + "ddd" = 41 42 63 63 63 64 64 64. The MAXIMUM image (17) would be the wrong answer.
           ALLOCATE BODO.
           MOVE "AB" TO BF1.
           MOVE "ccc" TO BT(1).
           MOVE "ddd" TO BT(2).
           MOVE FUNCTION CONVERT(BODO ANY ANUM HEX) TO R.
           DISPLAY "L4=" R.
           STOP RUN.
       END PROGRAM PB178CVRD.
