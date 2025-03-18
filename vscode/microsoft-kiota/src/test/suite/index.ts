import { runCLI } from 'jest';
import * as path from 'path';

export async function run(): Promise<void> {
    const testsRoot = path.resolve(__dirname, '..');

    try {
        const result = await runCLI({
            config: path.resolve(__dirname, '../../../jest.config.js'),
            _: [],
            $0: ''
        }, [testsRoot]);

        if (result.results.numFailedTests > 0) {
            throw new Error(`${result.results.numFailedTests} tests failed.`);
        }
    } catch (err) {
        console.error(err);
    }
}