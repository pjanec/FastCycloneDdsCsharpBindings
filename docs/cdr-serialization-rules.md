# CONSOLIDATED DDS SERIALIZATION RULES
**Derived from empirical analysis of 110 CycloneDDS Test Cases**

This document serves as the technical specification for implementing the C# Serializer/Deserializer to match native CycloneDDS behavior.

## 1. Encoding Formats

*   **@final Types (XCDR1 / Plain CDR):**
    *   Encapsulation Header: `00 01 00 00` (Little Endian) or `00 01 00 03` (if padding is required/used).
    *   Strict alignment rules relative to the **Stream Start**.
    *   Padding is inserted **before** fields to meet alignment.
    *   Padding is inserted **at the end** of structs to align to the largest member's alignment (usually 4 or 8).

*   **@appendable / @mutable Types (XCDR2 / PL_CDR2):**
    *   Encapsulation Header: `00 09 00 00` (Little Endian) or `00 09 00 02` / `00 09 00 03`.
    *   Uses **DHEADER** (Delimiter Header) before composite types.
    *   Alignment is tighter; end-of-struct padding is suppressed inside DHEADERs.

## 2. XCDR2 (@appendable) Specific Rules

### A. DHEADER Calculation
*   **Definition:** A 4-byte unsigned integer indicating the size of the object body.
*   **Calculation:** `Size = Sum(Member_Sizes)`.
*   **Rule:** Do **NOT** include "End of Struct" alignment padding in the DHEADER count. The DHEADER count reflects exactly the bytes used by the members.

### B. Sequence Serialization
The most critical distinction found in the tests is how sequences are serialized within an appendable struct:

1.  **Primitive Sequences:** ( `sequence<bool/byte/char/short/int/long/float/double>` )
    *   **NO Wrapper DHEADER.**
    *   Format: `[Length (4)]` + `[Data (N * Size)]`.
2.  **Constructed Sequences:** ( `sequence<String>`, `sequence<Struct>`, `sequence<Union>` )
    *   **HAS Wrapper DHEADER.**
    *   Format: `[DHEADER (4)]` + `[Length (4)]` + `[Data]`.
    *   *Note:* The DHEADER value = `4 (Length size) + Data size`.
3.  **Enum Sequences:** ( `sequence<Enum>` )
    *   **HAS Wrapper DHEADER.**
    *   *Crucial:* Enums are treated as constructed types in this context.

### C. Optional Fields
1.  **Primitives / Enums / Structs:**
    *   **Absent:** Serialize **1 byte** `0x00`.
    *   **Present:** Serialize **1 byte** `0x01` followed by the value (aligned).
2.  **Strings:**
    *   **Absent:** Serialize **1 byte** `0x00`.
3.  **Multiple Optionals:**
    *   Serialize all presence flags contiguously as bytes (`0x00` or `0x01`) before serializing any values.
    *   Example: 3 optionals -> 3 bytes of flags -> (Padding if needed) -> Value 1 -> ...

### D. Nested Structs
*   If an `@appendable` struct contains another `@appendable` struct member, the inner struct **MUST** be prefixed by its own DHEADER.

### E. Unions in XCDR2
*   **Structure:** `[DHEADER] + [Discriminator] + [Selected Member]`.
*   **Discriminator:** Serialized as 4 bytes (even for boolean/short) in observed tests, or aligned to 4 bytes.
*   **Alignment:** Unlike XCDR1, XCDR2 unions showed **no padding** between a 4-byte discriminator and an 8-byte value (e.g., double) in specific test cases (Test 106). The payload is packed tightly.

## 3. XCDR1 (@final) Specific Rules

### A. Alignment & Padding
*   **Start Alignment:** All alignment is calculated relative to the 4-byte Encapsulation Header.
    *   *Offset 0* = First byte of Encapsulation Header.
    *   *Offset 4* = First byte of Payload.
*   **Internal Padding:** Insert `0x00` bytes *before* a field if the current `Offset % Alignment != 0`.
*   **Trailing Padding:** Insert `0x00` bytes *after* the last field until `TotalSize % MaxMemberAlignment == 0`.
    *   *Example:* A struct with `long` (4) + `char` (1) has size 5. Max alignment is 4. Pad 3 bytes -> Total 8.

### B. Sequences
*   Format: `[Length (4)]` + `[Padding (if needed)]` + `[Data]`.
*   **Padding Rule:** Unlike XCDR2, there is no DHEADER wrapper. However, alignment rules apply to the *elements*. If the element type requires 8-byte alignment (e.g., `double`), and the Length takes 4 bytes, the data is usually written immediately (Offset 4 relative to seq start) because standard CDR often aligns sequence elements relative to the *buffer*, not the sequence container.
*   *Observation:* Tests 102/103 (`int64`/`double` sequence) showed **NO padding** between Length(4) and Data(8). This suggests the native serializer treats the sequence buffer start as the alignment reference, or `int64`/`double` inside sequences in this implementation tolerate 4-byte alignment.

### C. Strings
*   Format: `[Length (4)]` + `[Characters]` + `[Null Terminator]`.
*   **Padding:** The total string size (4 + chars + 1) is padded to align to 4 bytes (or the max struct alignment) if it is the last field or followed by a field requiring alignment.

## 4. Common Pitfalls Checklist for Developer

1.  [ ] **Do not wrap** primitive sequences (`int`, `double`) in DHEADERs in XCDR2.
2.  [ ] **Do wrap** Enum sequences in DHEADERs in XCDR2.
3.  [ ] **Do wrap** String sequences in DHEADERs in XCDR2.
4.  [ ] **Do not add padding** to the end of XCDR2 DHEADERs.
5.  [ ] **Check Optional Flags:** Read 1 byte. Do not assume 4-byte headers for optionals in XCDR2.
6.  [ ] **Recursion:** Ensure `GetSize()` calls are recursive for nested appendable types to calculate DHEADERs correctly.
7.  [ ] **Multi-Optional:** Read *all* flags for a scope before reading *any* values.
8.  [ ] **Visual Debugging:** Don't confuse the lower 32-bits of a `double` (often `00 00 00 00`) with padding. Check the values.
---
