#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""一键诊断：为什么推不上去。

依次检查 环境 / 网络 / token 权限，最后给出明确结论。
不推送任何东西，只做检测。
"""

import glob
import json
import os
import shutil
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request

OWNER = "HZW3210022"
REPO = "DefectClassic-StS2"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
API = "https://api.github.com"

OK, BAD, WARN = "[OK]  ", "[!!]  ", "[..]  "


def line(text=""):
    print(text, flush=True)


def find_git():
    """Same lookup as push_via_api.py: cmd.exe here has no git on PATH."""
    exe = shutil.which("git")
    if exe:
        return exe
    home = os.path.expanduser("~")
    local = os.environ.get("LOCALAPPDATA", os.path.join(home, "AppData", "Local"))
    patterns = [
        os.path.join(home, ".workbuddy", "binaries", "PortableGit",
                     "versions", "*", "cmd", "git.exe"),
        os.path.join(home, ".workbuddy", "binaries", "PortableGit",
                     "versions", "*", "mingw64", "bin", "git.exe"),
        os.path.join(local, "Programs", "Git", "cmd", "git.exe"),
        r"C:\Program Files\Git\cmd\git.exe",
        r"C:\Program Files (x86)\Git\cmd\git.exe",
    ]
    for pat in patterns:
        for hit in sorted(glob.glob(pat), reverse=True):
            if os.path.isfile(hit):
                return hit
    return None


GIT = find_git()


def git(*args):
    if not GIT:
        return ""
    try:
        return subprocess.run([GIT] + list(args), cwd=ROOT, capture_output=True,
                              text=True, encoding="utf-8", timeout=30).stdout.strip()
    except Exception:
        return ""


def check_env():
    line("[1] 运行环境")
    line("    Python      %s" % sys.version.split()[0])
    line("    工作目录    %s" % ROOT)
    line("    git 程序    %s" % (GIT if GIT else "找不到！（PATH 里没有，常见位置也没有）"))
    inrepo = os.path.isdir(os.path.join(ROOT, ".git"))
    line("    git 仓库    %s" % ("是" if inrepo else "否 —— 目录不对！"))
    if inrepo:
        line("    本地 HEAD   %s" % git("log", "-1", "--format=%h %s"))
        pending = git("diff", "--name-only", "cccd7b9", "HEAD").splitlines()
        line("    待推送      %d 个文件" % len(pending) if pending else "    待推送      0（已是最新）")
    line()
    return inrepo


def check_network():
    line("[2] 网络")
    result = {}
    for host in ["github.com", "api.github.com"]:
        t0 = time.time()
        try:
            s = socket.create_connection((host, 443), timeout=12)
            s.close()
            result[host] = True
            line("    连 %-18s %s (%.1fs)" % (host, "通", time.time() - t0))
        except Exception as e:
            result[host] = False
            line("    连 %-18s %s %s" % (host, "不通", type(e).__name__))
    line()
    return result.get("api.github.com", False)


def call(path, token, method="GET", payload=None):
    data = json.dumps(payload).encode() if payload is not None else None
    req = urllib.request.Request(API + path, data=data, method=method, headers={
        "Authorization": "Bearer " + token,
        "Accept": "application/vnd.github+json",
        "Content-Type": "application/json",
        "User-Agent": "DefectClassic-diagnose",
    })
    try:
        with urllib.request.urlopen(req, timeout=45) as r:
            return r.status, json.loads(r.read().decode() or "{}")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")[:300]
    except Exception as e:
        return None, "%s: %s" % (type(e).__name__, e)


def check_token():
    line("[3] Token")
    line("    在这里粘贴 token 后回车（输入不会显示，也不会存到任何文件）")
    try:
        token = input("    token: ").strip()
    except (EOFError, KeyboardInterrupt):
        line("\n    （没有输入，跳过）")
        return None
    if not token:
        line("    （空，跳过）")
        return None

    # 类型
    if token.startswith("ghp_"):
        kind = "classic"
        line("    类型        classic (ghp_)")
    elif token.startswith("github_pat_"):
        kind = "fine-grained"
        line("    类型        fine-grained (github_pat_)")
        line("                ^ 这种 token 的权限必须逐项勾选，最容易漏")
    elif token.startswith("gho_") or token.startswith("ghs_"):
        kind = "other"
        line("    类型        %s…（不是普通 PAT）" % token[:4])
    else:
        kind = "unknown"
        line("    类型        无法识别（不是 GitHub token？）")

    # 身份
    s, d = call("/user", token)
    if s == 200:
        line("    账号        %s" % d.get("login"))
    else:
        line("    账号        读取失败 -> %s %s" % (s, str(d)[:120]))
        line()
        line("    结论：token 本身无效（写错了 / 已过期 / 已被撤销）")
        return None

    # 读仓库
    s, d = call("/repos/%s/%s" % (OWNER, REPO), token)
    if s == 200:
        line("    读仓库      可以")
    else:
        line("    读仓库      失败 -> %s %s" % (s, str(d)[:120]))

    # 写权限
    s, d = call("/repos/%s/%s/git/ref/heads/main" % (OWNER, REPO), token)
    head = (d.get("object", {}) or {}).get("sha") if s == 200 else None
    if head:
        line("    远程 main   %s" % head[:12])

    s, d = call("/repos/%s/%s/git/blobs" % (OWNER, REPO), token, "POST",
                {"content": "cHJvYmUK", "encoding": "base64"})   # "probe\n"
    if s == 201:
        line("    写权限      %s有" % OK)
        return "writable"
    line("    写权限      %s没有" % BAD)
    line("                -> %s %s" % (s, str(d)[:160]))
    line()
    line("    结论：token 没有写入权限 —— 问题就在这里，与网络和脚本无关。")
    line()
    line("    请重建一个 CLASSIC token（一个勾选就够）：")
    line("      https://github.com/settings/tokens/new?scopes=repo&description=DefectClassic")
    if kind == "fine-grained":
        line()
        line("    如果你坚持用 fine-grained，这两项必须同时满足，漏一个就 403：")
        line("      - Repository access     -> 选中 %s/%s" % (OWNER, REPO))
        line("      - Repository permissions -> Contents: Read and write")
    return "readonly"


def main():
    line("=" * 62)
    line("  推送问题诊断  (%s/%s)" % (OWNER, REPO))
    line("=" * 62)
    line()
    inrepo = check_env()
    net = check_network()
    if not net:
        line("    结论：连不上 api.github.com —— 是网络问题，先解决网络。")
        return 1
    state = check_token()
    line()
    line("=" * 62)
    if state == "writable":
        line("  token 没问题。直接双击「推送 mod 到 GitHub」即可。")
        line("  如果那次仍然失败，把窗口里的文字发我。")
    elif state is None:
        line("  没有拿到可用的 token，诊断到此为止。")
    line("=" * 62)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        print("\n诊断脚本自身出错：%s: %s" % (type(e).__name__, e))
        sys.exit(1)
