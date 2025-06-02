const fs = require('fs');
const path = require('path');

const sourceFile = path.resolve(__dirname, '..', 'runtime.json'); // Move up one level from the `scripts` folder
const destDirs = [
  path.resolve(__dirname, '..', 'dist', 'esm'),
  path.resolve(__dirname, '..', 'dist', 'cjs'),
];

try {
  if (!fs.existsSync(sourceFile)) {
    throw new Error(`Source file does not exist: ${sourceFile}`);
  }

  destDirs.forEach((destDir) => {
    try {
      if (!fs.existsSync(destDir)) {
        fs.mkdirSync(destDir, { recursive: true });
      }

      const destFile = path.join(destDir, 'runtime.json');
      fs.copyFileSync(sourceFile, destFile);
      console.log(`Copied to ${destFile}`);
    } catch (err) {
      console.error(`Failed to copy to ${destDir}: ${err.message}`);
    }
  });
} catch (err) {
  console.error(`Error: ${err.message}`);
}