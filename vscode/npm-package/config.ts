export interface Config {
  binaryLocation: string;
  binaryVersion: string;
}

let config: Config = {
  binaryLocation: "",
  binaryVersion: ""
};

export function setKiotaConfig(newConfig: Partial<Config>) {
  config = { ...config, ...newConfig };
}

export function getKiotaConfig(): Config {
  return config;
}
