# Core Libraries

Kiota attempts to minimizing the amount of generated code to decrease processing time and reduce the binary footprint of the SDKs. In order to achieve this, we attempt to put as much code as a possible into the core library.

Kiota ships with a default set of core libraries, currently available for CSharp, Java and TypeScript.  These are simply default implementations. Replacing these core libraries with ones optimized for your scenarios is a completly supported scenario.

The core libraries takes care of all generic processing of HTTP requests. The service library that Kiota generates is designed to create a strongly typed layer over the core libraries to simplify the process of creating requests and consuming responses.

In order to implement a custom core library, it is necessary to implement the interfaces defined in the Kiota [Abstractions](kiotaabstractions) library.
