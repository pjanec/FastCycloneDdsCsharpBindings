
// --- SequenceUnionAppendableTopic ---
static void generate_SequenceUnionAppendableTopic(void* data, int seed) {
    AtomicTests_SequenceUnionAppendableTopic* msg = (AtomicTests_SequenceUnionAppendableTopic*)data;
    msg->id = seed;
    
    // Logic: len = (s % 2) + 1; (1 or 2)
    int len = (seed % 2) + 1;
    msg->unions._length = len;
    msg->unions._maximum = len;
    msg->unions._release = true; // generated native needs to own this memory? or we allocate via dds_alloc
    msg->unions._buffer = dds_sequence_AtomicTests_SimpleUnionAppendable_allocbuf(len);
    
    for (int i = 0; i < len; i++) {
        AtomicTests_SimpleUnionAppendable* u = &msg->unions._buffer[i];
        int disc = ((seed + i) % 3) + 1;
        u->_d = disc;
        if (disc == 1) {
            u->_u.int_value = (seed + i) * 10;
        } else if (disc == 2) {
            u->_u.double_value = (seed + i) * 2.5;
        } else if (disc == 3) {
            char buf[64];
            snprintf(buf, sizeof(buf), "U_%d_%d", seed, i);
            u->_u.string_value = dds_string_dup(buf);
        }
    }
}

static int validate_SequenceUnionAppendableTopic(void* data, int seed) {
    AtomicTests_SequenceUnionAppendableTopic* msg = (AtomicTests_SequenceUnionAppendableTopic*)data;
    if (msg->id != seed) return -1;
    
    int len = (seed % 2) + 1;
    if (msg->unions._length != len) return -1;
    
    for (int i = 0; i < len; i++) {
        AtomicTests_SimpleUnionAppendable* u = &msg->unions._buffer[i];
        int disc = ((seed + i) % 3) + 1;
        if (u->_d != disc) return -1;
        
        if (disc == 1) {
            if (u->_u.int_value != (seed + i) * 10) return -1;
        } else if (disc == 2) {
            if (fabs(u->_u.double_value - ((seed + i) * 2.5)) > 0.0001) return -1;
        } else if (disc == 3) {
            char expected[64];
            snprintf(expected, sizeof(expected), "U_%d_%d", seed, i);
            if (strcmp(u->_u.string_value, expected) != 0) return -1;
        }
    }
    return 0;
}
DEFINE_HANDLER(SequenceUnionAppendableTopic, sequence_union_appendable_topic);

// --- SequenceEnumAppendableTopic ---
static void generate_SequenceEnumAppendableTopic(void* data, int seed) {
    AtomicTests_SequenceEnumAppendableTopic* msg = (AtomicTests_SequenceEnumAppendableTopic*)data;
    msg->id = seed;
    
    // Logic: len = (s % 3) + 1;
    int len = (seed % 3) + 1;
    msg->colors._length = len;
    msg->colors._maximum = len;
    msg->colors._release = true;
    msg->colors._buffer = dds_sequence_AtomicTests_ColorEnum_allocbuf(len);
    
    for (int i = 0; i < len; i++) {
        msg->colors._buffer[i] = (AtomicTests_ColorEnum)((seed + i) % 6);
    }
}

static int validate_SequenceEnumAppendableTopic(void* data, int seed) {
    AtomicTests_SequenceEnumAppendableTopic* msg = (AtomicTests_SequenceEnumAppendableTopic*)data;
    if (msg->id != seed) return -1;
    
    int len = (seed % 3) + 1;
    if (msg->colors._length != len) return -1;
    
    for (int i = 0; i < len; i++) {
         if (msg->colors._buffer[i] != (AtomicTests_ColorEnum)((seed + i) % 6)) return -1;
    }
    return 0;
}
DEFINE_HANDLER(SequenceEnumAppendableTopic, sequence_enum_appendable_topic);
