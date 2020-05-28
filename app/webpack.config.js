const webpack = require('webpack');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const CopyPlugin = require('copy-webpack-plugin');
const path = require('path');

var entries = {};
entries['index'] = './src/index.ts';

var tsLoader = 'ts-loader?' + JSON.stringify({
    compilerOptions: {
        declaration: false
    }
});

module.exports = {
    entry: entries,
    mode: "production",
    plugins: [
        new webpack.NamedModulesPlugin(),
        new webpack.HotModuleReplacementPlugin(),
        new HtmlWebpackPlugin({
            filename: 'index.html',
            template: './src/index.html'
        }),
        new CopyPlugin({
            patterns: [
                { from: 'assets', to: 'assets' }
            ],
        })
    ],
    output: {
        filename: '[name].js',
        libraryTarget: 'umd',
        path: path.resolve(__dirname, 'dist')
    },
    module: {
        rules: [
            {
                test: /\.ts$/,
                use: [tsLoader]
            },
            {
                test: /\.component\.html$/,
                loader: "html-loader",
                options : {
                    preprocessor: (content, loaderContext) => {
                        return { default: content };
                    }
                }
            },
            {
                test: /\.html$/,
                use: ["html-loader"]
            },
            {
                test: /\.css$/i,
                use: ['style-loader', 'css-loader'],
            },
            {
                test: /\.(png|jpe?g|gif)$/i,
                use: [
                    {
                        loader: 'file-loader',
                    },
                ],
            }
        ]
    },
    resolve: {
        extensions: ['.ts', '.js']
    },
    devServer: {
        contentBase: "./dist",
        host: "localhost",
        publicPath: "/",
        port: 9001,
        hot: true,
        overlay: {
            warnings: true,
            errors: true
        }
    },
    performance: {
        hints: false
    }
};
