export abstract class Command {
  public abstract getName(): string;

  public abstract execute(args: unknown): Promise<void>;
}