import {
  DateOnly,
  Duration,
  Parsable,
  ParsableFactory,
  ParseNode,
  TimeOnly,
  toFirstCharacterUpper,
} from "@microsoft/kiota-abstractions";

export class TextParseNode implements ParseNode {
  private static noStructuredDataMessage =
    "text does not support structured data";
  /**
   *
   */
  constructor(private readonly text: string) {
    this.text = this.text.startsWith('"') ? this.text.substring(1) : this.text;
    this.text = this.text.endsWith('"')
      ? this.text.substring(0, this.text.length - 2)
      : this.text;
  }
  public onBeforeAssignFieldValues: ((value: Parsable) => void) | undefined;
  public onAfterAssignFieldValues: ((value: Parsable) => void) | undefined;
  public getStringValue = (): string => this.text;
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  public getChildNode = (identifier: string): ParseNode => {
    throw new Error(TextParseNode.noStructuredDataMessage);
  };
  public getBooleanValue = (): boolean =>
    (this.text && this.text.toLowerCase() === "true") || this.text === "1";
  public getNumberValue = (): number => Number(this.text);
  public getGuidValue = (): string => this.text;
  public getDateValue = (): Date => new Date(Date.parse(this.text));
  public getDateOnlyValue = () => DateOnly.parse(this.getStringValue());
  public getTimeOnlyValue = () => TimeOnly.parse(this.getStringValue());
  public getDurationValue = () => Duration.parse(this.getStringValue());
  public getCollectionOfPrimitiveValues = <T>(): T[] | undefined => {
    throw new Error(TextParseNode.noStructuredDataMessage);
  };
  public getCollectionOfObjectValues = <T extends Parsable>(
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    type: ParsableFactory<T>
  ): T[] | undefined => {
    throw new Error(TextParseNode.noStructuredDataMessage);
  };
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  public getObjectValue = <T extends Parsable>(type: ParsableFactory<T>): T => {
    throw new Error(TextParseNode.noStructuredDataMessage);
  };
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  public getEnumValues = <T>(type: any): T[] => {
    throw new Error(TextParseNode.noStructuredDataMessage);
  };
  public getEnumValue = <T>(type: any): T | undefined => {
    return type[toFirstCharacterUpper(this.text)] as T;
  };
}
