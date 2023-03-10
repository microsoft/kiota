const { readFile } = require('fs').promises;

const fileReplacer = (pattern, transformer) => ({
  name: 'fileReplacer',
  setup(build) {
    build.onLoad({ filter: pattern }, async (args) => ({
      contents: await readFile(transformer(args.path), 'utf8'),
      loader: 'default',
    }));
  },
});

module.exports = fileReplacer;
