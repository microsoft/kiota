
export interface SharedState {
  clientOrPluginKey?: string;
}

export class SharedService {
  private static instance: SharedService;
  private state: Map<keyof SharedState, any> = new Map();

  private constructor() { }

  public static getInstance(): SharedService {
    if (!SharedService.instance) {
      SharedService.instance = new SharedService();
    }
    return SharedService.instance;
  }

  public get<K extends keyof SharedState>(key: K): SharedState[K] | undefined {
    return this.state.get(key);
  }

  public set<K extends keyof SharedState>(key: K, value: SharedState[K]): void {
    this.state.set(key, value);
  }

  public clear<K extends keyof SharedState>(key: K): void {
    this.state.delete(key);
  }

  // Method to reset the singleton instance for testing
  public static resetInstance(): void {
    SharedService.instance = new SharedService();
  }
}