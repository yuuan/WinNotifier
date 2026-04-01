#!/bin/sh
# Install winnotify and winnotify-send
# https://github.com/yuuan/WinNotifier
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/yuuan/WinNotifier/main/tools/notify-sh/install.sh | sh
#   curl -fsSL ... | sh -s -- --notify-send   # also create notify-send symlink
set -e

link_notify_send=false
for arg in "$@"; do
  case "$arg" in
    --notify-send) link_notify_send=true ;;
  esac
done

repo_owner="yuuan"
repo_name="WinNotifier"
base_url="https://raw.githubusercontent.com/${repo_owner}/${repo_name}/main/tools/notify-sh"
install_dir="${HOME}/.local/bin"
config_dir="${XDG_CONFIG_HOME:-$HOME/.config}/winnotifier"

mkdir -p "$install_dir" "$config_dir"

echo "Installing winnotify to ${install_dir} ..."

curl -fsSL "${base_url}/winnotify" -o "${install_dir}/winnotify"
chmod +x "${install_dir}/winnotify"

curl -fsSL "${base_url}/winnotify-send" -o "${install_dir}/winnotify-send"
chmod +x "${install_dir}/winnotify-send"

# Record installed commit hash
commit=$(curl -sS "https://api.github.com/repos/${repo_owner}/${repo_name}/commits/main" \
  -H "Accept: application/vnd.github.sha" 2>/dev/null) || true
if [ -n "$commit" ] && [ ${#commit} -eq 40 ]; then
  printf '%s\n' "$commit" > "${config_dir}/version"
fi

if [ "$link_notify_send" = true ]; then
  "${install_dir}/winnotify-send" --install
fi

echo "Installed: winnotify, winnotify-send"

case ":${PATH}:" in
  *":${install_dir}:"*) ;;
  *) echo "Warning: ${install_dir} is not in your PATH." >&2 ;;
esac
