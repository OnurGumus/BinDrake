
const babelConfig = {
    presets: [
        ['@babel/preset-env', {
            modules: false,
            // This adds polyfills when needed. Requires core-js dependency.
            // See https://babeljs.io/docs/en/babel-preset-env#usebuiltins
            // Note that you still need to add custom polyfills if necessary (e.g. whatwg-fetch)
            useBuiltIns: 'usage',
            corejs: 3
        }]
    ]
};

module.exports = async ({ config, mode }) => {

    config.module.rules.push({
        test: /\.fs(x|proj)?$/,
        use: {
            loader: 'fable-loader',
            options: {
                babel: babelConfig,
                typedArrays: false
            }
        }
    });
    config.module.rules.push({
        test: /\.html$/,
        use: [
            {
                loader: "raw-loader"
            }
        ]
    });


    return config
}