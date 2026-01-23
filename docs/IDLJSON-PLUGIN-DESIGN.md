# IDL JSON Plugin Design Document

**Author:** System Architect  
**Date:** 2026-01-23  
**Version:** 1.0  
**Project:** Fast Cyclone DDS C# Bindings  

---

## 1. Executive Summary

This document describes the comprehensive design for transforming the Cyclone DDS `idlc` compiler plugin into a JSON-generating plugin (`idljson`). The new plugin will export complete type metadata and topic descriptors in JSON format, enabling seamless interoperability with C# code generation tools without requiring manual parsing of generated C code.

### 1.1 Objectives

- **Primary Goal**: Create a plugin that generates structured JSON output from IDL files
- **Output**: A shared library `cycloneddsidljson.dll` that can be loaded via `idlc.exe -l json`
- **Format**: JSON files containing complete type definitions, topic descriptors, and QoS settings
- **Target Consumer**: C# code generators that produce zero-allocation serializers

### 1.2 Key Features

1. **Complete Type Metadata**: Structs, unions, enums, bitmasks, typedefs with full annotations
2. **Topic Descriptors**: Serialization opcodes, key definitions, and memory layout information
3. **QoS Settings**: Reliability, durability, history settings from IDL pragmas
4. **Layout Information**: Computed struct sizes, member offsets, and alignment requirements
5. **Resolved References**: Fully scoped type names with nested type dependencies

---

## 2. Architecture Overview

### 2.1 System Context

```
┌─────────────┐
│   IDL File  │
│  (Input)    │
└──────┬──────┘
       │
       v
┌─────────────────────────┐
│  idlc.exe -l json       │
│  (Cyclone DDS Compiler) │
└──────┬──────────────────┘
       │
       v
┌──────────────────────────┐
│  cycloneddsidljson.dll   │
│  (JSON Plugin)           │
│                          │
│  Components:             │
│  - Data Model Builder    │
│  - Layout Calculator     │
│  - Descriptor Extractor  │
│  - JSON Emitter          │
└──────┬───────────────────┘
       │
       v
┌─────────────┐
│ JSON Output │
│  (Types +   │
│ Descriptors)│
└─────────────┘
```

### 2.2 Plugin Architecture

The plugin is organized into four core modules:

1. **Data Model (`model.h/c`)**: In-memory representation of IDL types
2. **Type Extractor (`libidlc__types.c`)**: Populates data model from AST
3. **Layout Calculator (`model.c`)**: Computes C-ABI memory layouts
4. **Descriptor Extractor (`libidlc__descriptor.c`)**: Extracts serialization opcodes
5. **JSON Emitter (`model.c`)**: Serializes data model to JSON

---

## 3. Data Model Design

### 3.1 Core Structures

#### 3.1.1 Type Record (`dm_rec_t`)

```c
typedef struct dm_rec {
    // Identity
    char* name;              // Scoped IDL name (e.g., "MyModule::MyStruct")
    char* c_name;            // C-mangled name (e.g., "MyModule_MyStruct")
    char* kind;              // "struct", "union", "enum", "bitmask", "alias"
    char* type;              // Base type for members/aliases
    
    // Member Metadata
    int is_key;              // [DdsKey] - member is part of topic key
    int member_id;           // [DdsId(x)] - explicit member ID
    int has_explicit_id;     // True if @id annotation was present
    int is_optional;         // [DdsOptional] - member is optional
    int is_external;         // @external - use external allocation
    
    // Type Extensibility
    char* extensibility;     // "final", "appendable", "mutable"
    
    // Collection Properties
    int is_array;            // True for fixed-size arrays
    int size;                // Array size OR total struct size
    uint32_t bound;          // Bounds for strings/sequences OR enum bit-bound
    
    // Memory Layout (computed)
    int offset;              // Byte offset within containing struct
    int align;               // Alignment requirement in bytes
    
    // Value Metadata (for enums, default values)
    int has_value;
    enum dm_type value_type;
    union dm_value value;
    
    // Relationships
    struct dm_rec* members;  // Child members (struct fields, enum values)
    struct dm_rec* next;     // Next sibling
    
    // Union-specific
    char* discriminator;     // Type of union discriminator
    struct dm_rec* labels;   // Case labels
    
    // Topic-specific
    dm_descriptor_t* topic_descriptor;  // Serialization descriptor
    dm_qos_t* qos;                      // QoS settings
    
} dm_rec_t;
```

#### 3.1.2 Topic Descriptor (`dm_descriptor_t`)

```c
typedef struct dm_descriptor {
    // Type Properties
    uint32_t size;           // sizeof(struct) in bytes
    uint32_t align;          // alignof(struct) in bytes
    uint32_t flagset;        // DDS flags
    char* typename;          // Fully scoped type name
    
    // Key Definitions
    struct {
        char* name;          // Key field path (e.g., "ProcessAddr.StationId")
        uint32_t offset;     // Instruction offset in ops array
        uint32_t order;      // Key index (0, 1, 2, ...)
    } *keys;
    uint32_t n_keys;
    
    // Serialization Opcodes
    uint32_t* ops;           // Bytecode instruction array
    uint32_t n_ops;          // Number of instructions
    
} dm_descriptor_t;
```

#### 3.1.3 QoS Settings (`dm_qos_t`)

```c
typedef struct dm_qos {
    char* reliability;      // "reliable", "best_effort"
    char* durability;       // "volatile", "transient_local", "transient", "persistent"
    char* history;          // "keep_last", "keep_all"
    int32_t depth;          // History depth (e.g., 1, 10)
} dm_qos_t;
```

### 3.2 Global State

```c
extern dm_rec_t* dm_sources;     // Source file metadata
extern dm_rec_t* dm_types;       // All user-defined types
extern dm_rec_t* dm_last_struct; // Current struct being processed
extern dm_rec_t* dm_last_enum;   // Current enum being processed
```

---

## 4. Type Extraction Process

### 4.1 Visitor Pattern

The plugin uses the IDL AST visitor pattern to traverse type definitions:

```c
idl_retcode_t generate_types(const idl_pstate_t *pstate, struct generator *gen) {
    idl_visitor_t visitor;
    memset(&visitor, 0, sizeof(visitor));
    
    // Configure visitor callbacks
    visitor.visit = IDL_STRUCT | IDL_UNION | IDL_ENUM | 
                    IDL_BITMASK | IDL_TYPEDEF;
    visitor.accept[IDL_ACCEPT_STRUCT] = &emit_struct;
    visitor.accept[IDL_ACCEPT_UNION] = &emit_union;
    visitor.accept[IDL_ACCEPT_ENUM] = &emit_enum;
    visitor.accept[IDL_ACCEPT_BITMASK] = &emit_bitmask;
    visitor.accept[IDL_ACCEPT_TYPEDEF] = &emit_typedef;
    
    return idl_visit(pstate, pstate->root, &visitor, gen);
}
```

### 4.2 Struct Processing

#### Phase 1: Struct Header (First Visit)

```c
static idl_retcode_t emit_struct(
    const idl_pstate_t *pstate,
    bool revisit,
    const idl_path_t *path,
    const void *node,
    void *user_data)
{
    if (!revisit) {
        // Extract struct metadata
        char* scoped_name = get_scoped_name(node);
        char* c_name = get_c_name(node);
        
        dm_rec_t *rec = dm_new();
        rec->name = scoped_name;
        rec->c_name = c_name;
        rec->kind = idl_strdup("struct");
        
        // Extract extensibility annotation
        if (idl_is_extensible(node, IDL_MUTABLE))
            rec->extensibility = idl_strdup("mutable");
        else if (idl_is_extensible(node, IDL_APPENDABLE))
            rec->extensibility = idl_strdup("appendable");
        else
            rec->extensibility = idl_strdup("final");
        
        // Extract QoS if this is a topic
        if (idl_is_topic(node, keylist_only)) {
            rec->qos = extract_qos((const idl_struct_t*)node);
        }
        
        dm_add(&dm_types, rec);
        dm_last_struct = rec;
        
        // Continue C generation to /dev/null (for side effects)
        fprintf(gen->header.handle, "typedef struct %s {\n", c_name);
    }
    return IDL_RETCODE_OK;
}
```

#### Phase 2: Field Processing

```c
static idl_retcode_t emit_field(
    const idl_pstate_t *pstate,
    bool revisit,
    const idl_path_t *path,
    const void *node,
    void *user_data)
{
    if (!dm_last_struct) return IDL_RETCODE_OK;
    
    const char* name = idl_identifier(node);
    const idl_type_spec_t* type_spec = idl_type_spec(node);
    const void* parent_member = idl_parent(node);
    
    dm_rec_t *field = dm_new();
    field->name = idl_strdup(name);
    field->type = get_scoped_type_name(type_spec);
    
    // Extract member ID
    if (idl_is_declarator(node)) {
        field->member_id = ((const idl_declarator_t*)node)->id.value;
        field->has_explicit_id = 1;
    }
    
    // Extract @key annotation
    if (is_key_member(parent_member)) {
        field->is_key = 1;
    }
    
    // Extract @optional
    if (idl_is_optional(parent_member)) {
        field->is_optional = 1;
    }
    
    // Extract @external
    if (idl_is_external(parent_member)) {
        field->is_external = 1;
    }
    
    // Extract bounds (strings/sequences)
    if (idl_is_bounded(type_spec)) {
        field->bound = idl_bound(type_spec);
    }
    
    // Handle arrays
    if (idl_is_array(node)) {
        field->is_array = 1;
        const idl_literal_t *literal = 
            ((const idl_declarator_t *)node)->const_expr;
        
        int total_size = 1;
        for (; literal; literal = idl_next(literal)) {
            total_size *= literal->value.uint32;
        }
        field->size = total_size;
    }
    
    // Handle union cases
    if (idl_is_case(parent_member)) {
        extract_union_labels(parent_member, field);
    }
    
    // Extract @default annotation
    if (idl_is_member(parent_member)) {
        extract_default_value(parent_member, field);
    }
    
    dm_add(&dm_last_struct->members, field);
    
    // Continue C generation
    fprintf(gen->header.handle, "  %s %s;\n", c_type, name);
    
    return IDL_RETCODE_OK;
}
```

#### Phase 3: Struct Footer (Revisit)

```c
static idl_retcode_t emit_struct(
    const idl_pstate_t *pstate,
    bool revisit,
    const idl_path_t *path,
    const void *node,
    void *user_data)
{
    if (revisit) {
        // Calculate memory layout
        if (dm_last_struct) {
            dm_calculate_layout(dm_last_struct);
        }
        
        // Close C struct
        fprintf(gen->header.handle, "} %s;\n\n", c_name);
    }
    return IDL_RETCODE_OK;
}
```

### 4.3 Enum Processing

```c
static idl_retcode_t emit_enum(
    const idl_pstate_t *pstate,
    bool revisit,
    const idl_path_t *path,
    const void *node,
    void *user_data)
{
    char* scoped_name = get_scoped_name(node);
    char* c_name = get_c_name(node);
    
    dm_rec_t *enum_rec = dm_new();
    enum_rec->name = scoped_name;
    enum_rec->c_name = c_name;
    enum_rec->kind = idl_strdup("enum");
    
    // Extract bit bound
    const idl_enum_t *_enum = (const idl_enum_t *)node;
    enum_rec->bound = (_enum->bit_bound.value > 0) 
        ? _enum->bit_bound.value : 32;
    
    // Set size/align
    enum_rec->size = 4;  // Standard enum size
    enum_rec->align = 4;
    
    dm_add(&dm_types, enum_rec);
    dm_last_enum = enum_rec;
    
    // Process enumerators
    const idl_enumerator_t *enumerator = _enum->enumerators;
    for (; enumerator; enumerator = idl_next(enumerator)) {
        dm_rec_t *value = dm_new();
        value->name = idl_strdup(enumerator->name->identifier);
        value->has_value = 1;
        value->value_type = DM_TYPE_UNSIGNED_INT;
        value->value.uint64 = enumerator->value.value;
        
        dm_add(&enum_rec->members, value);
    }
    
    return IDL_VISIT_DONT_RECURSE;
}
```

### 4.4 Union Processing

Unions require special handling for discriminators and case labels:

```c
static idl_retcode_t emit_union(
    const idl_pstate_t *pstate,
    bool revisit,
    const idl_path_t *path,
    const void *node,
    void *user_data)
{
    if (!revisit) {
        const idl_union_t *union_node = (const idl_union_t *)node;
        const idl_switch_type_spec_t *switch_spec = 
            union_node->switch_type_spec;
        
        dm_rec_t *union_rec = dm_new();
        union_rec->name = get_scoped_name(node);
        union_rec->c_name = get_c_name(node);
        union_rec->kind = idl_strdup("union");
        union_rec->discriminator = 
            get_scoped_type_name(switch_spec->type_spec);
        
        // Extensibility
        if (idl_is_extensible(node, IDL_MUTABLE))
            union_rec->extensibility = idl_strdup("mutable");
        else if (idl_is_extensible(node, IDL_APPENDABLE))
            union_rec->extensibility = idl_strdup("appendable");
        else
            union_rec->extensibility = idl_strdup("final");
        
        dm_add(&dm_types, union_rec);
        dm_last_struct = union_rec;  // Reuse for member processing
    } else {
        // Calculate layout (unions overlay members)
        if (dm_last_struct) {
            dm_calculate_union_layout(dm_last_struct);
        }
    }
    return IDL_RETCODE_OK;
}
```

---

## 5. Memory Layout Calculation

### 5.1 C ABI Rules (x64)

The plugin implements standard C structure packing rules:

1. **Alignment**: Each field aligns to its natural boundary (min of size and 8)
2. **Padding**: Insert padding to satisfy alignment
3. **Struct Size**: Round up to largest member alignment

### 5.2 Primitive Type Sizes

```c
static size_t get_primitive_size_align(const char* type_name) {
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
    if (strcmp(type_name, "string") == 0) return 8;  // char* pointer
    if (strcmp(type_name, "wstring") == 0) return 8; // wchar_t* pointer
    return 0;  // Complex type
}
```

### 5.3 Layout Algorithm

```c
void dm_calculate_layout(dm_rec_t* struct_rec) {
    if (!struct_rec || !struct_rec->members) return;
    
    uint32_t cursor = 0;
    uint32_t max_align = 1;
    
    bool is_union = (struct_rec->kind && 
                     strcmp(struct_rec->kind, "union") == 0);
    
    for (dm_rec_t* member = struct_rec->members; 
         member != NULL; 
         member = member->next) {
        
        size_t member_size = 0;
        size_t member_align = 1;
        
        // 1. Determine base size/alignment
        size_t prim_size = get_primitive_size_align(member->type);
        
        if (prim_size > 0) {
            member_size = prim_size;
            member_align = prim_size;
        } else {
            // Lookup complex type
            dm_rec_t* nested = dm_find_by_name(dm_types, member->type);
            if (nested && nested->size > 0) {
                member_size = nested->size;
                member_align = nested->align;
            } else if (strstr(member->type, "sequence")) {
                // DDS sequence struct: {uint32 max, uint32 len, T* buf, bool rel}
                member_size = 24;  // 4+4+8+1+padding
                member_align = 8;
            } else {
                // Unknown type (could be enum or forward reference)
                member_size = 4;
                member_align = 4;
            }
        }
        
        // 2. Handle arrays (multiply size by array dimensions)
        if (member->is_array && member->size > 0) {
            member_size *= member->size;
            // Alignment remains element alignment
        }
        
        // 3. Apply alignment padding
        if (!is_union) {
            cursor = align_up(cursor, member_align);
        } else {
            cursor = 0;  // Unions overlay members
        }
        
        // 4. Record offset
        member->offset = cursor;
        
        // 5. Advance cursor
        if (!is_union) {
            cursor += member_size;
        } else {
            if (member_size > cursor) cursor = member_size;
        }
        
        // 6. Track maximum alignment
        if (member_align > max_align) {
            max_align = member_align;
        }
    }
    
    // 7. Final struct padding
    struct_rec->size = align_up(cursor, max_align);
    struct_rec->align = max_align;
}

static uint32_t align_up(uint32_t offset, size_t alignment) {
    if (alignment == 0) return offset;
    size_t remainder = offset % alignment;
    return (remainder == 0) ? offset : offset + (alignment - remainder);
}
```

### 5.4 Sequence Type Handling

DDS sequences are represented as structs:

```c
// C representation
struct dds_sequence_T {
    uint32_t _maximum;  // Offset 0, size 4
    uint32_t _length;   // Offset 4, size 4
    T* _buffer;         // Offset 8, size 8 (x64 pointer)
    bool _release;      // Offset 16, size 1
    // Padding: 7 bytes to align to 8
};
// Total size: 24 bytes, alignment: 8
```

---

## 6. Descriptor Extraction

### 6.1 Topic Descriptor Components

Each topic has a descriptor containing:

1. **Type Metadata**: Size, alignment, typename
2. **Keys**: Key field paths and instruction offsets
3. **Ops**: Serialization bytecode array

### 6.2 Opcode Extraction

The descriptor extractor converts internal instruction structures to uint32_t opcodes:

```c
idl_retcode_t dm_extract_descriptor_data(
    const struct descriptor *descriptor, 
    dm_descriptor_t **out_desc)
{
    dm_descriptor_t *dd = calloc(1, sizeof(dm_descriptor_t));
    
    // 1. Populate size/align from calculated layout
    char *topic_c_name = get_c_name(descriptor->topic);
    dm_rec_t* topic_type = dm_find_by_c_name(dm_types, topic_c_name);
    if (topic_type) {
        dd->size = topic_type->size;
        dd->align = topic_type->align;
    }
    dd->typename = get_scoped_name(descriptor->topic);
    dd->flagset = descriptor->flags;
    
    // 2. Extract keys
    dd->n_keys = descriptor->n_keys;
    if (dd->n_keys > 0) {
        dd->keys = calloc(dd->n_keys, sizeof(*dd->keys));
        
        uint32_t kof_offs = 0;
        for (struct constructed_type *ctype = descriptor->constructed_types;
             ctype; ctype = ctype->next) {
            kof_offs += ctype->instructions.count;
        }
        
        for (uint32_t k = 0; k < dd->n_keys; k++) {
            dd->keys[k].name = idl_strdup(descriptor->keys[k].name);
            dd->keys[k].offset = kof_offs + descriptor->keys[k].inst_offs;
            dd->keys[k].order = descriptor->keys[k].key_idx;
        }
    }
    
    // 3. Calculate total opcode count
    uint32_t total_ops = 0;
    for (struct constructed_type *ctype = descriptor->constructed_types;
         ctype; ctype = ctype->next) {
        total_ops += ctype->instructions.count;
    }
    total_ops += descriptor->key_offsets.count;
    total_ops += descriptor->member_ids.count;
    
    dd->ops = calloc(total_ops, sizeof(uint32_t));
    dd->n_ops = total_ops;
    
    // 4. Convert instructions to opcodes
    uint32_t idx = 0;
    
    for (struct constructed_type *ctype = descriptor->constructed_types;
         ctype; ctype = ctype->next) {
        
        for (size_t i = 0; i < ctype->instructions.count; i++) {
            struct instruction *inst = &ctype->instructions.table[i];
            dd->ops[idx++] = instruction_to_opcode(inst, topic_type);
        }
    }
    
    // Key offsets
    for (size_t i = 0; i < descriptor->key_offsets.count; i++) {
        struct instruction *inst = &descriptor->key_offsets.table[i];
        dd->ops[idx++] = instruction_to_opcode(inst, topic_type);
    }
    
    // Member IDs
    for (size_t i = 0; i < descriptor->member_ids.count; i++) {
        struct instruction *inst = &descriptor->member_ids.table[i];
        dd->ops[idx++] = instruction_to_opcode(inst, topic_type);
    }
    
    *out_desc = dd;
    return IDL_RETCODE_OK;
}
```

### 6.3 Instruction to Opcode Conversion

```c
static uint32_t instruction_to_opcode(
    struct instruction *inst, 
    dm_rec_t* context_type)
{
    uint32_t code = 0;
    
    switch(inst->type) {
        case OPCODE:
            code = inst->data.opcode.code;
            break;
        
        case SINGLE:
            code = inst->data.single;
            break;
        
        case COUPLE:
            // Handles "(3u << 16u) + 25u"
            code = (inst->data.couple.high << 16) | 
                   inst->data.couple.low;
            break;
        
        case OFFSET:
            // Handles "offsetof(Type, Member)"
            code = (uint32_t)dm_get_member_offset(
                inst->data.offset.type, 
                inst->data.offset.member);
            break;
        
        case MEMBER_SIZE:
            // Handles "sizeof(Type)"
            {
                dm_rec_t* target = dm_find_by_c_name(
                    dm_types, 
                    inst->data.size.type);
                code = target ? target->size : 0;
            }
            break;
        
        case ELEM_OFFSET:
            code = (inst->data.inst_offset.inst.high << 16) | 
                   (uint16_t)inst->data.inst_offset.elem_offs;
            break;
        
        case JEQ_OFFSET:
            code = (inst->data.inst_offset.inst.opcode & 
                    (DDS_OP_MASK | DDS_OP_TYPE_FLAGS_MASK | DDS_OP_TYPE_MASK)) |
                   (uint16_t)inst->data.inst_offset.elem_offs;
            break;
        
        case MEMBER_OFFSET:
            code = (inst->data.inst_offset.inst.opcode & 
                    (DDS_OP_MASK | DDS_PLM_FLAGS_MASK)) |
                   (uint16_t)inst->data.inst_offset.addr_offs;
            break;
        
        case BASE_MEMBERS_OFFSET:
            code = ((DDS_OP_PLM | (DDS_OP_FLAG_BASE << 16)) & 
                    (DDS_OP_MASK | DDS_PLM_FLAGS_MASK)) |
                   (uint16_t)inst->data.inst_offset.elem_offs;
            break;
        
        case KEY_OFFSET:
            code = (DDS_OP_KOF & DDS_OP_MASK) | 
                   (inst->data.key_offset.len & DDS_KOF_OFFSET_MASK);
            break;
        
        case KEY_OFFSET_VAL:
            code = inst->data.key_offset_val.offs;
            break;
        
        case MEMBER_ID:
            code = (DDS_OP_MID & DDS_OP_MASK) | 
                   (inst->data.member_id.addr_offs & DDS_KOF_OFFSET_MASK);
            break;
        
        default:
            code = 0;
            break;
    }
    
    return code;
}
```

### 6.4 Member Offset Lookup

```c
int dm_get_member_offset(const char* type_c_name, const char* member_name) {
    if (!type_c_name || !member_name) return 0;
    
    // Find type by C name
    dm_rec_t* type_rec = dm_find_by_c_name(dm_types, type_c_name);
    if (!type_rec) return 0;
    
    // Handle nested paths (e.g., "ProcessAddr.StationId")
    char* path_copy = idl_strdup(member_name);
    char* token = strtok(path_copy, ".");
    dm_rec_t* current_type = type_rec;
    int current_offset = 0;
    
    while (token != NULL) {
        // Find member in current type
        dm_rec_t* member = find_member_by_name(current_type, token);
        if (!member) {
            free(path_copy);
            return 0;
        }
        
        current_offset += member->offset;
        
        // If there's a next token, navigate to member's type
        token = strtok(NULL, ".");
        if (token) {
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

---

## 7. QoS Extraction

### 7.1 IDL Pragma Mapping

The plugin extracts QoS settings from custom pragmas:

```idl
struct SpikeLauncher {
    unsigned long Id;
    // ... fields ...
};

#pragma keylist SpikeLauncher Id ProcessAddr.StationId ProcessAddr.ProcessId
#pragma topic reliable transient_local keep_last 1
```

### 7.2 QoS Extractor

```c
static dm_qos_t* extract_qos(const idl_struct_t* struct_node) {
    dm_qos_t* qos = calloc(1, sizeof(dm_qos_t));
    if (!qos) return NULL;
    
    // Reliability
    switch (struct_node->qos.reliability) {
        case IDL_RELIABILITY_BEST_EFFORT:
            qos->reliability = idl_strdup("best_effort");
            break;
        case IDL_RELIABILITY_RELIABLE:
            qos->reliability = idl_strdup("reliable");
            break;
        default:
            qos->reliability = NULL;
            break;
    }
    
    // Durability
    switch (struct_node->qos.durability) {
        case IDL_DURABILITY_VOLATILE:
            qos->durability = idl_strdup("volatile");
            break;
        case IDL_DURABILITY_TRANSIENT_LOCAL:
            qos->durability = idl_strdup("transient_local");
            break;
        case IDL_DURABILITY_TRANSIENT:
            qos->durability = idl_strdup("transient");
            break;
        case IDL_DURABILITY_PERSISTENT:
            qos->durability = idl_strdup("persistent");
            break;
        default:
            qos->durability = NULL;
            break;
    }
    
    // History
    switch (struct_node->qos.history) {
        case IDL_HISTORY_KEEP_LAST:
            qos->history = idl_strdup("keep_last");
            break;
        case IDL_HISTORY_KEEP_ALL:
            qos->history = idl_strdup("keep_all");
            break;
        default:
            qos->history = NULL;
            break;
    }
    
    // Depth
    qos->depth = (int32_t)struct_node->qos.depth;
    
    return qos;
}
```

---

## 8. JSON Output Format

### 8.1 Overall Structure

```json
{
  "File": [
    {
      "Name": "SpikeLauncher.idl",
      "Members": [
        { "Name": "Location.idl" },
        { "Name": "Orientation.idl" }
      ],
      "_eof": 0
    }
  ],
  "Types": [
    { /* Type definition 1 */ },
    { /* Type definition 2 */ },
    ...
  ]
}
```

### 8.2 Struct Type Example

```json
{
  "Name": "Bagira::DDS::DM::Spike::SpikeLauncher",
  "Kind": "struct",
  "Extensibility": "appendable",
  "QoS": {
    "Reliability": "reliable",
    "Durability": "transient_local",
    "History": "keep_last",
    "HistoryDepth": 1
  },
  "TopicDescriptor": {
    "Size": 128,
    "Align": 8,
    "FlagSet": 0,
    "TypeName": "Bagira::DDS::DM::Spike::SpikeLauncher",
    "Keys": [
      { "Name": "Id", "Offset": 50, "Order": 0 },
      { "Name": "ProcessAddr.StationId", "Offset": 52, "Order": 1 },
      { "Name": "ProcessAddr.ProcessId", "Offset": 55, "Order": 2 }
    ],
    "Ops": [
      251658244, 2, 196611, 25, 196611, 30, 4, 6, 
      // ... (full opcode array)
    ]
  },
  "Members": [
    {
      "Name": "Id",
      "Type": "unsigned long",
      "Id": 0,
      "IsKey": true,
      "_eof": 0
    },
    {
      "Name": "Position",
      "Type": "Bagira::DDS::DM::Location_Struct",
      "Id": 1,
      "_eof": 0
    },
    {
      "Name": "Rotation",
      "Type": "Bagira::DDS::DM::Orientation_Struct",
      "Id": 2,
      "_eof": 0
    },
    {
      "Name": "Type",
      "Type": "Bagira::DDS::DM::eLauncherType",
      "Id": 8,
      "_eof": 0
    }
  ],
  "_eof": 0
}
```

### 8.3 Enum Type Example

```json
{
  "Name": "Bagira::DDS::DM::eLauncherType",
  "Kind": "enum",
  "Bound": 32,
  "Members": [
    { "Name": "SPIKE_LR", "Value": 0, "_eof": 0 },
    { "Name": "SPIKE_MR", "Value": 1, "_eof": 0 },
    { "Name": "SPIKE_ER", "Value": 2, "_eof": 0 },
    { "Name": "SPIKE_NLOS", "Value": 3, "_eof": 0 }
  ],
  "_eof": 0
}
```

### 8.4 Union Type Example

```json
{
  "Name": "MyModule::MyUnion",
  "Kind": "union",
  "Discriminator": "long",
  "Extensibility": "appendable",
  "Members": [
    {
      "Name": "stringValue",
      "Type": "string",
      "Labels": ["1", "2"],
      "_eof": 0
    },
    {
      "Name": "intValue",
      "Type": "long",
      "Labels": ["3"],
      "_eof": 0
    },
    {
      "Name": "defaultValue",
      "Type": "boolean",
      "Labels": ["default"],
      "_eof": 0
    }
  ],
  "_eof": 0
}
```

### 8.5 Typedef Example

```json
{
  "Name": "MyModule::MySequence",
  "Kind": "alias",
  "Type": "MyModule::MyStruct",
  "CollectionType": "sequence",
  "Bound": 100,
  "_eof": 0
}
```

### 8.6 Array Example

```json
{
  "Name": "matrix",
  "Type": "double",
  "CollectionType": "array",
  "Size": 16,
  "_eof": 0
}
```

---

## 9. JSON Emitter Implementation

### 9.1 High-Level Printer

```c
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

### 9.2 Record Printer

```c
static void dm_print_rec(FILE* fh, dm_rec_t* rec, int indent) {
    if (!rec) return;
    
    dm_indent(fh, indent);
    fprintf(fh, "{\n");
    
    // Basic identity
    if (rec->name) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"Name\": \"%s\",\n", dm_escapize(rec->name));
    }
    
    if (rec->kind) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"Kind\": \"%s\",\n", dm_escapize(rec->kind));
    }
    
    if (rec->type) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"Type\": \"%s\",\n", dm_escapize(rec->type));
    }
    
    // Annotations
    if (rec->extensibility) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"Extensibility\": \"%s\",\n", rec->extensibility);
    }
    
    if (rec->is_key) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"IsKey\": true,\n");
    }
    
    if (rec->has_explicit_id) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"Id\": %d,\n", rec->member_id);
    }
    
    if (rec->is_optional) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"IsOptional\": true,\n");
    }
    
    if (rec->is_external) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"IsExternal\": true,\n");
    }
    
    // Bounds and collections
    if (rec->bound > 0) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"Bound\": %u,\n", rec->bound);
    }
    
    if (rec->is_array) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"CollectionType\": \"array\",\n");
        if (rec->size > 0) {
            dm_indent(fh, indent+1);
            fprintf(fh, "\"Size\": %d,\n", rec->size);
        }
    } else if (rec->kind && strcmp(rec->kind, "sequence") == 0) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"CollectionType\": \"sequence\",\n");
    }
    
    // Union discriminator and labels
    if (rec->discriminator) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"Discriminator\": \"%s\",\n", 
                dm_escapize(rec->discriminator));
    }
    
    if (rec->labels) {
        dm_print_labels(fh, rec->labels, indent+1);
    }
    
    // Default values
    if (rec->has_value) {
        dm_print_value(fh, rec, indent+1);
    }
    
    // QoS
    if (rec->qos) {
        dm_print_qos(fh, rec->qos, indent+1);
    }
    
    // Topic Descriptor
    if (rec->topic_descriptor) {
        dm_print_descriptor(fh, rec->topic_descriptor, indent+1);
    }
    
    // Members
    if (rec->members) {
        dm_indent(fh, indent+1);
        fprintf(fh, "\"Members\":\n");
        dm_print_list(fh, rec->members, indent+1);
        fprintf(fh, ",\n");
    }
    
    // EOF marker (prevents trailing comma issues)
    dm_indent(fh, indent+1);
    fprintf(fh, "\"_eof\": 0\n");
    
    dm_indent(fh, indent);
    fprintf(fh, "}");
}
```

### 9.3 Descriptor Printer

```c
static void dm_print_descriptor(FILE* fh, dm_descriptor_t* desc, int indent) {
    if (!desc) return;
    
    dm_indent(fh, indent);
    fprintf(fh, "\"TopicDescriptor\": {\n");
    
    dm_indent(fh, indent+1);
    fprintf(fh, "\"Size\": %u,\n", desc->size);
    
    dm_indent(fh, indent+1);
    fprintf(fh, "\"Align\": %u,\n", desc->align);
    
    dm_indent(fh, indent+1);
    fprintf(fh, "\"FlagSet\": %u,\n", desc->flagset);
    
    dm_indent(fh, indent+1);
    fprintf(fh, "\"TypeName\": \"%s\",\n", 
            desc->typename ? desc->typename : "");
    
    // Keys
    dm_indent(fh, indent+1);
    fprintf(fh, "\"Keys\": [\n");
    for (uint32_t i = 0; i < desc->n_keys; i++) {
        dm_indent(fh, indent+2);
        fprintf(fh, "{ \"Name\": \"%s\", \"Offset\": %u, \"Order\": %u }",
                desc->keys[i].name, 
                desc->keys[i].offset, 
                desc->keys[i].order);
        if (i < desc->n_keys - 1) fprintf(fh, ",");
        fprintf(fh, "\n");
    }
    dm_indent(fh, indent+1);
    fprintf(fh, "],\n");
    
    // Ops
    dm_indent(fh, indent+1);
    fprintf(fh, "\"Ops\": [\n");
    for (uint32_t i = 0; i < desc->n_ops; i++) {
        if (i % 8 == 0) dm_indent(fh, indent+2);
        fprintf(fh, "%u", desc->ops[i]);
        if (i < desc->n_ops - 1) fprintf(fh, ", ");
        if ((i+1) % 8 == 0 || i == desc->n_ops - 1) fprintf(fh, "\n");
    }
    dm_indent(fh, indent+1);
    fprintf(fh, "]\n");
    
    dm_indent(fh, indent);
    fprintf(fh, "},\n");
}
```

---

## 10. Build System Integration

### 10.1 CMakeLists.txt Modifications

```cmake
# Set output library name
set_target_properties(libidlc PROPERTIES
   OUTPUT_NAME "cycloneddsidljson"  # Changed from "cycloneddsidlc"
   VERSION ${PROJECT_VERSION}
   SOVERSION ${PROJECT_VERSION_MAJOR}
   C_STANDARD 99)

# Add new source files
set(
  libidlc_srcs
  src/libidlc/libidlc__types.h
  src/libidlc/libidlc__descriptor.h
  src/libidlc/libidlc__generator.h
  src/libidlc/libidlc__descriptor.c
  src/libidlc/libidlc__generator.c
  src/libidlc/libidlc__types.c
  src/libidlc/model.h           # NEW
  src/libidlc/model.c)          # NEW
```

### 10.2 Generator Entry Point

```c
// src/libidlc/libidlc__generator.c

idl_retcode_t generate(
    const idl_pstate_t *pstate, 
    const idlc_generator_config_t *config)
{
    struct generator gen;
    memset(&gen, 0, sizeof(gen));
    
    // Open C output to /dev/null (Windows: nul)
#ifdef _WIN32
    gen.header.handle = fopen("nul", "wb");
    gen.source.handle = fopen("nul", "wb");
#else
    gen.header.handle = fopen("/dev/null", "wb");
    gen.source.handle = fopen("/dev/null", "wb");
#endif
    
    gen.header.path = idl_strdup("dummy.h");
    gen.source.path = idl_strdup("dummy.c");
    
    // Open JSON output
    const char* input_path = pstate->sources->path->name;
    if (idl_asprintf(&gen.json.path, "%s.json", input_path) < 0)
        goto err;
    
    gen.json.handle = fopen(gen.json.path, "wb");
    if (!gen.json.handle) goto err;
    
    // Initialize data model
    dm_rec_t *source_rec = dm_new();
    source_rec->name = idl_strdup(input_path);
    dm_add(&dm_sources, source_rec);
    
    // Process includes
    for (idl_source_t* include = pstate->sources->includes;
         include; include = include->next) {
        dm_rec_t *inc = dm_new();
        inc->name = idl_strdup(include->path->name);
        dm_add(&source_rec->members, inc);
    }
    
    // Generate types (populates dm_types)
    idl_retcode_t ret = generate_types(pstate, &gen);
    if (ret != IDL_RETCODE_OK) goto err;
    
    // Emit JSON
    dm_fprint(gen.json.handle);
    
    // Cleanup
    fclose(gen.header.handle);
    fclose(gen.source.handle);
    fclose(gen.json.handle);
    free(gen.header.path);
    free(gen.source.path);
    free(gen.json.path);
    
    return IDL_RETCODE_OK;
    
err:
    // Cleanup on error
    if (gen.header.handle) fclose(gen.header.handle);
    if (gen.source.handle) fclose(gen.source.handle);
    if (gen.json.handle) fclose(gen.json.handle);
    if (gen.header.path) free(gen.header.path);
    if (gen.source.path) free(gen.source.path);
    if (gen.json.path) free(gen.json.path);
    return IDL_RETCODE_NO_MEMORY;
}
```

---

## 11. Usage

### 11.1 Command Line

```bash
# Generate JSON from IDL
idlc.exe -l json SpikeLauncher.idl

# Output: SpikeLauncher.idl.json
```

### 11.2 Integration with C# Code Generator

```csharp
// C# JSON consumer
var json = File.ReadAllText("SpikeLauncher.idl.json");
var model = JsonSerializer.Deserialize<IdlModel>(json);

foreach (var type in model.Types)
{
    if (type.Kind == "struct")
    {
        GenerateStruct(type);
        
        if (type.TopicDescriptor != null)
        {
            GenerateTopic(type, type.TopicDescriptor);
        }
    }
}
```

---

## 12. File Organization

### 12.1 Source Tree

```
cyclonedds/src/tools/idljson/
├── CMakeLists.txt
├── include/
│   ├── libidlc/
│   │   └── libidlc_generator.h
│   └── idlc/
│       └── generator.h
├── src/
│   ├── libidlc/
│   │   ├── model.h                    (NEW - Data model definitions)
│   │   ├── model.c                    (NEW - JSON emitter, layout calc)
│   │   ├── libidlc__types.h
│   │   ├── libidlc__types.c           (MODIFIED - Extract to data model)
│   │   ├── libidlc__descriptor.h
│   │   ├── libidlc__descriptor.c      (MODIFIED - Extract opcodes)
│   │   ├── libidlc__generator.h
│   │   └── libidlc__generator.c       (MODIFIED - JSON output setup)
│   └── idlc/
│       ├── idlc.c
│       ├── generator.c
│       └── options.c
├── tests/
└── xtests/
```

### 12.2 Key Files

| File | Purpose | Changes |
|------|---------|---------|
| `model.h` | NEW - Data model structures | Define dm_rec_t, dm_descriptor_t, dm_qos_t |
| `model.c` | NEW - JSON output & layout | Implement dm_fprint(), dm_calculate_layout() |
| `libidlc__types.c` | MODIFIED - Type extraction | Add JSON extraction to emit_struct(), emit_field(), etc. |
| `libidlc__descriptor.c` | MODIFIED - Opcode extraction | Add dm_extract_descriptor_data() |
| `libidlc__generator.c` | MODIFIED - Main generator | Redirect C output to /dev/null, JSON to .json file |
| `CMakeLists.txt` | MODIFIED - Build config | Change OUTPUT_NAME to cycloneddsidljson |

---

## 13. Implementation Strategy

### 13.1 Phase 1: Core Data Model (Week 1)

**Tasks:**
1. Create `model.h` with complete structure definitions
2. Implement `model.c` basic functions (dm_new, dm_add, dm_find_by_name)
3. Add global state variables (dm_sources, dm_types, dm_last_struct, dm_last_enum)
4. Implement JSON printer skeleton (dm_fprint, dm_print_rec, dm_print_list)

**Deliverable:** Compiles and can print empty JSON structure

### 13.2 Phase 2: Type Extraction (Week 2)

**Tasks:**
1. Modify `emit_struct()` to populate dm_rec_t
2. Modify `emit_field()` to extract member metadata
3. Implement `emit_enum()` with enumerator extraction
4. Implement `emit_union()` with discriminator/labels
5. Implement `emit_typedef()` and `emit_bitmask()`
6. Add extensibility annotation extraction

**Deliverable:** JSON contains all type definitions with annotations

### 13.3 Phase 3: Layout Calculation (Week 3)

**Tasks:**
1. Implement `get_primitive_size_align()`
2. Implement `dm_calculate_layout()` with padding logic
3. Add `dm_calculate_union_layout()` for unions
4. Implement `dm_find_by_name()` for type lookups
5. Call layout calculation incrementally in `emit_struct` revisit

**Deliverable:** JSON includes computed Size and Align for all structs

### 13.4 Phase 4: Descriptor Extraction (Week 4)

**Tasks:**
1. Implement `dm_extract_descriptor_data()`
2. Implement `instruction_to_opcode()` switch statement
3. Implement `dm_get_member_offset()` with nested path support
4. Extract keys array
5. Extract ops array
6. Integrate with `generate_descriptor()` call

**Deliverable:** JSON includes complete TopicDescriptor with opcodes

### 13.5 Phase 5: QoS Extraction (Week 5)

**Tasks:**
1. Implement `extract_qos()` function
2. Check `idl_is_topic()` in emit_struct
3. Map QoS enums to string values
4. Add QoS printer to JSON emitter

**Deliverable:** JSON includes QoS settings for topic structs

### 13.6 Phase 6: Integration & Testing (Week 6)

**Tasks:**
1. Modify CMakeLists.txt to set OUTPUT_NAME
2. Modify `generate()` to redirect C output to /dev/null
3. Open JSON file handle in `generate()`
4. Test with sample IDL files
5. Validate JSON output structure
6. Test integration with C# importer

**Deliverable:** Fully functional cycloneddsidljson.dll

---

## 14. Testing Strategy

### 14.1 Unit Tests

**Test Files:**
- `tests/model_test.c` - Test data model operations
- `tests/layout_test.c` - Test layout calculation
- `tests/json_test.c` - Test JSON output format

**Coverage:**
- Primitive types
- Nested structs
- Arrays (single and multi-dimensional)
- Sequences (bounded and unbounded)
- Strings (fixed and unbounded)
- Enums and bitmasks
- Unions with discriminators
- Optional fields
- Key fields

### 14.2 Integration Tests

**Test IDL Files:**
1. `simple_struct.idl` - Basic struct with primitives
2. `nested_struct.idl` - Nested struct references
3. `keyed_topic.idl` - Struct with @key annotations
4. `union_test.idl` - Union with discriminator
5. `enum_test.idl` - Enum with explicit values
6. `complex_topic.idl` - Full topic with QoS pragmas

**Validation:**
- JSON schema validation
- Round-trip C# code generation
- Serialization correctness
- Memory layout accuracy

### 14.3 Regression Tests

**Baseline:**
- Compare generated C code (to /dev/null) behavior
- Ensure descriptor opcodes match original idlc
- Verify calculated offsets match runtime offsetof()

---

## 15. Performance Considerations

### 15.1 Memory Management

- **Allocated Structures**: ~10-100 KB per IDL file
- **JSON Output Size**: ~2-10x source IDL size
- **Memory Leaks**: Use valgrind to verify cleanup

### 15.2 Time Complexity

- **Layout Calculation**: O(n * m) where n=types, m=avg members
- **Opcode Extraction**: O(k) where k=instruction count
- **JSON Serialization**: O(n * m)

**Expected Performance:**
- Typical IDL: < 100ms
- Large IDL (1000+ types): < 1 second

---

## 16. Error Handling

### 16.1 Invalid IDL

**Handled by idlc parser:**
- Syntax errors
- Undefined type references
- Invalid annotations

### 16.2 Plugin-Specific Errors

**Custom validation:**
- Warn if struct size exceeds 64KB
- Warn if alignment is non-power-of-2
- Error if opcode extraction fails
- Error if JSON file cannot be opened

**Error Reporting:**
```c
if (!gen.json.handle) {
    idl_error(pstate, node, 
        "Failed to open JSON output file: %s", gen.json.path);
    return IDL_RETCODE_NO_MEMORY;
}
```

---

## 17. Future Enhancements

### 17.1 Planned Features

1. **XML Output Option**: Support `idlc -l xml` for XML format
2. **Compact JSON**: Minified output option for production
3. **Schema Evolution**: Version tracking for type changes
4. **Cross-References**: Include dependency graph in JSON
5. **Platform Variants**: x86/ARM layout variants

### 17.2 Optimization Opportunities

1. **Incremental Generation**: Only regenerate changed types
2. **Parallel Processing**: Multi-threaded layout calculation
3. **Caching**: Reuse layout calculations across files
4. **Compression**: GZIP JSON output option

---

## 18. Appendix

### 18.1 JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "File": {
      "type": "array",
      "items": { "$ref": "#/definitions/SourceFile" }
    },
    "Types": {
      "type": "array",
      "items": { "$ref": "#/definitions/TypeDef" }
    }
  },
  "definitions": {
    "TypeDef": {
      "type": "object",
      "required": ["Name", "Kind"],
      "properties": {
        "Name": { "type": "string" },
        "Kind": { "enum": ["struct", "union", "enum", "bitmask", "alias"] },
        "Extensibility": { "enum": ["final", "appendable", "mutable"] },
        "Discriminator": { "type": "string" },
        "Bound": { "type": "integer" },
        "Members": {
          "type": "array",
          "items": { "$ref": "#/definitions/Member" }
        },
        "TopicDescriptor": { "$ref": "#/definitions/Descriptor" },
        "QoS": { "$ref": "#/definitions/QoS" }
      }
    }
  }
}
```

### 18.2 Example Output

See Section 8 (JSON Output Format) for complete examples.

### 18.3 Glossary

- **AST**: Abstract Syntax Tree - internal representation of parsed IDL
- **CDR**: Common Data Representation - DDS serialization format
- **XCDR2**: Extended CDR version 2 - supports extensibility
- **Opcode**: Serialization instruction in topic descriptor
- **Layout**: Memory organization of struct members
- **Extensibility**: Type versioning mode (final/appendable/mutable)

---

**End of Design Document**
