import {
  DateOnly,
  Duration,
  Parsable,
  ParsableFactory,
  ParseNode,
  TimeOnly,
  toFirstCharacterUpper,
} from "@microsoft/kiota-abstractions";

export class JsonParseNode implements ParseNode {
  /**
   *
   */
  constructor(private readonly _jsonNode: unknown) {}
  public onBeforeAssignFieldValues: ((value: Parsable) => void) | undefined;
  public onAfterAssignFieldValues: ((value: Parsable) => void) | undefined;
  public getStringValue = (): string => this._jsonNode as string;
  public getChildNode = (identifier: string): ParseNode =>
    new JsonParseNode((this._jsonNode as any)[identifier]);
  public getBooleanValue = (): boolean => this._jsonNode as boolean;
  public getNumberValue = (): number => this._jsonNode as number;
  public getGuidValue = (): string => this._jsonNode as string;
  public getDateValue = (): Date => this._jsonNode as Date;
  public getDateOnlyValue = () => DateOnly.parse(this.getStringValue());
  public getTimeOnlyValue = () => TimeOnly.parse(this.getStringValue());
  public getDurationValue = () => Duration.parse(this.getStringValue());
  public getCollectionOfPrimitiveValues = <T>(): T[] | undefined => {
    return (this._jsonNode as unknown[]).map((x) => {
      const currentParseNode = new JsonParseNode(x);
      const typeOfX = typeof x;
      if (typeOfX === "boolean") {
        return currentParseNode.getBooleanValue() as unknown as T;
      } else if (typeOfX === "string") {
        return currentParseNode.getStringValue() as unknown as T;
      } else if (typeOfX === "number") {
        return currentParseNode.getNumberValue() as unknown as T;
      } else if (x instanceof Date) {
        return currentParseNode.getDateValue() as unknown as T;
      } else if (x instanceof DateOnly) {
        return currentParseNode.getDateValue() as unknown as T;
      } else if (x instanceof TimeOnly) {
        return currentParseNode.getDateValue() as unknown as T;
      } else if (x instanceof Duration) {
        return currentParseNode.getDateValue() as unknown as T;
      } else {
        throw new Error(
          `encountered an unknown type during deserialization ${typeof x}`
        );
      }
    });
  };
  public getCollectionOfObjectValues = <T extends Parsable>(
    type: ParsableFactory<T>
  ): T[] | undefined => {
    return (this._jsonNode as unknown[])
      .map((x) => new JsonParseNode(x))
      .map((x) => x.getObjectValue<T>(type));
  };
  public getObjectValue = <T extends Parsable>(type: ParsableFactory<T>): T => {
    const result = type(this);
    this.onBeforeAssignFieldValues && this.onBeforeAssignFieldValues(result);
    this.assignFieldValues(result);
    this.onAfterAssignFieldValues && this.onAfterAssignFieldValues(result);
    return result;
  };
  public getEnumValues = <T>(type: any): T[] => {
    const rawValues = this.getStringValue();
    if (!rawValues) {
      return [];
    }
    return rawValues.split(",").map((x) => type[toFirstCharacterUpper(x)] as T);
  };
  public getEnumValue = <T>(type: any): T | undefined => {
    const values = this.getEnumValues(type);
    if (values.length > 0) {
      return values[0] as T;
    } else {
      return undefined;
    }
  };
  private assignFieldValues = <T extends Parsable>(item: T): void => {
    const fields = item.getFieldDeserializers();
    Object.entries(this._jsonNode as any).forEach(([k, v]) => {
      const deserializer = fields.get(k);
      if (deserializer) {
        deserializer(item, new JsonParseNode(v));
      } else {
        item.additionalData.set(k, v);
      }
    });
  };
}
