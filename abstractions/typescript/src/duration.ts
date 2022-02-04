import {
  parse as parseDuration,
  serialize as serializeDuration,
} from "tinyduration";
/**
 * Represents a duration value. ISO 8601.
 */
export class Duration implements DurationInterface {
  /**
   * Creates a new Duration value from the given parameters.
   * @returns The new Duration
   * @throws An error if years is invalid
   * @throws An error if months is invalid
   * @throws An error if weeks is invalid
   * @throws An error if days is invalid
   * @throws An error if hours is invalid
   * @throws An error if minutes is invalid
   * @throws An error if seconds is invalid
   * @throws An error if weeks is used in combination with years or months
   */
  public constructor({
    years = 0,
    months = 0,
    weeks = 0,
    days = 0,
    hours = 0,
    minutes = 0,
    seconds = 0,
    negative = false,
  }: Partial<DurationInterface>) {
    if (years < 0 || years > 9999) {
      throw new Error("Year must be between 0 and 9999");
    }
    if (months < 0 || months > 11) {
      throw new Error("Month must be between 0 and 11");
    }
    if (weeks < 0 || weeks > 53) {
      throw new Error("Week must be between 0 and 53");
    }
    if (days < 0 || days > 6) {
      throw new Error("Day must be between 0 and 6");
    }
    if (hours < 0 || hours > 23) {
      throw new Error("Hour must be between 0 and 23");
    }
    if (minutes < 0 || minutes > 59) {
      throw new Error("Minute must be between 0 and 59");
    }
    if (seconds < 0 || seconds > 59) {
      throw new Error("Second must be between 0 and 59");
    }
    if ((years > 0 || months > 0) && weeks > 0) {
      throw new Error("Cannot have weeks and months or weeks and years");
    }
    this.years = years;
    this.months = months;
    this.weeks = weeks;
    this.days = days;
    this.hours = hours;
    this.minutes = minutes;
    this.seconds = seconds;
    this.negative = negative;
  }
  public years: number;
  public months: number;
  public weeks: number;
  public days: number;
  public hours: number;
  public minutes: number;
  public seconds: number;
  public negative: boolean;
  /**
   * Parses a string into a Duration. The string can be of the ISO 8601 duration format.
   * @param value The value to parse
   * @returns The parsed Duration.
   * @throws An error if the value is invalid
   */
  public static parse(value: string | undefined): Duration | undefined {
    if (!value || value.length === 0) {
      return undefined;
    }
    const duration = parseDuration(value);
    return new Duration({
      years: duration.years ?? 0,
      months: duration.months ?? 0,
      weeks: duration.weeks ?? 0,
      days: duration.days ?? 0,
      hours: duration.hours ?? 0,
      minutes: duration.minutes ?? 0,
      seconds: duration.seconds ?? 0,
      negative: duration.negative ?? false,
    });
  }
  /**
   * Serializes the duration to a string in the ISO 8601 duration format.
   * @returns The serialized duration.
   */
  public toString(): string {
    return serializeDuration(this);
  }
}

interface DurationInterface {
  /**
   * Years of the duration
   * @default 0
   */
  years: number;
  /**
   * Months of the duration
   * @default 0
   */
  months: number;
  /**
   * Weeks of the duration, can't be used together with years or months
   * @default 0
   */
  weeks: number;
  /**
   * Days of the duration
   * @default 0
   */
  days: number;
  /**
   * Hours of the duration
   * @default 0
   */
  hours: number;
  /**
   * Minutes of the duration
   * @default 0
   */
  minutes: number;
  /**
   * Seconds of the duration
   * @default 0
   */
  seconds: number;
  /**
   * Whether the duration is negative
   * @default false
   */
  negative: boolean;
}
