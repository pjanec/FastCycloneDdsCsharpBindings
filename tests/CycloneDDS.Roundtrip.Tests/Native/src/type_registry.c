#include "type_registry.h"
#include <string.h>
#include <stdio.h>

// ============================================================================
// Forward Declarations (Handlers)
// ============================================================================

// AllPrimitives
extern void* alloc_AllPrimitives();
extern void free_AllPrimitives(void* sample);
extern const dds_topic_descriptor_t* descriptor_AllPrimitives();
extern void fill_AllPrimitives(void* sample, int seed);
extern bool compare_AllPrimitives(const void* a, const void* b);

// CompositeKey
extern void* alloc_CompositeKey();
extern void free_CompositeKey(void* sample);
extern const dds_topic_descriptor_t* descriptor_CompositeKey();
extern void fill_CompositeKey(void* sample, int seed);
extern bool compare_CompositeKey(const void* a, const void* b);

// NestedKeyTopic
extern void* alloc_NestedKeyTopic();
extern void free_NestedKeyTopic(void* sample);
extern const dds_topic_descriptor_t* descriptor_NestedKeyTopic();
extern void fill_NestedKeyTopic(void* sample, int seed);
extern bool compare_NestedKeyTopic(const void* a, const void* b);

// SequenceTopic
extern void* alloc_SequenceTopic();
extern void free_SequenceTopic(void* sample);
extern const dds_topic_descriptor_t* descriptor_SequenceTopic();
extern void fill_SequenceTopic(void* sample, int seed);
extern bool compare_SequenceTopic(const void* a, const void* b);

// Add more as implemented...

// ============================================================================
// Registry Table
// ============================================================================

static const type_handler_t registry[] = {
    {
        .topic_name = "AllPrimitives",
        .alloc_fn = alloc_AllPrimitives,
        .free_fn = free_AllPrimitives,
        .descriptor_fn = descriptor_AllPrimitives,
        .fill_fn = fill_AllPrimitives,
        .compare_fn = compare_AllPrimitives
    },
    {
        .topic_name = "CompositeKey",
        .alloc_fn = alloc_CompositeKey,
        .free_fn = free_CompositeKey,
        .descriptor_fn = descriptor_CompositeKey,
        .fill_fn = fill_CompositeKey,
        .compare_fn = compare_CompositeKey
    },
    {
        .topic_name = "NestedKeyTopic",
        .alloc_fn = alloc_NestedKeyTopic,
        .free_fn = free_NestedKeyTopic,
        .descriptor_fn = descriptor_NestedKeyTopic,
        .fill_fn = fill_NestedKeyTopic,
        .compare_fn = compare_NestedKeyTopic
    },
    {
        .topic_name = "SequenceTopic",
        .alloc_fn = alloc_SequenceTopic,
        .free_fn = free_SequenceTopic,
        .descriptor_fn = descriptor_SequenceTopic,
        .fill_fn = fill_SequenceTopic,
        .compare_fn = compare_SequenceTopic
    },
    
    // Sentinel (end of table)
    { .topic_name = NULL }
};

// ============================================================================
// Implementation
// ============================================================================

const type_handler_t* registry_lookup(const char* topic_name) {
    if (topic_name == NULL) {
        return NULL;
    }
    
    for (int i = 0; registry[i].topic_name != NULL; i++) {
        if (strcmp(registry[i].topic_name, topic_name) == 0) {
            return &registry[i];
        }
    }
    
    return NULL;
}

void registry_print_all() {
    printf("========================================\n");
    printf("Registered Types:\n");
    printf("========================================\n");
    
    for (int i = 0; registry[i].topic_name != NULL; i++) {
        printf("  [%d] %s\n", i + 1, registry[i].topic_name);
    }
    
    printf("========================================\n");
}
