# XIVConfigUI

XIVConfigUI is a ui, localization, config library in Imgui designed to work within Dlamaud Plugins.

## Getting Started

Add XIVConfigUI as a submodule to your project:

```shell
git submodule add https://github.com/RianMorningstar/XIVConfigUI
```

Add it to your plugin's CSProj file:

```xml
<ItemGroup>
	<ProjectReference Include="..\XIVConfigUI\XIVConfigUI\XIVConfigUI.csproj" />
</ItemGroup>
```

Then, in the entry point of your plugin:

```c#
XIVConfigUIMain.Init(pluginInterface, "%NAME%");
```

where pluginInterface is a **DalamudPluginInterface**.

Don't forget to **dispose** it!

## Usage

