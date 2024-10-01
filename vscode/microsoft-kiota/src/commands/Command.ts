export abstract class Command {
  public abstract getName(): string;

  abstract execute(args: unknown): Promise<void> | void;
}