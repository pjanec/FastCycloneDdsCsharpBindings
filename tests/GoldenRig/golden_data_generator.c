#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "dds/dds.h"
#include "Golden.h"
#include "dds/cdr/dds_cdrstream.h"

// Helper to print hex
void print_hex(const uint8_t* data, size_t len, const char* name) {
    printf("%s: ", name);
    for (size_t i = 0; i < len; i++) {
        printf("%02X", data[i]);
    }
    printf("\n");
}

// Helper to serialize and print
void serialize_and_print(const void* sample, const dds_topic_descriptor_t* desc, const char* name) {
    struct dds_cdrstream_desc cdr_desc;
    dds_cdrstream_desc_from_topic_desc(&cdr_desc, desc);

    uint8_t buffer[4096]; // Sufficiently large buffer
    dds_ostream_t os;
    dds_ostream_init(&os, sizeof(buffer), DDSI_RTPS_CDR_ENC_VERSION_2);
    os.m_buffer = buffer;

    bool ok = dds_stream_write_sample(&os, sample, &cdr_desc);
    if (!ok) {
        fprintf(stderr, "Failed to serialize %s\n", name);
    } else {
        print_hex(buffer, os.m_index, name);
    }

    dds_cdrstream_desc_fini(&cdr_desc);
}

int main() {
    // 1. SimplePrimitive
    {
        Golden_SimplePrimitive sample;
        sample.id = 123456789;
        sample.value = 123.456;
        serialize_and_print(&sample, &Golden_SimplePrimitive_desc, "SimplePrimitive");
    }

    // 2. NestedStruct
    {
        Golden_NestedStruct sample;
        sample.byte_field = 0xAB;
        sample.nested.a = 987654321;
        sample.nested.b = 987.654;
        serialize_and_print(&sample, &Golden_NestedStruct_desc, "NestedStruct");
    }

    // 3. FixedString
    {
        Golden_FixedString sample;
        // Fixed string is char[32] in C
        strncpy(sample.message, "FixedString123", 32);
        serialize_and_print(&sample, &Golden_FixedString_desc, "FixedString");
    }

    // 4. UnboundedString
    {
        Golden_UnboundedString sample;
        sample.id = 111222;
        sample.message = "UnboundedStringData";
        serialize_and_print(&sample, &Golden_UnboundedString_desc, "UnboundedString");
        // Note: dds_stream_write_sample does not free the sample strings, we don't need to free them as they are literals/stack
    }

    // 5. PrimitiveSequence
    {
        Golden_PrimitiveSequence sample;
        int32_t values[] = { 10, 20, 30, 40, 50 };
        sample.values._length = 5;
        sample.values._maximum = 5;
        sample.values._buffer = values;
        sample.values._release = false;
        serialize_and_print(&sample, &Golden_PrimitiveSequence_desc, "PrimitiveSequence");
    }

    // 6. StringSequence
    {
        Golden_StringSequence sample;
        char* strings[] = { "One", "Two", "Three" };
        sample.values._length = 3;
        sample.values._maximum = 3;
        sample.values._buffer = strings;
        sample.values._release = false;
        serialize_and_print(&sample, &Golden_StringSequence_desc, "StringSequence");
    }

    // 7. MixedStruct
    {
        Golden_MixedStruct sample;
        sample.b = 0xFF;
        sample.i = -555;
        sample.d = 0.00001;
        sample.s = "MixedString";
        serialize_and_print(&sample, &Golden_MixedStruct_desc, "MixedStruct");
    }

    // 8. AppendableStruct
    {
        Golden_AppendableStruct sample;
        sample.id = 999;
        sample.message = "Appendable";
        serialize_and_print(&sample, &Golden_AppendableStruct_desc, "AppendableStruct");
    }

    return 0;
}
