const path = require("node:path");
const [workingDirectory, nodeModules] = process.argv.slice(2);
require(path.join(nodeModules, "esbuild")).buildSync({
  entryPoints: [path.join(workingDirectory, "serialization.ts")],
  outfile: path.join(workingDirectory, "serialization.cjs"),
  tsconfig: path.join(workingDirectory, "tsconfig.json"),
  bundle: true,
  platform: "node",
  format: "cjs",
  nodePaths: [nodeModules],
});
