// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;

namespace Microsoft.Kiota.Abstractions
{
    /// <summary>
    /// Model to represent only the date component of a DateTime
    /// </summary>
    public struct Time
    {
        /// <summary>
        /// Create a new Time from hours, minutes, and seconds.
        /// </summary>
        /// <param name="hour">The hour.</param>
        /// <param name="minute">The minute.</param>
        /// <param name="second">The second.</param>
        public Time(int hour, int minute, int second)
            : this(new DateTime(1, 1, 1, hour, minute, second))
        {
        }

        /// <summary>
        /// Create a new Time from a <see cref="DateTime"/> object
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> object to create the object from.</param>
        public Time(DateTime dateTime)
        {
            this.DateTime = dateTime;
        }

        /// <summary>
        /// The <see cref="DateTime"/> representation of the class
        /// </summary>
        public DateTime DateTime { get; }

        /// <summary>
        /// The hour.
        /// </summary>
        public int Hour
        {
            get
            {
                return this.DateTime.Hour;
            }
        }

        /// <summary>
        /// The minute.
        /// </summary>
        public int Minute
        {
            get
            {
                return this.DateTime.Minute;
            }
        }

        /// <summary>
        /// The second.
        /// </summary>
        public int Second
        {
            get
            {
                return this.DateTime.Second;
            }
        }

        /// <summary>
        /// The time of day, formatted as "HH:mm:ss".
        /// </summary>
        /// <returns>The string time of day.</returns>
        public override string ToString()
        {
            return this.DateTime.ToString("HH:mm:ss");
        }
    }
}
