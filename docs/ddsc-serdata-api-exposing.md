cyclonedds\src\core\ddsc\include\dds\dds.h
-------------------------------------------
...
#include "dds/ddsrt/time.h"
#include "dds/ddsrt/retcode.h"
#include "dds/ddsrt/log.h"
#include "dds/ddsrt/iovec.h" // <============ added
#include "dds/ddsc/dds_public_impl.h"
#include "dds/ddsc/dds_public_alloc.h"
#include "dds/ddsc/dds_public_qos.h"
...



cyclonedds\src/core/ddsc/src/dds_topic.c
------------------------------


/**
 * @brief Get the sertype associated with a topic.
 * @ingroup topic
 *
 * @param[in] topic The topic entity.
 * @returns The sertype pointer, or NULL if the entity is not a topic or invalid.
 */
DDS_EXPORT const struct ddsi_sertype * dds_get_topic_sertype (dds_entity_t topic)
{
  struct dds_topic *tp;
  if (dds_topic_pin (topic, &tp) != DDS_RETCODE_OK)
    return NULL;
  const struct ddsi_sertype *st = tp->m_stype;
  dds_topic_unpin (tp);
  return st;
}

#include "dds/ddsi/ddsi_serdata.h"

DDS_EXPORT struct ddsi_serdata *dds_serdata_ref(struct ddsi_serdata *serdata) {
    return ddsi_serdata_ref(serdata);
}

DDS_EXPORT void dds_serdata_unref(struct ddsi_serdata *serdata) {
    ddsi_serdata_unref(serdata);
}

DDS_EXPORT uint32_t dds_serdata_size(const struct ddsi_serdata *serdata) {
    return ddsi_serdata_size(serdata);
}

DDS_EXPORT void dds_serdata_to_ser(const struct ddsi_serdata *serdata, size_t off, size_t sz, void *buf) {
    ddsi_serdata_to_ser(serdata, off, sz, buf);
}

DDS_EXPORT struct ddsi_serdata *dds_serdata_from_ser_iov(const struct ddsi_sertype *type, int kind, uint32_t niov, const ddsrt_iovec_t *iov, size_t size) {
    return ddsi_serdata_from_ser_iov(type, (enum ddsi_serdata_kind)kind, niov, iov, size);
}

DDS_EXPORT uint32_t dds_sample_info_size(void) {
    return (uint32_t)sizeof(dds_sample_info_t);
}

