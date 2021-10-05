// ------------------------------------------------------------------------------
// Copyright (c) 2017 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sub-license, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ------------------------------------------------------------------------------

package com.microsoft.kiota.http.logger;

import javax.annotation.Nullable;
import javax.annotation.Nonnull;

/**
 * The logger for the service client
 */
public interface ILogger {

    /**
     * Sets the logging level of this logger
     *
     * @param level the level to log at
     */
    void setLoggingLevel(@Nonnull final LoggerLevel level);

    /**
     * Gets the logging level of this logger
     *
     * @return the level the logger is set to
     */
    @Nonnull
    LoggerLevel getLoggingLevel();

    /**
     * Log a debug message
     *
     * @param message the message
     */
    void logDebug(@Nonnull final String message);

    /**
     * Log an error message with throwable
     *
     * @param message   the message
     * @param throwable the throwable
     */
    void logError(@Nonnull final String message, @Nullable final Throwable throwable);
}

