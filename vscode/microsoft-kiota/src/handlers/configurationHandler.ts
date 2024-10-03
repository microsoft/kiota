import { GenerateState } from "../steps";

let configuration: Partial<GenerateState>;

export const getGenerationConfiguration = () => configuration;
export const setGenerationConfiguration = (config: Partial<GenerateState>) => {
  configuration = { ...configuration, ...config };
};