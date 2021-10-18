# Zebble CSS Tool

To install this tool, in `cmd` run the following:

```
C:\> dotnet tool install --global zebble-css
```

## Compile CSS to C#
To generate C# code from CSS files of your Zebble project, run the following command:

```
C:\Projects\> zebble-css generate
```

## Watch for CSS changes constantly
To keep an eye on CSS files changes and immediately turning them to C# code, run the following command:

```
C:\Projects\> zebble-css watch
```

### Note
When you execute it with the watch argument, to prevent MSBuild from being blocked, it will automatically start another instance of the tool with the following arguments:

```
C:\Projects\> zebble-css watch --block
```