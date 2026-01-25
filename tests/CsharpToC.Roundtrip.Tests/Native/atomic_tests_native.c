#include "atomic_tests.h"
#include "test_registry.h"
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
#include "dds/dds.h" 

// --- Helper Macros ---
#define DEFINE_HANDLER(TYPE, NAME) \
    const topic_handler_t NAME##_handler = { \
        .name = "AtomicTests::" #TYPE, \
        .descriptor = &AtomicTests_##TYPE##_desc, \
        .generate = generate_##TYPE, \
        .validate = validate_##TYPE, \
        .size = sizeof(AtomicTests_##TYPE) \
    }

// ----------------------------------------------------------------------------
// Primitives
// ----------------------------------------------------------------------------

// --- BooleanTopic ---
static void generate_BooleanTopic(void* data, int seed) {
    AtomicTests_BooleanTopic* msg = (AtomicTests_BooleanTopic*)data;
    msg->id = seed;
    msg->value = (seed % 2) != 0;
}

static int validate_BooleanTopic(void* data, int seed) {
    AtomicTests_BooleanTopic* msg = (AtomicTests_BooleanTopic*)data;
    if (msg->id != seed) return -1;
    bool expected = (seed % 2) != 0;
    if (msg->value != expected) return -1;
    return 0;
}
DEFINE_HANDLER(BooleanTopic, boolean_topic);

// --- Int32Topic ---
static void generate_Int32Topic(void* data, int seed) {
    AtomicTests_Int32Topic* msg = (AtomicTests_Int32Topic*)data;
    msg->id = seed;
    msg->value = (int32_t)((seed * 1664525L) + 1013904223L);
}

static int validate_Int32Topic(void* data, int seed) {
    AtomicTests_Int32Topic* msg = (AtomicTests_Int32Topic*)data;
    if (msg->id != seed) return -1;
    int32_t expected = (int32_t)((seed * 1664525L) + 1013904223L);
    if (msg->value != expected) {
        fprintf(stderr, "Int32Topic mismatch: expected %d, got %d\n", expected, msg->value);
        return -1;
    }
    return 0;
}
DEFINE_HANDLER(Int32Topic, int32_topic);

// ----------------------------------------------------------------------------
// Strings
// ----------------------------------------------------------------------------

// --- StringBounded32Topic ---
static void generate_StringBounded32Topic(void* data, int seed) {
    AtomicTests_StringBounded32Topic* msg = (AtomicTests_StringBounded32Topic*)data;
    msg->id = seed;
    char buffer[32];
    snprintf(buffer, 32, "Str_%d", seed);
    // Fixed size array in struct
    strncpy(msg->value, buffer, 32);
    msg->value[32] = '\0'; // Ensure null termination (although array is 33)
}

static int validate_StringBounded32Topic(void* data, int seed) {
    AtomicTests_StringBounded32Topic* msg = (AtomicTests_StringBounded32Topic*)data;
    if (msg->id != seed) return -1;
    char buffer[32];
    snprintf(buffer, 32, "Str_%d", seed);
    
    // Fixed size array comparison
    if (strncmp(msg->value, buffer, 32) != 0) {
        fprintf(stderr, "StringBounded32Topic mismatch: expected '%s', got '%s'\n", buffer, msg->value);
        return -1;
    }
    return 0;
}
DEFINE_HANDLER(StringBounded32Topic, string_bounded_32_topic);

// ----------------------------------------------------------------------------
// Arrays
// ----------------------------------------------------------------------------

// --- ArrayInt32Topic ---
static void generate_ArrayInt32Topic(void* data, int seed) {
    AtomicTests_ArrayInt32Topic* msg = (AtomicTests_ArrayInt32Topic*)data;
    msg->id = seed;
    for(int i=0; i<5; i++) {
        msg->values[i] = seed + i;
    }
}

static int validate_ArrayInt32Topic(void* data, int seed) {
    AtomicTests_ArrayInt32Topic* msg = (AtomicTests_ArrayInt32Topic*)data;
    if (msg->id != seed) return -1;
    for(int i=0; i<5; i++) {
        if (msg->values[i] != (seed + i)) return -1;
    }
    return 0;
}
DEFINE_HANDLER(ArrayInt32Topic, array_int32_topic);

// ----------------------------------------------------------------------------
// Sequences
// ----------------------------------------------------------------------------

// --- SequenceInt32Topic ---
static void generate_SequenceInt32Topic(void* data, int seed) {
    AtomicTests_SequenceInt32Topic* msg = (AtomicTests_SequenceInt32Topic*)data;
    msg->id = seed;
    
    uint32_t len = (seed % 6);
    msg->values._maximum = len;
    msg->values._length = len;
    msg->values._release = true; 
    
    if (len > 0) {
        msg->values._buffer = dds_alloc(sizeof(int32_t) * len);
        for (uint32_t i = 0; i < len; i++) {
            msg->values._buffer[i] = (int32_t)((seed + i) * 31);
        }
    } else {
        msg->values._buffer = NULL;
    }
}

static int validate_SequenceInt32Topic(void* data, int seed) {
    AtomicTests_SequenceInt32Topic* msg = (AtomicTests_SequenceInt32Topic*)data;
    if (msg->id != seed) return -1;
    
    uint32_t expected_len = (seed % 6);
    if (msg->values._length != expected_len) {
        fprintf(stderr, "Seq len mismatch: expected %d, got %d\n", expected_len, msg->values._length);
        return -1;
    }
    
    for (uint32_t i = 0; i < expected_len; i++) {
        int32_t expected = (int32_t)((seed + i) * 31);
        if (msg->values._buffer[i] != expected) {
            fprintf(stderr, "Seq elem[%d] mismatch: expected %d, got %d\n", i, expected, msg->values._buffer[i]);
            return -1;
        }
    }
    return 0;
}
DEFINE_HANDLER(SequenceInt32Topic, sequence_int32_topic);

// ----------------------------------------------------------------------------
// Unions
// ----------------------------------------------------------------------------

// --- UnionLongDiscTopic ---
// switch(long) { case 1: long; case 2: double; case 3: string; }
static void generate_UnionLongDiscTopic(void* data, int seed) {
    AtomicTests_UnionLongDiscTopic* msg = (AtomicTests_UnionLongDiscTopic*)data;
    msg->id = seed;
    
    int discriminator = (seed % 3) + 1; // 1, 2, 3
    msg->data._d = discriminator;
    
    if (discriminator == 1) {
        msg->data._u.int_value = seed * 100;
    } else if (discriminator == 2) {
        msg->data._u.double_value = seed * 1.5;
    } else if (discriminator == 3) {
        char buffer[64];
        snprintf(buffer, 64, "Union_%d", seed);
        msg->data._u.string_value = dds_string_dup(buffer);
    }
}

static int validate_UnionLongDiscTopic(void* data, int seed) {
    AtomicTests_UnionLongDiscTopic* msg = (AtomicTests_UnionLongDiscTopic*)data;
    if (msg->id != seed) return -1;
    
    int expected_disc = (seed % 3) + 1;
    if (msg->data._d != expected_disc) {
        fprintf(stderr, "Union disc mismatch: expected %d, got %d\n", expected_disc, msg->data._d);
        return -1;
    }
    
    if (expected_disc == 1) {
        if (msg->data._u.int_value != seed * 100) return -1;
    } else if (expected_disc == 2) {
        if (msg->data._u.double_value != (seed * 1.5)) return -1;
    } else if (expected_disc == 3) {
        char buffer[64];
        snprintf(buffer, 64, "Union_%d", seed);
        if (strcmp(msg->data._u.string_value, buffer) != 0) return -1;
    }
    return 0;
}
DEFINE_HANDLER(UnionLongDiscTopic, union_long_disc_topic);

// ============================================================================
// APPENDABLE DUPLICATES HANDLERS
// ============================================================================

// --- BooleanTopicAppendable ---
static void generate_BooleanTopicAppendable(void* data, int seed) {
    AtomicTests_BooleanTopicAppendable* msg = (AtomicTests_BooleanTopicAppendable*)data;
    msg->id = seed;
    msg->value = (seed % 2) != 0;
}

static int validate_BooleanTopicAppendable(void* data, int seed) {
    AtomicTests_BooleanTopicAppendable* msg = (AtomicTests_BooleanTopicAppendable*)data;
    if (msg->id != seed) return -1;
    bool expected = (seed % 2) != 0;
    if (msg->value != expected) return -1;
    return 0;
}
DEFINE_HANDLER(BooleanTopicAppendable, boolean_topic_appendable);

// --- Int32TopicAppendable ---
static void generate_Int32TopicAppendable(void* data, int seed) {
    AtomicTests_Int32TopicAppendable* msg = (AtomicTests_Int32TopicAppendable*)data;
    msg->id = seed;
    msg->value = (int32_t)((seed * 1664525L) + 1013904223L);
}

static int validate_Int32TopicAppendable(void* data, int seed) {
    AtomicTests_Int32TopicAppendable* msg = (AtomicTests_Int32TopicAppendable*)data;
    if (msg->id != seed) return -1;
    int32_t expected = (int32_t)((seed * 1664525L) + 1013904223L);
    if (msg->value != expected) return -1;
    return 0;
}
DEFINE_HANDLER(Int32TopicAppendable, int32_topic_appendable);

// --- StringBounded32TopicAppendable ---
static void generate_StringBounded32TopicAppendable(void* data, int seed) {
    AtomicTests_StringBounded32TopicAppendable* msg = (AtomicTests_StringBounded32TopicAppendable*)data;
    msg->id = seed;
    char buffer[32];
    snprintf(buffer, 32, "Str_%d", seed);
    strncpy(msg->value, buffer, 32);
    msg->value[32] = '\0';
}

static int validate_StringBounded32TopicAppendable(void* data, int seed) {
    AtomicTests_StringBounded32TopicAppendable* msg = (AtomicTests_StringBounded32TopicAppendable*)data;
    if (msg->id != seed) return -1;
    char buffer[32];
    snprintf(buffer, 32, "Str_%d", seed);
    if (strncmp(msg->value, buffer, 32) != 0) return -1;
    return 0;
}
DEFINE_HANDLER(StringBounded32TopicAppendable, string_bounded_32_topic_appendable);

// --- SequenceInt32TopicAppendable ---
static void generate_SequenceInt32TopicAppendable(void* data, int seed) {
    AtomicTests_SequenceInt32TopicAppendable* msg = (AtomicTests_SequenceInt32TopicAppendable*)data;
    msg->id = seed;
    
    uint32_t len = (seed % 6);
    msg->values._maximum = len;
    msg->values._length = len;
    msg->values._release = true; 
    
    if (len > 0) {
        msg->values._buffer = dds_alloc(sizeof(int32_t) * len);
        for (uint32_t i = 0; i < len; i++) {
            msg->values._buffer[i] = (int32_t)((seed + i) * 31);
        }
    } else {
        msg->values._buffer = NULL;
    }
}

static int validate_SequenceInt32TopicAppendable(void* data, int seed) {
    AtomicTests_SequenceInt32TopicAppendable* msg = (AtomicTests_SequenceInt32TopicAppendable*)data;
    if (msg->id != seed) return -1;
    
    uint32_t expected_len = (seed % 6);
    if (msg->values._length != expected_len) return -1;
    
    for (uint32_t i = 0; i < expected_len; i++) {
        int32_t expected = (int32_t)((seed + i) * 31);
        if (msg->values._buffer[i] != expected) return -1;
    }
    return 0;
}
DEFINE_HANDLER(SequenceInt32TopicAppendable, sequence_int32_topic_appendable);

// --- UnionLongDiscTopicAppendable ---
static void generate_UnionLongDiscTopicAppendable(void* data, int seed) {
    AtomicTests_UnionLongDiscTopicAppendable* msg = (AtomicTests_UnionLongDiscTopicAppendable*)data;
    msg->id = seed;
    
    int discriminator = (seed % 3) + 1;
    msg->data._d = discriminator;
    
    if (discriminator == 1) {
        msg->data._u.int_value = seed * 100;
    } else if (discriminator == 2) {
        msg->data._u.double_value = seed * 1.5;
    } else if (discriminator == 3) {
        char buffer[64];
        snprintf(buffer, 64, "Union_%d", seed);
        msg->data._u.string_value = dds_string_dup(buffer);
    }
}

static int validate_UnionLongDiscTopicAppendable(void* data, int seed) {
    AtomicTests_UnionLongDiscTopicAppendable* msg = (AtomicTests_UnionLongDiscTopicAppendable*)data;
    if (msg->id != seed) return -1;
    
    int expected_disc = (seed % 3) + 1;
    if (msg->data._d != expected_disc) return -1;
    
    if (expected_disc == 1) {
        if (msg->data._u.int_value != seed * 100) return -1;
    } else if (expected_disc == 2) {
        if (msg->data._u.double_value != (seed * 1.5)) return -1;
    } else if (expected_disc == 3) {
        char buffer[64];
        snprintf(buffer, 64, "Union_%d", seed);
        if (strcmp(msg->data._u.string_value, buffer) != 0) return -1;
    }
    return 0;
}
DEFINE_HANDLER(UnionLongDiscTopicAppendable, union_long_disc_topic_appendable);
