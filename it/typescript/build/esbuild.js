const argv = require('minimist')(process.argv.slice(2));

const esbuild = require('esbuild');
const fileReplacer = require('./file-replacer');

/** @type esbuild.BuildOptions */
const devConfig = {
  sourcemap: 'linked',
};

/** @type esbuild.BuildOptions */
const prodConfig = {
  minify: true,
};

/** @type esbuild.BuildOptions */
const config = {
  entryPoints: ['src/main.ts'],
  outfile: 'dist/main.js',
  bundle: true,
  platform: 'node',
  logLevel: 'info',

  define: {
    VERSION: JSON.stringify(process.env.npm_package_version),
    DEVELOP: JSON.stringify(!!argv.dev),
  },

  plugins: [
    fileReplacer(/[\\/]src[\\/]config[\\/]config.ts$/, (path) =>
      argv.dev ? path.replace('config.ts', 'config.dev.ts') : path,
    ),
  ],

  metafile: argv.meta,
  ...(argv.dev ? devConfig : prodConfig),
};

if (argv.run) config.plugins.push(require('@es-exec/esbuild-plugin-start').default({ script: 'node dist/main.js' }));

if (argv.watch) {
  (async () => {
    const ctx = await esbuild.context(config);
    await ctx.watch();
  })();
} else {
  esbuild.build(config).then((file) => {
    if (argv.meta) require('fs').writeFileSync('dist/meta.json', JSON.stringify(file.metafile));
  });
}
