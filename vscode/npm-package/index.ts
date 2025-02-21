const { generateClient } = require('./lib/generateClient');
const { generatePlugin } = require('./lib/generatePlugin');
const { getKiotaTree } = require('./lib/getKiotaTree');
const { getKiotaVersion } = require('./lib/getKiotaVersion');
const { getManifestDetails } = require('./lib/getManifestDetails');
const { getLanguageInformationForDescription, getLanguageInformationInternal } = require('./lib/languageInformation');
const { migrateFromLockFile } = require('./lib/migrateFromLockFile');
const { removeClient, removePlugin } = require('./lib/removeItem');
const { searchDescription } = require('./lib/searchDescription');
const { updateClients } = require('./lib/updateClients');

export {
    generateClient,
    generatePlugin, getKiotaTree, getKiotaVersion,
    getLanguageInformationForDescription,
    getLanguageInformationInternal,
    getManifestDetails,
    migrateFromLockFile,
    removeClient,
    removePlugin,
    searchDescription, updateClients
};

export * from './types';
export * from './utils';
