<?php

/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */

namespace Microsoft\Kiota\Abstractions\Types;

use DateTime;
use Exception;

class Date {
    /**
     * @var string $value
     */
    private string $value;

    /**
     * @param string $dateString The date value in string format YYYY-MM-DD.
     * Y - Year
     * M - Month
     * D - Day
     * @throws Exception
     */
    public function __construct(string $dateString) {
        $this->value = (new DateTime($dateString))->format('Y-m-d');
    }

    /**
     * Creates a date object from a DateTime object
     * @param DateTime $dateTime
     * @return Date
     * @throws Exception
     */
    public static function createFromDateTime(DateTime $dateTime): Date {
        return new self($dateTime->format('Y-m-d'));
    }

    /**
     * Creates a new Date object from $year,$month and $day
     * @param int $year
     * @param int $month
     * @param int $day
     * @return Date
     * @throws Exception
     */
    public static function createFrom(int $year, int $month, int $day): Date {
        $date = new DateTime('1970-12-12T00:00:00Z');
        $date->setDate($year, $month, $day);
        return self::createFromDateTime($date);
    }

    /**
     * @return string
     */
    public function __toString() {
        return $this->value;
    }
}
