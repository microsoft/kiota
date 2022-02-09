// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;

namespace Microsoft.Kiota.Abstractions
{
    /// <summary>
    /// Model to represent only the date component of a DateTime
    /// </summary>
    public struct Date
    {
        /// <summary>
        /// Create a new Date object from a <see cref="DateTime"/> object
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> object to use</param>
        public Date(DateTime dateTime)
        {
            this.DateTime = dateTime;
        }

        /// <summary>
        /// Create a new Date object from a year, month, and day.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day of the month.</param>
        public Date(int year, int month, int day)
            : this(new DateTime(year, month, day))
        {
        }

        /// <summary>
        /// The DateTime object.
        /// </summary>
        public DateTime DateTime { get; }

        /// <summary>
        /// The date's year.
        /// </summary>
        public int Year
        {
            get
            {
                return this.DateTime.Year;
            }
        }

        /// <summary>
        /// The date's month.
        /// </summary>
        public int Month
        {
            get
            {
                return this.DateTime.Month;
            }
        }

        /// <summary>
        /// The date's day.
        /// </summary>
        public int Day
        {
            get
            {
                return this.DateTime.Day;
            }
        }

        /// <summary>
        /// Convert the date to a string.
        /// </summary>
        /// <returns>The string value of the date in the format "yyyy-MM-dd".</returns>
        public override string ToString()
        {
            return this.DateTime.ToString("yyyy-MM-dd");
        }
    }
}
