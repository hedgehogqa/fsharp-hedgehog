var path = require("path");
const CopyPlugin = require('copy-webpack-plugin');

module.exports = {
    entry: "./Hedgehog.Fable.Tests.fsproj",
    output: {
        path: path.join(__dirname, "./tests-js"),
        filename: "bundle.js",
    },
    devServer: {
        contentBase: "./tests-js",
        port: 8085,
    },
    module: {
        rules: [{
            test: /\.fs(x|proj)?$/,
            use: "fable-loader"
        }]
    },
    plugins: [
        new CopyPlugin([
            { from: 'index.html' }
        ])
    ]
}