#ifndef TYPE_REGISTRY_H
#define TYPE_REGISTRY_H

#include "dds/dds.h"
#include <stdint.h>
#include <stdbool.h>

// ============================================================================
// Type Handler Function Signatures
// ============================================================================

/**
 * Allocates a new instance of the type
 */
typedef void* (*type_alloc_fn)();

/**
 * Frees a type instance and all its resources
 */
typedef void (*type_free_fn)(void* sample);

/**
 * Returns the DDS topic descriptor for this type
 */
typedef const dds_topic_descriptor_t* (*type_descriptor_fn)();

/**
 * Fills a type instance deterministically based on a seed value
 * @param sample Pointer to the allocated type instance
 * @param seed Integer seed for deterministic generation
 */
typedef void (*type_fill_fn)(void* sample, int seed);

/**
 * Compares two type instances for equality
 * @param a First instance
 * @param b Second instance
 * @return true if equal, false otherwise
 */
typedef bool (*type_compare_fn)(const void* a, const void* b);

// ============================================================================
// Type Handler Structure
// ============================================================================

typedef struct {
    const char* topic_name;
    type_alloc_fn alloc_fn;
    type_free_fn free_fn;
    type_descriptor_fn descriptor_fn;
    type_fill_fn fill_fn;
    type_compare_fn compare_fn;
} type_handler_t;

// ============================================================================
// Registry API
// ============================================================================

/**
 * Looks up a type handler by topic name
 * @param topic_name Topic name string
 * @return Pointer to handler or NULL if not found
 */
const type_handler_t* registry_lookup(const char* topic_name);

/**
 * Prints all registered types to stdout (for debugging)
 */
void registry_print_all();

#endif // TYPE_REGISTRY_H
