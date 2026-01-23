# IDL JSON Plugin - Documentation Index

**Project:** Fast Cyclone DDS C# Bindings  
**Date:** 2026-01-23  
**Version:** 1.0  
**Status:** Design Phase Complete  

---

## Overview

This directory contains the complete design and implementation documentation for transforming the Cyclone DDS `idlc` compiler plugin into a JSON-generating plugin (`idljson`).

The plugin will generate structured JSON output from IDL files containing:
- Complete type metadata (structs, unions, enums, etc.)
- Topic descriptors with serialization opcodes
- QoS settings from IDL pragmas
- Computed memory layout information

---

## Document Hierarchy

### Core Design Documents

1. **[IDLJSON-PLUGIN-DESIGN.md](./IDLJSON-PLUGIN-DESIGN.md)** ⭐ **START HERE**
   - **Purpose:** Comprehensive architectural and design specification
   - **Audience:** Architects, lead developers
   - **Sections:**
     - Executive Summary
     - Architecture Overview
     - Data Model Design (dm_rec_t, dm_descriptor_t, dm_qos_t)
     - Type Extraction Process (visitor pattern)
     - Memory Layout Calculation (C-ABI rules)
     - Descriptor Extraction (opcode generation)
     - QoS Extraction (pragma parsing)
     - JSON Output Format (with examples)
     - JSON Emitter Implementation
     - Build System Integration
     - Usage Examples
     - Testing Strategy
     - Performance Considerations
     - Error Handling
     - Future Enhancements
   - **Length:** ~6000 lines
   - **Complexity:** 8/10

2. **[IDLJSON-IMPLEMENTATION-GUIDE.md](./IDLJSON-IMPLEMENTATION-GUIDE.md)** 📖
   - **Purpose:** Detailed code-level implementation instructions
   - **Audience:** Developers implementing the plugin
   - **Sections:**
     - Prerequisites and environment setup
     - Step-by-step implementation (6 steps)
     - Complete code snippets for all functions
     - Debugging strategies
     - Common pitfalls and solutions
     - Testing checklist
     - CMake integration
     - Performance profiling
     - Version control strategy
   - **Length:** ~1800 lines
   - **Complexity:** 7/10
   - **Key Features:**
     - Copy-paste ready code
     - Platform-specific notes (Windows/Linux)
     - Memory management best practices
     - Debug macros and helpers

3. **[IDLJSON-TRANSFORMATION-ROADMAP.md](./IDLJSON-TRANSFORMATION-ROADMAP.md)** 🗺️
   - **Purpose:** Project planning and timeline
   - **Audience:** Project managers, team leads
   - **Sections:**
     - Current state analysis
     - Transformation strategy
     - File-by-file transformation plan
     - 6-week implementation timeline
     - Effort estimates (90-108 hours)
     - Critical path dependencies
     - Success criteria
     - Risk mitigation
     - Deliverables checklist
     - Quick reference commands
   - **Length:** ~900 lines
   - **Complexity:** 6/10
   - **Key Features:**
     - Week-by-week milestones
     - Per-file LOC estimates
     - Risk assessment matrix
     - Code statistics

---

## Implementation Workflow

### For First-Time Readers

**Day 1:**
1. Read **IDLJSON-PLUGIN-DESIGN.md** sections 1-4 (Overview, Architecture, Data Model)
2. Study the existing idlc source:
   - `cyclonedds/src/tools/idlc/src/libidlc/libidlc__types.c`
   - `cyclonedds/src/tools/idlc/src/libidlc/libidlc__generator.c`
3. Read the design talk: `cyclonedds/src/tools/idljson/transform-from-ildc.md`

**Day 2-3:**
1. Read **IDLJSON-PLUGIN-DESIGN.md** sections 5-9 (Layout Calc, Descriptor, QoS, JSON)
2. Review **IDLJSON-IMPLEMENTATION-GUIDE.md** sections 1-3
3. Set up development environment and test build

**Week 1 Implementation:**
1. Follow **IDLJSON-TRANSFORMATION-ROADMAP.md** Week 1 tasks
2. Reference **IDLJSON-IMPLEMENTATION-GUIDE.md** Steps 1-2
3. Use code snippets from **IDLJSON-PLUGIN-DESIGN.md** Section 3

**Weeks 2-6:**
1. Follow roadmap week-by-week
2. Use implementation guide for detailed code
3. Reference design doc for architecture decisions

---

## Quick Navigation

### By Role

#### **Architect / Tech Lead**
→ Start with: **IDLJSON-PLUGIN-DESIGN.md**  
→ Focus on: Sections 2, 3, 5, 6 (Architecture, Data Model, Layout, Descriptor)

#### **Developer**
→ Start with: **IDLJSON-IMPLEMENTATION-GUIDE.md**  
→ Then read: **IDLJSON-PLUGIN-DESIGN.md** Section 8 (JSON Output Format)  
→ Reference: **IDLJSON-TRANSFORMATION-ROADMAP.md** for current week's tasks

#### **Project Manager**
→ Start with: **IDLJSON-TRANSFORMATION-ROADMAP.md**  
→ Focus on: Sections 4, 7, 8 (Timeline, Success Criteria, Risks)

#### **QA / Tester**
→ Read: **IDLJSON-PLUGIN-DESIGN.md** Section 14 (Testing Strategy)  
→ Read: **IDLJSON-IMPLEMENTATION-GUIDE.md** Section 6 (Testing Checklist)

### By Task

#### **Understanding the Design**
- IDLJSON-PLUGIN-DESIGN.md § 2 (Architecture)
- IDLJSON-PLUGIN-DESIGN.md § 3 (Data Model)

#### **Implementing Data Model**
- IDLJSON-IMPLEMENTATION-GUIDE.md § 3 Step 1 (model.h)
- IDLJSON-IMPLEMENTATION-GUIDE.md § 3 Step 2 (model.c basics)
- IDLJSON-PLUGIN-DESIGN.md § 3.1 (Structure definitions)

#### **Implementing Layout Calculator**
- IDLJSON-IMPLEMENTATION-GUIDE.md § 3 Step 3 (Layout algorithm)
- IDLJSON-PLUGIN-DESIGN.md § 5 (Memory Layout Calculation)

#### **Implementing JSON Output**
- IDLJSON-IMPLEMENTATION-GUIDE.md § 3 Step 5-6 (JSON printers)
- IDLJSON-PLUGIN-DESIGN.md § 8 (JSON Output Format)
- IDLJSON-PLUGIN-DESIGN.md § 9 (JSON Emitter)

#### **Implementing Descriptor Extraction**
- IDLJSON-PLUGIN-DESIGN.md § 6 (Descriptor Extraction)
- IDLJSON-IMPLEMENTATION-GUIDE.md § 3 Step 4 (Offset lookup)

#### **Debugging Issues**
- IDLJSON-IMPLEMENTATION-GUIDE.md § 4 (Debugging Strategies)
- IDLJSON-IMPLEMENTATION-GUIDE.md § 5 (Common Pitfalls)

#### **Writing Tests**
- IDLJSON-IMPLEMENTATION-GUIDE.md § 6 (Testing Checklist)
- IDLJSON-PLUGIN-DESIGN.md § 14 (Testing Strategy)

---

## Key Concepts Reference

### Core Data Structures

| Structure | Purpose | Defined In |
|-----------|---------|------------|
| `dm_rec_t` | Type/member record | DESIGN § 3.1.1, GUIDE § 3 Step 1 |
| `dm_descriptor_t` | Topic descriptor | DESIGN § 3.1.2, GUIDE § 3 Step 1 |
| `dm_qos_t` | QoS settings | DESIGN § 3.1.3, GUIDE § 3 Step 1 |

### Core Algorithms

| Algorithm | Purpose | Defined In |
|-----------|---------|------------|
| Layout Calculation | Compute struct sizes/offsets | DESIGN § 5, GUIDE § 3 Step 3 |
| Opcode Extraction | Extract descriptor instructions | DESIGN § 6.2, DESIGN § 6.3 |
| JSON Serialization | Emit JSON output | DESIGN § 9, GUIDE § 3 Step 6 |

### Core Processes

| Process | Purpose | Defined In |
|---------|---------|------------|
| Type Extraction | Populate dm_types from AST | DESIGN § 4, ROADMAP § 3.2 |
| Visitor Pattern | Traverse IDL AST | DESIGN § 4.1, GUIDE § 2.1 |
| Dual-Mode Generation | C + JSON simultaneously | ROADMAP § 2.2, ROADMAP § 2.3 |

---

## Code Statistics Summary

| Metric | Value | Source |
|--------|-------|--------|
| New header files | 1 file (~150 LOC) | ROADMAP § A |
| New implementation files | 1 file (~600 LOC) | ROADMAP § A |
| Modified files | 4 files (+400 LOC) | ROADMAP § A |
| Total new/modified code | ~1150 LOC | ROADMAP § A |
| Estimated implementation time | 90-108 hours | ROADMAP § 5 |
| Implementation duration | 6 weeks (11-14 days) | ROADMAP § 5 |

---

## Dependencies and Prerequisites

### Required Knowledge

- ✅ C programming (structures, pointers, linked lists)
- ✅ IDL (Interface Definition Language) basics
- ✅ JSON format
- ✅ CMake build system
- ⚠️ Cyclone DDS architecture (can learn on the job)
- ⚠️ AST visitor pattern (explained in docs)

### Required Tools

- ✅ CMake 3.16+
- ✅ C Compiler (MSVC/GCC/Clang)
- ✅ Cyclone DDS source tree
- ✅ Git
- ⚠️ Valgrind (Linux only, for memory checks)
- ⚠️ Visual Studio Profiler (Windows only)

### Source Files to Study

Priority order:
1. `cyclonedds/src/tools/idlc/src/libidlc/libidlc__types.c` (visitor pattern)
2. `cyclonedds/src/tools/idlc/src/libidlc/libidlc__descriptor.c` (opcodes)
3. `cyclonedds/src/tools/idlc/src/libidlc/libidlc__generator.c` (orchestration)
4. `cyclonedds/src/core/ddsc/include/dds/ddsc/dds_opcodes.h` (opcode defs)
5. `transform-from-ildc.md` (design discussion)

---

## Testing Resources

### Unit Test Coverage

| Component | Test Location | Test Count |
|-----------|---------------|------------|
| Data Model | `tests/model_test.c` | ~10 tests |
| Layout Calculator | `tests/layout_test.c` | ~15 tests |
| JSON Emitter | `tests/json_test.c` | ~10 tests |

### Integration Test IDL Files

| File | Purpose | Coverage |
|------|---------|----------|
| `simple_struct.idl` | Basic primitives | Baseline |
| `nested_struct.idl` | Nested types | Recursion |
| `keyed_topic.idl` | Keys & QoS | Topics |
| `union_test.idl` | Unions | Discriminators |
| `enum_test.idl` | Enums | Enumerators |
| `complex_topic.idl` | All features | Full coverage |

### Performance Benchmarks

| IDL Size | Expected Time | Memory |
|----------|---------------|--------|
| Small (10 types) | < 50ms | < 1MB |
| Medium (100 types) | < 200ms | < 5MB |
| Large (1000 types) | < 1s | < 50MB |

---

## JSON Output Examples

### Simple Struct

**IDL:**
```idl
struct Point {
    long x;
    long y;
};
```

**JSON:**
```json
{
  "File": [
    { "Name": "point.idl", "_eof": 0 }
  ],
  "Types": [
    {
      "Name": "Point",
      "Kind": "struct",
      "Extensibility": "appendable",
      "Members": [
        { "Name": "x", "Type": "long", "Id": 0, "_eof": 0 },
        { "Name": "y", "Type": "long", "Id": 1, "_eof": 0 }
      ],
      "_eof": 0
    }
  ]
}
```

See **IDLJSON-PLUGIN-DESIGN.md § 8** for more examples (struct, enum, union, topic with descriptor).

---

## Build and Usage

### Build Commands

```bash
# Navigate to plugin directory
cd D:\Work\FastCycloneDdsCsharpBindings\cyclonedds\src\tools\idljson

# Create build directory
mkdir build
cd build

# Configure
cmake ..

# Build
cmake --build .

# Output: cycloneddsidljson.dll (Windows) or libcycloneddsidljson.so (Linux)
```

### Usage

```bash
# Generate JSON from IDL
idlc.exe -l json SpikeLauncher.idl

# Output: SpikeLauncher.idl.json
```

### Integration with C# Code Generator

```csharp
// Load JSON
var json = File.ReadAllText("SpikeLauncher.idl.json");
var model = JsonSerializer.Deserialize<IdlModel>(json);

// Generate C# code
foreach (var type in model.Types)
{
    if (type.Kind == "struct" && type.TopicDescriptor != null)
    {
        GenerateTopic(type);
    }
}
```

---

## Related Documents

### In This Repository

- `transform-from-ildc.md` - Original design discussion (3400+ lines)
- `SERDATA-TASK-MASTER.md` - Overall project task tracking
- `DESIGN-UPDATES-CLI-TOOL.md` - C# code generator design

### External References

- Cyclone DDS Documentation: https://cyclonedds.io/docs/
- IDL4 Specification: https://www.omg.org/spec/IDL/4.2/
- XTypes Specification: https://www.omg.org/spec/DDS-XTypes/

---

## Glossary

| Term | Definition |
|------|------------|
| **AST** | Abstract Syntax Tree - internal representation of parsed IDL |
| **CDR** | Common Data Representation - DDS serialization format |
| **XCDR2** | Extended CDR version 2 - supports extensibility |
| **Opcode** | Serialization instruction in topic descriptor |
| **Layout** | Memory organization of struct members (size, offset, alignment) |
| **Extensibility** | Type versioning mode (final/appendable/mutable) |
| **Topic Descriptor** | Serialization metadata (opcodes, keys, size) |
| **QoS** | Quality of Service - reliability, durability, history settings |
| **Dual-Mode** | Generating both C (to /dev/null) and JSON simultaneously |

---

## Changelog

### Version 1.0 (2026-01-23)

**Created:**
- IDLJSON-PLUGIN-DESIGN.md (complete architectural design)
- IDLJSON-IMPLEMENTATION-GUIDE.md (detailed implementation instructions)
- IDLJSON-TRANSFORMATION-ROADMAP.md (project timeline and planning)
- IDLJSON-DOCUMENTATION-INDEX.md (this document)

**Status:** Design phase complete, ready for implementation

---

## Next Steps

1. **Review and Approval** (1-2 days)
   - Architect reviews design document
   - Team reviews roadmap and estimates
   - Stakeholders approve project plan

2. **Environment Setup** (1 day)
   - Set up development environment
   - Clone Cyclone DDS repository
   - Test build of existing idlc plugin

3. **Week 1 Implementation** (Start immediately after approval)
   - Create `model.h` (data structures)
   - Create `model.c` (basic functions)
   - Test compilation

4. **Weekly Progress Reviews**
   - Every Friday: Demo current milestone
   - Review against roadmap
   - Adjust timeline if needed

---

## Support and Contact

### Questions About Design
→ Refer to: IDLJSON-PLUGIN-DESIGN.md  
→ Not covered? Check: transform-from-ildc.md

### Questions About Implementation
→ Refer to: IDLJSON-IMPLEMENTATION-GUIDE.md  
→ Debugging issues? See: § 4 Debugging Strategies

### Questions About Schedule
→ Refer to: IDLJSON-TRANSFORMATION-ROADMAP.md  
→ Timeline concerns? See: § 8 Risk Mitigation

### Cyclone DDS Specific Questions
→ Cyclone DDS GitHub: https://github.com/eclipse-cyclonedds/cyclonedds  
→ Mailing list: cyclonedds-dev@eclipse.org

---

## Document Maintenance

**Owner:** System Architect  
**Last Updated:** 2026-01-23  
**Review Cycle:** After each implementation week  
**Next Review:** 2026-01-30 (after Week 1 completion)

**Update Procedure:**
1. Implementation reveals issues → Update GUIDE § 5 (Pitfalls)
2. Timeline changes → Update ROADMAP § 4 (Timeline)
3. Design changes → Update DESIGN and increment version
4. New examples → Update DESIGN § 8 (JSON Format)

---

**END OF DOCUMENTATION INDEX**

**Status:** ✅ Complete and ready for implementation  
**Total Documentation:** ~8500 lines across 4 documents  
**Estimated Implementation:** 90-108 hours (6 weeks)  
**Next Action:** Begin Week 1 - Foundation
