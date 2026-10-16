import assert from 'assert';
import * as vscode from 'vscode';

suite('Command contributions', () => {
    test('does not advertise the retired lock search command', () => {
        const extension = vscode.extensions.getExtension('ms-graph.kiota');
        assert(extension);
        const commands = extension.packageJSON.contributes.commands as { command: string }[];
        assert(!commands.some(command => command.command === 'kiota.searchLock'));
        assert(commands.some(command => command.command === 'kiota.migrateFromLockFile'));
    });
});
