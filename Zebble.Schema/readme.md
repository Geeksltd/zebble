# Zebble Schema Tool

To install this tool, in `cmd` run the following:

```
C:\> dotnet tool install --global zebble-schema
```

## Update .zebble-schema.xml file
To reflect all changes you've made in your Zebble project to `.zebble-schema.xml` file, run the following command:

```
C:\Projects\> zebble-schema
```

### Note
When you execute it, to prevent MSBuild from being blocked, it will automatically start another instance of the tool with the following arguments:

```
C:\Projects\> zebble-schema --block
```