export interface Config {
  binaryLocation: string;
}

let config: Config = {
  binaryLocation: ""
};

export function setConfig(newConfig: Partial<Config>) {
  config = { ...config, ...newConfig };
}

export function getConfig(): Config {
  return config;
}
