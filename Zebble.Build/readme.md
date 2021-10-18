# Zebble Build Tool

To install this tool, in `cmd` run the following:

```
C:\> dotnet tool install --global zebble-build
```

## Create a new Zebble project

To create a new Zebble project, run the following command:

```
C:\Projects\> zebble-build new --name MyZebbleApp1 
```

At this point, the template repository will be downloaded [from here](https://github.com/Geeksltd/Zebble.Template/tree/main/Template), and the placeholders will be replaced with the name provided in the `--name` parameter. 

### Optional parameters:

- `--template-repo ...` specifies the repository to use as template. The default value is *https://github.com/Geeksltd/Zebble.Template*
- `--template-name ...` specifies the directory name inside the repo. The default value is `Template`
- `--log` shows log messages. Use this for troubleshooting only.

## Upgrade existing projects

To upgrade your existing Zebble project, run the following command:

```
C:\Projects\> zebble-build upgrade 
```

## Update a Zebble plugin

To update your Zebble plugin, run the following command:

```
C:\Projects\> zebble-build update-plugin
```

### Optional parameters:
- `--increase-version` specifies whether the plugin version should be increased. The default value is *false*
- `--configuration` specifies which configuration should be used. The default value is *Release*
- `--publish` specifies the updated plugin should be published. The default value is *false*
- `--source ...` specifies the source you want to publish your plugin. The default value is *https://api.nuget.org/v3/index.json*
- `--api-key ...` specifies the API key to be used for publishing. There is no default value and you need to specify this if you want to publish.
- `--commit` specifies whether to commit and push the increased version to the repo or not. This flag will be considered only if you specify `--increase-version` and `--publish` flags. The default value is *false*

## Convert a legacy Zebble plugin to the TFM style

To convert an existing Zebble plugin, run the following command:

```
C:\Projects\> zebble-build convert-plugin
```

At this point, the template repository will be downloaded [from here](https://github.com/Geeksltd/Zebble.Template/tree/main/Plugin), and all placeholders will be replaced with the name of the plugin. The plugin name, version and description will be gathered automatically by scanning `.nuspec` file. 

### Optional parameters:
- `--template-repo ...` specifies the repository to use as template. The default value is *https://github.com/Geeksltd/Zebble.Template*
- `--template-name ...` specifies the directory name inside the repo. The default value is `Plugin`
- `--log` shows log messages. Use this for troubleshooting only.
