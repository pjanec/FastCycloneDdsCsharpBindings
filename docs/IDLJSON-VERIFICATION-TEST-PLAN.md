# IDL JSON Plugin Verification Test Plan

This document outlines the instructions to implement a robust verification testbed for the IDL JSON plugin. The goal is to mathematically prove that the JSON output (offsets, sizes, opcodes) matches exactly what the C compiler produces for the same IDL.

## 📂 Directory Structure

Create the following directory structure:

```
D:\Work\FastCycloneDdsCsharpBindings\tests\IdlJson.Tests\
├── CMakeLists.txt
├── verification.idl
├── verifier.c
└── cJSON/
    ├── cJSON.h
    └── cJSON.c
```

**Prerequisite:** Download `cJSON.h` and `cJSON.c` from [DaveGamble/cJSON](https://github.com/DaveGamble/cJSON) and place them in the `cJSON` subdirectory.

---

## 1. The Test IDL (`verification.idl`)

Create `D:\Work\FastCycloneDdsCsharpBindings\tests\IdlJson.Tests\verification.idl`.
This IDL covers complex nesting, inheritance, unions, and padding scenarios. THIS IS JUST AN EXAMPLE, YOU NEED TO CREATE YOUR OWN IDL TO TEST YOUR OWN, MUCH MORE COMPLEX SCENARIOS.

We need many possible combination of complex topic data struct includion unions, sequences of stucts and unions, topics with nested keys etc.


```idl
module TestModule {
    enum Color { RED, GREEN, BLUE };

    struct Point {
        double x;
        double y;
    };

    union Container switch(long) {
        case 1: long id;
        case 2: Point pt;
    };

    struct NestedStruct {
        char a;
        long b; // Should trigger padding
    };

    @topic
    struct ComplexType {
        @key long id;           // 4 bytes
        boolean flag;           // 1 byte (padding +3)
        Point pt;               // 16 bytes (aligned 8)
        Color col;              // 4 bytes
        long data[3];           // 12 bytes
        NestedStruct nested;    // Struct inside struct
        Container payload;      // Union
        sequence<long> seq;     // Sequence
        string msg;             // String
    };
};
```

---

## 2. The Verification App (`verifier.c`)

Create `D:\Work\FastCycloneDdsCsharpBindings\tests\IdlJson.Tests\verifier.c`.
This C program compiles the generated C header and compares it against the JSON.

```c
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stddef.h>
#include <inttypes.h>

// Include the IDLC generated header (C Backend)
#include "verification.h"

// Include cJSON
#include "cJSON/cJSON.h"

#define ASSERT_EQ(name, actual, expected) \
    do { \
        if ((long long)(actual) != (long long)(expected)) { \
            fprintf(stderr, "[FAIL] %s: C-Compiler %lld != JSON %lld\n", \
                    name, (long long)(actual), (long long)(expected)); \
            errors++; \
        } else { \
            printf("[PASS] %s: %lld\n", name, (long long)(actual)); \
        } \
    } while(0)

// Helper to find a type definition in the JSON array
cJSON* find_type(cJSON* root, const char* name) {
    cJSON* types = cJSON_GetObjectItem(root, "Types");
    cJSON* item = NULL;
    cJSON_ArrayForEach(item, types) {
        cJSON* n = cJSON_GetObjectItem(item, "Name");
        if (n && strcmp(n->valuestring, name) == 0) return item;
    }
    return NULL;
}

// Helper to find a member in a Type object
cJSON* find_member(cJSON* typeNode, const char* memberName) {
    cJSON* members = cJSON_GetObjectItem(typeNode, "Members");
    cJSON* item = NULL;
    cJSON_ArrayForEach(item, members) {
        cJSON* n = cJSON_GetObjectItem(item, "Name");
        if (n && strcmp(n->valuestring, memberName) == 0) return item;
    }
    return NULL;
}

int main(int argc, char** argv) {
    if (argc < 2) {
        fprintf(stderr, "Usage: %s <path_to_json_file>\n", argv[0]);
        return 1;
    }

    // --- Load JSON ---
    FILE* f = fopen(argv[1], "rb");
    if (!f) { perror("File open"); return 1; }
    fseek(f, 0, SEEK_END);
    long len = ftell(f);
    fseek(f, 0, SEEK_SET);
    char* data = (char*)malloc(len + 1);
    fread(data, 1, len, f);
    data[len] = '\0';
    fclose(f);

    cJSON* json = cJSON_Parse(data);
    if (!json) { fprintf(stderr, "JSON Parse Error\n"); free(data); return 1; }

    int errors = 0;

    printf("==================================================\n");
    printf("VERIFYING LAYOUT AGAINST C COMPILER ABI\n");
    printf("==================================================\n");

    // ---------------------------------------------------------
    // 1. Verify TestModule::ComplexType
    // ---------------------------------------------------------
    printf("\n--- Checking Struct: TestModule::ComplexType ---\n");
    cJSON* jNode = find_type(json, "TestModule::ComplexType");
    if (!jNode) { fprintf(stderr, "FATAL: Type missing in JSON\n"); return 1; }

    // Size
    ASSERT_EQ("sizeof(ComplexType)", sizeof(TestModule_ComplexType), 
              cJSON_GetObjectItem(jNode, "Size")->valueint);

    // Alignment
    // Note: C doesn't have standard alignof in C99, but we can infer or skip if needed.
    // _Alignof is C11.
    #if __STDC_VERSION__ >= 201112L || defined(_MSC_VER)
       ASSERT_EQ("alignof(ComplexType)", _Alignof(TestModule_ComplexType), 
                 cJSON_GetObjectItem(jNode, "Align")->valueint);
    #endif

    // Offsets
    cJSON* m;
    
    m = find_member(jNode, "id");
    ASSERT_EQ("offset(id)", offsetof(TestModule_ComplexType, id), 
              cJSON_GetObjectItem(m, "Offset")->valueint);

    m = find_member(jNode, "flag");
    ASSERT_EQ("offset(flag)", offsetof(TestModule_ComplexType, flag), 
              cJSON_GetObjectItem(m, "Offset")->valueint);

    m = find_member(jNode, "pt");
    ASSERT_EQ("offset(pt)", offsetof(TestModule_ComplexType, pt), 
              cJSON_GetObjectItem(m, "Offset")->valueint);

    m = find_member(jNode, "nested");
    ASSERT_EQ("offset(nested)", offsetof(TestModule_ComplexType, nested), 
              cJSON_GetObjectItem(m, "Offset")->valueint);

    m = find_member(jNode, "payload");
    ASSERT_EQ("offset(payload)", offsetof(TestModule_ComplexType, payload), 
              cJSON_GetObjectItem(m, "Offset")->valueint);

    // ---------------------------------------------------------
    // 2. Verify Union: TestModule::Container
    // ---------------------------------------------------------
    printf("\n--- Checking Union: TestModule::Container ---\n");
    cJSON* jUnion = find_type(json, "TestModule::Container");
    
    ASSERT_EQ("sizeof(Container)", sizeof(TestModule_Container), 
              cJSON_GetObjectItem(jUnion, "Size")->valueint);

    // ---------------------------------------------------------
    // 3. Verify Descriptor Opcodes
    // ---------------------------------------------------------
    printf("\n--- Checking Topic Descriptor Opcodes ---\n");
    
    cJSON* jDesc = cJSON_GetObjectItem(jNode, "TopicDescriptor");
    if (jDesc) {
        cJSON* jOps = cJSON_GetObjectItem(jDesc, "Ops");
        
        // The C descriptor is global: TestModule_ComplexType_desc
        const uint32_t* cOps = TestModule_ComplexType_desc.m_ops;
        uint32_t cNops = TestModule_ComplexType_desc.m_nops;

        int jNops = cJSON_GetArraySize(jOps);
        
        ASSERT_EQ("Ops Count", cNops, jNops);

        for (int i = 0; i < jNops && i < (int)cNops; i++) {
            uint32_t jsonOp = (uint32_t)cJSON_GetArrayItem(jOps, i)->valueint;
            uint32_t cOp = cOps[i];
            
            if (jsonOp != cOp) {
                fprintf(stderr, "[FAIL] Opcode[%d]: C 0x%08X != JSON 0x%08X\n", i, cOp, jsonOp);
                errors++;
            }
        }
        if (errors == 0) printf("[PASS] All Opcodes match.\n");
    } else {
        fprintf(stderr, "[FAIL] TopicDescriptor missing in JSON\n");
        errors++;
    }

    printf("\n==================================================\n");
    if (errors == 0) printf("RESULT: PASSED\n");
    else printf("RESULT: FAILED (%d errors)\n", errors);
    printf("==================================================\n");

    free(data);
    cJSON_Delete(json);
    return errors;
}
```

---

## 3. Build Script (`CMakeLists.txt`)

Create `D:\Work\FastCycloneDdsCsharpBindings\tests\IdlJson.Tests\CMakeLists.txt`.

**Important:** Update the `CYCLONE_INSTALL_DIR` path if it differs on your machine.

```cmake
cmake_minimum_required(VERSION 3.16)
project(IdlJsonVerifier C)

# --- Configuration ---
set(CYCLONE_INSTALL_DIR "D:/Work/FastCycloneDdsCsharpBindings/cyclone-compiled")
set(IDLC_EXE "${CYCLONE_INSTALL_DIR}/bin/idlc.exe")

# --- Find CycloneDDS ---
# We need headers and libs to compile the generated C code
include_directories("${CYCLONE_INSTALL_DIR}/include")
link_directories("${CYCLONE_INSTALL_DIR}/lib")

# --- Generation Step ---
set(IDL_FILE "${CMAKE_CURRENT_SOURCE_DIR}/verification.idl")
set(GEN_C    "${CMAKE_CURRENT_BINARY_DIR}/verification.c")
set(GEN_H    "${CMAKE_CURRENT_BINARY_DIR}/verification.h")
set(GEN_JSON "${CMAKE_CURRENT_BINARY_DIR}/verification.idl.json")

# 1. Generate C Code (Reference)
add_custom_command(
    OUTPUT ${GEN_C} ${GEN_H}
    COMMAND ${IDLC_EXE} -l c ${IDL_FILE}
    WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
    DEPENDS ${IDL_FILE}
    COMMENT "Generating C code from IDL..."
)

# 2. Generate JSON (Target)
# Note: We assume 'cycloneddsidljson.dll' is in the same bin folder as idlc.exe
# If not, you might need to copy it there or set PATH.
add_custom_command(
    OUTPUT ${GEN_JSON}
    COMMAND ${IDLC_EXE} -l json ${IDL_FILE}
    WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
    DEPENDS ${IDL_FILE}
    COMMENT "Generating JSON from IDL..."
)

# --- Build Verifier ---
add_executable(verify_layout 
    verifier.c 
    cJSON/cJSON.c
    ${GEN_C} 
    ${GEN_H}
    ${GEN_JSON} # Add to target so it runs generation
)

# Link against CycloneDDS C library (ddsc)
target_link_libraries(verify_layout ddsc)

# Copy JSON to output dir for easy running
add_custom_command(TARGET verify_layout POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy
        ${GEN_JSON}
        $<TARGET_FILE_DIR:verify_layout>/verification.idl.json
)
```

---

## 4. How to Run

1.  **Open Terminal** (PowerShell or cmd).
2.  Navigate to the test folder:
    ```powershell
    cd D:\Work\FastCycloneDdsCsharpBindings\tests\IdlJson.Tests
    ```
3.  **Create Build Directory**:
    ```powershell
    mkdir build
    cd build
    ```
4.  **Configure & Build**:
    ```powershell
    cmake ..
    cmake --build . --config Release
    ```
5.  **Run Verification**:
    ```powershell
    .\Release\verify_layout.exe verification.idl.json
    ```

### Expected Output

You should see a series of `[PASS]` messages confirming that the JSON values (calculated by your plugin) match exactly the `sizeof` and `offsetof` values reported by the C compiler for the generated C code.

```text
CASE-01:
[PASS] sizeof(ComplexType): 80
[PASS] offset(id): 0
[PASS] offset(flag): 4
[PASS] offset(pt): 8
...
[PASS] All Opcodes match.
RESULT: PASSED
```


PLS UPDATE THE OUTPUT ABOVE - Each test case must be clearly identified bu unique name,  its name printed tohether with the result (PASSED/FAILED).


PLs generate your own complex idl, write the app, build (the build MUST automatically generate the C code and JSON from the idl), run it, and keep fixing and builing until the test passes.