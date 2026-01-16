#ifndef DDS_EXPORT_H
#define DDS_EXPORT_H

#if defined(DDS_Build_Lib)
  #define DDS_EXPORT __declspec(dllexport)
#else
  #define DDS_EXPORT __declspec(dllimport)
#endif

#define DDS_INLINE_EXPORT
#define DDS_DEPRECATED_EXPORT

#endif
