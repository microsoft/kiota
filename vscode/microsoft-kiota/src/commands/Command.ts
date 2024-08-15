export abstract class Command {
  public abstract toString(): string;

  abstract execute(args: unknown): Promise<void> | void;
}