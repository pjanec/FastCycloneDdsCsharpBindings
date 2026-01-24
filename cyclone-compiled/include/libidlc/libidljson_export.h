
#ifndef LIBIDLJSON_EXPORT_H
#define LIBIDLJSON_EXPORT_H

#ifdef LIBIDLJSON_STATIC_DEFINE
#  define LIBIDLJSON_EXPORT
#  define LIBIDLJSON_NO_EXPORT
#else
#  ifndef LIBIDLJSON_EXPORT
#    ifdef libidljson_EXPORTS
        /* We are building this library */
#      define LIBIDLJSON_EXPORT __declspec(dllexport)
#    else
        /* We are using this library */
#      define LIBIDLJSON_EXPORT __declspec(dllimport)
#    endif
#  endif

#  ifndef LIBIDLJSON_NO_EXPORT
#    define LIBIDLJSON_NO_EXPORT 
#  endif
#endif

#ifndef LIBIDLJSON_DEPRECATED
#  define LIBIDLJSON_DEPRECATED __declspec(deprecated)
#endif

#ifndef LIBIDLJSON_DEPRECATED_EXPORT
#  define LIBIDLJSON_DEPRECATED_EXPORT LIBIDLJSON_EXPORT LIBIDLJSON_DEPRECATED
#endif

#ifndef LIBIDLJSON_DEPRECATED_NO_EXPORT
#  define LIBIDLJSON_DEPRECATED_NO_EXPORT LIBIDLJSON_NO_EXPORT LIBIDLJSON_DEPRECATED
#endif

#if 0 /* DEFINE_NO_DEPRECATED */
#  ifndef LIBIDLJSON_NO_DEPRECATED
#    define LIBIDLJSON_NO_DEPRECATED
#  endif
#endif

#ifndef LIBIDLJSON_INLINE_EXPORT
#  if __MINGW32__ && (!defined(__clang__) || !defined(libidljson_EXPORTS))
#    define LIBIDLJSON_INLINE_EXPORT
#  else
#    define LIBIDLJSON_INLINE_EXPORT LIBIDLJSON_EXPORT
#  endif
#endif

// Some internal functions are exported even though are not part of the API nor
// foreseen to ever be called by a user of the library (unlike some functions
// that are exported for convenience in building tools or even examples, such as
// the AVL tree).  One reason for this is that they are useful in instrumenting
// Cyclone DDS with some performance analysis tools, and it is in the interest
// of the projec that such analyses can be done.
//
// There is no guarantee that such internal symbols will remain available or
// that their role will be the same.
#ifndef LIBIDLJSON_EXPORT_INTERNAL_FUNCTION
#  define LIBIDLJSON_EXPORT_INTERNAL_FUNCTION LIBIDLJSON_EXPORT
#endif

#endif /* LIBIDLJSON_EXPORT_H */
