# C# to C Roundtrip Testing Framework - Documentation Index

**Quick navigation to all documentation for the minimalistic roundtrip testing framework**

---

## 🚀 Getting Started

**New to this framework? Start here:**

1. **[Implementation Summary](IMPLEMENTATION-SUMMARY.md)** - Overview of what's been created and next steps
2. **[Quick Start Guide](../tests/CsharpToC.Roundtrip.Tests/QUICKSTART.md)** - 5-step guide to run your first test
3. **[Framework README](../tests/CsharpToC.Roundtrip.Tests/README.md)** - Project structure and topic catalog

---

## 📚 Core Documentation

### Design & Architecture

**[C# to C Roundtrip Design](CSHARP-TO-C-ROUNDTRIP-DESIGN.md)**
- Complete framework architecture
- 72 minimalistic test topic definitions
- 3-phase testing workflow (Receive, Serialize, Send)
- CDR dump format specification
- Native and C# implementation patterns
- Success criteria

**When to read**: Understanding overall design, seeing implementation examples

---

### Integration Guides

**[IdlJson Integration Guide](IDLJSON-INTEGRATION-GUIDE.md)**
- Step-by-step guide for adding topics to IdlJson.Tests
- Verification macros and commands
- Troubleshooting common issues
- Quick reference

**When to read**: Adding any new topic to IdlJson verification

---

**[Atomic Tests IdlJson Integration](ATOMIC-TESTS-IDLJSON-INTEGRATION.md)**
- Batch-by-batch integration of all 72 topics
- Complete code snippets for verifier.c
- 10 batches organized by complexity
- Verification checklist
- Shell script for automation

**When to read**: Systematically verifying all atomic test topics

---

## 🎯 Reference Documents

### Test Framework

**[CsharpToC.Roundtrip.Tests README](../tests/CsharpToC.Roundtrip.Tests/README.md)**
- Project structure
- 72 topics organized by category
- Testing workflow explanation
- Debugging with CDR dumps
- Success criteria

**When to read**: Day-to-day reference while working with tests

---

**[Quick Start Guide](../tests/CsharpToC.Roundtrip.Tests/QUICKSTART.md)**
- Prerequisites checklist
- 5-step getting started
- First test run (BooleanTopic)
- Troubleshooting guide
- Testing strategy timeline

**When to read**: First time setup, troubleshooting issues

---

### Original Roundtrip Framework (Complex Topics)

**[Roundtrip Design](ROUNDTRIP-DESIGN.md)**
- Original framework for complex topics
- Seed-based verification strategy
- AllPrimitives, CompositeKey topics

**When to read**: Understanding the original approach and why we need atomic tests

---

### Historical Context

**[Fixing C# to Native Issues](Fixing-csharp-to-native-issues.md)**
- Record of debugging attempts
- Key technical findings
- Known issues with sequences
- C# vs C layout mismatch analysis

**When to read**: Understanding what's been tried and what failed

---

## 🗂️ Documentation by Use Case

### "I want to understand the framework"
1. [Implementation Summary](IMPLEMENTATION-SUMMARY.md)
2. [C# to C Roundtrip Design](CSHARP-TO-C-ROUNDTRIP-DESIGN.md)
3. [Framework README](../tests/CsharpToC.Roundtrip.Tests/README.md)

---

### "I want to get it running"
1. [Quick Start Guide](../tests/CsharpToC.Roundtrip.Tests/QUICKSTART.md)
2. [IdlJson Integration Guide](IDLJSON-INTEGRATION-GUIDE.md)
3. [Atomic Tests IdlJson Integration](ATOMIC-TESTS-IDLJSON-INTEGRATION.md)

---

### "I want to add a new topic"
1. [IdlJson Integration Guide](IDLJSON-INTEGRATION-GUIDE.md)
2. [Atomic Tests IdlJson Integration](ATOMIC-TESTS-IDLJSON-INTEGRATION.md) (see relevant batch)
3. [C# to C Roundtrip Design](CSHARP-TO-C-ROUNDTRIP-DESIGN.md) (section 7: Native Implementation)

---

### "I want to debug a failing test"
1. [Quick Start Guide](../tests/CsharpToC.Roundtrip.Tests/QUICKSTART.md) (Troubleshooting section)
2. [Framework README](../tests/CsharpToC.Roundtrip.Tests/README.md) (Debugging with CDR Dumps)
3. [Fixing C# to Native Issues](Fixing-csharp-to-native-issues.md) (Known issues)

---

### "I want to understand sequences issues"
1. [Fixing C# to Native Issues](Fixing-csharp-to-native-issues.md)
2. [C# to C Roundtrip Design](CSHARP-TO-C-ROUNDTRIP-DESIGN.md) (section 3.6: Simple Sequences)
3. [Atomic Tests IdlJson Integration](ATOMIC-TESTS-IDLJSON-INTEGRATION.md) (Batch 6: Sequences)

---

## 📋 Implementation Checklist

Use this checklist to track implementation progress:

### Phase 0: IdlJson Verification
- [ ] Batch 1: Primitives (14 topics) - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-1-basic-primitives-14-topics)
- [ ] Batch 2: Enums (2 topics) - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-2-enumerations-2-topics--enums)
- [ ] Batch 3: Nested Structs (4 topics) - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-3-nested-structures-4-topics--structs)
- [ ] Batch 4: Unions (4 topics) - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-4-unions-4-topics--union-types)
- [ ] Batch 5: Optionals (6 topics) - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-5-optional-fields-6-topics)
- [ ] Batch 6: Sequences (11 topics) ⚠️ **CRITICAL** - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-6-sequences-11-topics--critical-batch)
- [ ] Batch 7: Arrays (6 topics) - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-7-arrays-6-topics)
- [ ] Batch 8: Extensibility (6 topics) - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-8-extensibility-6-topics)
- [ ] Batch 9: Composite Keys (4 topics) - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-9-composite-keys-4-topics)
- [ ] Batch 10: Nested Keys (3 topics) - [Guide](ATOMIC-TESTS-IDLJSON-INTEGRATION.md#batch-10-nested-keys-3-topics--key-structs)

### Phase 1: Native Implementation
- [ ] CMakeLists.txt created
- [ ] Test registry pattern implemented
- [ ] BooleanTopic handler
- [ ] Int32Topic handler
- [ ] All primitive handlers
- [ ] Sequence handlers
- [ ] All 72 handlers complete

### Phase 2: C# Implementation
- [ ] Project structure created
- [ ] TestRunner implemented
- [ ] CdrDumper implemented
- [ ] DataGenerator implemented
- [ ] BooleanTopic test
- [ ] All primitive tests
- [ ] Sequence tests
- [ ] All 72 tests complete

### Phase 3: Testing & Validation
- [ ] BooleanTopic passes all 3 phases
- [ ] All primitives pass (14 topics)
- [ ] Enums pass (2 topics)
- [ ] Nested structs pass (4 topics)
- [ ] **Sequences pass (11 topics)** ← Breakthrough moment
- [ ] All 72 topics pass
- [ ] CI/CD integration
- [ ] Documentation updates

---

## 🔗 Related Documentation

### IDL and JSON
- [IDLJSON README](../cyclonedds/src/tools/idljson/IDLJSON-README.md) - idlc JSON plugin documentation
- [IDL Generation Guide](IDL-GENERATION.md) - How IDL is processed

### DDS Internals
- [Serdata Design](SERDATA-DESIGN.md) - Understanding DDS data representation
- [XCDR2 Implementation](XCDR2-IMPLEMENTATION-DETAILS.md) - CDR encoding details

### C# Bindings
- [Advanced IDL Generation Design](ADVANCED-IDL-GENERATION-DESIGN.md)
- [Advanced Optimizations Design](ADVANCED-OPTIMIZATIONS-DESIGN.md)

---

## 📊 Quick Stats

| Metric | Count |
|--------|-------|
| **Documentation Files** | 6 core docs + this index |
| **Test Topics Defined** | 72 minimalistic topics |
| **Testing Phases** | 3 per topic (Receive, Serialize, Send) |
| **Total Tests** | 216 (72 topics × 3 phases) |
| **Categories** | 12 (primitives, enums, structs, unions, optionals, sequences, arrays, extensibility, keys, combos, edge cases) |
| **Current Blocker** | Sequences (11 topics) |

---

## 🎯 The Path Forward

```
Step 1: IdlJson Verification
  ↓
  All 72 topics verified (sizes, ops, keys match C compiler)
  ↓
Step 2: Implement First Test
  ↓
  BooleanTopic passes all 3 phases
  ↓
Step 3: Scale to Primitives
  ↓
  All 14 primitive topics pass
  ↓
Step 4: Tackle Sequences
  ↓
  Debug SequenceInt32Topic using CDR dumps
  Fix C# serializer/deserializer
  Verify all 11 sequence topics pass
  ↓
Step 5: Complete Remaining Categories
  ↓
  Arrays, Optionals, Unions, Keys, etc.
  ↓
Step 6: Production Ready
  ↓
  All 72 topics pass, CI/CD integrated, documented
```

---

## 🆘 Need Help?

**Can't find what you're looking for?**

1. Check this index for the right document
2. Use the "Documentation by Use Case" section
3. Search for keywords in relevant docs
4. Check the Implementation Checklist for progress tracking

**Found an issue or gap in documentation?**

Document it in [Fixing-csharp-to-native-issues.md](Fixing-csharp-to-native-issues.md) for future reference.

---

**Last Updated**: January 25, 2026  
**Framework Version**: 2.0 (Minimalistic Atomic Testing)  
**Status**: Design Complete - Implementation Ready
