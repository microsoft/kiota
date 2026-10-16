import assert from 'assert';
import { ClientObjectProperties, getKiotaConfig, setKiotaConfig } from '@microsoft/kiota';
import * as fs from 'fs/promises';
import * as http from 'http';
import * as os from 'os';
import * as path from 'path';
import * as sinon from 'sinon';
import * as vscode from 'vscode';

import { OpenApiTreeProvider } from '../../../providers/openApiTreeProvider';
import { SharedService } from '../../../providers/sharedService';
import { getExtensionSettings } from '../../../types/extensionSettings';
import * as util from '../../../util';

suite('Workspace description paths', function () {
    this.timeout(10000);
    const sandbox = sinon.createSandbox();
    let workspaceRoot: string;
    let descriptionFile: string;
    let remoteDescription: string;
    let server: http.Server;
    let provider: OpenApiTreeProvider;
    const originalConfig = getKiotaConfig();

    suiteSetup(async () => {
        setKiotaConfig({ binaryLocation: path.dirname(path.dirname(require.resolve('@microsoft/kiota'))) });
        workspaceRoot = await fs.mkdtemp(path.join(os.tmpdir(), 'kiota-paths-'));
        descriptionFile = path.join(workspaceRoot, 'openapi.json');
        const itemPath = '/items';
        const successStatus = '200';
        const description = JSON.stringify({
            openapi: '3.0.3', info: { title: 'Workspace path regression', version: '1.0' },
            paths: { [itemPath]: { get: { responses: { [successStatus]: { description: 'OK' } } } } }
        });
        await fs.writeFile(descriptionFile, description);
        server = http.createServer((_request, response) => {
            response.setHeader('Content-Type', 'application/json');
            response.end(description);
        });
        await new Promise<void>(resolve => server.listen(0, '127.0.0.1', resolve));
        const address = server.address();
        assert(address && typeof address !== 'string');
        remoteDescription = `http://127.0.0.1:${address.port}/openapi.json`;
    });

    suiteTeardown(async () => {
        setKiotaConfig(originalConfig);
        await new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
        await fs.rm(workspaceRoot, { recursive: true, force: true });
    });

    setup(() => {
        sandbox.stub(util, 'getWorkspaceJsonDirectory').returns(workspaceRoot);
        provider = new OpenApiTreeProvider(
            {} as vscode.ExtensionContext,
            () => getExtensionSettings('kiota'),
            SharedService.getInstance()
        );
    });

    teardown(() => sandbox.restore());

    function client(descriptionLocation: string): ClientObjectProperties {
        return {
            descriptionLocation, includePatterns: [], excludePatterns: [],
            outputPath: './client', language: 'CSharp', clientNamespaceName: 'ApiSdk',
            structuredMimeTypes: [], usesBackingStore: false, includeAdditionalData: true,
            excludeBackwardCompatible: false, disabledValidationRules: []
        };
    }

    test('workspace selection ignores an empty description', async () => {
        await provider.loadEditPaths('ApiClient', client(''));
        assert.strictEqual(provider.descriptionUrl, '');
        assert.strictEqual(provider.apiTitle, undefined);
    });

    test('workspace file ignores an empty description', async () => {
        const workspaceFile = path.join(workspaceRoot, '.kiota', 'workspace.json');
        await fs.mkdir(path.dirname(workspaceFile), { recursive: true });
        await fs.writeFile(workspaceFile, JSON.stringify({
            version: '1.0.0', clients: { apiClient: client('') }, plugins: {}
        }));
        await provider.loadWorkspaceFile(workspaceFile);
        assert.strictEqual(provider.descriptionUrl, '');
        assert.strictEqual(provider.apiTitle, undefined);
    });

    for (const kind of ['relative', 'absolute', 'remote']) {
        function description(): string {
            return kind === 'relative' ? './openapi.json' : kind === 'absolute' ? descriptionFile : remoteDescription;
        }
        function assertLoaded(): void {
            assert.strictEqual(provider.apiTitle, 'Workspace path regression');
            assert.strictEqual(provider.descriptionUrl, kind === 'remote' ? remoteDescription : descriptionFile);
        }

        test(`workspace selection loads the ${kind} description`, async () => {
            await provider.loadEditPaths('ApiClient', client(description()));
            assertLoaded();
        });

        test(`workspace file loads the ${kind} description`, async () => {
            const workspaceFile = path.join(workspaceRoot, '.kiota', 'workspace.json');
            await fs.mkdir(path.dirname(workspaceFile), { recursive: true });
            await fs.writeFile(workspaceFile, JSON.stringify({
                version: '1.0.0', clients: { apiClient: client(description()) }, plugins: {}
            }));
            await provider.loadWorkspaceFile(workspaceFile);
            assertLoaded();
        });
    }
});
