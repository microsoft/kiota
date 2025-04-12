if (process.env.SKIP_POSTINSTALL) {
    process.exit(0); // Successfully exits the script
}

const { getKiotaVersion } = require('../dist/cjs/lib/getKiotaVersion.js');

(async () => {
    try {
        const result = await getKiotaVersion();
        console.log(`Kiota version ${result} installed successfully!`);
    } catch (error) {
        console.error("An error occurred while bootstrapping Kiota.", error);
    }
})();