## Entry 1170 — 2026-08-04 06:05 PDT — PB24's PHYSICAL keyword lands, and "finish it" turned up a FOURTH defect that measurement had not yet reached

**Three of PB24's four shapes are now done. The fourth is not "absent" — it is CONFIRMED VIOLATED, which is a
different and worse thing, and it took a deliberate probe to find out.**

**✅ PHYSICAL — landed.** `FUNCTION LENGTH(WS-G PHYSICAL)` was rejected with `COBOLNET1504: takes 1 argument(s);
2 given`. §15.50.2's format is `FUNCTION LENGTH ( argument-1 [ PHYSICAL ] )` — a KEYWORD, counted as an operand.
⭐ **No grammar change was needed**, and that is worth recording because the queue entry budgeted one ("needs a
grammar production, a catalog arity slot"). A bare word already parses as an argument context, so PHYSICAL is
consumed exactly as `ANYCASE` is for the NUMVAL-C family — the precedent was already in the file.

⚖ **What it RETURNS is an implementor determination, and r8's closing sentence makes it small:** *"If argument-1
is physically located where it is defined, LENGTH returns the same value that would be returned had the PHYSICAL
argument not been specified."* COBOL.NET determines that a variable-length group IS physically located where it
is defined — no addressable out-of-line pointer is observable by a program, and the alternative reading would
require inventing a user-visible pointer width nothing here exposes. Documented in `docs/CONFORMANCE.md` per
§4.2.16, and the golden asserts each PHYSICAL form EQUALS its plain form, which is the only way that
determination is checkable.
⚠ **Plus a NEGATIVE golden**, because consuming a keyword must not degrade into accepting anything: a genuine two-
operand call still fails, now with a message naming the actual format.

**⛔ AND r9 IS NOT MISSING — IT IS WRONG.** The previous entry recorded it as "not measured, do not call it absent
without a repro". Probing it settled two things:
· **The construct is legal**, and §3's own definition says so: an "alphanumeric group item" is a *"group item
  except for a bit group item, a national group item, a strongly-typed group item, or a variable-length group
  item"*. A group mixing `PIC X` and `PIC 1 USAGE BIT` is therefore alphanumeric, so r3 applies and r9 rounds.
· **MEASURED:** `X(3)` + `1(5) USAGE BIT` returns **8**. Required: 24 bits + 5 bits = 29 bits = **3.625**
  alphanumeric positions → r9 → **4**. Each bit is being counted as a whole character position. Silent.

**The fix shape is different from the three that landed**, which is why it is not bundled: group length must be
composed in BITS and converted with a ceiling, rather than summed per-child in character widths. That is a change
to how `ImageWidth` composes a mixed group — not another arm on `BindLengthFold`.

⚠ **SO PB24 IS STILL OPEN, AND ITS FLAGS WENT BACK UP.** `wrong_answer` and `silent` are true again and the
register returned it to the actionable list (13 → 14). That is the register doing its job: "three quarters done"
is not a state a work list should let me feel finished in.

**GATE.** Conformance corpus **426/426** — both new goldens and the negative case included.

