/**
 * Represents a date only. ISO 8601.
 */
export class DateOnly implements DateOnlyInterface {
    /**
     * Creates a new DateOnly from the given string.
     * @returns The new DateOnly
     * @throws An error if the year is invalid
     * @throws An error if the month is invalid
     * @throws An error if the day is invalid
     */
    public constructor(
        {year = 0,
        month = 1,
        day = 1} : Partial<DateOnlyInterface>
    ) {
        this.date = new Date(year, month - 1, day);
    }
    private date: Date;
    public get year(): number {
        return this.date.getFullYear();
    }
    public set year(value: number) {
        this.date.setFullYear(value);
    }
    public get month(): number {
        return this.date.getMonth() + 1;
    }
    public set month(value: number) {
        this.date.setMonth(value - 1);
    }
    public get day(): number {
        return this.date.getDate();
    }
    public set day(value: number) {
        this.date.setDate(value);
    }
    /**
     * Creates a new DateOnly from the given date.
     * @param date The date
     * @returns The new DateOnly
     * @throws An error if the date is invalid
     */
    public static fromDate(date: Date): DateOnly {
        if (!date)
            throw new Error("Date cannot be undefined");
        const result = new DateOnly({});
        result.date = new Date(date);
        return result;
    }
    /**
     * Parses a string into a DateOnly. The string can be of the ISO 8601 time only format or a number representing the ticks of a Date.
     * @param value The value to parse
     * @returns The parsed DateOnly.
     * @throws An error if the value is invalid
     */
     public static parse(value: string | undefined): DateOnly | undefined {
        if(!value || value.length === 0)
            return undefined;
        const ticks = Date.parse(value);
        if (isNaN(ticks)) {
            const exec = /^(?<year>\d{4,})-(?<month>0[1-9]|1[012])-(?<day>0[1-9]|[12]\d|3[01])$/gi.exec(value);
            if(exec) {
                const year = parseInt(exec.groups?.year ?? '');
                const month = parseInt(exec.groups?.month ?? '');
                const day = parseInt(exec.groups?.day ?? '');
                return new DateOnly({year, month, day});
            } else
                throw new Error("Value is not a valid date-only representation");
        } else {
            const date = new Date(ticks);
            return this.fromDate(date)
        }
    }
    /**
     *  Returns a string representation of the date in the format YYYY-MM-DD
     * @returns The date in the format YYYY-MM-DD ISO 8601
     */
    public toString(): string {
        return `${formatSegment(this.year, 4)}-${formatSegment(this.month)}-${formatSegment(this.day)}`;
    }
}
interface DateOnlyInterface {
    /**
     * The year
     * @default 0
     * @minium 0
     */
    year: number;
    /**
     * The month
     * @default 1
     * @minium 1
     * @maximum 12
     */
    month: number;
    /**
     * The day
     * @default 1
     * @minium 1
     * @maximum 31
     */
    day: number;
}
export function formatSegment(segment: number, digits: number = 2): string {
    return segment.toString().padStart(digits, "0");
}
