import { Config } from '@jest/types';
import { ensureKiotaIsPresent } from '../../install';

export default async (globalConfig: Config.GlobalConfig, projectConfig: Config.ProjectConfig) => {
    // Ensure kiota binary to optimize integration tests
    await ensureKiotaIsPresent();
};