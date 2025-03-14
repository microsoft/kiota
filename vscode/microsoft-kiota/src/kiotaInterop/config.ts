export interface Config {
  binaryLocation: string;
}

let config: Config = {
  binaryLocation: ""
};

export function setKiotaConfig(newConfig: Partial<Config>) {
  config = { ...config, ...newConfig };
}

export function getKiotaConfig(): Config {
  return config;
}
