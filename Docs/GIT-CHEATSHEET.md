# Git 速查表

> 三层文档体系的旁支：**只解决"手抖了怎么办"，不解释原理**。
> 规则用一句话记：**拿不准就 `git status`，丢东西就 `git reflog`，公共分支只用 `revert`。**

**本项目远程：** `origin` → `github.com/yririkowj-cmd/unity-demo`，主分支 `main`

---

## 0. 三句话总纲

1. **不确定就先 `git status`** —— 它会告诉你现在在哪个分支、改了哪些文件。
2. **公共分支（main）永不 `reset` + 强推**，要撤销就用 `revert`。
3. **提交丢了不要慌，`git reflog` 30 天内都能找回。**

---

## 1. 日常循环（照抄即可）

```bash
git switch main && git pull          # 1. 回到主干，拉最新
git switch -c feature/登录           # 2. 开分支干活（名字用英文更省事）
# ...改代码...
git status                           # 3. 看改了啥
git add -A && git commit -m "feat: 登录页骨架"   # 4. 提交
git push -u origin feature/登录       # 5. 首推，建立跟踪
# 6. GitHub 上开 PR → 合并 → 删分支
git switch main && git pull          # 7. 回到最新主干
```

**提交信息前缀：** `feat:` 新功能 · `fix:` 修 bug · `refactor:` 重构 · `docs:` 文档 · `chore:` 杂项

---

## 2. 场景速查（最常用的一张表）

| 我想… | 命令 |
|---|---|
| 看现在改了哪些文件 | `git status` |
| 看我到底改了什么内容 | `git diff` |
| 看已暂存的改动 | `git diff --staged` |
| 把这一个文件加进下次提交 | `git add 路径/文件.cs` |
| 全部暂存（含删除） | `git add -A` |
| 提交 | `git commit -m "feat: 说明"` |
| 提交并跳过 add（已跟踪文件） | `git commit -am "fix: 说明"` |
| 看历史（带分支图） | `git log --oneline --graph --decorate -20` |
| 看某一行是谁改的 | `git blame 路径/文件.cs` |
| 临时把改动收起来 | `git stash push -m "说明"` |
| 把收起来的改动拿回来 | `git stash pop` |
| 看收了几个 | `git stash list` |

---

## 3. 分支

```bash
git branch                    # 本地分支列表（* 是当前）
git branch -a                 # 含远程分支
git switch -c feature/xxx     # 新建并切过去
git switch main               # 切回主干
git branch -d feature/xxx     # 删除已合并的分支
git branch -D feature/xxx     # 强制删除（没合并也删）
git push origin --delete feature/xxx   # 删远程分支
```

> 切分支前必须 `git status` 是 clean。有未提交改动时 `git switch` 会拒绝或把改动带过去，容易乱。

---

## 4. 合并：四种方式

| 方式 | 命令 / GitHub 选项 | 效果 | 用在哪 |
|---|---|---|---|
| 快进 | `git merge feature` | 无合并提交，直线历史 | 个人小改动 |
| 保留合并点 | `git merge --no-ff feature` | 多一个 merge commit | 想知道"这是一整块功能" |
| **压缩** | GitHub **Squash and merge** | N 个碎提交压成 1 个 | **团队主干首选** |
| 变基 | `git rebase main` | 把自己的提交搬到 main 最新处 | 开 PR 前整理自己的分支 |

**推荐组合：** 自己的分支先 `git rebase main` 理干净 → 开 PR → Squash merge。

### 合并前先同步主干

```bash
git switch feature/xxx
git fetch origin
git rebase origin/main        # 有冲突就在这里解决，比在 PR 里解更可控
```

---

## 5. 冲突：五步走

```bash
git merge feature/xxx         # 或 git rebase / git pull，提示 CONFLICT
git status                    # 列出 both modified 的文件
# 打开文件，会看到：
#   <<<<<<< HEAD
#   你当前分支的版本
#   =======
#   要合进来的版本
#   >>>>>>> feature/xxx
# 手工改成最终想要的样子，并把这三行标记本身删掉
git add 解决好的文件
git merge --continue          # rebase 时用 git rebase --continue
```

**想放弃这次合并：**

```bash
git merge --abort             # rebase 时用 git rebase --abort
```

> 冲突标记里的两段**都要看完**再决定，很多时候正确答案是"两边都保留"，而不是二选一。

---

## 6. 回滚：先判断三件事

**① 提交了吗？ ② 推送了吗？ ③ 这分支只有我吗？**

| 情况 | 命令 | 危险度 | 说明 |
|---|---|---|---|
| 改了没提交，想丢掉 | `git restore 文件` | 低 | 不可恢复，确认后再敲 |
| 全部丢弃未提交改动 | `git restore .` | 低 | 同上 |
| add 了想撤回暂存 | `git restore --staged 文件` | 低 | 改动还在 |
| 提交了，想改提交信息/补文件 | `git commit --amend` | 低 | **仅未推送时** |
| 撤销最近一次提交，**保留**改动 | `git reset --soft HEAD~1` | 低 | 改动回到暂存区 |
| 撤销最近一次提交，**丢弃**改动 | `git reset --hard HEAD~1` | **高** | 改动真的没了 |
| 撤销已推送的公共提交 | `git revert <提交ID>` | 低 | 新增反向提交，历史不改写 |
| 自己的分支改了历史要强推 | `git push --force-with-lease` | **高** | 别用 `--force` |
| 提交丢了 / reset 搞砸 | `git reflog` → `git reset --hard <ID>` | 救 | 见第 7 节 |

**规则：**

- **main 上只能 `revert`。** 强推 main 会让所有人的本地历史对不上。
- 强推必须用 `--force-with-lease`：别人在你之后推过东西时它会拒绝，保护你不覆盖别人的工作。
- 破坏性命令（`reset --hard`、`clean -fd`、`push -f`）敲之前，先 `git stash` 或开个临时分支兜底。

### 撤销一个合并提交（特殊情况）

```bash
git revert -m 1 <合并提交ID>     # -m 1 = 保留第一父提交（通常是 main 那条线）
```

> 坑：回滚后那个分支**再也合不进来**（Git 认为已合并过），要重新合并得先 revert 那个 revert。

---

## 7. 后悔药：reflog

Git 几乎不真删提交，HEAD 每次移动都有记录，默认保留 30 天。

```bash
git reflog                    # 看 HEAD 的移动历史
git reset --hard HEAD@{2}     # 回到两步之前
git reset --hard 4e46ef1      # 或直接给提交 ID
```

**误删了分支：**

```bash
git reflog                    # 找到那条分支最后的提交 ID
git branch 分支名 <ID>         # 重建
```

**误提交了不该提交的大文件（还没推送）：**

```bash
git reset --soft HEAD~1
git restore --staged 大文件
rm 大文件                     # 或移出目录
git commit -c ORIG_HEAD
```

---

## 8. GitHub 网页端对应操作

| 想做的事 | 在哪点 |
|---|---|
| 开 PR | push 后首页会出现 `Compare & pull request` |
| 选合并方式 | 绿色合并按钮**右边的下拉箭头** |
| 撤销已合并的 PR | PR 页面右下 `Revert`，会自动开一个反向 PR |
| 删掉合并后的分支 | PR 合并成功后出现的 `Delete branch` |
| 禁止直接推 main | Settings → Branches → 加规则，勾 `Require a pull request before merging` |
| 看某个文件的修改历史 | 文件页右上 `History` |
| 下载某一版代码 | 仓库首页 `Code` → `Download ZIP`，或切到某个 tag |

---

## 9. Unity 项目专属注意

- **`.meta` 必须跟资源一起提交**，漏了别人的 Unity 里引用会全丢。本仓库已用 `Visible Meta Files` 模式，这是对的。
- **`.unity` 场景和 `.prefab` 是 YAML 文本**，能合并但冲突极难手工解决。避免两人同时改同一场景；分工上"一人管场景，其他人只改脚本"。
- **`Library/`、`Temp/`、`Logs/`、`obj/`、`.vs/`、`*.csproj`、`*.sln` 都不要提交**，`.gitignore` 已经处理好，别用 `git add -f` 强行加。
- 合并后如果 Unity 报引用丢失，先确认冲突文件里没有残留的 `<<<<<<<` 标记：`git diff` 或全局搜索这三个字符。

---

## 10. 出事了按这个顺序排查

```
1. git status                 → 我在哪个分支？改了啥？有没有正在进行的合并？
2. git log --oneline -5       → 现在在哪一步
3. git reflog                 → 上个好状态是什么
4. git stash                  → 先把手头改动收起来，保证干净
5. 还不行 → 别继续敲命令，先备份整个目录再研究
```

**永远不要做的事：**

- ❌ `git push --force` 到 main
- ❌ 在冲突没解决时 `git commit`（会提交带 `<<<<<<<` 的代码）
- ❌ 用 `git checkout .` 代替 `git status` 看一眼（会静默丢改动）
- ❌ 删除 `.git` 目录来"修复"问题（历史全没）
- ❌ 在 `Library/` 里手动改东西然后提交

---

## 11. 一分钟自检（每次收工前）

```bash
git status                 # 干净吗？
git log --oneline -3       # 提交信息看得懂吗？
git switch main && git pull   # 主干是最新的吗？
```

三个都过，今天就收工。

---

*最后更新：2026-11-11 · 遇到表里没有的情况，把 `git status` 和 `git log --oneline -5` 的输出贴出来再说。*
