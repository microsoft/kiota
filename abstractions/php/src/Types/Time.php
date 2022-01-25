<?php

namespace Microsoft\Kiota\Abstractions\Types;

use DateTime;
use Exception;

/**
 * This object represents time in hours minutes and seconds
 */
class Time
{

    /**
     * The final string representation of the TimeOfDay
     * @var string $value
     */
    private string $value;

    /**
     * @param string $timeString The time value in string format HH:MM:SS
     * H - Hour
     * M - Minutes
     * S - Seconds
     * @throws Exception
     */
    public function __construct(string $timeString) {
        $this->value = (new DateTime($timeString))->format('H:i:s');
    }

    /**
     * Creates a TimeOfDay object from a DateTime object
     * @param DateTime $dateTime
     * @return self
     * @throws Exception
     */
    public static function createFromDateTime(DateTime $dateTime): self {
        return new self($dateTime->format('H:i:s'));
    }

    /**
     * Creates a new TimeOfDay object from $hour,$minute and $seconds
     * @param int $hour
     * @param int $minutes
     * @param int $seconds
     * @return self
     * @throws Exception
     */
    public static function createFrom(int $hour, int $minutes, int $seconds = 0): self {
        $date = new DateTime('now');
        $date->setTime($hour, $minutes, $seconds);
        return self::createFromDateTime($date);
    }

    /**
     * @return string
     */
    public function __toString() {
        return $this->value;
    }
}