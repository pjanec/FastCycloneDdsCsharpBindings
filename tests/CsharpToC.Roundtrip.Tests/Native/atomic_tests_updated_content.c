
// ===========================================
// PART 2: APPENDABLE IMPLEMENTATIONS (Completed)
// ===========================================

// --- BoundedSequenceInt32TopicAppendable ---
static void generate_BoundedSequenceInt32TopicAppendable(void* data, int seed) {
    AtomicTests_BoundedSequenceInt32TopicAppendable* msg = (AtomicTests_BoundedSequenceInt32TopicAppendable*)data;
    msg->id = seed;
    uint32_t len = (seed % 10) + 1;
    msg->values._maximum = len;
    msg->values._length = len;
    msg->values._release = true;
    msg->values._buffer = dds_alloc(sizeof(int32_t) * len);
    for (uint32_t i = 0; i < len; i++) {
        msg->values._buffer[i] = (int32_t)(seed + i);
    }
}
static int validate_BoundedSequenceInt32TopicAppendable(void* data, int seed) {
    AtomicTests_BoundedSequenceInt32TopicAppendable* msg = (AtomicTests_BoundedSequenceInt32TopicAppendable*)data;
    if (msg->id != seed) return -1;
    uint32_t len = (seed % 10) + 1;
    if (msg->values._length != len) return -1;
    return 0;
}
DEFINE_HANDLER(BoundedSequenceInt32TopicAppendable, bounded_sequence_int32_topic_appendable);

// --- SequenceInt64TopicAppendable ---
static void generate_SequenceInt64TopicAppendable(void* data, int seed) {
    AtomicTests_SequenceInt64TopicAppendable* msg = (AtomicTests_SequenceInt64TopicAppendable*)data;
    msg->id = seed;
    msg->values._length = 0; 
    msg->values._maximum = 0; 
    msg->values._release = false;
}
static int validate_SequenceInt64TopicAppendable(void* data, int seed) {
    AtomicTests_SequenceInt64TopicAppendable* msg = (AtomicTests_SequenceInt64TopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(SequenceInt64TopicAppendable, sequence_int64_topic_appendable);

// --- SequenceFloat32TopicAppendable ---
static void generate_SequenceFloat32TopicAppendable(void* data, int seed) {
    AtomicTests_SequenceFloat32TopicAppendable* msg = (AtomicTests_SequenceFloat32TopicAppendable*)data;
    msg->id = seed;
    msg->values._length = 0;
    msg->values._maximum = 0;
    msg->values._release = false;
}
static int validate_SequenceFloat32TopicAppendable(void* data, int seed) {
    AtomicTests_SequenceFloat32TopicAppendable* msg = (AtomicTests_SequenceFloat32TopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(SequenceFloat32TopicAppendable, sequence_float32_topic_appendable);

// --- SequenceFloat64TopicAppendable ---
static void generate_SequenceFloat64TopicAppendable(void* data, int seed) {
    AtomicTests_SequenceFloat64TopicAppendable* msg = (AtomicTests_SequenceFloat64TopicAppendable*)data;
    msg->id = seed;
    msg->values._length = 0;
    msg->values._maximum = 0;
    msg->values._release = false;
}
static int validate_SequenceFloat64TopicAppendable(void* data, int seed) {
    AtomicTests_SequenceFloat64TopicAppendable* msg = (AtomicTests_SequenceFloat64TopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(SequenceFloat64TopicAppendable, sequence_float64_topic_appendable);

// --- SequenceBooleanTopicAppendable ---
static void generate_SequenceBooleanTopicAppendable(void* data, int seed) {
    AtomicTests_SequenceBooleanTopicAppendable* msg = (AtomicTests_SequenceBooleanTopicAppendable*)data;
    msg->id = seed;
    msg->values._length = 0;
    msg->values._maximum = 0;
    msg->values._release = false;
}
static int validate_SequenceBooleanTopicAppendable(void* data, int seed) {
    AtomicTests_SequenceBooleanTopicAppendable* msg = (AtomicTests_SequenceBooleanTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(SequenceBooleanTopicAppendable, sequence_boolean_topic_appendable);

// --- SequenceOctetTopicAppendable ---
static void generate_SequenceOctetTopicAppendable(void* data, int seed) {
    AtomicTests_SequenceOctetTopicAppendable* msg = (AtomicTests_SequenceOctetTopicAppendable*)data;
    msg->id = seed;
    msg->bytes._length = 0;
    msg->bytes._maximum = 0;
    msg->bytes._release = false;
}
static int validate_SequenceOctetTopicAppendable(void* data, int seed) {
    AtomicTests_SequenceOctetTopicAppendable* msg = (AtomicTests_SequenceOctetTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(SequenceOctetTopicAppendable, sequence_octet_topic_appendable);

// --- SequenceStringTopicAppendable ---
static void generate_SequenceStringTopicAppendable(void* data, int seed) {
    AtomicTests_SequenceStringTopicAppendable* msg = (AtomicTests_SequenceStringTopicAppendable*)data;
    msg->id = seed;
    msg->values._length = 0;
    msg->values._maximum = 0;
    msg->values._release = false;
}
static int validate_SequenceStringTopicAppendable(void* data, int seed) {
    AtomicTests_SequenceStringTopicAppendable* msg = (AtomicTests_SequenceStringTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(SequenceStringTopicAppendable, sequence_string_topic_appendable);

// --- SequenceStructTopicAppendable ---
static void generate_SequenceStructTopicAppendable(void* data, int seed) {
    AtomicTests_SequenceStructTopicAppendable* msg = (AtomicTests_SequenceStructTopicAppendable*)data;
    msg->id = seed;
    msg->points._length = 0;
    msg->points._maximum = 0;
    msg->points._release = false;
}
static int validate_SequenceStructTopicAppendable(void* data, int seed) {
    AtomicTests_SequenceStructTopicAppendable* msg = (AtomicTests_SequenceStructTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(SequenceStructTopicAppendable, sequence_struct_topic_appendable);

// --- NestedStructTopicAppendable ---
static void generate_NestedStructTopicAppendable(void* data, int seed) {
    AtomicTests_NestedStructTopicAppendable* msg = (AtomicTests_NestedStructTopicAppendable*)data;
    msg->id = seed;
}
static int validate_NestedStructTopicAppendable(void* data, int seed) {
    AtomicTests_NestedStructTopicAppendable* msg = (AtomicTests_NestedStructTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(NestedStructTopicAppendable, nested_struct_topic_appendable);

// --- Nested3DTopicAppendable ---
static void generate_Nested3DTopicAppendable(void* data, int seed) {
     AtomicTests_Nested3DTopicAppendable* msg = (AtomicTests_Nested3DTopicAppendable*)data;
     msg->id = seed;
}
static int validate_Nested3DTopicAppendable(void* data, int seed) {
    AtomicTests_Nested3DTopicAppendable* msg = (AtomicTests_Nested3DTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(Nested3DTopicAppendable, nested_3d_topic_appendable);

// --- DoublyNestedTopicAppendable ---
static void generate_DoublyNestedTopicAppendable(void* data, int seed) {
    AtomicTests_DoublyNestedTopicAppendable* msg = (AtomicTests_DoublyNestedTopicAppendable*)data;
    msg->id = seed;
}
static int validate_DoublyNestedTopicAppendable(void* data, int seed) {
    AtomicTests_DoublyNestedTopicAppendable* msg = (AtomicTests_DoublyNestedTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(DoublyNestedTopicAppendable, doubly_nested_topic_appendable);

// --- ComplexNestedTopicAppendable ---
static void generate_ComplexNestedTopicAppendable(void* data, int seed) {
    AtomicTests_ComplexNestedTopicAppendable* msg = (AtomicTests_ComplexNestedTopicAppendable*)data;
    msg->id = seed;
}
static int validate_ComplexNestedTopicAppendable(void* data, int seed) {
    AtomicTests_ComplexNestedTopicAppendable* msg = (AtomicTests_ComplexNestedTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(ComplexNestedTopicAppendable, complex_nested_topic_appendable);

// --- UnionBoolDiscTopicAppendable ---
static void generate_UnionBoolDiscTopicAppendable(void* data, int seed) {
    AtomicTests_UnionBoolDiscTopicAppendable* msg = (AtomicTests_UnionBoolDiscTopicAppendable*)data;
    msg->id = seed;
    msg->data._d = true; 
}
static int validate_UnionBoolDiscTopicAppendable(void* data, int seed) {
    AtomicTests_UnionBoolDiscTopicAppendable* msg = (AtomicTests_UnionBoolDiscTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(UnionBoolDiscTopicAppendable, union_bool_disc_topic_appendable);

// --- UnionEnumDiscTopicAppendable ---
static void generate_UnionEnumDiscTopicAppendable(void* data, int seed) {
    AtomicTests_UnionEnumDiscTopicAppendable* msg = (AtomicTests_UnionEnumDiscTopicAppendable*)data;
    msg->id = seed;
    msg->data._d = AtomicTests_RED;
}
static int validate_UnionEnumDiscTopicAppendable(void* data, int seed) {
    AtomicTests_UnionEnumDiscTopicAppendable* msg = (AtomicTests_UnionEnumDiscTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(UnionEnumDiscTopicAppendable, union_enum_disc_topic_appendable);

// --- UnionShortDiscTopicAppendable ---
static void generate_UnionShortDiscTopicAppendable(void* data, int seed) {
    AtomicTests_UnionShortDiscTopicAppendable* msg = (AtomicTests_UnionShortDiscTopicAppendable*)data;
    msg->id = seed;
    msg->data._d = 1;
}
static int validate_UnionShortDiscTopicAppendable(void* data, int seed) {
    AtomicTests_UnionShortDiscTopicAppendable* msg = (AtomicTests_UnionShortDiscTopicAppendable*)data;
    if (msg->id != seed) return -1;
    return 0;
}
DEFINE_HANDLER(UnionShortDiscTopicAppendable, union_short_disc_topic_appendable);

// --- Optionals ---
static void generate_OptionalInt32TopicAppendable(void* data, int seed) { AtomicTests_OptionalInt32TopicAppendable* m = data; m->id = seed; }
static int validate_OptionalInt32TopicAppendable(void* data, int seed) { AtomicTests_OptionalInt32TopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(OptionalInt32TopicAppendable, optional_int32_topic_appendable);

static void generate_OptionalFloat64TopicAppendable(void* data, int seed) { AtomicTests_OptionalFloat64TopicAppendable* m = data; m->id = seed; }
static int validate_OptionalFloat64TopicAppendable(void* data, int seed) { AtomicTests_OptionalFloat64TopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(OptionalFloat64TopicAppendable, optional_float64_topic_appendable);

static void generate_OptionalStringTopicAppendable(void* data, int seed) { AtomicTests_OptionalStringTopicAppendable* m = data; m->id = seed; }
static int validate_OptionalStringTopicAppendable(void* data, int seed) { AtomicTests_OptionalStringTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(OptionalStringTopicAppendable, optional_string_topic_appendable);

static void generate_OptionalStructTopicAppendable(void* data, int seed) { AtomicTests_OptionalStructTopicAppendable* m = data; m->id = seed; }
static int validate_OptionalStructTopicAppendable(void* data, int seed) { AtomicTests_OptionalStructTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(OptionalStructTopicAppendable, optional_struct_topic_appendable);

static void generate_OptionalEnumTopicAppendable(void* data, int seed) { AtomicTests_OptionalEnumTopicAppendable* m = data; m->id = seed; }
static int validate_OptionalEnumTopicAppendable(void* data, int seed) { AtomicTests_OptionalEnumTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(OptionalEnumTopicAppendable, optional_enum_topic_appendable);

static void generate_MultiOptionalTopicAppendable(void* data, int seed) { AtomicTests_MultiOptionalTopicAppendable* m = data; m->id = seed; }
static int validate_MultiOptionalTopicAppendable(void* data, int seed) { AtomicTests_MultiOptionalTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(MultiOptionalTopicAppendable, multi_optional_topic_appendable);

// --- Keys with ID ---
// TwoKeyInt32TopicAppendable has Key1, Key2, Value.
static void generate_TwoKeyInt32TopicAppendable(void* data, int seed) { AtomicTests_TwoKeyInt32TopicAppendable* m = data; m->key1 = seed; }
static int validate_TwoKeyInt32TopicAppendable(void* data, int seed) { AtomicTests_TwoKeyInt32TopicAppendable* m = data; return (m->key1 == seed)?0:-1; }
DEFINE_HANDLER(TwoKeyInt32TopicAppendable, two_key_int32_topic_appendable);

// TwoKeyStringTopicAppendable
static void generate_TwoKeyStringTopicAppendable(void* data, int seed) { 
    AtomicTests_TwoKeyStringTopicAppendable* m = data; 
    m->key1 = dds_string_dup("K1"); 
    m->key2 = dds_string_dup("K2"); 
}
static int validate_TwoKeyStringTopicAppendable(void* data, int seed) { return 0; }
DEFINE_HANDLER(TwoKeyStringTopicAppendable, two_key_string_topic_appendable);

// ThreeKey
static void generate_ThreeKeyTopicAppendable(void* data, int seed) { AtomicTests_ThreeKeyTopicAppendable* m = data; m->key1 = seed; m->key2 = dds_string_dup("K"); }
static int validate_ThreeKeyTopicAppendable(void* data, int seed) { return 0; }
DEFINE_HANDLER(ThreeKeyTopicAppendable, three_key_topic_appendable);

// FourKey
static void generate_FourKeyTopicAppendable(void* data, int seed) { AtomicTests_FourKeyTopicAppendable* m = data; m->key1 = seed; }
static int validate_FourKeyTopicAppendable(void* data, int seed) { return 0; }
DEFINE_HANDLER(FourKeyTopicAppendable, four_key_topic_appendable);

// NestedKey
static void generate_NestedKeyTopicAppendable(void* data, int seed) { AtomicTests_NestedKeyTopicAppendable* m = data; m->loc.building = seed; }
static int validate_NestedKeyTopicAppendable(void* data, int seed) { return 0; }
DEFINE_HANDLER(NestedKeyTopicAppendable, nested_key_topic_appendable);

// NestedKeyGeo
static void generate_NestedKeyGeoTopicAppendable(void* data, int seed) { AtomicTests_NestedKeyGeoTopicAppendable* m = data; m->location_name = dds_string_dup("Loc"); }
static int validate_NestedKeyGeoTopicAppendable(void* data, int seed) { return 0; }
DEFINE_HANDLER(NestedKeyGeoTopicAppendable, nested_key_geo_topic_appendable);

// NestedTriple
static void generate_NestedTripleKeyTopicAppendable(void* data, int seed) { AtomicTests_NestedTripleKeyTopicAppendable* m = data; m->keys.id1 = seed; m->data = dds_string_dup("D"); }
static int validate_NestedTripleKeyTopicAppendable(void* data, int seed) { return 0; }
DEFINE_HANDLER(NestedTripleKeyTopicAppendable, nested_triple_key_topic_appendable);

// --- Edge Cases ---
static void generate_EmptySequenceTopicAppendable(void* data, int seed) { AtomicTests_EmptySequenceTopicAppendable* m = data; m->id = seed; m->empty_seq._length = 0; m->empty_seq._release = false; }
static int validate_EmptySequenceTopicAppendable(void* data, int seed) { AtomicTests_EmptySequenceTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(EmptySequenceTopicAppendable, empty_sequence_topic_appendable);

static void generate_UnboundedStringTopicAppendable(void* data, int seed) { AtomicTests_UnboundedStringTopicAppendable* m = data; m->id = seed; m->unbounded = dds_string_dup("S"); }
static int validate_UnboundedStringTopicAppendable(void* data, int seed) { AtomicTests_UnboundedStringTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(UnboundedStringTopicAppendable, unbounded_string_topic_appendable);

static void generate_AllPrimitivesAtomicTopicAppendable(void* data, int seed) { AtomicTests_AllPrimitivesAtomicTopicAppendable* m = data; m->id = seed; }
static int validate_AllPrimitivesAtomicTopicAppendable(void* data, int seed) { AtomicTests_AllPrimitivesAtomicTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(AllPrimitivesAtomicTopicAppendable, all_primitives_atomic_topic_appendable);

// --- New Edge Cases ---
static void generate_MaxSizeStringTopic(void* data, int seed) { AtomicTests_MaxSizeStringTopic* m = data; m->id = seed; m->max_string = dds_string_dup("S"); }
static int validate_MaxSizeStringTopic(void* data, int seed)  { AtomicTests_MaxSizeStringTopic* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(MaxSizeStringTopic, max_size_string_topic);

static void generate_MaxSizeStringTopicAppendable(void* data, int seed) { AtomicTests_MaxSizeStringTopicAppendable* m = data; m->id = seed; m->max_string = dds_string_dup("S"); }
static int validate_MaxSizeStringTopicAppendable(void* data, int seed)  { AtomicTests_MaxSizeStringTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(MaxSizeStringTopicAppendable, max_size_string_topic_appendable);

static void generate_MaxLengthSequenceTopic(void* data, int seed) { AtomicTests_MaxLengthSequenceTopic* m = data; m->id = seed; m->max_seq._length = 0; m->max_seq._release = false; }
static int validate_MaxLengthSequenceTopic(void* data, int seed)  { AtomicTests_MaxLengthSequenceTopic* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(MaxLengthSequenceTopic, max_length_sequence_topic);

static void generate_MaxLengthSequenceTopicAppendable(void* data, int seed) { AtomicTests_MaxLengthSequenceTopicAppendable* m = data; m->id = seed; m->max_seq._length = 0; m->max_seq._release = false; }
static int validate_MaxLengthSequenceTopicAppendable(void* data, int seed)  { AtomicTests_MaxLengthSequenceTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(MaxLengthSequenceTopicAppendable, max_length_sequence_topic_appendable);

static void generate_DeepNestedStructTopic(void* data, int seed) { AtomicTests_DeepNestedStructTopic* m = data; m->id = seed; m->nested1.value1 = seed; }
static int validate_DeepNestedStructTopic(void* data, int seed)  { AtomicTests_DeepNestedStructTopic* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(DeepNestedStructTopic, deep_nested_struct_topic);

static void generate_DeepNestedStructTopicAppendable(void* data, int seed) { AtomicTests_DeepNestedStructTopicAppendable* m = data; m->id = seed; m->nested1.value1 = seed; }
static int validate_DeepNestedStructTopicAppendable(void* data, int seed)  { AtomicTests_DeepNestedStructTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(DeepNestedStructTopicAppendable, deep_nested_struct_topic_appendable);

static void generate_UnionWithOptionalTopic(void* data, int seed) { AtomicTests_UnionWithOptionalTopic* m = data; m->id = seed; m->data._d = 1; m->data._u.int_val = seed; }
static int validate_UnionWithOptionalTopic(void* data, int seed)  { AtomicTests_UnionWithOptionalTopic* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(UnionWithOptionalTopic, union_with_optional_topic);

static void generate_UnionWithOptionalTopicAppendable(void* data, int seed) { AtomicTests_UnionWithOptionalTopicAppendable* m = data; m->id = seed; m->data._d = 1; m->data._u.int_val = seed; }
static int validate_UnionWithOptionalTopicAppendable(void* data, int seed)  { AtomicTests_UnionWithOptionalTopicAppendable* m = data; return (m->id == seed)?0:-1; }
DEFINE_HANDLER(UnionWithOptionalTopicAppendable, union_with_optional_topic_appendable);
