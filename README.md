cacheout
========

Cacheout is a user-mode memory cache that is local to a single application. Cacheout uses the managed injection techniques from Nightmare to hook file system calls in a target x86 process and redirect them to managed handlers, allowing for arbitrary modification of an application's view of the underlying file system.
