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

import java.util.Objects;
import java.util.logging.Logger;

import javax.annotation.Nullable;
import javax.annotation.Nonnull;

/**
 * The default logger for the service client
 */
public class DefaultLogger implements ILogger {

    /**
     * The logging level
     */
    private LoggerLevel level = LoggerLevel.ERROR;

    private final static Logger LOGGER = Logger.getLogger(Logger.GLOBAL_LOGGER_NAME);

    /**
     * Sets the logging level of this logger
     *
     * @param level the level to log at
     */
    public void setLoggingLevel(@Nonnull final LoggerLevel level) {
        LOGGER.info("Setting logging level to " + level);
        this.level = Objects.requireNonNull(level, "parameter level cannot be null");
    }

    /**
     * Gets the logging level of this logger
     *
     * @return the level the logger is set to
     */
    @Nonnull
    public LoggerLevel getLoggingLevel() {
        return level;
    }

    /**
     * Creates the tag automatically
     *
     * @return the tag for the current method
     * Sourced from https://gist.github.com/eefret/a9c7ac052854a10a8936
     */
    @Nullable
    private String getTag() {
        try {
            final StringBuilder sb = new StringBuilder();
            final int callerStackDepth = 4;
            final String className = Thread.currentThread().getStackTrace()[callerStackDepth].getClassName();
            sb.append(className.substring(className.lastIndexOf('.') + 1));
            sb.append("[");
            sb.append(Thread.currentThread().getStackTrace()[callerStackDepth].getMethodName());
            sb.append("] - ");
            sb.append(Thread.currentThread().getStackTrace()[callerStackDepth].getLineNumber());
            return sb.toString();
        } catch (final Exception ex) {
            LOGGER.warning(ex.getMessage());
        }
        return null;
    }

    /**
     * Logs a debug message
     *
     * @param message the message
     */
    @Override
    public void logDebug(@Nonnull final String message) {
        Objects.requireNonNull(message, "parameter message cannot be null");
        if(this.level == LoggerLevel.DEBUG)
            for (final String line : message.split("\n")) {
                LOGGER.info(line);
            }

    }

    /**
     * Logs an error message with throwable
     *
     * @param message   the message
     * @param throwable the throwable
     */
    @Override
    public void logError(@Nonnull final String message, @Nonnull final Throwable throwable) {
        Objects.requireNonNull(message, "parameter message cannot be null");
        Objects.requireNonNull(throwable, "parameter throwable cannot be null");
        switch (level) {
            case DEBUG:
            case ERROR:
            default:
                for (final String line : message.split("\n")) {
                    LOGGER.severe(getTag() + line);
                }
                LOGGER.severe("Throwable detail: " + throwable);
        }
    }
}
