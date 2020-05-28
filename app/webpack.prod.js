const merge = require('webpack-merge');
const config = require('./webpack.config')
var ZipPlugin = require('zip-webpack-plugin');

module.exports = merge(config, {
    mode: "production",
    plugins: [
        new ZipPlugin({
            filename: "dist.zip"
        })
    ]
});
