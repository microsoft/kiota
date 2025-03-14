import { ensureKiotaIsPresent } from '../install';
import { Config } from '@jest/types';

export default async (globalConfig: Config.GlobalConfig, projectConfig: Config.ProjectConfig) => {
    // Ensure kiota binary to optimize integration tests
    await ensureKiotaIsPresent();
};