# Chill with You 衣服控制插件

这是一个用于《Chill with You Lo-Fi Story》的 BepInEx 插件，用来控制角色当天穿的衣服。

## 功能

- 启动后按配置固定衣服，不再完全依赖每日随机。
- 游戏内显示衣服控制面板。
- 支持手动选择 `Default_1`、`Polo_1`、`Polo_2`、`Tee_1`、`Tee_2`。
- 支持随机一套并写入配置。
- 面板内显示日志，方便判断是否捕获到换装服务。
- 隐藏面板后保留左上角小按钮“衣装”，可重新展开。

## 前提

- Windows 版《Chill with You Lo-Fi Story》。
- BepInEx 5 x64 已安装到游戏根目录。
- .NET SDK 可用。

本仓库不包含游戏文件、Unity DLL、BepInEx 核心 DLL 或任何原游戏资源。

## 本地引用配置

复制示例配置：

```powershell
Copy-Item .\GameReferences.props.example .\GameReferences.props
```

如果你的游戏不在默认 Steam 路径，请编辑 `GameReferences.props`：

```xml
<GameRoot>F:\STEAM\steamapps\common\Chill with You Lo-Fi Story</GameRoot>
```

`GameReferences.props` 是本机私有文件，已被 `.gitignore` 忽略。

## 编译

```powershell
.\scripts\build.ps1
```

编译产物：

```text
src\Sherry.CostumeControl\bin\Release\net472\Sherry.CostumeControl.dll
```

## 安装

```powershell
.\scripts\install.ps1
```

或手动复制：

```powershell
Copy-Item .\src\Sherry.CostumeControl\bin\Release\net472\Sherry.CostumeControl.dll "<游戏目录>\BepInEx\plugins\" -Force
```

## 使用

启动游戏并进入房间主界面后，会出现 `Sherry 衣服控制` 面板。

可选衣服编号：

```text
Default_1 = 1
Polo_1 = 1001
Polo_2 = 1002
Tee_1 = 2001
Tee_2 = 2002
```

配置文件位置：

```text
<游戏目录>\BepInEx\config\Sherry.CostumeControl.json
```

## 卸载

删除：

```text
<游戏目录>\BepInEx\plugins\Sherry.CostumeControl.dll
<游戏目录>\BepInEx\config\Sherry.CostumeControl.json
```

如果还想完全移除 BepInEx，请删除游戏根目录中的：

```text
BepInEx
winhttp.dll
doorstop_config.ini
.doorstop_version
```

## 说明

本项目仅用于学习交流，不用于盈利或任何非法用途。请支持正版游戏，不要上传、分发或打包任何原游戏文件。

如果本项目存在侵权或不当内容，请联系 `1219323890@qq.com`，我会及时处理或删除。
