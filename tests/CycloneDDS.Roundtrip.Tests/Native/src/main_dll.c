#include "type_registry.h"
#include "dds/dds.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

// ============================================================================
// Platform Export Macros
// ============================================================================

#ifdef _WIN32
    #ifdef BUILDING_DLL
        #define EXPORT __declspec(dllexport)
    #else
        #define EXPORT __declspec(dllimport)
    #endif
#else
    #define EXPORT __attribute__((visibility("default")))
#endif

// ============================================================================
// Global State
// ============================================================================

static dds_entity_t g_participant = 0;
static dds_entity_t g_publisher = 0;
static dds_entity_t g_subscriber = 0;

static char g_last_error[512] = {0};

// Entity storage (topic -> writer/reader map)
#define MAX_ENTITIES 64

typedef struct {
    char topic_name[128];
    dds_entity_t topic;
    dds_entity_t writer;
    dds_entity_t reader;
} entity_entry_t;

static entity_entry_t g_entities[MAX_ENTITIES];
static int g_entity_count = 0;

// ============================================================================
// Helper Functions
// ============================================================================

static void set_error(const char* message) {
    strncpy(g_last_error, message, sizeof(g_last_error) - 1);
    g_last_error[sizeof(g_last_error) - 1] = '\0';
}

static entity_entry_t* find_entity(const char* topic_name) {
    for (int i = 0; i < g_entity_count; i++) {
        if (strcmp(g_entities[i].topic_name, topic_name) == 0) {
            return &g_entities[i];
        }
    }
    return NULL;
}

static entity_entry_t* add_entity(const char* topic_name) {
    if (g_entity_count >= MAX_ENTITIES) {
        set_error("Entity storage full");
        return NULL;
    }
    
    entity_entry_t* entry = &g_entities[g_entity_count++];
    strncpy(entry->topic_name, topic_name, sizeof(entry->topic_name) - 1);
    entry->topic = 0;
    entry->writer = 0;
    entry->reader = 0;
    
    return entry;
}

// ============================================================================
// Exported API
// ============================================================================

/**
 * Initialize the native test framework
 * @param domain_id DDS domain ID (typically 0)
 */
EXPORT void Native_Init(uint32_t domain_id) {
    printf("[Native] Initializing (Domain %u)...\n", domain_id);
    
    // Create participant
    g_participant = dds_create_participant(domain_id, NULL, NULL);
    if (g_participant < 0) {
        set_error("Failed to create participant");
        printf("[Native] ERROR: %s\n", g_last_error);
        return;
    }
    
    // Create publisher
    g_publisher = dds_create_publisher(g_participant, NULL, NULL);
    if (g_publisher < 0) {
        set_error("Failed to create publisher");
        printf("[Native] ERROR: %s\n", g_last_error);
        return;
    }
    
    // Create subscriber
    g_subscriber = dds_create_subscriber(g_participant, NULL, NULL);
    if (g_subscriber < 0) {
        set_error("Failed to create subscriber");
        printf("[Native] ERROR: %s\n", g_last_error);
        return;
    }
    
    g_entity_count = 0;
    memset(g_entities, 0, sizeof(g_entities));
    
    printf("[Native] Initialization complete.\n");
    
    // Print registered types
    registry_print_all();
}

/**
 * Cleanup and shutdown
 */
EXPORT void Native_Cleanup() {
    printf("[Native] Cleaning up...\n");
    
    // Delete all entities
    for (int i = 0; i < g_entity_count; i++) {
        if (g_entities[i].reader > 0) {
            dds_delete(g_entities[i].reader);
        }
        if (g_entities[i].writer > 0) {
            dds_delete(g_entities[i].writer);
        }
        if (g_entities[i].topic > 0) {
            dds_delete(g_entities[i].topic);
        }
    }
    
    if (g_subscriber > 0) {
        dds_delete(g_subscriber);
    }
    
    if (g_publisher > 0) {
        dds_delete(g_publisher);
    }
    
    if (g_participant > 0) {
        dds_delete(g_participant);
    }
    
    g_participant = 0;
    g_publisher = 0;
    g_subscriber = 0;
    g_entity_count = 0;
    
    printf("[Native] Cleanup complete.\n");
}

/**
 * Create a DDS writer for a topic
 * @param topic_name Topic name
 * @return 0 on success, negative on error
 */
EXPORT int Native_CreatePublisher(const char* topic_name) {
    printf("[Native] Creating publisher for '%s'...\n", topic_name);
    
    const type_handler_t* handler = registry_lookup(topic_name);
    if (handler == NULL) {
        snprintf(g_last_error, sizeof(g_last_error), 
                 "Type '%s' not found in registry", topic_name);
        printf("[Native] ERROR: %s\n", g_last_error);
        return -1;
    }
    
    entity_entry_t* entry = find_entity(topic_name);
    if (entry == NULL) {
        entry = add_entity(topic_name);
        if (entry == NULL) {
            return -1;
        }
    }
    
    // Create topic if needed
    if (entry->topic == 0) {
        entry->topic = dds_create_topic(
            g_participant, 
            handler->descriptor_fn(), 
            topic_name, 
            NULL, 
            NULL
        );
        
        if (entry->topic < 0) {
            set_error("Failed to create topic");
            printf("[Native] ERROR: %s\n", g_last_error);
            return -1;
        }
    }
    
    // Create writer
    if (entry->writer == 0) {
        entry->writer = dds_create_writer(g_publisher, entry->topic, NULL, NULL);
        
        if (entry->writer < 0) {
            set_error("Failed to create writer");
            printf("[Native] ERROR: %s\n", g_last_error);
            return -1;
        }
    }
    
    printf("[Native] Publisher created successfully.\n");
    return 0;
}

/**
 * Create a DDS reader for a topic
 * @param topic_name Topic name
 * @return 0 on success, negative on error
 */
EXPORT int Native_CreateSubscriber(const char* topic_name) {
    printf("[Native] Creating subscriber for '%s'...\n", topic_name);
    
    const type_handler_t* handler = registry_lookup(topic_name);
    if (handler == NULL) {
        snprintf(g_last_error, sizeof(g_last_error), 
                 "Type '%s' not found in registry", topic_name);
        printf("[Native] ERROR: %s\n", g_last_error);
        return -1;
    }
    
    entity_entry_t* entry = find_entity(topic_name);
    if (entry == NULL) {
        entry = add_entity(topic_name);
        if (entry == NULL) {
            return -1;
        }
    }
    
    // Create topic if needed
    if (entry->topic == 0) {
        entry->topic = dds_create_topic(
            g_participant, 
            handler->descriptor_fn(), 
            topic_name, 
            NULL, 
            NULL
        );
        
        if (entry->topic < 0) {
            set_error("Failed to create topic");
            printf("[Native] ERROR: %s\n", g_last_error);
            return -1;
        }
    }
    
    // Create reader
    if (entry->reader == 0) {
        entry->reader = dds_create_reader(g_subscriber, entry->topic, NULL, NULL);
        
        if (entry->reader < 0) {
            set_error("Failed to create reader");
            printf("[Native] ERROR: %s\n", g_last_error);
            return -1;
        }
    }
    
    printf("[Native] Subscriber created successfully.\n");
    return 0;
}

/**
 * Send a message with deterministic seed-based data
 * @param topic_name Topic name
 * @param seed Seed for data generation
 */
EXPORT int Native_SendWithSeed(const char* topic_name, int seed) {
    printf("[Native] Sending on '%s' with seed %d...\n", topic_name, seed);
    
    const type_handler_t* handler = registry_lookup(topic_name);
    if (handler == NULL) {
        set_error("Type not found");
        printf("[Native] ERROR: %s\n", g_last_error);
        return -1;
    }
    
    entity_entry_t* entry = find_entity(topic_name);
    if (entry == NULL || entry->writer == 0) {
        set_error("Writer not created");
        printf("[Native] ERROR: %s\n", g_last_error);
        return -1;
    }
    
    // Allocate and fill sample
    void* sample = handler->alloc_fn();
    handler->fill_fn(sample, seed);
    
    // Write to DDS
    int rc = dds_write(entry->writer, sample);
    int final_result = 0;
    
    if (rc < 0) {
        set_error("dds_write failed");
        printf("[Native] ERROR: %s (rc=%d)\n", g_last_error, rc);
        final_result = -1;
    } else {
        printf("[Native] Message sent successfully.\n");
    }
    
    // Cleanup
    handler->free_fn(sample);
    return final_result;
}

/**
 * Wait for and verify a message with expected seed
 * @param topic_name Topic name
 * @param expected_seed Expected seed value
 * @param timeout_ms Timeout in milliseconds
 * @return 0 if match, -1 if timeout, -2 if mismatch
 */
EXPORT int Native_ExpectWithSeed(const char* topic_name, int expected_seed, int timeout_ms) {
    printf("[Native] Expecting on '%s' with seed %d (timeout %dms)...\n", 
           topic_name, expected_seed, timeout_ms);
    
    const type_handler_t* handler = registry_lookup(topic_name);
    if (handler == NULL) {
        set_error("Type not found");
        return -2;
    }
    
    entity_entry_t* entry = find_entity(topic_name);
    if (entry == NULL || entry->reader == 0) {
        set_error("Reader not created");
        return -2;
    }
    
    // Generate reference sample once
    void* reference = handler->alloc_fn();
    handler->fill_fn(reference, expected_seed);
    
    dds_entity_t waitset = dds_create_waitset(g_participant);
    dds_waitset_attach(waitset, entry->reader, entry->reader);
    
    dds_attach_t triggered[1];
    dds_time_t deadline = dds_time() + DDS_MSECS(timeout_ms);
    
    void* samples[1];
    dds_sample_info_t infos[1];
    samples[0] = handler->alloc_fn();
    
    int result = -1;
    set_error("Timeout waiting for data");

    while (dds_time() < deadline) {
        dds_duration_t rem = deadline - dds_time();
        if (rem < 0) rem = 0;
        
        int wait_rc = dds_waitset_wait(waitset, triggered, 1, rem);
        
        if (wait_rc > 0) {
            int take_rc = dds_take(entry->reader, samples, infos, 1, 1);
            if (take_rc > 0) {
                if (infos[0].valid_data) {
                    if (handler->compare_fn(samples[0], reference)) {
                        printf("[Native] Verification PASSED\n");
                        result = 0;
                        break;
                    } else {
                        // Diagnostic print
                        printf("[Native] Ignored mismatched data (possible loopback/old)\n");
                    }
                }
            }
        }
    }
    
    handler->free_fn(samples[0]);
    handler->free_fn(reference);
    dds_delete(waitset);
    
    if (result != 0) {
         printf("[Native] FAILED: %s\n", g_last_error);
    }
    
    return result;
}

/**
 * Get last error message
 * @return Error string
 */
EXPORT const char* Native_GetLastError() {
    return g_last_error;
}
