# IDL JSON Plugin Implementation Guide

**Related:** IDLJSON-PLUGIN-DESIGN.md  
**Date:** 2026-01-23  
**Version:** 1.0  

---

## 1. Overview

This guide provides detailed implementation instructions for transforming the `idlc` plugin into the `idljson` plugin. It complements the design document with code-level details, debugging strategies, and gotchas.

---

## 2. Prerequisites

### 2.1 Development Environment

**Required Tools:**
- CMake 3.16+
- C Compiler (MSVC on Windows, GCC/Clang on Linux)
- Cyclone DDS source tree
- Git for version control

**Knowledge Required:**
- C programming (structures, pointers, linked lists)
- IDL specification basics
- JSON format
- CMake build system

### 2.2 Source Familiarity

**Read these files first:**
1. `cyclonedds/src/tools/idlc/src/libidlc/libidlc__types.c` - Understand visitor pattern
2. `cyclonedds/src/tools/idlc/src/libidlc/libidlc__descriptor.c` - Opcode generation  
3. `cyclonedds/src/core/ddsc/include/dds/ddsc/dds_opcodes.h` - Opcode definitions
4. Transform design talk: `idljson/transform-from-ildc.md`

---

## 3. Step-by-Step Implementation

### Step 1: Create Data Model Header

**File:** `src/libidlc/model.h`

```c
#ifndef MODEL_H
#define MODEL_H

#include <stdio.h>
#include <stdint.h>

// Value type enumeration
enum dm_type {
    DM_TYPE_BOOL,
    DM_TYPE_INT,
    DM_TYPE_UNSIGNED_INT,
    DM_TYPE_DOUBLE,
    DM_TYPE_LONG_DOUBLE,
    DM_TYPE_STRING,
};

// Value union
typedef union dm_value {
    uint8_t bln;
    uint64_t uint64;
    int64_t int64;
    double dbl;
    long double ldbl;
    char* str;
} dm_value_t;

// QoS settings
typedef struct dm_qos {
    char* reliability;
    char* durability;
    char* history;
    int32_t depth;
} dm_qos_t;

// Topic descriptor
typedef struct dm_descriptor {
    uint32_t size;
    uint32_t align;
    uint32_t flagset;
    char* typename;
    
    struct {
        char* name;
        uint32_t offset;
        uint32_t order;
    } *keys;
    uint32_t n_keys;
    
    uint32_t* ops;
    uint32_t n_ops;
} dm_descriptor_t;

// Type record
typedef struct dm_rec {
    // Identity
    char* name;
    char* c_name;
    char* kind;
    char* type;
    
    // Metadata
    int is_key;
    int member_id;
    int has_explicit_id;
    int is_optional;
    int is_external;
    char* extensibility;
    
    // Collections
    int is_array;
    int size;
    uint32_t bound;
    
    // Layout
    int offset;
    int align;
    
    // Values
    int has_value;
    enum dm_type value_type;
    union dm_value value;
    
    // Relationships
    struct dm_rec* members;
    struct dm_rec* next;
    
    // Union specific
    char* discriminator;
    struct dm_rec* labels;
    
    // Topic specific
    dm_descriptor_t* topic_descriptor;
    dm_qos_t* qos;
} dm_rec_t;

// Global state
extern dm_rec_t* dm_sources;
extern dm_rec_t* dm_types;
extern dm_rec_t* dm_last_struct;
extern dm_rec_t* dm_last_enum;

// Functions
extern dm_rec_t* dm_new(void);
extern dm_rec_t* dm_add(dm_rec_t** list, dm_rec_t* item);
extern void dm_fprint(FILE* fh);
extern void dm_calculate_layout(dm_rec_t* struct_rec);
extern int dm_get_member_offset(const char* type_c_name, const char* member_name);
extern dm_rec_t* dm_find_by_name(dm_rec_t* list, const char* name);
extern dm_rec_t* dm_find_by_c_name(dm_rec_t* list, const char* c_name);

#endif /* MODEL_H */
```

**Key Points:**
- All fields nullable (use NULL checks)
- c_name stores C-mangled names for offset lookups
- descriptor and qos only set for topic types

---

### Step 2: Implement Data Model Core

**File:** `src/libidlc/model.c`

**2.1 Global State:**

```c
#include "model.h"
#include "idl/heap.h"
#include "idl/string.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <inttypes.h>

dm_rec_t* dm_sources = NULL;
dm_rec_t* dm_types = NULL;
dm_rec_t* dm_last_struct = NULL;
dm_rec_t* dm_last_enum = NULL;
```

**2.2 Basic Operations:**

```c
dm_rec_t* dm_new(void) {
    return (dm_rec_t*)calloc(1, sizeof(dm_rec_t));
}

dm_rec_t* dm_add(dm_rec_t** list, dm_rec_t* item) {
    if (!list || !item) return NULL;
    
    if (*list == NULL) {
        *list = item;
        return item;
    }
    
    dm_rec_t* p = *list;
    while (p->next) {
        p = p->next;
    }
    p->next = item;
    return item;
}
```

**2.3 Search Functions:**

```c
dm_rec_t* dm_find_by_name(dm_rec_t* list, const char* name) {
    if (!name) return NULL;
    
    for (dm_rec_t* p = list; p != NULL; p = p->next) {
        if (p->name && strcmp(p->name, name) == 0) {
            return p;
        }
    }
    return NULL;
}

dm_rec_t* dm_find_by_c_name(dm_rec_t* list, const char* c_name) {
    if (!c_name) return NULL;
    
    for (dm_rec_t* p = list; p != NULL; p = p->next) {
        if (p->c_name && strcmp(p->c_name, c_name) == 0) {
            return p;
        }
    }
    return NULL;
}

static dm_rec_t* find_member_by_name(dm_rec_t* type_rec, const char* name) {
    if (!type_rec || !name) return NULL;
    
    for (dm_rec_t* m = type_rec->members; m != NULL; m = m->next) {
        if (m->name && strcmp(m->name, name) == 0) {
            return m;
        }
    }
    return NULL;
}
```

**Critical Note:**
- Always use `idl_strdup()` when storing strings (proper cleanup)
- Never store direct pointers to AST nodes

---

### Step 3: Implement Layout Calculator

**File:** `src/libidlc/model.c` (continued)

```c
static size_t get_primitive_size_align(const char* type_name) {
    if (!type_name) return 0;
    
    if (strcmp(type_name, "boolean") == 0) return 1;
    if (strcmp(type_name, "char") == 0) return 1;
    if (strcmp(type_name, "octet") == 0) return 1;
    if (strcmp(type_name, "short") == 0) return 2;
    if (strcmp(type_name, "unsigned short") == 0) return 2;
    if (strcmp(type_name, "long") == 0) return 4;
    if (strcmp(type_name, "unsigned long") == 0) return 4;
    if (strcmp(type_name, "long long") == 0) return 8;
    if (strcmp(type_name, "unsigned long long") == 0) return 8;
    if (strcmp(type_name, "float") == 0) return 4;
    if (strcmp(type_name, "double") == 0) return 8;
    if (strcmp(type_name, "long double") == 0) return 16;
    if (strcmp(type_name, "string") == 0) return 8;
    if (strcmp(type_name, "wstring") == 0) return 8;
    
    return 0;
}

static uint32_t align_up(uint32_t offset, size_t alignment) {
    if (alignment == 0) return offset;
    size_t remainder = offset % alignment;
    return (remainder == 0) ? offset : offset + (alignment - remainder);
}

void dm_calculate_layout(dm_rec_t* struct_rec) {
    if (!struct_rec || !struct_rec->members) return;
    
    uint32_t cursor = 0;
    uint32_t max_align = 1;
    
    int is_union = (struct_rec->kind && strcmp(struct_rec->kind, "union") == 0);
    
    for (dm_rec_t* member = struct_rec->members; member != NULL; member = member->next) {
        size_t member_size = 0;
        size_t member_align = 1;
        
        // Check primitive types
        size_t prim_size = get_primitive_size_align(member->type);
        
        if (prim_size > 0) {
            member_size = prim_size;
            member_align = (prim_size >= 8) ? 8 : prim_size;
        } else {
            // Complex type - lookup
            dm_rec_t* nested = dm_find_by_name(dm_types, member->type);
            
            if (nested && nested->size > 0) {
                member_size = nested->size;
                member_align = nested->align;
            } else if (strstr(member->type, "sequence")) {
                // DDS sequence: {uint32, uint32, T*, bool} = 24 bytes aligned to 8
                member_size = 24;
                member_align = 8;
            } else {
                // Assume enum or unknown (4-byte aligned)
                member_size = 4;
                member_align = 4;
            }
        }
        
        // Handle arrays
        if (member->is_array && member->size > 0) {
            member_size *= member->size;
        }
        
        // Apply padding
        if (!is_union) {
            cursor = align_up(cursor, member_align);
        } else {
            cursor = 0;  // Union members overlay
        }
        
        member->offset = cursor;
        
        if (!is_union) {
            cursor += member_size;
        } else {
            if (member_size > cursor) cursor = member_size;
        }
        
        if (member_align > max_align) {
            max_align = member_align;
        }
    }
    
    // Final padding
    struct_rec->size = align_up(cursor, max_align);
    struct_rec->align = max_align;
}
```

**Testing:**
```c
// Test case: struct { char a; long b; }
// Expected: a at offset 0, b at offset 4, size 8
```

---

### Step 4: Implement Member Offset Lookup

```c
int dm_get_member_offset(const char* type_c_name, const char* member_name) {
    if (!type_c_name || !member_name) return 0;
    
    dm_rec_t* type_rec = dm_find_by_c_name(dm_types, type_c_name);
    if (!type_rec) {
        // Fallback: try IDL name
        type_rec = dm_find_by_name(dm_types, type_c_name);
        if (!type_rec) return 0;
    }
    
    // Handle nested paths (e.g., "ProcessAddr.StationId")
    char* path_copy = idl_strdup(member_name);
    char* saveptr = NULL;
    char* token = strtok_r(path_copy, ".", &saveptr);
    dm_rec_t* current_type = type_rec;
    int current_offset = 0;
    
    while (token != NULL) {
        dm_rec_t* member = find_member_by_name(current_type, token);
        if (!member) {
            free(path_copy);
            return 0;
        }
        
        current_offset += member->offset;
        
        token = strtok_r(NULL, ".", &saveptr);
        if (token) {
            // Navigate to member's type
            current_type = dm_find_by_name(dm_types, member->type);
            if (!current_type) {
                free(path_copy);
                return 0;
            }
        }
    }
    
    free(path_copy);
    return current_offset;
}
```

**Windows Note:**
- Windows doesn't have `strtok_r`, use `strtok_s` instead or implement portable version

---

### Step 5: Implement JSON Escaping

```c
static const char* dm_escapize(const char* s) {
    static char buf[2048];
    buf[0] = '\0';
    if (!s) return buf;
    
    const char* c = s;
    char* d = buf;
    
    while (*c && (d - buf) < 2046) {
        if (*c == '\\' || *c == '"') {
            *d++ = '\\';
        }
        *d++ = *c++;
    }
    *d = '\0';
    
    return buf;
}

static void dm_indent(FILE* fh, int indent) {
    for (int i = 0; i < indent * 2; i++) {
        fprintf(fh, " ");
    }
}
```

---

### Step 6: Implement JSON Printers

**Value Printer:**

```c
static void dm_print_value(FILE* fh, dm_rec_t* rec, int indent) {
    if (!rec->has_value) return;
    
    dm_indent(fh, indent);
    
    switch (rec->value_type) {
        case DM_TYPE_BOOL:
            fprintf(fh, "\"Value\": %s,\n", rec->value.bln ? "true" : "false");
            break;
        case DM_TYPE_INT:
            fprintf(fh, "\"Value\": %" PRId64 ",\n", rec->value.int64);
            break;
        case DM_TYPE_UNSIGNED_INT:
            fprintf(fh, "\"Value\": %" PRIu64 ",\n", rec->value.uint64);
            break;
        case DM_TYPE_DOUBLE:
            fprintf(fh, "\"Value\": %lf,\n", rec->value.dbl);
            break;
        case DM_TYPE_LONG_DOUBLE:
            fprintf(fh, "\"Value\": %Lf,\n", rec->value.ldbl);
            break;
        case DM_TYPE_STRING:
            fprintf(fh, "\"Value\": \"%s\",\n", dm_escapize(rec->value.str));
            break;
    }
}
```

**Label Printer:**

```c
static void dm_print_labels(FILE* fh, dm_rec_t* labels, int indent) {
    if (!labels) return;
    
    dm_indent(fh, indent);
    fprintf(fh, "\"Labels\": [\n");
    
    for (dm_rec_t* l = labels; l; l = l->next) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"%s\"", dm_escapize(l->name));
        if (l->next) fprintf(fh, ",");
        fprintf(fh, "\n");
    }
    
    dm_indent(fh, indent);
    fprintf(fh, "],\n");
}
```

**QoS Printer:**

```c
static void dm_print_qos(FILE* fh, dm_qos_t* qos, int indent) {
    if (!qos) return;
    
    dm_indent(fh, indent);
    fprintf(fh, "\"QoS\": {\n");
    
    if (qos->reliability) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Reliability\": \"%s\",\n", qos->reliability);
    }
    
    if (qos->durability) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Durability\": \"%s\",\n", qos->durability);
    }
    
    if (qos->history) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"History\": \"%s\",\n", qos->history);
    }
    
    dm_indent(fh, indent + 1);
    fprintf(fh, "\"HistoryDepth\": %d\n", qos->depth);
    
    dm_indent(fh, indent);
    fprintf(fh, "},\n");
}
```

**Descriptor Printer:**

```c
static void dm_print_descriptor(FILE* fh, dm_descriptor_t* desc, int indent) {
    if (!desc) return;
    
    dm_indent(fh, indent);
    fprintf(fh, "\"TopicDescriptor\": {\n");
    
    dm_indent(fh, indent + 1);
    fprintf(fh, "\"Size\": %u,\n", desc->size);
    
    dm_indent(fh, indent + 1);
    fprintf(fh, "\"Align\": %u,\n", desc->align);
    
    dm_indent(fh, indent + 1);
    fprintf(fh, "\"FlagSet\": %u,\n", desc->flagset);
    
    dm_indent(fh, indent + 1);
    fprintf(fh, "\"TypeName\": \"%s\",\n", desc->typename ? desc->typename : "");
    
    // Keys
    dm_indent(fh, indent + 1);
    fprintf(fh, "\"Keys\": [\n");
    for (uint32_t i = 0; i < desc->n_keys; i++) {
        dm_indent(fh, indent + 2);
        fprintf(fh, "{ \"Name\": \"%s\", \"Offset\": %u, \"Order\": %u }",
                desc->keys[i].name,
                desc->keys[i].offset,
                desc->keys[i].order);
        if (i < desc->n_keys - 1) fprintf(fh, ",");
        fprintf(fh, "\n");
    }
    dm_indent(fh, indent + 1);
    fprintf(fh, "],\n");
    
    // Ops
    dm_indent(fh, indent + 1);
    fprintf(fh, "\"Ops\": [\n");
    for (uint32_t i = 0; i < desc->n_ops; i++) {
        if (i % 8 == 0) dm_indent(fh, indent + 2);
        fprintf(fh, "%u", desc->ops[i]);
        if (i < desc->n_ops - 1) fprintf(fh, ", ");
        if ((i + 1) % 8 == 0 || i == desc->n_ops - 1) fprintf(fh, "\n");
    }
    dm_indent(fh, indent + 1);
    fprintf(fh, "]\n");
    
    dm_indent(fh, indent);
    fprintf(fh, "},\n");
}
```

**Main Record Printer:**

```c
static void dm_print_list(FILE* fh, dm_rec_t* list, int indent);  // Forward decl

static void dm_print_rec(FILE* fh, dm_rec_t* rec, int indent) {
    if (!rec) return;
    
    dm_indent(fh, indent);
    fprintf(fh, "{\n");
    
    // Identity
    if (rec->name) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Name\": \"%s\",\n", dm_escapize(rec->name));
    }
    
    if (rec->kind) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Kind\": \"%s\",\n", dm_escapize(rec->kind));
    }
    
    if (rec->type) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Type\": \"%s\",\n", dm_escapize(rec->type));
    }
    
    // Annotations
    if (rec->extensibility) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Extensibility\": \"%s\",\n", rec->extensibility);
    }
    
    if (rec->discriminator) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Discriminator\": \"%s\",\n", dm_escapize(rec->discriminator));
    }
    
    if (rec->is_key) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"IsKey\": true,\n");
    }
    
    if (rec->has_explicit_id) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Id\": %d,\n", rec->member_id);
    }
    
    if (rec->is_optional) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"IsOptional\": true,\n");
    }
    
    if (rec->is_external) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"IsExternal\": true,\n");
    }
    
    if (rec->bound > 0) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Bound\": %u,\n", rec->bound);
    }
    
    if (rec->is_array) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"CollectionType\": \"array\",\n");
        if (rec->size > 0) {
            dm_indent(fh, indent + 1);
            fprintf(fh, "\"Size\": %d,\n", rec->size);
        }
    } else if (rec->kind && strcmp(rec->kind, "sequence") == 0) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"CollectionType\": \"sequence\",\n");
    }
    
    // Labels
    if (rec->labels) {
        dm_print_labels(fh, rec->labels, indent + 1);
    }
    
    // Value
    if (rec->has_value) {
        dm_print_value(fh, rec, indent + 1);
    }
    
    // QoS
    if (rec->qos) {
        dm_print_qos(fh, rec->qos, indent + 1);
    }
    
    // Descriptor
    if (rec->topic_descriptor) {
        dm_print_descriptor(fh, rec->topic_descriptor, indent + 1);
    }
    
    // Members
    if (rec->members) {
        dm_indent(fh, indent + 1);
        fprintf(fh, "\"Members\":\n");
        dm_print_list(fh, rec->members, indent + 1);
        fprintf(fh, ",\n");
    }
    
    // EOF marker
    dm_indent(fh, indent + 1);
    fprintf(fh, "\"_eof\": 0\n");
    
    dm_indent(fh, indent);
    fprintf(fh, "}");
}

static void dm_print_list(FILE* fh, dm_rec_t* list, int indent) {
    dm_indent(fh, indent);
    fprintf(fh, "[\n");
    
    for (dm_rec_t* rec = list; rec != NULL; rec = rec->next) {
        dm_print_rec(fh, rec, indent + 1);
        if (rec->next) fprintf(fh, ",");
        fprintf(fh, "\n");
    }
    
    dm_indent(fh, indent);
    fprintf(fh, "]");
}

void dm_fprint(FILE* fh) {
    fprintf(fh, "{\n");
    fprintf(fh, "  \"File\":\n");
    dm_print_list(fh, dm_sources, 2);
    fprintf(fh, ",\n");
    fprintf(fh, "  \"Types\":\n");
    dm_print_list(fh, dm_types, 2);
    fprintf(fh, "\n}\n");
}
```

---

## 4. Debugging Strategies

### 4.1 Enable Debug Output

```c
#ifdef DEBUG_JSON
#define JSON_DEBUG(fmt, ...) fprintf(stderr, "[JSON] " fmt "\n", ##__VA_ARGS__)
#else
#define JSON_DEBUG(fmt, ...)
#endif

// Usage:
JSON_DEBUG("Processing struct: %s", struct_name);
JSON_DEBUG("Member %s at offset %d", member_name, offset);
```

### 4.2 Validate Layout

Create test helper:

```c
void dm_debug_layout(dm_rec_t* rec) {
    if (!rec) return;
    
    fprintf(stderr, "Type: %s\n", rec->name);
    fprintf(stderr, "  Size: %d, Align: %d\n", rec->size, rec->align);
    
    for (dm_rec_t* m = rec->members; m; m = m->next) {
        fprintf(stderr, "  Member: %s  Offset: %d\n", m->name, m->offset);
    }
}
```

### 4.3 Dump AST Nodes

```c
void dm_debug_node(const void* node) {
    idl_type_t type = idl_type(node);
    
    fprintf(stderr, "Node type: 0x%x\n", type);
    
    if (idl_is_struct(node)) {
        const idl_struct_t* s = (const idl_struct_t*)node;
        fprintf(stderr, "  Struct: %s\n", s->name->identifier);
        fprintf(stderr, "  Extensibility: %d\n", idl_extensibility(node));
    }
}
```

### 4.4 Compare with Original idlc Output

```bash
# Generate both C and JSON
idlc -l c test.idl
idlc -l json test.idl

# Manually verify sizes match:
# 1. Check sizeof() in generated .c file
# 2. Check "Size" in generated .json file
```

---

## 5. Common Pitfalls

### 5.1 Memory Leaks

**Problem:** Forgetting to duplicate strings

```c
// WRONG
rec->name = node->name->identifier;  // Dangling pointer!

// CORRECT
rec->name = idl_strdup(node->name->identifier);
```

**Solution:** Always use `idl_strdup()` from `idl/heap.h`

### 5.2 NULL Pointer Dereferencing

**Problem:** Not checking NULL before access

```c
// WRONG
if (rec->qos->reliability) { ... }  // Crash if qos is NULL!

// CORRECT
if (rec->qos && rec->qos->reliability) { ... }
```

### 5.3 Circular Type References

**Problem:** Layout calculator infinite loop

```c
struct A {
    B* ptr;  // Pointer to B (8 bytes, not infinite)
};
```

**Solution:** Pointers always have fixed size (8 on x64), sequence members too

### 5.4 Trailing Commas in JSON

**Problem:** Invalid JSON due to trailing comma

```json
{
  "Members": [
    { "Name": "field1" },
    { "Name": "field2" },   // <- Trailing comma!
  ]
}
```

**Solution:** Use `_eof` marker technique or careful comma logic

### 5.5 Windows vs. Linux Path Differences

**Problem:** Hardcoded `/dev/null`

```c
// WRONG
gen.header.handle = fopen("/dev/null", "wb");

// CORRECT
#ifdef _WIN32
gen.header.handle = fopen("nul", "wb");
#else
gen.header.handle = fopen("/dev/null", "wb");
#endif
```

---

## 6. Testing Checklist

### 6.1 Basic Types

- [ ] boolean
- [ ] char, octet
- [ ] short, unsigned short
- [ ] long, unsigned long
- [ ] long long, unsigned long long
- [ ] float, double, long double
- [ ] string, wstring

### 6.2 Collections

- [ ] Unbounded string
- [ ] Bounded string (`string<64>`)
- [ ] Unbounded sequence
- [ ] Bounded sequence (`sequence<MyType, 100>`)
- [ ] Single-dimension array (`long arr[10]`)
- [ ] Multi-dimension array (`double matrix[4][4]`)

### 6.3 Complex Types

- [ ] Simple struct
- [ ] Nested struct
- [ ] Struct with enums
- [ ] Struct with unions
- [ ] Recursive types (via pointer)

### 6.4 Annotations

- [ ] @key
- [ ] @id(x)
- [ ] @optional
- [ ] @external
- [ ] @default(value)

### 6.5 Extensibility

- [ ] @final
- [ ] @appendable
- [ ] @mutable

### 6.6 QoS Pragmas

- [ ] #pragma keylist
- [ ] #pragma topic
- [ ] Reliability (reliable, best_effort)
- [ ] Durability (volatile, transient_local, transient, persistent)
- [ ] History (keep_last N, keep_all)

### 6.7 Edge Cases

- [ ] Empty struct
- [ ] Single-member struct
- [ ] Large struct (1000+ members)
- [ ] Deep nesting (10+ levels)
- [ ] All members optional

---

## 7. CMake Integration

### 7.1 Modify CMakeLists.txt

```cmake
# Add new sources
set(
  libidlc_srcs
  src/libidlc/libidlc__types.h
  src/libidlc/libidlc__descriptor.h
  src/libidlc/libidlc__generator.h
  src/libidlc/libidlc__descriptor.c
  src/libidlc/libidlc__generator.c
  src/libidlc/libidlc__types.c
  src/libidlc/model.h              # NEW
  src/libidlc/model.c)             # NEW

# Change output library name
set_target_properties(libidlc PROPERTIES
   OUTPUT_NAME "cycloneddsidljson"
   VERSION ${PROJECT_VERSION}
   SOVERSION ${PROJECT_VERSION_MAJOR}
   C_STANDARD 99)
```

### 7.2 Build Commands

```bash
# From cyclonedds/src/tools/idljson
mkdir build
cd build
cmake ..
cmake --build .

# Output: cycloneddsidljson.dll (or .so on Linux)
```

### 7.3 Installation

```bash
cmake --install . --prefix /path/to/install
```

---

## 8. Performance Profiling

### 8.1 Measure Generation Time

```c
#include <time.h>

clock_t start = clock();

// ... generate JSON ...

clock_t end = clock();
double elapsed = (double)(end - start) / CLOCKS_PER_SEC;
fprintf(stderr, "JSON generation took %.3f seconds\n", elapsed);
```

### 8.2 Memory Usage

On Linux:

```bash
valgrind --leak-check=full --show-leak-kinds=all \
         idlc -l json large_file.idl
```

On Windows (Visual Studio):

```
Performance Profiler -> Memory Usage
```

---

## 9. Version Control Strategy

### 9.1 Branch Structure

```
main
  └── feature/idljson-plugin
       ├── step1-data-model
       ├── step2-layout-calc
       ├── step3-json-emitter
       ├── step4-descriptor-extractor
       └── step5-integration
```

### 9.2 Commit Message Format

```
[idljson] Add data model structures

- Created model.h with dm_rec_t, dm_descriptor_t, dm_qos_t
- Implemented dm_new(), dm_add(), dm_find_by_name()
- Added global state variables

Ref: IDLJSON-PLUGIN-DESIGN.md Section 3
```

---

## 10. Documentation Updates

### 10.1 Add README

**File:** `cyclonedds/src/tools/idljson/README.md`

```markdown
# IDL JSON Plugin

Generates JSON output from IDL files for use with external code generators.

## Usage

```
bash
idlc -l json myfile.idl
```

Output: `myfile.idl.json`

## JSON Format

See `docs/IDLJSON-PLUGIN-DESIGN.md` for complete specification.
```

---

## 11. Next Steps After Implementation

1. **Submit Pull Request** to Cyclone DDS upstream
2. **Update C# Code Generator** to consume JSON
3. **Create Integration Tests** in C# project
4. **Benchmark Performance** vs. regex parsing
5. **Document Migration** from old approach

---

**End of Implementation Guide**
