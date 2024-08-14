export abstract class Command {
  public toString():string {
    return this.constructor.name;
  } 

  abstract execute(args: unknown):void;
}