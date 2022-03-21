/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */


/**
 * Maintain and export `dom.shim.d.ts` containing empty interfaces. This way the user can compile their code with the library without having a DOM library
 * Read more {@link docs/design/isomorphic.md}
 * This file should be shipped in the package
 * */


interface Request {}
interface RequestInit {}
interface Response {}
interface Headers {}
interface ReadableStream {}
interface fetch {}
interface Blob {}
