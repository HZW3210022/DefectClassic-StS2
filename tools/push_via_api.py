#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Push the local git tree to GitHub through the REST API instead of `git push`.

Why this exists: on this machine `git push` keeps failing inside the TLS layer
(schannel CRYPT_E_NO_REVOCATION_CHECK, or simply "could not connect to
github.com:443"), while plain HTTPS to api.github.com is rock solid (~0.3s).
So the commit is rebuilt server-side with the Git Data API:

    for each file -> POST /git/blobs      (base64, so binaries survive)
    then          -> POST /git/trees
    then          -> POST /git/commits    (parent = current remote head)
    then          -> PATCH /git/refs/heads/<branch>

The file list comes from `git ls-files` and the commit message is taken from
the local HEAD, so the result matches the local commit.

Usage:
    python tools/push_via_api.py <owner> <repo> <token> [branch]

Environment:
    PUSH_ROOT     repository directory (default: cwd)
    PUSH_MESSAGE  overrides the commit message (default: local HEAD message)
    GITHUB_TOKEN  used when no token is given on the command line

The token may instead be kept in a file (never committed):
    ~/.defect-classic-token      <- preferred, outside the repository
    <repo>/.github-token         <- fallback, git-ignored
Pass "-" as the token argument to force reading from there.
"""

import base64
import glob
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor

API = "https://api.github.com"
ROOT = os.environ.get("PUSH_ROOT", os.getcwd())

TOKEN_FILES = [
    os.path.join(os.path.expanduser("~"), ".defect-classic-token"),
    os.path.join(os.path.dirname(os.path.abspath(__file__)), os.pardir, ".github-token"),
]


def resolve_token(argv_token):
    """Token precedence: command line -> env var -> token file."""
    if argv_token and argv_token not in ("-", "auto", ""):
        return argv_token
    for name in ("GITHUB_TOKEN", "GH_TOKEN"):
        val = os.environ.get(name)
        if val and val.strip():
            print("token      : from environment variable %s" % name)
            return val.strip()
    for path in TOKEN_FILES:
        path = os.path.abspath(path)
        if os.path.isfile(path):
            val = open(path, encoding="utf-8").read().strip()
            if val:
                print("token      : from file %s" % path)
                return val
    return None


def find_git():
    """Locate git.exe without relying on PATH.

    On this machine cmd.exe has no `git` at all (there is no Git for Windows
    install), which is why the first version of this script died with
    FileNotFoundError. The git that the editor uses is the PortableGit that
    ships alongside it, so look there too.
    """
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
        # reverse: prefer the highest version number when several are installed
        for hit in sorted(glob.glob(pat), reverse=True):
            if os.path.isfile(hit):
                return hit
    return None


GIT = find_git()
if not GIT:
    sys.exit("找不到 git。请把 git.exe 所在目录加入 PATH，"
             "或安装 Git for Windows 后重试。\n"
             "（本机可用的位置一般在 "
             r"%USERPROFILE%\.workbuddy\binaries\PortableGit\versions\*\cmd" " 下）")


def git(*args):
    return subprocess.run([GIT] + list(args), cwd=ROOT, capture_output=True,
                          text=True, encoding="utf-8").stdout


def api(method, path, token, payload=None, tries=5, raise_on_4xx=True):
    data = json.dumps(payload).encode() if payload is not None else None
    last = None
    for attempt in range(1, tries + 1):
        req = urllib.request.Request(API + path, data=data, method=method, headers={
            "Authorization": "Bearer " + token,
            "Accept": "application/vnd.github+json",
            "Content-Type": "application/json",
            "User-Agent": "DefectClassic-push",
        })
        try:
            with urllib.request.urlopen(req, timeout=90) as r:
                body = r.read()
                return json.loads(body) if body else {}
        except urllib.error.HTTPError as e:
            detail = e.read().decode("utf-8", "replace")[:300]
            # GitHub signals "you are going too fast" as 403 with this text,
            # and it IS worth waiting out - unlike a genuine permission 403.
            secondary = e.code == 403 and "secondary rate limit" in detail.lower()
            retryable = e.code >= 500 or e.code == 429 or secondary
            if not retryable:
                if raise_on_4xx:
                    raise SystemExit("[%s %s] HTTP %s: %s" % (method, path, e.code, detail))
                return e.code, detail
            last = "HTTP %s: %s" % (e.code, detail[:160])
            if secondary:
                wait = 30 * attempt
                print("      (secondary rate limit - waiting %ds before retry %d/%d)"
                      % (wait, attempt, tries), flush=True)
                time.sleep(wait)
                continue
        except Exception as e:            # timeouts, connection resets
            last = "%s: %s" % (type(e).__name__, e)
        time.sleep(1.5 * attempt)
    if not raise_on_4xx:
        return None, last
    raise SystemExit("[%s %s] failed after %d tries - %s" % (method, path, tries, last))


def local_files():
    # core.quotePath=false: otherwise git escapes non-ASCII names as \345\215\241...
    return [p for p in git("-c", "core.quotePath=false", "ls-files").splitlines() if p.strip()]


def local_message():
    msg = (os.environ.get("PUSH_MESSAGE") or git("log", "-1", "--format=%B")).strip()
    return msg or "Push from local tree"


def get_ref(owner, repo, token, branch):
    """Current head sha of `branch`, or None when the branch does not exist yet."""
    try:
        return api("GET", "/repos/%s/%s/git/ref/heads/%s" % (owner, repo, branch), token)["object"]["sha"]
    except SystemExit as e:
        if "HTTP 404" in str(e) or "HTTP 409" in str(e):
            return None
        raise


def seed_empty_repo(owner, repo, token, branch):
    """GitHub refuses `POST /git/blobs` while the repo has no commit at all,
    so create one throwaway file first; it is replaced by the real tree below."""
    api("PUT", "/repos/%s/%s/contents/.initializing" % (owner, repo), token, {
        "message": "Initialize repository",
        "content": base64.b64encode(b"placeholder, replaced by the next commit\n").decode(),
        "branch": branch,
    })
    print("  seeded empty repository (temporary file, will be replaced)")


def api_probe(path, token):
    """Like api(), but always returns (status, payload-or-detail) and never raises."""
    req = urllib.request.Request(API + path, headers={
        "Authorization": "Bearer " + token,
        "Accept": "application/vnd.github+json",
        "User-Agent": "DefectClassic-push",
    })
    try:
        with urllib.request.urlopen(req, timeout=45) as r:
            return r.status, json.loads(r.read().decode() or "{}")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")[:300]
    except Exception as e:
        return None, "%s: %s" % (type(e).__name__, e)


def check_token(token):
    """Validate the token up front, so a mistyped one fails fast with a clear
    message instead of surfacing later as an opaque 403/404."""
    looks_ok = token.startswith(("ghp_", "github_pat_", "gho_", "ghs_", "ghu_"))
    s, d = api_probe("/user", token)
    if s == 200:
        print("token      : %s (%s…, username %s)"
              % ("classic" if token.startswith("ghp_") else "fine-grained/other",
                 token[:8], d.get("login")))
        return True

    print("\n!! 这个 token 用不了，先别推了 —— 下面是诊断：")
    if not looks_ok:
        print("   - 格式不对：GitHub token 应以 ghp_ / github_pat_ 开头，")
        print("     而你给的是 %r 开头、共 %d 个字符。" % (token[:6], len(token)))
        print("     classic token 的完整长度是 40 —— 很可能是复制时漏了开头的字符。")
    if s == 401:
        print("   - 服务端回 401 Bad credentials：token 无效 / 已过期 / 已被撤销。")
    elif s == 403:
        print("   - 服务端回 403：token 有效但权限不足（大概没勾 repo 或 Contents）。")
    else:
        print("   - 服务端回 %s：%s" % (s, str(d)[:200]))
    print("\n   重新建一个（repo 权限已勾好，点 Generate 即可）：")
    print("     https://github.com/settings/tokens/new?scopes=repo&description=DefectClassic")
    print("   复制时务必取到整串（ghp_ 开头、40 个字符），别漏首尾字符。\n")
    return False


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 1
    owner, repo = sys.argv[1], sys.argv[2]
    token = resolve_token(sys.argv[3] if len(sys.argv) > 3 else None)
    if not token:
        print("找不到 token。三种给法，任选其一：")
        print("  1) 作为第 3 个参数： push_via_api.py <owner> <repo> <token>")
        print("  2) 环境变量 GITHUB_TOKEN")
        print("  3) 写进文件： %s" % TOKEN_FILES[0])
        return 1
    branch = sys.argv[4] if len(sys.argv) > 4 else "main"

    if not check_token(token):
        return 1

    dirty = git("status", "--porcelain").strip()
    if dirty:
        print("warning: the working tree has uncommitted changes; those files are")
        print("         pushed as they exist on disk, but stay uncommitted locally.\n")

    files = local_files()
    total = sum(os.path.getsize(os.path.join(ROOT, p)) for p in files)
    print("files : %d  (%.2f MB)" % (len(files), total / 1048576))

    parent = get_ref(owner, repo, token, branch)
    if parent is None:
        print("branch %s does not exist yet" % branch)
        seed_empty_repo(owner, repo, token, branch)
        parent = get_ref(owner, repo, token, branch)
        root_commit = True
    else:
        print("remote head: %s" % parent[:12])
        root_commit = False

    message = local_message()
    print("message    : %s" % message.splitlines()[0][:70])

    # ---- 1) blobs (parallel) -------------------------------------------------
    def make_blob(path):
        with open(os.path.join(ROOT, path), "rb") as fh:
            content = base64.b64encode(fh.read()).decode()
        res = api("POST", "/repos/%s/%s/git/blobs" % (owner, repo), token,
                  {"content": content, "encoding": "base64"})
        return {"path": path, "mode": "100644", "type": "blob", "sha": res["sha"]}

    t0 = time.time()
    entries, done = [], 0
    with ThreadPoolExecutor(max_workers=5) as pool:
        for entry in pool.map(make_blob, files):
            entries.append(entry)
            done += 1
            if done % 80 == 0 or done == len(files):
                print("  blobs %d/%d  (%.0fs)" % (done, len(files), time.time() - t0), flush=True)

    # ---- 2) tree -------------------------------------------------------------
    tree = api("POST", "/repos/%s/%s/git/trees" % (owner, repo), token, {"tree": entries})
    print("tree       : %s" % tree["sha"][:12])

    # ---- 3) commit -----------------------------------------------------------
    # On the very first push build a root commit and force the branch onto it, so
    # the seed file above leaves no trace in history.
    commit = api("POST", "/repos/%s/%s/git/commits" % (owner, repo), token,
                 {"message": message, "tree": tree["sha"],
                  "parents": [] if root_commit else [parent]})
    print("commit     : %s%s" % (commit["sha"][:12], "  (root)" if root_commit else ""))

    # ---- 4) point the branch at it -------------------------------------------
    api("PATCH", "/repos/%s/%s/git/refs/heads/%s" % (owner, repo, branch), token,
        {"sha": commit["sha"], "force": root_commit})

    # ---- 5) verify -----------------------------------------------------------
    info = api("GET", "/repos/%s/%s" % (owner, repo), token)
    check = api("GET", "/repos/%s/%s/git/trees/%s?recursive=1" % (owner, repo, tree["sha"]), token)
    blobs = [t for t in check["tree"] if t["type"] == "blob"]
    print("\nverified   : %d files on remote, repo %s KB, default branch %s"
          % (len(blobs), info.get("size"), info.get("default_branch")))
    print("https://github.com/%s/%s" % (owner, repo))
    return 0


if __name__ == "__main__":
    sys.exit(main())
