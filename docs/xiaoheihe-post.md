# 《Chill with You》衣服固定/手动切换插件分享

做了一个小插件，用来控制角色每天穿的衣服。原游戏会按日期/权重随机换装，这个插件可以固定衣服，也可以在游戏里手动切换。

## 功能

- 固定指定衣服，不再每天随机。
- 游戏内显示衣服控制面板。
- 支持选择 `Default_1`、`Polo_1`、`Polo_2`、`Tee_1`、`Tee_2`。
- 支持随机一套。
- 面板里带日志，方便看有没有捕获到换装服务。
- 面板可以隐藏，隐藏后左上角保留“衣装”按钮，点一下重新展开。

## 前提

- Windows Steam 版《Chill with You Lo-Fi Story》。
- 已安装 BepInEx 5 x64。

## 安装

把编译好的：

```text
Sherry.CostumeControl.dll
```

放到：

```text
<游戏目录>\BepInEx\plugins\
```

启动游戏即可。

## 可选衣服

```text
Default_1 = 1
Polo_1 = 1001
Polo_2 = 1002
Tee_1 = 2001
Tee_2 = 2002
```

## 配置文件

首次启动后会生成：

```text
<游戏目录>\BepInEx\config\Sherry.CostumeControl.json
```

可以手动改 `skinType`。

## 卸载

删除：

```text
<游戏目录>\BepInEx\plugins\Sherry.CostumeControl.dll
<游戏目录>\BepInEx\config\Sherry.CostumeControl.json
```

## 注意

插件不包含任何游戏资源或游戏 DLL。请支持正版游戏。

本插件仅用于学习交流，不用于盈利或任何非法用途。如果存在侵权或不当内容，请联系 `1219323890@qq.com`，我会及时处理或删除。
